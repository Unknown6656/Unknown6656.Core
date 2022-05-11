using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Analysis;


/// <completionlist cref="ScalarFunction"/>
public partial class ScalarFunction
{
    public static ScalarFunction Round { get; } = new(c => c.Rounded);
    public static ScalarFunction Floor { get; } = new(c => c.Floor);
    public static ScalarFunction Ceiling { get; } = new(c => c.Ceiling);

    public static ScalarFunction NaturalLogarithm { get; } = new(c => c.Log());
    public static ScalarFunction Logarithm10 { get; } = new(c => Scalar.Log(c, 10));
    public static ScalarFunction Logarithm2 { get; } = new(c => Scalar.Log(c, 2));
    public static ScalarFunction Exponential { get; } = new(c => c.Exp());
    public static ScalarFunction Logistic { get; } = new(c => c.MultiplicativeInverse.Exp().Increment().MultiplicativeInverse);
    public static ScalarFunction Logit { get; } = new(c => Scalar.One.Subtract(c).MultiplicativeInverse.Log());

    public static ScalarFunction Sinc { get; } = new(c => c.Sinc());
    public static ScalarFunction Sine { get; } = new(c => c.Sin());
    public static ScalarFunction Cosine { get; } = new(c => c.Cos());
    public static ScalarFunction Tangent { get; } = new(c => c.Tan());
    public static ScalarFunction Cotangent { get; } = new(c => c.Cot());
    public static ScalarFunction Secant { get; } = new(c => c.Sec());
    public static ScalarFunction Cosecant { get; } = new(c => c.Csc());
    public static ScalarFunction InverseSine { get; } = new(c => c.Asin());
    public static ScalarFunction InverseCosine { get; } = new(c => c.Acos());
    public static ScalarFunction InverseTangent { get; } = new(c => c.Atan());
    public static ScalarFunction InverseCotangent { get; } = new(c => c.Acot());
    public static ScalarFunction InverseSecant { get; } = new(c => c.Asec());
    public static ScalarFunction InverseCosecant { get; } = new(c => c.Acsc());

    public static ScalarFunction HyperbolicSine { get; } = new(c => c.Sinh());
    public static ScalarFunction HyperbolicCosine { get; } = new(c => c.Cosh());
    public static ScalarFunction HyperbolicTangent { get; } = new(c => c.Tanh());
    public static ScalarFunction HyperbolicCotangent { get; } = new(c => c.Coth());
    public static ScalarFunction HyperbolicSecant { get; } = new(c => c.Sech());
    public static ScalarFunction HyperbolicCosecant { get; } = new(c => c.Csch());
    public static ScalarFunction InverseHyperbolicSine { get; } = new(c => c.Asinh());
    public static ScalarFunction InverseHyperbolicCosine { get; } = new(c => c.Acosh());
    public static ScalarFunction InverseHyperbolicTangent { get; } = new(c => c.Atanh());
    public static ScalarFunction InverseHyperbolicCotangent { get; } = new(c => c.Acoth());
    public static ScalarFunction InverseHyperbolicSecant { get; } = new(c => c.Asech());
    public static ScalarFunction InverseHyperbolicCosecant { get; } = new(c => c.Acsch());

    public static ScalarFunction UnitParabola { get; } = new(c => c * c);
    public static ScalarFunction UnitTent { get; } = new(c => c.Min(1 - c));

    public static ScalarFunction Gamma { get; } = new(c => ComplexFunction.Gamma[c].Real);
    public static ScalarFunction Stirling { get; } = new(c => ComplexFunction.Stirling[c].Real);

    public static ScalarFunction Tent(Scalar height, Scalar skew) => new(c => height * (c / skew).Min((1 - c) / (1 - skew)));
}

/// <completionlist cref="ComplexFunction"/>
public partial class ComplexFunction
{
    private const double MAXSTIR = 143.01608;
    private static readonly ComplexPolynomial gamma_P = new(
        1.60119522476751861407E-4,
        1.19135147006586384913E-3,
        1.04213797561761569935E-2,
        4.76367800457137231464E-2,
        2.07448227648435975150E-1,
        4.94214826801497100753E-1,
        9.99999999999999996796E-1
    );
    private static readonly ComplexPolynomial gamma_Q = new(
        -2.31581873324120129819E-5,
        5.39605580493303397842E-4,
        -4.45641913851797240494E-3,
        1.18139785222060435552E-2,
        3.58236398605498653373E-2,
        -2.34591795718243348568E-1,
        7.14304917030273074085E-2,
        1.00000000000000000320E0
    );
    private static readonly ComplexPolynomial STIR = new(
        7.87311395793093628397E-4,
        -2.29549961613378126380E-4,
        -2.68132617805781232825E-3,
        3.47222221605458667310E-3,
        8.33333333333482257126E-2
    );

    public static ComplexFunction Round { get; } = new(c => c.Rounded);
    public static ComplexFunction Floor { get; } = new(c => c.Floor);
    public static ComplexFunction Ceiling { get; } = new(c => c.Ceiling);

