#nullable enable

using System.Runtime.CompilerServices;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using System.Collections;

namespace Unknown6656.Mathematics.Analysis
{
    public interface IRelation<Function, Index, Value>
        : IGroup<Function>
        where Function : IRelation<Function, Index, Value>
        where Index : IEquatable<Index>
    {
        /// <inheritdoc cref="Evaluate(Index)"/>
        Value this[Index x] { get; }
        /// <summary>
        /// Returns the cached variant of this function instance
        /// </summary>
        FunctionCache<Function, Index, Value> Cached { get; }
        /// <summary>
        /// Evaluates the current function at the given X value
        /// </summary>
        /// <param name="x">X value</param>
        /// <returns>Function evaluated at X</returns>
        Value Evaluate(Index x);
    }

    public interface IRelation<Function, Value>
        : IRelation<Function, Value, Value>
        where Function : IRelation<Function, Value>
        where Value : IEquatable<Value>
    {
    }

    public interface IVectorFieldFunction<Function, VectorField, Scalar>
        : IRelation<Function, VectorField>
        , IGroup<Function>
        where Function : IVectorFieldFunction<Function, VectorField, Scalar>
        where VectorField : unmanaged, Algebra<Scalar>.IVectorSpace<VectorField>
        where Scalar : unmanaged, IField<Scalar>
    {
    }

    public interface IFieldFunction<Function, Value>
        : IRelation<Function, Value>
        , IGroup<Function>
        , Algebra<Value>.IVectorSpace<Function>
        where Function : IFieldFunction<Function, Value>
        where Value : unmanaged, IField<Value>
    {
    }

    public interface IContinuousFieldFunction<Function, DerivativeFunc, IntegralFunc, Value>
        : IFieldFunction<Function, Value>
        , IField<Function>
        where Function : IContinuousFieldFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where DerivativeFunc : IFieldFunction<DerivativeFunc, Value>
        where IntegralFunc : IFieldFunction<IntegralFunc, Value>
        where Value : unmanaged, IField<Value>
    {
        DerivativeFunc Derivative { get; }
        IntegralFunc Integral { get; }
    }

