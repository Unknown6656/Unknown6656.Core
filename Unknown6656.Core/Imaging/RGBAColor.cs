using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics;
using Unknown6656.IO;

namespace Unknown6656.Imaging
{
    /// <summary>
    /// Represents a native pixel 32-bit color information structure
    /// </summary>
    [NativeCppClass, Serializable, StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct RGBAColor
        : IComparable<RGBAColor>
    {
        #region STATIC FIELDS AND PROPERTIES

        private static readonly Dictionary<ConsoleColor, RGBAColor>[] _console_colors = new[]
        {
            new Dictionary<ConsoleColor, RGBAColor>
            {
                [ConsoleColor.Black      ] = 0xff000000,
                [ConsoleColor.DarkBlue   ] = 0xff000080,
                [ConsoleColor.DarkGreen  ] = 0xff008000,
                [ConsoleColor.DarkCyan   ] = 0xff008080,
                [ConsoleColor.DarkRed    ] = 0xff800000,
                [ConsoleColor.DarkMagenta] = 0xff800080,
                [ConsoleColor.DarkYellow ] = 0xff808000,
                [ConsoleColor.Gray       ] = 0xffc0c0c0,
                [ConsoleColor.DarkGray   ] = 0xff808080,
                [ConsoleColor.Blue       ] = 0xff0000ff,
                [ConsoleColor.Green      ] = 0xff00ff00,
                [ConsoleColor.Cyan       ] = 0xff00ffff,
                [ConsoleColor.Red        ] = 0xffff0000,
                [ConsoleColor.Magenta    ] = 0xffff00ff,
                [ConsoleColor.Yellow     ] = 0xffffff00,
                [ConsoleColor.White      ] = 0xffffffff,
            },
            new Dictionary<ConsoleColor, RGBAColor>
            {
                [ConsoleColor.Black      ] = new RGBAColor(12, 12, 12),
                [ConsoleColor.DarkBlue   ] = new RGBAColor(0, 55, 218),
                [ConsoleColor.DarkGreen  ] = new RGBAColor(19, 161, 14),
                [ConsoleColor.DarkCyan   ] = new RGBAColor(58, 150, 221),
                [ConsoleColor.DarkRed    ] = new RGBAColor(197, 15, 31),
                [ConsoleColor.DarkMagenta] = new RGBAColor(136, 23, 152),
                [ConsoleColor.DarkYellow ] = new RGBAColor(193, 156, 0),
                [ConsoleColor.Gray       ] = new RGBAColor(204, 204, 204),
                [ConsoleColor.DarkGray   ] = new RGBAColor(118, 118, 118),
                [ConsoleColor.Blue       ] = new RGBAColor(59, 120, 255),
                [ConsoleColor.Green      ] = new RGBAColor(22, 198, 12),
                [ConsoleColor.Cyan       ] = new RGBAColor(97, 214, 214),
                [ConsoleColor.Red        ] = new RGBAColor(231, 72, 86),
                [ConsoleColor.Magenta    ] = new RGBAColor(180, 0, 158),
                [ConsoleColor.Yellow     ] = new RGBAColor(249, 241, 165),
                [ConsoleColor.White      ] = new RGBAColor(242, 242, 242),
            }
        };

        #endregion
        #region PROPERTIES AND FIELDS

        /// <summary>
        /// The pixel's blue channel
        /// </summary>
        [FieldOffset(0)]
        public byte B;

        /// <summary>
        /// The pixel's green channel
        /// </summary>
        [FieldOffset(1)]
        public byte G;
        
        /// <summary>
        /// The pixel's red channel
        /// </summary>
        [FieldOffset(2)]
        public byte R;
        
        /// <summary>
        /// The pixel's alpha channel
        /// </summary>
        [FieldOffset(3)]
        public byte A;


        public byte this[BitmapChannel channel]
        {
            get => (byte)((ARGB >> (int)channel) & 0xff);
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
            get => (int)ARGBu;
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
            get => (uint)((A << (int)BitmapChannel.A)
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
            get => A / 255.0;
        }

        /// <summary>
        /// The pixel's red channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Rf
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => R = (byte)Math.Round(value.Clamp() * 255);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => R / 255.0;
        }

        /// <summary>
        /// The pixel's green channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Gf
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => G = (byte)Math.Round(value.Clamp() * 255);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => G / 255.0;
        }

        /// <summary>
        /// The pixel's blue channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Bf
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => B = (byte)Math.Round(value.Clamp() * 255);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => B / 255.0;
        }

