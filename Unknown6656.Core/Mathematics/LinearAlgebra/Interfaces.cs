// #define DEFAULT_IMPL
#define READONLY

#if DEFAULT_IMPL
using System.ComponentModel;
using System.Reflection;
#endif
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Generics;
using Unknown6656.IO;

namespace Unknown6656.Mathematics.LinearAlgebra;


// TODO : Use C# 10's abstract static !!
// TODO : Use C# 10's generic math !!

public interface IDisplayable
    : IFormattable
{
    string ToString();
    string ToShortString();
}

public interface ISerializable<@this>
    : INative<@this>
    , IDisplayable
    , ISerializable
    , IDeserializationCallback
    where @this : unmanaged, ISerializable<@this>
{
    // TODO
}

/// <summary>
/// Represents an interface containing basic equality comparison methods
/// </summary>
/// <typeparam name="Object">Generic data type</typeparam>
public interface IEquality<Object>
    : IEquatable<Object>
    , IStructuralEquatable
  //, IEqualityOperators<Object, Object, bool>
    where Object : IEquality<Object>
{
    /// <summary>
    /// Returns whether the given object is equal to the current instance.
    /// </summary>
    /// <param name="o">Object to compare to</param>
    /// <returns>Equality comparison result</returns>
    bool Is(Object? o);

    /// <summary>
    /// Returns whether the given object is unequal to the current instance.
    /// </summary>
    /// <param name="o">Object to compare to</param>
    /// <returns>Inequality comparison result</returns>
    bool IsNot(Object? o);

    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) =>
        other is Object o && comparer.GetHashCode(o) == comparer.GetHashCode(this);

    int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => comparer.GetHashCode(this);
}

public interface IGroup
{
    /// <summary>
    /// Indicates whether the current instance is equal to the zero element.
    /// </summary>
    bool IsZero { get; }

    /// <summary>
    /// Indicates whether the current instance is not equal to the zero element.
    /// </summary>
    bool IsNonZero
#if DEFAULT_IMPL
        => !IsZero;
#else
        { get; }
#endif
}

public interface INumericGroup<Group>
    : IGroup
    , IEquality<Group>
  //, IAdditionOperators<Group, Group, Group>
    where Group : INumericGroup<Group>
{
    /// <summary>
    /// Returns the groups's zero instance based upon the presence of a static member "Zero" or "Null".
    /// <br/>
    /// This is therefore euqal to "<see cref="Group"/>.Zero" or "<see cref="Group"/>.Null".
    /// </summary>
    public static abstract Group? Zero { get; }
    //[DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    //public static Group? Zero { get; } = (from p in typeof(Group).GetProperties(BindingFlags.Public | BindingFlags.Static)
    //                                      where p.DeclaringType != typeof(INumericIGroup<Group>)
    //                                      where p.Name.ToLowerInvariant() is "zero" or "null" or "nullelement"
    //                                      where typeof(INumericIGroup<Group>).IsAssignableFrom(p.PropertyType)
    //                                      select (Group)p.GetValue(null)).FirstOrDefault();

#if DEFAULT_IMPL
    /// <summary>
    /// <i>[AUTO-IMPLEMENTED]</i><br/>
    /// Indicates whether the current instance is equal to the zero element.
    /// </summary>
    bool IGroup.IsZero => Is(Zero);
#endif

    /// <summary>
    /// Adds the given object to the current instance and returns the addition's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Addition result</returns>
    Group Add(in Group second);

    Group Add(params Group[] others)
#if DEFAULT_IMPL
        => (Group)others.Aggregate(this, (t, n) => t.Add(n));
#else
        ;
#endif

    static abstract Group operator +(in Group first, in Group second);
}

/// <summary>
/// Represents an algebraic group containing a zero element (<see cref="Zero"/>), the notion of addition, and a notion of additive inversibility.
/// </summary>
/// <typeparam name="Group">Generic group data type</typeparam>
public interface IGroup<Group>
    : INumericGroup<Group>
    , IEquality<Group>
    where Group : IGroup<Group>
{
    /// <summary>
    /// Returns the current instance's additive inverse.
    /// </summary>
    Group AdditiveInverse
#if DEFAULT_IMPL
        => Negate();
#else
    { get; }
#endif


    /// <summary>
    /// Negates the current instance and returns the result without modifying the current instance.
    /// </summary>
    /// <returns>Negated object</returns>
    Group Negate();

    /// <summary>
    /// Subtracts the given object from the current instance and returns the substraction's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Subtraction result</returns>
    Group Subtract(in Group second)
#if DEFAULT_IMPL
        => Add(second.AdditiveInverse);
#else
        ;
#endif

    Group Subtract(params Group[] others)
#if DEFAULT_IMPL
        => (Group)others.Aggregate(this, (t, n) => t.Subtract(n));
#else
        ;
#endif

    static abstract Group operator +(in Group group);

    static abstract Group operator -(in Group group);

    static abstract Group operator -(in Group first, in Group second);
}

public interface IRing
    : IGroup
{
    /// <summary>
    /// Indicates whether the current instance is equal to the one element.
    /// </summary>
    bool IsOne { get; }
}

/// <summary>
/// Represents an algebraic ring containing an one element, a notion of multiplication.
/// <br/>
/// Any implementing class/structure should also expose the following members:
/// <para/>
/// <list type="bullet">
/// <item>'<code>static [T] Zero { get; }</code>' or '<code>static [T] Null { get; }</code>'</item>
/// <br/>
/// <item>'<code>static [T] One { get; }</code>' or '<code>static [T] Unit { get; }</code>' or '<code>static [T] Identity { get; }</code>'</item>
/// </list>
/// </summary>
/// <typeparam name="Ring">Generic ring data type</typeparam>
public interface IRing<Ring>
    : IRing
    , IGroup<Ring>
    where Ring : IRing<Ring>
{
    /// <summary>
    /// Returns the ring's one element. The element is neutral towards the ring's multiplication.
    /// </summary>
    public static abstract Ring? One { get; }
    //[DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    //Ring One => (from p in GetType().GetProperties(BindingFlags.Public | BindingFlags.Static)
    //                    where p.Name.ToLowerInvariant() is "one" or "identity" or "unit"
    //                    where typeof(IRing<Ring>).IsAssignableFrom(p.PropertyType)
    //                    select (Ring)p.GetValue(null)).FirstOrDefault();

#if DEFAULT_IMPL
    /// <summary>
    /// Indicates whether the current instance is equal to the one element.
    /// </summary>
    bool IRing.IsOne => Is(One);
#endif

    /// <summary>
    /// Multiplies the given object with the current instance and returns the multiplication's result without modifying the current instance.
    /// <para/>
    /// This method is not to be confused the dot-product for matrices and vectors.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Multiplication result</returns>
    Ring Multiply(in Ring second);

    Ring Multiply(params Ring[] others)
#if DEFAULT_IMPL
     => (Ring)others.Aggregate(this, (t, n) => t.Multiply(n));
#else
        ;
#endif

    /// <summary>
    /// Increments the current instance by one and returns the result. The current instance will not be modified.
    /// </summary>
    /// <returns>Incremented value</returns>
    Ring Increment()
#if DEFAULT_IMPL
        => Add(One);
#else
        ;
#endif

    /// <summary>
    /// Decrements the current instance by one and returns the result. The current instance will not be modified.
    /// </summary>
    /// <returns>Decremented value</returns>
    Ring Decrement()
#if DEFAULT_IMPL
        => Subtract(One);
#else
        ;
#endif

    /// <summary>
    /// Raises the current instance to the given power and returns the result without modifying the current instance.
    /// </summary>
    /// <param name="e">Positive or zero exponent</param>
    Ring Power(int e)
#if DEFAULT_IMPL
    {
        if (e < 0)
            throw new IndexOutOfRangeException("The given power must be greater or equal to zero.");

        Ring acc = One;

        for (int i = 0; i < e; ++i)
            acc = acc.Multiply((Ring)this);

        return acc;
    }
#else
        ;
#endif

    static abstract Ring operator ++(in Ring ring);

    static abstract Ring operator --(in Ring ring);

    static abstract Ring operator *(in Ring first, in Ring second);
}

