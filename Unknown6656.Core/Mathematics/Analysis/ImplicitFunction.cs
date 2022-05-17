using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using System.Data;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Analysis;


public abstract class GeneralizedImplicitFunction<Domain>
{
    public bool this[Domain value] => Evaluate(value);

    public bool this[Domain value, Scalar tolerance] => Evaluate(value, tolerance);


    public virtual bool Evaluate(Domain value) => Evaluate(value, Scalar.ComputationalEpsilon);

    public abstract bool Evaluate(Domain value, Scalar tolerance);

    public GeneralizedImplicitFunction<Domain> Negate() => new Delegated((x, ε) => !Evaluate(x, ε));

    public GeneralizedImplicitFunction<Domain> Except(GeneralizedImplicitFunction<Domain> second) => new Delegated((x, ε) => Evaluate(x, ε) && !second.Evaluate(x, ε));

    public GeneralizedImplicitFunction<Domain> SymmetricDifference(GeneralizedImplicitFunction<Domain> second) => new Delegated((x, ε) => Evaluate(x, ε) ^ second.Evaluate(x, ε));

    public GeneralizedImplicitFunction<Domain> Union(params GeneralizedImplicitFunction<Domain>[] others) => new Delegated((x, ε) =>
        others.Aggregate(Evaluate(x, ε), (r, f) => r || f.Evaluate(x, ε)));

    public GeneralizedImplicitFunction<Domain> Intersect(params GeneralizedImplicitFunction<Domain>[] others) => new Delegated((x, ε) =>
        others.Aggregate(Evaluate(x, ε), (r, f) => r && f.Evaluate(x, ε)));

    public static GeneralizedImplicitFunction<Domain> operator !(GeneralizedImplicitFunction<Domain> function) => function.Negate();

    public static GeneralizedImplicitFunction<Domain> operator &(GeneralizedImplicitFunction<Domain> first, GeneralizedImplicitFunction<Domain> second) => first.Intersect(second);

    public static GeneralizedImplicitFunction<Domain> operator ^(GeneralizedImplicitFunction<Domain> first, GeneralizedImplicitFunction<Domain> second) => first.SymmetricDifference(second);

    public static GeneralizedImplicitFunction<Domain> operator |(GeneralizedImplicitFunction<Domain> first, GeneralizedImplicitFunction<Domain> second) => first.Union(second);


    protected class Delegated
        : GeneralizedImplicitFunction<Domain>
    {
        private readonly Func<Domain, Scalar, bool> _func;


        public Delegated(Func<Domain, Scalar, bool> func) => _func = func;

        public override bool Evaluate(Domain value, Scalar tolerance) => _func(value, tolerance);
    }
}

public abstract class ImplicitFunction<@this, Domain, Function>
    : GeneralizedImplicitFunction<Domain>
    where @this : ImplicitFunction<@this, Domain, Function>
    where Domain : IGroup<Domain>
    where Function : Function<Domain, Scalar>
{
    public Function ExplicitFunction { get; }
    public ComparisonOperator ComparisonOperator { get; }


    protected ImplicitFunction(Function function, ComparisonOperator comparison)
    {
        ExplicitFunction = function;
        ComparisonOperator = comparison;
    }

    public override bool Evaluate(Domain value, Scalar tolerance)
    {
        Scalar diff = EvaluateSignedDifference(value, tolerance);

        return ComparisonOperator switch
        {
            ComparisonOperator.EqualTo => diff == Scalar.Zero,
            ComparisonOperator.SmallerOrEqualTo => diff <= Scalar.Zero,
            ComparisonOperator.GreaterOrEqualTo => diff >= Scalar.Zero,
            ComparisonOperator.SmallerThan => diff < Scalar.Zero,
            ComparisonOperator.GreaterThan => diff > Scalar.Zero,
            _ => throw new InvalidOperationException($"Invalid value '{ComparisonOperator}' for the property '{nameof(ComparisonOperator)}'"),
        };
    }

    public Scalar EvaluateSignedDifference(Domain value, Scalar tolerance)
    {
        tolerance = tolerance.Min(Scalar.ComputationalEpsilon);

        Scalar eval = ExplicitFunction.Evaluate(value);

        if (eval.Abs() <= tolerance)
            return Scalar.Zero;
        else if (eval < -tolerance)
            return eval + tolerance;
        else
            return eval - tolerance;
    }

    public override string ToString() => $"{ExplicitFunction} {ComparisonOperator switch
    {
        ComparisonOperator.SmallerThan => '<',
        ComparisonOperator.SmallerOrEqualTo => '≤',
        ComparisonOperator.EqualTo => '=',
        ComparisonOperator.GreaterOrEqualTo => '≥',
        ComparisonOperator.GreaterThan => '>',
        _ => '?',
    }} 0";

    protected static ComparisonOperator Combine(ComparisonOperator first, ComparisonOperator second)
    {
        if (first == second)
            return first;
        else if (first <= ComparisonOperator.EqualTo && second <= ComparisonOperator.EqualTo)
            return (ComparisonOperator)Math.Min((int)first, (int)second);
        else if (first >= ComparisonOperator.EqualTo && second >= ComparisonOperator.EqualTo)
            return (ComparisonOperator)Math.Max((int)first, (int)second);
        else
            return ComparisonOperator.EqualTo;
    }

    public static explicit operator Function<Domain, Scalar>(ImplicitFunction<@this, Domain, Function> function) => function.ExplicitFunction;

    public static explicit operator Func<Domain, Scalar>(ImplicitFunction<@this, Domain, Function> function) => function.ExplicitFunction;
}

