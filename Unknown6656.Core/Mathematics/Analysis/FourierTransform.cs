using System.Collections.Generic;
using System.Text;
using System;

using static System.Math;

using Complex = System.Numerics.Complex; // todo : define complex


namespace Unknown6656.Mathematics.Analysis
{
    /// <summary>
    /// Fourier transformation direction.
    /// </summary>
    public enum FourierDirection
    {
        /// <summary>
        /// Forward direction of Fourier transformation.
        /// </summary>
        Forward = 1,
        /// <summary>
        /// Backward direction of Fourier transformation.
        /// </summary>
        Backward = -1
    };

    /// <summary>
    /// Fourier transformation.
    /// </summary>
    /// <remarks>The class implements one dimensional and two dimensional Discrete and Fast Fourier Transformation.</remarks>
    public static unsafe class FourierTransform
    {
        private const int MIN_LENGTH = 2;
        private const int MAX_LENGTH = 16384;
        private const int MIN_BITS = 1;
        private const int MAX_BITS = 14;

        private static readonly Complex[,][] _rot = new Complex[MAX_BITS, 2][];
        private static readonly int[][] _rev = new int[MAX_BITS][];
        private static readonly object _mutex = new object();


        /// <summary>
        /// One dimensional Discrete Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        public static void DFT(Span<Complex> data, FourierDirection direction)
        {
            double arg;
            int n = data.Length;
            Complex[] dst = new Complex[n];

            fixed (Complex* psrc = data)
            fixed (Complex* pdst = dst)
            {
                for (int i = 0; i < n; i++)
                {
                    pdst[i] = Complex.Zero;
                    arg = -(int)direction * 2 * PI * i / (double)n;

                    for (int j = 0; j < n; j++)
                        pdst[i] += psrc[j] * Complex.FromPolarCoordinates(1, j * arg);
                }

                if (direction == FourierDirection.Forward)
                    for (int i = 0; i < n; i++)
                        psrc[i] = pdst[i] / n;
                else
                    for (int i = 0; i < n; i++)
                        psrc[i] = pdst[i];
            }
        }

        /// <summary>
        /// Two dimensional Discrete Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        public static void DFT2(Complex[,] data, FourierDirection direction)
        {
            int n = data.GetLength(0);
            int m = data.GetLength(1);
            Complex[] dst = new Complex[Max(n, m)];
            double arg;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    dst[j] = Complex.Zero;

                    arg = -(int)direction * 2 * PI * j / (double)m;

                    for (int k = 0; k < m; k++)
                        dst[j] += data[i, k] * Complex.FromPolarCoordinates(1, k * arg);
                }

                if (direction == FourierDirection.Forward)
                    for (int j = 0; j < m; j++)
                        data[i, j] = dst[j] / m;
                else
                    for (int j = 0; j < m; j++)
                        data[i, j] = dst[j];
            }

            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    dst[i] = Complex.Zero;
                    arg = -(int)direction * 2 * PI * i / (double)n;

                    for (int k = 0; k < n; k++)
                        dst[i] += data[k, j] * Complex.FromPolarCoordinates(1, k * arg);
                }

                if (direction == FourierDirection.Forward)
                    for (int i = 0; i < n; i++)
                        data[i, j] = dst[i] / n;
                else
                    for (int i = 0; i < n; i++)
                        data[i, j] = dst[i];
            }
        }

        /// <summary>
        /// One dimensional Fast Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        /// <remarks>
        /// <para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
        /// only, where <b>n</b> may vary in the [1, 14] range.</note></para>
        /// </remarks>
        public static void FFT(Span<Complex> data, FourierDirection direction)
        {
            int n = data.Length;
            int m = (int)Log(n, 2);
            int tn = 1, tm;

            lock (_mutex)
            {
                ReorderData(data);

                for (int k = 1; k <= m; k++)
                {
                    Complex[] rot = GetComplexRotation(k, direction);

                    tm = tn;
                    tn <<= 1;

                    for (int i = 0; i < tm; i++)
                    {
                        var t = rot[i];

                        for (int even = i; even < n; even += tn)
                        {
                            int odd = even + tm;
                            var ce = data[even];
                            var cot = data[odd] * t;

                            data[even] += cot;
                            data[odd] = ce - cot;
                        }
                    }
                }
            }

            if (direction == FourierDirection.Forward)
                for (int i = 0; i < n; i++)
                    data[i] /= (double)n;
        }

        /// <summary>
        /// Two dimensional Fast Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
        /// only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
        /// is valid, but 15x15 is not.</note></para></remarks>
        public static void FFT2(Complex[,] data, FourierDirection direction)
        {
            int k = data.GetLength(0);
            int n = data.GetLength(1);

            // check data size
            if (!k.IsPowerOf2() || !n.IsPowerOf2() ||
                (k < MIN_LENGTH) || (k > MAX_LENGTH) ||
                (n < MIN_LENGTH) || (n > MAX_LENGTH))
                throw new ArgumentException("Incorrect data length.");

            Complex[] row = new Complex[n];
            Complex[] col = new Complex[k];

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < n; j++)
                    row[j] = data[i, j];

                FFT(row, direction);

                for (int j = 0; j < n; j++)
                    data[i, j] = row[j];
            }

            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < k; i++)
                    col[i] = data[i, j];

                FFT(col, direction);

                for (int i = 0; i < k; i++)
                    data[i, j] = col[i];
            }
        }

        private static int[] GetReversedBits(int bits)
        {
            if ((bits < MIN_BITS) || (bits > MAX_BITS))
                throw new ArgumentOutOfRangeException();

            if (_rev[bits - 1] == null)
            {
                int n = (int)Pow(bits, 2);
                int[] rBits = new int[n];

                for (int i = 0; i < n; i++)
                {
                    int oldBits = i;
                    int newBits = 0;

                    for (int j = 0; j < bits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = oldBits >> 1;
                    }

                    rBits[i] = newBits;
                }

                _rev[bits - 1] = rBits;
            }

            return _rev[bits - 1];
        }

        private static Complex[] GetComplexRotation(int bits, FourierDirection direction)
        {
            int directionIndex = (direction == FourierDirection.Forward) ? 0 : 1;

            if (_rot[bits - 1, directionIndex] == null)
            {
                int n = 1 << (bits - 1);
                double uR = 1.0;
                double uI = 0.0;
                double angle = PI / n * (int)direction;
                double wR = Cos(angle);
                double wI = Sin(angle);
                double t;
                Complex[] rotation = new Complex[n];

                for (int i = 0; i < n; i++)
                {
                    rotation[i] = new Complex(uR, uI);

                    t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                _rot[bits - 1, directionIndex] = rotation;
            }

            return _rot[bits - 1, directionIndex];
        }

        private static void ReorderData(Span<Complex> data)
        {
            int len = data.Length;

            if ((len < MIN_LENGTH) || (len > MAX_LENGTH) || !len.IsPowerOf2())
                throw new ArgumentException("Incorrect data length.");

            int[] rbits = GetReversedBits((int)Log(len,2));

            fixed (int* ptr = rbits)
                for (int i = 0; i < len; i++)
                {
                    int s = ptr[i];

                    if (s > i)
                    {
                        Complex t = data[i];

                        data[i] = data[s];
                        data[s] = t;
                    }
                }
        }
    }
}
