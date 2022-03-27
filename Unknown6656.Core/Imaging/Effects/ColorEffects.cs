using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics;
using Unknown6656.Runtime;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Imaging.Effects;


/// <summary>
/// Represents an abstract color gradient effect.
/// A gradient effect generates a coordinate-based color, which is blended with the original bitmap using the <see cref="Blending"/>-property.
/// <para/>
/// Known implementations of this class are <see cref="ConstantColor"/>, <see cref="LinearGradient"/>, <see cref="RadialGradient"/>,
/// <see cref="MultiPointGradient"/>, <see cref="VoronoiGradient"/>, etc.
/// </summary>
/// <completionlist cref="Gradient"/>
public abstract class Gradient
    : CoordinateColorEffect
{
    public BlendMode Blending { get; set; } = BlendMode.Top;


    private protected abstract RGBAColor ProcessCoordinate(int x, int y, int w, int h);

    private protected sealed override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source) =>
        RGBAColor.Blend(source, ProcessCoordinate(x, y, w, h), Blending);


    // TODO : add static methods
}

/// <summary>
/// An effect which fills a given bitmap with a specified <see cref="RGBAColor"/> using a specified <see cref="BlendMode"/>.
/// </summary>
public sealed class ConstantColor
    : Gradient
{
    public RGBAColor Color { get; }


    public ConstantColor(RGBAColor color) => Color = color;

    private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h) => Color;
}

public sealed class LinearGradient
    : Gradient
{
    public ColorMap Colors { get; }
    public Vector2 Start { get; }
    public Vector2 End { get; }

    private readonly Vector2 _to;
    private readonly Scalar _min;
    private readonly Scalar _max;


    public LinearGradient(Vector2 start, Vector2 end, params RGBAColor[] colors)
        : this(start, end, ColorMap.Uniform(colors))
    {
    }

    public LinearGradient(Vector2 start, Vector2 end, IEnumerable<RGBAColor> colors)
        : this(start, end, colors as RGBAColor[] ?? colors.ToArray())
    {
    }

    public LinearGradient(Vector2 start, Vector2 end, ColorMap colors)
    {
        if (colors is DiscreteColorMap { Size: 0 })
            throw new ArgumentException("The color map must not be empty.", nameof(colors));

        Colors = colors;
        Start = start;
        End = end;

        _to = End - Start;
        _min = _to * Start;
        _max = _to * End;
    }

    private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h)
    {
        Scalar progress = _to * (x, y);

        return Colors[progress <= _min ? 0 : progress >= _max ? 1 : (progress - _min) / (_max - _min)];
    }
}

public sealed class RadialGradient
    : Gradient
{
    public ColorMap Colors { get; }
    public Vector2 Center { get; }
    public Scalar? Size { get; }


    public RadialGradient(Vector2 center, Scalar? size, params RGBAColor[] colors)
        : this(center, size, ColorMap.Uniform(colors))
    {
    }

    public RadialGradient(Vector2 center, Scalar? size, IEnumerable<RGBAColor> colors)
        : this(center, size, colors as RGBAColor[] ?? colors.ToArray())
    {
    }

    public RadialGradient(Vector2 center, Scalar? size, ColorMap colors)
    {
        if (colors is DiscreteColorMap { Size: 0 })
            throw new ArgumentException("The color map must not be empty.", nameof(colors));
        else if (size is Scalar s && (s.IsNegative || !s.IsFinite))
            throw new ArgumentException("The size must either be null, zero, or a positive, finite number.", nameof(size));

        Colors = colors;
        Center = center;
        Size = size;
    }

    private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h)
    {
        Scalar dist = Center.DistanceTo((x, y));
        Scalar size = Size ?? new Vector2(w, h).Length / 2;

        return size > Scalar.ComputationalEpsilon ? Colors[dist, Scalar.Zero, size] : Colors[Scalar.One];
    }
}

