using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System;

using Unknown6656.IO;

namespace Unknown6656.Imaging;


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

    public static Bitmap LoadQOIFImage(Stream stream)
    {
        using BinaryReader rd = new(stream);
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
        using BinaryWriter wr = new(stream);

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
    private const byte TAG_MASK2 = 0b_1100_0000;

    private static readonly byte[] END = { 0, 0, 0, 0, 0, 0, 0, 1 };


    public override Bitmap Load(QOIFHeader header, BinaryReader rd)
    {
        Bitmap bitmap = new(header.Width, header.Height, header.Channels switch
        {
            QOIFChannels.RGB => PixelFormat.Format24bppRgb,
            QOIFChannels.RGBA => PixelFormat.Format32bppArgb,
            _ => throw new NotImplementedException(),
        });
        RGBAColor[] indexed = new RGBAColor[64];
        RGBAColor previous = RGBAColor.Black;
        int end_match = 0;

        bitmap.LockRGBAPixels((ptr, w, h) =>
        {
            int index = 0;

            while (index < w * h && end_match < END.Length && rd.ReadByte() is byte @byte)
                if (@byte == END[end_match])
                    ++end_match;
                else if (@byte is TAG_OP_RGB or TAG_OP_RGBA)
                {
                    previous.R = rd.ReadByte();
                    previous.G = rd.ReadByte();
                    previous.B = rd.ReadByte();

                    if (@byte is TAG_OP_RGBA)
                        previous.A = rd.ReadByte();

                    ptr[index++] = previous;
                }
                else
                {
                    int op = @byte & TAG_MASK2;

                    if (op is TAG_OP_INDEX)
                        ;
                    else if (op is TAG_OP_DIFF)
                        ;
                    else if (op is TAG_OP_LUMA)
                        ;
                    else if (op is TAG_OP_RUN)
                        ;
                    else
                // TODO

            }
        });
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

        for (int i = 0, c = bitmap.Width * bitmap.Height; i < c; ++i)
        {

        }
    }

    private static int GetIndex(RGBAColor color) => (color.R * 3 + color.G * 5 + color.B * 7 + color.A * 11) & 0x3F;
}

// TODO : add new protocol versions

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
    0 = sRGB_LinearAlpha,
    1 = AllLinear,
}
