// #define USE_HSL_BRIGHTNESS
// #define USE_HSL_SATURATION

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Common;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Imaging.Effects
{
    #region BASIC COLOR EFFECTS

    public sealed class ConstantBlendEffect
        : ColorEffect
    {
        public RGBAColor Color { get; }

        public BlendMode Mode { get; }


        public ConstantBlendEffect(RGBAColor color, BlendMode mode)
        {
            Color = color;
            Mode = mode;
        }

        protected override RGBAColor ProcessColor(RGBAColor input) => RGBAColor.Blend(input, Color, Mode);
    }

    /// <summary>
    /// Represents a grayscale bitmap color effect.
    /// </summary>
    public sealed class Grayscale
        : RGBColorEffect
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Grayscale()
            : base(new Matrix3(
                1, 1, 1,
                1, 1, 1,
                1, 1, 1
            ) / 3)
        {
        }
    }

    /// <summary>
    /// Represents an alpha-opacity bitmap effect.
    /// </summary>
    public sealed class Opacity
        : RGBAColorEffect
    {
        /// <summary>
        /// Creates a new instance, which applies the current effect to the given amount
        /// </summary>
        /// <param name="amount">Amount [0..1]</param>
        public Opacity(Scalar amount)
            : base((
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, amount.Clamp()
            ))
        {
        }
    }

    /// <summary>
    /// Represents an color inversion bitmap effect.
    /// </summary>
    public sealed class Invert
        : ColorEffect.Delegated
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Invert()
            : base(c => c.Complement)
        {
        }
    }

    public sealed class Brightness
        : RGBColorEffect
    {
        public Brightness(Scalar amount)
            : base((
                amount, 0, 0,
                0, amount, 0,
                0, 0, amount
            ))
        {
        }
    }

    public sealed class Saturation
        : RGBColorEffect
    {
        public Saturation(Scalar amount)
            : base(new Func<Matrix3>(() =>
            {
                amount = amount.Max(0);
                (Scalar r, Scalar g, Scalar b) = (1 - amount) * new Vector3(.3086, .6094, .0820);

                return (
                    r + amount, g, b,
                    r, g + amount, b,
                    r, g, b + amount
                );
            })())
        {
        }
    }

    public sealed class Contrast
        : RGBColorEffect
    {
        public Contrast(Scalar amount)
            : base(new Func<Matrix3>(() =>
            {
                Scalar c = amount.Max(0);
                
                c *= 1 - ((2 - c) / 2);

                // TODO : fix this shite

                return (
                    c, 0, 0,
                    0, c, 0,
                    0, 0, c
                );
            })())
        {
        }
    }

    public sealed class Hue
        : ColorEffect
    {
        public Scalar Degree { get; }


        public Hue(Scalar degree) => Degree = degree.Modulus(Scalar.Tau);

        protected override RGBAColor ProcessColor(RGBAColor input)
        {
            input.ToHSL(out double h, out double s, out double l);

            return RGBAColor.FromHSL(h + Degree, s, l);
        }
    }

    // TODO : gamma correction
    // TODO : color overlay
    // TODO : tint effect

    public sealed class SimpleGlow
        //: PartialBitmapEffectBase
    {
        //public override Bitmap ApplyTo(Bitmap bmp, Rectangle region) => ;
        /*
            return bmp.ApplyEffectRange<FastBlurBitmapEffect>(Range, Radius)
                        .ApplyBlendEffectRange<AddBitmapBlendEffect>(bmp, Range)
                        .ApplyEffectRange<SaturationBitmapEffect>(Range, 1 + (.075 * Amount))
                        .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1 - (.075 * Amount))
                        .Average(bmp, Amount);
         */
    }

    public sealed class RGBtoHSL
        : ColorEffect
    {
        protected override RGBAColor ProcessColor(RGBAColor input)
        {
            input.ToHSL(out double h, out double s, out double l);

            return new Vector4(h / Scalar.Tau, s, l, input.Af);
        }
    }

    public sealed class HSLtoRGB
        : ColorEffect
    {
        protected override RGBAColor ProcessColor(RGBAColor input)
        {
            Scalar h = input.Rf * Scalar.Tau;
            Scalar s = input.Gf;
            Scalar l = input.Bf;

            return RGBAColor.FromHSL(h, s, l);
        }
    }

    public sealed class Cartoon
        : ColorEffect
    {
        public int Steps { get; }


        public Cartoon(int steps) => Steps = Math.Max(steps, 1);

        protected override RGBAColor ProcessColor(RGBAColor input)
        {
            Vector4 v = input;

            return new Vector4(
                (v.X * Steps).Rounded / Steps,
                (v.Y * Steps).Rounded / Steps,
                (v.Z * Steps).Rounded / Steps,
                v.W
            );
        }
    }

    public sealed class Cartoon2
        : PartialBitmapEffect.Accelerated
    {
        public int Steps { get; }


        public Cartoon2(int steps) => Steps = Math.Max(steps, 1);

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int s = Steps;
            int s2 = Math.Max(s / 2, 1);
            int[] indices = GetIndices(bmp, region);
            RGBAColor[] blurred = new RGBAColor[bmp.Width * bmp.Height];

            fixed (RGBAColor* ptr = blurred)
                new BoxBlur(1).Process(bmp, source, ptr, region);

            Parallel.For(0, indices.Length, i =>
            {
                Scalar l = ((Scalar)source[i].CIEGray * Steps).Rounded / Steps;

                blurred[i].ToHSL(out double h, out double s, out _);

                destination[i] = RGBAColor.FromHSL(
                    ((Scalar)h * s2).Rounded / s2,
                    s,
                    l,
                    source[i].Af
                );
            });
        }
    }

    public sealed class Cartoon3
        : PartialBitmapEffect
    {
        public int Steps { get; }
        public Scalar EdgeSensitivity { get; }


        public Cartoon3(int steps)
            : this(steps, .3)
        {
        }

        public Cartoon3(int steps, Scalar edgeSensitivity)
        {
            Steps = Math.Max(steps, 2);
            EdgeSensitivity = edgeSensitivity.Clamp();
        }

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region)
        {
            double e = EdgeSensitivity / 10;
            Bitmap background = new Cartoon2(Steps).ApplyTo(bmp, region);
            
            return bmp.ApplyEffect(new AcceleratedChainedPartialBitmapEffect(
                new SingleConvolutionEffect(new Matrix3(
                    1, 1, 1,
                    1, 1, 1,
                    1, 1, 1
                ) / 9),
                new SingleConvolutionEffect(new Matrix3(
                    0, -1, 0,
                    -1, 4, -1,
                    0, -1, 0
                )),
                new ColorEffect.Delegated(c =>
                {
                    double s = 1 - c.CIEGray;

                    s = s < .3 ? 0 : s * s;
                    s = s > .9 + e ? 1 : 0;

                    return new Vector4(s, s, s, c.Af);
                }),
                new BlendEffect(background, BlendMode.Multiply, 1)
            ), region);
        }
    }

    #endregion
    #region INSTAGRAM CSS COLOR FILTERS

    public sealed class SepiaFilter
        : RGBColorEffect
    {
        public SepiaFilter()
            : base((
                .393, .769, .189,
                .349, .686, .168,
                .272, .534, .131
            ))
        {
        }
    }

    /// <summary>
    /// Represents the Instagram 'Nashville' CSS bitmap filter.
    /// </summary>
    public sealed class NashvilleFilter
        : PartialBitmapEffect
    {
        // filter: sepia(.25) contrast(1.5) brightness(.9) hue-rotate(-15deg)
        // before: radial-gradient(circle closest-corner, rgba(128, 78, 15, .5) 0, rgba(128, 78, 15, .65) 100%)
        //         [screen]

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region) => bmp.ApplyEffect(new SepiaFilter(), region, .25)
                                                                           .ApplyEffect(new Contrast(1.5), region)
                                                                           .ApplyEffect(new Brightness(.9), region)
                                                                           .ApplyEffect(new Hue(-.261799), region)
                                                                           .ApplyEffect(new ConstantBlendEffect(0x47804e0f, BlendMode.Screen), region);
    }

    // ADEN
    // INKWELL
    // REYES
    // WALDEN
    // JUNO
    // MAVEN
    // PERPETUA
    // SLUMBER
    // LOFI

    #endregion
    #region MATRIX CONVOLUTION EFFECTS

    /// <summary>
    /// Represents a simple 3x3 edge-detection bitmap effect.
    /// The underlying convolution matrix is:
    /// <para>
    /// -1, -1, -1<br/>
    /// -1, 8, -1 <br/>
    /// -1, -1, -1<br/>
    /// </para>
    /// </summary>
    public sealed class EdgeDetection
        : SingleConvolutionEffect
    {
        public EdgeDetection()
            : base(new Matrix3(
                -1, -1, -1,
                -1, 8, -1,
                -1, -1, -1
            ))
        {
        }
    }

    /// <summary>
    /// Represents the Sobel edge-detection bitmap effect.
    /// </summary>
    public sealed class SobelEdgeDetection
        : MultiConvolutionEffect
    {
        public SobelEdgeDetection()
            : base(new Matrix3(
                -1, 0, 1,
                -2, 0, 2,
                -1, 0, 1
            ), new Matrix3(
                1, 2, 1,
                0, 0, 0,
                -1, -2, -1
            ))
        {
        }
    }

    /// <summary>
    /// Represents the Prewitt edge-detection bitmap effect.
    /// </summary>
    public sealed class PrewittEdgeDetection
        : MultiConvolutionEffect
    {
        public PrewittEdgeDetection()
            : base(new Matrix3(
                -1, 0, 1,
                -1, 0, 1,
                -1, 0, 1
            ), new Matrix3(
                1, 1, 1,
                0, 0, 0,
                -1, -1, -1
            ))
        {
        }
    }

    /// <summary>
    /// Represents the Scharr edge-detection bitmap effect.
    /// </summary>
    public sealed class ScharrEdgeDetection
        : MultiConvolutionEffect
    {
        public ScharrEdgeDetection()
            : base(new Matrix3(
                -3, 0, 3,
                -10, 0, 10,
                -3, 0, 3
            ), new Matrix3(
                3, 10, 3,
                0, 0, 0,
                -3, -10, -3
            ))
        {
        }
    }

    /// <summary>
    /// Represents the Kirsch edge-detection bitmap effect.
    /// </summary>
    public sealed class KirschEdgeDetection
        : MultiConvolutionEffect
    {
        public KirschEdgeDetection(Scalar amount)
            : base(new Func<MatrixNM[]>(() =>
            {
                Scalar a = amount.Clamp();

                return new MatrixNM[]
                {
                    new Matrix3(
                        a * 5, a * 5, a * 5,
                        a * -3, 1 - a, a * -3,
                        a * -3, a * -3, a * -3
                    ),
                    new Matrix3(
                        a * 5, a * -3, a * -3,
                        a * 5, 1 - a, a * -3,
                        a * 5, a * -3, a * -3
                    ),
                };
            })())
        {
        }
    }
    
    public sealed class RobertsEdgeDetection
        : MultiConvolutionEffect
    {
        public RobertsEdgeDetection()
            : base(new Matrix3(
                -1, 0, 0,
                0, 0, 0,
                0, 0, 1
            ),
            new Matrix3(
                0, 0, -1,
                0, 0, 0,
                1, 0, 0
            ))
        {
        }
    }

    public sealed class LaplaceEdgeDetection
        : SingleConvolutionEffect
    {
        public LaplaceEdgeDetection()
            : base(new Matrix5(
                 -1, -1, -1, -1, -1,
                 -1, -1, -1, -1, -1,
                 -1, -1, 24, -1, -1,
                 -1, -1, -1, -1, -1,
                 -1, -1, -1, -1, -1
            ))
        {
        }
    }

    public sealed class CannyEdgeDetection
        : AcceleratedChainedPartialBitmapEffect
    {
        public Scalar Threshold { get; }


        public CannyEdgeDetection()
            : this(.4)
        {
        }

        public CannyEdgeDetection(Scalar threshold)
            : base(
                new Grayscale(),
                new GaussianBlur5x5(),
                new SobelEdgeDetection(),
                // TODO : edge thinning
                new ColorEffect.Delegated(c =>
                {
                    Vector3 col = c.Average >= threshold.Clamp() ? RGBAColor.White : RGBAColor.Black;

                    return new RGBAColor(col.X, col.Y, col.Z, c.Af);
                })
            ) => Threshold = threshold.Clamp();
    }

    public sealed class ED88EdgeDetection
        : SingleConvolutionEffect
    {
        public ED88EdgeDetection()
            : base(new Matrix5(
                  1,  0, -2, -1,  1,
                 -1,  0, -1,  0,  0, 
                 -2, -1, .5,  1,  2,
                  0,  0,  1,  0,  1,
                 -1,  1,  2,  0, -1
            ))
        {
        }
    }

    public sealed class BoxSharpen
        : SingleConvolutionEffect
    {
        public BoxSharpen()
            : this(1)
        {
        }

        public BoxSharpen(Scalar radius)
            : base(new Func<MatrixNM>(() =>
            {
                int sz = (int)radius.Clamp(0, 10) * 2 + 1;
                Scalar[,] mat = new Scalar[sz, sz];

                for (int i = 0; i < sz * sz; i++)
                    mat[i / sz, i % sz] = -1d / (sz * sz);

                mat[sz / 2, sz / 2] += 2d;

                return new MatrixNM(mat);
            })())
        {
        }
    }

    public sealed class FastSharpen
        : SingleConvolutionEffect
    {
        public FastSharpen()
            : base(new Matrix5(
                0, 0, -1, 0, 0,
                0, 0, -4, 0, 0,
                -1, -4, 21, -4, -1,
                0, 0, -4, 0, 0,
                0, 0, -1, 0, 0
            ))
        {
        }
    }

    public sealed class BoxUnsharp
        : SingleConvolutionEffect
    {
        public BoxUnsharp()
            : base(new Matrix3(
                1, 1, 1,
                1, -7, 1,
                1, 1, 1
            ))
        {
        }
    }

    public sealed class BoxBlur
        : SingleConvolutionEffect
    {
        public int Radius { get; }


        public BoxBlur()
            : this(1)
        {
        }

        public BoxBlur(Scalar radius)
            : base(new Func<MatrixNM>(() =>
            {
                int sz = (int)radius.Clamp(0, 10) * 2 + 1;
                Scalar[,] mat = new Scalar[sz, sz];

                for (int i = 0; i < sz * sz; i++)
                    mat[i / sz, i % sz] = 1d / (sz * sz);

                return new MatrixNM(mat);
            })()) => Radius = (int)radius;
    }

    public sealed class FastBoxBlur
        : PartialBitmapEffect
    {
        public int Radius { get; }


        public FastBoxBlur()
            : this(3)
        {
        }

        public FastBoxBlur(Scalar radius) => Radius = (int)radius.Clamp(0, 20);

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region)
        {
            int c = 2 * Radius + 1;
            Scalar[,] m = new Scalar[c, 1];

            for (int i = 0; i < c; ++i)
                m[i, 0] = Scalar.One / c;

            MatrixNM h = new MatrixNM(m);
            MatrixNM v = h.Transposed;

            return new AcceleratedChainedPartialBitmapEffect(
                new SingleConvolutionEffect(h),
                new SingleConvolutionEffect(v)
            ).ApplyTo(bmp, region);
        }
    }

    public sealed class Emboss
        : SingleConvolutionEffect
    {
        public Emboss()
            : base(new Matrix3(
                -2, -1, 0,
                -1, 1, 1,
                0, 1, 2
            ))
        {
        }
    }

    public sealed class Engrave
        : SingleConvolutionEffect
    {
        public Engrave()
            : base(new Matrix3(
                 -2, 0, 0,
                 0, 2, 0,
                 0, 0, 0
            ))
        {
        }
    }

    public sealed class GaussianBlur5x5
        : SingleConvolutionEffect
    {
        public GaussianBlur5x5()
            : base(new Matrix5(
                 1, 4, 6, 4, 1,
                 4, 16, 24, 16, 4,
                 6, 24, 36, 24, 6,
                 4, 16, 24, 16, 4,
                 1, 4, 6, 4, 1
            ) / 256)
        {
        }
    }

    public sealed unsafe class GaussianBlur
        : PartialBitmapEffect.Accelerated
    {
        public Scalar Radius { get; }
        public EdgeHandlingMode EdgeHandling { get; } = EdgeHandlingMode.Extend;


        public GaussianBlur(Scalar radius) => Radius = radius.Max(0);

        protected internal override void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            RGBAColor[] temp = new RGBAColor[bmp.Width * bmp.Height];
            Scalar[] sz = GenerateBoxes(3);

            fixed (RGBAColor* ptr = temp)
            {
                BoxBlur(source, destination, bmp, region, (sz[0] - 1) / 2);
                BoxBlur(destination, ptr, bmp, region, (sz[1] - 1) / 2);
                BoxBlur(ptr, destination, bmp, region, (sz[2] - 1) / 2);
            }
        }

        private Scalar[] GenerateBoxes(int count)
        {
            Scalar sig = 12 * Radius * Radius;
            int wl = (int)((sig / count) + 1).Sqrt();

            if (wl % 2 == 0)
                --wl;

            int wu = wl + 2;
            Scalar mIdeal = (sig - (count * wl * wl) - (4 * count * wl) - (3 * count)) / (-4 * wl - 4);
            Scalar m = mIdeal.Rounded;

            Scalar[] sizes = new Scalar[count];

            for (int i = 0; i < count; i++)
                sizes[i] = i < m ? wl : wu;

            return sizes;
        }

        private void BoxBlur(RGBAColor* src, RGBAColor* dst, Bitmap bmp, Rectangle region, Scalar r)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            Scalar iar = r + r + 1;
            int[] idx = GetIndices(bmp, region);

            Parallel.For(0, idx.Length, i => dst[idx[i]] = src[idx[i]]);
            Parallel.For(0, idx.Length, i =>
            {
                int y = idx[i] / w;
                int x = idx[i] % w;
                Vector4 v = Vector4.Zero;

                for (int offs = -(int)r; offs <= r; ++offs)
                    v += GetIndex(x + offs, y, w, h, region, EdgeHandling) is int idx ? dst[idx] : default;

                dst[idx[i]] = v / iar;
            });
            Parallel.For(0, idx.Length, i =>
            {
                int y = idx[i] / w;
                int x = idx[i] % w;
                Vector4 v = Vector4.Zero;

                for (int offs = -(int)r; offs <= r; ++offs)
                    v += GetIndex(x, y + offs, w, h, region, EdgeHandling) is int idx ? dst[idx] : default;

                dst[idx[i]] = v / iar;
            });
        }
    }

    #endregion
    #region TRANSFORM EFFECTS

    public sealed class ScaleTransform
        : TransformEffect
    {
        public ScaleTransform(Scalar s)
            : this(s, s)
        {
        }

        public ScaleTransform(Scalar sx, Scalar sy)
            : base((
                sx, 0,
                0, sy
            ))
        {
        }


        public static ScaleTransform Uniform(Scalar factor) => new ScaleTransform(factor);

        public static ScaleTransform X(Scalar factor) => new ScaleTransform(factor, 0);

        public static ScaleTransform Y(Scalar factor) => new ScaleTransform(0, factor);
    }

    public sealed class FlipTransform
        : TransformEffect
    {
        public FlipTransform(bool xdir, bool ydir)
            : base((
                xdir ? -1 : 1, 0,
                0, ydir ? -1 : 1
            ))
        {
        }

        public static FlipTransform FlipX => new FlipTransform(true, false);

        public static FlipTransform FlipY => new FlipTransform(false, true);
    }

    public sealed class RotateTransform
        : TransformEffect
    {
        // TODO : fix this shite

        public RotateTransform(Scalar φ)
            : base((
                φ.Cos(), -φ.Sin(),
                φ.Sin(), φ.Cos()
            ))
        {
        }
    }

    public sealed class TranslateTransform
        : TransformEffect
    {
        public TranslateTransform(Vector2 t)
            : base((1, 0, 0, 1), t)
        {
        }

        public TranslateTransform(Scalar tx, Scalar ty)
            : this(new Vector2(tx, ty))
        {
        }
    }

    #endregion

    public sealed class NoiseEffect
        : PartialBitmapEffect.Accelerated
    {
        public Random RandomNumberGenerator { get; }

        public long Seed => RandomNumberGenerator.Seed;

        public NoiseMode Mode { get; }


        public NoiseEffect(Random rng)
            : this(rng, NoiseMode.Regular)
        {
        }

        public NoiseEffect(Random rng, NoiseMode mode)
        {
            RandomNumberGenerator = rng;
            Mode = mode;
        }

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            bool gray = Mode.HasFlag(NoiseMode.Grayscale);
            bool alpha = Mode.HasFlag(NoiseMode.AlphaNoise);

            Parallel.For(0, indices.Length, i =>
            {
                int index = indices[i];
                RGBAColor c = new RGBAColor(RandomNumberGenerator.NextByte(), RandomNumberGenerator.NextByte(), RandomNumberGenerator.NextByte(), destination[index].A);

                if (gray)
                    c = new RGBAColor(c.R, c.R, c.R, c.A);

                if (alpha)
                    c.A = RandomNumberGenerator.NextByte();

                destination[index] = c;
            });
        }
    }

    [Flags, Serializable]
    public enum NoiseMode
        : byte
    {
        Regular = 0,
        Grayscale = 1,
        AlphaNoise = 2,
    }

    public sealed class PerlinNoiseEffect
        : PartialBitmapEffect.Accelerated
    {
        public PerlinNoise Noise { get; }


        public PerlinNoiseEffect(PerlinNoiseSettings settings) => Noise = new PerlinNoise(settings);

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            throw new NotImplementedException("TODO");

            Scalar[,] map = Noise.GenerateNoiseMap2D(region.Width, region.Height);
            int[] indices = GetIndices(bmp, region);
            int w = bmp.Width;

            Parallel.For(0, indices.Length, i =>
            {
                int index = indices[i];
                (int x, int y) = GetRelativeCoordinates(index, w, region);
                byte v = (byte)((1 + map[x, y]) * 128);

                destination[index] = new RGBAColor(v, v, v, 0xff);
            });
        }
    }

    public sealed class Glow
        : PartialBitmapEffect
    {
        public int Radius { get; }
        public Scalar Intensity { get; }


        public Glow(Scalar radius, Scalar intensity)
        {
            Radius = (int)radius.Clamp(0, 20);
            Intensity = intensity.Max(0);
        }

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region) => new ChainedPartialBitmapEffect(
            new FastBoxBlur(Radius),
            new RGBColorEffect(Matrix3.Identity * Intensity),
            new BlendEffect(bmp, BlendMode.Add, 1)
        ).ApplyTo(bmp, region);
    }

    public sealed class RGBSplit
        : PartialBitmapEffect
    {
        public Scalar Angle { get; }
        public Scalar Size { get; }

        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

        public PixelInterpolationMode Interpolation { set; get; } = PixelInterpolationMode.BilinearInterpolation;


        public RGBSplit(Scalar angle, Scalar size)
        {
            Angle = angle.Modulus(Scalar.Tau);
            Size = size.Max(Scalar.Zero);
        }

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region)
        {
            Vector2 shift = (Angle.Cos() * Size, Angle.Sin() * Size);
            Bitmap red = bmp.ApplyEffect(new AcceleratedChainedPartialBitmapEffect(
                new RGBAColorEffect((
                    1, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0
                )),
                new TranslateTransform(shift)
                {
                    EdgeHandling = EdgeHandling,
                    Interpolation = Interpolation
                }
            ), region);
            Bitmap blue = bmp.ApplyEffect(new AcceleratedChainedPartialBitmapEffect(
                new RGBAColorEffect((
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 0
                )),
                new TranslateTransform(-shift)
                {
                    EdgeHandling = EdgeHandling,
                    Interpolation = Interpolation
                }
            ), region);

            return bmp.ApplyEffect(new AcceleratedChainedPartialBitmapEffect(
                new RGBAColorEffect((
                    0, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 1
                )),
                new BlendEffect(red, BlendMode.Add, 1),
                new BlendEffect(blue, BlendMode.Add, 1)
            ), region);
        }
    }





    public sealed class VHSEffect
        : PartialBitmapEffect
    {
        private readonly Random _rng;
        private long _curr;

        public long InitialSeed { get; }

        public long FrameCount { get; private set; }


        public VHSEffect()
            : this(Guid.NewGuid().GetHashCode())
        {
        }

        public VHSEffect(long seed)
        {
            _rng = new XorShift(seed);
            InitialSeed = seed;

            NextSeed();
        }

        public void NextSeed()
        {
            _curr = _rng.NextLong();
            FrameCount++;
        }

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region)
        {
            XorShift random = new XorShift(_curr);

            bmp = new RGBSplit(0, 10 + random.NextFloat())
            {
                EdgeHandling = EdgeHandlingMode.Mirror,
                Interpolation = PixelInterpolationMode.BilinearInterpolation
            }.ApplyTo(bmp, region);

            // TODO

            return bmp;
        }
    }



    public sealed class SquarePixelation
        : PartialBitmapEffect.Accelerated
    {
        public int Size { get; }


        public SquarePixelation(Scalar size) => Size = (int)size.Max(1);

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int rx = region.X;
            int ry = region.Y;
            int rw = region.Width;
            int rh = region.Height;
            int w = bmp.Width;
            int s = Size;

            Parallel.For(0, rw * rh, i =>
            {
                int xo = i % rw;
                int yo = i / rw;

                if (xo % s == 0 && yo % s == 0)
                {
                    Vector4 sum = Vector4.Zero;
                    int count = 0;

                    for (int x = 0; x < Size; ++x)
                        for (int y = 0; y < Size; ++y)
                            if (x + xo < rw && y + yo < rh)
                            {
                                sum += source[(ry + yo + y) * w + rx + xo + x];
                                ++count;
                            }

                    RGBAColor avg = sum / count;

                    for (int x = 0; x < Size; ++x)
                        for (int y = 0; y < Size; ++y)
                            if (y + yo < rh && x + xo < rw)
                                destination[(ry + yo + y) * w + rx + xo + x] = avg;
                }
            });
        }
    }

    // https://www.redblobgames.com/grids/hexagons
    public sealed class HexagonalPixelation
        : PartialBitmapEffect.Accelerated
    {
        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

        public Scalar Size { get; }


        public HexagonalPixelation(Scalar size) => Size = size.Max(1);

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            (int x, int y)[] hexindices = new (int, int)[indices.Length];
            int w = bmp.Width;
            int h = bmp.Height;
            int SZ = (int)Size;
            Scalar s3 = Math.Sqrt(3);
            Matrix2 px_to_hex = (
                s3 / 3, -1d / 3,
                0, 2d / 3
            );
            Matrix2 hex_to_px = (
                s3, s3 / 2,
                0, 3d / 2
            );

            Parallel.For(0, indices.Length, idx =>
            {
                int coord = indices[idx];
                Vector2 hex = px_to_hex * (
                    coord % w,
                    coord / w
                ) / SZ;

                int rx = (int)hex.X.Rounded;
                int rz = (int)hex.Y.Rounded;
                int ry = (int)(-hex.X - hex.Y).Rounded;

                Scalar x_diff = (rx - hex.X).Abs();
                Scalar y_diff = (ry + hex.X + hex.Y).Abs();
                Scalar z_diff = (rz - hex.Y).Abs();
                
                if ((x_diff > y_diff) && (x_diff > z_diff))
                    rx = -ry - rz;
                else if (y_diff > z_diff)
                    ry = -rx - rz;
                else
                    rz = -rx - ry;

                hex = (rx, rz);
                hex = hex_to_px * hex * SZ;

                if (GetIndex((int)hex.X, (int)hex.Y, w, h, region, EdgeHandling) is int i)
                    destination[coord] = source[i];
            });
        }
    }

    public sealed class TriangularPixelation
        : PartialBitmapEffect.Accelerated
    {
        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

        public Scalar Size { get; }


        public TriangularPixelation(Scalar size) => Size = size.Max(1);

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            (int x, int y)[] hexindices = new (int, int)[indices.Length];
            int w = bmp.Width;
            int h = bmp.Height;
            int SZ = (int)Size;
            Scalar s3 = Math.Sqrt(3);
            Matrix2 px_to_tri = (
                s3 / 3, -1d / 3,
                0, 2d / 3
            );
            Matrix2 tri_to_px = (
                s3, s3 / 2,
                0, 3d / 2
            );

            Parallel.For(0, indices.Length, idx =>
            {
                int coord = indices[idx];
                Vector2 tri = px_to_tri * (
                    coord % w,
                    coord / w
                ) / SZ;

                Scalar rx = (int)tri.X;
                Scalar rz = (int)tri.Y;
                int ry = (int)(-tri.X - tri.Y);

                Scalar x_diff = (rx - tri.X).Abs();
                Scalar y_diff = (ry + tri.X + tri.Y).Abs();

                tri = (rx, rz);

                if (y_diff >= x_diff)
                    tri += 0.5;

                tri = tri_to_px * tri * SZ;

                if (GetIndex((int)tri.X, (int)tri.Y, w, h, region, EdgeHandling) is int i)
                    destination[coord] = source[i];
            });
        }
    }

    // rotate
    // skew
    // polar coords
    // 4-corner pins
    // ...

    // rgb split
    // bokeh
    // any shader ?

    /// <summary>
    /// Represents the normal bump map filter.
    /// </summary>
    public sealed class NormalMapFilter
        : PartialBitmapEffect.Accelerated
    {
        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;


        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int rx = region.X;
            int ry = region.Y;
            int rw = region.Width;
            int rh = region.Height;
            int w = bmp.Width;
            int h = bmp.Height;
            Scalar[,] m1 =
            {
                { -1, -2, -1 },
                { 0, 0, 0 },
                { 1, 2, 1 },
            };
            Scalar[,] m2 =
            {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 },
            };

            Parallel.For(0, rw * rh, j =>
            {
                int y = (j / rw) + ry;
                int x = (j % rw) + rx;
                Scalar δx = .5;
                Scalar δy = .5;

                for (int sy = -1; sy <= 1; ++sy)
                    for (int sx = -1; sx <= 1; ++sx)
                        if (GetIndex(x + sx, y + sy, w, h, region, EdgeHandling) is int idx)
                        {
                            double g = source[idx].CIEGray;

                            δx += m1[sx + 1, sy + 1] * g;
                            δy += m2[sx + 1, sy + 1] * g;
                        }

                byte δz = (byte)(((δx - .5).Abs() + (δy - .5).Abs()) * 64).Rounded;

                destination[y * w + x] = new RGBAColor
                {
                    Rf = δx,
                    Gf = δy,
                    B = (byte)(δz > 64 ? 191 : δz < 0 ? 255 : 255 - δz),
                    A = source[y * w + x].A
                };
            });
        }
    }

    public sealed class DiscreteFourierTransform
        : PartialBitmapEffect.Accelerated
    {
        public static DiscreteFourierTransform Forward { get; } = new DiscreteFourierTransform { Direction = true };

        public static DiscreteFourierTransform Backward { get; } = new DiscreteFourierTransform { Direction = false };

        private bool Direction { set; get; }


        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            int w = bmp.Width;
            int rw = region.Width;
            int rh = region.Height;
            int rx = region.X;
            int ry = region.Y;

            if (Direction) // forward
            {
                Scalar f = 1d / (rw * rh);

                Parallel.For(0, indices.Length, i =>
                {
                    int x = i % w;
                    int y = i / w;
                    Complex sum = 0;

                    for (int v = 0; v < rh; ++v)
                    {
                        Complex partial = 0;

                        for (int u = 0; u < rw; ++u)
                            partial += source[(v + ry) * w + u + rx].CIEGray * Complex.Cis((double)(-u * x) / rw);

                        sum += partial * Complex.Cis((double)(-v * y) / rh);
                    }

                    source[i].ToHSL(out double hue, out _, out _);
                    destination[i] = new Vector4(
                        sum.Real,
                        sum.Imaginary,
                        hue,
                        source[i].Af
                    );
                });
            }
            else // backward
                Parallel.For(0, indices.Length, i =>
                {
                    int x = i % w;
                    int y = i / w;
                    Complex sum = 0;

                    for (int v = 0; v < rh; ++v)
                    {
                        Complex partial = 0;

                        for (int u = 0; u < rw; ++u)
                        {
                            RGBAColor col = source[(v + ry) * w + u + rx];

                            partial += new Complex(col.Rf, col.Gf) * Complex.Cis((double)(u * x) / rw);
                        }

                        sum += partial * Complex.Cis((double)(v * y) / rh);
                    }

                    destination[i] = (Vector4)RGBAColor.FromHSL(source[i].Bf, 1, 1, source[i].Af) * sum.Real;
                });
        }
    }

    // gnerators: grid, checkerboard, mandelbrot

    public sealed class SolidColor
        : PartialBitmapEffect.Accelerated
    {
        public RGBAColor Color { get; }


        public SolidColor(RGBAColor color) => Color = color;

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* _, RGBAColor* destination, Rectangle region)
        {
            RGBAColor col = Color;
            int rw = region.Width;
            int rx = region.X;
            int ry = region.Y;
            int w = bmp.Width;

            Parallel.For(0, rw * region.Height, i => destination[(i / rw + ry) * w + (i % rw) + rx] = col);
        }
    }

    public sealed class GridGenerator
        : PartialBitmapEffect.Accelerated
    {
        public Scalar GridSpacing { get; }
        public Scalar LineSize { get; }
        public RGBAColor LineColor { get; }
        public RGBAColor BackgroundColor { get; }


        public GridGenerator(Scalar spacing, Scalar line_size, RGBAColor foreground, RGBAColor? background = null)
        {
            GridSpacing = spacing.Max(1);
            LineSize = line_size.Max(1);
            LineColor = foreground;
            BackgroundColor = background ?? (0, 0, 0, 1);
        }

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            RGBAColor fg = LineColor;
            RGBAColor bg = BackgroundColor;
            int rw = region.Width;
            int rh = region.Height;
            int rx = region.X;
            int ry = region.Y;
            int w = bmp.Width;
            Scalar s = GridSpacing;
            Scalar l = LineSize;

            Parallel.For(0, rw * rh, i =>
            {
                int x = i % rw;
                int y = i / rw;

                destination[(y + ry) * w + x + rx] = (x % s < l) || (y % s < l) ? fg : bg;
            });
        }
    }

    public sealed class CheckerboardGenerator
        : PartialBitmapEffect.Accelerated
    {
        public Scalar GridSpacing { get; }
        public RGBAColor LineColor { get; }
        public RGBAColor BackgroundColor { get; }


        public CheckerboardGenerator(Scalar spacing, RGBAColor foreground, RGBAColor? background = null)
        {
            GridSpacing = spacing.Max(1);
            LineColor = foreground;
            BackgroundColor = background ?? (0, 0, 0, 1);
        }

        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            RGBAColor fg = LineColor;
            RGBAColor bg = BackgroundColor;
            int rw = region.Width;
            int rh = region.Height;
            int rx = region.X;
            int ry = region.Y;
            int w = bmp.Width;
            Scalar s = GridSpacing * 2;

            Parallel.For(0, rw * rh, i =>
            {
                int x = i % rw;
                int y = i / rw;

                destination[(y + ry) * w + x + rx] = (int)(x / s) % 2 == (int)(y / s) % 2 ? fg : bg;
            });
        }
    }

    public sealed class FractalGenerator
        : PartialBitmapEffect.Accelerated
    {
        public delegate Complex IteratorDelegate(Complex z, Complex c);


        public static IteratorDelegate Mandelbrot { get; } = (z, c) => z * z + c;

        public static IteratorDelegate Juliaset { get; } = (z, c) => z.Power(3) + c;

        public (Scalar epsilon, Scalar max) Bounds { get; set; } = (1e-4, 4);
        public Vector2 Offset { set; get; } = (-1.2, .1);
        public Scalar Scale { set; get; } = .8;
        public int MaxIterationCount { get; set; } = 100;
        public IteratorDelegate Iterator { get; }
        public ColorMap ColorMap { get; }


        public FractalGenerator(IteratorDelegate iterator, ColorMap colormap)
        {
            Iterator = iterator;
            ColorMap = colormap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            int w = bmp.Width;
            int max_iter = MaxIterationCount;
            Scalar sw = region.Width / 2d;
            Scalar s = Scale;
            Complex offs = Offset;
            Scalar ε = Bounds.epsilon;
            Scalar max = Bounds.max;
            ColorMap map = ColorMap;

            Parallel.For(0, indices.Length, idx =>
            {
                idx = indices[idx];

                int count = 0;
                Complex z = 0;
                Complex c = (
                    (idx % w) / sw - 1,
                    (idx / w) / sw - 1
                ) + offs;
                Scalar l;

                c /= s;

                do
                {
                    ++count;
                    z = Iterator(z, c);
                    l = z.Length;

                    if (l <= ε)
                        count = max_iter;
                }
                while ((count < max_iter) && (l < max));

                destination[idx] = map[count / (Scalar)max_iter];
            });
        }
    }

    public class LinearGradient
        : CoordinateColorEffect
    {
        public (Vector2 Position, RGBAColor Color) From { get; }
        public (Vector2 Position, RGBAColor Color) To { get; }


        public LinearGradient((Vector2 Position, RGBAColor Color) from, (Vector2 Position, RGBAColor Color) to)
        {
            From = from;
            To = to;
        }

        protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor _)
        {
            Vector2 dir = To.Position.Subtract(From.Position);
            Scalar dot = new Vector2(x, y).Subtract(From.Position) * dir.Normalized;

            return RGBAColor.LinearInterpolate(From.Color, To.Color, dot / dir.Length);
        }
    }

    public sealed class HyperbolicGradient
        : CoordinateColorEffect
    {
        public (Vector2 Position, RGBAColor Color) From { get; }
        public (Vector2 Position, RGBAColor Color) To { get; }


        public HyperbolicGradient((Vector2 Position, RGBAColor Color) from, (Vector2 Position, RGBAColor Color) to)
        {
            From = from;
            To = to;
        }

        protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor _)
        {
            Scalar f = From.Position.Subtract((x, y)).Length;
            Scalar t = To.Position.Subtract((x, y)).Length;

            return RGBAColor.LinearInterpolate(From.Color, To.Color, f / (f + t));
        }
    }

    public class ThreePointLinearGradient
        : LinearGradient
    {
        public (Vector2 Position, RGBAColor Color) P1 { get; }
        public (Vector2 Position, RGBAColor Color) P2 { get; }
        public (Vector2 Position, RGBAColor Color) P3 { get; }


        public ThreePointLinearGradient((Vector2 Position, RGBAColor Color) p1, (Vector2 Position, RGBAColor Color) p2, (Vector2 Position, RGBAColor Color) p3)
            : base(p1, p2)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor _)
        {
            Vector2 v0 = P2.Position - P1.Position;
            Vector2 v1 = P3.Position - P1.Position;
            Vector2 v2 = (x, y) - P1.Position;
            Scalar d00 = v0 * v0;
            Scalar d01 = v0 * v1;
            Scalar d11 = v1 * v1;
            Scalar d20 = v2 * v0;
            Scalar d21 = v2 * v1;
            Scalar denom = d00 * d11 - d01 * d01;
            Scalar _v = (d11 * d20 - d01 * d21) / denom;
            Scalar _w = (d00 * d21 - d01 * d20) / denom;
            Scalar _u = 1 - _v - _w;

            return _u * (Vector4)P1.Color
                 + _v * (Vector4)P2.Color
                 + _w * (Vector4)P3.Color;
        }
    }

    public sealed class MultiPointGradient
        : CoordinateColorEffect
    {
        public (Vector2 Position, RGBAColor Color)[] Points { get; }
        public Scalar PowerParameter { set; get; } = 1;


        public MultiPointGradient(params (Vector2 Position, RGBAColor Color)[] points) => Points = points;

        protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor _)
        {
            Vector2 pixel = (x, y);
            Vector4 num = Vector4.Zero;
            Scalar den = Scalar.Zero;

            for (int i = 0; i < Points.Length; ++i)
            {
                (Vector2 pos, RGBAColor col) = Points[i];

                if (pos.Is(pixel))
                    return col;

                Scalar f = pos.DistanceTo(pixel).Power(-PowerParameter);

                num += f * (Vector4)col;
                den += f;
            }

            return num / den;
        }
    }

    public sealed class ColorKey
        : ColorEffect.Delegated
    {
        public ColorKey(RGBAColor key, Scalar tolerance)
            : base(new Func<Func<RGBAColor, RGBAColor>>(() =>
            {
                tolerance = tolerance.Clamp();

                return c =>
                {
                    Scalar α = ((Vector3)c - key).Length - tolerance;
                    Scalar s = 1 - (20 * α).Exp().Increment().Inverse;

                    return new Vector4(1, 1, 1, s).ComponentwiseMultiply(c);
                };
            })())
        {
        }
    }

    // static noise generator
    // perlin noise generator
}

