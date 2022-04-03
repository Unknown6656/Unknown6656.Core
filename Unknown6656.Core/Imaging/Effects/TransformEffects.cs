
using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Imaging.Effects;


public sealed class ScaleTransform
    : TransformEffect
{
    public ScaleTransform(Scalar factor)
        : this(factor, factor)
    {
    }

    public ScaleTransform(Vector2 factors)
        : this(factors.X, factors.Y)
    {
    }

    public ScaleTransform(Scalar sx, Scalar sy)
        : base((
            sx, 0,
            0, sy
        ))
    {
    }


    public static ScaleTransform Uniform(Scalar factor) => new(factor);

    public static ScaleTransform X(Scalar factor) => new(factor, 0);

    public static ScaleTransform Y(Scalar factor) => new(0, factor);
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

    public static FlipTransform FlipX => new(true, false);

    public static FlipTransform FlipY => new(false, true);
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
