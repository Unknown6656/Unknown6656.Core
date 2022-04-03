using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;

namespace Unknown6656.Imaging.Effects;


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
