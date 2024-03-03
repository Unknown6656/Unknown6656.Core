using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Analysis;


public abstract class Function<Func, Domain, Codomain>
    : IGroup<Func>
    , IStructuralEquatable
    where Func : Function<Func, Domain, Codomain>, IGroup<Func>
    where Domain : IEquatable<Domain>
{
    public static Func Zero => throw new InvalidOperationException();


    /// <inheritdoc cref="Evaluate(Domain)"/>
    public Codomain? this[Domain x] => Evaluate(x);

    /// <summary>
    /// Returns the cached variant of this function instance
    /// </summary>
    public abstract FunctionCache<Func, Domain, Codomain> Cached { get; }

    public Func AdditiveInverse => Negate();

    public virtual bool IsZero => false;

    public bool IsNonZero => !IsZero;


    public virtual bool Is([MaybeNull] Func? other) => Equals(other);

    public virtual bool IsNot([MaybeNull] Func? other) => !Is(other);

    public abstract bool Equals(Func? other);

    int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => GetHashCode();

    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => Equals(other);

    /// <summary>
    /// Evaluates the current function at the given X value
    /// </summary>
    /// <param name="x">X value</param>
    /// <returns>Function evaluated at X</returns>
    public abstract Codomain? Evaluate(Domain x);

    public abstract Func Negate();

    public abstract Func Add(in Func second);

    public virtual Func Add(params Func[] others) => others.Aggregate((Func)this, (x, y) => x + y);

    public virtual Func Subtract(in Func second) => Add(second.Negate());

    public virtual Func Subtract(params Func[] others) => others.Aggregate((Func)this, (x, y) => x - y);

    static Func IGroup<Func>.operator +(in Func function) => function;

    static Func IGroup<Func>.operator -(in Func function) => function.Negate();

    static Func INumericGroup<Func>.operator +(in Func first, in Func second) => first.Add(in second);

    static Func IGroup<Func>.operator -(in Func first, in Func second) => first.Subtract(in second);

    //public static bool operator ==(Func? first, Func? second) => first?.Equals(second) ?? second is null;

    //public static bool operator !=(Func? first, Func? second) => !(first?.Equals(second) ?? second is null);
}

public class Function<Domain, Codomain>
    : Function<Function<Domain, Codomain>, Domain, Codomain>
    where Domain : IEquatable<Domain>
    where Codomain : IGroup<Codomain>
{
    private readonly Func<Domain, Codomain?> _func;


    public override FunctionCache<Function<Domain, Codomain>, Domain, Codomain> Cached => new(this);

    public override bool IsZero => Is(Zero!);

    public static new Function<Domain, Codomain> Zero { get; } = new(_ => default!);


    public Function()
        : this(default(Codomain))
    {
    }

    public Function(Codomain? constant)
        : this(_ => constant)
    {
    }

    public Function(Func<Domain, Codomain?> func) => _func = func;

    public override Codomain? Evaluate(Domain x) => _func(x);

    public Function<Domain, T> Then<T>(Function<Codomain, T> second)
        where T : IGroup<T> => new(x => second.Evaluate(Evaluate(x)));

    public override Function<Domain, Codomain> Negate() => new(x => _func(x).Negate());

    public override Function<Domain, Codomain> Add(in Function<Domain, Codomain> second)
    {
        Func<Domain, Codomain?> other = second._func;

        return new(x => _func(x) + other(x));
    }

    public Function<Domain, Codomain> Add(Codomain constant) => Add(new Function<Domain, Codomain>(constant));

    public Function<Domain, Codomain> Subtract(Codomain constant) => Subtract(new Function<Domain, Codomain>(constant));

    public override bool Equals(Function<Domain, Codomain>? other) => throw new NotImplementedException();

    public static Function<Domain, Codomain> FromDelegate(Func<Domain, Codomain?> f) => new(f);

    public static implicit operator Function<Domain, Codomain>(Func<Domain, Codomain?> f) => FromDelegate(f);

    public static implicit operator Func<Domain, Codomain?>(Function<Domain, Codomain> r) => r._func;

    public static implicit operator Function<Domain, Codomain>(Codomain s) => new(s);

    public static Function<Domain, Codomain> operator +(Function<Domain, Codomain> f) => f;

    public static Function<Domain, Codomain> operator -(Function<Domain, Codomain> f) => f.Negate();

    public static Function<Domain, Codomain> operator +(Function<Domain, Codomain> f, Codomain c) => f.Add(c);

    public static Function<Domain, Codomain> operator +(Codomain c, Function<Domain, Codomain> f) => new Function<Domain, Codomain>(c).Add(f);

    public static Function<Domain, Codomain> operator +(Function<Domain, Codomain> f1, Function<Domain, Codomain> f2) => f1.Add(f2);

    public static Function<Domain, Codomain> operator -(Function<Domain, Codomain> f, Codomain c) => f.Subtract(c);

    public static Function<Domain, Codomain> operator -(Codomain c, Function<Domain, Codomain> f) => new Function<Domain, Codomain>(c).Subtract(f);

    public static Function<Domain, Codomain> operator -(Function<Domain, Codomain> f1, Function<Domain, Codomain> f2) => f1.Subtract(f2);
}

