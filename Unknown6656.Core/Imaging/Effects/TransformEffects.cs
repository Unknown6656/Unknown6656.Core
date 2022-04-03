using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;

namespace Unknown6656.Imaging.Effects;


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

    protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        Scalar scale = .5 * Math.Max(w, h);
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
            Vector2 src_coord = (dst_coord - TranslationOffset) / scale - 1;

            src_coord = inv_transf.Multiply(in src_coord);
            src_coord = (src_coord + 1) * scale;

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


    unsafe void __old__Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        bool invertible = LinearTransformMatrix.Determinant.IsInvertible;
        Matrix2 m = invertible ? LinearTransformMatrix.Inverse : LinearTransformMatrix;
        Vector2 offset = TranslationOffset;
        int rw = region.Width;
        int rh = region.Height;
        int rx = region.X;
        int ry = region.Y;
        int w = bmp.Width;
        int h = bmp.Height;

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
            int ix = idx % rw;
            int iy = idx / rw;
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

public sealed class Scale
    : TransformEffect
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
    : TransformEffect
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
    : TransformEffect
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
    : TransformEffect
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
    : TransformEffect
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
