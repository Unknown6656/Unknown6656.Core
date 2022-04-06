using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;
using System.Collections.Generic;

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

    public int IterpolationSampleSize { get; init; } = 4;


    public PixelTransform(Func<Vector2, Vector2> transform) => TransformationFunction = transform;

    protected internal override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        Scalar sc = 2d / Math.Max(w, h);
        (bool set, Vector2 source)[,] map = new(bool, Vector2)[w, h];
        Vector4 get_src_color(Vector2 coord) => coord is { X.Rounded: { IsFinite: true } x, Y.Rounded: { IsFinite: true } y } && x >= 0 && x < w && y >= 0 && y < h
                                              ? (Vector4)source[(int)x + (int)y * w]
                                              : Vector4.Zero;

        Parallel.For(0, w * h, src_idx =>
        {
            Vector2 coord = GetAbsoluteCoordinates(src_idx, w);
            Vector2 orig = coord;

            coord = coord * sc - 1;
            coord = TransformationFunction(coord);
            coord = (coord + 1) / sc;

            if (coord is { X.Rounded: { IsFinite: true } x, Y.Rounded: { IsFinite: true } y } && x >= 0 && x < w && y >= 0 && y < h)
                map[(int)x, (int)y] = (true, orig);
        });

        Parallel.For(0, w * h, src_idx =>
        {
            (int x, int y) = GetAbsoluteCoordinates(src_idx, w);

            if (!map[x, y].set)
            {
                List<Vector2> coords = new(IterpolationSampleSize);
                int samples = 0;
                int count = 0;
                int sx = x;
                int sy = y;

                do
                {
                    int N = (1 + count) / 2;
                    int sign = (N & 1) * 2 - 1;

                    ((count & 1) == 0 ? ref sx : ref sy) += N * sign;

                    if (sx >= 0 && sy >= 0 && sx < w && sy < h && map[sx, sy].set)
                    {
                        coords.Add(map[sx, sy].source);

                        ++samples;
                    }

                    ++count;
                }
                while (samples < IterpolationSampleSize && count * 4 < w * h);

                Vector2 coord = Vector2.Zero;
                Scalar scale = Scalar.Zero;

                for (int i = 0; i < coords.Count; ++i)
                {
                    Scalar f = coords[i].DistanceTo((x, y)).MultiplicativeInverse;

                    coord += f * coord;
                    scale += f;
                }

                map[x, y] = (true, coord / scale);
            }
        });

        Parallel.ForEach(GetIndices(bmp, region), dst_idx =>
        {
            (int x, int y) = GetAbsoluteCoordinates(dst_idx, w);
            Vector2 src = map[x, y].source;

            // TODO : interpolation

            destination[dst_idx] = get_src_color(src);
        });
    }
}

public sealed class Crop
    : BitmapEffect
{
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    public bool Relative { get; }


    private Crop(int left, int top, int right, int bottom, bool relative)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Relative = relative;
    }

    public override Bitmap ApplyTo(Bitmap bmp)
    {
        (int width, int height) = Relative ? (bmp.Width - Right - Left, bmp.Height - Bottom - Top) : (Right - Left, Bottom - Top);

        if (width < 0)
            throw new ArgumentException("The horizontal cropping margins result in a bitmap with a negative width.");
        else if (height < 0)
            throw new ArgumentException("The vertical cropping margins result in a bitmap with a negative height.");

        Bitmap result = new(width, height, bmp.PixelFormat);
        using Graphics g = Graphics.FromImage(result);

        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.AssumeLinear;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.DrawImageUnscaled(bmp, -Left, -Top);

        return result;
    }

    public static Crop To(int width, int height) => To(new(0, 0, width, height));

    public static Crop To(int x, int y, int width, int height) => To(new(x, y, width, height));

    public static Crop To(Rectangle rectangle) => new(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, false);

    public static Crop By(int left_right, int top_bottom) => By(left_right, top_bottom, left_right, top_bottom);

    /// <summary>
    /// Crops or extends the bitmap bounds by the given offsets.
    /// The extended regions will be filled with the color <see cref="RGBAColor.Transparent"/>.
    /// This is a non-destructive operation.
    /// </summary>
    /// <param name="left">The left offset. A positive amount crops the bitmap on the left side by the given value. A negative amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="top">The top offset. A positive amount crops the bitmap on the top side by the given value. A negative amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="right">The right offset. A positive amount crops the bitmap on the right side by the given value. A negative amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="bottom">The bottom offset. A positive amount crops the bitmap on the bottom side by the given value. A negative amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    public static Crop By(int left, int top, int right, int bottom) => new(left, top, right, bottom, true);
}

public sealed class Scale
    : BitmapEffect
{
    public Scalar HorizontalFactor { get; }
    public Scalar VerticalFactor { get; }


    public Scale(Scalar factor)
        : this(factor, factor)
    {
    }

    public Scale(Vector2 factors)
        : this(factors.X, factors.Y)
    {
    }

    public Scale(Scalar sx, Scalar sy)
    {
        HorizontalFactor = sx;
        VerticalFactor = sy;
    }

    public override Bitmap ApplyTo(Bitmap bmp)
    {
        int w = (int)(bmp.Width * HorizontalFactor);
        int h = (int)(bmp.Height * VerticalFactor);
        Flip? optional_flip = (w, h) switch
        {
            (0, _) => throw new InvalidOperationException("The resulting bitmap would have a width of 0px."),
            (_, 0) => throw new InvalidOperationException("The resulting bitmap would have a height of 0px."),
            ( < 1, < 1) => Flip.FlipBoth,
            ( < 1, _) => Flip.FlipX,
            (_, < 1) => Flip.FlipY,
            _ => null,
        };

        bmp = optional_flip?.ApplyTo(bmp) ?? bmp;

        return new(bmp, Math.Abs(w), Math.Abs(h));
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

    public static Flip FlipBoth { get; } = new(true, true);


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
        : this(Scalar.One)
    {
    }

    public CartesianToPolar(Scalar amount)
        : base(pos =>
        {
            Scalar r = pos.Length / Scalar.Sqrt2 * 2 - 1;
            Scalar φ = pos.Angle / Scalar.Pi;

            return Vector2.LinearInterpolate(in pos, new(φ, r), amount.Clamp());
        })
    {
    }
}

public sealed class PolarToCartesian
    : PixelTransform
{
    public PolarToCartesian()
        : this(Scalar.One)
    {
    }

    public PolarToCartesian(Scalar amount)
        : base(pos =>
        {
            (Scalar φ, Scalar r) = pos;

            φ *= Scalar.Pi;
            r = (r + 1) * .5 * Scalar.Sqrt2;;

            return Vector2.LinearInterpolate(in pos, (Vector2)φ.Cis() * r, amount.Clamp());
        })
    {
    }
}


// TODO : 3D rotation