public class Function<Domain>
    : Function<Domain, Domain>
    where Domain : IEquatable<Domain>, IGroup<Domain>
{
    public static Function<Domain> Identity { get; } = new(LINQ.id);


    public Function(Domain constant)
        : base(constant)
    {
    }

    public Function(Func<Domain, Domain> func)
        : base(func)
    {
    }
}

public class FieldFunction<Scalar>
    : Function<Scalar>
    , Algebra<Scalar>.IVectorSpace<FieldFunction<Scalar>>
    where Scalar : unmanaged, IField<Scalar>
{
    public static new FieldFunction<Scalar> Zero { get; } = new(Scalar.Zero);

    public static FieldFunction<Scalar> One { get; } = new(Scalar.One);

    FieldFunction<Scalar> IGroup<FieldFunction<Scalar>>.AdditiveInverse => Negate();


    public FieldFunction(Scalar constant)
        : base(constant)
    {
    }

    public FieldFunction(Func<Scalar, Scalar> func)
        : base(func)
    {
    }

    public override FieldFunction<Scalar> Negate() => (FieldFunction<Scalar>)base.Negate();

    public virtual FieldFunction<Scalar> Add(in FieldFunction<Scalar> second) => (FieldFunction<Scalar>)base.Add(this);

    public FieldFunction<Scalar> Add(params FieldFunction<Scalar>[] others) => (FieldFunction<Scalar>)base.Add(others); //.ToArray(f => f as Function<Scalar, Scalar>));

    public virtual FieldFunction<Scalar> Subtract(in FieldFunction<Scalar> second) => (FieldFunction<Scalar>)base.Subtract(this);

    public FieldFunction<Scalar> Subtract(params FieldFunction<Scalar>[] others) => (FieldFunction<Scalar>)base.Subtract(others); // .ToArray(f => f as Function<Scalar, Scalar>));

    public virtual FieldFunction<Scalar> Divide(Scalar factor) => new(x => Evaluate(x) / factor);

    public virtual FieldFunction<Scalar> Multiply(Scalar factor) => new(x => Evaluate(x) * factor);

    public virtual FieldFunction<Scalar> Modulus(Scalar factor) => new(x => Evaluate(x) % factor);

    public bool Equals(FieldFunction<Scalar>? other) => base.Equals(other);

    public bool Is(FieldFunction<Scalar>? other) => base.Is(other);

    public bool IsNot(FieldFunction<Scalar>? other) => !Is(other);

    public virtual bool IsLinearDependant(in FieldFunction<Scalar> other, out Scalar? factor) =>
        throw new InvalidOperationException("Linear dependency is not defined for arbitrary functions.");

    public virtual FieldFunction<Scalar> LinearInterpolate(in FieldFunction<Scalar> other, Scalar factor)
    {
        Func<Scalar, Scalar> _s = other.Evaluate;

        return new(x => _s(x) * factor + Evaluate(x) * (default(Scalar).Increment() - factor));
    }

    public static bool operator ==(FieldFunction<Scalar>? f1, FieldFunction<Scalar>? f2) => f1?.Equals(f2) ?? f2 is null;

    public static bool operator !=(FieldFunction<Scalar>? f1, FieldFunction<Scalar>? f2) => !(f1 == f2);

    public static FieldFunction<Scalar> operator +(in FieldFunction<Scalar> f) => f;

    public static FieldFunction<Scalar> operator -(in FieldFunction<Scalar> f) => f.Negate();

    public static FieldFunction<Scalar> operator +(in FieldFunction<Scalar> f1, in FieldFunction<Scalar> f2) => f1.Add(in f2);

    public static FieldFunction<Scalar> operator -(in FieldFunction<Scalar> f1, in FieldFunction<Scalar> f2) => f1.Subtract(in f2);

    public static FieldFunction<Scalar> operator *(in FieldFunction<Scalar> f, Scalar s) => f.Multiply(s);

    public static FieldFunction<Scalar> operator *(Scalar s, in FieldFunction<Scalar> f) => f.Multiply(s);

    public static FieldFunction<Scalar> operator /(in FieldFunction<Scalar> f, Scalar s) => f.Divide(s);

    public static FieldFunction<Scalar> operator %(in FieldFunction<Scalar> f, Scalar s) => f.Modulus(s);
}

