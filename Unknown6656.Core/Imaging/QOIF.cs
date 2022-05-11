// #define DEBUG_LOG

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Unknown6656.Generics;
using Unknown6656.IO;

namespace Unknown6656.Imaging;

#pragma warning disable CA1416 // Validate platform compatibility


public static class QOIF
{
    private static readonly NotImplementedException _version_not_implemented =
        new("Any other QOIF version than the original proposal (https://qoiformat.org/qoi-specification.pdf) is currently not supported.");
    private static readonly Dictionary<QOIFVersion, QOIFImplementation> _funcs = new()
    {
        [QOIFVersion.Original] = new QOIF_V1(),
        [QOIFVersion.V2] = new QOIF_V2(),
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

    public static void SaveQOIFImage(this Bitmap bitmap, string path, QOIFVersion format_version = QOIFVersion.Original) => SaveQOIFImage(bitmap, new FileInfo(path), format_version);

    public static void SaveQOIFImage(this Bitmap bitmap, FileInfo path, QOIFVersion format_version = QOIFVersion.Original)
    {
        using DataStream ds = new();

        SaveQOIFImage(bitmap, ds, format_version);

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
#if DEBUG_LOG
        StringBuilder sb = new StringBuilder().AppendLine(header.ToString()).AppendLine("DECODING");
#endif
        bitmap.LockRGBAPixels((ptr, w, h) =>
        {
            int index = 0;
#if DEBUG_LOG
            void set_pixel(RGBAColor color, string log)
#else
            void set_pixel(RGBAColor color)
#endif
            {
                if (index < w * h)
                {
                    int cache = GetIndex(color);
#if DEBUG_LOG
                    sb.AppendLine($"I={index,6} X={index % w,5} Y={index / w,5} P={previous,12} C={color,12} (CH={cache,2}) {log}");
#endif
                    indexed[cache] = color;
                    ptr[index++] = color;

                    if (previous != color)
                        previous = color;
                }
#if DEBUG_LOG
                else
                    sb.AppendLine($" <<<< index {index} >= {w}x{h} = {w * h} >>>>");
#endif
            }

            try
            {
                while (index < w * h && end_match < END.Length && rd.ReadByte() is byte @byte)
                    if (@byte is TAG_OP_RGB or TAG_OP_RGBA)
                    {
                        RGBAColor color = new(
                            rd.ReadByte(),
                            rd.ReadByte(),
                            rd.ReadByte()
                        );

                        if (@byte is TAG_OP_RGBA)
                            color.A = rd.ReadByte();
#if DEBUG_LOG
                        set_pixel(color, "EXPLICIT");
#else
                        set_pixel(color);
#endif
                    }
                    else
                    {
                        int op = @byte & MASK_TAG2;
                        byte data = (byte)(@byte & ~MASK_TAG2);

                        if (op is TAG_OP_INDEX)
                        {
#if DEBUG_LOG
                            set_pixel(indexed[data], $"INDEXED  {@byte:x2} I={data}");
#else
                            set_pixel(indexed[data]);
#endif
                            if (@byte == END[end_match])
                                ++end_match;
                            else
                                end_match = 0;
                        }
                        else if (op is TAG_OP_DIFF)
                        {
                            int dr = (@byte & MASK_DIFF_R) >> SHIFT_DIFF_R;
                            int dg = (@byte & MASK_DIFF_G) >> SHIFT_DIFF_G;
                            int db = @byte & MASK_DIFF_B;

                            RGBAColor color = ComputeFrom2BitDistance(previous, dr, dg, db);
#if DEBUG_LOG
                            set_pixel(color, $"2BITDIFF {@byte:x2} ({dr} {dg} {db})");
#else
                            set_pixel(color);
#endif
                        }
                        else if (op is TAG_OP_LUMA)
                        {
                            @byte = rd.ReadByte();

                            int drg = (@byte & MASK_LUMA_RG) >> SHIFT_LUMA_RG;
                            int dbg = @byte & MASK_LUMA_BG;
                            RGBAColor color = ComputeFromLuma(previous, 6, 4, drg, data, dbg);
#if DEBUG_LOG
                            set_pixel(color, $"LUMADIFF {op | data:x2}{@byte:x2} ({data} {drg} {dbg})");
#else
                            set_pixel(color);
#endif
                        }
                        else if (op is TAG_OP_RUN)
                        {
                            for (int i = 0, c = data + 1; i < c && index < w * h; ++i)
#if DEBUG_LOG
                                set_pixel(previous, $"RUN {@byte:x2} R={data + 1}");
#else
                                set_pixel(previous);
#endif
                        }
#if DEBUG_LOG
                        else
                            sb.AppendLine($" <<<< UNKNOWN INSTRUCTION {@byte:x2} >>>>");
#endif
                    }
            }
            catch (EndOfStreamException)
            {
#if DEBUG_LOG
                sb.AppendLine($" <<<< UNEXPECTED EOF >>>>");
#endif
            }
        });
#if DEBUG_LOG
        DataStream.FromStringBuilder(sb).ToFile("qoi-v1-decoder-debug.log");
#endif
        if (header.Channels is QOIFChannels.RGB)
            return bitmap.ToRGB24();
        else if (header.Channels is not QOIFChannels.RGBA)
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

        QOIFHeader header = new(bitmap.Width, bitmap.Height, QOIFVersion.Original, channels, QOIFColorSpace.AllLinear);

        wr.WriteNative(header);
#if DEBUG_LOG
        StringBuilder sb = new StringBuilder().AppendLine(header.ToString()).AppendLine("ENCODING");
#endif
        RGBAColor[] indexed = new RGBAColor[64];
        RGBAColor previous = RGBAColor.Black;
        int rept = 0;

        bitmap.ToARGB32().LockRGBAPixels((ptr, w, h) =>
        {
            for (int i = 0; i < w * h; ++i)
            {
                RGBAColor current = ptr[i];
                int cache_index = GetIndex(current);
#if DEBUG_LOG
                void write_to_log(string log) => sb.AppendLine($"I={i,6} X={i % w,5} Y={i / w,5} P={previous,12} C={current,12} (CH={cache_index,2}) {log}");
#endif
                void update()
                {
                    previous = current;
                    indexed[GetIndex(current)] = current;
                }

                if (current == previous && rept < 62)
                    ++rept;
                else
                {
                    if (rept > 0)
                    {
                        wr.Write((byte)(TAG_OP_RUN | ((rept - 1) & ~MASK_TAG2)));
#if DEBUG_LOG
                        write_to_log($"RUN {rept}");
#endif
                    }

                    if (current == previous)
                        rept = 1;
                    else
                    {
                        rept = 0;

                        for (int idx = 0; idx < indexed.Length; ++idx)
                            if (indexed[idx] == current)
                            {
                                wr.Write((byte)(TAG_OP_INDEX | (idx & ~MASK_TAG2)));
#if DEBUG_LOG
                                write_to_log($"INDEXED  I={idx}");
#endif
                                update();

                                continue;
                            }

                        if (ptr[i].A == previous.A)
                            if (Try2BitDistance(previous, current, out int dr, out int dg, out int db))
                            {
                                wr.Write((byte)(TAG_OP_DIFF | dr << SHIFT_DIFF_R | dg << SHIFT_DIFF_G | db));
#if DEBUG_LOG
                                write_to_log($"2BITDIFF {previous} {TAG_OP_DIFF | dr << SHIFT_DIFF_R | dg << SHIFT_DIFF_G | db:x2} ({dr} {dg} {db})");
#endif
                                update();

                                continue;
                            }
                            else if (TryLumaDistance(previous, current, 6, 4, out dr, out dg, out db))
                            {
                                wr.Write((byte)(TAG_OP_LUMA | dg));
                                wr.Write((byte)(dr << SHIFT_LUMA_RG | db));
#if DEBUG_LOG
                                write_to_log($"LUMADIFF {previous} {TAG_OP_LUMA | dg:x2}{dr << SHIFT_LUMA_RG | db:x2} ({dg} {dr} {db})");
#endif
                                update();

                                continue;
                            }

                        wr.Write(current.A != previous.A ? TAG_OP_RGBA : TAG_OP_RGB);
                        wr.Write(current.R);
                        wr.Write(current.G);
                        wr.Write(current.B);

                        if (current.A != previous.A)
                            wr.Write(current.A);
#if DEBUG_LOG
                        write_to_log("EXPLICIT");
#endif
                        update();
                    }
                }
            }
        });

        wr.Write(END);
#if DEBUG_LOG
        DataStream.FromStringBuilder(sb).ToFile("qoi-v1-encoder-debug.log");
#endif
    }

    internal static bool Try2BitDistance(RGBAColor previous, RGBAColor current, out int δr, out int δg, out int δb)
    {
        δr = current.R - previous.R + 2;
        δg = current.G - previous.G + 2;
        δb = current.B - previous.B + 2;

        return δr is >= 0 and <= 3 && δg is >= 0 and <= 3 && δb is >= 0 and <= 3;
    }

    internal static RGBAColor ComputeFrom2BitDistance(RGBAColor previous, int δr, int δg, int δb) => new(
        (byte)(previous.R + δr - 2),
        (byte)(previous.G + δg - 2),
        (byte)(previous.B + δb - 2),
        previous.A
    );

    internal static bool TryLumaDistance(RGBAColor previous, RGBAColor current, int g_bitdepth, int rb_bitdepth, out int δr, out int δg, out int δb)
    {
        int bias_g = 1 << (g_bitdepth - 1);
        int bias_rb = 1 << (rb_bitdepth - 1);

        δg = current.G - previous.G;
        δr = current.R - previous.R - δg + bias_rb;
        δb = current.B - previous.B - δg + bias_rb;
        δg += bias_g;

        return δr >= 0 && δr < (bias_rb << 1)
            && δg >= 0 && δg < (bias_g << 1)
            && δb >= 0 && δb < (bias_rb << 1);
    }

    internal static RGBAColor ComputeFromLuma(RGBAColor previous, int g_bitdepth, int rb_bitdepth, int δr, int δg, int δb)
    {
        int bias_g = 1 << (g_bitdepth - 1);
        int bias_rb = 1 << (rb_bitdepth - 1);

        δg -= bias_g;

        int r = previous.R + δg + δr - bias_rb;
        int b = previous.B + δg + δb - bias_rb;
        int g = previous.G + δg;

        return new((byte)r, (byte)g, (byte)b, previous.A);
    }

    internal static int GetIndex(RGBAColor color) => (color.R * 3 + color.G * 5 + color.B * 7 + color.A * 11) & 0x3F;
}

internal sealed unsafe class QOIF_V2
    : QOIFImplementation
{
    private const byte MASK_RUN = 0b_____________1111_1000;
    private const byte TAG_RUN_PREV = 0b_________1011_0000;
    private const byte TAG_RUN_PPREV = 0b________1011_1000;
    private const byte TAG_PAL_INDEX = 0b________1010_0000;
    private const byte MASK_PAL_INDEX = 0b_______1111_0000;
    private const byte TAG_INDEXED64 = 0b________0000_0000;
    private const byte MASK_INDEXED64 = 0b_______1100_0000;
    private const byte TAG_2BIT_DIFF = 0b________0100_0000;
    private const byte MASK_2BIT_DIFF = 0b_______1100_0000;
    private const byte TAG_2CHN_DIFF = 0b________1000_0000;
    private const byte MASK_2CHN_DIFF = 0b_______1110_0000;
    private const byte MASK_2CHN_DIFF_SEL = 0b___0001_1100;
    private const byte MASK_2CHN_DIFF_CH1_1 = 0b_0000_0011;
    private const byte MASK_2CHN_DIFF_CH1_2 = 0b_1110_0000;
    private const byte MASK_2CHN_DIFF_CH2 = 0b___0001_1111;
    private const byte TAG_LUMA_DIFF = 0b________1100_0000;
    private const byte MASK_LUMA_DIFF = 0b_______1110_0000;
    private const byte TAG_AVG = 0b______________1110_0000;
    private const byte MASK_AVG = 0b_____________1111_1100;
    private const byte BIT_AVG_TOPLEFT = 0b______0000_0001;
    private const byte BIT_AVG_PPREVIOUS = 0b____0000_0010;
    private const byte TAG_LUMA_TOP_DIFF = 0b____1001_1100;
    private const byte MASK_LUMA_TOP_DIFF = 0b___1111_1100;
    private const byte TAG_1CHN_DIFF = 0b________1001_1000;
    private const byte TAG_REPT_TOP = 0b_________1110_0100;
    private const byte TAG_REPT_TOPLEFT = 0b_____1110_0101;
    private const byte TAG_RGB = 0b______________1001_1001;
    private const byte TAG_RGBA = 0b_____________1001_1010;
    private const byte MASK_RGBA = 0b____________1111_1110;
    private const byte BIT_RGBA_SETPAL = 0b______0000_0001;


    public override Bitmap Load(QOIFHeader header, BinaryReader rd)
    {
        Bitmap bitmap = new(header.Width, header.Height, PixelFormat.Format32bppArgb);
#if DEBUG_LOG
        StringBuilder sb = new StringBuilder()
                         .AppendLine(header.ToString())
                         .AppendLine($"DECODING {DateTime.Now}")
                         .AppendLine(" INDEX X-POS Y-POS   P.PREVIOUS     PREVIOUS      CURRENT CACHE ACTION");
#endif
        bitmap.LockRGBAPixels((ptr, w, h) =>
        {
            RGBAColor[] palette = new RGBAColor[16];
            RGBAColor[] indexed = new RGBAColor[64];
            RGBAColor pprevious = RGBAColor.Black;
            RGBAColor previous = RGBAColor.Black;
            int palette_set_index = 0;
            int index = 0;
#if DEBUG_LOG
            void set_pixel(RGBAColor color, string log)
#else
            void set_pixel(RGBAColor color)
#endif
            {
                if (index < w * h)
                {
                    int cache = GetIndex(color);
#if DEBUG_LOG
                    sb.AppendLine($"{index,6} {index % w,5} {index / w,5} {pprevious,12} {previous,12} {color,12} {cache,5} {log}");
#endif
                    ptr[index++] = color;

                    if (previous != color)
                    {
                        indexed[cache] = color;
                        pprevious = previous;
                        previous = color;
                    }
                }
#if DEBUG_LOG
                else
                    sb.AppendLine($" <<<< index {index} >= {w}x{h} = {w * h} >>>>");
#endif
            }

            try
            {
                while (index < w * h && rd.ReadByte() is byte @byte)
                {
                    int x = index % w;
                    int y = index / w;

                    if ((@byte & MASK_INDEXED64) == TAG_INDEXED64)
                    {
                        int cache_idx = @byte & ~MASK_INDEXED64;
#if DEBUG_LOG
                        set_pixel(indexed[cache_idx], $"INDEXED {cache_idx}");
#else
                        set_pixel(indexed[cache_idx]);
#endif
                    }
                    else if ((@byte & MASK_2BIT_DIFF) == TAG_2BIT_DIFF)
                    {
                        int dr = (@byte >> 4) & 3;
                        int dg = (@byte >> 2) & 3;
                        int db = @byte & 3;
                        RGBAColor color = QOIF_V1.ComputeFrom2BitDistance(previous, dr, dg, db);
#if DEBUG_LOG
                        set_pixel(color, $"2BITDIFF {@byte:x2} ({dr} {dg} {db})");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_LUMA_DIFF) == TAG_LUMA_DIFF)
                    {
                        int dg = @byte & ~MASK_LUMA_DIFF;
                        int dr = rd.ReadByte();
                        int db = dr & 15;

                        dr >>= 4;

                        RGBAColor color = QOIF_V1.ComputeFromLuma(previous, 5, 4, dr, dg, db);
#if DEBUG_LOG
                        set_pixel(color, $"LUMADIFF ({dg} {dr} {db})");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_PAL_INDEX) == TAG_PAL_INDEX)
                    {
                        int pal_idx = @byte & ~MASK_PAL_INDEX;
#if DEBUG_LOG
                        set_pixel(palette[pal_idx], $"PALETTE {pal_idx}");
#else
                        set_pixel(palette[pal_idx]);
#endif
                    }
                    else if ((@byte & MASK_RUN) is TAG_RUN_PPREV or TAG_RUN_PREV)
                    {
                        RGBAColor color = (@byte & MASK_RUN) == TAG_RUN_PREV ? previous : pprevious;
                        int count = (@byte & ~MASK_RUN) + 2;

                        while (count-- > 1 && index < w * h)
                            ptr[index++] = color;
#if DEBUG_LOG
                        set_pixel(color, $"RUN {((@byte & MASK_RUN) is TAG_RUN_PPREV ? "PP" : "")} {@byte:x2} r={(@byte & ~MASK_RUN) + 2}");
#else
                        set_pixel(color);
#endif
                    }
                    else if (@byte == TAG_1CHN_DIFF)
                    {
                        @byte = rd.ReadByte();
                        int d = (@byte & 63) - 32;
                        QOIFv2_1CHDIFF ch = (QOIFv2_1CHDIFF)(@byte >> 6);
                        RGBAColor color = pprevious;

                        if (ch is QOIFv2_1CHDIFF.R)
                            color.R = (byte)(color.R + d);
                        else if (ch is QOIFv2_1CHDIFF.G)
                            color.G = (byte)(color.G + d);
                        else if (ch is QOIFv2_1CHDIFF.B)
                            color.B = (byte)(color.B + d);
                        else if (ch is QOIFv2_1CHDIFF.A)
                            color.A = (byte)(color.A + d);
#if DEBUG_LOG
                        set_pixel(color, $"1CHNDIFF (d={d}, ch={ch})");
#else
                        set_pixel(color);
#endif
                    }
                    else if (@byte == TAG_RGB)
                    {
                        RGBAColor color = new(rd.ReadByte(), rd.ReadByte(), rd.ReadByte(), previous.A);
#if DEBUG_LOG
                        set_pixel(color, "EXPLICIT");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_RGBA) == TAG_RGBA)
                    {
                        RGBAColor color = new(rd.ReadByte(), rd.ReadByte(), rd.ReadByte(), rd.ReadByte());

                        if ((@byte & BIT_RGBA_SETPAL) != 0)
                        {
                            palette[palette_set_index++] = color;
                            palette_set_index %= palette.Length;
                        }
#if DEBUG_LOG
                        set_pixel(color, $"EXPLICIT{((@byte & BIT_RGBA_SETPAL) != 0 ? $" + PALETTE {palette_set_index - 1}" : "")}");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_LUMA_TOP_DIFF) == TAG_LUMA_TOP_DIFF)
                    {
                        int dg = @byte & ~MASK_LUMA_TOP_DIFF;
                        int dr = rd.ReadByte();
                        int db = dr & 7;

                        dg = dg << 2 | dr >> 6;
                        dr = (dr >> 3) & 7;

                        RGBAColor top = y > 0 ? ptr[(y - 1) * w + x] : RGBAColor.Black;
                        RGBAColor color = QOIF_V1.ComputeFromLuma(top, 4, 3, dr, dg, db);
#if DEBUG_LOG
                        set_pixel(color, $"LUMA TOP ({dg} {dr} {db})");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_2CHN_DIFF) == TAG_2CHN_DIFF)
                    {
                        QOIFv2_2CHDIFF diff2ch = (QOIFv2_2CHDIFF)((@byte >> 2) & 7);
                        int ch1 = (@byte & 3) << 3;
                        int ch2;

                        @byte = rd.ReadByte();
                        ch1 |= @byte >> 5;
                        ch2 = @byte & 31;
                        ch1 -= 16;
                        ch2 -= 16;

                        (int dr, int dg, int db, int da) = diff2ch switch
                        {
                            QOIFv2_2CHDIFF.RG => (ch1, ch2, 0, 0),
                            QOIFv2_2CHDIFF.RB => (ch1, 0, ch2, 0),
                            QOIFv2_2CHDIFF.RA => (ch1, 0, 0, ch2),
                            QOIFv2_2CHDIFF.GB => (0, ch1, ch2, 0),
                            QOIFv2_2CHDIFF.GA => (0, ch1, 0, ch2),
                            QOIFv2_2CHDIFF.BA => (0, 0, ch1, ch2),
                        };
                        RGBAColor color = new((byte)(previous.R + dr), (byte)(previous.G + dg), (byte)(previous.B + db), (byte)(previous.A + da));
#if DEBUG_LOG
                        set_pixel(color, $"2CHNDIFF ({diff2ch} {dr} {dg} {db} {da})");
#else
                        set_pixel(color);
#endif
                    }
                    else if ((@byte & MASK_AVG) == TAG_AVG)
                    {
                        bool pp = (@byte & BIT_AVG_PPREVIOUS) != 0;
                        bool tl = (@byte & BIT_AVG_TOPLEFT) != 0;
                        RGBAColor top = y > 0 ? ptr[(y - 1) * w + x] : RGBAColor.Black;
                        RGBAColor topleft = x > 0 && y > 0 ? ptr[(y - 1) * w + x - 1] : top;
                        RGBAColor color = (pp, tl) switch
                        {
                            (false, false) => Average(previous, top),
                            (false, true) => Average(previous, top, topleft),
                            (true, false) => Average(previous, pprevious, top),
                            (true, true) => Average(previous, pprevious, top, topleft),
                        };
#if DEBUG_LOG
                        set_pixel(color, $"AVG {@byte} (pp={pp} tl={tl})");
#else
                        set_pixel(color);
#endif
                    }
                    else if (@byte is TAG_REPT_TOP or TAG_REPT_TOPLEFT)
                    {
                        RGBAColor top = x >= 0 && y >= 0 ? ptr[(y - 1) * w + x - (@byte is TAG_REPT_TOPLEFT ? 1 : 0)] : RGBAColor.Black;
#if DEBUG_LOG
                        set_pixel(top, $"TOP/TOPLEFT {@byte:x2}");
#else
                        set_pixel(top);
#endif
                    }
#if DEBUG_LOG
                    else
                        sb.AppendLine($" <<<< UNKNOWN INSTRUCTION {@byte:x2} >>>>");
#endif
                }
            }
            catch (EndOfStreamException)
            {
#if DEBUG_LOG
                sb.AppendLine($" <<<< UNEXPECTED EOF >>>>");
#endif
            }
        });