public interface IField
    : IRing
{
    /// <summary>
    /// Indicates whether the current structure is invertible, meaning that a multiplicative inverse exists.
    /// </summary>
    bool IsInvertible { get; }
}

/// <summary>
/// Represents an algebraic field containing a notion of multiplicative inversibility.
/// </summary>
/// <typeparam name="Field">Generic field data type</typeparam>
public interface IField<Field>
    : IField
    , IRing<Field>
    where Field : IField<Field>
{
    /// <summary>
    /// Returns the multiplicative inverse of the current field.
    /// </summary>
    Field MultiplicativeInverse { get; }


    /// <summary>
    /// Divides the current instance by the given object and returns the division's result without modifying the current instance.
    /// The division is performed by multiplying the current instance with the multiplicative inverse of the given divisor.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Division result</returns>
    Field Divide(in Field second)
#if DEFAULT_IMPL
        => Multiply(second.MultiplicativeInverse);
#else
        ;
#endif

    Field Modulus(in Field second);

    /// <summary>
    /// Raises the current instance to the given power and returns the result without modifying the current instance.
    /// </summary>
    /// <param name="e">Exponent</param>
    new Field Power(int e)
#if DEFAULT_IMPL
        => e < 0 ? Power(Math.Abs(e)).MultiplicativeInverse : ((IRing<Field>)this).Power(e);
#else
        ;
#endif

    static abstract Field operator /(in Field first, in Field second);

    static abstract Field operator %(in Field first, in Field second);
}

public interface INumericRing<Field>
    : IRing<Field>
    , IComparable<Field>
    where Field : INumericRing<Field>
{
    /// <summary>
    /// Returns the scalar's absolute value.
    /// </summary>
    /// <returns>Absolute value</returns>
    Field Abs();

    /// <summary>
    /// Determines the minimum value of the current instance and the given scalar, and returns the smallest of both values.
    /// </summary>
    /// <param name="second">Second scalar</param>
    /// <returns>Minimum of both scalar values</returns>
    Field Min(Field second)
#if DEFAULT_IMPL
        => CompareTo(second) <= 0 ? (Field)this : second;
#else
        ;
#endif

    /// <summary>
    /// Determines the maximum value of the current instance and the given scalar, and returns the largest of both values.
    /// </summary>
    /// <param name="second">Second scalar</param>
    /// <returns>Maximum of both scalar values</returns>
    Field Max(Field second)
#if DEFAULT_IMPL
        => CompareTo(second) >= 0 ? (Field)this : second;
#else
        ;
#endif

    Field Clamp()
#if DEFAULT_IMPL
        => Clamp(Zero, One);
#else
        ;
#endif

    Field Clamp(Field low, Field high)
#if DEFAULT_IMPL
        => Max(low).Min(high);
#else
        ;
#endif
}

public interface IScalar
    : IField
{
    bool IsNaN { get; }
    bool IsNegative { get; }
    bool IsPositive { get; }
    bool IsNegativeInfinity { get; }
    bool IsPositiveInfinity { get; }
    bool IsInfinity { get; }
    bool IsFinite { get; }
    bool IsBinary { get; }
}

public interface INumericScalar<Field>
    : INumericRing<Field>
    where Field : INumericScalar<Field>
{
    bool IsPrime { get; }

    Field[] PrimeFactors { get; }

    Field Phi
#if DEFAULT_IMPL
        => PrimeFactors is { Length: 2 } f ? f[0].Decrement().Multiply(f[1].Decrement()) : throw new InvalidOperationException($"φ({this}) is not defined.");
#else
        { get; }
#endif

    (Field P, Field Q)? DecomposePQ(Field phi);
}

/// <summary>
/// Represents a scalar-like algebraic field.
/// </summary>
/// <typeparam name="Field">Generic scalar data type</typeparam>
public interface IScalar<Field>
    : INumericScalar<Field>
    , IScalar
    , IField<Field>
    , IComparable<Field>
    where Field : IScalar<Field>
{
    static abstract Field NaN { get; }

    static abstract Field NegativeInfinity { get; }

    static abstract Field PositiveInfinity { get; }

    static abstract Field MinValue { get; }

    static abstract Field MaxValue { get; }


    int Sign
#if DEFAULT_IMPL
         => CompareTo(Subtract((Field)this));
#else
    { get; }
#endif

    Field DecimalPlaces { get; }

    Field Floor { get; }

    Field Ceiling
#if DEFAULT_IMPL
        => IsInteger ? Floor : Floor.Increment();
#else
    { get; }
#endif

    Field Rounded { get; }

    bool IsInteger { get; }

    /// <summary>
    /// Calculates the square root of the scalar and returns the result. The current instance will not be modified.
    /// </summary>
    /// <returns>Square root</returns>
    Field Sqrt();

    Field Acos();

    Field Asin();

    Field Atan();

    Field Acot();

    Field Asec();

    Field Acsc();

    Field Sinh();

    Field Cosh();

    Field Tanh();

    Field Coth();

    Field Sech();

    Field Csch();

    Field Asinh();

    Field Acosh();

    Field Atanh();

    Field Acoth();

    Field Asech();

    Field Acsch();

    Field Sin();

    Field Cos();

    Field Tan();

    Field Cot();

    Field Sec();

    Field Csc();

    Field Exp();

    Field Log();
}

