#define USE_CACHE

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
#if USE_CACHE
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.Statistics;
using Unknown6656.Generics;

namespace Unknown6656.Imaging;

using sys_palette = System.Drawing.Imaging.ColorPalette;


public class ColorPalette<@this, Color>
    : IEnumerable<Color>
    where @this : ColorPalette<@this, Color>
    where Color : unmanaged, IColor<Color>
{
    public HashSet<Color> Colors { get; }

    public int Count => Colors.Count;


    public ColorPalette(params Color[] colors)
        : this(colors as IEnumerable<Color>)
    {
    }

    public ColorPalette(IEnumerable<Color> colors) => Colors = new(colors);

    public override string ToString()
    {
        const int LIMIT = 5;

        return $"{Colors.Count} Colors: [{Colors.Take(LIMIT).StringJoin(", ")}{(Colors.Count > LIMIT ? ", ..." : "")}]";
    }

    public override bool Equals(object? obj) => obj is ColorPalette<@this, Color> other && Colors.SetEquals(other.Colors);

    public override int GetHashCode()
    {
        int i = Colors.Count;

        foreach (uint argb in from c in Colors
                              let argb = c.ToARGB32()
                              orderby argb
                              select argb)
            i = HashCode.Combine(i, argb);

        return i;
    }

    public IEnumerator<Color> GetEnumerator() => Colors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(Color color) => Colors.Contains(color);

    public @this Intersect(ColorPalette<@this, Color> second) => new ColorPalette<@this, Color>(Colors.Intersect(second.Colors));

    public @this Except(ColorPalette<@this, Color> second) => new ColorPalette<@this, Color>(Colors.Except(second.Colors));

    public @this Union(ColorPalette<@this, Color> second) => new ColorPalette<@this, Color>(Colors.Union(second.Colors));

    public static bool operator ==(ColorPalette<@this, Color>? c1, @this? c2) => c1?.Equals(c2) ?? c2 is null;

    public static bool operator !=(ColorPalette<@this, Color>? c1, @this? c2) => !(c1 == c2);

    public static bool operator ==(@this? c1, ColorPalette<@this, Color>? c2) => c2 == c1;

    public static bool operator !=(@this? c1, ColorPalette<@this, Color>? c2) => c2 != c1;

    public static implicit operator @this(ColorPalette<@this, Color> palette) => (@this)palette;
}

