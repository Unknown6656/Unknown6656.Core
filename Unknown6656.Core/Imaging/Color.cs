using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Physics.Optics;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;

namespace Unknown6656.Imaging;


public partial interface IColor
{
    internal static Dictionary<ConsoleColorScheme, Dictionary<ConsoleColor, uint>> ConsoleColorSchemes { get; } = new()
    {
        [ConsoleColorScheme.Legacy] = new()
        {
            [ConsoleColor.Black      ] = 0xff000000u,
            [ConsoleColor.DarkBlue   ] = 0xff000080u,
            [ConsoleColor.DarkGreen  ] = 0xff008000u,
            [ConsoleColor.DarkCyan   ] = 0xff008080u,
            [ConsoleColor.DarkRed    ] = 0xff800000u,
            [ConsoleColor.DarkMagenta] = 0xff800080u,
            [ConsoleColor.DarkYellow ] = 0xff808000u,
            [ConsoleColor.Gray       ] = 0xffc0c0c0u,
            [ConsoleColor.DarkGray   ] = 0xff808080u,
            [ConsoleColor.Blue       ] = 0xff0000ffu,
            [ConsoleColor.Green      ] = 0xff00ff00u,
            [ConsoleColor.Cyan       ] = 0xff00ffffu,
            [ConsoleColor.Red        ] = 0xffff0000u,
            [ConsoleColor.Magenta    ] = 0xffff00ffu,
            [ConsoleColor.Yellow     ] = 0xffffff00u,
            [ConsoleColor.White      ] = 0xffffffffu,
        },
        [ConsoleColorScheme.Windows10] = new()
        {
            [ConsoleColor.Black      ] = 0xff0c0c0cu,
            [ConsoleColor.DarkBlue   ] = 0xff0037dau,
            [ConsoleColor.DarkGreen  ] = 0xff13a10eu,
            [ConsoleColor.DarkCyan   ] = 0xff3a96ddu,
            [ConsoleColor.DarkRed    ] = 0xffc50f1fu,
            [ConsoleColor.DarkMagenta] = 0xff881798u,
            [ConsoleColor.DarkYellow ] = 0xffc19c00u,
            [ConsoleColor.Gray       ] = 0xffccccccu,
            [ConsoleColor.DarkGray   ] = 0xff767676u,
            [ConsoleColor.Blue       ] = 0xff3b78ffu,
            [ConsoleColor.Green      ] = 0xff16c60cu,
            [ConsoleColor.Cyan       ] = 0xff61d6d6u,
            [ConsoleColor.Red        ] = 0xffe74856u,
            [ConsoleColor.Magenta    ] = 0xffb4009eu,
            [ConsoleColor.Yellow     ] = 0xfff9f1a5u,
            [ConsoleColor.White      ] = 0xfff2f2f2u,
        }
    };

    /// <summary>
    /// The average of all color channels as a value in the range of [0..1].
    /// </summary>
    double Average { get; }
    double CIEGray { get; }
    (double L, double a, double b) CIELAB94 { get; }
    (double Hue, double Saturation, double Luminosity) HSL { get; }
    (double Hue, double Saturation, double Value) HSV { get; }
    (double C, double M, double Y, double K) CMYK { get; }
    (double Y, double U, double V) YUV { get; }
    (double Y, double I, double Q) YIQ { get; }

    Scalar CIALAB94DistanceTo(IColor other);

    /// <summary>
    /// Exports the HSL-color channels.
    /// </summary>
    (double H, double S, double L) ToHSL();

    (double H, double S, double V) ToHSV();

    (double R, double G, double B) ToRGB();

    uint ToARGB32();

    (double L, double a, double b) ToCIELAB94();

    (double C, double M, double Y, double K) ToCMYK();

    (double Y, double I, double Q) ToYIQ();

    (double Y, double U, double V) ToYUV();

    (double Y, double Cb, double Cr) ToYCbCr();

    DiscreteSpectrum ToSpectrum();

    double GetIntensity(Wavelength wavelength, double tolerance = 1e-1);
}

