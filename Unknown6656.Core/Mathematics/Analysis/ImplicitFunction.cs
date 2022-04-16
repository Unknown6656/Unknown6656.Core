using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Analysis;


public abstract class GeneralizedImplicitFunction<Codomain>
{
    public bool this[Codomain value] => Evaluate(value);

    public bool this[Codomain value, Scalar tolerance] => Evaluate(value, tolerance);


    public virtual bool Evaluate(Codomain value) => Evaluate(value, Scalar.ComputationalEpsilon);

    public abstract bool Evaluate(Codomain value, Scalar tolerance);

    public GeneralizedImplicitFunction<Codomain> Negate() => new Delegated((x, ε) => !Evaluate(x, ε));

    public GeneralizedImplicitFunction<Codomain> Except(GeneralizedImplicitFunction<Codomain> second) => new Delegated((x, ε) => Evaluate(x, ε) && !second.Evaluate(x, ε));

    public GeneralizedImplicitFunction<Codomain> SymmetricDifference(GeneralizedImplicitFunction<Codomain> second) => new Delegated((x, ε) => Evaluate(x, ε) ^ second.Evaluate(x, ε));

    public GeneralizedImplicitFunction<Codomain> Union(params GeneralizedImplicitFunction<Codomain>[] others) => new Delegated((x, ε) =>
        others.Aggregate(Evaluate(x, ε), (r, f) => r || f.Evaluate(x, ε)));

    public GeneralizedImplicitFunction<Codomain> Intersect(params GeneralizedImplicitFunction<Codomain>[] others) => new Delegated((x, ε) =>
        others.Aggregate(Evaluate(x, ε), (r, f) => r && f.Evaluate(x, ε)));

    public static GeneralizedImplicitFunction<Codomain> operator !(GeneralizedImplicitFunction<Codomain> function) => function.Negate();

    public static GeneralizedImplicitFunction<Codomain> operator &(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.Intersect(second);

    public static GeneralizedImplicitFunction<Codomain> operator ^(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.SymmetricDifference(second);

    public static GeneralizedImplicitFunction<Codomain> operator |(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.Union(second);

    protected class Delegated
        : GeneralizedImplicitFunction<Codomain>
    {
        private readonly Func<Codomain, Scalar, bool> _func;


        public Delegated(Func<Codomain, Scalar, bool> func) => _func = func;

        public override bool Evaluate(Codomain value, Scalar tolerance) => _func(value, tolerance);
    }
}

public abstract class ImplicitFunction<@this, Codomain, Function>
    : GeneralizedImplicitFunction<Codomain>
    where @this : ImplicitFunction<@this, Codomain, Function>
    where Codomain : IGroup<Codomain>
    where Function : Function<Codomain, Scalar>
{
    public Function Left { get; }
    public Function Right { get; }
    public ComparisonOperator ComparisonOperator { get; }


    protected ImplicitFunction(Function left, ComparisonOperator comparison, Function right)
    {
        Left = left;
        Right = right;
        ComparisonOperator = comparison;
    }

    public override bool Evaluate(Codomain value, Scalar tolerance)
    {
        Scalar left = Left.Evaluate(value);
        Scalar right = Left.Evaluate(value);
        bool eq = left.Is(right, tolerance);
        int cmp = left.CompareTo(right);

        return ComparisonOperator switch
        {
            ComparisonOperator.EqualTo => eq,
            ComparisonOperator.SmallerOrEqualTo => eq || cmp <= 0,
            ComparisonOperator.GreaterOrEqualTo => eq || cmp >= 0,
            ComparisonOperator.SmallerThan => cmp < 0,
            ComparisonOperator.GreaterThan => cmp > 0,
            _ => throw new InvalidOperationException($"Invalid value '{ComparisonOperator}' for the property '{nameof(ComparisonOperator)}'"),
        };
    }

    public override string ToString() => $"{Left} {ComparisonOperator switch
    {
        ComparisonOperator.SmallerThan => '<',
        ComparisonOperator.SmallerOrEqualTo => '≤',
        ComparisonOperator.EqualTo => '=',
        ComparisonOperator.GreaterOrEqualTo => '≥',
        ComparisonOperator.GreaterThan => '>',
        _ => '?',
    }} {Right}";
}

public class ImplicitFunction<Codomain>
    : ImplicitFunction<ImplicitFunction<Codomain>, Codomain, Function<Codomain, Scalar>>
    where Codomain : IGroup<Codomain>
{
    public ImplicitFunction(Function<Codomain, Scalar> left, ComparisonOperator comparison, Function<Codomain, Scalar> right)
        : base(left, comparison, right)
    {
    }

    public ImplicitFunction(Func<Codomain, Scalar> left, ComparisonOperator comparison, Func<Codomain, Scalar> right)
        : base(new(left), comparison, new(right))
    {
    }

    public ImplicitFunction<Codomain> Create(Function<Codomain, Scalar> left, ComparisonOperator comparison, Function<Codomain, Scalar> right) => new(left, comparison, right);
}

public class ImplicitScalarFunction
    : ImplicitFunction<Scalar>
{
    public ImplicitScalarFunction(ScalarFunction left, ComparisonOperator comparison, ScalarFunction right)
        : base((Function<Scalar, Scalar>)left, comparison, right)
    {
    }

    public ImplicitScalarFunction(Func<Scalar, Scalar> left, ComparisonOperator comparison, Func<Scalar, Scalar> right)
        : base(left, comparison, right)
    {
    }
}

public partial class ImplicitScalarFunction2D
{
    public static ImplicitScalarFunction2D Circle(Scalar radius, bool fill = false) => Circle(Vector2.Zero, radius, fill);

    public static ImplicitScalarFunction2D Circle(Vector2 center, Scalar radius, bool fill = false) =>
        new(xy => xy.DistanceTo(center), fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo, _ => radius);

    public static ImplicitScalarFunction2D Ellipse(Vector2 center, Vector2 radii, bool fill = false) =>
        new(xy => (((xy.X - center.X) / radii.X) ^ 2)
                + (((xy.Y - center.Y) / radii.Y) ^ 2),
            fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo,
            _ => Scalar.One
        );
}

public partial class ImplicitScalarFunction3D
{
    public static ImplicitScalarFunction3D Sphere(Scalar radius, bool fill = false) => Sphere(Vector3.Zero, radius, fill);

    public static ImplicitScalarFunction3D Sphere(Vector3 center, Scalar radius, bool fill = false) =>
        new(xyz => xyz.DistanceTo(center), fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo, _ => radius);

    public static ImplicitScalarFunction3D Ellipsoid(Vector3 center, Vector3 radii, bool fill = false) =>
        new(xyz => (((xyz.X - center.X) / radii.X) ^ 2)
                + (((xyz.Y - center.Y) / radii.Y) ^ 2)
                + (((xyz.Z - center.Z) / radii.Z) ^ 2),
            fill ? ComparisonOperator.SmallerOrEqualTo : ComparisonOperator.EqualTo,
            _ => Scalar.One
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
