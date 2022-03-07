using System.Collections.Generic;
using System.Reflection;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;
using System.Text;
using Unknown6656.Generics;
using System.Collections;

namespace Unknown6656.Imaging;

using sys_palette = System.Drawing.Imaging.ColorPalette;



public class ColorPalette<Color>
    : IEnumerable<Color>
    where Color : unmanaged, IColor<Color>
{
    public HashSet<Color> Colors { get; }


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

    public override bool Equals(object? obj) => obj is ColorPalette<Color> other && Colors.SetEquals(other.Colors);

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

    public static bool operator ==(ColorPalette<Color>? c1, ColorPalette<Color>? c2) => c1?.Equals(c2) ?? c2 is null;

    public static bool operator !=(ColorPalette<Color>? c1, ColorPalette<Color>? c2) => !(c1 == c2);
}

public class ColorPalette
    : ColorPalette<RGBAColor>
{
    private static readonly ConstructorInfo _palette_ctor = typeof(sys_palette).GetConstructor(new[] { typeof(int) })!;


    public static ColorPalette BlackAndWhite { get; } = new(Color.Black, Color.White);

    public static ColorPalette Grayscale256 { get; } = new(Enumerable.Range(0, 256).Select(i => Color.FromArgb(i, i, i)));

    public static ColorPalette Reds256 { get; } = new(Enumerable.Range(0, 256).Select(i => Color.FromArgb(i, 0, 0)));

    public static ColorPalette Greens256 { get; } = new(Enumerable.Range(0, 256).Select(i => Color.FromArgb(0, i, 0)));

    public static ColorPalette Blues256 { get; } = new(Enumerable.Range(0, 256).Select(i => Color.FromArgb(0, 0, i)));

    public static ColorPalette _3Bit { get; } = new(Enumerable.Range(0, 8).Select(i => Color.FromArgb(i & 1, (i >> 1) & 1, (i >> 2) & 1)));

    public static ReadOnlyIndexer<ConsoleColorScheme, ColorPalette> ConsoleColorSchemes { get; } = new(scheme =>
    {
        Dictionary<ConsoleColor, uint> dict = IColor.ConsoleColorSchemes[scheme];

        return new(Enum.GetValues<ConsoleColor>().Select(color => (RGBAColor)dict[color]));
    });

    public static ColorPalette Windows10_ConsoleColors { get; } = ConsoleColorSchemes[ConsoleColorScheme.Windows10];

    public static ColorPalette Legacy_ConsoleColors { get; } = ConsoleColorSchemes[ConsoleColorScheme.Legacy];

    public static ColorPalette _6Bit { get; } = new(Enumerable.Range(0, 64).Select(i => Color.FromArgb(i & 3, (i >> 2) & 3, (i >> 4) & 3)));

    //public static ColorPalette WebSafeColors { get; } = new(Enumerable.Range(0, 256).Select(i => ));

    public static ColorPalette CGA16 { get; } = new(
        RGBAColor.Black,
        (RGBAColor)0x00a,
        (RGBAColor)0x0a0,
        (RGBAColor)0x0aa,
        (RGBAColor)0xa00,
        (RGBAColor)0xa0a,
        (RGBAColor)0xa50,
        (RGBAColor)0xaaa,
        (RGBAColor)0x555,
        (RGBAColor)0x55f,
        (RGBAColor)0x5f5,
        (RGBAColor)0x5ff,
        (RGBAColor)0xf55,
        (RGBAColor)0xf5f,
        (RGBAColor)0xff5,
        RGBAColor.White
    );

    public static ColorPalette CGAMode4Palette1LowIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x0aa, (RGBAColor)0xa0a, (RGBAColor)0xaaa);

    public static ColorPalette CGAMode4Palette1HighIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x5ff, (RGBAColor)0xf5f, RGBAColor.White);

    public static ColorPalette CGAMode4Palette2LowIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x0a0, (RGBAColor)0xa00, (RGBAColor)0xa50);

    public static ColorPalette CGAMode4Palette2HighIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x5f5, (RGBAColor)0xf55, (RGBAColor)0xff5);

    public static ColorPalette CGAMode5LowIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x0aa, (RGBAColor)0xa00, (RGBAColor)0xaaa);

    public static ColorPalette CGAMode5HighIntensity { get; } = new(RGBAColor.Black, (RGBAColor)0x5ff, (RGBAColor)0xf55, RGBAColor.White);

    public static ColorPalette BIOS16Colors { get; } = new(
        RGBAColor.Black,
        RGBAColor.Navy,
        RGBAColor.Blue,
        RGBAColor.Green,
        (RGBAColor)0x0f0,
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


    public ColorPalette(params RGBAColor[] colors)
        : base(colors)
    {
    }

    public ColorPalette(IEnumerable<RGBAColor> colors)
        : base(colors)
    {
    }

    public ColorPalette(params uint[] argb_values)
        : this(argb_values as IEnumerable<uint>)
    {
    }

    public ColorPalette(IEnumerable<uint> argb_values)
        : this(argb_values.Select(argb => new RGBAColor(argb))
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
        : this(palette.Entries)
    {
    }

    public sys_palette ToImageColorPalette()
    {
        sys_palette palette = (sys_palette)_palette_ctor.Invoke(new object?[] { Colors.Length })!;

        Colors.CopyTo(palette.Entries);

        return palette;
    }

    public static implicit operator sys_palette(ColorPalette palette) => palette.ToImageColorPalette();

    public static implicit operator ColorPalette(sys_palette palette) => new(palette);
}