public sealed class MultiPointGradient
    : Gradient
{
    public (Vector2 Position, RGBAColor Color)[] Points { get; }


    public MultiPointGradient(IEnumerable<(Vector2 Position, RGBAColor Color)> points)
        : this(points as (Vector2, RGBAColor)[] ?? points.ToArray())
    {
    }

    public MultiPointGradient(params (Vector2 Position, RGBAColor Color)[] points) =>
        Points = points.Length is 0 ? throw new ArgumentException("At least one point has to be provided.", nameof(points)) : points;

    private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source)
    {
        Vector2 pos = (x, y);
        Scalar[] distances = Points.ToArray(p => p.Position.DistanceTo(in pos));
        Scalar maxdist = distances.Max();



        RGBAColor color = ;

        return color;
    }
}

public enum VoronoiMode
{
    EucledianDistance,
    ManhattanDistance,
    XCoordinate,
    YCoordinate,
    // TODO : more ?
}

public sealed class VoronoiGradient
    : CoordinateColorEffect
{
    public VoronoiMode VoronoiDistanceMetric { get; set; } = VoronoiMode.EucledianDistance;
    public (Vector2 Position, RGBAColor Color)[] Points { get; }


    public VoronoiGradient(IEnumerable<(Vector2 Position, RGBAColor Color)> points)
        : this(points as (Vector2, RGBAColor)[] ?? points.ToArray())
    {
    }

    public VoronoiGradient(params (Vector2 Position, RGBAColor Color)[] points) =>
        Points = points.Length is 0 ? throw new ArgumentException("At least one point has to be provided.", nameof(points)) : points;

    private protected override RGBAColor ProcessCoordinate(int x, int y, int w, int h, RGBAColor source)
    {
        Vector2 pos = (x, y);
        Func<Vector2, Scalar> dist = VoronoiDistanceMetric switch
        {
            VoronoiMode.XCoordinate => v => v.X.Subtract(x).Abs(),
            VoronoiMode.YCoordinate => v => v.Y.Subtract(y).Abs(),
            VoronoiMode.ManhattanDistance => v => v.X.Subtract(x).Abs() + v.Y.Subtract(y).Abs(),
            VoronoiMode.EucledianDistance => v => v.DistanceTo(pos),
            _ => throw new ArgumentOutOfRangeException(nameof(VoronoiDistanceMetric)),
        };

        return Points.OrderBy(p => dist(p.Position)).FirstOrDefault().Color;
    }
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

    private protected override RGBAColor ProcessColor(RGBAColor input)
    {
        (double h, double s, double l) = input.HSL;

        return RGBAColor.FromHSL(h + Degree, s, l);
    }
}

public class ReplaceColor
    : ColorEffect
{
    private readonly (RGBAColor search, RGBAColor replace)[] _pairs;
    private readonly ColorTolerance _tolerance;



    public ReplaceColor(RGBAColor search, RGBAColor replace)
        : this(new[] { (search, replace) })
    {
    }

    public ReplaceColor(RGBAColor search, RGBAColor replace, ColorTolerance tolerance)
        : this(new[] { (search, replace) }, tolerance)
    {
    }

    public ReplaceColor(IEnumerable<RGBAColor> search, RGBAColor replace)
        : this(search.Select(s => (s, replace)))
    {
    }

    public ReplaceColor(IEnumerable<RGBAColor> search, RGBAColor replace, ColorTolerance tolerance)
        : this(search.Select(s => (s, replace)), tolerance)
    {
    }

    public ReplaceColor(IEnumerable<(RGBAColor search, RGBAColor replace)> pairs)
        : this(pairs, ColorTolerance.RGBDefault)
    {
    }

    public ReplaceColor(IEnumerable<(RGBAColor search, RGBAColor replace)> pairs, ColorTolerance tolerance)
    {
        _pairs = pairs as (RGBAColor, RGBAColor)[] ?? pairs.ToArray();
        _tolerance = tolerance;
    }

    private protected override RGBAColor ProcessColor(RGBAColor input)
    {
        foreach ((RGBAColor search, RGBAColor replace) in _pairs)
            if (input.Equals(search, _tolerance))
                return replace;

        return input;
    }
}