public interface IColor<Color>
    : IColor
    where Color : IColor<Color>
{
    (Color @this, Color Triadic1, Color Triadic2) Triadic { get; }
    Color[] Analogous { get; }
    Color[] Neutrals { get; }
    Color Normalized { get; }
    Color Complement { get; }

    Color Rotate(Scalar φ);
    Color CorrectGamma(Scalar gamma);
    Color[] GetNeutrals(Scalar φ_step, int count);

    /// <summary>
    /// Converts the given HSL-color to a <typeparamref name="Color"/>-instance.
    /// </summary>
    /// <param name=""H"">The HSL-color's hue channel [0..2π]</param>
    /// <param name=""S"">The HSL-color's saturation channel [0..1]</param>
    /// <param name=""L"">The HSL-color's luminosity channel [0..1]</param>
    /// <returns></returns>
    static abstract Color FromHSL(double H, double S, double L);

    /// <summary>
    /// Converts the given HSL-color to a <typeparamref name="Color"/>-instance.
    /// </summary>
    /// <param name=""H"">The HSL-color's hue channel [0..2π]</param>
    /// <param name=""S"">The HSL-color's saturation channel [0..1]</param>
    /// <param name=""L"">The HSL-color's luminosity channel [0..1]</param>
    /// <param name=""α"">The color's α-channel (opacity) [0..1]</param>
    /// <returns></returns>
    static abstract Color FromHSL(double H, double S, double L, double α);

    static abstract Color FromHSV(double H, double S, double V);

    static abstract Color FromHSV(double H, double S, double V, double α);

    static abstract Color FromRGB(double R, double G, double B);

    static abstract Color FromRGB(double R, double G, double B, double α);

    static abstract Color FromARGB32(int ARGB);

    static abstract Color FromARGB32(uint ARGB);

    static abstract Color FromCIELAB94(double L, double a, double b);

    static abstract Color FromCIELAB94(double L, double a, double b, double α);

    static abstract Color FromCMYK(double C, double M, double Y, double K);

    static abstract Color FromCMYK(double C, double M, double Y, double K, double α);

    static abstract Color FromYIQ(double Y, double I, double Q);

    static abstract Color FromYIQ(double Y, double I, double Q, double α);

    static abstract Color FromYUV(double Y, double U, double V);

    static abstract Color FromYUV(double Y, double U, double V, double α);

    static abstract Color FromYCbCr(double Y, double Cb, double Cr);

    static abstract Color FromYCbCr(double Y, double Cb, double Cr, double α);

    /// <summary>
    /// Returns the <typeparamref name="Color"/> associated with the given black body temperature (in Kelvin).
    /// </summary>
    /// <param name=""temperature"">The black body temperature (in Kelvin).</param>
    /// <returns><typeparamref name="Color"/></returns>
    static abstract Color FromBlackbodyRadiation(double temperature);

    /// <summary>
    /// Returns the <typeparamref name="Color"/> associated with the given black body temperature (in Kelvin).
    /// </summary>
    /// <param name=""temperature"">The black body temperature (in Kelvin).</param>
    /// <returns><typeparamref name="Color"/></returns>
    static abstract Color FromBlackbodyRadiation(double temperature, double α);

    static abstract implicit operator Color(System.Drawing.Color color);

    static abstract implicit operator System.Drawing.Color(Color color);
}

public interface IColor<Color, Channel>
    : IColor<Color>
    where Color : unmanaged, IColor<Color, Channel>
    where Channel : unmanaged
{
    /// <summary>
    /// The color's red channel.
    /// </summary>
    Channel R { get; }
    /// <summary>
    /// The color's green channel.
    /// </summary>
    Channel G { get; }
    /// <summary>
    /// The color's blue channel.
    /// </summary>
    Channel B { get; }
    /// <summary>
    /// The color's alpha channel.
    /// </summary>
    Channel A { get; }
    public Channel this[ColorChannel channel] { get; }

    //void Deconstruct(out Channel r, out Channel g, out Channel b);
    //void Deconstruct(out Channel r, out Channel g, out Channel b, out Channel α);


    double DistanceTo(Color other, ColorEqualityMetric metric);
    bool Equals(Color other, ColorEqualityMetric metric);
    bool Equals(Color other, ColorEqualityMetric metric, double tolerance);
    bool Equals(Color other, ColorTolerance tolerance);
}

