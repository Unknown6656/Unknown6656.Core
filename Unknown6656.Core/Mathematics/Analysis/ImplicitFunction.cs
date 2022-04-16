using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static implicit operator GeneralizedImplicitFunction<Codomain> !(GeneralizedImplicitFunction<Codomain> function) => function.Negate();

    public static implicit operator GeneralizedImplicitFunction<Codomain> &(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.Intersect(second);

    public static implicit operator GeneralizedImplicitFunction<Codomain> ^(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.SymmetricDifference(second);

    public static implicit operator GeneralizedImplicitFunction<Codomain> |(GeneralizedImplicitFunction<Codomain> first, GeneralizedImplicitFunction<Codomain> second) => first.Union(second);

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
}

public class ImplicitFunction<Codomain>
    : ImplicitFunction<ImplicitFunction<Codomain>, Codomain, Function<Codomain, Scalar>>
    where Codomain : IGroup<Codomain>
{
    public ImplicitFunction(Function<Codomain, Scalar> left, ComparisonOperator comparison, Function<Codomain, Scalar> right)
        : base(left, comparison, right)
    {
    }

    public ImplicitFunction<Codomain> Create(Function<Codomain, Scalar> left, ComparisonOperator comparison, Function<Codomain, Scalar> right) => new(left, comparison, right);
}

public enum ComparisonOperator
{
    SmallerThan,
    SmallerOrEqualTo,
    EqualTo,
    GreaterOrEqualTo,
    GreaterThan,
}
