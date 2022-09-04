using System.Threading.Tasks;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Cryptography;


public unsafe class TextbookRSA
{
    public static byte[] Encrypt(ModuloRing @public, byte[] M)
    {
        byte[] C = new byte[M.Length * 4];

        fixed (byte* pc = C)
        {
            int* ptr = (int*)pc;

            Parallel.For(0, M.Length, i => ptr[i] = Encrypt(@public, M[i]));
        }

        return C;
    }

    public static byte[] Decrypt(ModuloRing @private, byte[] C)
    {
        byte[] M = new byte[C.Length / 4];

        fixed (byte* pc = C)
        {
            int* ptr = (int*)pc;

            Parallel.For(0, M.Length, i => M[i] = Decrypt(@private, ptr[i]));
        }

        return M;
    }

    private static int Encrypt(ModuloRing @public, byte M) => (int)bint.ModPow(M + 2, @public.Value, @public.Modulus);

    private static byte Decrypt(ModuloRing @private, int C) => (byte)(bint.ModPow(C, @private.Value, @private.Modulus) - 2);

    public static (ModuloRing @public, ModuloRing @private) GenerateKeyPair()
    {
        static bint random_prime() => MathExtensions._primes[Numerics.Random.XorShift.NextInt(0, MathExtensions._primes.Length)];
        static (bint N, ModuloRing e) GenPublicKey()
        {
            bint P = random_prime();
            bint Q = P;

            while (P == Q)
                Q = random_prime();

            bint N = P * Q;
            bint φ = (P - 1) * (Q - 1);
            bint e = φ;

            while ((φ % e) == 0 || (e >= φ))
                e = random_prime();

            return (N, (e, φ));
        }

        ModuloRing e, d;
        bint N;

        do
        {
            (N, e) = GenPublicKey();
            d = e.Invert();
        }
        while (d == e || N < 514 || bint.Log(N, 2) > 30);

        return (e, d);
    }
}