/// <summary>
/// Represents a set of algebraic interfaces based on the given generic scalar value type.
/// </summary>
/// <typeparam name="Scalar">Generic Scalar value type</typeparam>
/// <typeparam name="raw">The underlying generic scalar value data type</typeparam>
public static class Algebra<Scalar>
    where Scalar : unmanaged, IField<Scalar>
{
    public interface IVectorSpace
        : IGroup
    {
        // TODO
    }

    /// <summary>
    /// Represents a vector space over the given generic algebraic group structure.
    /// </summary>
    /// <typeparam name="Vector">Generic group data type</typeparam>
    public interface IVectorSpace<Vector>
        : IVectorSpace
        , IGroup<Vector>
        where Vector : IVectorSpace<Vector>
    {
        /// <summary>
        /// Multiplies the given scalar factor with the current instance and returns the multiplication's result without modifying the current instance.
        /// </summary>
        /// <param name="factor">Scalar factor</param>
        /// <returns>Multiplication result</returns>
        Vector Multiply(Scalar factor);

        /// <summary>
        /// <summary>
        /// Divides the current instance by the given scalar factor and returns the division's result without modifying the current instance.
        /// </summary>
        /// <param name="factor">Scalar factor</param>
        /// <returns>Division result</returns>
        Vector Divide(Scalar factor)
#if DEFAULT_IMPL
            => Multiply(factor.MultiplicativeInverse);
#else
            ;
#endif

        Vector Modulus(Scalar factor);

        bool IsLinearDependant(in Vector other, out Scalar? factor);

        Vector LinearInterpolate(in Vector other, Scalar factor)
#if DEFAULT_IMPL
            => Multiply(factor.One.Subtract(factor)).Add(other.Multiply(factor));
#else
            ;
#endif

        static abstract Vector operator *(Scalar scalar, in Vector vector);

        static abstract Vector operator *(in Vector vector, Scalar scalar);

        static abstract Vector operator /(in Vector vector, Scalar scalar);

        static abstract Vector operator %(in Vector vector, Scalar scalar);
    }

    public interface IComposite
    {
        bool IsBinary { get; }

        bool IsNegative
#if DEFAULT_IMPL
             => Coefficients.All(c => c.IsNegative);
#else
        { get; }
#endif

        bool IsPositive
#if DEFAULT_IMPL
             => Coefficients.All(c => c.IsPositive);
#else
        { get; }
#endif

        bool HasNegatives
#if DEFAULT_IMPL
             => Coefficients.Any(c => c.IsNegative);
#else
        { get; }
#endif

        bool HasPositives
#if DEFAULT_IMPL
             => Coefficients.Any(c => c.IsPositive);
#else
        { get; }
#endif

        bool HasNaNs
#if DEFAULT_IMPL
             => Coefficients.Any(c => c.IsNaN);
#else
        { get; }
#endif

        /// <summary>
        /// The sum of the coefficients.
        /// </summary>
        Scalar CoefficientSum
#if DEFAULT_IMPL
             => Coefficients.Aggregate(new Scalar().Zero, (a, b) => a.Add(b));
#else
        { get; }
#endif

        /// <summary>
        /// The average of the coefficients.
        /// </summary>
        Scalar CoefficientAvg
#if DEFAULT_IMPL
             => Sum.Divide(Dimension);
#else
        { get; }
#endif

        /// <summary>
        /// The minimum value of the coefficients.
        /// </summary>
        Scalar CoefficientMin
#if DEFAULT_IMPL
             => Coefficients.OrderBy(Generics.id).FirstOrDefault();
#else
        { get; }
#endif

        /// <summary>
        /// The maximum value of the coefficients.
        /// </summary>
        Scalar CoefficientMax
#if DEFAULT_IMPL
            => Coefficients.OrderByDescending(Generics.id).FirstOrDefault();
#else
        { get; }
#endif
    }

    public interface IComposite<@this>
        : IComposite
        where @this : IComposite<@this>
    {
        @this ComponentwiseDivide(in @this second);

        @this ComponentwiseMultiply(in @this second);

        @this Clamp();

        @this Clamp(Scalar low, Scalar high);

        bool Is(@this other, Scalar tolerance);
    }

    public interface IComposite1D
        : IVectorSpace
        , IComposite
        , IGroup
    {
        /// <summary>
        /// Returns the coefficient stored at the given index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Coefficient</returns>
        Scalar this[int index]
#if DEFAULT_IMPL
             => Coefficients[index];
#else
            { get; }
#endif

        Scalar[] Coefficients { get; }

        int Dimension { get; }
    }

    public interface IComposite1D<Vector>
        : IComposite1D
        , IComposite<Vector>
        , IVectorSpace<Vector>
        where Vector : IComposite1D<Vector>
    {
        Vector this[int index, Scalar value] { get; }
    }

    public interface IComposite2D
        : IVectorSpace
        , IComposite
    {
        /// <summary>
        /// Returns the coefficient stored at the given indices.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns>Coefficient</returns>
        Scalar this[int column, int row]
#if DEFAULT_IMPL
             => Coefficients[column, row];
#else
             { get; }
#endif

        Scalar[,] Coefficients { get; }

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IEnumerable<Scalar> FlattenedCoefficients
#if DEFAULT_IMPL
             => Coefficients.Cast<Scalar>();
#else
             { get; }
#endif

        (int Columns, int Rows) Dimensions { get; }
    }

    public interface IComposite2D<Matrix>
        : IComposite2D
        , IComposite<Matrix>
        , IVectorSpace<Matrix>
        where Matrix : IComposite2D<Matrix>
    {
        Matrix this[int column, int row, Scalar value] { get; }
    }

    /// <summary>
    /// Represents an eucledian vector space over the given generic algebraic group structure.
    /// </summary>
    /// <typeparam name="Vector">Generic vector space data type</typeparam>
    public interface IEucledianVectorSpace<Vector>
        : IVectorSpace<Vector>
        , IComposite1D<Vector>
        where Vector : IEucledianVectorSpace<Vector>
    {
        /// <summary>
        /// Calculates the dot product of the current instance with the given second vector.
        /// </summary>
        /// <param name="second">Second vector</param>
        /// <returns>Dot product</returns>
        Scalar Dot(in Vector second)
#if DEFAULT_IMPL
             => ComponentwiseMultiply(second).Sum;
#else
            ;
#endif

        Scalar AngleTo(in Vector second);

        /// <summary>
        /// Returns whether the current instance is orthogonal to the given second vector.
        /// </summary>
        /// <param name="second">Second vector</param>
        /// <returns>Orthogonality check result</returns>
        bool IsOrthogonal(in Vector second)
#if DEFAULT_IMPL
             => Dot(second).IsZero;
#else
            ;
#endif

        /// <summary>
        /// Reflects the current vector instance at the given normal vector and returns the reflected vector. This does not modify the current instance.
        /// </summary>
        /// <param name="normal">Normal vector</param>
        /// <returns>Reflected vector</returns>
        Vector Reflect(in Vector normal)
#if DEFAULT_IMPL
        {
            Scalar θ = Dot(normal);

            return normal.Multiply(θ.Add(θ)).Subtract((Vector)this);
        }
#else
            ;
#endif

        /// <summary>
        /// Refracts the current vector instance at the given normal vector and returns the refracted vector. This does not modify the current instance.
        /// <para/>
        /// A return value of <see cref="false"/> indicates that the refraction was a total reflection
        /// </summary>
        /// <param name="normal">Normal vector</param>
        /// <param name="eta">Ratio of refractive indices</param>
        /// <param name="refracted">Refracted (or reflected) vector</param>
        /// <returns>Total reflection indicator</returns>
        bool Refract(in Vector normal, Scalar eta, out Vector refracted)
#if DEFAULT_IMPL
        {
            Scalar i = eta.One;
            Scalar θ = Dot(normal);
            Scalar k = i.Subtract(eta.Multiply(eta, i.Subtract(θ.Multiply(θ))));
            bool res = k.IsNegative;

            refracted = res ? Reflect(normal.Negate()) : Multiply(eta).Add(normal.Multiply(eta.Multiply(θ).Subtract(k.Sqrt())));

            return res;
        }
#else
            ;
#endif

        static abstract Scalar operator*(in Vector first, in Vector second);
    }

    public interface IMetricVectorSpace
        : IVectorSpace
        , IComposite1D
    {
        /// <summary>
        /// Indicates whether the vector is normalized.
        /// </summary>
        bool IsNormalized
#if DEFAULT_IMPL
            => Length.IsOne;
#else
            { get; }
#endif

        /// <summary>
        /// Indicates whether the vector is inside the unit sphere, meaning that the vector's length is smaller or equal to one.
        /// </summary>
        bool IsInsideUnitSphere { get; }

        /// <summary>
        /// Returns the metric length of the current vector.
        /// </summary>
        Scalar Length { get; }
    }

    /// <summary>
    /// Represents a metric vector which contains a notion of lengths.
    /// </summary>
    /// <typeparam name="Vector">Generic vector space data type</typeparam>
    public interface IMetricVectorSpace<Vector>
        : IMetricVectorSpace
        , IEucledianVectorSpace<Vector>
        where Vector : IMetricVectorSpace<Vector>
    {
        /// <summary>
        /// Returns the squared norm of the current vector. This is computed using the dot product with itself.
        /// </summary>
        Scalar SquaredNorm
#if !DEFAULT_IMPL
            { get; }
#else
           => Dot((Vector)this);

        Scalar IMetricVectorSpace.Length => SquaredNorm.Sqrt();
#endif

        /// <summary>
        /// Returns the normalized vector.
        /// </summary>
        Vector Normalized
#if DEFAULT_IMPL
             => Length is Scalar l && l.IsZero ? Zero : Divide(l);
#else
            { get; }
#endif

        /// <summary>
        /// Computes the metric distance between the current and the given vector.
        /// </summary>
        /// <param name="second">Second vector</param>
        /// <returns>Distance</returns>
        Scalar DistanceTo(in Vector second)
#if DEFAULT_IMPL
             => Subtract(second).Length;
#else
            ;
#endif
    }

    public interface IVector
        : IMetricVectorSpace
    {
        /// <summary>
        /// Returns the array representation of the vector.
        /// </summary>
        /// <returns>Flat array representation</returns>
        Scalar[] ToArray()
#if DEFAULT_IMPL
              => Coefficients;
#else
            ;
#endif
    }

    public interface IVector<Vector>
        : IVector
        , IMetricVectorSpace<Vector>
        where Vector : IVector<Vector>
    {
        // TODO : c-wise abs
        // TODO : c-wise sqrt
        // TODO : c-wise clamp
        // TODO : swap
        // TODO : set

        static abstract Vector FromArray(Scalar[] coefficients);
    }

    public interface IVector<Vector, Matrix>
        : IVector<Vector>
        where Vector : IVector<Vector, Matrix>
        where Matrix : IMatrix<Matrix>
    {
        /// <summary>
        /// The householder matrix represented by the current vector.
        /// </summary>
        Matrix HouseholderMatrix
        {
            get
#if DEFAULT_IMPL
            {
                if (IsZero)
                    throw new InvalidOperationException("The Householder matrix is undefined for zero vectors.");

                Scalar i = new Scalar().One;

                return OuterProduct((Vector)this).Multiply(i.Add(i).Divide(SquaredNorm));
            }
#else
            ;
#endif
        }

        /// <summary>
        /// Calculates the outer product of the current instance with the given vector and returns the result without modifying the current instance.
        /// </summary>
        /// <param name="second">Second vector</param>
        /// <returns>Outer product</returns>
        Matrix OuterProduct(in Vector second);
    }

    public interface ICrossableVector<Vector>
        : IVector<Vector>
        where Vector : ICrossableVector<Vector>
    {
        /// <summary>
        /// Computes the cross product of the current instance with the given one and returns the result without modifying the current instance.
        /// </summary>
        /// <param name="other">Second vector</param>
        /// <returns>Cross product</returns>
        Vector Cross(in Vector other);

        /// <summary>
        /// Computes the triple product of the current instance and the two given ones. The triple product is computed using the dot product of the current instance with the cross product of the two given vectors.
        /// </summary>
        /// <param name="y">Second vector</param>
        /// <param name="z">Third vector</param>
        /// <returns>Triple product</returns>
        Scalar TripleProduct(in Vector y, in Vector z)
#if DEFAULT_IMPL
            => Dot(y.Cross(z));
#else
            ;
#endif
    }

    public interface IMatrix
        : IRing
        , IComposite2D
    {
        /// <summary>
        /// The matrix' determinant.
        /// </summary>
        Scalar Determinant { get; }

        /// <summary>
        /// The matrix' trace.
        /// </summary>
        Scalar Trace { get; }

        bool IsPositiveDefinite { get; }

        /// <summary>
        /// Indicates whether the matrix is a diagonal matrix.
        /// </summary>
        bool IsDiagonal { get; }

        /// <summary>
        /// Indicates whether the matrix is an upper (right) triangular matrix.
        /// </summary>
        bool IsUpperTriangular { get; }

        /// <summary>
        /// Indicates whether the matrix is a lower (left) triangular matrix.
        /// </summary>
        bool IsLowerTriangular { get; }

        /// <summary>
        /// Indicates whether the matrix is symmetric.
        /// </summary>
        bool IsSymmetric { get; }

        /// <summary>
        /// Indicates whether the matrix is a projection matrix.
        /// </summary>
        bool IsProjection { get; }

        /// <summary>
        /// Indicates whether the current matrix 'A' is a conference matrix, meaning that AᵀA is a multiple of the identity matrix.
        /// </summary>
        bool IsConferenceMatrix { get; }

        /// <summary>
        /// Indicated whether the matrix is involutory, meaning that the square of this matrix is equal to the identity matrix.
        /// </summary>
        bool IsInvolutory { get; }

        /// <summary>
        /// Indicates whether the matrix is stable in the sense of the Hurwitz criterium.
        /// </summary>
        bool IsHurwitzStable { get; }

        /// <summary>
        /// Indicates whether the matrix is orthogonal.
        /// </summary>
        bool IsOrthogonal { get; }

        /// <summary>
        /// Indicates whether the matrix is skew symmetric.
        /// </summary>
        bool IsSkewSymmetric { get; }

        /// <summary>
        /// The matrix' characteristic polynomial.
        /// </summary>
        bool IsSignMatrix { get; }

        bool IsSignatureMatrix { get; }

        /// <summary>
        /// The matrix' eigenvalues.
        /// </summary>
        // ∀λ∈Spec(A) : Ax = λx
        Scalar[] Eigenvalues { get; }

        /// <summary>
        /// The rank of the matrix.
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Returns a set of principal submatrices.
        /// </summary>
        /// <returns>Set of principal submatrices</returns>
        IMatrix[] GetPrincipalSubmatrices();

        /// <summary>
        /// Returns the matrix as a flat array of matrix elements in column major format.
        /// </summary>
        /// <returns>Column major representation of the matrix</returns>
        Scalar[] ToArray();
    }

    public interface IMatrix<Matrix>
        : IMatrix
        , IRing<Matrix>
        , IComposite2D<Matrix>
        where Matrix : IMatrix<Matrix>
    {
        /// <summary>
        /// Returns the gaussian reduced matrix.
        /// </summary>
        Matrix GaussianReduced { get; }

        /// <summary>
        /// The matrix' orthonormal basis.
        /// </summary>
        Matrix OrthonormalBasis { get; }

        /// <summary>
        /// The transposed matrix.
        /// </summary>
        Matrix Transposed { get; }


        static abstract Matrix FromArray(Scalar[] coefficients);
    }

    public interface IMatrix<Vector, Matrix>
        : IMatrix<Matrix>
        where Matrix : IMatrix<Vector, Matrix>
        where Vector : IVector<Vector, Matrix>
    {
        /// <summary>
        /// Gets the matrix' column vector at the given index.
        /// </summary>
        /// <param name="column">Column vector index (zero-based)</param>
        /// <returns>Column vector</returns>
        Vector this[int column] { get; }

        /// <summary>
        /// Sets the matrix' column vector at the given index and returns the modified matrix.
        /// </summary>
        /// <param name="column">Column vector index (zero-based)</param>
        /// <param name="vector">New column vector</param>
        /// <returns>Modified matrix</returns>
        Matrix this[int column, in Vector vector] { get; }

        /// <summary>
        /// The matrix' main diagonal.
        /// </summary>
        Vector MainDiagonal { get; }

        /// <summary>
        /// The matrix' column vectors.
        /// </summary>
        Vector[] Columns { get; }

        /// <summary>
        /// The matrix' row vectors.
        /// </summary>
        Vector[] Rows { get; }


        /// <summary>
        /// Calculates the product of the current matrix with the given vector.
        /// </summary>
        /// <param name="vector">Vector</param>
        /// <returns>Product</returns>
        Vector Multiply(in Vector vector);

        /// <summary>
        /// Solves the current matrix for the given vector in a linear equation system and returns whether a solution could be found.
        /// </summary>
        /// <param name="vector">Input Vector</param>
        /// <param name="solution">Solution Vector</param>
        /// <returns>Solution indicator.</returns>
        bool Solve(Vector vector, out Vector solution);

        /// <summary>
        /// Gets the matrix' row vector at the given index.
        /// </summary>
        /// <param name="row">Row vector index (zero-based)</param>
        /// <returns>Row vector</returns>
        Vector GetRow(int row);

        /// <summary>
        /// Sets the matrix' row vector at the given index and returns the modified matrix.
        /// </summary>
        /// <param name="row">Row vector index (zero-based)</param>
        /// <param name="vector">New row vector</param>
        /// <returns>Modified matrix</returns>
        Matrix SetRow(int row, in Vector vector);

        /// <summary>
        /// Gets the matrix' column vector at the given index.
        /// </summary>
        /// <param name="column">Column vector index (zero-based)</param>
        /// <returns>Column vector</returns>
        Vector GetColumn(int column);

        /// <summary>
        /// Sets the matrix' column vector at the given index and returns the modified matrix.
        /// </summary>
        /// <param name="column">Column vector index (zero-based)</param>
        /// <param name="vector">New column vector</param>
        /// <returns>Modified matrix</returns>
        Matrix SetColumn(int column, in Vector vector);

        /// <summary>
        /// Multiplies a row with the given factor and returns the modified matrix.
        /// </summary>
        /// <param name="row">Zero-based row index</param>
        /// <param name="factor">Scalar multiplication factor</param>
        /// <returns>Modified matrix</returns>
        Matrix MultiplyRow(int row, Scalar factor)
#if DEFAULT_IMPL
             => SetRow(row, GetRow(row).Multiply(factor));
#else
            ;
#endif

        /// <summary>
        /// Swaps two rows and returns the modified matrix.
        /// </summary>
        /// <param name="src_row">Zero-based first row index</param>
        /// <param name="dst_row">Zero-based second row index</param>
        /// <returns>Modified matrix</returns>
        Matrix SwapRows(int src_row, int dst_row)
#if DEFAULT_IMPL
        {
            Vector row = GetRow(src_row);

            return SetRow(src_row, GetRow(dst_row))
                  .SetRow(dst_row, row);
        }
#else
            ;
#endif

        /// <summary>
        /// Adds the given source row to the given destination row and returns the modified matrix.
        /// </summary>
        /// <param name="src_row">Zero-based source row index</param>
        /// <param name="dst_row">Zero-based destination row index</param>
        /// <returns>Modified matrix</returns>
        Matrix AddRows(int src_row, int dst_row)
#if DEFAULT_IMPL
             => AddRows(src_row, dst_row, new Scalar().One);
#else
            ;
#endif

        /// <summary>
        /// Adds the given source row (premultiplied with the given scalar factor) to the given destination row and returns the modified matrix.
        /// </summary>
        /// <param name="src_row">Zero-based source row index</param>
        /// <param name="dst_row">Zero-based destination row index</param>
        /// <param name="factor">Scalar factor</param>
        /// <returns>Modified matrix</returns>
        Matrix AddRows(int src_row, int dst_row, Scalar factor)
#if DEFAULT_IMPL
            => SetRow(dst_row, GetRow(src_row).Multiply(factor).Add(GetRow(dst_row)));
#else
            ;
#endif

        /// <summary>
        /// Multiplies a column with the given factor and returns the modified matrix.
        /// </summary>
        /// <param name="col">Zero-based column index</param>
        /// <param name="factor">Scalar factor</param>
        /// <returns>Modified matrix</returns>
        Matrix MultiplyColumn(int col, Scalar factor)
#if DEFAULT_IMPL
            => this[col, this[col].Multiply(factor)];
#else
            ;
#endif

        /// <summary>
        /// Swaps two columns and returns the modified matrix.
        /// </summary>
        /// <param name="src_col">Zero-based first column index</param>
        /// <param name="dst_col">Zero-based second column index</param>
        /// <returns>Modified matrix</returns>
        Matrix SwapColumns(int src_col, int dst_col)
#if DEFAULT_IMPL
        {
            Vector col = this[src_col];

            return this[src_col, this[dst_col]][dst_col, col];
        }
#else
            ;
#endif

        /// <summary>
        /// Adds the given source column to the given destination column and returns the modified matrix.
        /// </summary>
        /// <param name="src_col">Zero-based source coulmn index</param>
        /// <param name="dst_col">Zero-based destination column index</param>
        /// <returns>Modified matrix</returns>
        Matrix AddColumns(int src_col, int dst_col)
#if DEFAULT_IMPL
             => AddColumns(src_col, dst_col, new Scalar().One);
#else
            ;
#endif

        /// <summary>
        /// Adds the given source column (premultiplied with the given scalar factor) to the given destination column and returns the modified matrix.
        /// </summary>
        /// <param name="src_col">Zero-based source coulmn index</param>
        /// <param name="dst_col">Zero-based destination column index</param>
        /// <param name="factor">Scalar factor</param>
        /// <returns>Modified matrix</returns>
        Matrix AddColumns(int src_col, int dst_col, Scalar factor)
#if DEFAULT_IMPL
             => this[dst_col, this[src_col].Multiply(factor).Add(this[dst_col])];
#else
            ;
#endif
    }
}