    public class Relation<Value>
        : IRelation<Relation<Value>, Value>
        , IEquality<Relation<Value>>
        where Value : IGroup<Value>
    {
        private readonly Func<Value, Value> _func;


        public Value this[Value x] => Evaluate(x);

        public FunctionCache<Relation<Value>, Value, Value> Cached => new FunctionCache<Relation<Value>, Value, Value>(this);
        
        public virtual Relation<Value> AdditiveInverse => Negate();

        public virtual bool IsZero => false;
        
        public bool IsNonZero => !IsZero;


        public Relation(Func<Value, Value> function) => _func = function ?? throw new ArgumentNullException(nameof(function));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Relation<Value>? other) => _func == other?._func;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Relation<Value> o && Equals(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _func.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Value Evaluate(Value x) => _func(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Is(Relation<Value>? o) => Equals(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsNot(Relation<Value>? o) => !Is(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Relation<Value> Add(in Relation<Value> second)
        {
            Func<Value, Value> f = second._func;

            return new Relation<Value>(v => _func(v).Add(f(v)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Relation<Value> Add(params Relation<Value>[] others) => others.Aggregate(this, (x, y) => x.Add(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Relation<Value> Negate() => new Relation<Value>(v => Evaluate(v).AdditiveInverse);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Relation<Value> Subtract(in Relation<Value> second) => Add(second.Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Relation<Value> Subtract(params Relation<Value>[] others) => others.Aggregate(this, (x, y) => x.Subtract(y));


        public static Relation<Value> FromDelegate(Func<Value, Value> f) => new Relation<Value>(f);


        public static implicit operator Relation<Value>(Func<Value, Value> f) => new Relation<Value>(f);

        public static implicit operator Func<Value, Value>(Relation<Value> r) => r._func;
    }

    public abstract class VectorFieldFunction<Func, VectorField, Scalar>
        : Relation<VectorField>
        , IVectorFieldFunction<Func, VectorField, Scalar>
        where Func : VectorFieldFunction<Func, VectorField, Scalar>
        where VectorField : unmanaged, Algebra<Scalar>.IVectorSpace<VectorField>
        where Scalar : unmanaged, IField<Scalar>
    {
        #region PROPERTIES + FIELDS

        public new FunctionCache<Func, VectorField, VectorField> Cached => new FunctionCache<Func, VectorField, VectorField>((Func)this);

        public override Func AdditiveInverse => Negate();

        #endregion
        #region CONSTRUCTORS

        public VectorFieldFunction(Func<VectorField, VectorField> function)
            : base(function)
        {
        }

        public VectorFieldFunction(VectorFieldFunction<Func, VectorField, Scalar> function)
            : this(function.Evaluate)
        {
        }

        public VectorFieldFunction(Func function)
            : this(function.Evaluate)
        {
        }

        public VectorFieldFunction(VectorField constant)
            : this(_ => constant)
        {
        }

        #endregion
        #region METHODS

        public override abstract Func Negate();

        public abstract Func Add(Func second);

        public abstract Func Add(VectorField constant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func INumericIGroup<Func>.Add(in Func second) => Add(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func IGroup<Func>.Subtract(in Func second) => Subtract(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(Func second) => Add(second.Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(VectorField constant) => Add(constant.Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Add(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Add(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Add(params VectorField[] constants) => constants.Aggregate((Func)this, (x, y) => x.Add(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Subtract(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(params VectorField[] constants) => constants.Aggregate((Func)this, (x, y) => x.Subtract(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Multiply(Scalar factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Divide(Scalar factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Multiply(params Scalar[] factors) => factors.Aggregate((Func)this, (x, y) => x.Multiply(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Divide(params Scalar[] factors) => factors.Aggregate((Func)this, (x, y) => x.Divide(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func LinearInterpolate(Func other, Scalar factor) => Multiply(default(Scalar).Increment().Subtract(factor)).Add(other.Multiply(factor));

        bool IEquality<Func>.Is(Func? o) => Is(o);

        bool IEquality<Func>.IsNot(Func? o) => IsNot(o);

        bool IEquatable<Func>.Equals(Func? other) => Equals(other);

#pragma warning disable IDE0004
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => ((IStructuralEquatable)(Relation<VectorField>)this).Equals(other, comparer);

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => ((IStructuralEquatable)(Relation<VectorField>)this).GetHashCode(comparer);
#pragma warning restore IDE0004

        #endregion
        #region OPERATORS

        public static implicit operator Func<VectorField, VectorField>(VectorFieldFunction<Func, VectorField, Scalar> f) => f.Evaluate;

        #endregion
    }

    public class VectorFieldFunction<VectorField, Scalar>
        : VectorFieldFunction<VectorFieldFunction<VectorField, Scalar>, VectorField, Scalar>
        where VectorField : unmanaged, Algebra<Scalar>.IVectorSpace<VectorField>
        where Scalar : unmanaged, IField<Scalar>
    {
        public VectorFieldFunction(VectorField constant)
            : base(constant)
        {
        }

        public VectorFieldFunction(Func<VectorField, VectorField> function)
            : base(function)
        {
        }

        public VectorFieldFunction(VectorFieldFunction<VectorField, Scalar> function)
            : base(function)
        {
        }


        public override VectorFieldFunction<VectorField, Scalar> Negate() => new(x => this[x].Negate());

        public override VectorFieldFunction<VectorField, Scalar> Add(VectorFieldFunction<VectorField, Scalar> second) => new(x => this[x].Add(second[x]));

        public override VectorFieldFunction<VectorField, Scalar> Subtract(VectorFieldFunction<VectorField, Scalar> second) => new(x => this[x].Subtract(second[x]));

        public override VectorFieldFunction<VectorField, Scalar> Add(VectorField constant) => new(x => this[x].Add(constant));

        public override VectorFieldFunction<VectorField, Scalar> Subtract(VectorField constant) => new(x => this[x].Subtract(constant));

        public override VectorFieldFunction<VectorField, Scalar> Divide(Scalar factor) => new(x => this[x].Divide(factor));

        public override VectorFieldFunction<VectorField, Scalar> Multiply(Scalar factor) => new(x => this[x].Multiply(factor));


        public static implicit operator VectorFieldFunction<VectorField, Scalar>(Func<VectorField, VectorField> func) => new(func);
    }

    public abstract class AbstractFieldFunction<Func, Value>
        : Relation<Value>
        , IFieldFunction<Func, Value>
        where Func : AbstractFieldFunction<Func, Value>
        where Value : unmanaged, IField<Value>
    {
        #region PROPERTIES + FIELDS

        public new FunctionCache<Func, Value, Value> Cached => new FunctionCache<Func, Value, Value>((Func)this);

        public override Func AdditiveInverse => Negate();

        public override abstract bool IsZero { get; }

        #endregion
        #region CONSTRUCTORS

        public AbstractFieldFunction(AbstractFieldFunction<Func, Value> function)
            : this(function.Evaluate)
        {
        }

        public AbstractFieldFunction(Func<Value, Value> function)
            : base(function)
        {
        }

        public AbstractFieldFunction(Func function)
            : this(function as AbstractFieldFunction<Func, Value>)
        {
        }

        public AbstractFieldFunction(Value constant)
            : this(_ => constant)
        {
        }

        #endregion
        #region METHODS

        public abstract override Func Negate();

        public abstract Func Add(Func second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func INumericIGroup<Func>.Add(in Func second) => Add(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func IGroup<Func>.Subtract(in Func second) => Subtract(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func Subtract(Func second) => Add(second.Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func Add(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Add(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func Subtract(params Func[] others) => others.Aggregate((Func)this, (x, y) => x.Subtract(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Multiply(Value factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Func Divide(Value factor);

        bool Algebra<Value>.IVectorSpace<Func>.IsLinearDependant(in Func other, out Value? factor) =>
            throw new InvalidOperationException("Linear dependency is not defined for arbitrary functions.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func Algebra<Value>.IVectorSpace<Func>.LinearInterpolate(in Func other, Value factor) => LinearInterpolate(other, factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Func LinearInterpolate(Func other, Value factor) => Multiply(default(Value).Increment().Subtract(factor)).Add(other.Multiply(factor));

        bool IEquality<Func>.Is(Func? o) => Is(o);

        bool IEquality<Func>.IsNot(Func? o) => IsNot(o);

        bool IEquatable<Func>.Equals(Func? other) => Equals(other);

#pragma warning disable IDE0004
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => ((IStructuralEquatable)(Relation<Value>)this).Equals(other, comparer);

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => ((IStructuralEquatable)(Relation<Value>)this).GetHashCode(comparer);
#pragma warning restore IDE0004

        #endregion
        #region OPERATORS

        public static implicit operator Func<Value, Value>(AbstractFieldFunction<Func, Value> f) => f.Evaluate;

        public static explicit operator Func(AbstractFieldFunction<Func, Value> f) => (Func)f;

        #endregion
    }

    public abstract class ContinuousFieldFunction<Function, DerivativeFunc, IntegralFunc, Value>
        : AbstractFieldFunction<Function, Value>
        , IContinuousFieldFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where Function : ContinuousFieldFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where DerivativeFunc : IFieldFunction<DerivativeFunc, Value>
        where IntegralFunc : IFieldFunction<IntegralFunc, Value>
        where Value : unmanaged, IField<Value>
    {
        #region PROPERTIES

        public abstract DerivativeFunc Derivative { get; }

        public abstract IntegralFunc Integral { get; }

        public abstract Function MultiplicativeInverse { get; }

        public abstract bool IsInvertible { get; }

        public virtual bool IsOne => Decrement().IsZero;

        #endregion
        #region CONSTRUCTORS

        public ContinuousFieldFunction(Func<Value, Value> function)
            : base(function)
        {
        }

        public ContinuousFieldFunction(AbstractFieldFunction<Function, Value> function)
            : base(function)
        {
        }

        public ContinuousFieldFunction(Function function)
            : base(function)
        {
        }

        public ContinuousFieldFunction(Value constant)
            : base(constant)
        {
        }

        #endregion
        #region METHODS

        public override abstract Value Evaluate(Value x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Function Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Function Decrement();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Function IRing<Function>.Multiply(in Function second) => Multiply(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Function Multiply(Function second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Function Multiply(params Function[] others) => others.Aggregate((Function)this, (x, y) => x.Multiply(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Function IField<Function>.Divide(in Function second) => Divide(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Function Divide(Function second) => Multiply(second.MultiplicativeInverse);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Function Power(int e) => e >= 0 ? Multiply(Enumerable.Repeat((Function)this, e - 1).ToArray()) : Power(-e).MultiplicativeInverse;

        #endregion
    }

    public abstract class ScalarMap<Function, Scalar>
        : AbstractFieldFunction<Function, Scalar>
        where Function : ScalarMap<Function, Scalar>
        where Scalar : unmanaged, IField<Scalar>
    {
        #region PROPERTIES + FIELDS

        private static readonly Func<Func<Scalar, Scalar>, Function> _create;

        public static ScalarMap<Function, Scalar> Zero { get; }

        public override bool IsZero => Is((Function)Zero);

        #endregion
        #region CONSTRUCTORS

        static ScalarMap()
        {
            Type F = typeof(Function);

            if (F.GetConstructor(new[] { typeof(Func<Scalar, Scalar>) }) is { } ctor)
                _create = f => (Function)ctor.Invoke(new object[] { f });
            else
                throw new InvalidOperationException($"The type parameter '{F}' cannot be used as function type, as it has no constructor accepting an function of the type '{typeof(Scalar)} -> {typeof(Scalar)}'  ('{typeof(Func<Scalar, Scalar>)}').");

            Zero = default(Scalar);
        }

        public ScalarMap(Func<Scalar, Scalar> function)
            : base(function)
        {
        }

        public ScalarMap(ScalarMap<Function, Scalar> function)
            : base(function)
        {
        }

        public ScalarMap(Function function)
            : this((ScalarMap<Function, Scalar>)function)
        {
        }

        public ScalarMap(Scalar constant)
            : this(constant.IsZero ? Zero : (ScalarMap<Function, Scalar>)(_ => constant))
        {
        }

        #endregion
        #region METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function Negate() => _create(x => Evaluate(x).Negate());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function Add(Function second) => _create(x => Evaluate(x).Add(second.Evaluate(x)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function Subtract(Function second) => _create(x => Evaluate(x).Subtract(second.Evaluate(x)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function Multiply(Scalar factor) => _create(x => Evaluate(x).Multiply(factor));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function Divide(Scalar factor) => _create(x => Evaluate(x).Divide(factor));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Function LinearInterpolate(Function other, Scalar factor) => _create(x => Evaluate(x).Multiply(default(Scalar).Increment().Subtract(factor)).Add(other.Evaluate(x).Multiply(factor)));

        #endregion
        #region OPERATORS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScalarMap<Function, Scalar>(Scalar s) => _create(_ => s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScalarMap<Function, Scalar>(Func<Scalar, Scalar> f) => _create(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Func<Scalar, Scalar>(ScalarMap<Function, Scalar> f) => f.Evaluate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Function(ScalarMap<Function, Scalar> f) => _create(f.Evaluate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScalarMap<Function, Scalar> operator +(ScalarMap<Function, Scalar> f) => f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator -(ScalarMap<Function, Scalar> f) => f.Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator +(ScalarMap<Function, Scalar> f1, ScalarMap<Function, Scalar> f2) => f1.Add(f2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator -(ScalarMap<Function, Scalar> f1, ScalarMap<Function, Scalar> f2) => f1.Subtract(f2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator *(ScalarMap<Function, Scalar> f, Scalar s) => f.Multiply(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator *(Scalar s, ScalarMap<Function, Scalar> f) => f.Multiply(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Function operator /(ScalarMap<Function, Scalar> f, Scalar s) => f.Divide(s);

        #endregion
    }

    //public class LambdaFunction<T>
    //    : AbstractFieldFunction<LambdaFunction<T>, T>
    //    where T : unmanaged, IField<T>
    //{
    //    public Func<T, T> Lambda { get; }
    //
    //    public override bool IsZero { get; } = false;
    //
    //
    //    public LambdaFunction(Func<T, T> lambda) : base(lambda) => Lambda = lambda;
    //
    //    public override LambdaFunction<T> Add(LambdaFunction<T> second) => new(v => this[v].Add(second[v]));
    //
    //    public override LambdaFunction<T> Divide(T factor) => new(v => this[v].Divide(factor));
    //
    //    public override LambdaFunction<T> Multiply(T factor) => new(v => this[v].Multiply(factor));
    //
    //    public override LambdaFunction<T> Negate() => new(v => this[v].Negate());
    //}

    public class ScalarMap
        : ScalarMap<ScalarMap, Scalar>
    {
        public ScalarMap(Func<Scalar, Scalar> function)
            : base(function)
        {
        }
    }

    public class ScalarMap<T>
        : ScalarMap<ScalarMap<T>, Scalar<T>>
        where T : unmanaged, IComparable<T>
    {
        public ScalarMap(Func<Scalar<T>, Scalar<T>> function)
            : base(function)
        {
        }
    }

    public class ComplexMap
        : ScalarMap<ComplexMap, Complex>
    {
        public ComplexMap(Func<Complex, Complex> function)
            : base(function)
        {
        }
    }
}
