using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging.Effects;

namespace Unknown6656.Imaging;


public abstract class BitmapComputation<T>
{
    public abstract T Compute(Bitmap bitmap);
}

public abstract class PartialBitmapComputation<T>
    : BitmapComputation<T>
{
    public T Compute(Bitmap bmp, Rectangle region)
    {
        bmp = bmp.ApplyEffect(Crop.To(region));

        return Compute(bmp);
    }

    public T Compute(Bitmap bmp, (Range Horizontal, Range Vertical) region) => Compute(bmp, region.Horizontal, region.Vertical);

    public T Compute(Bitmap bmp, Range horizontal, Range vertical)
    {
        int hs = horizontal.Start.GetOffset(bmp.Width);
        int he = horizontal.End.GetOffset(bmp.Width);
        int vs = vertical.Start.GetOffset(bmp.Height);
        int ve = vertical.End.GetOffset(bmp.Height);
        Rectangle rect = new(hs, vs, he - hs, ve - vs);

        return Compute(bmp, rect);
    }
}

/// <summary>
/// Represents an abstract bitmap effect.
/// </summary>
public abstract class BitmapEffect
    //: BitmapComputation<Bitmap>
{
    public abstract Bitmap ApplyTo(Bitmap bmp);

    public virtual unsafe Bitmap ApplyTo(Bitmap bmp, Scalar intensity)
    {
        if (intensity.IsZero)
            return bmp;

        Bitmap output = ApplyTo(bmp);

        if (bmp != output && !intensity.IsOne)
        {
            bmp.LockRGBAPixels((ps, ws, hs) =>
            output.LockRGBAPixels((pd, _, _) =>
            {
                Parallel.For(0, ws * hs, i => pd[i] = RGBAColor.LinearInterpolate(ps[i], pd[i], intensity));
            }));
        }

        return output;
    }
}

internal unsafe delegate void ProcessFunc(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region);

/// <summary>
/// Represents an abstract bitmap effect which can be partially applied by controlling the effect region and intensity.
/// </summary>
public abstract unsafe class PartialBitmapEffect
    : BitmapEffect
{
    private protected abstract Bitmap Process(Bitmap bmp, Rectangle region);

    /// <summary>
    /// Applies the current effect to the given bitmap inside the given range and returns the result.
    /// </summary>
    /// <param name="bmp">Input bitmap</param>
    /// <param name="region">Range, in which the effect should be applied.</param>
    /// <returns>Output bitmap</returns>
    public Bitmap ApplyTo(Bitmap bmp, Rectangle region) => Process(bmp, Rectangle.Intersect(region, new(0, 0, bmp.Width, bmp.Height)));

    public Bitmap ApplyTo(Bitmap bmp, Rectangle region, Scalar intensity)
    {
        if (intensity.IsZero)
            return bmp;

        Bitmap fx = ApplyTo(bmp, region);

        if (!intensity.IsOne && bmp != fx)
        {
            BitmapLocker l_src = bmp;
            BitmapLocker l_dst = fx;

            l_src.LockRGBAPixels((ps, ws, hs) =>
            l_dst.LockRGBAPixels((pd, wd, hd) =>
            {
                Parallel.For(0, ws * hs, i => pd[i] = RGBAColor.LinearInterpolate(ps[i], pd[i], intensity));
            }));
        }

        return fx;
    }

    public override Bitmap ApplyTo(Bitmap bmp, Scalar intensity) => ApplyTo(bmp, (.., ..), intensity);

    public override Bitmap ApplyTo(Bitmap bmp) => ApplyTo(bmp, (.., ..), Scalar.One);

    public Bitmap ApplyTo(Bitmap bmp, (Range Horizontal, Range Vertical) region, Scalar intensity) => ApplyTo(bmp, region.Horizontal, region.Vertical, intensity);

    public Bitmap ApplyTo(Bitmap bmp, (Range Horizontal, Range Vertical) region) => ApplyTo(bmp, region, Scalar.One);

    public Bitmap ApplyTo(Bitmap bmp, Range horizontal, Range vertical) => ApplyTo(bmp, horizontal, vertical, Scalar.One);

    public Bitmap ApplyTo(Bitmap bmp, Range horizontal, Range vertical, Scalar intensity)
    {
        int hs = horizontal.Start.GetOffset(bmp.Width);
        int he = horizontal.End.GetOffset(bmp.Width);
        int vs = vertical.Start.GetOffset(bmp.Height);
        int ve = vertical.End.GetOffset(bmp.Height);
        Rectangle rect = new(hs, vs, he - hs, ve - vs);

        return ApplyTo(bmp, rect, intensity);
    }

    public Bitmap ApplyTo(Bitmap bmp, BitmapMask mask)
    {
        Bitmap fx = ApplyTo(bmp);

        fx = mask.ApplyTo(fx);

        return new BitmapBlend(bmp, BlendMode.Overlay, 1).ApplyTo(fx);
    }

    public static PartialBitmapEffect FromDelegate(Func<Bitmap, Rectangle, Bitmap> function) => new Delegated(function);

    private protected static int[] GetIndices(Bitmap bmp, Rectangle region)
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

    private protected static (int X, int Y)[] GetAbsoluteCoordinates(Bitmap bmp, Rectangle region)
    {
        int w = bmp.Width;
        int[] idx = GetIndices(bmp, region);
        (int x, int y)[] coords = new (int, int)[idx.Length];

        Parallel.For(0, idx.Length, i => coords[i] = GetAbsoluteCoordinates(idx[i], w));

        return coords;
    }

    private protected static (int X, int Y) GetRelativeCoordinates(int index, int width, Rectangle region) => ((index % width) - region.Left, (index / width) - region.Top);

    private protected static (int X, int Y) GetAbsoluteCoordinates(int index, int width) => (index % width, index / width);

    private protected static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

    private protected static int GetIndex(int x, int y, int image_width) => y * image_width + x;

    private protected static int? GetIndex(int x, int y, int image_width, Rectangle region, EdgeHandlingMode mode)
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
                    x = Clamp(x, rx, rx + rw - 1);
                    y = Clamp(y, ry, ry + rh - 1);

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

        return y * image_width + x;
    }


    public abstract unsafe class Accelerated
        : PartialBitmapEffect
    {
        private protected override Bitmap Process(Bitmap bmp, Rectangle region)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                bmp = bmp.ToARGB32();

            Bitmap result = new(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
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
    }

    public class Delegated
        : PartialBitmapEffect
    {
        public Func<Bitmap, Rectangle, Bitmap> Function { get; }


        public Delegated(Func<Bitmap, Rectangle, Bitmap> function) => Function = function;

        private protected override Bitmap Process(Bitmap bmp, Rectangle region) => Function(bmp, region);
    }
}