/// <summary>
/// Represents a set of algebraic interfaces based on the given generic scalar value type and a given generic polynomial type.
/// </summary>
/// <typeparam name="Scalar">Generic Scalar value type</typeparam>
/// <typeparam name="Poly">Generic polynomial type</typeparam>
/// <typeparam name="raw">The underlying generic scalar value data type</typeparam>
public static class Algebra<Scalar, Poly>
    where Poly : Polynomial<Poly, Scalar>
    where Scalar : unmanaged, IField<Scalar>, INumericRing<Scalar>
{
    public interface IComposite1D
        : Algebra<Scalar>.IComposite1D
    {
        /// <summary>
        /// Converts the vector to its polynomial representation in ascending exponential order.
        /// <para/>
        /// The vector <code>(a,b,c,d, ...)</code> will be translated to the polynomial <code>a + bx + cx² + dx³ + ...</code>.
        /// </summary>
        /// <returns>Polynomial</returns>
        Poly ToPolynomial();
    }

    public interface IMatrix
        : Algebra<Scalar>.IMatrix
    {
        Poly CharacteristicPolynomial { get; }
    }

    public interface IMatrix<Matrix, SubMatrix>
        : Algebra<Scalar>.IMatrix<Matrix>
        , IMatrix
        where Matrix : IMatrix<Matrix, SubMatrix>
        where SubMatrix : Algebra<Scalar>.IMatrix<SubMatrix>, IMatrix<SubMatrix, SubMatrix>
    {
        SubMatrix GetRows(Range rows);

        Matrix SetRows(Range rows, in SubMatrix values);

        SubMatrix GetColumns(Range columns);

        Matrix SetColumns(Range columns, in SubMatrix values);

        SubMatrix GetRegion(Range columns, Range rows);

        Matrix SetRegion(Range columns, Range rows, in SubMatrix values);
    }
}