/// <summary>
/// Represents a color information structure consisting of 4x64Bit floating-point structures for the channels
/// red (<see cref="R"/>), green (<see cref="G"/>), blue (<see cref="B"/>), and opacity (<see cref="A"/>).
/// </summary>
/// <completionlist cref="HDRColor"/>
[NativeCppClass, Serializable, StructLayout(LayoutKind.Sequential)]
public partial struct HDRColor
    : IColor<HDRColor, double>
    , IComparable<HDRColor>
{
    private double _α, _r, _g, _b;


    public double R
    {
        readonly get => _r;
        set => _r = value.Clamp();
    }

    public double G
    {
        readonly get => _g;
        set => _g = value.Clamp();
    }

    public double B
    {
        readonly get => _b;
        set => _b = value.Clamp();
    }

    public double A
    {
        readonly get => _α;
        set => _α = value.Clamp();
    }

    public double this[ColorChannel channel]
    {
        readonly get => channel switch
        {
            ColorChannel.A => A,
            ColorChannel.R => R,
            ColorChannel.G => G,
            ColorChannel.B => B,
            _ => throw new ArgumentOutOfRangeException(nameof(channel)),
        };
        set
        {
            if (channel == ColorChannel.A)
                A = value;
            else if (channel == ColorChannel.R)
                R = value;
            else if (channel == ColorChannel.G)
                G = value;
            else if (channel == ColorChannel.B)
                B = value;
            else
                throw new ArgumentOutOfRangeException(nameof(channel));
        }
    }

    public RGBAColor ARGB32
    {
        readonly get => new(R, G, B, A);
        set => (R, G, B, A) = (value.Rf, value.Gf, value.Bf, value.Af);
    }

    public readonly HDRColor Complement => new(1 - R, 1 - G, 1 - B, A);


    public readonly uint ToARGB32() => ARGB32.ARGBu;

    public readonly override string ToString() => $"(R:{Math.Round(R, 6)}, G:{Math.Round(G, 6)}, B:{Math.Round(B, 6)}, α:{Math.Round(A, 6)})";

    public static implicit operator HDRColor(RGBAColor color) => new() { ARGB32 = color };

    public static implicit operator RGBAColor(HDRColor color) => color.ARGB32;

    public static implicit operator HDRColor(Color color) => (HDRColor)(RGBAColor)color;

    public static implicit operator Color(HDRColor color) => (Color)(RGBAColor)color;

    public static explicit operator HDRColor(int argb) => FromARGB32(argb);

    public static explicit operator HDRColor(uint argb) => FromARGB32(argb);
}