#if DEBUG_LOG
        DataStream.FromStringBuilder(sb).ToFile("qoi-v2-decoder-debug.log");
#endif
        if (header.Channels is QOIFChannels.RGB)
            return bitmap.ToRGB24();
        else if (header.Channels is not QOIFChannels.RGBA)
            throw new NotImplementedException();

        return bitmap;
    }

    public override void Save(Bitmap bitmap, BinaryWriter wr)
    {
        QOIFChannels channels = QOIFChannels.RGBA;
        ColorPalette palette = bitmap.Palette;

        if (bitmap.PixelFormat is PixelFormat.Format24bppRgb)
            channels = QOIFChannels.RGB;
        else if (bitmap.PixelFormat is not PixelFormat.Format32bppArgb)
            bitmap = bitmap.ToARGB32();

        if (palette.Count < 16)
            palette += GetPalette(bitmap, 16 - palette.Count);

        QOIFHeader header = new(bitmap.Width, bitmap.Height, QOIFVersion.V2, channels, QOIFColorSpace.AllLinear);
#if DEBUG_LOG
        StringBuilder sb = new StringBuilder()
                         .AppendLine(header.ToString())
                         .AppendLine($"ENCODING {DateTime.Now}")
                         .AppendLine(" INDEX X-POS Y-POS   P.PREVIOUS     PREVIOUS      CURRENT CACHE ACTION");
#endif
        wr.WriteNative(header);
        bitmap.LockRGBAPixels((ptr, w, h) =>
        {
            RGBAColor[] palette_colors = palette.Colors.ToArray();
            int palette_set_idx = 0;
            RGBAColor[] indexed = new RGBAColor[64];
            RGBAColor pprevious = RGBAColor.Black;
            RGBAColor previous = RGBAColor.Black;

            if (palette_colors.Length > 16)
            {
                for (int i = 0; i < indexed.Length && i + 16 < palette_colors.Length; ++i)
                    indexed[i] = palette_colors[i + 16];

                Array.Resize(ref palette_colors, 16);
            }

            for (int index = 0; index < w * h; ++index)
            {
                RGBAColor current = ptr[index];
                int pal_idx = palette_colors.IndexOf(current);
#if DEBUG_LOG
                void update(string log)
#else
                void update()
#endif
                {
                    int cache = GetIndex(current);
#if DEBUG_LOG
                    sb.AppendLine($"{index,6} {index % w,5} {index / w,5} {pprevious,12} {previous,12} {current,12} {cache,5} {log}");
#endif
                    if (current != previous)
                    {
                        indexed[cache] = current;
                        pprevious = previous;
                        previous = current;
                    }
                }
                int? get_run_length(RGBAColor refcol)
                {
                    int j = index;
                    int len = 0;

                    while (len < 9 && j < w * h && ptr[j] == refcol)
                    {
                        ++len;
                        ++j;
                    }

                    return len > 1 ? len - 2 : null;
                }
                int dr, dg, db;

                if (get_run_length(pprevious) is int rlen_pprev)
                {
                    wr.Write((byte)(TAG_RUN_PPREV | rlen_pprev));

                    index += rlen_pprev + 1;
#if DEBUG_LOG
                    update($"RUN PP {TAG_RUN_PPREV | rlen_pprev:x2} R={rlen_pprev + 2}");
#endif
                }
                else if (get_run_length(previous) is int rlen_prev)
                {
                    wr.Write((byte)(TAG_RUN_PREV | rlen_prev));

                    index += rlen_prev + 1;
#if DEBUG_LOG
                    update($"RUN {TAG_RUN_PPREV | rlen_prev:x2} R={rlen_prev + 2}");
#endif
                }
                else if (pal_idx >= 0)
                    if (pal_idx == palette_set_idx)
                    {
                        ++palette_set_idx;

                        wr.Write((byte)(TAG_RGBA | BIT_RGBA_SETPAL));
                        wr.Write(current.R);
                        wr.Write(current.G);
                        wr.Write(current.B);
                        wr.Write(current.A);
#if DEBUG_LOG
                        update($"EXPLICIT + PALETTE {pal_idx}");
#endif
                    }
                    else
                    {
                        wr.Write((byte)(TAG_PAL_INDEX | pal_idx));
#if DEBUG_LOG
                        update($"PALETTE {pal_idx}");
#endif
                    }
                else if (indexed.IndexOf(current) is int cache_idx and >= 0)
                {
                    wr.Write((byte)(TAG_INDEXED64 | cache_idx));
#if DEBUG_LOG
                    update($"INDEXED {cache_idx}");
#endif
                }
                else if (QOIF_V1.Try2BitDistance(previous, current, out dr, out dg, out db))
                {
                    wr.Write((byte)(TAG_2BIT_DIFF | dr << 4 | dg << 2 | db));
#if DEBUG_LOG
                    update($"2BITDIFF {TAG_2BIT_DIFF | dr << 4 | dg << 2 | db:x2} ({dr} {dg} {db})");
#endif
                }
                else
                {
                    int x = index % w;
                    int y = index / w;
                    bool matched = false;

                    if (y > 0)
                    {
                        RGBAColor top = ptr[(y - 1) * w + x];
                        RGBAColor topleft = x > 0 ? ptr[(y - 1) * w + x - 1] : top;

                        if (matched = (current == top))
                        {
                            wr.Write(TAG_REPT_TOP);
#if DEBUG_LOG
                            update("TOP");
#endif
                        }
                        else if (matched = (current == topleft))
                        {
                            wr.Write(TAG_REPT_TOPLEFT);
#if DEBUG_LOG
                            update("TOPLEFT");
#endif
                        }
                        else
                        {
                            bool tl = false, pp = false;

                            if (current == Average(previous, top))
                                matched = true;
                            else if (current == Average(previous, pprevious, top))
                                pp = true;
                            else if (x > 0 && current == Average(previous, top, topleft))
                                tl = true;
                            else if (x > 0 && current == Average(previous, pprevious, top, topleft))
                                (tl, pp) = (true, true);

                            if (matched |= pp | tl)
                            {
                                wr.Write((byte)(TAG_AVG | (tl ? BIT_AVG_TOPLEFT : 0) | (pp ? BIT_AVG_PPREVIOUS : 0)));
#if DEBUG_LOG
                                update($"AVG (pp={pp} tl={tl})");
#endif
                            }
                            else if (matched = QOIF_V1.TryLumaDistance(top, current, 4, 3, out dr, out dg, out db))
                            {
                                wr.Write((byte)(TAG_LUMA_TOP_DIFF | dg >> 2));
                                wr.Write((byte)(dg << 6 | dr << 3 | db));
#if DEBUG_LOG
                                update($"LUMA TOP ({dg} {dr} {db})");
#endif
                            }
                        }
                    }

                    if (!matched)
                        if (matched = QOIF_V1.TryLumaDistance(previous, current, 5, 4, out dr, out dg, out db))
                        {
                            wr.Write((byte)(TAG_LUMA_DIFF | dg));
                            wr.Write((byte)(dr << 4 | db));
#if DEBUG_LOG
                            update($"LUMADIFF ({dg} {dr} {db})");
#endif
                        }
                        else if (Try2ChannelDifference(current, previous, out int c1, out int c2) is QOIFv2_2CHDIFF diff2ch)
                        {
                            matched = true;
                            wr.Write((byte)(TAG_2CHN_DIFF | (int)diff2ch << 2 | c1 >> 3));
                            wr.Write((byte)(c1 << 5 | c2));
#if DEBUG_LOG
                            update($"2CHNDIFF ({diff2ch} {c1} {c2})");
#endif
                        }
                        else if (Try1ChannelDifference(current, previous, out int c0) is QOIFv2_1CHDIFF diff1ch)
                        {
                            matched = true;
                            wr.Write(TAG_1CHN_DIFF);
                            wr.Write((byte)((int)diff1ch << 6 | c0));
#if DEBUG_LOG
                            update($"1CHNDIFF ({diff1ch} {c0})");
#endif
                        }

                    if (!matched)
                    {
                        wr.Write(current.A == previous.A ? TAG_RGB : TAG_RGBA);
                        wr.Write(current.R);
                        wr.Write(current.G);
                        wr.Write(current.B);

                        if (current.A != previous.A)
                            wr.Write(current.A);
#if DEBUG_LOG
                        update("EXPLICIT");
#endif
                    }
                }
#if !DEBUG_LOG
                update();
#endif
            }
        });
#if DEBUG_LOG
        DataStream.FromStringBuilder(sb).ToFile("qoi-v2-encoder-debug.log");
#endif
    }

    private static ColorPalette GetPalette(Bitmap bmp, int count)
    {
        int size = (int)Math.Sqrt(count + 1) + 1;
        Bitmap resized = new(bmp, size, size);

        return new(resized.ToColorPalette().Colors.Take(count));
    }

    internal static RGBAColor Average(params RGBAColor[] colors)
    {
        int r = 0, g = 0, b = 0, a = 0, i = 0;

        for (; i < colors.Length; ++i)
        {
            r += colors[i].R;
            g += colors[i].G;
            b += colors[i].B;
            a += colors[i].A;
        }

        return new(
            (byte)(r / i),
            (byte)(g / i),
            (byte)(b / i),
            (byte)(a / i)
        );
    }

    internal static QOIFv2_2CHDIFF? Try2ChannelDifference(RGBAColor current, RGBAColor previous, out int c1, out int c2)
    {
        int? r = current.R == previous.R ? null : current.R - previous.R + 16;
        int? g = current.G == previous.G ? null : current.G - previous.G + 16;
        int? b = current.B == previous.B ? null : current.B - previous.B + 16;
        int? a = current.A == previous.A ? null : current.A - previous.A + 16;
        bool? inside_range(int? c) => c is null ? null : c is >= 0 and <= 31;

        (QOIFv2_2CHDIFF? diff, c1, c2) = (inside_range(r), inside_range(g), inside_range(b), inside_range(a)) switch
        {
            (true, true, null, null) => (QOIFv2_2CHDIFF.RG, r.Value, g.Value),
            (true, null, true, null) => (QOIFv2_2CHDIFF.RB, r.Value, b.Value),
            (true, null, null, true) => (QOIFv2_2CHDIFF.RA, r.Value, a.Value),
            (null, true, true, null) => (QOIFv2_2CHDIFF.GB, g.Value, b.Value),
            (null, true, null, true) => (QOIFv2_2CHDIFF.GA, g.Value, a.Value),
            (null, null, true, true) => (QOIFv2_2CHDIFF.BA, b.Value, a.Value),
            _ => ((QOIFv2_2CHDIFF?, int, int))(null, 0, 0),
        };

        return diff;
    }

    internal static QOIFv2_1CHDIFF? Try1ChannelDifference(RGBAColor current, RGBAColor previous, out int c0)
    {
        int? r = current.R == previous.R ? null : current.R - previous.R + 32;
        int? g = current.G == previous.G ? null : current.G - previous.G + 32;
        int? b = current.B == previous.B ? null : current.B - previous.B + 32;
        int? a = current.A == previous.A ? null : current.A - previous.A + 32;
        bool? inside_range(int? c) => c is null ? null : c is >= 0 and <= 63;

        (QOIFv2_1CHDIFF? diff, c0) = (inside_range(r), inside_range(g), inside_range(b), inside_range(a)) switch
        {
            (true, null, null, null) => (QOIFv2_1CHDIFF.R, r.Value),
            (null, true, null, null) => (QOIFv2_1CHDIFF.G, g.Value),
            (null, null, true, null) => (QOIFv2_1CHDIFF.B, b.Value),
            (null, null, null, true) => (QOIFv2_1CHDIFF.A, a.Value),
            _ => ((QOIFv2_1CHDIFF?, int))(null, 0),
        };

        return diff;
    }

    internal static int GetIndex(RGBAColor color) => QOIF_V1.GetIndex(color); // (int)((uint)HashCode.Combine(color.A, color.R, color.G, color.B) % 64);
}