public sealed class ClosenessComparer<Scalar>
    : IEqualityComparer<Scalar>
    where Scalar : unmanaged, IField<Scalar>, IComparable<Scalar>
{
    public Scalar Delta { get; }


    public ClosenessComparer(Scalar max_delta) => Delta = max_delta;

    public bool Equals(Scalar x, Scalar y) => x.Add(y).Divide(2.ToRing<Scalar>().Subtract(y)).CompareTo(Delta) <= 0;

    public int GetHashCode(Scalar obj) => 0;
}

public static class InterfaceExtensions
{
    public static T ToRing<T>(this int i) where T : unmanaged, IRing<T> => i switch
    {
        >= 0 => default(T).ApplyRecursively(e => e.Increment(), i),
        _ => default(T).ApplyRecursively(e => e.Decrement(), -i)
    };

    public static int ToInt<T>(this T r) where T : unmanaged, IRing<T>, IComparable<T>
    {
        T zero = default;
        int c, v = 0;

        while ((c = r.CompareTo(zero)) != 0)
            if (c < 0)
            {
                r = r.Increment();
                ++v;
            }
            else
            {
                r = r.Decrement();
                --v;
            }

        return v;
    }

    public static T Constrain<T>(this T scalar, T min, T max) where T : unmanaged, INumericRing<T> => scalar.Min(max).Max(min);