        public double Average => (Rf + Gf + Bf) / 3;

        public double EucledianLength => Math.Sqrt(Rf * Rf + Gf * Gf + Bf * Bf);

        public double CIEGray => (.299 * Rf) + (.587 * Gf) + (.114 * Bf);

        public (double L, double a, double b) CIELAB
        {
            get
            {
                ToCIELAB(out double L, out double a, out double b);

                return (L, a, b);
            }
        }

        public (double Hue, double Saturation, double Luminosity) HSL
        {
            get
            {
                ToHSL(out double h, out double s, out double l);

                return (h, l, s);
            }
        }

        public RGBAColor Complement => new RGBAColor((byte)(255 - R), (byte)(255 - G), (byte)(255 - B), A);

        public (RGBAColor @this, RGBAColor t1, RGBAColor t2) Triadic
        {
            get
            {
                ToHSL(out double h, out double s, out double l);

                return (
                    this,
                    FromHSL(h + Math.PI * 2 / 3, s, l, Af),
                    FromHSL(h + Math.PI * 4 / 3, s, l, Af)
                );
            }
        }

        public RGBAColor[] Tetradic
        {
            get
            {
                RGBAColor sec = Rotate(Math.PI / 3);

                return new[]
                {
                    this,
                    sec,
                    this.Complement,
                    sec.Complement
                };
            }
        }

        public RGBAColor[] Analogous
        {
            get
            {
                RGBAColor c = this;

                return Enumerable.Range(-3, 7).Select(i => c.Rotate(i * Math.PI / 6)).ToArray();
            }
        }

        public RGBAColor[] Neutrals
        {
            get
            {
                RGBAColor c = this;

                return Enumerable.Range(-3, 7).Select(i => c.Rotate(i * Math.PI / 12)).ToArray();
            }
        }

        #endregion
        #region CONSTRUCTORS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor(uint argb)
            : this((int)argb)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor(int argb) : this() => ARGB = argb;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor(double r, double g, double b)
            : this(r, g, b, 1d)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RGBAColor(double r, double g, double b, double α)
            : this()
        {
            Af = α;
            Rf = r;
            Gf = g;
            Bf = b;
        }

        #endregion
        #region INSTANCE METHODS

        public override string ToString() => $"#{ARGB:x8}";

        public RGBAColor Rotate(Scalar φ)
        {
            φ += Scalar.Tau;
            φ %= Scalar.Tau;

            if (φ.IsZero)
                return this;
            else if (φ.Is(Scalar.Pi))
                return Complement;

            ToHSL(out double h, out double s, out double l);

            return FromHSL(h + φ, s, l, Af);
        }

        public RGBAColor[] GetNeutrals(double φ_step, int count)
        {
            RGBAColor c = this;

            return Enumerable.Range(-count / 2, count).Select(i => c.Rotate(i * φ_step)).ToArray();
        }

        public Scalar EucledianDistanceTo(RGBAColor other) => ((Vector4)this).DistanceTo(other);

        public Scalar CIALAB94DistanceTo(RGBAColor other)
        {
            ToCIELAB(out double L1, out double a1, out double b1);
            other.ToCIELAB(out double L2, out double a2, out double b2);

            double δL = L1 - L2;
            double δa = a1 - a2;
            double δb = b1 - b2;

            double c1 = Math.Sqrt(a1 * a1 + b1 *b1);
            double c2 = Math.Sqrt(a2 * a2 + b2 * b2);
            double δC = c1 - c2;
            double δH = δa * δa + δb * δb - δC * δC;

            δH = δH < 0 ? 0 : Math.Sqrt(δH);

            double sc = 1.0 + 0.045 * c1;
            double sh = 1.0 + 0.015 * c1;

            double δCkcsc = δC / sc;
            double δHkhsh = δH / sh;

            double i = δL * δL + δCkcsc * δCkcsc + δHkhsh * δHkhsh;

            return i < 0 ? 0 : Math.Sqrt(i);
        }