public partial class ScalarFunction
    : FieldFunction<Scalar>
{
    public static new ScalarFunction Zero { get; } = new(_ => Scalar.Zero);

    public static new ScalarFunction Identity { get; } = new(LINQ.id);

    public override bool IsZero => Is(Zero);


    public ScalarFunction(Scalar constant)
        : base(constant)
    {
    }

    public ScalarFunction(Func<Scalar, Scalar> func)
        : base(func)
    {
    }

    public override ScalarFunction Negate() => new(x => Evaluate(x).Negate());

    public override ScalarFunction Add(in FieldFunction<Scalar> second)
    {
        Func<Scalar, Scalar> _other = second.Evaluate;

        return new(x => Evaluate(x).Add(_other(x)));
    }

    public override ScalarFunction Subtract(in FieldFunction<Scalar> second)
    {
        Func<Scalar, Scalar> _other = second.Evaluate;

        return new(x => Evaluate(x).Subtract(_other(x)));
    }

    public override ScalarFunction Multiply(Scalar factor) => new(x => Evaluate(x).Multiply(factor));

    public override ScalarFunction Divide(Scalar factor) => new(x => Evaluate(x).Divide(factor));

    public override ScalarFunction LinearInterpolate(in FieldFunction<Scalar> other, Scalar factor)
    {
        Func<Scalar, Scalar> _other = other.Evaluate;

        return new(x => Evaluate(x) * (1 - factor) + _other(x) * factor);
    }
}

public partial class ComplexFunction
    : FieldFunction<Complex>
{
    public static new ComplexFunction Zero { get; } = new(_ => Complex.Zero);

    public static new ComplexFunction Identity { get; } = new(LINQ.id);

    public override bool IsZero => Is(Zero);


    public ComplexFunction(Complex constant)
        : base(constant)
    {
    }

    public ComplexFunction(Func<Complex, Complex> func)
        : base(func)
    {
    }

    public override ComplexFunction Negate() => new(x => Evaluate(x).Negate());

    public override ComplexFunction Add(in FieldFunction<Complex> second)
    {
        Func<Complex, Complex> _other = second.Evaluate;

        return new(x => Evaluate(x).Add(_other(x)));
    }

    public override ComplexFunction Subtract(in FieldFunction<Complex> second)
    {
        Func<Complex, Complex> _other = second.Evaluate;

        return new(x => Evaluate(x).Subtract(_other(x)));
    }

    public override ComplexFunction Multiply(Complex factor) => new(x => Evaluate(x).Multiply(factor));

    public override ComplexFunction Divide(Complex factor) => new(x => Evaluate(x).Divide(factor));

    public override ComplexFunction LinearInterpolate(in FieldFunction<Complex> other, Complex factor)
    {
        Func<Complex, Complex> _other = other.Evaluate;

        return new(x => Evaluate(x) * (1 - factor) + _other(x) * factor);
    }
}

// public class VectorFieldFunction<VectorField, Scalar>
//     : Function<VectorField, Scalar>
//     , Algebra<Scalar>.IVectorSpace<VectorFieldFunction<VectorField, Scalar>>
//     where Scalar : unmanaged, IField<Scalar>
//     where VectorField : Algebra<Scalar>.IVectorSpace<VectorField>
// {
// }

public abstract class ContinuousFunction<DerivativeFunc, IntegralFunc, Domain, Codomain>
    : Function<Domain, Codomain> //, IField<Function>
    //where Func : ContinuousFunction<DerivativeFunc, IntegralFunc, Domain, Codomain>
    where DerivativeFunc : ContinuousFunction<DerivativeFunc, IntegralFunc, Domain, Codomain>
    where IntegralFunc : ContinuousFunction<DerivativeFunc, IntegralFunc, Domain, Codomain>
    where Domain : IEquatable<Domain>
    where Codomain : IGroup<Codomain>
{
    public abstract DerivativeFunc Derivative { get; }
    public abstract IntegralFunc Integral { get; }


    public ContinuousFunction(Func<Domain, Codomain?> func)
        : base(func)
    {
    }

    public ContinuousFunction(Codomain? constant)
        : base(constant)
    {
    }
}



// https://github.com/dotnet/runtime/issues/45344
// https://github.com/dotnet/runtime/issues/47007
