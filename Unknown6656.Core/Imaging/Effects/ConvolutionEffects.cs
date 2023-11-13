using System.Threading.Tasks;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Imaging.Effects;


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

            return
            [
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
            ];
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
              1, 0, -2, -1, 1,
             -1, 0, -1, 0, 0,
             -2, -1, .5, 1, 2,
              0, 0, 1, 0, 1,
             -1, 1, 2, 0, -1
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

    private protected override Bitmap Process(Bitmap bmp, Rectangle region)
    {
        int c = 2 * Radius + 1;
        Scalar[,] m = new Scalar[c, 1];

        for (int i = 0; i < c; ++i)
            m[i, 0] = Scalar.One / c;

        MatrixNM h = new(m);
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
                v += GetIndex(x + offs, y, w, region, EdgeHandling) is int idx ? dst[idx] : default;

            dst[idx[i]] = v / iar;
        });
        Parallel.For(0, idx.Length, i =>
        {
            int y = idx[i] / w;
            int x = idx[i] % w;
            Vector4 v = Vector4.Zero;

            for (int offs = -(int)r; offs <= r; ++offs)
                v += GetIndex(x, y + offs, w, region, EdgeHandling) is int idx ? dst[idx] : default;

            dst[idx[i]] = v / iar;
        });
    }
}
