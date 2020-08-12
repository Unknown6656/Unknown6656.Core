#nullable enable

using System.Runtime.CompilerServices;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;


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

    public interface IFunction<Function, Value>
        : IRelation<Function, Value>
        , IGroup<Function>
        , Algebra<Value>.IVectorSpace<Function>
        where Function : IFunction<Function, Value>
        where Value : unmanaged, IField<Value>
    {
    }

    public interface IContinousFunction<Function, DerivativeFunc, IntegralFunc, Value>
        : IFunction<Function, Value>
        , IField<Function>
        where Function : IContinousFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where DerivativeFunc : IFunction<DerivativeFunc, Value>
        where IntegralFunc : IFunction<IntegralFunc, Value>
        where Value : unmanaged, IField<Value>
    {
        DerivativeFunc Derivative { get; }
        IntegralFunc Integral { get; }
    }

    public class Relation<Value>
        : IRelation<Relation<Value>, Value>
        where Value : IGroup<Value>
    {
        private readonly Func<Value, Value> _func;


        public Value this[Value x] => _func(x);

        public FunctionCache<Relation<Value>, Value, Value> Cached => new FunctionCache<Relation<Value>, Value, Value>(this);
        
        public Relation<Value> AdditiveInverse => Negate();
        
        public bool IsZero => false;
        
        public bool IsNonZero => true;


        public Relation(Func<Value, Value> f) => _func = f;

        public Relation<Value> Add(in Relation<Value> second)
        {
            Func<Value, Value> f = second._func;

            return new Relation<Value>(v => _func(v).Add(f(v)));
        }

        public Relation<Value> Add(params Relation<Value>[] others) => others.Aggregate(this, (x, y) => x.Add(y));

        public bool Equals(Relation<Value>? other) => _func == other?._func;

        public Value Evaluate(Value x) => _func(x);
        
        public bool Is(Relation<Value> o) => Equals(o);
        
        public bool IsNot(Relation<Value> o) => !Is(o);
        
        public Relation<Value> Negate() => new Relation<Value>(v => _func(v).AdditiveInverse);

        public Relation<Value> Subtract(in Relation<Value> second) => Add(second.Negate());

        public Relation<Value> Subtract(params Relation<Value>[] others) => others.Aggregate(this, (x, y) => x.Subtract(y));


        public static Relation<Value> FromDelegate(Func<Value, Value> f) => new Relation<Value>(f);


        public static implicit operator Relation<Value>(Func<Value, Value> f) => new Relation<Value>(f);

        public static implicit operator Func<Value, Value>(Relation<Value> r) => r._func;
    }

    public abstract class Function<Func, Value>
        : IFunction<Func, Value>
        where Func : Function<Func, Value>
        where Value : unmanaged, IField<Value>
    {
        #region PROPERTIES + FIELDS

        private protected Func<Value, Value> _func { get; set; }


        public Value this[Value x] => Evaluate(x);

        public virtual FunctionCache<Func, Value, Value> Cached => new FunctionCache<Func, Value, Value>((Func)this);

        public Func AdditiveInverse => Negate();

        public abstract bool IsZero { get; }

        public virtual bool IsNonZero => !IsZero;

        #endregion
        #region CONSTRUCTORS

        public Function(Func<Value, Value> function) => _func = function ?? throw new ArgumentNullException(nameof(function));

        public Function(Function<Func, Value> function)
            : this(function._func)
        {
        }

        public Function(Func function)
            : this(function as Function<Func, Value>)
        {
        }

        public Function(Value constant)
            : this(_ => constant)
        {
        }

        #endregion
        #region METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Value Evaluate(Value x) => _func(x);

        public abstract Func Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Func INumericIGroup<Func>.Add(in Func second) => Add(second);

        public abstract Func Add(Func second);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Is(Func o) => _func.Equals(o._func);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNot(Func o) => !Is(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Func other) => Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Func o && Equals(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _func.GetHashCode();

        #endregion
        #region OPERATORS

        public static implicit operator Func<Value, Value>(Function<Func, Value> f) => f._func;

        public static explicit operator Func(Function<Func, Value> f) => (Func)f;

        #endregion
    }

    public abstract class ContinousFunction<Function, DerivativeFunc, IntegralFunc, Value>
        : Function<Function, Value>
        , IContinousFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where Function : ContinousFunction<Function, DerivativeFunc, IntegralFunc, Value>
        where DerivativeFunc : IFunction<DerivativeFunc, Value>
        where IntegralFunc : IFunction<IntegralFunc, Value>
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

        public ContinousFunction(Func<Value, Value> function)
            : base(function)
        {
        }

        public ContinousFunction(Function<Function, Value> function)
            : base(function)
        {
        }

        public ContinousFunction(Function function)
            : base(function)
        {
        }

        public ContinousFunction(Value constant)
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
        : Function<Function, Scalar>
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
        public static implicit operator Func<Scalar, Scalar>(ScalarMap<Function, Scalar> f) => f._func;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Function(ScalarMap<Function, Scalar> f) => _create(f._func);

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
