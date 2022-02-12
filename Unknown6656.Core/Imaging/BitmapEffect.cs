using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Imaging
{
    /// <summary>
    /// Represents an abstract bitmap effect.
    /// </summary>
    public abstract class BitmapEffect
    {
        public abstract Bitmap ApplyTo(Bitmap bmp);
    }

    /// <summary>
    /// Represents an abstract bitmap effect which can be partially applied by controlling the effect region and intensity.
    /// </summary>
    public abstract unsafe class PartialBitmapEffect
        : BitmapEffect
    {
        protected int[] GetIndices(Bitmap bmp, Rectangle region)
        {
            int w = bmp.Width;

            region.Intersect(new Rectangle(0, 0, w, bmp.Height));

            int rw = region.Width;
            int x = region.Left;
            int y = region.Top;
            int[] indices = new int[rw * region.Height];

            Parallel.For(0, indices.Length, i => indices[i] = (i / rw + y) * w + (i % rw) + x);

            return indices;
        }

        protected (int X, int Y) GetRelativeCoordinates(int index, int width, Rectangle region) => ((index % width) - region.Left, (index / width) - region.Top);

        protected (int X, int Y) GetAbsoluteCoordinates(int index, int width) => (index % width, index / width);

        public abstract Bitmap ApplyTo(Bitmap bmp, Rectangle region);

        public Bitmap ApplyTo(Bitmap bmp, Rectangle region, Scalar intensity)
        {
            using Bitmap fx = ApplyTo(bmp, region);
            Bitmap res = new Bitmap(bmp.Width, bmp.Height);
            BitmapLocker l_src = bmp;
            BitmapLocker l_fx = fx;
            BitmapLocker l_dst = res;

            l_src.LockRGBAPixels((ps, ws, hs) => l_fx.LockRGBAPixels((px, wx, hx) => l_dst.LockRGBAPixels((pd, wd, hd) =>
                Parallel.For(0, ws * hs, i => pd[i] = RGBAColor.LinearInterpolate(ps[i], px[i], intensity))
            )));

            return res;
        }

        public Bitmap ApplyTo(Bitmap bmp, Scalar intensity) => ApplyTo(bmp, (.., ..), intensity);

        public override Bitmap ApplyTo(Bitmap bmp) => ApplyTo(bmp, (.., ..), Scalar.One);

        public Bitmap ApplyTo(Bitmap bmp, (Range Horizontal, Range Vertical) region, Scalar intensity)
        {
            int hs = region.Horizontal.Start.GetOffset(bmp.Width);
            int he = region.Horizontal.End.GetOffset(bmp.Width);
            int vs = region.Vertical.Start.GetOffset(bmp.Height);
            int ve = region.Vertical.End.GetOffset(bmp.Height);
            Rectangle rect = new Rectangle(hs, vs, he - hs, ve - vs);

            return ApplyTo(bmp, rect, intensity);
        }

        public Bitmap ApplyTo(Bitmap bmp, (Range Horizontal, Range Vertical) region) => ApplyTo(bmp, region, Scalar.One);

        public Bitmap ApplyTo(Bitmap bmp, BitmapMask mask)
        {
            Bitmap fx = ApplyTo(bmp);

            fx = mask.ApplyTo(fx);

            return new BlendEffect(bmp, BlendMode.Overlay, 1).ApplyTo(fx);
        }

        private protected static int minmax(int v, int min, int max) => v < min ? min : v > max ? max : v;


        public abstract unsafe class Accelerated
            : PartialBitmapEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap inside the given range and returns the result.
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <param name="region">Range, in which the effect should be applied.</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap ApplyTo(Bitmap bmp, Rectangle region)
            {
                if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                    bmp = bmp.ToARGB32();

                Bitmap result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                BitmapLocker src = bmp;
                BitmapLocker dst = result;

                src.LockRGBAPixels((ps, ws, hs) => dst.LockRGBAPixels((pd, wd, hd) =>
                {
                    Process(bmp, ps, pd, region);

                    Parallel.For(0, ws * hs, i =>
                    {
                        if (!region.Contains(i % ws, i / ws))
                            pd[i] = ps[i];
                    });
                }));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal protected abstract void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region);

            private protected int? GetIndex(int x, int y, int w, int h, Rectangle region, EdgeHandlingMode mode)
            {
                int rx = region.X;
                int ry = region.Y;
                int rw = region.Width;
                int rh = region.Height;

                if (x < rx || y < ry || x >= rx + rw || y >= ry + rh)
                    switch (mode)
                    {
                        case EdgeHandlingMode.Black:
                            return null;
                        case EdgeHandlingMode.Extend:
                            x = minmax(x, rx, rx + rw - 1);
                            y = minmax(y, ry, ry + rh - 1);

                            break;
                        case EdgeHandlingMode.Wrap:
                            x = rx + ((x - rx) % rw + rw) % rw;
                            y = ry + ((y - ry) % rh + rh) % rh;

                            break;
                        case EdgeHandlingMode.Mirror:
                            x = ((x - rx) % (2 * rw) + 2 * rw) % (2 * rw);
                            y = ((y - ry) % (2 * rh) + 2 * rh) % (2 * rh);

                            if (x >= rw)
                                x = 2 * rw - x;

                            if (y >= rh)
                                y = 2 * rh - y;

                            x += rx;
                            y += ry;

                            break;
                    }

                return y * w + x;
            }
        }
    }

    public interface IChainedEffects<T, F>
        where T : F, IChainedEffects<T, F>
        where F : BitmapEffect
    {
        IReadOnlyCollection<F> Effects { get; }
    }

    public class ChainedBitmapEffect
        : BitmapEffect
        , IChainedEffects<ChainedBitmapEffect, BitmapEffect>
    {
        private readonly BitmapEffect[] _effects;

        public IReadOnlyCollection<BitmapEffect> Effects => _effects;


        public ChainedBitmapEffect(params BitmapEffect[] effects) => _effects = effects;

        public ChainedBitmapEffect(IEnumerable<BitmapEffect> effects)
            : this(effects.ToArray())
        {
        }

        public override Bitmap ApplyTo(Bitmap bmp) => _effects.Length == 0 ? bmp : _effects.Aggregate(bmp, (b, fx) => fx.ApplyTo(b));
    }

    public class ChainedPartialBitmapEffect
        : PartialBitmapEffect
        , IChainedEffects<ChainedPartialBitmapEffect, PartialBitmapEffect>
    {
        private readonly PartialBitmapEffect[] _effects;

        public IReadOnlyCollection<PartialBitmapEffect> Effects => _effects;


        public ChainedPartialBitmapEffect(params PartialBitmapEffect[] effects) => _effects = effects;

        public ChainedPartialBitmapEffect(IEnumerable<PartialBitmapEffect> effects)
            : this(effects.ToArray())
        {
        }

        public override Bitmap ApplyTo(Bitmap bmp, Rectangle region) => _effects.Length == 0 ? bmp : _effects.Aggregate(bmp, (b, fx) => fx.ApplyTo(b, region));
    }

    public class AcceleratedChainedPartialBitmapEffect
        : PartialBitmapEffect.Accelerated
        , IChainedEffects<AcceleratedChainedPartialBitmapEffect, PartialBitmapEffect.Accelerated>
    {
        private readonly Accelerated[] _effects;

        public IReadOnlyCollection<Accelerated> Effects => _effects;


        public AcceleratedChainedPartialBitmapEffect(params Accelerated[] effects) => _effects = effects;

        public AcceleratedChainedPartialBitmapEffect(IEnumerable<Accelerated> effects)
            : this(effects.ToArray())
        {
        }

        internal protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            if (_effects.Length == 0)
            {
                Parallel.For(0, bmp.Width * bmp.Height, i => destination[i] = source[i]);

                return;
            }
            else if (_effects.Length == 1)
            {
                _effects[0].Process(bmp, source, destination, region);

                return;
            }

            RGBAColor[] tmp = new RGBAColor[bmp.Width * bmp.Height];

            if ((_effects.Length % 2) == 0)
                Parallel.For(0, tmp.Length, i => destination[i] = source[i]);
            else
                Parallel.For(0, tmp.Length, i => tmp[i] = source[i]);

            fixed (RGBAColor* ptmp = tmp)
                for (int i = 0, l = _effects.Length; i < l; ++i)
                    if ((l - i) % 2 == 0)
                        _effects[i].Process(bmp, destination, ptmp, region);
                    else
                        _effects[i].Process(bmp, ptmp, destination, region);
        }
    }

    public abstract class ColorEffect
        : PartialBitmapEffect.Accelerated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract RGBAColor ProcessColor(RGBAColor input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected override sealed unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);

            Parallel.For(0, indices.Length, i =>
            {
                int idx = indices[i];

                destination[idx] = ProcessColor(source[idx]);
            });
        }

        public static Delegated FromDelegate(Func<RGBAColor, RGBAColor> @delegate) => FromDelegate(@delegate);


        public class Delegated
            : ColorEffect
        {
            public Func<RGBAColor, RGBAColor> Delegate { get; }


            public Delegated(Func<RGBAColor, RGBAColor> @delegate) => Delegate = @delegate;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override RGBAColor ProcessColor(RGBAColor input) => Delegate(input);
        }
    }

    public abstract class CoordinateColorEffect
        : PartialBitmapEffect.Accelerated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal override sealed unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            int[] indices = GetIndices(bmp, region);
            int w = bmp.Width;
            int h = bmp.Height;

            Parallel.For(0, indices.Length, i =>
            {
                int idx = indices[i];

                destination[idx] = ProcessCoordinate(idx % w, idx / w, w, h, source[idx]);
            });
        }

        public static CoordinateColorEffect FromDelegate(Func<int, int, int, int, RGBAColor, RGBAColor> func) => new __del(func);


        private sealed class __del
            : CoordinateColorEffect
        {
            private readonly Func<int, int, int, int, RGBAColor, RGBAColor> _func;


            public __del(Func<int, int, int, int, RGBAColor, RGBAColor> func) => _func = func;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source) => _func(x, y, w, h, source);
        }

        /// <summary>
        /// (x, y) in [-1..1]²
        /// </summary>
        public abstract class Relative
            : CoordinateColorEffect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected abstract RGBAColor ProcessCoordinate(Vector2 coord, Scalar aspect_ratio, RGBAColor source);

            protected sealed override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source) => ProcessCoordinate(new Vector2((2d * x / w) - 1, (2d * y / h) - 1), (Scalar)w / h, source);

            public static Relative FromDelegate(Func<Vector2, RGBAColor> func) => new __del((xy, r, c) => func(xy));

            public static Relative FromDelegate(Func<Vector2, Scalar, RGBAColor> func) => new __del((xy, r, _) => func(xy, r));

            public static Relative FromDelegate(Func<Vector2, RGBAColor, RGBAColor> func) => new __del((xy, _, c) => func(xy, c));

            public static Relative FromDelegate(Func<Vector2, Scalar, RGBAColor, RGBAColor> func) => new __del(func);


            private new sealed class __del
                : Relative
            {
                private readonly Func<Vector2, Scalar, RGBAColor, RGBAColor> _func;


                public __del(Func<Vector2, Scalar, RGBAColor, RGBAColor> func) => _func = func;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected override RGBAColor ProcessCoordinate(Vector2 coord, Scalar aspect_ratio, RGBAColor source) => _func(coord, aspect_ratio, source);
            }
        }
    }

    /// <summary>
    /// Represents an abstract bitmap effect wich applies a stored color matrix to each of the bitmap's pixel
    /// </summary>
    public class RGBAColorEffect
        : ColorEffect
    {
        /// <summary>
        /// Color matrix to be applied to the bitmap
        /// </summary>
        public Matrix4 ColorMatrix { get; }
        public Vector4 ColorBias { get; }


        public RGBAColorEffect(Matrix4 matrix)
            : this(matrix, Vector4.Zero)
        {
        }

        public RGBAColorEffect(Matrix4 matrix, Vector4 bias)
        {
            ColorMatrix = matrix;
            ColorBias = bias;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override RGBAColor ProcessColor(RGBAColor input) => (ColorMatrix * input) + ColorBias;
    }

    public class RGBColorEffect
        : ColorEffect
    {
        /// <summary>
        /// Color matrix to be applied to the bitmap
        /// </summary>
        public Matrix3 ColorMatrix { get; }
        public Vector3 ColorBias { get; }


        public RGBColorEffect(Matrix3 matrix)
            : this(matrix, Vector3.Zero)
        {
        }

        public RGBColorEffect(Matrix3 matrix, Vector3 bias)
        {
            ColorMatrix = matrix;
            ColorBias = bias;
        }

        private protected RGBColorEffect((Matrix3 matrix, Vector3 bias) t)
            : this(t.matrix, t.bias)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override RGBAColor ProcessColor(RGBAColor input) => (ColorMatrix * input) + ColorBias;
    }

    /// <summary>
    /// Represents an abstract bitmap effect wich applies a stored transformation matrix to each of the bitmap's pixel
    /// </summary>
    public class TransformEffect
        : PartialBitmapEffect.Accelerated
    {
        /// <summary>
        /// Transformation matrix to be applied to the bitmap
        /// </summary>
        public Matrix2 LinearTransformMatrix { get; }

        public Vector2 TranslationOffset { get; }

        public Matrix3 AffineTransformationMatrix { get; }

        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

        public PixelInterpolationMode Interpolation { set; get; } = PixelInterpolationMode.BilinearInterpolation;


        public TransformEffect(Matrix2 matrix)
            : this(matrix, default)
        {
        }

        public TransformEffect(Matrix2 matrix, Vector2 offset)
        {
            var (a, b, c, d) = matrix;

            TranslationOffset = offset;
            LinearTransformMatrix = matrix;
            AffineTransformationMatrix = (
                a, b, offset[0],
                c, d, offset[1],
                0, 0, 1
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            bool invertible = LinearTransformMatrix.Determinant.IsInvertible;
            Matrix2 m = invertible ? LinearTransformMatrix.Inverse : LinearTransformMatrix;
            Vector2 offset = TranslationOffset;
            int[] indices = GetIndices(bmp, region);
            int rw = region.Width;
            int rh = region.Height;
            int rx = region.X;
            int ry = region.Y;
            int w = bmp.Width;
            int h = bmp.Height;
            RGBAColor get_pixel(int x, int y)
            {
                if (EdgeHandling == EdgeHandlingMode.Extend)
                    return source[minmax(y, 0, h - 1) * w + minmax(x, 0, w - 1)];
                else if (GetIndex(x, y, w, h, region, EdgeHandling) is int i)
                    return source[i];

                return 0;
            }

            Parallel.For(0, rw * rh, i =>
            {
                int ix = i % rw;
                int iy = i / rw;
                Vector2 coord = m * (
                    (ix * 2d / rw) - 1,
                    (iy * 2d / rh) - 1
                );

                Scalar ox = (coord.X + 1) / 2 * rw + rx;
                Scalar oy = (coord.Y + 1) / 2 * rh + ry;

                if (invertible)
                {
                    ox -= offset.X;
                    oy -= offset.Y;
                }
                else
                {
                    ox += offset.X;
                    oy += offset.Y;
                }

                iy += ry;
                ix += rx;

                if (invertible)
                    if (ox.IsInteger && oy.IsInteger)
                        destination[iy * w + ix] = get_pixel((int)ox, (int)oy);
                    else if (Interpolation == PixelInterpolationMode.NearestNeighbour)
                        destination[iy * w + ix] = get_pixel((int)ox.Rounded, (int)oy.Rounded);
                    else
                    {
                        Vector4 tl = get_pixel((int)ox, (int)oy);
                        Vector4 tr = get_pixel((int)ox.Increment(), (int)oy);
                        Vector4 bl = get_pixel((int)ox, (int)oy.Increment());
                        Vector4 br = get_pixel((int)ox.Increment(), (int)oy.Increment());
                        Scalar dx = ox.DecimalPlaces;
                        Scalar dy = oy.DecimalPlaces;
                        Vector4 col = Vector4.LinearInterpolate(
                            Vector4.LinearInterpolate(in tl, in tr, dx),
                            Vector4.LinearInterpolate(in bl, in br, dx),
                            dy
                        );

                        destination[iy * w + ix] = col;
                    }
                else
                    destination[(int)oy * w + (int)ox] = get_pixel(ix, iy);
            });
        }
    }

    public class BlendEffect
        : PartialBitmapEffect.Accelerated
    {
        public Bitmap BaseLayer { get; }
        public BlendMode Mode { get; }
        public double Amount { get; }


        public BlendEffect(Bitmap base_layer, BlendMode mode, double amount)
        {
            BaseLayer = base_layer;
            Amount = amount;
            Mode = mode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected override unsafe void Process(Bitmap bmp, RGBAColor* p_top, RGBAColor* p_dest, Rectangle region)
        {
            if (bmp.Size != BaseLayer.Size)
                throw new ArgumentException($"The top layer ('bmp') must have the same dimensions as the base layer. The required dimensions are: {BaseLayer.Width}x{BaseLayer.Height}.", nameof(bmp));

            int[] indices = GetIndices(BaseLayer, region);
            BitmapLocker lck = BaseLayer;
            BlendMode mode = Mode;

            lck.LockRGBAPixels((p_base, w, h) => Parallel.For(0, indices.Length, i =>
            {
                int idx = indices[i];

                p_dest[idx] = RGBAColor.Blend(p_base[idx], p_top[idx], mode);
            }));
        }
    }

    public class MultiConvolutionEffect
        : PartialBitmapEffect.Accelerated
    {
        public MatrixNM[] ConvolutionKernel { get; }

        public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

        public bool ConvoluteAlpha { set; get; }


        public MultiConvolutionEffect(params MatrixNM[] kernels)
        {
            if (kernels.Length == 0)
                throw new ArgumentException("The kernel array must not be empty.", nameof(kernels));

            foreach (MatrixNM k in kernels)
                if (!(k.Size is (int w, int h) && (w % 2) == 1 && (h % 2) == 1))
                    throw new ArgumentException("All kernel dimensions must be odd.", nameof(kernels));

            ConvolutionKernel = kernels;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            (int cols, int rows, Scalar[,] vals)[] kernels = ConvolutionKernel.Select(k => (k.ColumnCount, k.RowCount, k.Coefficients)).ToArray();
            int kcount = kernels.Length;
            int w = bmp.Width;
            int h = bmp.Height;
            bool ca = ConvoluteAlpha;
            int[] indices = GetIndices(bmp, region);

            Parallel.For(0, indices.Length, j =>
            {
                int y = indices[j] / w;
                int x = indices[j] % w;
                Vector4[] colors = new Vector4[kcount];

                for (int i = 0; i < kcount; ++i)
                {
                    int kw = kernels[i].cols / 2;
                    int kh = kernels[i].rows / 2;

                    for (int sy = -kh; sy <= kh; ++sy)
                        for (int sx = -kw; sx <= kw; ++sx)
                            if (GetIndex(x + sx, y + sy, w, h, region, EdgeHandling) is int idx)
                                colors[i] += kernels[i].vals[sx + kw, sy + kh] * (Vector4)source[idx];
                }

                Vector4 col = Vector4.Zero;

                foreach ((Scalar a, Scalar r, Scalar g, Scalar b) in colors)
                    col += (a * a, b * b, g * g, b * b);

                col = col.ComponentwiseSqrt();

                destination[y * w + x] = ca ? (RGBAColor)col : (RGBAColor)col.XYZ;
            });
        }
    }

    public class SingleConvolutionEffect
        : MultiConvolutionEffect
    {
        public new MatrixNM ConvolutionKernel { get; }


        public SingleConvolutionEffect(MatrixNM kernel)
            : base(kernel) => ConvolutionKernel = kernel;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
        {
            Scalar[,] matrix = ConvolutionKernel;
            bool ca = ConvoluteAlpha;
            int kw = ConvolutionKernel.ColumnCount / 2;
            int kh = ConvolutionKernel.RowCount / 2;
            int w = bmp.Width;
            int h = bmp.Height;
            int[] indices = GetIndices(bmp, region);

            Parallel.For(0, indices.Length, i =>
            {
                int y = indices[i] / w;
                int x = indices[i] % w;
                Vector4 c = Vector4.Zero;

                for (int sy = -kh; sy <= kh; ++sy)
                    for (int sx = -kw; sx <= kw; ++sx)
                        if (GetIndex(x + sx, y + sy, w, h, region, EdgeHandling) is int idx)
                            c += matrix[sx + kw, sy + kh] * (Vector4)source[idx];

                destination[y * w + x] = ca ? (RGBAColor)c : (RGBAColor)c.XYZ;
            });
        }
    }

    public enum PixelInterpolationMode
    {
        NearestNeighbour,
        BilinearInterpolation
    }

    public enum EdgeHandlingMode
    {
        Extend = 0,
        Wrap = 1,
        Mirror = 2,
        Black = 3,
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
        Min,
        Max,
    }
}