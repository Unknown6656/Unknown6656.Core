#nullable enable

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Analysis
{
    public abstract class Function<Func, Domain, Codomain>
        : IGroup<Func>
        , IStructuralEquatable
        where Func : Function<Func, Domain, Codomain>
        where Domain : IEquatable<Domain>
    {
        /// <inheritdoc cref="Evaluate(Domain)"/>
        public Codomain this[Domain x] => Evaluate(x);

        /// <summary>
        /// Returns the cached variant of this function instance
        /// </summary>
        public abstract FunctionCache<Func, Domain, Codomain> Cached { get; }

        /// <inheritdoc/>
        public Func AdditiveInverse => Negate();

        /// <inheritdoc/>
        public virtual bool IsZero => false;

        /// <inheritdoc/>
        public bool IsNonZero => !IsZero;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Is([MaybeNull] Func? other) => Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsNot([MaybeNull] Func? other) => !Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool Equals(Func? other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => Equals(other);

        /// <summary>
        /// Evaluates the current function at the given X value
        /// </summary>
        /// <param name="x">X value</param>
        /// <returns>Function evaluated at X</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Codomain Evaluate(Domain x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Add(in Func second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Add(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Add(in y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(in Func second) => Add(second.Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Subtract(in y));
    }

    public class Function<Domain, Codomain>
        : Function<Function<Domain, Codomain>, Domain, Codomain>
        where Domain : IEquatable<Domain>
        where Codomain : IGroup<Codomain>
    {
        private readonly Func<Domain, Codomain> _func;


        public override FunctionCache<Function<Domain, Codomain>, Domain, Codomain> Cached => new(this);

        public override bool IsZero => Is(Zero!);

        public static Function<Domain, Codomain> Zero { get; } = new(_ => default!);


        public Function(Func<Domain, Codomain> func) => _func = func;

        public Function(Codomain constant)
            : this(_ => constant)
        {
        }

        public override Codomain Evaluate(Domain x) => _func(x);

        public override Function<Domain, Codomain> Negate() => new(x => Evaluate(x).Negate());

        public override Function<Domain, Codomain> Add(in Function<Domain, Codomain> second)
        {
            Func<Domain, Codomain> other = second.Evaluate;

            return new(x => Evaluate(x).Add(other(x)));
        }

        public Function<Domain, Codomain> Add(Codomain constant) => Add(new Function<Domain, Codomain>(constant));

        public Function<Domain, Codomain> Subtract(Codomain constant) => Subtract(new Function<Domain, Codomain>(constant));

        public override bool Equals(Function<Domain, Codomain>? other) => throw new NotImplementedException();

        public static Function<Domain, Codomain> FromDelegate(Func<Domain, Codomain> f) => new(f);

        public static implicit operator Function<Domain, Codomain>(Func<Domain, Codomain> f) => FromDelegate(f);

        public static implicit operator Func<Domain, Codomain>(Function<Domain, Codomain> r) => r._func;

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
        FieldFunction<Scalar> IGroup<FieldFunction<Scalar>>.AdditiveInverse => (FieldFunction<Scalar>)Negate();


        public FieldFunction(Scalar constant)
            : base(constant)
        {
        }

        public FieldFunction(Func<Scalar, Scalar> func)
            : base(func)
        {
        }

        FieldFunction<Scalar> IGroup<FieldFunction<Scalar>>.Negate() => (FieldFunction<Scalar>)Negate();

        public virtual FieldFunction<Scalar> Add(in FieldFunction<Scalar> second) => (FieldFunction<Scalar>)base.Add(this);

        public FieldFunction<Scalar> Add(params FieldFunction<Scalar>[] others) => (FieldFunction<Scalar>)base.Add(others); //.ToArray(f => f as Function<Scalar, Scalar>));

        public virtual FieldFunction<Scalar> Subtract(in FieldFunction<Scalar> second) => (FieldFunction<Scalar>)base.Subtract(this);

        public FieldFunction<Scalar> Subtract(params FieldFunction<Scalar>[] others) => (FieldFunction<Scalar>)base.Subtract(others); // .ToArray(f => f as Function<Scalar, Scalar>));

        public virtual FieldFunction<Scalar> Divide(Scalar factor) => new(x => Evaluate(x).Divide(in factor));

        public virtual FieldFunction<Scalar> Multiply(Scalar factor) => new(x => Evaluate(x).Multiply(in factor));

        public bool Equals(FieldFunction<Scalar>? other) => base.Equals(other);
        
        public bool Is([MaybeNull] FieldFunction<Scalar> other) => base.Is(other);

        public bool IsNot([MaybeNull] FieldFunction<Scalar> other) => !Is(other);

        public virtual bool IsLinearDependant(in FieldFunction<Scalar> other, out Scalar? factor) => throw new InvalidOperationException("Linear dependency is not defined for arbitrary functions.");

        public virtual FieldFunction<Scalar> LinearInterpolate(in FieldFunction<Scalar> other, Scalar factor)
        {
            Func<Scalar, Scalar> _s = other.Evaluate;

            return new(x => _s(x).Multiply(in factor).Add(Evaluate(x).Multiply(factor.Negate().Add(default(Scalar).Increment()))));
        }

        public static FieldFunction<Scalar> operator *(FieldFunction<Scalar> f, Scalar s) => f.Multiply(s);

        public static FieldFunction<Scalar> operator *(Scalar s, FieldFunction<Scalar> f) => f.Multiply(s);

        public static FieldFunction<Scalar> operator /(FieldFunction<Scalar> f, Scalar s) => f.Divide(s);
    }

    public class ScalarFunction
        : FieldFunction<Scalar>
    {
        public static new ScalarFunction Zero { get; } = new(_ => Scalar.Zero);

        public override bool IsZero => Is(Zero);


        public ScalarFunction(Scalar constant)
            : base(constant)
        {
        }

        public ScalarFunction(Func<Scalar, Scalar> func)
            : base(func)
        {
        }

        public override FieldFunction<Scalar> Negate() => new ScalarFunction(x => Evaluate(x).Negate());

        public override FieldFunction<Scalar> Add(in FieldFunction<Scalar> second)
        {
            Func<Scalar, Scalar> _other = second.Evaluate;

            return new ScalarFunction(x => Evaluate(x).Add(_other(x)));
        }

        public override FieldFunction<Scalar> Subtract(in FieldFunction<Scalar> second)
        {
            Func<Scalar, Scalar> _other = second.Evaluate;

            return new ScalarFunction(x => Evaluate(x).Subtract(_other(x)));
        }
        
        public override FieldFunction<Scalar> Multiply(Scalar factor) => new ScalarFunction(x => Evaluate(x).Multiply(factor));

        public override FieldFunction<Scalar> Divide(Scalar factor) => new ScalarFunction(x => Evaluate(x).Divide(factor));

        public override FieldFunction<Scalar> LinearInterpolate(in FieldFunction<Scalar> other, Scalar factor)
        {
            Func<Scalar, Scalar> _other = other.Evaluate;

            return new ScalarFunction(x => Evaluate(x) * (1 - factor) + _other(x) * factor);
        }
    }

    public class ComplexFunction
        : FieldFunction<Complex>
    {
        public ComplexFunction(Complex constant)
            : base(constant)
        {
        }

        public ComplexFunction(Func<Complex, Complex> func)
            : base(func)
        {
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


        public ContinuousFunction(Func<Domain, Codomain> func)
            : base(func)
        {
        }

        public ContinuousFunction(Codomain constant)
            : base(constant)
        {
        }
    }



    // https://github.com/dotnet/runtime/issues/45344
    // https://github.com/dotnet/runtime/issues/47007
}
