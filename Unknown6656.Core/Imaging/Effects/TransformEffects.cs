using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;

namespace Unknown6656.Imaging.Effects;


/// <summary>
/// Represents a bitmap effect wich applies a stored transformation matrix to each of the bitmap's pixel
/// </summary>
public class AffinePixelTransform
    : PartialBitmapEffect.Accelerated
{
    /// <summary>
    /// Transformation matrix to be applied to the bitmap
    /// </summary>
    public Matrix2 LinearTransformMatrix { get; }

    public Vector2 TranslationOffset { get; }

    public Vector2 TransformationOriginOffset { get; set; } = Vector2.Zero;

    public Matrix3 AffineTransformationMatrix { get; }

    public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

    public PixelInterpolationMode Interpolation { set; get; } = PixelInterpolationMode.BilinearInterpolation;


    public AffinePixelTransform(Matrix2 matrix)
        : this(matrix, default)
    {
    }

    public AffinePixelTransform(Matrix2 matrix, Vector2 offset)
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

    protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        if (LinearTransformMatrix.IsInvertible)
            ProcessInvertible(bmp, source, destination, region);
        else
            ProcessNonInvertible(bmp, source, destination, region);
    }

    private unsafe void ProcessInvertible(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        Scalar scale = .5 * Math.Max(w, h);
        Vector2 origin = TransformationOriginOffset;
        Matrix2 inv_transf = LinearTransformMatrix.Inverse;

        RGBAColor get_pixel(int x, int y)
        {
            if (EdgeHandling == EdgeHandlingMode.Extend)
                return source[Clamp(y, 0, h - 1) * w + Clamp(x, 0, w - 1)];
            else if (GetIndex(x, y, w, region, EdgeHandling) is int i)
                return source[i];

            return 0;
        }

        Parallel.ForEach(GetIndices(bmp, region), idx =>
        {
            Vector2 dst_coord = GetAbsoluteCoordinates(idx, w);
            Vector2 src_coord = (dst_coord - TranslationOffset - origin) / scale - 1;

            src_coord = inv_transf.Multiply(in src_coord);
            src_coord = (src_coord + 1) * scale + origin;

            if (src_coord.X.IsInteger && src_coord.Y.IsInteger)
                destination[idx] = get_pixel((int)src_coord.X, (int)src_coord.Y);
            else if (Interpolation == PixelInterpolationMode.NearestNeighbour)
                destination[idx] = get_pixel((int)src_coord.X.Rounded, (int)src_coord.Y.Rounded);
            else
            {
                Vector4 tl = get_pixel((int)src_coord.X, (int)src_coord.Y);
                Vector4 tr = get_pixel((int)src_coord.X.Increment(), (int)src_coord.Y);
                Vector4 bl = get_pixel((int)src_coord.X, (int)src_coord.Y.Increment());
                Vector4 br = get_pixel((int)src_coord.X.Increment(), (int)src_coord.Y.Increment());
                Scalar dx = src_coord.X.DecimalPlaces;
                Scalar dy = src_coord.Y.DecimalPlaces;

                destination[idx] = Vector4.LinearInterpolate(
                    Vector4.LinearInterpolate(in tl, in tr, dx),
                    Vector4.LinearInterpolate(in bl, in br, dx),
                    dy
                );
            }
        });
    }

    private unsafe void ProcessNonInvertible(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        Scalar scale = .5 * Math.Max(w, h);
        Vector2 offset = TranslationOffset;
        Vector2 origin = TransformationOriginOffset;
        Matrix2 transf = LinearTransformMatrix;

        RGBAColor get_pixel(int x, int y)
        {
            if (EdgeHandling == EdgeHandlingMode.Extend)
                return source[Clamp(y, 0, h - 1) * w + Clamp(x, 0, w - 1)];
            else if (GetIndex(x, y, w, region, EdgeHandling) is int i)
                return source[i];

            return 0;
        }

        Parallel.ForEach(GetIndices(bmp, region), idx =>
        {
            (int x, int y) = GetAbsoluteCoordinates(idx, w);
            Vector2 src_coord = new(x, y);
            Vector2 dst_coord = (src_coord - origin) / scale - 1;

            dst_coord = transf.Multiply(in dst_coord);
            dst_coord = (dst_coord + 1) * scale + origin + TranslationOffset;

            destination[(int)dst_coord.Y * w + (int)dst_coord.X] = get_pixel(x, y);
        });
    }
}