    public static T Map<T>(this T scalar, (T lower, T upper) from, (T lower, T upper) to) where T : unmanaged, IField<T> =>
        scalar.Subtract(from.lower).Divide(from.upper.Subtract(from.lower)).Multiply(to.upper.Subtract(to.lower)).Add(to.lower);

    public static T ConstrainMap<T>(this T scalar, (T lower, T upper) from, (T lower, T upper) to) where T : unmanaged, IField<T>, INumericRing<T> => scalar.Constrain(from.lower, from.upper).Map(from, to);

    public static T Product<T>(this IEnumerable<T> scalars) where T : unmanaged, IRing<T> => scalars.Aggregate(default(T).Increment(), (s1, s2) => s1.Multiply(s2));

    public static T Sum<T>(this IEnumerable<T>? scalars) where T : unmanaged, IRing<T> => scalars?.Aggregate(default(T), (s1, s2) => s1.Add(s2)) ?? T.Zero;

    public static T Min<T>(this IEnumerable<T> scalars) where T : unmanaged, INumericRing<T> => scalars.AggregateNonEmpty((s, a) => s.Min(a));

    public static T Max<T>(this IEnumerable<T> scalars) where T : unmanaged, INumericRing<T> => scalars.AggregateNonEmpty((s, a) => s.Max(a));

    public static T Average<T>(this IEnumerable<T>? scalars) where T : unmanaged, IField<T> => scalars?.ToArray() is T[] arr ? arr.Sum().Divide(arr.Length.ToRing<T>()) : T.Zero;

    public static T Variance<T>(this IEnumerable<T>? scalars)
        where T : unmanaged, IField<T>
    {
        T avg = scalars.Average();

        return scalars.Sum(x => x.Subtract(avg).Power(2));
    }

    public static T StandardDeviation<T>(this IEnumerable<T>? scalars) where T : unmanaged, IScalar<T> => scalars.Variance().Sqrt();

    public static U Product<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, IRing<U> => scalars.Select(selector).Product();

    public static U Sum<T, U>(this IEnumerable<T>? scalars, Func<T, U> selector) where U : unmanaged, IRing<U> => scalars?.Select(selector).Sum() ?? U.Zero;

    public static U Min<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, INumericRing<U> => scalars.Select(selector).Min();

    public static U Max<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, INumericRing<U> => scalars.Select(selector).Max();

    public static U Average<T, U>(this IEnumerable<T>? scalars, Func<T, U> selector) where U : unmanaged, IField<U> => scalars?.Select(selector)?.Average() ?? U.Zero;

    public static U Median<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, IComparable<U> => scalars.Select(selector).Median();

    public static U Variance<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, IField<U> => scalars.Select(selector).Variance();

    public static U StandardDeviation<T, U>(this IEnumerable<T> scalars, Func<T, U> selector) where U : unmanaged, IScalar<U> => scalars.Select(selector).StandardDeviation();
}






#if false
namespace __________OLD_STUFF_________
{
/// <summary>
/// Represents a generic arithmetic wing with a notion of addition and scalar multiplication.
/// </summary>
/// <typeparam name="R">Generic ring type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IArithmeticRing<R, in S>
    : IArithmeticRing
    where R : IArithmeticRing<R, S>
{
    /// <summary>
    /// Adds the given object to the current instance and returns the addition's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Addition result</returns>
    R Add(R second);
    /// <summary>
    /// Negates the current instance and returns the result without modifying the current instance.
    /// </summary>
    /// <returns>Negated object</returns>
    R Negate();
    /// <summary>
    /// Subtracts the given object from the current instance and returns the substraction's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Subtraction result</returns>
    R Subtract(R second) => Add(second.Negate());
    /// <summary>
    /// Multiplies the given object with the current instance and returns the multiplication's result without modifying the current instance.
    /// <para/>
    /// This method is not to be confunded the dot-product for matrices and vectors.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Multiplication result</returns>
    R Multiply(R second);
    /// <summary>
    /// Multiplies the given scalar factor with the current instance and returns the multiplication's result without modifying the current instance.
    /// </summary>
    /// <param name="factor">Scalar factor</param>
    /// <returns>Multiplication result</returns>
    R Multiply(S factor);
    /// <summary>
    /// Divides the current instance by the given scalar factor and returns the division's result without modifying the current instance.
    /// </summary>
    /// <param name="factor">Scalar factor</param>
    /// <returns>Division result</returns>
    R Divide(S factor);
    /// <summary>
    /// Raises the current instance to the given power and returns the result without modifying the current instance.
    /// </summary>
    /// <param name="e">Exponent</param>
    R Power(int e)
    {
        if (e < 0)
            throw new IndexOutOfRangeException("The given power must be greater or equal to zero.");

        if (One is R acc)
        {
            for (int i = 0; i < e; ++i)
                acc = acc.Multiply((R)this);

            return acc;
        }
        else
            throw new NotImplementedException();
    }
}

/// <summary>
/// Represents a generic arithmetic field with a notion of multiplicative division and inversibility.
/// </summary>
/// <typeparam name="F">Generic field type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IArithmeticField<F, S>
    : IArithmeticRing<F, S>
    , IArithmeticField
    where F : IArithmeticField<F, S>
{
    // static F One { get; }

    /// <summary>
    /// The current instance's multiplicative inverse. The product of the current instance with its inverse results in the identity element.
    /// </summary>
    F MultiplicativeInverse { get; }

    /// <summary>
    /// Divides the current instance by the given object and returns the division's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Division result</returns>
    F Divide(F second) => Multiply(second.MultiplicativeInverse);
}

/// <summary>
/// Manages the transfer of raw binary data to foreign pointers.
/// </summary>
public unsafe interface IReadonlyNative
{
    /// <summary>
    /// The raw memory size of the current structure in bytes.
    /// </summary>
    int BinarySize { get; }

