#define DEBUG_LOG

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System;

using Unknown6656.IO;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Unknown6656.Imaging;

#pragma warning disable CA1416 // Validate platform compatibility


public static class QOIF
{
    private static readonly NotImplementedException _version_not_implemented =
        new("Any other QOIF version than the original proposal (https://qoiformat.org/qoi-specification.pdf) is currently not supported.");
    private static readonly Dictionary<QOIFVersion, QOIFImplementation> _funcs = new()
    {
        [QOIFVersion.Original] = new QOIF_V1(),
        // TODO : add new protocol versions
    };

    public static Bitmap LoadQOIFImage(string path) => LoadQOIFImage(new FileInfo(path));

    public static Bitmap LoadQOIFImage(FileInfo path) => LoadQOIFImage(DataStream.FromFile(path));

    public static Bitmap LoadQOIFImage(Stream stream, bool seek_beginning = false)
    {
        if (seek_beginning && stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        BinaryReader rd = new(stream);
        QOIFHeader header = rd.ReadNative<QOIFHeader>();

        return _funcs.TryGetValue(header.Version, out QOIFImplementation? impl) ? impl.Load(header, rd) : throw _version_not_implemented;
    }

    public static void SaveQOIFImage(this Bitmap bitmap, string path) => SaveQOIFImage(bitmap, new FileInfo(path));

    public static void SaveQOIFImage(this Bitmap bitmap, FileInfo path)
    {
        using DataStream ds = new();

        SaveQOIFImage(bitmap, ds);

        ds.ToFile(path);
    }

    public static void SaveQOIFImage(this Bitmap bitmap, Stream stream) => SaveQOIFImage(bitmap, stream, QOIFVersion.Original);

    public static void SaveQOIFImage(this Bitmap bitmap, Stream stream, QOIFVersion format_version)
    {
        BinaryWriter wr = new(stream);

        if (_funcs.TryGetValue(format_version, out QOIFImplementation? impl))
            impl.Save(bitmap, wr);
        else
            throw _version_not_implemented;
    }
}

internal abstract class QOIFImplementation
{
    public abstract Bitmap Load(QOIFHeader header, BinaryReader rd);
    public abstract void Save(Bitmap bitmap, BinaryWriter wr);
}

internal sealed unsafe class QOIF_V1
    : QOIFImplementation
{
    private const byte TAG_OP_RGB = 0b_1111_1110;
    private const byte TAG_OP_RGBA = 0b_1111_1111;
    private const byte TAG_OP_INDEX = 0b_0000_0000;
    private const byte TAG_OP_DIFF = 0b_0100_0000;
    private const byte TAG_OP_LUMA = 0b_1000_0000;
    private const byte TAG_OP_RUN = 0b_1100_0000;
    private const byte MASK_TAG2 = 0b_1100_0000;
    private const byte MASK_DIFF_R = 0b_0011_0000;
    private const byte MASK_DIFF_G = 0b_0000_1100;
    private const byte MASK_DIFF_B = 0b_0000_0011;
    private const int SHIFT_DIFF_R = 4;
    private const int SHIFT_DIFF_G = 2;
    private const byte MASK_LUMA_RG = 0b_1111_0000;
    private const byte MASK_LUMA_BG = 0b_0000_1111;
    private const int SHIFT_LUMA_RG = 4;

    private static readonly byte[] END = { 0, 0, 0, 0, 0, 0, 0, 1 };


    public override Bitmap Load(QOIFHeader header, BinaryReader rd)
    {
        Bitmap bitmap = new(header.Width, header.Height, PixelFormat.Format32bppArgb);
        RGBAColor[] indexed = new RGBAColor[64];
        RGBAColor previous = RGBAColor.Black;
        int end_match = 0;

        bitmap.LockRGBAPixels((ptr, w, h) =>
        {
            int index = 0;
            void set_pixel(RGBAColor color)
            {
                indexed[GetIndex(color)] = color;
                ptr[index++] = previous = color;
            }

            while (index < w * h && end_match < END.Length && rd.ReadByte() is byte @byte)
                if (@byte is TAG_OP_RGB or TAG_OP_RGBA)
                {
                    previous.R = rd.ReadByte();
                    previous.G = rd.ReadByte();
                    previous.B = rd.ReadByte();

                    if (@byte is TAG_OP_RGBA)
                        previous.A = rd.ReadByte();

                    set_pixel(previous);
                }
                else
                {
                    int op = @byte & MASK_TAG2;
                    byte data = (byte)(@byte & ~MASK_TAG2);

                    if (op is TAG_OP_INDEX)
                    {
                        set_pixel(indexed[data]);

                        if (@byte == END[end_match])
                            ++end_match;
                        else
                            end_match = 0;
                    }
                    else if (op is TAG_OP_DIFF)
                    {
                        previous.R = (byte)(previous.R + ((@byte & MASK_DIFF_R) >> SHIFT_DIFF_R) - 2);
                        previous.G = (byte)(previous.G + ((@byte & MASK_DIFF_G) >> SHIFT_DIFF_G) - 2);
                        previous.B = (byte)(previous.B + (@byte & MASK_DIFF_B) - 2);

                        set_pixel(previous);
                    }
                    else if (op is TAG_OP_LUMA)
                    {
                        @byte = rd.ReadByte();

                        int dg = data - 32;
                        int drg = ((@byte & MASK_LUMA_RG) >> SHIFT_LUMA_RG) - 8;
                        int dbg = (@byte & MASK_LUMA_BG) - 8;

                        RGBAColor curr = previous;

                        curr.G = (byte)(previous.G + dg);
                        curr.R = (byte)(previous.R + dg + drg);
                        curr.B = (byte)(previous.B + dg + dbg);

                        set_pixel(curr);
                    }
                    else if (op is TAG_OP_RUN)
                        for (int i = 0, c = data + 1; i < c && index < w * h; ++i)
                            set_pixel(previous);
                }
        });

        if (header.Channels is QOIFChannels.RGB)
            return bitmap.ToRGB24();
        else if (header.Channels is QOIFChannels.RGBA)
            throw new NotImplementedException();

        return bitmap;
    }

    public override void Save(Bitmap bitmap, BinaryWriter wr)
    {
        QOIFChannels channels = QOIFChannels.RGBA;

        if (bitmap.PixelFormat is PixelFormat.Format24bppRgb)
            channels = QOIFChannels.RGB;
        else if (bitmap.PixelFormat is not PixelFormat.Format32bppArgb)
            bitmap = bitmap.ToARGB32();

        wr.WriteNative(new QOIFHeader
        {
            Channels = channels,
            Colorspace = QOIFColorSpace.AllLinear,
            Width = bitmap.Width,
            Height = bitmap.Height,
            Version = QOIFVersion.Original,
        });

        RGBAColor[] indexed = new RGBAColor[64];
        RGBAColor previous = RGBAColor.Black;
        int rept = 0;

        bitmap.ToARGB32().LockRGBAPixels((ptr, w, h) =>
        {
            for (int i = 0; i < w * h; ++i)
            {
                RGBAColor current = ptr[i];

                if (current == previous && rept < 62)
                    ++rept;
                else
                {
                    if (rept > 0)
                        wr.Write((byte)(TAG_OP_RUN | ((rept - 1) & ~MASK_TAG2)));

                    if (current == previous)
                        rept = 1;
                    else
                    {
                        rept = 0;

                        for (int idx = 0; idx < indexed.Length; ++idx)
                            if (indexed[idx] == current)
                            {
                                wr.Write((byte)(TAG_OP_INDEX | (idx & ~MASK_TAG2)));

                                goto update;
                            }

                        if (ptr[i].A == previous.A)
                        {
                            int dr = current.R - previous.R + 2;
                            int dg = current.G - previous.G + 2;
                            int db = current.B - previous.B + 2;

                            if (dr is >= 0 and <= 3 && dg is >= 0 and <= 3 && db is >= 0 and <= 3)
                            {
                                wr.Write((byte)(TAG_OP_DIFF | (dr << SHIFT_DIFF_R) | (dg << SHIFT_DIFF_G) | db));

                                goto update;
                            }

                            dr -= dg - 8;
                            db -= dg - 8;
                            dg += 30;

                            if (dg is >= 0 and <= 63 && dr is >= 0 and <= 15 && db is >= 0 and <= 15)
                            {
                                wr.Write((byte)(TAG_OP_LUMA | dg));
                                wr.Write((byte)((dr << SHIFT_LUMA_RG) | db));

                                goto update;
                            }
                        }

                        wr.Write(current.A != previous.A ? TAG_OP_RGBA : TAG_OP_RGB);
                        wr.Write(current.R);
                        wr.Write(current.G);
                        wr.Write(current.B);

                        if (current.A != previous.A)
                            wr.Write(current.A);
update:
                        previous = current;
                        indexed[GetIndex(current)] = current;
                    }
                }
            }
        });

        wr.Write(END);
    }

    private static int GetIndex(RGBAColor color) => (color.R * 3 + color.G * 5 + color.B * 7 + color.A * 11) & 0x3F;
}

// TODO : add future protocol versions

internal unsafe struct QOIFHeader
{
    public QOIFVersion Version;
    public int Width;
    public int Height;
    public QOIFChannels Channels;
    public QOIFColorSpace Colorspace;
}

public enum QOIFVersion
    : uint
{
    Original = 0x514f4946u, // magic = "QOIF"
    V2 = 0x514f4932u, // magic = "QOI2"

    // TODO : add future protocol versions
}

internal enum QOIFChannels
    : byte
{
    RGB = 3,
    RGBA = 4,
}

internal enum QOIFColorSpace
    : byte
{
    sRGB_LinearAlpha = 0,
    AllLinear = 1,
}