/// <summary>
/// Represents a native pixel 32-bit color information structure.
/// </summary>
/// <completionlist cref="RGBAColor"/>
[NativeCppClass, Serializable, StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RGBAColor
    : IColor<RGBAColor, byte>
    , IComparable<RGBAColor>
{
    #region PROPERTIES AND FIELDS

    //////////////////////////// DO NOT CHANGE THE ORDER OF THE FOLLOWING FIELDS ! NATIVE CODE DEPENDS ON THIS ORDER ! ////////////////////////////

    /// <summary>
    /// The color's blue channel.
    /// </summary>
    [FieldOffset(0)]
    public byte B;

    /// <summary>
    /// The color's green channel.
    /// </summary>
    [FieldOffset(1)]
    public byte G;

    /// <summary>
    /// The color's red channel.
    /// </summary>
    [FieldOffset(2)]
    public byte R;

    /// <summary>
    /// The color's alpha channel.
    /// </summary>
    [FieldOffset(3)]
    public byte A;


    public byte this[ColorChannel channel]
    {
        readonly get => (byte)((ARGB >> (int)channel) & 0xff);
        set
        {
            int mask = ARGB & ~(0xff << (int)channel);

            ARGB = mask | (value << (int)channel);
        }
    }

    /// <summary>
    /// The color information stored in an 32-Bit signed integer value
    /// </summary>
    public int ARGB
    {
        set => ARGBu = (uint)value;
        readonly get => (int)ARGBu;
    }

    /// <summary>
    /// The color information stored in an 32-Bit unsigned integer value
    /// </summary>
    public uint ARGBu
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value <= 0xfff)
                value = ((value & 0xf00) << 12)
                      | ((value & 0xff0) << 8)
                      | ((value & 0x0ff) << 4)
                      | (value & 0x00f)
                      | 0xff000000;
            else if (value <= 0xffff)
                value = ((value & 0xf000) << 16)
                      | ((value & 0xff00) << 12)
                      | ((value & 0x0ff0) << 8)
                      | ((value & 0x00ff) << 4)
                      | (value & 0x000f);
            else if (value <= 0xffffff)
                value |= 0xff000000;

            A = (byte)((value >> (int)ColorChannel.A) & 0xff);
            R = (byte)((value >> (int)ColorChannel.R) & 0xff);
            G = (byte)((value >> (int)ColorChannel.G) & 0xff);
            B = (byte)((value >> (int)ColorChannel.B) & 0xff);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (uint)((A << (int)ColorChannel.A)
                    | (R << (int)ColorChannel.R)
                    | (G << (int)ColorChannel.G)
                    | (B << (int)ColorChannel.B));
    }

    /// <summary>
    /// The pixel's alpha channel represented as floating-point value in the interval of [0..1]
    /// </summary>
    public double Af
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => A = (byte)Math.Round(value.Clamp() * 255);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => A / 255.0;
    }

    /// <summary>
    /// The pixel's red channel represented as floating-point value in the interval of [0..1]
    /// </summary>
    public double Rf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => R = (byte)Math.Round(value.Clamp() * 255);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => R / 255.0;
    }

    /// <summary>
    /// The pixel's green channel represented as floating-point value in the interval of [0..1]
    /// </summary>
    public double Gf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => G = (byte)Math.Round(value.Clamp() * 255);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => G / 255.0;
    }

    /// <summary>
    /// The pixel's blue channel represented as floating-point value in the interval of [0..1]
    /// </summary>
    public double Bf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => B = (byte)Math.Round(value.Clamp() * 255);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => B / 255.0;
    }

    readonly byte IColor<RGBAColor, byte>.R => R;

    readonly byte IColor<RGBAColor, byte>.G => G;

    readonly byte IColor<RGBAColor, byte>.B => B;

    readonly byte IColor<RGBAColor, byte>.A => A;

    public readonly double EucledianLength => Math.Sqrt(Rf * Rf + Gf * Gf + Bf * Bf);

    public readonly RGBAColor Complement => new((byte)(255 - R), (byte)(255 - G), (byte)(255 - B), A);

    #endregion
    #region CONSTRUCTORS

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="r">Red value</param>
    /// <param name="g">Green value</param>
    /// <param name="b">Blue value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RGBAColor(byte r, byte g, byte b)
        : this(r, g, b, 255)
    {
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="α">Alpha value</param>
    /// <param name="r">Red value</param>
    /// <param name="g">Green value</param>
    /// <param name="b">Blue value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RGBAColor(byte r, byte g, byte b, byte α)
    {
        A = α;
        R = r;
        G = g;
        B = b;
    }

    #endregion
    #region INSTANCE METHODS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string ToString()
    {
        uint argb = ARGBu;

        return IColor.KnownColorNames.TryGetValue(argb, out string? name) ? name : $"#{argb:x8}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint ToARGB32() => ARGBu;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string ToVT100ForegroundString() => $"\x1b[38;2;{R};{G};{B}m";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string ToVT100BackgroundString() => $"\x1b[48;2;{R};{G};{B}m";

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public (double X, double Y, double Z) ToXYZ()
    //{
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public (double U, double Y) ToUV()
    //{
    //    (double X, double Y, double Z) = ToXYZ();
    //
    //    return (
    //        (4 * x) / ((-2 * x) + (12 * y) + 3),
    //        (9 * y) / ((-2 * x) + (12 * y) + 3)
    //    );
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out byte r, out byte g, out byte b) => Deconstruct(out r, out g, out b, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte α)
    {
        r = R;
        g = G;
        b = B;
        α = A;
    }

    #endregion

    // TODO : Adobe color space
    // TODO : CIE76
    // TODO : CIELUV
    // TODO : chroma
    // TODO : https://en.wikipedia.org/wiki/Color_difference

    #region STATIC METHODS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBAColor Blend(RGBAColor bottom, RGBAColor top, BlendMode mode)
    {
        Vector3 b = bottom;
        Vector3 t = top;
        double ta = bottom.Af;
        double ba = top.Af;
        double α = 1 - (1 - ba) * (1 - ta);

        Vector3 cout = mode switch
        {
            BlendMode.Normal or BlendMode.Alpha => (ta * t + (1 - ta) * ba * t) / α,
            BlendMode.ColorBurn => b.ComponentwiseApply(t, (b, t) => b >= 1 ? 1 : t <= 0 ? 0 : 1 - Math.Min(1, (1 - b) / t)),
            BlendMode.ColorDodge => b.ComponentwiseApply(t, (b, t) => b <= 0 ? 0 : t >= 1 ? 1 : Math.Min(1, b / (1 - t))),


            BlendMode.Color => throw new NotImplementedException(), // TODO
            BlendMode.Hue => throw new NotImplementedException(), // TODO : B(Cb, Cs) = SetLum(SetSat(Cs, Sat(Cb)), Lum(Cb))
            BlendMode.Luminosity => throw new NotImplementedException(), // TODO


            BlendMode.Darken => new(
                Math.Min(b.X, t.X),
                Math.Min(b.Y, t.Y),
                Math.Min(b.Z, t.Z)
            ),
            BlendMode.Lighten => new(
                Math.Max(b.X, t.X),
                Math.Max(b.Y, t.Y),
                Math.Max(b.Z, t.Z)
            ),
            BlendMode.Multiply => b.ComponentwiseMultiply(t),
            BlendMode.Remainder => b.ComponentwiseModulus(t),
            BlendMode.Screen => 1 - (1 - b).ComponentwiseMultiply(1 - t),
            BlendMode.Divide => b.ComponentwiseDivide(t),
            BlendMode.Bottom => bottom,
            BlendMode.Top => top,
            BlendMode.Overlay => ba < .5 ? 2 * b.ComponentwiseMultiply(t) : 1 - (1 - b).ComponentwiseMultiply(1 - t).Multiply(2),
            BlendMode.SoftLight => ta < .5 ? b.ComponentwiseMultiply(t).Multiply(2).Add(b.ComponentwiseMultiply(b).Multiply(1 - 2 * t)) : b.ComponentwiseMultiply(1 - t).Multiply(2).Add(b.ComponentwiseSqrt().Multiply(t.Multiply(2) - 1)),
            BlendMode.HardLight => ta < .5 ? b.ComponentwiseMultiply(t).Multiply(2) : 1 - (1 - b).ComponentwiseMultiply(1 - t).Multiply(2),
            BlendMode.Add => b + t,
            BlendMode.Subtract => b - t,
            BlendMode.Difference => (b - t).ComponentwiseAbsolute(),
            BlendMode.Exclusion => b + t - 2 * b.ComponentwiseMultiply(t),
            BlendMode.Average => (b + t).Divide(2),
            BlendMode.BinaryOR => (Vector3)new RGBAColor(bottom.ARGB | top.ARGB),
            BlendMode.BinaryAND => (Vector3)new RGBAColor(bottom.ARGB & top.ARGB),
            BlendMode.BinaryXOR => (Vector3)new RGBAColor(bottom.ARGB ^ top.ARGB),
            BlendMode.BinaryNOR => (Vector3)new RGBAColor(~(bottom.ARGB | top.ARGB)),
            BlendMode.BinaryNAND => (Vector3)new RGBAColor(~(bottom.ARGB & top.ARGB)),
            BlendMode.BinaryNXOR => (Vector3)new RGBAColor(~(bottom.ARGB ^ top.ARGB)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), $"The blend mode '{mode}' is unknown or unsupported.")
        };

        return new Vector4(cout, α);
    }

    #endregion
    #region OPERATORS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(RGBAColor color) => color.ARGB;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(RGBAColor color) => color.ARGBu;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBAColor(int argb) => FromARGB32(argb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBAColor(uint argb) => FromARGB32(argb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (byte r, byte g, byte b, byte α)(RGBAColor color) => (color.R, color.G, color.B, color.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color(RGBAColor color) => Color.FromArgb(color.ARGB);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBAColor(Color color) => new(color.ToArgb());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBAColor((byte r, byte g, byte b, byte α) color) => new(color.r, color.g, color.b, color.α);

    #endregion
}

[Flags]
public enum ColorChannel
{
    A = 24,
    R = 16,
    G = 8,
    B = 0,

    RGB = R | G | B,
    ARGB = A | RGB,
}

public enum ConsoleColorScheme
{
    Legacy,
    Windows10
}

public enum ColorEqualityMetric
{
    RGBAChannels,
    RGBChannels,
    RChannel,
    GChannel,
    BChannel,
    RGChannels,
    RBChannels,
    RAChannels,
    GBChannels,
    GAChannels,
    BAChannels,
    RGAChannels,
    RBAChannels,
    GBAChannels,
    CChannel,
    MChannel,
    YChannel,
    KChannel,
    Alpha,
    Hue,
    Saturation,
    Luminance,
    CIEGray,
    CIALAB94,
    Average,
    EucledianRGBLength,
    EucledianRGBALength,
    LegacyConsoleColor,
    Windows10ConsoleColor,
}

/// <summary>
/// Represents an enumeration of blend modes.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Normal blend mode (alpha-screening).
    /// </summary>
    Normal = 0,
    Alpha = Normal,
    /// <summary>
    /// Multiply blend mode.
    /// </summary>
    Multiply,
    /// <summary>
    /// Screen blend mode (Inverse multiply mode).
    /// </summary>
    Screen,
    /// <summary>
    /// Divide blend mode.
    /// </summary>
    Divide,
    Bottom,
    Top,
    ColorBurn,
    ColorDodge,
    Hue,
    Color,
    Luminosity,
    Darken,
    Lighten,
    Remainder,
    Overlay,
    SoftLight,
    HardLight,
    /// <summary>
    /// Additive blend mode.
    /// </summary>
    Add,
    /// <summary>
    /// Subtractive blend mode.
    /// </summary>
    Subtract,
    /// <summary>
    /// Difference blend mode.
    /// </summary>
    Difference,
    Exclusion,
    Average,
    /// <summary>
    /// Binary XOR blend mode.
    /// </summary>
    BinaryXOR,
    /// <summary>
    /// Binary NXOR blend mode.
    /// </summary>
    BinaryNXOR,
    /// <summary>
    /// Binary AND blend mode.
    /// </summary>
    BinaryAND,
    /// <summary>
    /// Binary NAND blend mode.
    /// </summary>
    BinaryNAND,
    /// <summary>
    /// Binary OR blend mode.
    /// </summary>
    BinaryOR,
    /// <summary>
    /// Binary NOR blend mode.
    /// </summary>
    BinaryNOR,
}