/*

    /// <summary>
    /// Represents the Instagram smooth 'Walden' CSS bitmap effect
    /// </summary>
    public sealed unsafe class SmoothWaldenBitmapEffect
        : InstagramEffect
    {
        public override Bitmap ApplyTo(Bitmap bmp) => bmp.ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.1)
                                                        .ApplyEffectRange<TintBitmapEffect>(Range, -PI)
                                                        .ApplyEffectRange<SepiaBitmapEffect>(Range, .3)
                                                        .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.6)
                                                        .ApplyEffectRange<ScreenColorBlendEffect>(Range, 0, .075, .225) // (0,¼,¾) * .3
                                                        .Average(bmp, .3);
        // -webkit-filter: brightness(1.1) hue-rotate(-10deg) sepia(.3) saturate(1.6);
        // screen #04c .3
    }


    /// <summary>
    /// Represents a gaussian blur bitmap effect
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public sealed class GaussianBlurBitmapEffect
        : SingleMatrixConvolutionBitmapEffect
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public GaussianBlurBitmapEffect()
            : this(5)
        {
        }

        /// <summary>
        /// Creates a new instance with the given blur radius
        /// </summary>
        /// <param name="radius">Blur radius</param>
        public GaussianBlurBitmapEffect(double radius)
            : base(null, 1, 0, false)
        {
            int size = ((int)radius * 2) + 1;
            int r = size / 2;

            double[,] matrix = new double[size, size];
            double w = Pow(radius, 4);
            double φ = 1.0 / (PI * w);
            double s = 0;
            double d = 0;

            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    d = ((x * x) + (y * y)) / w * 16;

                    s += matrix[y + r, x + r] = φ * Exp(-d);
                }

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    matrix[y, x] *= 1.0 / s;

            this.Matrix = matrix;
        }
    }

    #endregion
    #region OTHER EFFECTS

        public override Bitmap ApplyTo(Bitmap bmp)
        {
            if (Grayscale)
                bmp = new GrayscaleBitmapEffect().Apply(bmp);

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

            // this are the sobel-matrices written in one line
            double[] hmat = new double[9] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            double[] vmat = new double[9] { 1, 2, 1, 0, 0, 0, -1, -2, -1 };
            int w = src.DAT.Width, h = src.DAT.Height, s = src.DAT.Stride, l = s * h, psz = s / w;

            if (Filter == NormalFilter.Scharr)
                // this are the scharr-matrices written in one line
                (hmat, vmat) = (new double[9] { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
                                new double[9] { 1, 1, 1, 0, 0, 0, -1, -1, -1 });
            else if (Filter == NormalFilter.Prewitt)
                // this are the prewitt-matrices written in one line
                (hmat, vmat) = (new double[9] { 3, 10, 3, 0, 0, 0, -3, -10, -3 },
                                new double[9] { 3, 0, -3, 10, 0, -10, 3, 0, -3 });

            double sx, sy, sz, sum;
            int so, to, nx, ox, oy, fx, fy;

            byte* sptr = (byte*)src.DAT.Scan0;

            fixed (double* vptr = vmat)
            fixed (double* hptr = hmat)
            fixed (byte* dptr = dst.ARR)
                for (oy = 0; oy < h; oy++)
                    for (ox = 0; ox < w; ox++)
                    {
                        to = (oy * s) + (ox * psz);
                        sx = sy = 128;

                        for (fy = -1; fy <= 1; fy++)
                            for (fx = -1; fx <= 1; fx++)
                            {
                                so = to + (fx * psz) + (fy * s);

                                if (so < 0 || so >= l - 3)
                                    continue;

                                sum = (0.0 + sptr[so] + sptr[so + 1] + sptr[so + 2]) / 3.0;
                                nx = ((fy + 1) * 3) + fx + 1;

                                sx += hptr[nx] * sum;
                                sy += vptr[nx] * sum;
                            }

                        sz = ((Abs(sx - 128.0) + Abs(sy - 128.0)) / 4.0);

                        dptr[to + 0] = (byte)(sz > 64 ? 191 : sz < 0 ? 255 : 255 - sz);
                        dptr[to + 1] = (byte)sy.Constrain(0, 255);
                        dptr[to + 2] = (byte)sx.Constrain(0, 255);

                        if (psz > 3)
                            dptr[to + 3] = 255;
                    }

            src.Unlock();

            bmp = dst.Unlock();

            if ((BlurRadius < 0 ? 0 : BlurRadius) > 0)
                bmp = bmp.ApplyEffect<FastBlurBitmapEffect>(BlurRadius)
                            .ApplyEffect<FastBlurBitmapEffect>(BlurRadius / 2);

            return bmp;
        }

        /// <summary>
        /// Creates a new instance with the internal sobel-filter
        /// </summary>
        public NormalMapBitmapEffect()
            : this(false, 0, NormalFilter.Sobel)
        {
        }

        /// <summary>
        /// Creates a new instance with the internal sobel-filter
        /// </summary>
        /// <param name="grayscale">Determines whether the effect should grayscale the image before processing</param>
        public NormalMapBitmapEffect(bool grayscale)
            : this(grayscale, 0, NormalFilter.Sobel)
        {
        }

        /// <summary>
        /// Creates a new instance with the internal sobel-filter and a pre-blurring technique
        /// </summary>
        /// <param name="grayscale">Determines whether the effect should grayscale the image before processing</param>
        /// <param name="radius">The pre-blur radius</param>
        public NormalMapBitmapEffect(bool grayscale, double radius)
            : this(grayscale, radius, NormalFilter.Sobel)
        {
        }

        /// <summary>
        /// Creates a new instance with the internal given filter and a pre-blurring technique
        /// </summary>
        /// <param name="grayscale">Determines whether the effect should grayscale the image before processing</param>
        /// <param name="radius">The pre-blur radius</param>
        /// <param name="filter">Edge detection filter</param>
        public NormalMapBitmapEffect(bool grayscale, double radius, NormalFilter filter)
        {
            this.Filter = filter;
            this.BlurRadius = radius;
            this.Grayscale = grayscale;
        }
    }

    /// <summary>
    /// Represents the RGB-split bitmap effect
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe sealed class RGBSplitBitmapEffect
        : PartialBitmapEffect
    {
        /// <summary>
        /// RGB-split direction [0...2π] (0 points to the right, clockwise)
        /// </summary>
        public double Direction { internal set; get; }
        /// <summary>
        /// RGB-split amount (in pixel)
        /// </summary>
        public double Amount { internal set; get; }

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override Bitmap ApplyTo(Bitmap bmp)
        {
            Bitmap bmp1 = bmp.ToARGB32().ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                { 1,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,1,1 },
                { 1,1,1,1,1 },
            });
            Bitmap bmp2 = bmp.ToARGB32().ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,1,1 },
                { 1,1,1,1,1 },
            });
            Bitmap tmp = bmp1.ApplyBlendEffect<DifferenceBitmapBlendEffect>(bmp2); // bmp1.DifferenceMask(bmp2, .05, true, true);

            // bmp2 = bmp2.DifferenceMask(bmp1, .05, true, true);
            bmp1 = tmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                { 1,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,1,1 },
                { 1,1,1,1,.4 },
            });
            bmp2 = tmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,1,1 },
                { 1,1,1,1,.4 },
            });

            double x = Amount * Cos(Direction);
            double y = -Amount * Sin(Direction);

            ////////////////////////////////////////////////////////// TODO //////////////////////////////////////////////////////////

            tmp = bmp1.Overlay(bmp2);
            bmp = bmp.DifferenceMask(tmp, .05, false, true, true);
            bmp = bmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                { 1,0,0,0,0 },
                { 0,1,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,1,0 },
                { 1,1,1,1,0 },
            });

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp1, (float)x, (float)y, bmp.Width, bmp.Height);
                g.DrawImage(bmp2, -(float)x, -(float)y, bmp.Width, bmp.Height);
            }

            return bmp;
        }

        [Obsolete("Use `Apply` instead.", true)]
        internal Bitmap __OLD__Apply(Bitmap bmp)
        {
            Func<double, double, double, double> MatrixValue = new Func<double, double, double, double>((x, y, θ) =>
            {
                double fac = 2 * Sqrt(2);
                double τ = y - (Tan(θ) * x);
                double nx = τ - x;
                double ny = (τ * Tan(θ)) - y;
                double δ = Sqrt((nx * nx) + (ny * ny));
                double σ = Max(0, fac - δ) / fac;

                if (Abs(x) < 1 && Abs(y) < 1)
                    return 1;
                else if (x == 0)
                    return y < 0 ? -σ : σ;
                else
                    return x < 0 ? -σ : σ;
            });

            int amnt = (int)Amount;
            int size = 1 + (amnt * 2);
            double[,] tmatr = new double[size, size];
            double[,] omatr = new double[size, size];
            double sum = 0;

            for (int x = -amnt; x <= amnt; x++)
                for (int y = -amnt; y <= amnt; y++)
                    sum += tmatr[amnt + x, amnt + y] = MatrixValue(x + .5, y + .5, Direction);

            Console.WriteLine(sum);
            Console.WriteLine();

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                    Console.Write(tmatr[x, y].ToString("0.00").PadLeft(5, ' ') + "  ");

                Console.WriteLine();
            }

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    omatr[size - 1 - x, size - 1 - y] = tmatr[x, y];

            Bitmap bmp1 = bmp.ApplyEffect(new SingleMatrixConvolutionColorBitmapEffect(tmatr, new double[5, 5] {
                { 1,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,0,1,0 },
                { 1,1,1,1,0 },
            }, 1.0 / sum, 0, false));
            Bitmap bmp2 = bmp.ApplyEffect(new SingleMatrixConvolutionColorBitmapEffect(omatr, new double[5, 5] {
                { 0,0,0,0,0 },
                { 0,0,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,1,0 },
                { 1,1,1,1,0 },
            }, 1.0 / sum, 0, false));

            return bmp1.Merge(bmp2);
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public RGBSplitBitmapEffect()
            : this(1, 0)
        {
        }

        /// <summary>
        /// Creates a new instance with the given amount
        /// </summary>
        /// <param name="amount">RGB-split amount (in pixel)</param>
        public RGBSplitBitmapEffect(double amount)
            : this(amount, 0)
        {
        }

        /// <summary>
        /// Creates a new instance with the given amount and direction
        /// </summary>
        /// <param name="amount">RGB-split amount (in pixel)</param>
        /// <param name="direction">RGB-split direction [0...2π] (0 points to the right, clockwise)</param>
        public RGBSplitBitmapEffect(double amount, double direction)
        {
            const double π2 = PI * 2;

            this.Amount = amount < 0 ? 0 : amount;
            this.Direction = (direction + π2) % π2;
        }
    }

    [Serializable, DebuggerStepThrough, DebuggerNonUserCode, Obsolete("Use `CoreLib::Imaging::RGBSplitBitmapEffect` instead.", true)]
    internal unsafe sealed class __OLD__RGBSplitBitmapEffect
        : PartialBitmapEffect
    {
        public double Delta { internal set; get; }
        public double Theta { internal set; get; }

        public unsafe override Bitmap ApplyTo(Bitmap bmp)
        {
            using (BitmapLockInfo nfo = bmp.LockBitmap())
            {
                double δ = Delta;
                double θ = Theta;
                double xo = δ * Sin(θ);
                double yo = δ * Cos(θ + PI);

                byte[] dup = new byte[nfo.ARR.Length];

                Array.Copy(nfo.ARR, dup, nfo.ARR.Length);

                int w = nfo.BMP.Width;
                int h = nfo.BMP.Height;

                fixed (byte* src = dup)
                fixed (byte* tar = nfo.ARR)
                    for (int y = 0, o; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            o = (x + (y * w)) * 4;

                            int r = src[o + 1];
                            int b = src[o + 3];

                            int xs = (int)Max(x - xo, 0);
                            int ys = (int)Max(y - yo, 0);

                            o = (xs + (ys * w)) * 4;

                            r += (int)(src[o + 1] * .5);

                            xs = (int)Min(xs + (2 * xo), w);
                            ys = (int)Min(ys + (2 * yo), h);

                            o = (xs + (ys * w)) * 4;

                            b += (int)(src[o + 3] * .5);

                            o = (x + (y * w)) * 4;

                            tar[o + 1] = (byte)Min(255, r);
                            tar[o + 3] = (byte)Min(255, b);
                        }

                return nfo.Unlock();
            }
        }

        public __OLD__RGBSplitBitmapEffect()
            : this(5, 0)
        {
        }

        public __OLD__RGBSplitBitmapEffect(double δ, double θ)
        {
            this.Delta = δ;
            this.Theta = θ;
        }
    }

    #endregion
    */
