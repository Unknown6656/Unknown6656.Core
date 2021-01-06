using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;
using Unknown6656.Common;
using System.Dynamic;

namespace Unknown6656.Imaging
{
    public interface IColor
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

        Scalar CIALAB94DistanceTo(IColor other);
        /// <summary>
        /// Exports the HSL-color channels.
        /// </summary>
        (double H, double S, double L) ToHSL();
        (double H, double S, double V) ToHSV();
        (double L, double a, double b) ToCIELAB94();
        (double C, double M, double Y, double K) ToCMYK();
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
        public Channel this[BitmapChannel channel] { get; }

        //void Deconstruct(out Channel r, out Channel g, out Channel b);
        //void Deconstruct(out Channel r, out Channel g, out Channel b, out Channel α);
    }

    /// <summary>
    /// Represents a color information structure consisting of 4x64Bit floating-point structures for the channels
    /// red (<see cref="R"/>), green (<see cref="G"/>), blue (<see cref="B"/>), and opacity (<see cref="A"/>).
    /// </summary>
    [NativeCppClass, Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct HDRColor
        : IColor<HDRColor, double>
        , IComparable<HDRColor>
    {
        private double _α, _r, _g, _b;


        /// <inheritdoc/>
        public double R
        {
            readonly get => _r;
            set => _r = value.Clamp();
        }

        /// <inheritdoc/>
        public double G
        {
            readonly get => _g;
            set => _g = value.Clamp();
        }

        /// <inheritdoc/>
        public double B
        {
            readonly get => _b;
            set => _b = value.Clamp();
        }

        /// <inheritdoc/>
        public double A
        {
            readonly get => _α;
            set => _α = value.Clamp();
        }

        public double this[BitmapChannel channel]
        {
            readonly get => channel switch
            {
                BitmapChannel.A => A,
                BitmapChannel.R => R,
                BitmapChannel.G => G,
                BitmapChannel.B => B,
                _ => throw new ArgumentOutOfRangeException(nameof(channel)),
            };
            set
            {
                if (channel == BitmapChannel.A)
                    A = value;
                else if (channel == BitmapChannel.R)
                    R = value;
                else if (channel == BitmapChannel.G)
                    G = value;
                else if (channel == BitmapChannel.B)
                    B = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }

        public RGBAColor ARGB32
        {
            readonly get => new RGBAColor(R, G, B, A);
            set => (R, G, B, A) = (value.Rf, value.Gf, value.Bf, value.Af);
        }

        public readonly HDRColor Complement => new HDRColor(1 - R, 1 - G, 1 - B, A);


        public int CompareTo(HDRColor other) => throw new NotImplementedException();

        public readonly override string ToString() => $"(R:{Math.Round(R, 6)}, G:{Math.Round(G, 6)}, B:{Math.Round(B, 6)}, α:{Math.Round(A, 6)})";

        public static implicit operator HDRColor(RGBAColor color) => new HDRColor { ARGB32 = color };

        public static implicit operator RGBAColor(HDRColor color) => color.ARGB32;
    }

/// <summary>
/// Represents a native pixel 32-bit color information structure.
/// </summary>
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


        public byte this[BitmapChannel channel]
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
                if (value <= 0xffff)
                    value = ((value & 0xf000) << 16)
                          | ((value & 0xff00) << 12)
                          | ((value & 0x0ff0) << 8)
                          | ((value & 0x00ff) << 4)
                          | (value & 0x000f);

                A = (byte)((value >> (int)BitmapChannel.A) & 0xff);
                R = (byte)((value >> (int)BitmapChannel.R) & 0xff);
                G = (byte)((value >> (int)BitmapChannel.G) & 0xff);
                B = (byte)((value >> (int)BitmapChannel.B) & 0xff);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (uint)((A << (int)BitmapChannel.A)
                        | (R << (int)BitmapChannel.R)
                        | (G << (int)BitmapChannel.G)
                        | (B << (int)BitmapChannel.B));
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

        /// <inheritdoc/>
        readonly byte IColor<RGBAColor, byte>.R => R;

        /// <inheritdoc/>
        readonly byte IColor<RGBAColor, byte>.G => G;

        /// <inheritdoc/>
        readonly byte IColor<RGBAColor, byte>.B => B;

        /// <inheritdoc/>
        readonly byte IColor<RGBAColor, byte>.A => A;

        public readonly double EucledianLength => Math.Sqrt(Rf * Rf + Gf * Gf + Bf * Bf);

        public readonly RGBAColor Complement => new RGBAColor((byte)(255 - R), (byte)(255 - G), (byte)(255 - B), A);

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

        public readonly override string ToString() => $"#{ARGB:x8}";


        public readonly int CompareTo([AllowNull] RGBAColor other)
        {
            int dist = ((Vector3)this).Length.CompareTo(((Vector3)other).Length);

            return dist == 0 ? A.CompareTo(other.A) : dist;
        }

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

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out byte r, out byte g, out byte b) => Deconstruct(out r, out g, out b, out _);

        /// <inheritdoc/>
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
        // TODO : CIE94
        // TODO : CMYK
        // TODO : CIEXYZ
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
                BlendMode.Normal => (ta * t + (1 - ta) * ba * t) / α,
                BlendMode.Multiply => b.ComponentwiseMultiply(t),
                BlendMode.Remainder => b.ComponentwiseModulus(t),
                BlendMode.Screen => 1 - (1 - b).ComponentwiseMultiply(1 - t),
                BlendMode.Divide => b.ComponentwiseDivide(t),
                BlendMode.Overlay => ba < .5 ? 2 * b.ComponentwiseMultiply(t) : 1 - (1 - b).ComponentwiseMultiply(1 - t).Multiply(2),
                BlendMode.SoftLight => ta < .5 ? b.ComponentwiseMultiply(t).Multiply(2).Add(b.ComponentwiseMultiply(b).Multiply(1 - 2 * t)) : b.ComponentwiseMultiply(1 - t).Multiply(2).Add(b.ComponentwiseSqrt().Multiply(t.Multiply(2) - 1)),
                BlendMode.HardLight => ta < .5 ? b.ComponentwiseMultiply(t).Multiply(2) : 1 - (1 - b).ComponentwiseMultiply(1 - t).Multiply(2),
                BlendMode.Add => b + t,
                BlendMode.Subtract => b - t,
                BlendMode.Difference => (b - t).ComponentwiseAbsolute(),
                BlendMode.Average => (b + t).Divide(2),
                BlendMode.BinaryOR => (Vector3)new RGBAColor(bottom.ARGB | top.ARGB),
                BlendMode.BinaryAND => (Vector3)new RGBAColor(bottom.ARGB & top.ARGB),
                BlendMode.BinaryXOR => (Vector3)new RGBAColor(bottom.ARGB ^ top.ARGB),
                BlendMode.BinaryNOR => (Vector3)new RGBAColor(~(bottom.ARGB | top.ARGB)),
                BlendMode.BinaryNAND => (Vector3)new RGBAColor(~(bottom.ARGB & top.ARGB)),
                BlendMode.BinaryNXOR => (Vector3)new RGBAColor(~(bottom.ARGB ^ top.ARGB)),
                BlendMode.Min => (
                    Math.Min(b.X, t.X),
                    Math.Min(b.Y, t.Y),
                    Math.Min(b.Z, t.Z)
                ),
                BlendMode.Max => (
                    Math.Max(b.X, t.X),
                    Math.Max(b.Y, t.Y),
                    Math.Max(b.Z, t.Z)
                ),
                _ => throw new ArgumentException($"The blend mode '{mode}' is unknown or unsupported.", nameof(mode))
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
        public static implicit operator (byte r, byte g, byte b, byte α)(RGBAColor color) => (color.R, color.G, color.B, color.A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color(RGBAColor color) => Color.FromArgb(color.ARGB);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(Color color) => new RGBAColor(color.ToArgb());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor((byte r, byte g, byte b, byte α) color) => new RGBAColor(color.r, color.g, color.b, color.α);

        #endregion
    }

    public class ColorMap
    {
        #region STATIC COLOR MAPS

        public static ColorMap BlackbodyHeat { get; } = Uniform(0xf000, 0xff88, 0xffb7, 0xffff, 0xf9ef, 0xf6cf);

        public static ColorMap Terrain { get; } = new ColorMap(
            (0,   (.2, .2, .6)),
            (.15, (0, .6, 1)),
            (.25, (0, .8, .4)),
            (.50, (1, 1, .6)),
            (.75, (.5, .36, .33)),
            (1,   (1, 1, 1))
        );

        public static ColorMap Blues { get; } = Uniform(
            (0.96862745098039216, 0.98431372549019602, 1.0),
            (0.87058823529411766, 0.92156862745098034, 0.96862745098039216),
            (0.77647058823529413, 0.85882352941176465, 0.93725490196078431),
            (0.61960784313725492, 0.792156862745098, 0.88235294117647056),
            (0.41960784313725491, 0.68235294117647061, 0.83921568627450982),
            (0.25882352941176473, 0.5725490196078431, 0.77647058823529413),
            (0.12941176470588237, 0.44313725490196076, 0.70980392156862748),
            (0.03137254901960784, 0.31764705882352939, 0.61176470588235299),
            (0.03137254901960784, 0.18823529411764706, 0.41960784313725491)
        );

        public static ColorMap BrBG { get; } = Uniform(
            (0.32941176470588235, 0.18823529411764706, 0.0196078431372549),
            (0.5490196078431373, 0.31764705882352939, 0.0392156862745098),
            (0.74901960784313726, 0.50588235294117645, 0.17647058823529413),
            (0.87450980392156863, 0.76078431372549016, 0.49019607843137253),
            (0.96470588235294119, 0.90980392156862744, 0.76470588235294112),
            (0.96078431372549022, 0.96078431372549022, 0.96078431372549022),
            (0.7803921568627451, 0.91764705882352937, 0.89803921568627454),
            (0.50196078431372548, 0.80392156862745101, 0.75686274509803919),
            (0.20784313725490197, 0.59215686274509804, 0.5607843137254902),
            (0.00392156862745098, 0.4, 0.36862745098039218),
            (0.0, 0.23529411764705882, 0.18823529411764706)
        );

        public static ColorMap BuGn { get; } = Uniform(
            (0.96862745098039216, 0.9882352941176471, 0.99215686274509807),
            (0.89803921568627454, 0.96078431372549022, 0.97647058823529409),
            (0.8, 0.92549019607843142, 0.90196078431372551),
            (0.6, 0.84705882352941175, 0.78823529411764703),
            (0.4, 0.76078431372549016, 0.64313725490196083),
            (0.25490196078431371, 0.68235294117647061, 0.46274509803921571),
            (0.13725490196078433, 0.54509803921568623, 0.27058823529411763),
            (0.0, 0.42745098039215684, 0.17254901960784313),
            (0.0, 0.26666666666666666, 0.10588235294117647)
        );

        public static ColorMap BuPu { get; } = Uniform(
            (0.96862745098039216, 0.9882352941176471, 0.99215686274509807),
            (0.8784313725490196, 0.92549019607843142, 0.95686274509803926),
            (0.74901960784313726, 0.82745098039215681, 0.90196078431372551),
            (0.61960784313725492, 0.73725490196078436, 0.85490196078431369),
            (0.5490196078431373, 0.58823529411764708, 0.77647058823529413),
            (0.5490196078431373, 0.41960784313725491, 0.69411764705882351),
            (0.53333333333333333, 0.25490196078431371, 0.61568627450980395),
            (0.50588235294117645, 0.05882352941176471, 0.48627450980392156),
            (0.30196078431372547, 0.0, 0.29411764705882354)
        );

        public static ColorMap GnBu { get; } = Uniform(
            (0.96862745098039216, 0.9882352941176471, 0.94117647058823528),
            (0.8784313725490196, 0.95294117647058818, 0.85882352941176465),
            (0.8, 0.92156862745098034, 0.77254901960784317),
            (0.6588235294117647, 0.8666666666666667, 0.70980392156862748),
            (0.4823529411764706, 0.8, 0.7686274509803922),
            (0.30588235294117649, 0.70196078431372544, 0.82745098039215681),
            (0.16862745098039217, 0.5490196078431373, 0.74509803921568629),
            (0.03137254901960784, 0.40784313725490196, 0.67450980392156867),
            (0.03137254901960784, 0.25098039215686274, 0.50588235294117645)
        );

        public static ColorMap Greens { get; } = Uniform(
            (0.96862745098039216, 0.9882352941176471, 0.96078431372549022),
            (0.89803921568627454, 0.96078431372549022, 0.8784313725490196),
            (0.7803921568627451, 0.9137254901960784, 0.75294117647058822),
            (0.63137254901960782, 0.85098039215686272, 0.60784313725490191),
            (0.45490196078431372, 0.7686274509803922, 0.46274509803921571),
            (0.25490196078431371, 0.6705882352941176, 0.36470588235294116),
            (0.13725490196078433, 0.54509803921568623, 0.27058823529411763),
            (0.0, 0.42745098039215684, 0.17254901960784313),
            (0.0, 0.26666666666666666, 0.10588235294117647)
        );

        public static ColorMap Grays { get; } = Uniform(
            (1.0, 1.0, 1.0),
            (0.94117647058823528, 0.94117647058823528, 0.94117647058823528),
            (0.85098039215686272, 0.85098039215686272, 0.85098039215686272),
            (0.74117647058823533, 0.74117647058823533, 0.74117647058823533),
            (0.58823529411764708, 0.58823529411764708, 0.58823529411764708),
            (0.45098039215686275, 0.45098039215686275, 0.45098039215686275),
            (0.32156862745098042, 0.32156862745098042, 0.32156862745098042),
            (0.14509803921568629, 0.14509803921568629, 0.14509803921568629),
            (0.0, 0.0, 0.0)
        );

        public static ColorMap Oranges { get; } = Uniform(
            (1.0, 0.96078431372549022, 0.92156862745098034),
            (0.99607843137254903, 0.90196078431372551, 0.80784313725490198),
            (0.99215686274509807, 0.81568627450980391, 0.63529411764705879),
            (0.99215686274509807, 0.68235294117647061, 0.41960784313725491),
            (0.99215686274509807, 0.55294117647058827, 0.23529411764705882),
            (0.94509803921568625, 0.41176470588235292, 0.07450980392156863),
            (0.85098039215686272, 0.28235294117647058, 0.00392156862745098),
            (0.65098039215686276, 0.21176470588235294, 0.01176470588235294),
            (0.49803921568627452, 0.15294117647058825, 0.01568627450980392)
        );

        public static ColorMap OrRd { get; } = Uniform(
            (1.0, 0.96862745098039216, 0.92549019607843142),
            (0.99607843137254903, 0.90980392156862744, 0.78431372549019607),
            (0.99215686274509807, 0.83137254901960789, 0.61960784313725492),
            (0.99215686274509807, 0.73333333333333328, 0.51764705882352946),
            (0.9882352941176471, 0.55294117647058827, 0.34901960784313724),
            (0.93725490196078431, 0.396078431372549, 0.28235294117647058),
            (0.84313725490196079, 0.18823529411764706, 0.12156862745098039),
            (0.70196078431372544, 0.0, 0.0),
            (0.49803921568627452, 0.0, 0.0)
        );

        public static ColorMap PiYG { get; } = Uniform(
            (0.55686274509803924, 0.00392156862745098, 0.32156862745098042),
            (0.77254901960784317, 0.10588235294117647, 0.49019607843137253),
            (0.87058823529411766, 0.46666666666666667, 0.68235294117647061),
            (0.94509803921568625, 0.71372549019607845, 0.85490196078431369),
            (0.99215686274509807, 0.8784313725490196, 0.93725490196078431),
            (0.96862745098039216, 0.96862745098039216, 0.96862745098039216),
            (0.90196078431372551, 0.96078431372549022, 0.81568627450980391),
            (0.72156862745098038, 0.88235294117647056, 0.52549019607843139),
            (0.49803921568627452, 0.73725490196078436, 0.25490196078431371),
            (0.30196078431372547, 0.5725490196078431, 0.12941176470588237),
            (0.15294117647058825, 0.39215686274509803, 0.09803921568627451)
        );

        public static ColorMap PrGN { get; } = Uniform(
            (0.25098039215686274,  0.0                ,  0.29411764705882354),
            (0.46274509803921571,  0.16470588235294117,  0.51372549019607838),
            (0.6                ,  0.4392156862745098 ,  0.6705882352941176 ),
            (0.76078431372549016,  0.6470588235294118 ,  0.81176470588235294),
            (0.90588235294117647,  0.83137254901960789,  0.90980392156862744),
            (0.96862745098039216,  0.96862745098039216,  0.96862745098039216),
            (0.85098039215686272,  0.94117647058823528,  0.82745098039215681),
            (0.65098039215686276,  0.85882352941176465,  0.62745098039215685),
            (0.35294117647058826,  0.68235294117647061,  0.38039215686274508),
            (0.10588235294117647,  0.47058823529411764,  0.21568627450980393),
            (0.0                ,  0.26666666666666666,  0.10588235294117647)
        );
    
        public static ColorMap PuBu { get; } = Uniform(
            (1.0                ,  0.96862745098039216,  0.98431372549019602),
            (0.92549019607843142,  0.90588235294117647,  0.94901960784313721),
            (0.81568627450980391,  0.81960784313725488,  0.90196078431372551),
            (0.65098039215686276,  0.74117647058823533,  0.85882352941176465),
            (0.45490196078431372,  0.66274509803921566,  0.81176470588235294),
            (0.21176470588235294,  0.56470588235294117,  0.75294117647058822),
            (0.0196078431372549 ,  0.4392156862745098 ,  0.69019607843137254),
            (0.01568627450980392,  0.35294117647058826,  0.55294117647058827),
            (0.00784313725490196,  0.2196078431372549 ,  0.34509803921568627)
        );
    
        public static ColorMap PuBuGn { get; } = Uniform(
            (1.0                ,  0.96862745098039216,  0.98431372549019602),
            (0.92549019607843142,  0.88627450980392153,  0.94117647058823528),
            (0.81568627450980391,  0.81960784313725488,  0.90196078431372551),
            (0.65098039215686276,  0.74117647058823533,  0.85882352941176465),
            (0.40392156862745099,  0.66274509803921566,  0.81176470588235294),
            (0.21176470588235294,  0.56470588235294117,  0.75294117647058822),
            (0.00784313725490196,  0.50588235294117645,  0.54117647058823526),
            (0.00392156862745098,  0.42352941176470588,  0.34901960784313724),
            (0.00392156862745098,  0.27450980392156865,  0.21176470588235294)
        );
    
        public static ColorMap PuOr { get; } = Uniform(
            (0.49803921568627452,  0.23137254901960785,  0.03137254901960784),
            (0.70196078431372544,  0.34509803921568627,  0.02352941176470588),
            (0.8784313725490196 ,  0.50980392156862742,  0.07843137254901961),
            (0.99215686274509807,  0.72156862745098038,  0.38823529411764707),
            (0.99607843137254903,  0.8784313725490196 ,  0.71372549019607845),
            (0.96862745098039216,  0.96862745098039216,  0.96862745098039216),
            (0.84705882352941175,  0.85490196078431369,  0.92156862745098034),
            (0.69803921568627447,  0.6705882352941176 ,  0.82352941176470584),
            (0.50196078431372548,  0.45098039215686275,  0.67450980392156867),
            (0.32941176470588235,  0.15294117647058825,  0.53333333333333333),
            (0.17647058823529413,  0.0                ,  0.29411764705882354)
        );

        public static ColorMap PuRd { get; } = Uniform(
            (0.96862745098039216, 0.95686274509803926, 0.97647058823529409),
            (0.90588235294117647, 0.88235294117647056, 0.93725490196078431),
            (0.83137254901960789, 0.72549019607843135, 0.85490196078431369),
            (0.78823529411764703, 0.58039215686274515, 0.7803921568627451),
            (0.87450980392156863, 0.396078431372549, 0.69019607843137254),
            (0.90588235294117647, 0.16078431372549021, 0.54117647058823526),
            (0.80784313725490198, 0.07058823529411765, 0.33725490196078434),
            (0.59607843137254901, 0.0, 0.2627450980392157),
            (0.40392156862745099, 0.0, 0.12156862745098039)
        );

        public static ColorMap Purples { get; } = Uniform(
            (0.9882352941176471 ,  0.98431372549019602,  0.99215686274509807),
            (0.93725490196078431,  0.92941176470588238,  0.96078431372549022),
            (0.85490196078431369,  0.85490196078431369,  0.92156862745098034),
            (0.73725490196078436,  0.74117647058823533,  0.86274509803921573),
            (0.61960784313725492,  0.60392156862745094,  0.78431372549019607),
            (0.50196078431372548,  0.49019607843137253,  0.72941176470588232),
            (0.41568627450980394,  0.31764705882352939,  0.63921568627450975),
            (0.32941176470588235,  0.15294117647058825,  0.5607843137254902 ),
            (0.24705882352941178,  0.0                ,  0.49019607843137253)
        );

        public static ColorMap RdBu { get; } = Uniform(
            (0.40392156862745099,  0.0                ,  0.12156862745098039),
            (0.69803921568627447,  0.09411764705882353,  0.16862745098039217),
            (0.83921568627450982,  0.37647058823529411,  0.30196078431372547),
            (0.95686274509803926,  0.6470588235294118 ,  0.50980392156862742),
            (0.99215686274509807,  0.85882352941176465,  0.7803921568627451 ),
            (0.96862745098039216,  0.96862745098039216,  0.96862745098039216),
            (0.81960784313725488,  0.89803921568627454,  0.94117647058823528),
            (0.5725490196078431 ,  0.77254901960784317,  0.87058823529411766),
            (0.2627450980392157 ,  0.57647058823529407,  0.76470588235294112),
            (0.12941176470588237,  0.4                ,  0.67450980392156867),
            (0.0196078431372549 ,  0.18823529411764706,  0.38039215686274508)
        );

        public static ColorMap RdGy { get; } = Uniform(
            (0.40392156862745099,  0.0                ,  0.12156862745098039),
            (0.69803921568627447,  0.09411764705882353,  0.16862745098039217),
            (0.83921568627450982,  0.37647058823529411,  0.30196078431372547),
            (0.95686274509803926,  0.6470588235294118 ,  0.50980392156862742),
            (0.99215686274509807,  0.85882352941176465,  0.7803921568627451 ),
            (1.0                ,  1.0                ,  1.0                ),
            (0.8784313725490196 ,  0.8784313725490196 ,  0.8784313725490196 ),
            (0.72941176470588232,  0.72941176470588232,  0.72941176470588232),
            (0.52941176470588236,  0.52941176470588236,  0.52941176470588236),
            (0.30196078431372547,  0.30196078431372547,  0.30196078431372547),
            (0.10196078431372549,  0.10196078431372549,  0.10196078431372549)
        );

        public static ColorMap RdPu { get; } = Uniform(
            (1.0                ,  0.96862745098039216,  0.95294117647058818),
            (0.99215686274509807,  0.8784313725490196 ,  0.86666666666666667),
            (0.9882352941176471 ,  0.77254901960784317,  0.75294117647058822),
            (0.98039215686274506,  0.62352941176470589,  0.70980392156862748),
            (0.96862745098039216,  0.40784313725490196,  0.63137254901960782),
            (0.86666666666666667,  0.20392156862745098,  0.59215686274509804),
            (0.68235294117647061,  0.00392156862745098,  0.49411764705882355),
            (0.47843137254901963,  0.00392156862745098,  0.46666666666666667),
            (0.28627450980392155,  0.0                ,  0.41568627450980394)
        );

        public static ColorMap PdYlBu { get; } = Uniform(
            (0.6470588235294118 , 0.0                 , 0.14901960784313725),
            (0.84313725490196079, 0.18823529411764706 , 0.15294117647058825),
            (0.95686274509803926, 0.42745098039215684 , 0.2627450980392157 ),
            (0.99215686274509807, 0.68235294117647061 , 0.38039215686274508),
            (0.99607843137254903, 0.8784313725490196  , 0.56470588235294117),
            (1.0                , 1.0                 , 0.74901960784313726),
            (0.8784313725490196 , 0.95294117647058818 , 0.97254901960784312),
            (0.6705882352941176 , 0.85098039215686272 , 0.9137254901960784 ),
            (0.45490196078431372, 0.67843137254901964 , 0.81960784313725488),
            (0.27058823529411763, 0.45882352941176469 , 0.70588235294117652),
            (0.19215686274509805, 0.21176470588235294 , 0.58431372549019611)
        );

        public static ColorMap RdYlGn { get; } = Uniform(
            (0.6470588235294118 , 0.0                 , 0.14901960784313725),
            (0.84313725490196079, 0.18823529411764706 , 0.15294117647058825),
            (0.95686274509803926, 0.42745098039215684 , 0.2627450980392157 ),
            (0.99215686274509807, 0.68235294117647061 , 0.38039215686274508),
            (0.99607843137254903, 0.8784313725490196  , 0.54509803921568623),
            (1.0                , 1.0                 , 0.74901960784313726),
            (0.85098039215686272, 0.93725490196078431 , 0.54509803921568623),
            (0.65098039215686276, 0.85098039215686272 , 0.41568627450980394),
            (0.4                , 0.74117647058823533 , 0.38823529411764707),
            (0.10196078431372549, 0.59607843137254901 , 0.31372549019607843),
            (0.0                , 0.40784313725490196 , 0.21568627450980393)
        );

        public static ColorMap Reds { get; } = Uniform(
            (1.0                , 0.96078431372549022 , 0.94117647058823528),
            (0.99607843137254903, 0.8784313725490196  , 0.82352941176470584),
            (0.9882352941176471 , 0.73333333333333328 , 0.63137254901960782),
            (0.9882352941176471 , 0.5725490196078431  , 0.44705882352941179),
            (0.98431372549019602, 0.41568627450980394 , 0.29019607843137257),
            (0.93725490196078431, 0.23137254901960785 , 0.17254901960784313),
            (0.79607843137254897, 0.094117647058823528, 0.11372549019607843),
            (0.6470588235294118 , 0.058823529411764705, 0.08235294117647058),
            (0.40392156862745099, 0.0                 , 0.05098039215686274)
        );

        public static ColorMap Spectral { get; } = Uniform(
            (0.61960784313725492, 0.003921568627450980, 0.25882352941176473),
            (0.83529411764705885, 0.24313725490196078 , 0.30980392156862746),
            (0.95686274509803926, 0.42745098039215684 , 0.2627450980392157 ),
            (0.99215686274509807, 0.68235294117647061 , 0.38039215686274508),
            (0.99607843137254903, 0.8784313725490196  , 0.54509803921568623),
            (1.0                , 1.0                 , 0.74901960784313726),
            (0.90196078431372551, 0.96078431372549022 , 0.59607843137254901),
            (0.6705882352941176 , 0.8666666666666667  , 0.64313725490196083),
            (0.4                , 0.76078431372549016 , 0.6470588235294118 ),
            (0.19607843137254902, 0.53333333333333333 , 0.74117647058823533),
            (0.36862745098039218, 0.30980392156862746 , 0.63529411764705879)
        );

        public static ColorMap YlGn { get; } = Uniform(
            (1.0                , 1.0                 , 0.89803921568627454),
            (0.96862745098039216, 0.9882352941176471  , 0.72549019607843135),
            (0.85098039215686272, 0.94117647058823528 , 0.63921568627450975),
            (0.67843137254901964, 0.8666666666666667  , 0.55686274509803924),
            (0.47058823529411764, 0.77647058823529413 , 0.47450980392156861),
            (0.25490196078431371, 0.6705882352941176  , 0.36470588235294116),
            (0.13725490196078433, 0.51764705882352946 , 0.2627450980392157 ),
            (0.0                , 0.40784313725490196 , 0.21568627450980393),
            (0.0                , 0.27058823529411763 , 0.16078431372549021)
        );

        public static ColorMap YlGnBu { get; } = Uniform(
            (1.0                , 1.0                 , 0.85098039215686272),
            (0.92941176470588238, 0.97254901960784312 , 0.69411764705882351),
            (0.7803921568627451 , 0.9137254901960784  , 0.70588235294117652),
            (0.49803921568627452, 0.80392156862745101 , 0.73333333333333328),
            (0.25490196078431371, 0.71372549019607845 , 0.7686274509803922 ),
            (0.11372549019607843, 0.56862745098039214 , 0.75294117647058822),
            (0.13333333333333333, 0.36862745098039218 , 0.6588235294117647 ),
            (0.14509803921568629, 0.20392156862745098 , 0.58039215686274515),
            (0.03137254901960784, 0.11372549019607843 , 0.34509803921568627)
        );

        public static ColorMap YlOrBn { get; } = Uniform(
            (1.0                , 1.0                 , 0.89803921568627454),
            (1.0                , 0.96862745098039216 , 0.73725490196078436),
            (0.99607843137254903, 0.8901960784313725  , 0.56862745098039214),
            (0.99607843137254903, 0.7686274509803922  , 0.30980392156862746),
            (0.99607843137254903, 0.6                 , 0.16078431372549021),
            (0.92549019607843142, 0.4392156862745098  , 0.07843137254901961),
            (0.8                , 0.29803921568627451 , 0.00784313725490196),
            (0.6                , 0.20392156862745098 , 0.01568627450980392),
            (0.4                , 0.14509803921568629 , 0.02352941176470588)
        );

        public static ColorMap YlOrRd { get; } = Uniform(
            (1.0                , 1.0                 , 0.8                ),
            (1.0                , 0.92941176470588238 , 0.62745098039215685),
            (0.99607843137254903, 0.85098039215686272 , 0.46274509803921571),
            (0.99607843137254903, 0.69803921568627447 , 0.29803921568627451),
            (0.99215686274509807, 0.55294117647058827 , 0.23529411764705882),
            (0.9882352941176471 , 0.30588235294117649 , 0.16470588235294117),
            (0.8901960784313725 , 0.10196078431372549 , 0.10980392156862745),
            (0.74117647058823533, 0.0                 , 0.14901960784313725),
            (0.50196078431372548, 0.0                 , 0.14901960784313725)
        );
    
        public static ColorMap Accent { get; } = Uniform(
            (0.49803921568627452, 0.78823529411764703, 0.49803921568627452),
            (0.74509803921568629, 0.68235294117647061, 0.83137254901960789),
            (0.99215686274509807, 0.75294117647058822, 0.52549019607843139),
            (1.0,                 1.0,                 0.6                ),
            (0.2196078431372549,  0.42352941176470588, 0.69019607843137254),
            (0.94117647058823528, 0.00784313725490196, 0.49803921568627452),
            (0.74901960784313726, 0.35686274509803922, 0.09019607843137254),
            (0.4,                 0.4,                 0.4                )
        );
        
        public static ColorMap Dark { get; } = Uniform(
            (0.10588235294117647, 0.61960784313725492, 0.46666666666666667),
            (0.85098039215686272, 0.37254901960784315, 0.00784313725490196),
            (0.45882352941176469, 0.4392156862745098,  0.70196078431372544),
            (0.90588235294117647, 0.16078431372549021, 0.54117647058823526),
            (0.4,                 0.65098039215686276, 0.11764705882352941),
            (0.90196078431372551, 0.6705882352941176,  0.00784313725490196),
            (0.65098039215686276, 0.46274509803921571, 0.11372549019607843),
            (0.4,                 0.4,                 0.4                )
        );
        
        public static ColorMap Paired { get; } = Uniform(
            (0.65098039215686276, 0.80784313725490198, 0.8901960784313725 ),
            (0.12156862745098039, 0.47058823529411764, 0.70588235294117652),
            (0.69803921568627447, 0.87450980392156863, 0.54117647058823526),
            (0.2,                 0.62745098039215685, 0.17254901960784313),
            (0.98431372549019602, 0.60392156862745094, 0.6                ),
            (0.8901960784313725,  0.10196078431372549, 0.10980392156862745),
            (0.99215686274509807, 0.74901960784313726, 0.43529411764705883),
            (1.0,                 0.49803921568627452, 0.0                ),
            (0.792156862745098,   0.69803921568627447, 0.83921568627450982),
            (0.41568627450980394, 0.23921568627450981, 0.60392156862745094),
            (1.0,                 1.0,                 0.6                ),
            (0.69411764705882351, 0.34901960784313724, 0.15686274509803921)
        );
        
        public static ColorMap Pastel1 { get; } = Uniform(
            (0.98431372549019602, 0.70588235294117652, 0.68235294117647061),
            (0.70196078431372544, 0.80392156862745101, 0.8901960784313725 ),
            (0.8,                 0.92156862745098034, 0.77254901960784317),
            (0.87058823529411766, 0.79607843137254897, 0.89411764705882357),
            (0.99607843137254903, 0.85098039215686272, 0.65098039215686276),
            (1.0,                 1.0,                 0.8                ),
            (0.89803921568627454, 0.84705882352941175, 0.74117647058823533),
            (0.99215686274509807, 0.85490196078431369, 0.92549019607843142),
            (0.94901960784313721, 0.94901960784313721, 0.94901960784313721)
        );
        
        public static ColorMap Pastel2 { get; } = Uniform(
            (0.70196078431372544, 0.88627450980392153, 0.80392156862745101),
            (0.99215686274509807, 0.80392156862745101, 0.67450980392156867),
            (0.79607843137254897, 0.83529411764705885, 0.90980392156862744),
            (0.95686274509803926, 0.792156862745098,   0.89411764705882357),
            (0.90196078431372551, 0.96078431372549022, 0.78823529411764703),
            (1.0,                 0.94901960784313721, 0.68235294117647061),
            (0.94509803921568625, 0.88627450980392153, 0.8                ),
            (0.8,                 0.8,                 0.8                )
        );
        
        public static ColorMap Set1 { get; } = Uniform(
            (0.89411764705882357, 0.10196078431372549, 0.10980392156862745),
            (0.21568627450980393, 0.49411764705882355, 0.72156862745098038),
            (0.30196078431372547, 0.68627450980392157, 0.29019607843137257),
            (0.59607843137254901, 0.30588235294117649, 0.63921568627450975),
            (1.0,                 0.49803921568627452, 0.0                ),
            (1.0,                 1.0,                 0.2                ),
            (0.65098039215686276, 0.33725490196078434, 0.15686274509803921),
            (0.96862745098039216, 0.50588235294117645, 0.74901960784313726),
            (0.6,                 0.6,                 0.6)
        );
        
        public static ColorMap Set2 { get; } = Uniform(
            (0.4,                 0.76078431372549016, 0.6470588235294118 ),
            (0.9882352941176471,  0.55294117647058827, 0.3843137254901961 ),
            (0.55294117647058827, 0.62745098039215685, 0.79607843137254897),
            (0.90588235294117647, 0.54117647058823526, 0.76470588235294112),
            (0.65098039215686276, 0.84705882352941175, 0.32941176470588235),
            (1.0,                 0.85098039215686272, 0.18431372549019609),
            (0.89803921568627454, 0.7686274509803922,  0.58039215686274515),
            (0.70196078431372544, 0.70196078431372544, 0.70196078431372544)
        );
        
        public static ColorMap Set3 { get; } = Uniform(
            (0.55294117647058827, 0.82745098039215681, 0.7803921568627451 ),
            (1.0,                 1.0,                 0.70196078431372544),
            (0.74509803921568629, 0.72941176470588232, 0.85490196078431369),
            (0.98431372549019602, 0.50196078431372548, 0.44705882352941179),
            (0.50196078431372548, 0.69411764705882351, 0.82745098039215681),
            (0.99215686274509807, 0.70588235294117652, 0.3843137254901961 ),
            (0.70196078431372544, 0.87058823529411766, 0.41176470588235292),
            (0.9882352941176471,  0.80392156862745101, 0.89803921568627454),
            (0.85098039215686272, 0.85098039215686272, 0.85098039215686272),
            (0.73725490196078436, 0.50196078431372548, 0.74117647058823533),
            (0.8,                 0.92156862745098034, 0.77254901960784317),
            (1.0,                 0.92941176470588238, 0.43529411764705883)
        );

        #endregion


        private readonly (Scalar X, RGBAColor Color)[] _colors;

        public RGBAColor this[Scalar c] => Interpolate(c);

        public RGBAColor this[Scalar c, Scalar min, Scalar max] => Interpolate(c, min, max);


        public ColorMap(params (Scalar X, RGBAColor Color)[] colors)
            : this(colors as IEnumerable<(Scalar, RGBAColor)>)
        {
        }

        public ColorMap(IEnumerable<(Scalar X, RGBAColor Color)> colors)
        {
            _colors = colors.OrderBy(c => c.X)
                            .Where(c => c.X >= 0 && c.X <= 1)
                            .ToArray();

            if (_colors.Length == 0)
                throw new ArgumentException("The color map must contain at least one color in the interval [0..1].", nameof(colors));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor Interpolate(Scalar c)
        {
            Scalar x0, x1;

            if (c <= _colors[0].X)
                return _colors[0].Color;
            else if (c >= _colors[^1].X)
                return _colors[^1].Color;

            for (int i = 1; i < _colors.Length; ++i)
            {
                x0 = _colors[i - 1].X;
                x1 = _colors[i].X;

                if (x0 <= c && c <= x1)
                    return Vector4.LinearInterpolate(_colors[i - 1].Color, _colors[i].Color, (c - x0) / (x1 - x0));
            }

            throw new ArgumentOutOfRangeException(nameof(c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor Interpolate(Scalar c, Scalar min, Scalar max) => Interpolate((c - min) / (max - min));

        public Scalar Approximate(RGBAColor color)
        {
            (Scalar x, Scalar fac)[] factors = (from pairs in _colors
                                                let dist = ((Vector3)pairs.Color - color).SquaredLength
                                                let fac = (1 - dist).Clamp()
                                                select (pairs.X, fac)).ToArray();
            Scalar total = 0;
            Scalar sum = 0;

            for (int i = 0; i < factors.Length; ++i)
            {
                total += factors[i].fac;
                sum += factors[i].x * factors[i].fac;
            }

            return sum / total; // / factors.Length;
        }

        public static ColorMap Bicolor(RGBAColor c0, RGBAColor c1) => Uniform(c0, c1);

        public static ColorMap Tricolor(RGBAColor c0, RGBAColor c05, RGBAColor c1) => Uniform(c0, c05, c1);

        public static ColorMap Uniform(params RGBAColor[] colors) => new ColorMap(colors.Select((c, i) => (i / (Scalar)(colors.Length - 1), c)));
    }

    public enum BitmapChannel
    {
        A = 24,
        R = 16,
        G = 8,
        B = 0,
    }

    public enum ConsoleColorScheme
    {
        Legacy,
        Windows10
    }
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