        public int CompareTo([AllowNull] RGBAColor other)
        {
            int dist = ((Vector3)this).Length.CompareTo(((Vector3)other).Length);

            return dist == 0 ? A.CompareTo(other.A) : dist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out double r, out double g, out double b, out double α)
        {
            r = Rf;
            g = Gf;
            b = Bf;
            α = Af;
        }

        /// <summary>
        /// Exports the HSL-color channels.
        /// </summary>
        /// <param name="h">The HSL-color's hue channel</param>
        /// <param name="s">The HSL-color's saturation channel</param>
        /// <param name="l">The HSL-color's luminosity channel</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToHSL(out double h, out double s, out double l)
        {
            double _R = Rf;
            double _G = Gf;
            double _B = Bf;
            double α = Math.Min(Math.Min(_R, _G), _B);
            double β = Math.Max(Math.Max(_R, _G), _B);
            double δ = β - α;

            l = (β + α) / 2;

            if (δ < 1e-5)
                s = h = 0;
            else
            {
                s = δ / (l < .5 ? β + α : 2 - β - α);

                var δr = (β - _R) / δ;
                var δg = (β - _G) / δ;
                var δb = (β - _B) / δ;

                h = _R == β ? δb - δg :
                    _G == β ? 2 + δr - δb :
                              4 + δg - δr;

                h *= 60;

                if (h < 0)
                    h += 360;

                h = h / 180 * Math.PI;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToCIELAB(out double L, out double a, out double b)
        {
            double pre(double channel) => channel > 0.04045 ? Math.Pow((channel + 0.055) / 1.055, 2.4) : channel / 12.92;
            double post(double channel) => channel > 0.008856 ? Math.Pow(channel, 1 / 3) : (7.787 * channel) + 16 / 116;

            double rf = pre(Rf);
            double gf = pre(Gf);
            double bf = pre(Bf);
            double x = (rf * 0.4124 + gf * 0.3576 + bf * 0.1805) / 0.95047;
            double y = (rf * 0.2126 + gf * 0.7152 + bf * 0.0722) / 1.00000;
            double z = (rf * 0.0193 + gf * 0.1192 + bf * 0.9505) / 1.08883;

            x = post(x);
            y = post(y);
            z = post(z);
            L = (116 * y) - 16;
            a = 500 * (x - y);
            b = 200 * (y - z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConsoleColor ToConsoleColor(ConsoleColorScheme color_scheme)
        {
            RGBAColor @this = this;
            Scalar norm = Scalar.Sqrt(3);

            return (from kvp in _console_colors[(int)color_scheme]
                    orderby kvp.Value.EucledianDistanceTo(@this) / norm ascending
                    select kvp.Key).FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToVT100ForegroundString() => $"\x1b[38;2;{R};{G};{B}m";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToVT100BackgroundString() => $"\x1b[48;2;{R};{G};{B}m";

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
        public static RGBAColor FromConsoleColor(ConsoleColor color, ConsoleColorScheme color_scheme) => _console_colors[(int)color_scheme][color];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromARGB(int argb) => new RGBAColor(argb);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromARGB(uint argb) => new RGBAColor(argb);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromComplexWrapped(Complex c) => FromComplexWrapped(c, Scalar.One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromComplexWrapped(Complex c, Scalar α)
        {
            Scalar l = c.Length;
            Scalar i = 1L << (int)Math.Log2(l);

            if (l < 1)
                return FromHSL(c.Argument, 1, l / 2, α);

            l %= i;
            l /= (long)i << 1;
            l *= 4 / 5d;
            l += 1 / 5d;

            return FromHSL(c.Argument, 1, l, α);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromComplexSmooth(Complex c) => FromComplexSmooth(c, Scalar.One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromComplexSmooth(Complex c, Scalar α) => FromComplexSmooth(c, α, .95);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromComplexSmooth(Complex c, Scalar α, Scalar white_shift) => FromHSL(c.Argument, 1, 1 - white_shift.Clamp().Power(c.Length), α);

        /// <summary>
        /// Converts the given HSL-color to a RGBA-color instance.
        /// </summary>
        /// <param name="h">The HSL-color's hue channel [0..2π]</param>
        /// <param name="s">The HSL-color's saturation channel [0..1]</param>
        /// <param name="l">The HSL-color's luminosity channel [0..1]</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromHSL(double h, double s, double l) => FromHSL(h, s, l, 1);

        /// <summary>
        /// Converts the given HSL-color to a RGBA-color instance.
        /// </summary>
        /// <param name="h">The HSL-color's hue channel [0..2π]</param>
        /// <param name="s">The HSL-color's saturation channel [0..1]</param>
        /// <param name="l">The HSL-color's luminosity channel [0..1]</param>
        /// <param name="α">The color's α-channel (opacity) [0..1]</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromHSL(double h, double s, double l, double α)
        {
            if (s.IsZero())
            {
                byte gray = (byte)Math.Round(l * 255);

                return new RGBAColor(gray, gray, gray)
                {
                    Af = α
                };
            }
            else
            {
                double t2 = l < .5 ? l * (1 + s) : l + s - (l * s);
                double t1 = (2 * l) - t2;

                h *= 180 / Math.PI;

                return new RGBAColor
                {
                    Rf = calc(h + 120, t1, t2),
                    Gf = calc(h, t1, t2),
                    Bf = calc(h - 120, t1, t2),
                    Af = α,
                };

                static double calc(double h, double t1, double t2)
                {
                    h = (h + 360) % 360;

                    return h < 60 ? t1 + ((t2 - t1) * h / 60)
                        : h < 180 ? t2
                        : h < 240 ? t1 + ((t2 - t1) * (240 - h) / 60)
                        : t1;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromCIELAB(double L, double a, double b) => FromCIELAB(L, a, b, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor FromCIELAB(double L, double a, double b, double α)
        {
            double y = (L + 16) / 116;
            double x = a / 500 + y;
            double z = y - b / 200;
            void pre(double fac, ref double channel)
            {
                double c3 = channel * channel * channel;

                channel = fac * ((c3 > 0.008856) ? c3 : (channel - 16 / 116) / 7.787);
            }
            double post(double channel) => ((channel > 0.0031308) ? (1.055 * Math.Pow(channel, 1 / 2.4) - 0.055) : 12.92 * channel).Clamp();

            pre(0.95047, ref x);
            pre(1.00000, ref y);
            pre(1.08883, ref z);

            double rf = post(x * 3.2406 + y * -1.5372 + z * -0.4986);
            double gf = post(x * -0.9689 + y * 1.8758 + z * 0.0415);
            double bf = post(x * 0.0557 + y * -0.2040 + z * 1.0570);

            return new RGBAColor(rf, gf, bf, α);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBAColor LinearInterpolate(RGBAColor px1, RGBAColor px2, double amount)
        {
            RGBAColor res = default;

            res.Rf = px1.Rf * (1 - amount) + px2.Rf * amount;
            res.Gf = px1.Gf * (1 - amount) + px2.Gf * amount;
            res.Bf = px1.Bf * (1 - amount) + px2.Bf * amount;
            res.Af = px1.Af * (1 - amount) + px2.Af * amount;

            return res;
        }

        #endregion
        #region OPERATORS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(RGBAColor px) => px.ARGB;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(RGBAColor px) => px.ARGBu;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(int argb) => new RGBAColor { ARGB = argb };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(uint argb) => new RGBAColor { ARGBu = argb };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator (byte r, byte g, byte b, byte α)(RGBAColor px) => (px.R, px.G, px.B, px.A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(RGBAColor px) => new Vector3(px.Rf, px.Gf, px.Bf);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4(RGBAColor px) => new Vector4(px.Rf, px.Gf, px.Bf, px.Af);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color(RGBAColor px) => Color.FromArgb(px.ARGB);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(Vector3 px) => new RGBAColor(px.X, px.Y, px.Z, 1d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(Vector4 px) => new RGBAColor(px.X, px.Y, px.Z, px.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor(Color px) => new RGBAColor(px.ToArgb());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor((double r, double g, double b) px) => new RGBAColor(px.r, px.g, px.b, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor((byte r, byte g, byte b, byte α) px) => new RGBAColor(px.r, px.g, px.b, px.α);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RGBAColor((double r, double g, double b, double α) px) => new RGBAColor(px.r, px.g, px.b, px.α);

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