// TODO : add future protocol versions

internal unsafe struct QOIFHeader
{
    private fixed byte Magic[3];
    public readonly QOIFVersion Version;
    public readonly int Width;
    public readonly int Height;
    public readonly QOIFChannels Channels;
    public readonly QOIFColorSpace Colorspace;


    public QOIFHeader(int width, int height, QOIFVersion version, QOIFChannels channels, QOIFColorSpace colorspace)
    {
        Magic[0] = (byte)'q';
        Magic[1] = (byte)'o';
        Magic[2] = (byte)'i';
        Width = width;
        Height = height;
        Version = version;
        Channels = channels;
        Colorspace = colorspace;
    }

    public override readonly string ToString() => $"[V={Version}] {Width}x{Height}, CH={Channels}, CS={Colorspace}";
}

public enum QOIFVersion
    : byte
{
    Original = (byte)'f',
    V2 = (byte)'2',

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

internal enum QOIFv2_2CHDIFF
    : byte
{
    RG = 0b000,
    RB = 0b001,
    RA = 0b010,
    GB = 0b011,
    GA = 0b100,
    BA = 0b101,
}

internal enum QOIFv2_1CHDIFF
    : byte
{
    R = 0b00,
    G = 0b01,
    B = 0b10,
    A = 0b11,
}
