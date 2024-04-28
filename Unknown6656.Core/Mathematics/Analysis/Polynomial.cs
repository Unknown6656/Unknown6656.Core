using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Optimization.ParticleSwarmOptimization;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Generics;
using Unknown6656.Common;

using static System.Math;

namespace Unknown6656.Mathematics.Analysis;


public interface IPolynomial<Polynomial, Scalar>
{
    static abstract Polynomial CreatePolynomial(IEnumerable<Scalar> coefficients);

    static abstract Polynomial CreatePolynomial(params Scalar[] coefficients);
}

/// <summary>
/// Represents an abstract polynomial.
/// <para/>
/// All derived classes must have a constructor accepting a coefficient array of the type <typeparamref name="T"/>[] as single parameter.
/// </summary>
public class Polynomial<Function, T>
    : ContinuousFunction<Polynomial<Function, T>, Polynomial<Function, T>, T, T>
    , IPolynomial<Function, T>
    , IEnumerable<T>
    , ICloneable
    where Function : Polynomial<Function, T>
    where T : unmanaged, IField<T>, INumericRing<T>
{
    private static readonly Func<IEnumerable<T>, Function> _create;

    /// <summary>
    /// index i corresponds to Xⁱ
    /// </summary>
    private protected readonly T[] _coefficients;

    #region STATIC PROPERTIES

    /// <summary>
    /// The constant zero polynomial 'p(x) = 0'.
    /// </summary>
    public static Function Zero { get; }

    /// <summary>
    /// The constant one polynomial 'p(x) = 1'.
    /// </summary>
    public static Function One { get; }

    /// <summary>
    /// The polynomial 'p(x) = x'.
    /// </summary>
    public static Function X { get; }

    #endregion
    #region INSTANCE PROPERTIES

    /// <summary>
    /// The polynomial's degree
    /// </summary>
    public int Degree => _coefficients.Length - 1;

    /// <summary>
    /// The polynomial's coefficient in ascending order
    /// </summary>
    public ReadOnlyCollection<T> Coefficients => new(_coefficients);

    /// <summary>
    /// The polynomial's leading coefficient
    /// </summary>
    public T LeadingCoefficient => _coefficients[^1];

    /// <summary>
    /// The polynomial's derivative
    /// </summary>
    public override Polynomial<Function, T> Derivative
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Degree > 0)
            {
                T[] arr = DecreaseDegree(1)._coefficients;
                T counter = default;
                int i = 0;

                do
                {
                    counter = counter.Increment();

                    arr[i] = arr[i].Multiply(counter);
                }
                while (++i < arr.Length);

                return _create(arr);
            }
            else
                return Zero;
        }
    }

    public override Polynomial<Function, T> Integral
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            T[] arr = new T[_coefficients.Length + 1];
            T counter = default;
            int i = 0;

            do
            {
                ++i;
                counter = counter.Increment();

                arr[i] = Coefficients[i - 1].Divide(in counter);
            }
            while (i < arr.Length);

            return _create(arr);
        }
    }

    public Function<T, T> Inverse { get; }

    public bool IsInvertible => !IsZero;

    public bool IsConstant => Degree == 0;

    public override bool IsZero => Is(Zero);

    public bool IsOne => Is(One);

    public bool IsX => Is(X);

    /// <summary>
    /// Returns the roots of the polynomial (meaning all x-values, at which poly(x) is evaluated to zero)
    /// </summary>
    public IEnumerable<T> Roots => Solve(default);

    #endregion
    #region CONSTRUCTORS

    static Polynomial()
    {
        Type F = typeof(Function);

        if (F.GetConstructor([typeof(T[])]) is { } ctor)
            _create = c => (Function)ctor.Invoke(new object[] { c is T[] a ? a : c.ToArray() });
        else
            throw new InvalidOperationException($"The type parameter '{F}' cannot be used as polynomial function type, as it has no constructor accepting an array of polynomial coefficents ('{typeof(T[])}').");

        Zero = _create(new T[1] { T.Zero });
        One = _create(new T[1] { T.One });
        X = One.IncreaseDegree(1);
    }

    public Polynomial(Function polynomial)
        : this(polynomial._coefficients)
    {
    }

    public Polynomial(Polynomial<Function, T> polynomial)
        : this(polynomial._coefficients)
    {
    }

    /// <summary>
    /// Creates a new polynomial using the given coefficients in ascending exponential order, meaning that the i-th coefficient represents the factor for Xⁱ.
    /// </summary>
    /// <param name="c">Polynomial coefficients</param>
    public Polynomial(IEnumerable<T> coefficients)
        : this(coefficients.ToArray())
    {
    }

    /// <summary>
    /// Creates a new polynomial using the given coefficients in ascending exponential order, meaning that the i-th coefficient represents the factor for Xⁱ.
    /// </summary>
    /// <param name="c">Polynomial coefficients</param>
    public Polynomial(params T[] coefficients)
        : this(SanitizeCoefficients(coefficients), default)
    {
    }

    private Polynomial(T[] coefficients, bool _)
        : base(x => coefficients.Reverse().Aggregate((acc, c) => acc.Multiply(x).Add(c)))
    {
        _coefficients = coefficients;
        Inverse = new Function<T, T>(x => Evaluate(x).MultiplicativeInverse);
    }

    private static T[] SanitizeCoefficients(T[] coefficients)
    {
        coefficients = coefficients.Reverse()
                                   .SkipWhile(c => c.IsZero)
                                   .Reverse()
                                   .ToArray();

        return coefficients is { Length: 0 } c ? ([default]) : coefficients;
    }

    #endregion
    #region INSTANCE METHODS

    public override T Evaluate(T x)
    {
        int i = _coefficients.Length;
        T res = default;
        T acc = res.Increment();

        while (i-- > 0)
        {
            res = res.Add(_coefficients[i].Multiply(acc));
            acc = acc.Multiply(x);
        }

        return res;
    }

    public override Polynomial<Function, T> Negate() => _create(_coefficients.Select(c => c.AdditiveInverse));

    public Function Add(Polynomial<Function, T> second) => _create(_coefficients.ZipOuter(second._coefficients, (c1, c2) => c1.Add(c2)));

    public Function Add(params Polynomial<Function, T>[] others) => others.Aggregate(this, (x, y) => x.Add(y));

    public Function Subtract(Polynomial<Function, T> second) => _create(_coefficients.ZipOuter(second._coefficients, (c1, c2) => c1.Subtract(c2)));

    public Function Subtract(params Polynomial<Function, T>[] others) => others.Aggregate(this, (x, y) => x.Subtract(y));

    public Function Increment() => Add(One);

    public Function Decrement() => Subtract(One);

    public Function Multiply(Polynomial<Function, T> second) => _create(_coefficients.Select((c, i) => second.Multiply(c).IncreaseDegree(i)).Aggregate(Zero, (p, a) => p.Add(a)));

    public Function Multiply(params Polynomial<Function, T>[] others) => others.Aggregate(this, (x, y) => x.Multiply(y));

    public Function Multiply(T factor) => _create(_coefficients.Select(c => c.Multiply(factor)));

    public Function Divide(Polynomial<Function, T> second) => second.IsConstant ? Divide(second.LeadingCoefficient) : PolynomialDivision(this, second).Quotient;

    public Function Divide(params Polynomial<Function, T>[] others) => others.Aggregate(this, (x, y) => x.Divide(y));

    public Function Divide(T factor) => Multiply(factor.MultiplicativeInverse);

    public Function Modulus(Function second) => Is(second) ? Zero : PolynomialDivision(this, second).Remainder;

    public Function Power(int e)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(e);

        Function r = One;
        Function p = _create(_coefficients);

        while (e > 0)
            if ((e & 1) == 1)
            {
                --e;
                r = r.Multiply(p);
            }
            else
            {
                e /= 2;
                p = p.Multiply(p);
            }

        return r;
    }

    public Function IncreaseDegree(int count) => count switch
    {
        0 => new Polynomial<Function, T>(_coefficients),
        > 0 => new Polynomial<Function, T>(Enumerable.Repeat(default(T), count).Concat(_coefficients)),
        _ => throw new ArgumentOutOfRangeException()
    };

    public Function DecreaseDegree(int count) => count switch
    {
        0 => new Polynomial<Function, T>(_coefficients),
        > 0 => new Polynomial<Function, T>(_coefficients.Skip(count)),
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>
    /// Solves the polynomial for the given Y value.
    /// <para/>
    /// WARNING: This function is not implemented by <see cref="Polynomial{Function, T}"/>!
    /// </summary>
    /// <param name="y">Y value.</param>
    /// <returns>Enumeration of possible X values.</returns>
    public virtual IEnumerable<T> Solve(T y) => Array.Empty<T>();

    public object Clone() => (Function)this;

    public bool Equals(Polynomial<Function, T>? other) => Is(other);

    public override bool Equals(Function<T, T>? other) => Is(other);

    public override bool Equals(object? obj) => Equals(obj as Polynomial<Function, T>);

    /// <summary>
    /// Compares the given polynomial with the current instance and returns whether both are equal.
    /// </summary>
    /// <param name="o">Second polynomial</param>
    /// <returns>Comparison result</returns>
    public override bool Is(Function<T, T>? o) => Is(o as Polynomial<Function, T>);

    /// <summary>
    /// Compares the given polynomial with the current instance and returns whether both are equal.
    /// </summary>
    /// <param name="o">Second polynomial</param>
    /// <returns>Comparison result</returns>
    public bool Is(Polynomial<Function, T>? o) => o is { } && _coefficients.Are(o._coefficients, (c1, c2) => c1.Is(c2));

    public override int GetHashCode() => LINQ.GetHashCode(_coefficients);

    /// <summary>
    /// Returns the string representation of the current polynomial.
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString() => Degree == 0 ? Coefficients[0].ToString()! : string.Join(" + ", this.Select((c, i) =>
    {
        if (c.IsZero)
            return "";

        string cstr = c.ToString()!;

        if (i == 0)
            return cstr;

        if (c.Abs().IsOne)
            cstr = cstr.Trim('1');

        if (c is Complex { Argument: Scalar φ } && !φ.IsMultipleOf(Scalar.Pi / 2))
            cstr = $"({cstr})";

        return $"{cstr}x{(i == 1 ? "" : MathExtensions.ToSuperScript(i))}";
    }).Where(s => s.Length > 0).Reverse()).Replace("+ -", "- ");

    public IEnumerator<T> GetEnumerator() => Coefficients.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<T>).GetEnumerator();

    #endregion
    #region STATIC METHODS

    /// <summary>
    /// Divides the two given polynomials using polynomial long division and returns the division result
    /// </summary>
    /// <param name="n">First polynomial</param>
    /// <param name="d">Second polynomial</param>
    /// <returns>Divison quotient and remainder</returns>
    public static (Function Quotient, Function Remainder) PolynomialDivision(Polynomial<Function, T> n, Polynomial<Function, T> d)
    {
        int nd = n.Degree;
        int dd = d.Degree;

        if (dd < 0)
            throw new ArgumentException("Divisor must have at least one one-zero coefficient");
        else if (nd < dd)
            throw new ArgumentException("The degree of the divisor cannot exceed that of the numerator");

        Function r = n;
        Function q = Zero;

        while (r != Zero && r.Degree >= dd)
        {
            int t_deg = r.Degree - d.Degree;
            T t_coef = r.LeadingCoefficient.Divide(d.LeadingCoefficient);
            Function t = _create(new[] { t_coef }).IncreaseDegree(t_deg);

            q = q.Add(t);
            r = r.Subtract(t.Multiply(d));
        }

        return (q, r);
    }

    public static Function GetChebychevBasePolynomial(int degree) => degree switch
    {
        0 => One,
        1 => X,
        < 0 => throw new ArgumentOutOfRangeException("The degree must not be negative."),
        _ => ((X + X) * GetChebychevBasePolynomial(degree - 1)) - GetChebychevBasePolynomial(degree - 2)
    };

    public static Function GetLagrangeBasePolynomial(int i, params T[] points)
    {
        Function L = One;

        for (int j = 0; j < points.Length; ++j)
            if (j != i)
                L *= (X - points[j]) / points[i].Subtract(points[j]);

        return L;
    }

    public static Function GetNewtonBasePolynomial(int i, params T[] points)
    {
        Function N = One;

        for (int j = 0; j < i - 1; ++j)
            N *= X - points[j];

        return N;
    }

    public static Function GetLagrangeInterpolationPolynomial(params (T x, T y)[] points)
    {
        Function L = Zero;

        for (int i = 0; i < points.Length; ++i)
        {
            Function l = One;

            for (int j = 0; j < points.Length; ++j)
                if (points[j].x.IsNot(points[i].x))
                    l *= (X - points[j].x) / points[i].x.Subtract(points[j].x);

            L += points[i].y * l;
        }

        return L;
    }

    public static Function CreatePolynomial(IEnumerable<T> coefficients) => (Function)new Polynomial<Function, T>(coefficients);

    public static Function CreatePolynomial(params T[] coefficients) => (Function)new Polynomial<Function, T>(coefficients);

    #endregion
    #region OPERATORS

    /// <summary>
    /// Implicitly converts the given scalar to a polynomial of degree zero.
    /// </summary>
    /// <param name="f">Scalar</param>
    public static implicit operator Polynomial<Function, T>(T f) => new(new[] { f });

    /// <summary>
    /// Implicitly converts the given array of scalar coefficients to their respective polynomial representation.
    /// </summary>
    /// <param name="c">Scalar coefficients</param>
    public static implicit operator Polynomial<Function, T>(T[] c) => new(c);

    public static explicit operator T[](Polynomial<Function, T> p) => p.Coefficients.ToArray();

    public static implicit operator Function(Polynomial<Function, T> p) => _create(p._coefficients);

    /// <summary>
    /// Compares the two given polynomials and returns whether both are equal.
    /// </summary>
    /// <param name="p1">Second polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Comparison result</returns>
    public static bool operator ==(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Is(p2);

    /// <summary>
    /// Compares the two given polynomials and returns whether both are not equal to each other.
    /// </summary>
    /// <param name="p1">Second polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Comparison result</returns>
    public static bool operator !=(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => !p1.Is(p2);

    /// <summary>
    /// Represents the identity-function
    /// </summary>
    /// <param name="p">Polynomial</param>
    /// <returns>Unchanged polynomial</returns>
    public static Function operator +(Polynomial<Function, T> p) => p;

    /// <summary>
    /// Negates the given polynomial by negating each coefficient
    /// </summary>
    /// <param name="p">Polynomial</param>
    /// <returns>Negated polynomial</returns>
    public static Function operator -(Polynomial<Function, T> p) => p.Negate();

    public static Function operator ++(Polynomial<Function, T> p) => p.Increment();

    public static Function operator --(Polynomial<Function, T> p) => p.Decrement();

    /// <summary>
    /// Performs the addition of two polynomials by adding their respective coefficients.
    /// </summary>
    /// <param name="p1">First polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Addition result</returns>
    public static Function operator +(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Add(p2);

    /// <summary>
    /// Performs the subtraction of two polynomials by subtracting their respective coefficients.
    /// </summary>
    /// <param name="p1">First polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Subtraction result</returns>
    public static Function operator -(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Subtract(p2);

    /*
    /// <summary>
    /// Performs the multiplication of a polynomial with a single scalar. All of the polynomial's coefficients will be multiplied by the scalar
    /// </summary>
    /// <param name="f">Scalar</param>
    /// <param name="p">Polynomial</param>
    /// <returns>Multiplication result</returns>
    public static Polynomial operator *(scalar f, Polynomial p) => p.Multiply(f);

    /// <summary>
    /// Performs the multiplication of a polynomial with a single scalar. All of the polynomial's coefficients will be multiplied by the scalar
    /// </summary>
    /// <param name="f">Scalar</param>
    /// <param name="p">Polynomial</param>
    /// <returns>Multiplication result</returns>
    public static Polynomial operator *(Polynomial p, scalar f) => p.Multiply(f);
    */

    /// <summary>
    /// Performs the multiplication of two polynomials.
    /// </summary>
    /// <param name="p1">First polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Multiplication result</returns>
    public static Function operator *(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Multiply(p2);

    /// <summary>
    /// Raises the given polynomial to the given (non-negative) power
    /// </summary>
    /// <param name="p">Polynomial</param>
    /// <param name="e">Exponent</param>
    /// <returns>Result</returns>
    public static Function operator ^(Polynomial<Function, T> p, int e) => p.Power(e);

    /// <summary>
    /// Performs the scalar division by dividing each of the given polynomial's coefficient by the given scalar value.
    /// </summary>
    /// <param name="p">Polynomial</param>
    /// <param name="f">Scalar divisor</param>
    /// <returns>Division result</returns>
    public static Function operator /(Polynomial<Function, T> p, T f) => p.Divide(f);

    /// <summary>
    /// Performs the polynomial long division and returns the quotient
    /// </summary>
    /// <param name="p1">First polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Quotient</returns>
    public static Function operator /(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Divide(p2);

    /// <summary>
    /// Performs the polynomial long division and returns the remainder
    /// </summary>
    /// <param name="p1">First polynomial</param>
    /// <param name="p2">Second polynomial</param>
    /// <returns>Remainder</returns>
    public static Function operator %(Polynomial<Function, T> p1, Polynomial<Function, T> p2) => p1.Modulus(p2);

    /// <summary>
    /// <b>Increaces</b> the polynomial's degree by the given amount <paramref name="a"/> (equivalent to multiplying it with X^a). This is NOT equal to computing the polynomial's integral.
    /// </summary>
    /// <param name="p">Input polynomial</param>
    /// <param name="a">Increacement 'amount'</param>
    /// <returns>Output polynomial</returns>
    public static Function operator >>(Polynomial<Function, T> p, int a) => a switch
    {
        0 => (Function)p,
        < 0 => p.DecreaseDegree(-a),
        _ => p.IncreaseDegree(a)
    };

    /// <summary>
    /// <b>Decreaces</b> the polynomial's degree by the given amount <paramref name="a"/> (equivalent to multiplying it with X^-a). This is NOT equal to computing the polynomial's derivative.
    /// </summary>
    /// <param name="p">Input polynomial</param>
    /// <param name="a">Decreacement 'amount'</param>
    /// <returns>Output polynomial</returns>
    public static Function operator <<(Polynomial<Function, T> p, int a) => a switch
    {
        0 => (Function)p,
        < 0 => p.IncreaseDegree(-a),
        _ => p.DecreaseDegree(a)
    };

    #endregion
}

public class ComplexPolynomial
    : Polynomial<ComplexPolynomial, Complex>
{
    public ComplexPolynomial(params Complex[] coefficients)
        : base(coefficients)
    {
    }

    public virtual YValueFinder<Complex> CreateZeroPointFinder() => new(this, Complex.Zero);

    public override IEnumerable<Complex> Solve(Complex y) => Solve(y, PSOSolverConfiguration<Complex>.CreateDefault(Complex.Zero));

    public IEnumerable<Complex> Solve(Complex y, PSOSolverConfiguration<Complex> configuration) =>
        new[] { CreateZeroPointFinder().CreateSolver(configuration).Solve().OptimalSolution };

    public override Complex Evaluate(Complex x)
    {
        int i = _coefficients.Length;
        Complex res = 0;
        Complex acc = 1;

        while (i-- > 0)
        {
            res += _coefficients[i] * acc;
            acc *= x;
        }

        return res;
    }
}

public class Polynomial<T>
    : Polynomial<Polynomial<T>, Scalar<T>>
    where T : unmanaged, IComparable<T>
{
    public Polynomial(params Scalar<T>[] coefficients)
        : base(coefficients)
    {
    }

    // TODO : parse

    public override Scalar<T> Evaluate(Scalar<T> x)
    {
        int i = _coefficients.Length;
        Scalar<T> res = Scalar<T>.Zero;
        Scalar<T> acc = Scalar<T>.One;

        while (i-- > 0)
        {
            res += _coefficients[i] * acc;
            acc *= x;
        }

        return res;
    }
}

public partial class Polynomial
    : Polynomial<Polynomial, Scalar>
{
    public Polynomial(params Scalar[] coefficients)
        : base(coefficients)
    {
    }

    public override Scalar Evaluate(Scalar x)
    {
        int i = _coefficients.Length;
        Scalar res = 0;
        Scalar acc = 1;

        while (i-- > 0)
        {
            res += _coefficients[i] * acc;
            acc *= x;
        }

        return res;
    }

    public Scalar NearestZeroPoint(Scalar x)
    {
        // TODO:
        //if (Degree < 3)
        //    return ZeroPoints.OrderBy(p => Abs(p - x)).First();

        var f = Cached;
        var δ = Derivative.Cached;
        int iterations = 0;
        double epsilon;

        do
        {
            ++iterations;

            while (Abs(δ[x]) < 1e-5)
                x += 1e-5;

            epsilon = -f[x] / δ[x];

            x += epsilon;
        }
        while ((Abs(epsilon) > Scalar.ComputationalEpsilon) && (iterations < 500));

        return x;
    }

    /// <summary>
    /// Solves the current polynomial for any given y-value
    /// </summary>
    /// <param name="y">y-value</param>
    /// <returns>X-values</returns>
    public override IEnumerable<Scalar> Solve(Scalar y)
    {
        Scalar[] co = _coefficients;
        Polynomial llc = this << 1;
        int deg = Degree;

        IEnumerable<Scalar> __solve()
        {
            if (deg == 0)
            {
                if (co[0].Is(y))
                    foreach (Scalar f in Sequences.AllNumbers())
                        yield return f;
            }
            else if (deg == 1)
                yield return (y - co[0]) / co[1];
            else if (co[0].Is(y))
            {
                yield return 0;

                foreach (Scalar f in llc.Solve(y))
                    yield return f;
            }
            else
            {
                if (deg == 2)
                {
                    Scalar a = co[2];
                    Scalar b = co[1];
                    Scalar c = co[0] - y;
                    Scalar q = b * b - 4 * a * c;

                    if (!q.IsNegative)
                    {
                        q = (Scalar)(-.5 * q.Sqrt());

                        yield return q / a;

                        if (!q.IsZero)
                            yield return c / q;
                    }
                }
                else if (deg == 3)
                    foreach (Complex c in SolveCardano(co[3], co[2], co[1], co[0] - y))
                    {
                        if (c.IsReal)
                            yield return c.Real;
                    }
                // else if (deg == 4)
                //      solve for 
                //      0 = ax⁴ + bx³ + cx² + dx + e
                //        = (((ax + b)x + c)x + d)x + e
                else
                    foreach (Scalar s in SolvePSO(y))
                        yield return s;
            }
        }

        foreach (Scalar x in __solve().Distinct())
            yield return x;
    }

    public virtual YValueFinder<Scalar> CreateZeroPointFinder() => new(this, Scalar.Zero);

    public IEnumerable<Scalar> SolvePSO(Scalar y) => SolvePSO(y, PSOSolverConfiguration<Scalar>.CreateDefault(Scalar.Zero));

    public IEnumerable<Scalar> SolvePSO(Scalar y, PSOSolverConfiguration<Scalar> configuration) =>
        new[] { CreateZeroPointFinder().CreateSolver(configuration).Solve().OptimalSolution };

    /// <summary>
    /// Solves a cubic polynomials of the form 'AX³ + BX² + CX + D'.
    /// <para/>
    /// 'A' must not be zero.
    /// </summary>
    public static IEnumerable<Complex> SolveCardano(Scalar A, Scalar B, Scalar C, Scalar D)
    {
        if (A.IsZero)
            throw new ArgumentException($"The leading coefficient 'A' must not be zero.", nameof(A));

        Scalar root(Scalar φ, Scalar τ)
        {
            Scalar i = φ < 0 ? -1 : 1;

            return i * Exp(Log(φ * i) / τ);
        }

        Scalar a = B / A;
        Scalar u, v;

        B = C / A;
        C = D / A;

        Scalar a3 = a / -3;
        Scalar p = a * a3 + B;
        Scalar q = (2d / 27 * a * a * a) + B * a3 + C;

        D = q * q / 4 + p * p * p / 27;

        if (D.IsZero)
            D = default;

        if (D.IsZero)
        {
            u = root(-q / 2, 3);

            yield return (a3 + 2 * u, default);
            yield return (a3 - u, default);
        }
        else if (D.IsPositive)
        {
            u = root(-q / 2 + D.Sqrt(), 3);
            v = root(-q / 2 - D.Sqrt(), 3);

            var x2 = (real: -(u + v) / 2 + a3, imag: Sqrt(3) / 2 * (u - v));

            yield return (u + v + a3, 0);
            yield return x2;
            yield return (x2.real, -x2.imag);
        }
        else
        {
            Scalar r = Sqrt(-p * p * p / 27);
            Scalar α = Atan(Sqrt(-D) / -q * 2);

            if (q > 0)
                α = 2.0 * PI - α;

            Scalar rr = root(r, 3);

            yield return (rr * (Cos((6 * PI - α) / 3) + Cos(α / 3.0)) + a3, 0);
            yield return (rr * (Cos((2 * PI + α) / 3) + Cos((4 * PI - α) / 3)) + a3, 0);
            yield return (rr * (Cos((4 * PI + α) / 3) + Cos((2 * PI - α) / 3)) + a3, 0);
        }
    }

    public static Polynomial GetChebychevInterpolationPolynomial<Func>(Func function, int degree)
        where Func : Function<Func, Scalar, Scalar>
    {
        (Scalar x, Scalar y)[] points = new (Scalar, Scalar)[degree + 1];
        Scalar c = Scalar.Pi / (2 * points.Length + 2);

        Parallel.For(0, points.Length, i =>
        {
            Scalar x = ((2 * i + 1) * c).Cos();

            points[i] = (x, function[x]);
        });

        return GetChebychevInterpolationPolynomial(points);
    }

    public static Polynomial GetChebychevInterpolationPolynomial(params (Scalar x, Scalar y)[] points)
    {
        int len = points.Length;

        if (len < 1)
            throw new ArgumentException("The chebychev polynomial needs at least one point, meaning that the input degree must be greater than zero.");

        Polynomial[] c = new Polynomial[len];
        Polynomial result = Zero;
        Scalar f = 2 / (len - 1d);

        Parallel.For(0, len, i =>
        {
            Scalar sum = 0;

            for (int j = 0; j < len; ++j)
                sum += points[j].y * (Scalar.Pi * i * (2 * j + 1) / (len * 2 + 2)).Cos();

            sum *= f;

            if (i == 0)
                sum /= 2;

            c[i] = sum * GetChebychevBasePolynomial(i);
        });

        for (int i = 0; i < len; ++i)
            result += c[i];

        return result;
    }

    /// <summary>
    /// Solves a cubic polynomials of the form 'AX² + BX + C'.
    /// <para/>
    /// 'A' must not be zero.
    /// </summary>
    public static Scalar[] SolveQuadratic(Scalar A, Scalar B, Scalar C)
    {
        if (A.IsZero)
            throw new ArgumentException($"The leading coefficient 'A' must not be zero.", nameof(A));

        IEnumerable<Scalar> solve()
        {
            Scalar discr = B * B - 4 * A * C;

            if (discr.IsZero)
                yield return -.5 * B / A;
            else if (discr.IsPositive)
            {
                Scalar q = B.IsPositive ? -.5 * (B + discr.Sqrt()) : -.5 * (B - discr.Sqrt());

                yield return q.Divide(A);
                yield return C.Divide(q);
            }
        }

        return (from x in solve()
                where !x.IsNaN
                where !x.IsInfinity
                orderby x ascending
                select x).Distinct().ToArray();
    }

    public static Polynomial Parse(string str)
    {
        str = str.RegexReplace(/* lang=regex */@"\s+", "")
                 .RegexReplace(/* lang=regex */@"⁺?(?<exp>[⁰¹²³⁴⁵⁶⁷⁸⁹]+)", m => '^' + string.Concat(m.Groups["exp"].Value.Select(c => c switch
                 {
                     '⁰' => "0",
                     '¹' => "1",
                     '²' => "2",
                     '³' => "3",
                     '⁴' => "4",
                     '⁵' => "5",
                     '⁶' => "6",
                     '⁷' => "7",
                     '⁸' => "8",
                     '⁹' => "9",
                     _ => throw new ArgumentException($"Invalid exponent character '{c}'.")
                 })));

        List<Polynomial> terms = [];

        while (str.Match(/* lang=regex */@"^(?<sign>[+\-]?)((?<coeff>(\d*\.)?\b\d+(e[+\-]?\d+)?)[fdm]?(\*?x\b(\^\+?(?<exp>[0-9]+)\b)?)?|\bx\b(\^\+?(?<exp2>[0-9]+)\b)?)", out Match match))
        {
            string c = match.Groups["coeff"].ToString();
            string e = match.Groups["exp"].ToString();

            if (c.Length == 0)
                c = "1";

            c = match.Groups["sign"] + c;

            if (e.Length == 0)
                e = match.Groups["exp2"].ToString();

            if (e.Length == 0)
                e = match.ToString().ToLower().Contains('x') ? "1" : "0";

            terms.Add(new Polynomial(Scalar.TryParse(c, out Scalar s) ? s : Scalar.Zero) >> int.Parse(e));
            str = str[match.Length..];
        }

        if (str.Length > 0)
            throw new ArgumentException($"Unparsable symbol sequence '{str}'.");

        return Zero.Add(terms.ToArray());
    }

    public static bool TryParse(string str, Polynomial? polynomial)
    {
        polynomial = null;

        try
        {
            polynomial = Parse(str);
        }
        catch
        {
        }

        return polynomial is null;
    }


    public static explicit operator Polynomial(string str) => Parse(str);
}

// public class BernsteinPolynomial
//     : Polynomial
// {
//     private readonly long _c, _n, _v;
//
//
//     public override BernsteinPolynomial Derivative
//     {
//         get
//         {
//             BernsteinPolynomial bs1 = new BernsteinPolynomial(_v - 1, _n - 1);
//             BernsteinPolynomial bs2 = new BernsteinPolynomial(_v, _n - 1);
//
//             return new DoubleFunction(x => _n * (bs1[x] - bs2[x]));
//         }
//     }
//
//     public IDoubleFunction Integral
//     {
//         get
//         {
//             List<BernsteinPolynomial> polynomials = new List<BernsteinPolynomial>();
//
//             for (long j = _v + 1; j < _n + 1; ++j)
//                 polynomials.Add(new BernsteinPolynomial(j, _n + 1));
//
//             return new DoubleFunction(x => polynomials.Select(b => b[x]).Sum() / (_n + 1));
//         }
//     }
//
//     FunctionCache<BernsteinPolynomial, scalar, scalar> IContinuousFunction<BernsteinPolynomial>.Cached => new FunctionCache<BernsteinPolynomial, scalar, scalar>(this);
//
//
//     private BernsteinPolynomial(long v, long n, long c)
//     {
//         if (n < 0)
//             throw new ArgumentException($"The parameter {nameof(n)} must be equal to or greater than zero.");
//         else if (v > n)
//             throw new ArgumentException($"The parameter {nameof(v)} must be smaller or equal to the parameter {nameof(n)}.");
//
//         _v = v;
//         _n = n;
//         _c = c;
//     }
//
//     public BernsteinPolynomial(long v, long n)
//         : this(v, n, MathExtensions.BinomialCoefficient(n, v))
//     {
//     }
//
//     public override double Evaluate(double x) => _c * x.FastPow(_v) * (1 - x).FastPow(_n - _v);
// }