    public static ComplexFunction NaturalLogarithm { get; } = new(c => c.Log());
    public static ComplexFunction Logarithm10 { get; } = new(c => c.Log10());
    public static ComplexFunction Logarithm2 { get; } = new(c => c.Log2());
    public static ComplexFunction Exponential { get; } = new(c => c.Exp());
    public static ComplexFunction Logistic { get; } = new(c => c.MultiplicativeInverse.Exp().Increment().MultiplicativeInverse);
    public static ComplexFunction Logit { get; } = new(c => Complex.One.Subtract(c).MultiplicativeInverse.Log());

    public static ComplexFunction Sinc { get; } = new(c => c.Sinc());
    public static ComplexFunction Sine { get; } = new(c => c.Sin());
    public static ComplexFunction Cosine { get; } = new(c => c.Cos());
    public static ComplexFunction Tangent { get; } = new(c => c.Tan());
    public static ComplexFunction Cotangent { get; } = new(c => c.Cot());
    public static ComplexFunction Secant { get; } = new(c => c.Sec());
    public static ComplexFunction Cosecant { get; } = new(c => c.Csc());
    public static ComplexFunction InverseSine { get; } = new(c => c.Asin());
    public static ComplexFunction InverseCosine { get; } = new(c => c.Acos());
    public static ComplexFunction InverseTangent { get; } = new(c => c.Atan());
    public static ComplexFunction InverseCotangent { get; } = new(c => c.Acot());
    public static ComplexFunction InverseSecant { get; } = new(c => c.Asec());
    public static ComplexFunction InverseCosecant { get; } = new(c => c.Acsc());

    public static ComplexFunction HyperbolicSine { get; } = new(c => c.Sinh());
    public static ComplexFunction HyperbolicCosine { get; } = new(c => c.Cosh());
    public static ComplexFunction HyperbolicTangent { get; } = new(c => c.Tanh());
    public static ComplexFunction HyperbolicCotangent { get; } = new(c => c.Coth());
    public static ComplexFunction HyperbolicSecant { get; } = new(c => c.Sech());
    public static ComplexFunction HyperbolicCosecant { get; } = new(c => c.Csch());
    public static ComplexFunction InverseHyperbolicSine { get; } = new(c => c.Asinh());
    public static ComplexFunction InverseHyperbolicCosine { get; } = new(c => c.Acosh());
    public static ComplexFunction InverseHyperbolicTangent { get; } = new(c => c.Atanh());
    public static ComplexFunction InverseHyperbolicCotangent { get; } = new(c => c.Acoth());
    public static ComplexFunction InverseHyperbolicSecant { get; } = new(c => c.Asech());
    public static ComplexFunction InverseHyperbolicCosecant { get; } = new(c => c.Acsch());

    public static ComplexFunction Gamma { get; } = new(x =>
    {
        Complex p, z;
        Complex q = x.Absolute;

        if (q > 33)
            if (x.IsNegative)
            {
                p = q.Floor;

                if (p.Is(q))
                    throw new OverflowException();

                z = q.Subtract(p);

                if (z > .5)
                {
                    p = p.Increment();
                    z = q.Subtract(p);
                }

                z = z.Multiply(Complex.Pi).Sin().Multiply(in q);

                if (z.IsZero)
                    throw new OverflowException();

                z = z.Absolute;
                z = Complex.Pi.Divide(z.Multiply(Stirling[q]));

                return -z;
            }
            else
                return Stirling[x];

        z = Complex.One;

        while (x >= 3)
        {
            x.Decrement();
            z = z.Multiply(x);
        }

        while (x <= 0)
        {
            if (x.IsZero)
                throw new ArithmeticException();
            else if (x > -1e-9)
                return z.Divide(x.Multiply(1 + .5772156649015329 * x));

            z = z.Divide(x);
            x = x.Increment();
        }

        while (x < 2)
        {
            if (x.IsZero)
                throw new ArithmeticException();
            else if (x < 1e-9)
                return z.Divide(x.Multiply(1 + .5772156649015329 * x));

            z = z.Divide(x);
            x = x.Increment();
        }

        if (x.Is(2) || x.Is(3))
            return z;

        x -= 2;
        p = gamma_P[x];
        q = gamma_Q[x];

        return z.Multiply(p).Divide(q);

    });

    public static ComplexFunction Stirling { get; } = new(x =>
    {
        Complex w = x.MultiplicativeInverse;
        Complex y = x.Exp();

        w = w.Multiply(STIR[w]).Increment();

        if (x > MAXSTIR)
        {
            Complex v = x.Power(x.Multiply(.5).Subtract(.25));

            if (!v.HasPositiveInfinity && y.HasPositiveInfinity)
            {
                // lim x -> inf { (x^(0.5*x - 0.25)) * (x^(0.5*x - 0.25) / exp(x))  }
                y = Scalar.PositiveInfinity;
            }
            else
                y = v.Multiply(v.Divide(y));
        }
        else
            y = x.Power(x.Subtract(.5)).Divide(y);

        y = y.Multiply(w, .91893853320467274178032973640562);

        return y;
    });
}