public class ImplicitFunction<Domain>
    : ImplicitFunction<ImplicitFunction<Domain>, Domain, Function<Domain, Scalar>>
    where Domain : IGroup<Domain>
{
    public ImplicitFunction(Function<Domain, Scalar> function)
        : this(function, ComparisonOperator.EqualTo)
    {
    }

    public ImplicitFunction(Func<Domain, Scalar> function)
        : this(new Function<Domain, Scalar>(function))
    {
    }

    public ImplicitFunction(Function<Domain, Scalar> function, ComparisonOperator comparison)
        : base(function, comparison)
    {
    }

    public ImplicitFunction(Func<Domain, Scalar> function, ComparisonOperator comparison)
        : this(new Function<Domain, Scalar>(function), comparison)
    {
    }

    public ImplicitFunction(Function<Domain, Scalar> function, ComparisonOperator comparison, Scalar value)
        : this(function.Subtract(value), comparison)
    {
    }

    public ImplicitFunction(Func<Domain, Scalar> function, ComparisonOperator comparison, Scalar value)
        : this(new Function<Domain, Scalar>(function), comparison, value)
    {
    }

    public ImplicitFunction(Function<Domain, Scalar> left, ComparisonOperator comparison, Function<Domain, Scalar> right)
        : this(left.Subtract(right), comparison)
    {
    }

    public ImplicitFunction(Func<Domain, Scalar> left, ComparisonOperator comparison, Func<Domain, Scalar> right)
        : this(new Function<Domain, Scalar>(left), comparison, new Function<Domain, Scalar>(right))
    {
    }

    public ImplicitFunction<Domain> Sign() => Then(new(x => x.Sign));

    public ImplicitFunction<Domain> Then(Function<Scalar, Scalar> second) =>
        new(ExplicitFunction.Then(second), ComparisonOperator);

    public ImplicitFunction<Domain> CombineWith(ImplicitFunction<Domain> second, Func<Scalar, Scalar, Scalar> combinator) => new(
        x => combinator(ExplicitFunction.Evaluate(x), second.ExplicitFunction.Evaluate(x)),
        Combine(ComparisonOperator, second.ComparisonOperator)
    );

    public static ImplicitFunction<Domain> LinearInterpolate(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second, Scalar blend_factor)
    {
        blend_factor = blend_factor.Clamp();

        return first.CombineWith(second, (x, y) => x.Multiply(1 - blend_factor).Add(y.Multiply(blend_factor)));
    }

    public static ImplicitFunction<Domain> Combine(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second, Func<Scalar, Scalar, Scalar> combinator) =>
        first.CombineWith(second, combinator);

    public static ImplicitFunction<Domain> Min(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, (x, y) => x.Min(y));

    public static ImplicitFunction<Domain> Max(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, (x, y) => x.Max(y));

    public static ImplicitFunction<Domain> Add(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, Scalar.Add);

    public static ImplicitFunction<Domain> Subtract(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, Scalar.Subtract);

    public static ImplicitFunction<Domain> Multiply(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, Scalar.Multiply);

    public static ImplicitFunction<Domain> Divide(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, Scalar.Divide);

    public static ImplicitFunction<Domain> Modulus(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, Scalar.Modulus);

    public static ImplicitFunction<Domain> Union(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Min(first, second);

    public static ImplicitFunction<Domain> Intersect(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Max(first, second);

    public static ImplicitFunction<Domain> Except(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Combine(first, second, (f, s) => f.Max(-s));

    public static ImplicitFunction<Domain> operator &(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Intersect(first, second);

    public static ImplicitFunction<Domain> operator |(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Union(first, second);

    public static ImplicitFunction<Domain> operator +(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Add(first, second);

    public static ImplicitFunction<Domain> operator -(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Subtract(first, second);

    public static ImplicitFunction<Domain> operator *(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Multiply(first, second);

    public static ImplicitFunction<Domain> operator /(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Divide(first, second);

    public static ImplicitFunction<Domain> operator %(ImplicitFunction<Domain> first, ImplicitFunction<Domain> second) => Modulus(first, second);

    public static explicit operator ImplicitFunction<Domain>(Function<Domain, Scalar> function) => new(function);

    public static explicit operator ImplicitFunction<Domain>(Func<Domain, Scalar> function) => new(function);
}

public class ImplicitScalarFunction
    : ImplicitFunction<Scalar>
{
    public ImplicitScalarFunction(Function<Scalar, Scalar> function) : base(function)
    {
    }

    public ImplicitScalarFunction(Func<Scalar, Scalar> function) : base(function)
    {
    }

    public ImplicitScalarFunction(Function<Scalar, Scalar> function, ComparisonOperator comparison) : base(function, comparison)
    {
    }

    public ImplicitScalarFunction(Func<Scalar, Scalar> function, ComparisonOperator comparison) : base(function, comparison)
    {
    }

    public ImplicitScalarFunction(Function<Scalar, Scalar> function, ComparisonOperator comparison, Scalar value) : base(function, comparison, value)
    {
    }

    public ImplicitScalarFunction(Func<Scalar, Scalar> function, ComparisonOperator comparison, Scalar value) : base(function, comparison, value)
    {
    }

    public ImplicitScalarFunction(Function<Scalar, Scalar> left, ComparisonOperator comparison, Function<Scalar, Scalar> right) : base(left, comparison, right)
    {
    }

    public ImplicitScalarFunction(Func<Scalar, Scalar> left, ComparisonOperator comparison, Func<Scalar, Scalar> right) : base(left, comparison, right)
    {
    }
}

public partial class ImplicitScalarFunction2D
{
    public ImplicitScalarFunction2D Transpose() => new(v => ExplicitFunction.Evaluate(new(v.Y, v.X)), ComparisonOperator);

    public static ImplicitScalarFunction2D Heart() => new((x, y) => x * x + (y - (x * x).Power(1 / 3f)).Power(2) - 1);

    public static ImplicitScalarFunction2D RoundHeart() => new((x, y) => (x * x + y * y - 1).Power(3) - x * x * y * y * y);

    public static ImplicitScalarFunction2D LambertW0() => new((x, y) => (x / y).Log().Subtract(y));

    public static ImplicitScalarFunction2D Circle(Scalar radius, bool fill = false) => Circle(Vector2.Zero, radius, fill);

    public static ImplicitScalarFunction2D Circle(Vector2 center, Scalar radius, bool fill = false) =>
        new(xy => xy.DistanceTo(center), fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo, radius);

    public static ImplicitScalarFunction2D Square(Vector2 center, Scalar side_length, bool fill = false) =>
        Rectangle(center, side_length, side_length, fill);

    public static ImplicitScalarFunction2D Rectangle(Vector2 center, Scalar width, Scalar height, bool fill = false) =>
        new((x, y) => Math.Max((x - center.X).Abs().Divide(width), (y - center.Y).Abs().Divide(height)), fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo, Scalar.One);

    public static ImplicitScalarFunction2D Triangle(Vector2 A, Vector2 B, Vector2 C, bool fill = false)
    {
        Vector2 BA = B - A;
        Vector2 CA = C - A;
        Vector2 BC = B - C;

        return new((x, y) =>
            (
                BA.Y.Multiply((x - 2 * A.X).Abs() + x, .5).Subtract(BA.X.Multiply(y + A.Y - A.X)).Abs()
              + BA.Y.Multiply(x + B.X - B.Y).Subtract(BA.X.Multiply((y - 2 * B.Y).Abs() + x, .5)).Abs()
            ) * (
                CA.Y.Multiply((x - 2 * A.X).Abs() + x, .5).Subtract(CA.X.Multiply(y + A.Y - A.X)).Abs()
              + CA.Y.Multiply(x + C.X - C.Y).Subtract(CA.X.Multiply((y - 2 * C.Y).Abs() + x, .5)).Abs()
            ) * (
                BC.Y.Multiply((x - 2 * C.X).Abs() + x, .5).Subtract(BC.X.Multiply(y + C.Y - C.X)).Abs()
              + BC.Y.Multiply(x + B.X - B.Y).Subtract(BC.X.Multiply((y - 2 * B.Y).Abs() + x, .5)).Abs()
            ),
            fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo
        );
    }

    public static ImplicitScalarFunction2D Ellipse(Vector2 center, Vector2 radii, bool fill = false) =>
        new(xy => (((xy.X - center.X) / radii.X) ^ 2)
                + (((xy.Y - center.Y) / radii.Y) ^ 2),
            fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo,
            Scalar.One
        );

    public static ImplicitScalarFunction2D Ellipse(Vector2 center, Vector2 radii, Scalar φ, bool fill = false) =>
        new(xy => ((((xy.X - center.X) * φ.Cos() + (xy.Y - center.Y) * φ.Sin()) / radii.X) ^ 2)
                + ((((xy.Y - center.Y) * φ.Cos() - (xy.X - center.X) * φ.Sin()) / radii.Y) ^ 2),
        fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo,
        Scalar.One
    );

    public static ImplicitScalarFunction2D EllipticCurve(Scalar a, Scalar b) =>
        new(xy => xy.Y ^ 2, ComparisonOperator.EqualTo, xy => (xy.X ^ 3) + a * xy.X + b);

    public static ImplicitScalarFunction2D WeierstrassEllipticCurve() => EllipticCurve(Scalar.Zero, Scalar.Two);

    public static ImplicitScalarFunction2D ConicalSurface(Scalar radius, Scalar conic_constant) =>
        new((x, y) => x * x / radius / radius * (x * x / y / y - 2 * radius / y + 1 + conic_constant));

    //public static ImplicitScalarFunction2D CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) => new((x, y) =>
    //    // TODO
    //);

    public static ImplicitScalarFunction2D InfiniteLine(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;

        return new((x, y) => (dir.X * (start.Y - y) - dir.Y * (start.X - x)) / dir.Length);
        //return new(xy => dir.X * (xy.Y - start.Y) - dir.Y * (xy.X - start.X));
    }

    // TODO
    public static ImplicitScalarFunction2D Line(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        Vector2 mid = (end + start) * .5;

        return new(Multiply(InfiniteLine(start, end), new ImplicitScalarFunction2D(xy =>
        {
            return (mid.DistanceTo(xy) * 2 - dir.Length).Min(Scalar.Zero);

            //  (xy.DistanceTo(start) + xy.DistanceTo(end) - dir.Length);
        })));
    }

    public static ImplicitScalarFunction2D RegularPolygon(int sides, Scalar radius) =>
        RegularPolygon(Vector2.Zero, sides, radius, Scalar.Zero);

    public static ImplicitScalarFunction2D RegularPolygon(Vector2 center, int sides, Scalar radius, Scalar θ)
    {
        if (sides < 2)
            throw new ArgumentOutOfRangeException(nameof(sides));
        else if (radius < Scalar.ComputationalEpsilon * 2)
            throw new ArgumentOutOfRangeException(nameof(radius));
        else
        {
            Scalar np = -radius * sides;
            Scalar a = Scalar.Tau / sides;

            return new((x, y) =>
            {
                Scalar sum = np;

                for (int i = 0; i < sides; ++i)
                {
                    Scalar φ = i * a - θ;

                    sum += ((x - center.X) * φ.Cos() + (y - center.Y) * φ.Sin() - radius).Abs();
                }

                return sum;
            });
        }
    }

    public static ImplicitScalarFunction2D ArbitraryPolygon(params Vector2[] points)
    {
        if (points is { Length: int l and > 2 })
            return new(xy =>
            {
                Scalar val = 1;//0;
                int sign = 0;

                for (int i = 0; i < l; ++i)
                {
                    var curr = points[i];
                    var next = points[(i + 1) % l];
                    var after = points[(i + 2) % l];


                    var e_curr = curr.To(next);
                    var e_next = next.To(after);

                    var e_test = next.To(xy);

                    var φ_next = e_curr.AngleTo(e_next);
                    var φ_test = e_curr.AngleTo(e_test);
                    var φ_diff = φ_test - φ_next;

                    val *= φ_diff;

                    //if (sign == 0)
                    //{
                    //    val = φ_diff;
                    //    sign = val.Sign;
                    //}
                    //else if (sign < 0)
                    //    val = φ_diff.Max(val);
                    //else
                    //    val = φ_diff.Min(val);
                }

                return val;
            });
        else
            throw new ArgumentException("The polygon must have at least three points.", nameof(points));
    }

    //public static ImplicitScalarFunction2D SoftmaxConvexPolygon(params Vector2[] points)
    //{
    //    if (points is { Length: int l and > 1 })
    //    {
    //        ImplicitFunction<Vector2>? func = null;

    //        foreach (var line in points.Select((p, i) => InfiniteLine(p, points[(i + 1) % l])))
    //            func = func is null ? line : Max(func, line);

    //        if (func is { })
    //            return new(func);
    //    }

    //    throw new ArgumentException("The polygon must have at least three points.", nameof(points));
    //}

    public static ImplicitScalarFunction2D CassiniOval(Scalar a, Scalar b) =>
        new(xy => xy.SquaredNorm.Power(2) - 2 * b * b * xy.SquaredNorm - a.Power(4) + b.Power(4));

    //public static ImplicitScalarFunction2D BlobbyMolecules(IEnumerable<(Vector2 Center, Scalar Radius)> balls, Scalar threshold) => ;

    //public static ImplicitScalarFunction2D SoftBodies(IEnumerable<(Vector2 Center, Scalar Radius)> balls, Scalar threshold) => ;

    //public static ImplicitScalarFunction2D Metaballs(IEnumerable<(Vector2 Center, Scalar Radius)> balls, Scalar threshold) => ;

    public static ImplicitScalarFunction2D Cartesian(Function<Scalar, Scalar> function, ComparisonOperator comparison = ComparisonOperator.EqualTo) =>
        new(xy => function.Evaluate(xy.X), comparison, xy => xy.Y);

    public static ImplicitScalarFunction2D CartesianInverse(Function<Scalar, Scalar> function, ComparisonOperator comparison = ComparisonOperator.EqualTo) =>
        new(xy => function.Evaluate(xy.Y), comparison, xy => xy.X);

    public static implicit operator ImplicitScalarFunction2D(Function<Scalar, Scalar> function) => Cartesian(function);
}

public partial class ImplicitScalarFunction3D
{
    public static ImplicitScalarFunction3D Sphere(Scalar radius, bool fill = false) => Sphere(Vector3.Zero, radius, fill);

    public static ImplicitScalarFunction3D Sphere(Vector3 center, Scalar radius, bool fill = false) =>
        new(xyz => xyz.DistanceTo(center), fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo, radius);

    public static ImplicitScalarFunction3D Ellipsoid(Vector3 center, Vector3 radii, bool fill = false) =>
        new(xyz => (((xyz.X - center.X) / radii.X) ^ 2)
                + (((xyz.Y - center.Y) / radii.Y) ^ 2)
                + (((xyz.Z - center.Z) / radii.Z) ^ 2),
            fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo,
            Scalar.One
        );
}

public enum ComparisonOperator
{
    SmallerThan,
    SmallerOrEqualTo,
    EqualTo,
    GreaterOrEqualTo,
    GreaterThan,
}
