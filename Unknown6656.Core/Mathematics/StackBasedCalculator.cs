using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Generics;
using Unknown6656.Common;

using bint = System.Numerics.BigInteger;
using data = Unknown6656.Union<Unknown6656.Mathematics.Analysis.Complex, System.Numerics.BigInteger>;

namespace Unknown6656.Mathematics
{
    [Obsolete]
    public sealed class StackBasedCalculator
    {
        #region PROPERTIES

        private readonly Stack<data> _stack = new Stack<data>();

        public data[] Stack => _stack.ToArray();

        public data TopMost => _stack.Peek();

        public int Size => _stack.Count;

        #endregion
        #region STACK METHODS

        public void Swap() => Swap(2);

        public void Swap(int depth)
        {
            data[] stack = new data[depth];

            for (int i = 0; i < depth; ++i)
                stack[i] = Pop();

            Push(stack[0]);

            for (int i = depth - 1; i > 0; --i)
                Push(stack[i]);
        }

        public void Duplicate() => Push(TopMost);

        public void Push(data v) => _stack.Push(v);

        public void Push(bint b) => Push(b);

        public void Push(Complex c) => Push(c);

        public data Pop() => _stack.Pop();

        public bint PopBigInteger() => Pop().Match(c => c.Real, LINQ.id);

        public Complex PopComplex() => Pop().Match(LINQ.id, c => new((Scalar)(decimal)c));

        private void UnaryOperator(Func<bint, bint> fb, Func<Complex, Complex> fc) => UnaryOperator(d =>
        {
            if (d.Is(out bint b))
                return fb(b);
            else if (d.Is(out Complex c))
                return fc(c);
            else
                throw new InvalidOperationException($"The current opeation is not defined for a stack element of the type '{d.UnsafeItem?.GetType()}'.");
        });

        private void UnaryOperator(Func<data, data> op) => Push(op(Pop()));

        private void BinaryOperator(Func<bint, bint, bint> fb, Func<Complex, Complex, Complex> fc) => BinaryOperator((d1, d2) =>
        {
            if (d1.Is(out bint b1) && d2.Is(out bint b2))
                return fb(b1, b2);

            return fc((Complex)d1.UnsafeItem, (Complex)d2.UnsafeItem);
        });

        private void BinaryOperator(Func<data, data, data> op)
        {
            data snd = Pop();
            data fst = Pop();

            Push(op(fst, snd));
        }

        private void TernaryOperator(Func<data, data, data, data> op)
        {
            data thr = Pop();
            data snd = Pop();
            data fst = Pop();

            Push(op(fst, snd, thr));
        }

        #endregion
        #region MATHEMATICAL METHODS

        public void CastToBint() => Push(PopBigInteger());

        public void CastToComplex() => Push(PopComplex());

        public void Add() => BinaryOperator(bint.Add, Complex.Add);

        public void Subtract() => BinaryOperator(bint.Subtract, Complex.Subtract);

        public void Multiply() => BinaryOperator(bint.Multiply, Complex.Multiply);

        public void Divide() => BinaryOperator(bint.Divide, Complex.Divide);

        public void Power() => BinaryOperator((b, e) => bint.Pow(b, (int)e), Complex.Power);

        // MOD
        // ABS
        // SIGN
        // DELTA
        // SIN
        // COS
        // TAN
        // COT
        // SEC
        // CSC
        // ASIN
        // ACOS
        // ATAN
        // ACOT
        // ASEC
        // ACSC
        // SINH
        // COSH
        // TANH
        // COTH
        // SECH
        // CSCH
        // ASINH
        // ACOSH
        // ATANH
        // ACOTH
        // ASECH
        // ACSCH
        // CIS
        // CONJ
        // EXP
        // LOG_e
        // LOG_2
        // LOG_10
        // LOG_<>
        // IF
        // BOOL
        // NOT
        // AND
        // OR
        // SHL
        // SHR
        // ROL
        // ROR
        // EQ
        // NEQ
        // LT
        // LTE
        // GT
        // GTE
        // RAND
        // ONE_MINUS
        // INVERT
        // NEGATE
        // FLOOR
        // CEILING
        // ROUND
        // SQRT
        // ROOT_n
        // FAC / PLS

        // c: i
        // c: ZERO
        // c: ONE
        // c: PI
        // c: TAU
        // c: PHI
        // c: e
        // c: SQRT2

        #endregion
    }
}
