﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Common;

using numcplx = System.Numerics.Complex;


namespace Unknown6656.Mathematics.Analysis
{
    [StructLayout(LayoutKind.Sequential), NativeCppClass]
    public unsafe readonly /* ref */ struct Complex
        : IScalar<Complex>
        , Algebra<Scalar>.IVector<Complex>
        , Algebra<Scalar, Polynomial, ScalarMap>.IComposite1D
     // , Algebra<Complex, ComplexPolynomial, ComplexMap>.IComposite1D
        , IReadonlyNative<Complex>
    {
        #region PRIVATE FIELDS

#pragma warning disable IDE0032
        private readonly Scalar _re;
        private readonly Scalar _im;
#pragma warning restore

        #endregion
        #region STATIC PROPERTIES

#pragma warning disable IDE1006
        public static Complex i { get; } = new Complex(0, 1);

        public static Complex π { get; } = new Complex(Math.PI, 0);
#pragma warning restore IDE1006

        public static Complex One { get; } = new Complex(1, 0);

        public static Complex NegativeOne { get; } = new Complex(-1, 0);

        public static Complex Zero { get; } = default;

        public static int BinarySize { get; } = sizeof(Complex);

        #endregion
        #region INSTANCE PROPERTIES

        public readonly Scalar this[int index] => index == 0 ? _re : index == 1 ? _im : throw new IndexOutOfRangeException("Invalid index.");

        public readonly Complex this[int index, Scalar newval] => index == 0 ? (newval, _im) : index == 1 ? (_re, newval) : throw new IndexOutOfRangeException("Invalid index.");


        public readonly Scalar Real => _re;

        public readonly Scalar Imaginary => _im;

        public readonly Scalar Length => SquaredNorm.Sqrt();

        public readonly Scalar Modulus => SquaredNorm.Sqrt();

        public readonly Scalar SignedModulus => Modulus.Multiply(Real.Sign);

        public readonly Scalar Argument => Math.Atan2(_im, _re);

        public readonly Scalar Norm => Length;

        public readonly Scalar SquaredNorm => _re * _re + _im * _im;

        public readonly Scalar LogarithmicNorm => Length.Log();

        public readonly Complex Conjugate => new Complex(_re, -_im);

        public readonly Complex Absolute => Abs();

        public readonly Complex Normalized => IsZero ? Zero : this / Length;

        public readonly bool HasNaNs => _re.IsNaN || _im.IsNaN;

        public readonly bool HasNegatives => _re.IsNegative || _im.IsNegative;

        public readonly bool HasPositives => _re.IsPositive || _im.IsPositive;

        public readonly bool IsNegative => this < 0;

        public readonly bool IsPositive => this > 0;

        public readonly bool IsNegativeInfinity => SignedModulus.IsNegativeInfinity;

        public readonly bool IsPositiveInfinity => SignedModulus.IsPositiveInfinity;

        public readonly bool HasNegativeInfinity => _re.IsNegativeInfinity || _im.IsNegativeInfinity;

        public readonly bool HasPositiveInfinity => _re.IsPositiveInfinity || _im.IsPositiveInfinity;

        public readonly bool IsInfinity => _re.IsInfinity || _im.IsInfinity;

        public readonly bool IsFinite => _re.IsFinite && _im.IsFinite;

        public readonly bool IsI => Is(i);

        public readonly bool IsOne => Is(One);

        public readonly bool IsZero => Is(Zero);

        public readonly bool IsNonZero => !IsZero;

        public readonly bool IsNaN => _re.IsNaN && _im.IsNaN;

        public readonly bool IsReal => Imaginary.IsZero;

        public readonly bool IsNormalized => Length.IsOne;

        public readonly bool IsInvertible => !IsZero;

        public readonly bool IsBinary => IsOne || IsZero;

        public readonly bool IsBetweenZeroAndOne => IsReal && Real.IsBetweenZeroAndOne;

        public readonly bool IsInsideUnitSphere => Length.IsBetweenZeroAndOne;

        readonly int IReadonlyNative<Complex>.BinarySize => BinarySize;

        public readonly int Dimension => 2;

        public readonly Scalar[] Coefficients => new[] { _re, _im };

        public readonly Scalar Sum => _re + _im;

        readonly Scalar Algebra<Scalar>.IComposite.Min => _re.Min(_im);

        readonly Scalar Algebra<Scalar>.IComposite.Max => _re.Max(_im);

        readonly Scalar Algebra<Scalar>.IComposite.Avg => Sum / 2;

        public readonly Complex AdditiveInverse => Negate();

        public readonly Complex MultiplicativeInverse => Invert();

        public int Sign => CompareTo(Zero);

        public Complex DecimalPlaces => new Complex(Real.DecimalPlaces, Imaginary.DecimalPlaces);

        public Complex Floor => new Complex(Real.Floor, Imaginary.Floor);

        public Complex Ceiling => new Complex(Real.Ceiling, Imaginary.Ceiling);

        public Complex Rounded => new Complex(Real.Rounded, Imaginary.Rounded);

        public bool IsInteger => IsReal && Real.IsInteger;

        public bool IsPrime => IsInteger && Real.IsPrime;

        public Complex[] PrimeFactors => IsInteger ? Real.PrimeFactors.Select(f => new Complex(f)).ToArray() : Array.Empty<Complex>();

        public Complex Phi => IsInteger ? Real.Phi : throw new InvalidOperationException("Euler's totient function φ(n) is only defined for positive integer numbers.");

        #endregion
        #region CONSTRUCTORS

        public Complex(Scalar s)
            : this(s, Scalar.Zero)
        {
        }

        public Complex(numcplx c)
            : this(c.Real, c.Imaginary)
        {
        }

        public Complex(Scalar re, Scalar im)
            : this() => (_re, _im) = (re, im);

        #endregion
        #region INSTANCE METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Negate() => new Complex(-_re, -_im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Increment() => Add(One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Decrement() => Add(NegativeOne);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Add(in Complex second) => new Complex(_re + second._re, _im + second._im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Add(params Complex[] others) => others.Aggregate(this, (x, y) => x.Add(in y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Invert() => Conjugate.Divide(SquaredNorm);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Subtract(in Complex second) => new Complex(_re - second._re, _im - second._im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Subtract(params Complex[] others) => others.Aggregate(this, (x, y) => x.Subtract(in y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Multiply(in Complex second) => new Complex(_re * second._re - _im * second._im, _re * second._im + _im * second._re);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Multiply(params Complex[] others) => others.Aggregate(this, (x, y) => x.Multiply(in y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Multiply(Scalar factor) => new Complex(_re * factor, _im * factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Divide(Scalar factor) => new Complex(_re / factor, _im / factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Divide(in Complex second) => Multiply(second.Invert());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex ComponentwiseMultiply(in Complex second) => new Complex(_re * second._re, _im * second._im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex ComponentwiseDivide(in Complex second) => new Complex(_re / second._re, _im / second._im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Power(int e) => e == 0 ? One : e < 0 ? Power(-e).Invert() : Cis(Argument * e).Multiply(Length.Power(e));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Power(Scalar e) => Cis(Argument.Multiply(e)).Multiply(Length.Power(e));

        // z1 == a + bi == r1 * cis(φ1)
        // z2 == c + di == r2 * cis(φ2)
        //
        // z1^z2 == cis(c + log(r1) * d) * r1^c * exp(-φ1 * d)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Power(Complex e) => e.IsReal ? Power(e.Real) : Cis(e.Real + Length.Log().Multiply(e.Imaginary)).Multiply(Length.Power(e.Real) * (e.Imaginary.MultiplicativeInverse * Argument).Exp());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Rotate(Scalar angle) => FromPolarCoordinates(Length, Argument + angle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar AngleTo(in Complex second) => second.Subtract(this).Argument;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar DistanceTo(in Complex second) => second.Subtract(this).Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar[] ToArray() => new[] { _re, _im };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray<T>() where T : unmanaged => ToArray().CopyTo<Scalar, T>(sizeof(Complex));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToNative<T>(T* dst) where T : unmanaged => ToVector().ToNative(dst);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 ToVector() => new Vector2(_re, _im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix2 ToMatrix() => new Matrix2(
            Real, -Imaginary,
            Imaginary, Real
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ComplexPolynomial ToPolynomial() => new ComplexPolynomial(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is(Complex other, Scalar tolerance) => Real.Is(other.Real, tolerance) && Imaginary.Is(other.Imaginary, tolerance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is(Complex other) => Real.Is(other.Real) && Imaginary.Is(other.Imaginary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsNot(Complex other) => !Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Complex c) => Is(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object o) => o is Complex c && Equals(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(_re, _im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => IsReal ? _re.ToString() : _re.IsZero ? $"{_im}i" : $"{_re}{(_im < 0 ? '-' : '+')}{_im.AbsoluteValue}i";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(Complex other) => SignedModulus.CompareTo(other.SignedModulus);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Polynomial Algebra<Scalar, Polynomial, ScalarMap>.IComposite1D.ToPolynomial() => new Polynomial(_re, _im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IEucledianVectorSpace<Complex>.Dot(in Complex other) => ComponentwiseMultiply(in other).Sum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Cos() => Multiply(i).Cosh();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Sin() => Multiply(i).Sinh().Divide(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Tan() => Sin().Divide(Cos());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Cot() => Cos().Divide(Sin());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Cosh() => new Complex(_re.Cosh() * _im.Cos(), _re.Sinh() * _im.Sin());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Sinh() => new Complex(_re.Sinh() * _im.Cos(), _re.Cosh() * _im.Sin());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Tanh() => Sinh().Divide(Cosh());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Coth() => Cosh().Divide(Sinh());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Acos()
        {
            Complex x = 1 - (Real * Real) - (2 * Real * Imaginary * i) + (Imaginary * Imaginary);

            x = x.Sqrt()[0] + Multiply(i);
            x = x.Log().Multiply(i);

            return x.Add(π / 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Asin()
        {
            Complex x = 1 - (Real * Real) - (2 * Real * Imaginary * i) + (Imaginary * Imaginary);

            x = x.Sqrt()[0] + Multiply(i);

            return x.Log().Multiply(i).Negate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Atan()
        {
            Complex mi = Multiply(i);
            Complex x = One.Subtract(mi).Log();
            Complex y = One.Add(mi).Log();

            return x.Subtract(y).Multiply(i / 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Cis() => Cis(Imaginary).Multiply(Real.Exp());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Exp() => Cis();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Log() => new Complex(Length.Log(), Argument);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Abs() => new Complex(_re.Abs(), _im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Complex IScalar<Complex>.Sqrt() => Sqrt()[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex[] Sqrt() => IsReal && !IsNegative ? new Complex[] { Real.Sqrt() } : Roots(2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex[] Roots(int root)
        {
            List<Complex> roots = new List<Complex>(root);
            Scalar fac = Length.Power(1d / root);
            Scalar φ = Argument;
            Scalar τ = 2 * Math.PI;

            for (int i = 0; i < root; ++i)
                roots.Add(Cis((φ + τ * i) / root).Multiply(fac));

            return roots.Distinct().ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Complex P, Complex Q)? DecomposePQ(Complex phi) => IsInteger && phi.IsInteger ? Real.DecomposePQ(phi.Real) : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Min(Complex second) => CompareTo(second) <= 0 ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Max(Complex second) => CompareTo(second) >= 0 ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex LinearInterpolate(Complex other, Scalar factor) => this * (1 - factor) + other * factor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Clamp() => Clamp(Zero, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Clamp(Complex low, Complex high) => Max(low, Min(this, high));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Clamp(Scalar low, Scalar high) => Clamp((Complex)low, (Complex)high);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsLinearDependant(in Complex other, out Scalar? factor) => (factor = Divide(other) is { IsReal: true, Real: Scalar r } ? (Scalar?)r : null) != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsOrthogonal(in Complex second) => Argument.Subtract(second.Argument).Abs().Modulus(Math.PI).Is(Math.PI / 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Reflect(in Complex normal) => ToVector().Reflect(normal.ToVector());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Refract(in Complex normal, Scalar eta, out Complex refracted)
        {
            bool res = ToVector().Refract(normal.ToVector(), eta, out Vector2 refr);

            refracted = refr;

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex LinearInterpolate(in Complex other, Scalar factor) => Multiply(Scalar.NegativeOne.Add(factor)).Add(other.Multiply(factor));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex SwapEntries() => new Complex(_im, _re);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex SwapEntries(int src_idx, int dst_idx)
        {
            if (src_idx < 0 || src_idx >= 2)
                throw new ArgumentOutOfRangeException(nameof(src_idx), $"The source index {src_idx} is invalid: The index must be a positive integer smaller than two.");
            else if (dst_idx < 0 || dst_idx >= 2)
                throw new ArgumentOutOfRangeException(nameof(dst_idx), $"The destination index {dst_idx} is invalid: The index must be a positive integer smaller than two.");
            else if (src_idx == dst_idx)
                return this;
            else
                return SwapEntries();
        }

        #endregion
        #region STATIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(Complex c1, Complex c2, Scalar? error = null) => c1.Is(c2, error ?? Scalar.ComputationalEpsilon);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Random() => new Complex(Scalar.Random(), Scalar.Random());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cos(Complex c) => c.Cos();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Sin(Complex c) => c.Sin();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Tan(Complex c) => c.Tan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cis(Complex c) => c.Cis();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cis(Scalar s) => FromPolarCoordinates(1, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Exp(Complex c) => c.Exp();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Log(Complex c) => c.Log();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Abs(Complex c) => c.Abs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Add(Complex c1, Complex c2) => c1.Add(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Subtract(Complex c1, Complex c2) => c1.Subtract(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Multiply(Complex c1, Complex c2) => c1.Multiply(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Divide(Complex c1, Complex c2) => c1.Divide(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Power(Complex c1, Complex c2) => c1.Power(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Min(Complex c1, Complex c2) => c1.Min(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Max(Complex c1, Complex c2) => c1.Max(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex[] Sqrt(Complex c) => c.Sqrt();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex[] Roots(Complex c, int root) => c.Roots(root);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromSize(Size sz) => new Complex(sz.Width, sz.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromSize(SizeF sz) => new Complex(sz.Width, sz.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromPoint(Point pt) => new Complex(pt.X, pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromPoint(PointF pt) => new Complex(pt.X, pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromVector2(Vector2 v) => v;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromCartesianCoordinates(Point p) => new Complex(p.X, p.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromCartesianCoordinates(PointF p) => new Complex(p.X, p.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromCartesianCoordinates(Vector2 v) => v;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromCartesianCoordinates(Scalar x, Scalar y) => new Complex(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromPolarCoordinates(Scalar r, Scalar φ) => new Complex(r * φ.Cos(), r * φ.Sin());

        // TODO : parse

        #endregion
        #region OPERATORS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex c) => c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex c) => c.Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex c1, Complex c2) => c1.Add(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex c1, Complex c2) => c1.Subtract(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex c1, Complex c2) => c1.Multiply(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator ^(Complex c1, Complex c2) => c1.Power(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator /(Complex c1, Complex c2) => c1.Divide(c2);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Complex operator %(Complex c1, Complex c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Complex c1, Complex c2) => c1.CompareTo(c2) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Complex c1, Complex c2) => c1.CompareTo(c2) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Complex c1, Complex c2) => !(c1 > c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Complex c1, Complex c2) => !(c1 < c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Complex c1, Complex c2) => c1.Is(c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Complex c1, Complex c2) => !(c1 == c2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator (Scalar re, Scalar im)(Complex c) => (c._re, c._im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Complex((Scalar re, Scalar im) t) => new Complex(t.re, t.im);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator numcplx(Complex c) => new numcplx(c.Real, c.Imaginary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Complex(numcplx c) => new Complex(c.Real, c.Imaginary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Complex(Scalar s) => new Complex(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Complex(double d) => new Complex((Scalar)d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Complex(Vector2 v) => new Complex(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Complex c) => c.ToVector();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(Complex c) => c._re;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(Complex c) => c._re;

        #endregion
    }
}