public class ChainedPartialBitmapEffect
    : PartialBitmapEffect
{
    public PartialBitmapEffect[] Effects { get; }


    public ChainedPartialBitmapEffect(IEnumerable<PartialBitmapEffect> effects)
        : this(effects.ToArray())
    {
    }

    public ChainedPartialBitmapEffect(params PartialBitmapEffect[] effects) => Effects = effects;

    private protected override Bitmap Process(Bitmap bmp, Rectangle region) => Effects.Length is 0 ? bmp : Effects.Aggregate(bmp, (b, fx) => fx.ApplyTo(b, region));
}

public class AcceleratedChainedPartialBitmapEffect
    : PartialBitmapEffect.Accelerated
{
    public Accelerated[] Effects { get; }


    public AcceleratedChainedPartialBitmapEffect(IEnumerable<Accelerated> effects)
        : this(effects.ToArray())
    {
    }

    public AcceleratedChainedPartialBitmapEffect(params Accelerated[] effects) => Effects = effects;

    internal protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        if (Effects.Length is 0)
            Parallel.For(0, w * h, i => destination[i] = source[i]);
        else if (Effects.Length is 1)
            Effects[0].Process(bmp, source, destination, region);
        else
        {
            RGBAColor[] tmp = new RGBAColor[w * h];

            if ((Effects.Length % 2) == 0)
                Parallel.For(0, tmp.Length, i => destination[i] = source[i]);
            else
                Parallel.For(0, tmp.Length, i => tmp[i] = source[i]);

            fixed (RGBAColor* ptmp = tmp)
                for (int i = 0, l = Effects.Length; i < l; ++i)
                    if ((l - i) % 2 == 0)
                        Effects[i].Process(bmp, destination, ptmp, region);
                    else
                        Effects[i].Process(bmp, ptmp, destination, region);
        }
    }
}

public abstract class ColorEffect
    : PartialBitmapEffect.Accelerated
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected abstract RGBAColor ProcessColor(RGBAColor input);

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


    public new class Delegated
        : ColorEffect
    {
        public Func<RGBAColor, RGBAColor> Delegate { get; }


        public Delegated(Func<RGBAColor, RGBAColor> @delegate) => Delegate = @delegate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override RGBAColor ProcessColor(RGBAColor input) => Delegate(input);
    }
}

public abstract class CoordinateColorEffect
    : PartialBitmapEffect.Accelerated
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected abstract RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal protected override sealed unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        Parallel.ForEach(GetIndices(bmp, region), idx => destination[idx] = ProcessCoordinate(idx % w, idx / w, w, h, source[idx]));
    }

    public static CoordinateColorEffect FromDelegate(Func<int, int, int, int, RGBAColor, RGBAColor> func) => new __del(func);


    private sealed class __del
        : CoordinateColorEffect
    {
        private readonly Func<int, int, int, int, RGBAColor, RGBAColor> _func;


        public __del(Func<int, int, int, int, RGBAColor, RGBAColor> func) => _func = func;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source) => _func(x, y, w, h, source);
    }

    /// <summary>
    /// (x, y) in [-1..1]²
    /// </summary>
    public abstract class Relative
        : CoordinateColorEffect
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract RGBAColor ProcessCoordinate(Vector2 coord, Scalar aspect_ratio, RGBAColor source);

        private protected sealed override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source) =>
            ProcessCoordinate(new Vector2((2d * x / w) - 1, (2d * y / h) - 1), (Scalar)w / h, source);

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
    private protected override RGBAColor ProcessColor(RGBAColor input) => (ColorMatrix * input) + ColorBias;
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
    private protected override RGBAColor ProcessColor(RGBAColor input) => (ColorMatrix * input) + ColorBias;
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
        (int w, int h) = (bmp.Width, bmp.Height);
        int kcount = kernels.Length;
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
                        if (GetIndex(x + sx, y + sy, w, region, EdgeHandling) is int idx)
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
        (int w, int h) = (bmp.Width, bmp.Height);
        int[] indices = GetIndices(bmp, region);

        Parallel.For(0, indices.Length, i =>
        {
            int y = indices[i] / w;
            int x = indices[i] % w;
            Vector4 c = Vector4.Zero;

            for (int sy = -kh; sy <= kh; ++sy)
                for (int sx = -kw; sx <= kw; ++sx)
                    if (GetIndex(x + sx, y + sy, w, region, EdgeHandling) is int idx)
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