//////////// TODO ////////////
internal struct CIEColorSystem 
{
public static (double x, double y) XY_RED { get; } = (0.7355, 0.2645);
public static (double x, double y) XY_GREEN { get; } = (0.2658, 0.7243);
public static (double x, double y) XY_BLUE { get; } = (0.1669, 0.0085);
public static (double x, double y) XY_WHITE { get; } = (1.0 / 3, 1.0 / 3);


void UVtoXY(double up, double vp, out double xc, out double yc)
{
    xc = (9 * up) / ((6 * up) - (16 * vp) + 12);
    yc = (4 * vp) / ((6 * up) - (16 * vp) + 12);
}

void XYtoUV(double xc, double yc, out double up, out double vp)
{
    up = (4 * xc) / ((-2 * xc) + (12 * yc) + 3);
    vp = (9 * yc) / ((-2 * xc) + (12 * yc) + 3);
}

bool inside_gamut(double r, double g, double b) => (r >= 0) && (g >= 0) && (b >= 0);



bool constrain_rgb(ref double r, ref double g, ref double b)
{
    double w = -Math.Min(0, Math.Min(r, Math.Min(g, b)));

    if (w > 0)
    {
        r += w;
        g += w;
        b += w;

        return true;
    }
    else
        return false;
}


}