public class PixelTransform
    : PartialBitmapEffect.Accelerated
{
    public Func<Vector2, Vector2> TransformationFunction { get; }

    public EdgeHandlingMode EdgeHandling { set; get; } = EdgeHandlingMode.Extend;

    public PixelInterpolationMode Interpolation { set; get; } = PixelInterpolationMode.BilinearInterpolation;


    public PixelTransform(Func<Vector2, Vector2> transform) => TransformationFunction = transform;

    protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        Scalar sc = .5 / Math.Max(w, bmp.Height);

        Parallel.ForEach(GetIndices(bmp, region), src_idx =>
        {
            Vector2 coord = GetAbsoluteCoordinates(src_idx, w);

            coord = coord * sc - 1;
            coord = TransformationFunction(coord);
            coord = (coord + 1) / sc;

            if (coord is { X.Rounded: { IsFinite: true } x, Y.Rounded: { IsFinite: true } y } &&
                GetIndex((int)x, (int)y, w, region, EdgeHandling) is int dst_idx)
            {
                 // TODO : interpolation

                destination[dst_idx] = source[src_idx];
            }
        });
    }
}

public sealed class Scale
    : AffinePixelTransform
{
    public Scale(Scalar factor)
        : this(factor, factor)
    {
    }

    public Scale(Vector2 factors)
        : this(factors.X, factors.Y)
    {
    }

    public Scale(Scalar sx, Scalar sy)
        : base((
            sx, 0,
            0, sy
        ))
    {
    }


    public static Scale Uniform(Scalar factor) => new(factor);

    public static Scale X(Scalar factor) => new(factor, 0);

    public static Scale Y(Scalar factor) => new(0, factor);
}

public sealed class Flip
    : AffinePixelTransform
{
    public static Flip FlipX { get; } = new(true, false);

    public static Flip FlipY { get; } = new(false, true);


    public Flip(bool xdir, bool ydir)
        : base((
            xdir ? -1 : 1, 0,
            0, ydir ? -1 : 1
        ))
    {
    }
}

public sealed class Rotate
    : AffinePixelTransform
{
    public static Rotate Rotate90 { get; } = new(90);

    public static Rotate Rotate180 { get; } = new(180);

    public static Rotate Rotate270 { get; } = new(270);

    // TODO : fix this shite

    public Rotate(Scalar φ_degrees)
        : base((
            φ_degrees.Radians().Cos(), -φ_degrees.Radians().Sin(),
            φ_degrees.Radians().Sin(), φ_degrees.Radians().Cos()
        ))
    {
    }
}

public sealed class Translate
    : AffinePixelTransform
{
    public Translate(Vector2 translation)
        : base(new(1, 0, 0, 1), translation)
    {
    }

    public Translate(Scalar translation_x, Scalar translation_y)
        : this(new(translation_x, translation_y))
    {
    }
}

public sealed class Skew
    : AffinePixelTransform
{
    public Skew(Vector2 φ_degrees)
        : this(φ_degrees.X, φ_degrees.Y)
    {
    }

    public Skew(Scalar φx_degrees, Scalar φy_degrees)
        : base((
            1, φx_degrees.Radians().Cot(),
            φy_degrees.Radians().Cot(), 1
        ))
    {
    }
}

public sealed class CartesianToPolar
    : PixelTransform
{
    public CartesianToPolar()
        : base(pos =>
        {
            Scalar r = pos.Length / Scalar.Sqrt2 * 2 - 1;
            Scalar φ = pos.Angle / Scalar.Pi;

            return new(φ, r);
        })
    {
    }
}

public sealed class PolarToCartesian
    : PixelTransform
{
    public PolarToCartesian()
        : base(pos =>
        {
            (Scalar φ, Scalar r) = pos;

            φ *= Scalar.Pi;
            r = (r + 1) * .5 * Scalar.Sqrt2;;

            return (Vector2)φ.Cis() * r;
        })
    {
    }
}