    /// <summary>
    /// Fills an array of the given generic data type with a byte-wise copy of the current instance.
    /// </summary>
    /// <typeparam name="T">Generic data type</typeparam>
    /// <returns>Generic data copy</returns>
    T[] ToArray<T>() where T : unmanaged;
    /// <summary>
    /// Fills the given pointer with the raw data represented by this instance. This is done by copying the current structure byte-wise into the given pointer.
    /// </summary>
    /// <typeparam name="T">Generic pointer type</typeparam>
    /// <param name="dst">Destination pointer</param>
    void ToNative<T>(T* dst) where T : unmanaged => ToArray<T>().BinaryCopy(dst, BinarySize);
}

/// <summary>
/// Manages the transfer of raw binary data from/to foreign pointers.
/// </summary>
public unsafe interface INative
    : IReadonlyNative
{
    /// <summary>
    /// Replaces the current instances's raw data with the data provided in the given pointer. This is done by copying the given pointer byte-wise into the current structure.
    /// </summary>
    /// <typeparam name="T">Generic pointer type</typeparam>
    /// <param name="src">Source pointer</param>
    void FromNative<T>(T* src) where T : unmanaged;
    /// <summary>
    /// Replaces the current instances's raw data with the data provided in the given array. This is done by copying the given array byte-wise into the current structure.
    /// </summary>
    /// <typeparam name="T">Generic array type</typeparam>
    /// <param name="arr">Source array</param>
    void FromArray<T>(T[] arr) where T : unmanaged;
}

/// <summary>
/// Represents the base interface for matrix-like types composed of scalar-valued coefficients.
/// </summary>
/// <typeparam name="S">The generic scalar type</typeparam>
public interface IMatrixBase<out S>
    : IReadonlyNative
    , IArithmeticRing
{
    /// <summary>
    /// Indicates whether the matrix/vector has NaN-values.
    /// </summary>
    bool HasNaNs { get; }
    /// <summary>
    /// Indicates whether the matrix/vector is negative.
    /// </summary>
    bool IsNegative { get; }
    /// <summary>
    /// Indicates whether the matrix/vector is positive.
    /// </summary>
    bool IsPositive { get; }
    /// <summary>
    /// The matrix/vector's dimension.
    /// </summary>
    int Size { get; }
    /// <summary>
    /// The sum of the matrix/vector's coefficients.
    /// </summary>
    S Sum { get; }
    /// <summary>
    /// The average of the matrix/vector's coefficients.
    /// </summary>
    S Avg { get; }
    /// <summary>
    /// The minimum value of the matrix/vector's coefficients.
    /// </summary>
    S Min { get; }
    /// <summary>
    /// The maximum value of the matrix/vector's coefficients.
    /// </summary>
    S Max { get; }
}

/// <summary>
/// Represents a generic vector.
/// </summary>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IVector<out S>
    : IMatrixBase<S>
{
#if READONLY
    /// <summary>
    /// Gets the vector's coefficient at the given index.
    /// </summary>
    /// <param name="index">Coefficient index (zero-based)</param>
    /// <returns>Coefficient value</returns>
    S this[int index] { get; }
#else
    /// <summary>
    /// Sets or gets the vector's coefficient at the given index.
    /// </summary>
    /// <param name="index">Coefficient index (zero-based)</param>
    /// <value>New coefficient value</value>
    /// <returns>Coefficient value</returns>
    S this[int index] { set; get; }
#endif
    /// <summary>
    /// The vector's eucledian length.
    /// </summary>
    S Length { get; }

    /// <summary>
    /// Returns the array representation of the vector.
    /// </summary>
    /// <returns>Flat array representation</returns>
    S[] ToArray();
    /// <summary>
    /// Converts the vector to its polynomial representation in ascending exponential order.
    /// <para/>
    /// The vector <code>(a,b,c,d, ...)</code> will be translated to the polynomial <code>a + bx + cx² + dx³ + ...</code>.
    /// </summary>
    /// <returns>Polynomial</returns>
    Polynomial ToPolynomial();
}

/// <summary>
/// Represents a generic vector.
/// </summary>
/// <typeparam name="V">Generic vector type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IVector<V, S>
    : IArithmeticRing<V, S>
    , IVector<S>
    , IComparable<V>
    where V : struct, IVector<V, S>
{
#if READONLY
    /// <summary>
    /// Sets the vector's coefficient at the given index and returns the modified vector.
    /// </summary>
    /// <param name="index">Coefficient index (zero-based)</param>
    /// <param name="value">The new coefficient value</param>
    /// <returns>Modified vector</returns>
    V this[int index, S value] { get; }
#endif
    /// <summary>
    /// The eucledian normalized vector.
    /// </summary>
    V Normalized => IsZero ? (V)this : Divide(Length);

    /// <summary>
    /// Calculates the dot product (aka. scalar product) of the current vector and the given one.
    /// </summary>
    /// <param name="other">Second vector</param>
    /// <returns>Dot product</returns>
    S Dot(V other);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    V Abs();
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    V Sqrt();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    V Lerp(V other, S factor) => Multiply()
    /// <summary>
    /// 
    /// </summary>
    /// <param name="low"></param>
    /// <param name="high"></param>
    /// <returns></returns>
    V Clamp(S low, S high);
    /// <summary>
    /// Returns whether the given vector is linear independent from the current one.
    /// </summary>
    /// <param name="other">Second vector</param>
    bool IsLinearIndependent(V other);
    /// <summary>
    /// Swaps the coefficients stored at the given indices and returns the resulting vector.
    /// </summary>
    /// <param name="src_idx">Source index (zero-based)</param>
    /// <param name="dst_idx">Target index (zero-based)</param>
    /// <returns>Vector with swapped coefficients</returns>
    V SwapEntries(int src_idx, int dst_idx);
}

/// <summary>
/// Represents a generic vector.
/// </summary>
/// <inheritdoc cref="IVector{V,S}"/>
/// <typeparam name="M">Generic matrix type</typeparam>
/// <typeparam name="V">Generic vector type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IVector<out M, V, S>
    : IVector<V, S>
    where M : struct, IMatrix<M, V, S>
    where V : struct, IVector<M, V, S>
{
    /// <summary>
    /// The householder matrix represented by the current vector.
    /// </summary>
    M HouseholderMatrix { get; }

    /// <summary>
    /// Calculates the outer product of the current instance with the given vector and returns the result without modifying the current instance.
    /// </summary>
    /// <param name="other">Second vector</param>
    /// <returns>Outer product</returns>
    M OuterProduct(V other);
}

/// <summary>
/// Represents a generic vector which defines the cross-product.
/// </summary>
/// <typeparam name="V">Generic vector type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface ICrossableVector<V, S>
    : IVector<V, S>
    where V : struct, IVector<V, S>
{
    /// <summary>
    /// Calculates the cross product of the current instance with the given one and returns the result without modifying the current instance.
    /// </summary>
    /// <param name="other">Second vector</param>
    /// <returns>Cross product</returns>
    V Cross(V other);
    /// <summary>
    /// Calculates the triple product of the current instance and the two given ones.
    /// </summary>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    S TripleProduct(V y, V z);
}

/// <summary>
/// Represents a generic square matrix.
/// </summary>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IMatrix<out S>
    : IMatrixBase<S>
{
#if READONLY
    /// <summary>
    /// Gets the vector's coefficient at the given index.
    /// </summary>
    /// <param name="column">Coefficient column index (zero-based)</param>
    /// <param name="row">Coefficient row index (zero-based)</param>
    /// <returns>Coefficient value</returns>
    S this[int column, int row] { get; }
#else
    /// <summary>
    /// Sets or gets the vector's coefficient at the given index.
    /// </summary>
    /// <param name="column">Coefficient column index (zero-based)</param>
    /// <param name="row">Coefficient row index (zero-based)</param>
    /// <value>New coefficient value</value>
    /// <returns>Coefficient value</returns>
    S this[int column, int row] { get; set; }
#endif
    /// <summary>
    /// The matrix' determinant.
    /// </summary>
    S Determinant { get; }
    /// <summary>
    /// The matrix' trace.
    /// </summary>
    S Trace { get; }
    /// <summary>
    /// Indicates whether the matrix is the identity matrix.
    /// </summary>
    bool IsIdentity { get; }
    /// <summary>
    /// Indicates whether the matrix is a diagonal matrix.
    /// </summary>
    bool IsDiagonal { get; }
    /// <summary>
    /// Indicates whether the matrix is an upper (right) triangular matrix.
    /// </summary>
    bool IsUpperTriangular { get; }
    /// <summary>
    /// Indicates whether the matrix is a lower (left) triangular matrix.
    /// </summary>
    bool IsLowerTriangular { get; }
    /// <summary>
    /// Indicates whether the matrix is symmetric.
    /// </summary>
    bool IsSymmetric { get; }
    /// <summary>
    /// Indicates whether the matrix is a projection matrix.
    /// </summary>
    bool IsProjection { get; }
    /// <summary>
    /// Indicates whether the current matrix 'A' is a conference matrix, meaning that AᵀA is a multiple of the identity matrix.
    /// </summary>
    bool IsConferenceMatrix { get; }
    /// <summary>
    /// Indicated whether the matrix is involutory, meaning that the square of this matrix is equal to the identity matrix.
    /// </summary>
    bool IsInvolutory { get; }
    /// <summary>
    /// Indicates whether the matrix is stable in the sense of the Hurwitz criterium.
    /// </summary>
    bool IsHurwitzStable { get; }
    /// <summary>
    /// Indicates whether the matrix is orthogonal.
    /// </summary>
    bool IsOrthogonal { get; }
    /// <summary>
    /// Indicates whether the matrix is skew symmetric.
    /// </summary>
    bool IsSkewSymmetric { get; }
    /// <summary>
    /// The matrix' characteristic polynomial.
    /// </summary>
    bool IsSignMatrix { get; }
    bool IsSignatureMatrix { get; }
    Polynomial CharacteristicPolynomial { get; }
    /// <summary>
    /// The matrix' eigenvalues.
    /// </summary>
    // ∀λ∈Spec(A) : Ax = λx
    S[] Eigenvalues { get; }
    /// <summary>
    /// The rank of the matrix.
    /// </summary>
    int Rank { get; }

    /// <summary>
    /// Returns a set of principal submatrices.
    /// </summary>
    /// <returns>Set of principal submatrices</returns>
    IMatrix<S>[] GetPrincipalSubmatrices();
    /// <summary>
    /// Returns the matrix as a flat array of matrix elements in column major format.
    /// </summary>
    /// <returns>Column major representation of the matrix</returns>
    S[] ToArray();
#if !READONLY
    /// <summary>
    /// Fills the current matrix with the given scalar matrix elements in a column major format.
    /// </summary>
    /// <param name="v">New matrix elements</param>
    void FromArray(S[] v);
#endif
}

/// <summary>
/// Represents a generic square matrix.
/// </summary>
/// <typeparam name="M">Generic matrix type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IMatrix<M, S>
    : IMatrix<S>
    , IArithmeticField<M, S>
    , IComparable<M>
    where M : struct, IMatrix<M, S>
{
#if READONLY
    /// <summary>
    /// Sets the vector's coefficient at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="column">Coefficient column index (zero-based)</param>
    /// <param name="row">Coefficient row index (zero-based)</param>
    /// <param name="value">The new coefficient value</param>
    /// <returns>Modified matrix</returns>
    M this[int column, int row, S value] { get; }
#endif
    /// <summary>
    /// Returns the gaussian reduced matrix.
    /// </summary>
    M GaussianReduced { get; }
    /// <summary>
    /// The matrix' orthonormal basis.
    /// </summary>
    M OrthonormalBasis { get; }
    /// <summary>
    /// The transposed matrix.
    /// </summary>
    M Transposed { get; }

    /// <summary>
    /// Multiplies a row with the given factor and returns the modified matrix.
    /// </summary>
    /// <param name="row">Zero-based row index</param>
    /// <param name="factor">Scalar multiplication factor</param>
    /// <returns>Modified matrix</returns>
    M MultiplyRow(int row, S factor);
    /// <summary>
    /// Swaps two rows and returns the modified matrix.
    /// </summary>
    /// <param name="src_row">Zero-based first row index</param>
    /// <param name="dst_row">Zero-based second row index</param>
    /// <returns>Modified matrix</returns>
    M SwapRows(int src_row, int dst_row);
    /// <summary>
    /// Adds the given source row to the given destination row and returns the modified matrix.
    /// </summary>
    /// <param name="src_row">Zero-based source row index</param>
    /// <param name="dst_row">Zero-based destination row index</param>
    /// <returns>Modified matrix</returns>
    M AddRows(int src_row, int dst_row);
    /// <summary>
    /// Adds the given source row (premultiplied with the given scalar factor) to the given destination row and returns the modified matrix.
    /// </summary>
    /// <param name="src_row">Zero-based source row index</param>
    /// <param name="dst_row">Zero-based destination row index</param>
    /// <param name="factor">Scalar factor</param>
    /// <returns>Modified matrix</returns>
    M AddRows(int src_row, int dst_row, S factor);
    /// <summary>
    /// Multiplies a column with the given factor and returns the modified matrix.
    /// </summary>
    /// <param name="col">Zero-based column index</param>
    /// <param name="factor">Scalar factor</param>
    /// <returns>Modified matrix</returns>
    M MultiplyColumn(int col, S factor);
    /// <summary>
    /// Swaps two columns and returns the modified matrix.
    /// </summary>
    /// <param name="src_col">Zero-based first column index</param>
    /// <param name="dst_col">Zero-based second column index</param>
    /// <returns>Modified matrix</returns>
    M SwapColumns(int src_col, int dst_col);
    /// <summary>
    /// Adds the given source column to the given destination column and returns the modified matrix.
    /// </summary>
    /// <param name="src_col">Zero-based source coulmn index</param>
    /// <param name="dst_col">Zero-based destination column index</param>
    /// <returns>Modified matrix</returns>
    M AddColumns(int src_col, int dst_col);
    /// <summary>
    /// Adds the given source column (premultiplied with the given scalar factor) to the given destination column and returns the modified matrix.
    /// </summary>
    /// <param name="src_col">Zero-based source coulmn index</param>
    /// <param name="dst_col">Zero-based destination column index</param>
    /// <param name="factor">Scalar factor</param>
    /// <returns>Modified matrix</returns>
    M AddColumns(int src_col, int dst_col, S factor);
}

/// <summary>
/// Represents a generic square matrix.
/// </summary>
/// <inheritdoc cref="IMatrix{M,S}"/>
/// <typeparam name="M">Generic matrix type</typeparam>
/// <typeparam name="V">Generic underlying vector type</typeparam>
/// <typeparam name="S">Generic (unmanaged) scalar value type</typeparam>
public interface IMatrix<M, V, S>
    : IMatrix<M, S>
    where M : struct, IMatrix<M, V, S>
    where V : struct, IVector<M, V, S>
{
#if READONLY
    /// <summary>
    /// Gets the matrix' column vector at the given index.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <returns>Column vector</returns>
    V this[int column] { get; }
    /// <summary>
    /// Sets the matrix' column vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <param name="vector">New column vector</param>
    /// <returns>Modified matrix</returns>
    M this[int column, V vector] { get; }
#else
    /// <summary>
    /// Sets or gets the matrix' column vector at the given index.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <value>New column vector</value>
    /// <returns>Column vector</returns>
    V this[int column] { get; set; }
#endif
    /// <summary>
    /// The matrix' main diagonal.
    /// </summary>
    V MainDiagonal { get; }
    /// <summary>
    /// The matrix' column vectors.
    /// </summary>
    V[] Columns { get; }
    /// <summary>
    /// The matrix' row vectors.
    /// </summary>
    V[] Rows { get; }

    /// <summary>
    /// Decomposes the current matrix into a diagonal and an upper triagonal matrix.
    /// </summary>
    /// <returns>Tuple consisiting of the upper triagonal and diagonal matrices</returns>
    (M U, M D) IwasawaDecompose();
#if !READONLY
    /// <summary>
    /// Fills the current matrix with the given column vectors
    /// </summary>
    /// <param name="v">Array of coulmn vectors</param>
    void FromArray(V[] v);
#endif
    /// <summary>
    /// Calculates the product of the current matrix with the given vector.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Product</returns>
    V Multiply(V vector);
    /// <summary>
    /// Solves the current matrix for the given vector in a linear equation system
    /// </summary>
    /// <param name="v">Vector</param>
    /// <returns>Solution</returns>
    V Solve(V v);
}
}
#endif