/// <completionlist cref="ColorPalette"/>
public class ColorPalette
    : ColorPalette<ColorPalette, RGBAColor>
{
    public const bool UsesCache =
#if USE_CACHE
        true;

    private readonly ConcurrentDictionary<(ColorEqualityMetric metric, uint color), (uint color, double distance)> _cache = new();
#else
        false;
#endif
    private static readonly ConstructorInfo _palette_ctor = typeof(sys_palette).GetConstructor([typeof(int)])!;


    public static ReadOnlyIndexer<int, ColorPalette> Grayscale { get; } = new(count => new(from i in Enumerable.Range(0, count)
                                                                                           let gray = i / (double)(count - 1)
                                                                                           select new RGBAColor(gray, gray, gray)));

    public static ReadOnlyIndexer<int, ColorPalette> Reds { get; } = new(count => new(from i in Enumerable.Range(0, count)
                                                                                      let v = i / (double)(count - 1)
                                                                                      select new RGBAColor(v, 0, 0)));

    public static ReadOnlyIndexer<int, ColorPalette> Greens { get; } = new(count => new(from i in Enumerable.Range(0, count)
                                                                                        let v = i / (double)(count - 1)
                                                                                        select new RGBAColor(0, v, 0)));

    public static ReadOnlyIndexer<int, ColorPalette> Blues { get; } = new(count => new(from i in Enumerable.Range(0, count)
                                                                                       let v = i / (double)(count - 1)
                                                                                       select new RGBAColor(0, 0, v)));

    public static ReadOnlyIndexer<RGBAColor, ColorPalette> Analogous { get; } = new(c => c.Analogous);

    public static ReadOnlyIndexer<RGBAColor, ColorPalette> Neutrals { get; } = new(c => c.Neutrals);

    public static ReadOnlyIndexer<RGBAColor, ColorPalette> Triadics { get; } = new(c => c.Triadic.ToArray());

    public static ColorPalette BlackAndWhite { get; } = Grayscale[2];

    public static ColorPalette PrimaryColors { get; } = new(RGBAColor.Red, RGBAColor.Lime, RGBAColor.Blue);

    public static ColorPalette PrimaryAndComplementaryColors { get; } = new(
        RGBAColor.Black,
        RGBAColor.White,
        RGBAColor.Red,
        RGBAColor.Yellow,
        RGBAColor.Lime,
        RGBAColor.Cyan,
        RGBAColor.Blue,
        RGBAColor.Magenta
    );

    public static ReadOnlyIndexer<ConsoleColorScheme, ColorPalette> ConsoleColorSchemes { get; } = new(scheme =>
    {
        Dictionary<ConsoleColor, uint> dict = IColor.ConsoleColorSchemes[scheme];

        return new(Enum.GetValues<ConsoleColor>().Select(color => (RGBAColor)dict[color]));
    });

    public static ColorPalette Windows10_ConsoleColors { get; } = ConsoleColorSchemes[ConsoleColorScheme.Windows10];

    public static ColorPalette Legacy_ConsoleColors { get; } = ConsoleColorSchemes[ConsoleColorScheme.Legacy];

    public static ColorPalette ANSI_3Bit_RGB { get; } = new(Enumerable.Range(0, 8).Select(i => Color.FromArgb((i & 1) * 255, ((i >> 1) & 1) * 255, ((i >> 2) & 1) * 255)));

    public static ColorPalette _6Bit_RGB { get; } = new(Enumerable.Range(0, 64).Select(i => Color.FromArgb((i & 3) * 85, ((i >> 2) & 3) * 85, ((i >> 4) & 3) * 85)));

    //public static ColorPalette WebSafeColors { get; } = new(Enumerable.Range(0, 256).Select(i => ));

    public static ColorPalette CGA16 { get; } = new(
        RGBAColor.Black,
        0x00a,
        0x0a0,
        0x0aa,
        0xa00,
        0xa0a,
        0xa50,
        0xaaa,
        0x555,
        0x55f,
        0x5f5,
        0x5ff,
        0xf55,
        0xf5f,
        0xff5,
        RGBAColor.White
    );

    public static ColorPalette ScenePAL { get; } = new(
        0x080000u,
        0x201a0bu,
        0x432817u,
        0x492910u,
        0x234309u,
        0x5d4f1eu,
        0x9c6b20u,
        0xa9220fu,
        0x2b347cu,
        0x2b7409u,
        0xd0ca40u,
        0xe8a077u,
        0x6a94abu,
        0xd5c4b3u,
        0xfce76eu,
        0xfcfae2u
    );

    public static ColorPalette CGAMode4_Palette1_LowIntensity { get; } = new(RGBAColor.Black, 0x0aa, 0xa0a, 0xaaa);

    public static ColorPalette CGAMode4_Palette1_HighIntensity { get; } = new(RGBAColor.Black, 0x5ff, 0xf5f, RGBAColor.White);

    public static ColorPalette CGAMode4_Palette2_LowIntensity { get; } = new(RGBAColor.Black, 0x0a0, 0xa00, 0xa50);

    public static ColorPalette CGAMode4_Palette2_HighIntensity { get; } = new(RGBAColor.Black, 0x5f5, 0xf55, 0xff5);

    public static ColorPalette CGAMode5_LowIntensity { get; } = new(RGBAColor.Black, 0x0aa, 0xa00, 0xaaa);

    public static ColorPalette CGAMode5_HighIntensity { get; } = new(RGBAColor.Black, 0x5ff, 0xf55, RGBAColor.White);

    public static ColorPalette BIOS16Colors { get; } = new(
        RGBAColor.Black,
        RGBAColor.Navy,
        RGBAColor.Blue,
        RGBAColor.Green,
        RGBAColor.Lime,
        RGBAColor.Teal,
        RGBAColor.Cyan,
        RGBAColor.Maroon,
        RGBAColor.Red,
        RGBAColor.Purple,
        RGBAColor.Magenta,
        RGBAColor.Olive,
        RGBAColor.Yellow,
        RGBAColor.Gray,
        RGBAColor.White
    );

    public static ColorPalette IBM_PCjr_Tandy1000 { get; } = new(
        RGBAColor.Black,
        0x00a,
        0x0a0,
        0x0aa,
        0xa00,
        0xa0a,
        0xaaa,
        0x555,
        0x55f,
        0x5f5,
        0x5ff,
        0xf55,
        0xf5f,
        0xff5
    );

    public static ColorPalette ThomsonMO5 { get; } = new(
        RGBAColor.Black,
        RGBAColor.Red,
        RGBAColor.Lime,
        RGBAColor.Yellow,
        RGBAColor.Blue,
        RGBAColor.Magenta,
        RGBAColor.Cyan,
        RGBAColor.White,
        0xbbb,
        0xfbb,
        0xbfb,
        0xffb,
        0xfbf,
        0xeb0
    );

    public static ColorPalette ThomsonTO770_Palette1 { get; } = new(
        RGBAColor.Black,
        RGBAColor.Red,
        RGBAColor.Lime,
        RGBAColor.Yellow,
        RGBAColor.Blue,
        RGBAColor.Magenta,
        RGBAColor.Cyan,
        RGBAColor.White,
        0xd77,
        0xeb0,
        0xdd7,
        0x7d7,
        0xbff,
        0x77d,
        0xd7e,
        0xbbb
    );

    public static ColorPalette ThomsonTO770_Palette2 { get; } = new(
        RGBAColor.Black,
        RGBAColor.Red,
        RGBAColor.Lime,
        RGBAColor.Yellow,
        RGBAColor.Blue,
        RGBAColor.Magenta,
        RGBAColor.Cyan,
        RGBAColor.White,
        0x555,
        0xaaa,
        0xc00,
        0x0a0,
        0x00e,
        0x099,
        0xb0b,
        0x770
    );

    public static ColorPalette ThomsonTO770_Palette3 { get; } = new(
        RGBAColor.Black,
        RGBAColor.Red,
        RGBAColor.Lime,
        RGBAColor.Yellow,
        RGBAColor.Blue,
        RGBAColor.Magenta,
        RGBAColor.Cyan,
        RGBAColor.White,
        0x555,
        0x00a,
        0xa00,
        0xf5f,
        0x0a0,
        0x5ff,
        0xff5,
        0xaaa
    );

    public static ColorPalette MattelAquarius { get; } = new(
        0x161616u,
        0xd8102eu,
        0x11c400u,
        0xd0be16u,
        0x0a1cc4u,
        0xc916dbu,
        0x03cbadu,
        0xfefefeu,
        0xc4c4c4u,
        0x49ad9eu,
        0x943a9cu,
        0x1a2379u,
        0xc1b861u,
        0x47a03eu,
        0x912d3cu,
        0x161616u
    );

    public static ColorPalette AmstradCPC { get; } = new(
        RGBAColor.Black,
        RGBAColor.Navy,
        RGBAColor.Blue,
        RGBAColor.Maroon,
        RGBAColor.Purple,
        0x8000ff,
        RGBAColor.Red,
        0xff0080,
        RGBAColor.Magenta,
        RGBAColor.Green,
        RGBAColor.Teal,
        0x0080ff,
        RGBAColor.Olive,
        RGBAColor.Gray,
        0x8080ff,
        0xff8000,
        0xff8080,
        0xff80ff,
        RGBAColor.Lime,
        0x00ff80,
        RGBAColor.Cyan,
        0x80ff00,
        0x80ff80,
        0x80ffff,
        RGBAColor.Yellow,
        0xffff80,
        RGBAColor.White
    );

    public static ColorPalette MSX2_8Bit_RGB { get; } = new(Enumerable.Range(0, 256).Select(i =>
    {
        int r = (int)(((i & 0b_1110_0000) >> 5) * 36.5);
        int g = (int)(((i & 0b_0001_1100) >> 2) * 36.5);
        int b = (i & 0b_0000_0011) * 85;

        return new RGBAColor((byte)r, (byte)g, (byte)b);
    }));

    public static ColorPalette MSX2_YamahaV9938_9Bit_RGB { get; } = new(Enumerable.Range(0, 512).Select(i =>
    {
        int r = (int)(((i & 0x1c0) >> 6) * 36.5);
        int g = (int)(((i & 0x038) >> 3) * 36.5);
        int b = (int)((i & 7) * 36.5);

        return new RGBAColor((byte)r, (byte)g, (byte)b);
    }));


    private static ColorPalette? _15bppRGB = null, _18bppRGB = null;

    public static ColorPalette MSX2P_YamahaV9958_15Bit_RGB => _15bppRGB ??= new(Enumerable.Range(0, 32768).Select(i =>
    {
        int r = (int)(((i & 0x7c00) >> 10) * 8.23);
        int g = (int)(((i & 0x03e0) >> 5) * 8.23);
        int b = (int)((i &  0x001f) * 8.23);

        return new RGBAColor((byte)r, (byte)g, (byte)b);
    }));

    public static ColorPalette Fujitsu_18Bit_RGB =>  _18bppRGB ??= new(Enumerable.Range(0, 262144).Select(i =>
    {
        int r = (int)(((i & 0x3f000) >> 12) * 4.05);
        int g = (int)(((i & 0x00fc0) >> 6) * 4.05);
        int b = (int)((i &  0x0003f) * 4.05);

        return new RGBAColor((byte)r, (byte)g, (byte)b);
    }));

    public static ColorPalette CTIA { get; } = new(
        RGBAColor.Black,
        0x404040,
        0x6C6C6C,
        0x909090,
        0xB0B0B0,
        0xC8C8C8,
        0xDCDCDC,
        0xECECEC,
        0x444400,
        0x646410,
        0x848424,
        0xA0A034,
        0xB8B840,
        0xD0D050,
        0xE8E85C,
        0xFCFC68,
        0x702800,
        0x844414,
        0x985C28,
        0xAC783C,
        0xBC8C4C,
        0xCCA05C,
        0xDCB468,
        0xECC878,
        0x841800,
        0x983418,
        0xAC5030,
        0xC06848,
        0xD0805C,
        0xE09470,
        0xECA880,
        0xFCBC94,
        0x880000,
        0x9C2020,
        0xB03C3C,
        0xC05858,
        0xD07070,
        0xE08888,
        0xECA0A0,
        0xFCB4B4,
        0x78005C,
        0x8C2074,
        0xA03C88,
        0xB0589C,
        0xC070B0,
        0xD084C0,
        0xDC9CD0,
        0xECB0E0,
        0x480078,
        0x602090,
        0x783CA4,
        0x8C58B8,
        0xA070CC,
        0xB484DC,
        0xC49CEC,
        0xD4B0FC,
        0x140084,
        0x302098,
        0x4C3CAC,
        0x6858C0,
        0x7C70D0,
        0x9488E0,
        0xA8A0EC,
        0xBCB4FC,
        0x000088,
        0x1C209C,
        0x3840B0,
        0x505CC0,
        0x6874D0,
        0x7C8CE0,
        0x90A4EC,
        0xA4B8FC,
        0x00187C,
        0x1C3890,
        0x3854A8,
        0x5070BC,
        0x6888CC,
        0x7C9CDC,
        0x90B4EC,
        0xA4C8FC,
        0x002C5C,
        0x1C4C78,
        0x386890,
        0x5084AC,
        0x689CC0,
        0x7CB4D4,
        0x90CCE8,
        0xA4E0FC,
        0x003C2C,
        0x1C5C48,
        0x387C64,
        0x509C80,
        0x68B494,
        0x7CD0AC,
        0x90E4C0,
        0xA4FCD4,
        0x003C00,
        0x205C20,
        0x407C40,
        0x5C9C5C,
        0x74B474,
        0x8CD08C,
        0xA4E4A4,
        0xB8FCB8,
        0x143800,
        0x345C1C,
        0x507C38,
        0x6C9850,
        0x84B468,
        0x9CCC7C,
        0xB4E490,
        0xC8FCA4,
        0x2C3000,
        0x4C501C,
        0x687034,
        0x848C4C,
        0x9CA864,
        0xB4C078,
        0xCCD488,
        0xE0EC9C,
        0x442800,
        0x644818,
        0x846830,
        0xA08444,
        0xB89C58,
        0xD0B46C,
        0xE8CC7C,
        0xFCE08C
    );

    public static ColorPalette GTIA { get; } = new(
        RGBAColor.Black,
        0x111111,
        0x222222,
        0x333333,
        0x444444,
        0x555555,
        0x666666,
        0x777777,
        0x888888,
        0x999999,
        0xAAAAAA,
        0xBBBBBB,
        0xCCCCCC,
        0xDDDDDD,
        0xEEEEEE,
        0xFFFFFF,
        0x003200,
        0x114300,
        0x225400,
        0x336500,
        0x447600,
        0x558700,
        0x669800,
        0x77A900,
        0x88BA00,
        0x99CB00,
        0xAADC00,
        0xBBED00,
        0xCCFE00,
        0xDDFF00,
        0xEEFF00,
        0xFFFF00,
        0x3B1000,
        0x4C2100,
        0x5D3200,
        0x6E4300,
        0x7F5400,
        0x906500,
        0xA17600,
        0xB28700,
        0xC39800,
        0xD4A900,
        0xE5BA00,
        0xF6CB00,
        0xFFDC00,
        0xFFED00,
        0xFFFE01,
        0xFFFF12,
        0x6C0000,
        0x7D0000,
        0x8E0D00,
        0x9F1E00,
        0xB02F00,
        0xC14000,
        0xD25100,
        0xE36200,
        0xF47300,
        0xFF8400,
        0xFF9500,
        0xFFA60E,
        0xFFB71F,
        0xFFC830,
        0xFFD941,
        0xFFEA52,
        0x8A0000,
        0x9B0000,
        0xAC0000,
        0xBD0000,
        0xCE0D00,
        0xDF1E05,
        0xF02F16,
        0xFF4027,
        0xFF5138,
        0xFF6249,
        0xFF735A,
        0xFF846B,
        0xFF957C,
        0xFFA68D,
        0xFFB79E,
        0xFFC8AF,
        0x91001B,
        0xA2002C,
        0xB3003D,
        0xC4004E,
        0xD5005F,
        0xE60670,
        0xF71781,
        0xFF2892,
        0xFF39A3,
        0xFF4AB4,
        0xFF5BC5,
        0xFF6CD6,
        0xFF7DE7,
        0xFF8EF8,
        0xFF9FFF,
        0xFFB0FF,
        0x7E0082,
        0x8F0093,
        0xA000A4,
        0xB100B5,
        0xC200C6,
        0xD300D7,
        0xE40DE8,
        0xF51EF9,
        0xFF2FFF,
        0xFF40FF,
        0xFF51FF,
        0xFF62FF,
        0xFF73FF,
        0xFF84FF,
        0xFF95FF,
        0xFFA6FF,
        0x5500D2,
        0x6600E3,
        0x7700F4,
        0x8800FF,
        0x9900FF,
        0xAA01FF,
        0xBB12FF,
        0xCC23FF,
        0xDD34FF,
        0xEE45FF,
        0xFF56FF,
        0xFF67FF,
        0xFF78FF,
        0xFF89FF,
        0xFF9AFF,
        0xFFABFF,
        0x1E00FD,
        0x2F00FF,
        0x4000FF,
        0x5100FF,
        0x6203FF,
        0x7314FF,
        0x8425FF,
        0x9536FF,
        0xA647FF,
        0xB758FF,
        0xC869FF,
        0xD97AFF,
        0xEA8BFF,
        0xFB9CFF,
        0xFFADFF,
        0xFFBEFF,
        0x0000FD,
        0x0000FF,
        0x0400FF,
        0x1511FF,
        0x2622FF,
        0x3733FF,
        0x4844FF,
        0x5955FF,
        0x6A66FF,
        0x7B77FF,
        0x8C88FF,
        0x9D99FF,
        0xAEAAFF,
        0xBFBBFF,
        0xD0CCFF,
        0xE1DDFF,
        0x0003D2,
        0x0014E3,
        0x0025F4,
        0x0036FF,
        0x0047FF,
        0x0058FF,
        0x1169FF,
        0x227AFF,
        0x338BFF,
        0x449CFF,
        0x55ADFF,
        0x66BEFF,
        0x77CFFF,
        0x88E0FF,
        0x99F1FF,
        0xAAFFFF,
        0x002782,
        0x003893,
        0x0049A4,
        0x005AB5,
        0x006BC6,
        0x007CD7,
        0x008DE8,
        0x009EF9,
        0x0AAFFF,
        0x1BC0FF,
        0x2CD1FF,
        0x3DE2FF,
        0x4EF3FF,
        0x5FFFFF,
        0x70FFFF,
        0x81FFFF,
        0x00441B,
        0x00552C,
        0x00663D,
        0x00774E,
        0x00885F,
        0x009970,
        0x00AA81,
        0x00BB92,
        0x00CCA3,
        0x08DDB4,
        0x19EEC5,
        0x2AFFD6,
        0x3BFFE7,
        0x4CFFF8,
        0x5DFFFF,
        0x6EFFFF,
        0x005600,
        0x006700,
        0x007800,
        0x008900,
        0x009A00,
        0x00AB05,
        0x00BC16,
        0x00CD27,
        0x00DE38,
        0x0FEF49,
        0x20FF5A,
        0x31FF6B,
        0x42FF7C,
        0x53FF8D,
        0x64FF9E,
        0x75FFAF,
        0x005900,
        0x006A00,
        0x007B00,
        0x008C00,
        0x009D00,
        0x00AE00,
        0x00BF00,
        0x0BD000,
        0x1CE100,
        0x2DF200,
        0x3EFF00,
        0x4FFF0E,
        0x60FF1F,
        0x71FF30,
        0x82FF41,
        0x93FF52,
        0x004C00,
        0x005D00,
        0x006E00,
        0x007F00,
        0x099000,
        0x1AA100,
        0x2BB200,
        0x3CC300,
        0x4DD400,
        0x5EE500,
        0x6FF600,
        0x80FF00,
        0x91FF00,
        0xA2FF00,
        0xB3FF01,
        0xC4FF12
    );

    public static ColorPalette Apple2 { get; } = new(
        RGBAColor.Black,
        0x404c00,
        0x722640,
        0xe46501,
        0x40337f,
        RGBAColor.Gray,
        0xe434fe,
        0xf1a6bf,
        0x0e5940,
        0x1bcb01,
        0xbfcc80,
        0x1b9afe,
        0x8dd9bf,
        0xbfb3ff,
        RGBAColor.White
    );

    public static ColorPalette Apple2_HighRes { get; } = new(
        RGBAColor.Black,
        RGBAColor.Magenta,
        RGBAColor.Gray,
        RGBAColor.White,
        0x0f0,
        0x00afff,
        0xff5000
    );

    public static ColorPalette CommodoreVIC20_5Levels { get; } = new(
        RGBAColor.Black,
        RGBAColor.White,
        0xaa7449,
        0xeab489,
        0x782922,
        0xb86962,
        0x87d6dd,
        0xc7ffff,
        0xaa5fb6,
        0xea9ff6,
        0x55a049,
        0x94e089,
        0x40318d,
        0x8071cc,
        0xbfce72,
        0xffffb2
    );

    public static ColorPalette CommodoreVIC20_9Levels { get; } = new(
        RGBAColor.Black,
        RGBAColor.White,
        0x8b5429,
        0xd59f74,
        0x8d3e37,
        0xb86962,
        0x72c1c8,
        0x87d6dd,
        0x80348b,
        0xaa5fb6,
        0x55a049,
        0x94e089,
        0x40318d,
        0x8071cc,
        0xaab95d,
        0xffffb2
    );

    public static ColorPalette CommodoreC64_9Levels { get; } = new(
        RGBAColor.Black,
        RGBAColor.White,
        0x8b5429,
        0x574200,
        0x883932,
        0xb86962,
        0x67b6bd,
        0x505050,
        0x8b3f96,
        0x787878,
        0x55a049,
        0x94e089,
        0x40318d,
        0x7869c4,
        0xbfce72,
        0x9f9f9f
    );

    public static ColorPalette Commodore16 { get; } = new(
        RGBAColor.Black,
        RGBAColor.Gray,
        RGBAColor.White,
        0x202020,
        0x404040,
        0x606060,
        0x9f9f9f,
        0xbfbfbf,
        0xdfdfdf,
        0x580902,
        0x782922,
        0x984942,
        0xb86962,
        0xd88882,
        0xf7a8a2,
        0xffc8c2,
        0xffe8e2,
        0x00373d,
        0x08575d,
        0x27777d,
        0x47969d,
        0x67b6bd,
        0x87d6dd,
        0xa7f6fd,
        0xc7ffff,
        0x4b0056,
        0x6b1f76,
        0x8b3f96,
        0xaa5fb6,
        0xca7fd6,
        0xea9ff6,
        0xffbfff,
        0xffdfff,
        0x004000,
        0x156009,
        0x358029,
        0x55a049,
        0x74c069,
        0x94e089,
        0xb4ffa9,
        0xd4ffc9,
        0x20116d,
        0x40318d,
        0x6051ac,
        0x8071cc,
        0x9f90ec,
        0xbfb0ff,
        0xdfd0ff,
        0xfff0ff,
        0x202f00,
        0x404f00,
        0x606f13,
        0x808e33,
        0x9fae53,
        0xbfce72,
        0xdfee92,
        0xffffb2,
        0x4b1500,
        0x6b3409,
        0x8b5429,
        0xaa7449,
        0xca9469,
        0xeab489,
        0xffd4a9,
        0xfff4c9,
        0x372200,
        0x574200,
        0x776219,
        0x978139,
        0xb7a158,
        0xd7c178,
        0xf6e198,
        0xffffb8,
        0x093a00,
        0x285900,
        0x487919,
        0x689939,
        0x88b958,
        0xa8d978,
        0xc8f998,
        0xe8ffb8,
        0x5d0120,
        0x7d2140,
        0x9c4160,
        0xbc6180,
        0xdc809f,
        0xfca0bf,
        0xffc0df,
        0xffe0ff,
        0x003f20,
        0x035f40,
        0x237f60,
        0x439e80,
        0x63be9f,
        0x82debf,
        0xa2fedf,
        0xc2ffff,
        0x002b56,
        0x154b76,
        0x356b96,
        0x558bb6,
        0x74abd6,
        0x94cbf6,
        0xb4eaff,
        0xd4ffff,
        0x370667,
        0x572687,
        0x7746a7,
        0x9766c6,
        0xb786e6,
        0xd7a6ff,
        0xf6c5ff,
        0xffe5ff,
        0x004202,
        0x086222,
        0x278242,
        0x47a262,
        0x67c282,
        0x87e2a2,
        0xa7ffc2,
        0xc7ffe2
    );

    public static ColorPalette TMS9918 { get; } = new(
        RGBAColor.Black,
        RGBAColor.White,
        0xdb6559,
        0xff897d,
        0x3eb849,
        0xccc35e,
        0x74d07d,
        0xded087,
        0x5955e0,
        0x3aa241,
        0x8076f1,
        0xb766b5,
        0xb95e51,
        0xcccccc,
        0x65dbef
    );

    public static ColorPalette MC6847 { get; } = new(
        0x1cd510u,
        0xe2db0fu,
        0x0320ffu,
        0xe2200au,
        0xcddbe0u,
        0x16d0e2u,
        0xcb39e2u,
        0xcc2d10u,
        0x101010u,
        0x003400u,
        0x321400u
    );

    public static ColorPalette SAMCoupé { get; } = new(
        RGBAColor.Black,
        0x000049,
        0x490000,
        0x490049,
        0x004900,
        0x004949,
        0x494900,
        0x494949,
        0x242424,
        0x24246d,
        0x6d2424,
        0x6d246d,
        0x246d24,
        0x246d6d,
        0x6d6d24,
        0x6d6d6d,
        0x000092,
        0x0000db,
        0x490092,
        0x4900db,
        0x004992,
        0x0049db,
        0x494992,
        0x4949db,
        0x2424b6,
        0x2424ff,
        0x6d24b6,
        0x6d24ff,
        0x246db6,
        0x246dff,
        0x6d6db6,
        0x6d6dff,
        0x920000,
        0x920049,
        0xdb0000,
        0xdb0049,
        0x924900,
        0x924949,
        0xdb4900,
        0xdb4949,
        0xb62424,
        0xb6246d,
        0xff2424,
        0xff246d,
        0xb66d24,
        0xb66d6d,
        0xff6d24,
        0xff6d6d,
        0x920092,
        0x9200db,
        0xdb0092,
        0xdb00db,
        0x924992,
        0x9249db,
        0xdb4992,
        0xdb49db,
        0xb624b6,
        0xb624ff,
        0xff24b6,
        0xff24ff,
        0xb66db6,
        0xb66dff,
        0xff6db6,
        0xff6dff,
        0x009200,
        0x009249,
        0x499200,
        0x499249,
        0x00db00,
        0x00db49,
        0x49db00,
        0x49db49,
        0x24b624,
        0x24b66d,
        0x6db624,
        0x6db66d,
        0x24ff24,
        0x24ff6d,
        0x6dff24,
        0x6dff6d,
        0x009292,
        0x0092db,
        0x499292,
        0x4992db,
        0x00db92,
        0x00dbdb,
        0x49db92,
        0x49dbdb,
        0x24b6b6,
        0x24b6ff,
        0x6db6b6,
        0x6db6ff,
        0x24ffb6,
        0x24ffff,
        0x6dffb6,
        0x6dffff,
        0x929200,
        0x929249,
        0xdb9200,
        0xdb9249,
        0x92db00,
        0x92db49,
        0xdbdb00,
        0xdbdb49,
        0xb6b624,
        0xb6b66d,
        0xffb624,
        0xffb66d,
        0xb6ff24,
        0xb6ff6d,
        0xffff24,
        0xffff6d,
        0x929292,
        0x9292db,
        0xdb9292,
        0xdb92db,
        0x92db92,
        0x92dbdb,
        0xdbdb92,
        0xdbdbdb,
        0xb6b6b6,
        0xb6b6ff,
        0xffb6b6,
        0xffb6ff,
        0xb6ffb6,
        0xb6ffff,
        0xffffb6,
        0xffffff,
        0x000049,
        0x490000,
        0x004900,
        0x242424,
        0x000092,
        0x920000,
        0x009200
    );


    public ColorPalette(IEnumerable<RGBAColor> colors)
        : base(colors)
    {
    }

    public ColorPalette(params RGBAColor[] colors)
        : this(colors as IEnumerable<RGBAColor>)
    {
    }

    public ColorPalette(params HDRColor[] colors)
        : this(colors as IEnumerable<HDRColor>)
    {
    }

    public ColorPalette(IEnumerable<HDRColor> colors)
        : this(colors.Select(c => (RGBAColor)c))
    {
    }

    public ColorPalette(params Color[] colors)
        : this(colors as IEnumerable<Color>)
    {
    }

    public ColorPalette(IEnumerable<Color> colors)
        : this(colors.Select(c => (RGBAColor)c))
    {
    }

    public ColorPalette(sys_palette palette)
#pragma warning disable CA1416 // Validate platform compatibility
        : this(palette.Entries)
#pragma warning restore CA1416
    {
    }

    public bool Contains(uint argb32) => base.Contains(argb32);

    public bool Contains(Color color) => base.Contains(color);

    public bool Contains<Color>(Color color) where Color : IColor<Color> => Contains(color.ToARGB32());

    public RGBAColor GetNearestColor<Color>(Color color, ColorEqualityMetric metric)
        where Color : IColor<Color> => GetNearestColor(color, metric, out _);

    public RGBAColor GetNearestColor<Color>(Color color, ColorEqualityMetric metric, out double distance)
        where Color : IColor<Color>
    {
        RGBAColor result;
#if USE_CACHE
        uint argb32 = color.ToARGB32();

        if (_cache.TryGetValue((metric, argb32), out (uint, double) match))
            (result, distance) = match;
        else
        {
#endif
            (result, distance) = GetNearestColors(color, metric).First();
#if USE_CACHE
            _cache[(metric, argb32)] = (result.ToARGB32(), distance);
        }
#endif

        return result;
    }

    public IEnumerable<(RGBAColor Color, double Distance)> GetNearestColors<Color>(Color color, ColorEqualityMetric metric)
        where Color : IColor<Color>
    {
        RGBAColor given = color is RGBAColor rc ? rc : (RGBAColor)color.ToARGB32();

        return from c in Colors
               let dist = c.DistanceTo(given, metric)
               orderby dist ascending
               select (c, dist);
    }

    public sys_palette ToImageColorPalette()
    {
        sys_palette palette = (sys_palette)_palette_ctor.Invoke([Colors.Count])!;
        int i = 0;

#pragma warning disable CA1416 // Validate platform compatibility
        foreach (RGBAColor color in Colors)
            palette.Entries[i++] = color;
#pragma warning restore CA1416

        return palette;
    }

    public static unsafe ColorPalette FromImage(Bitmap bitmap)
    {
        IEnumerable<RGBAColor> colors = [];

        bitmap.LockRGBAPixels((ptr, w, h) => colors = Enumerable.Range(0, w * h).Select(i => ptr[i]).Distinct());

        return new(colors);
    }

    public static unsafe ColorPalette FromImage(Bitmap bitmap, Clustering<RGBAColor> clustering)
    {
        IEnumerable<RGBAColor> colors = FromImage(bitmap).Colors;
        IEnumerable<Cluster<RGBAColor>> clusters = clustering.Process(colors);

        return new(clusters.Select(c => c.GetCenterItem()));
    }

    public static unsafe ColorPalette FromImage(Bitmap bitmap, int palette_size, bool ignore_alpha = true)
    {
        ClusteringConfiguration<RGBAColor> config = ignore_alpha ? new(3, c => new[] { c.Rf, c.Bf, c.Gf }) : new(4, c => new[] { c.Af, c.Rf, c.Bf, c.Gf });
        KMeansClustering<RGBAColor> clustering = new(palette_size, config);

        return FromImage(bitmap, clustering);
    }

    public static ColorPalette FromChannels(ColorChannel channels)
    {
        List<RGBAColor> colors = [RGBAColor.Black];
        bool a = channels.HasFlag(ColorChannel.A);
        bool r = channels.HasFlag(ColorChannel.R);
        bool g = channels.HasFlag(ColorChannel.G);
        bool b = channels.HasFlag(ColorChannel.B);

        if (a)
            colors.Add(RGBAColor.Transparent);

        if (r)
        {
            colors.Add(RGBAColor.Red);

            if (g)
            {
                colors.Add(RGBAColor.Yellow);

                if (b)
                    colors.Add(RGBAColor.White);
            }

            if (b)
                colors.Add(RGBAColor.Magenta);
        }

        if (g)
        {
            colors.Add(RGBAColor.Lime);

            if (b)
                colors.Add(RGBAColor.Cyan);
        }

        if (b)
            colors.Add(RGBAColor.Blue);

        return new(colors);
    }

    public static implicit operator sys_palette(ColorPalette palette) => palette.ToImageColorPalette();

    public static implicit operator ColorPalette(sys_palette palette) => new(palette);

    public static implicit operator RGBAColor[](ColorPalette palette) => palette.Colors.ToArray();

    public static implicit operator ColorPalette(RGBAColor[] palette) => new(palette);

    public static ColorPalette operator +(ColorPalette palette, RGBAColor color) => palette + new ColorPalette(color);

    public static ColorPalette operator +(RGBAColor color, ColorPalette palette) => palette + color;

    public static ColorPalette operator +(ColorPalette first, ColorPalette second) => new(first.Colors.Union(second.Colors));

    public static ColorPalette operator -(ColorPalette palette, RGBAColor color) => palette - new ColorPalette(color);

    public static ColorPalette operator -(ColorPalette first, ColorPalette second) => new(first.Colors.Except(second.Colors));

    public static ColorPalette operator &(ColorPalette first, ColorPalette second) => new(first.Colors.Intersect(second.Colors));
}