public sealed class RemoveColor
    : ReplaceColor
{
    public RemoveColor(RGBAColor color)
        : base(color, RGBAColor.Transparent)
    {
    }

    public RemoveColor(RGBAColor color, ColorTolerance tolerance)
        : base(color, RGBAColor.Transparent, tolerance)
    {
    }

    public RemoveColor(IEnumerable<RGBAColor> colors)
        : base(colors, RGBAColor.Transparent)
    {
    }

    public RemoveColor(IEnumerable<RGBAColor> colors, ColorTolerance tolerance)
        : base(colors, RGBAColor.Transparent, tolerance)
    {
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
    private protected override RGBAColor ProcessColor(RGBAColor input)
    {
        (double h, double s, double l) = input.HSL;

        return new Vector4(h / Scalar.Tau, s, l, input.Af);
    }
}

public sealed class HSLtoRGB
    : ColorEffect
{
    private protected override RGBAColor ProcessColor(RGBAColor input)
    {
        Scalar h = input.Rf * Scalar.Tau;
        Scalar s = input.Gf;
        Scalar l = input.Bf;

        return RGBAColor.FromHSL(h, s, l);
    }
}

public class ReduceColorSpace
    : ColorEffect.Delegated
{
    public ColorEqualityMetric EqualityMetric { get; }
    public ColorPalette ColorPalette { get; }


    public ReduceColorSpace(ColorPalette target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
        : base(c => target_palette.GetNearestColor(c, equality_metric))
    {
        ColorPalette = target_palette;
        EqualityMetric = equality_metric;
    }

    public ReduceColorSpace(IEnumerable<RGBAColor> target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
        : this(new ColorPalette(target_palette), equality_metric)
    {
    }
}

public class ColorSpaceReductionError
    : ColorEffect.Delegated
{
    public ColorEqualityMetric EqualityMetric { get; }
    public ColorPalette ColorPalette { get; }


    public ColorSpaceReductionError(ColorPalette target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
        : base(c =>
        {
            target_palette.GetNearestColor(c, equality_metric, out double dist);
            byte val = (byte)(dist * 255);

            return new(val, val, val);
        })
    {
        ColorPalette = target_palette;
        EqualityMetric = equality_metric;
    }

    public ColorSpaceReductionError(IEnumerable<RGBAColor> target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
        : this(new ColorPalette(target_palette), equality_metric)
    {
    }
}

public sealed class Cartoon
    : ColorEffect
{
    public int Steps { get; }


    public Cartoon(int steps) => Steps = Math.Max(steps, 1);

    private protected override RGBAColor ProcessColor(RGBAColor input)
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
            (double h, double s, _) = blurred[i].HSL;

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

    private protected override Bitmap Process(Bitmap bmp, Rectangle region)
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
            new BitmapBlend(background, BlendMode.Multiply, 1)
        ), region);
    }
}

public sealed class Colorize
    : ColorEffect
{
    public ColorMap Map { get; }


    public Colorize(ColorMap map) => Map = map;

    [Obsolete($"Use the effect '{nameof(ReduceColorSpace)}' instead.", true)]
    public Colorize(ColorPalette palette) => throw new Exception($"Please use the class '{typeof(ReduceColorSpace)}' instead.");

    private protected override RGBAColor ProcessColor(RGBAColor input) => Map[input.Average];
}

public sealed class Sepia
    : RGBColorEffect
{
    public Sepia()
        : this(Scalar.One)
    {
    }

    public Sepia(Scalar strength)
        : base(Matrix3.Identity.LinearInterpolate((
            .393, .769, .189,
            .349, .686, .168,
            .272, .534, .131
        ), strength))
    {
    }
}
