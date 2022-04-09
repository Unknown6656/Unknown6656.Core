using System.Threading.Tasks;
using System.Linq;

using Unknown6656.Mathematics.LinearAlgebra;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Mathematics.Numerics;


public abstract class Noise
{
    public abstract Random RandomNumberGenerator { get; }

    /// <inheritdoc cref="GetValue(Scalar)"/>
    public Scalar this[Scalar x] => GetValue(x);

    /// <inheritdoc cref="GetValue(Vector2)"/>
    public Scalar this[Vector2 xy] => GetValue(xy);

    /// <inheritdoc cref="GetValue(Scalar, Scalar)"/>
    public Scalar this[Scalar x, Scalar y] => GetValue(x, y);

    /// <inheritdoc cref="GetValue(Vector3)"/>
    public Scalar this[Vector3 xyz] => GetValue(xyz);

    /// <inheritdoc cref="GetValue(Scalar, Scalar, Scalar)"/>
    public Scalar this[Scalar x, Scalar y, Scalar z] => GetValue(x, y, z);


    public abstract void Reseed();

    public Scalar GetValue(Scalar x) => GetValue(x, 0, 0);

    public Scalar GetValue(Vector2 xy) => GetValue(xy.X, xy.Y, 0);

    public Scalar GetValue(Scalar x, Scalar y) => GetValue(x, y, 0);

    public Scalar GetValue(Vector3 xyz) => GetValue(xyz.X, xyz.Y, xyz.Z);

    public abstract Scalar GetValue(Scalar x, Scalar y, Scalar z);
}

public sealed class PerlinNoise
    : Noise
{
    public const int PERMUTATION_SIZE = 256;

    private readonly Vector3[] _gradients;
    private int[] _permutation;


    public override Random RandomNumberGenerator => Settings.RandomNumberGenerator;

    public PerlinNoiseSettings Settings { get; }


    public PerlinNoise(Random random)
        : this(new PerlinNoiseSettings(random))
    {
    }

    public PerlinNoise(PerlinNoiseSettings settings)
    {
        Settings = settings;
        _permutation = CalculatePermutation();
        _gradients = CalculateGradients();
    }

    public override void Reseed() => _permutation = CalculatePermutation();

    private int[] CalculatePermutation()
    {
        int[] perm = Enumerable.Range(0, PERMUTATION_SIZE).ToArray();

        for (int i = 0; i < perm.Length; ++i)
        {
            int source = RandomNumberGenerator.NextInt(perm.Length);
            int t = perm[i];

            perm[i] = perm[source];
            perm[source] = t;
        }

        return perm;
    }

    private Vector3[] CalculateGradients()
    {
        Vector3[] arr = new Vector3[PERMUTATION_SIZE];
        Random rng = RandomNumberGenerator;

        for (int i = 0; i < arr.Length; i++)
        {
            Vector3 v;

            do
                v = (rng.NextScalar(-1, 1), rng.NextScalar(-1, 1), rng.NextScalar(-1, 1));
            while (v.SquaredNorm >= 1);

            arr[i] = v.Normalized;
        }

        return arr;
    }

    public override Scalar GetValue(Scalar x, Scalar y, Scalar z)
    {
        static Scalar Drop(Scalar t)
        {
            t = t.Abs();

            return 1 - t * t * t * (t * (t * 6 - 15) + 10);
        }
        Vector3 cell = (x.Floor, y.Floor, z.Floor);
        Scalar total = 0;

        foreach (Vector3 corner in new Vector3[]
        {
            (0, 0, 0),
            (0, 0, 1),
            (0, 1, 0),
            (0, 1, 1),
            (1, 0, 0),
            (1, 0, 1),
            (1, 1, 0),
            (1, 1, 1),
        })
        {
            Vector3 ijk = cell + corner;
            int idx = _permutation[(int)ijk.X % _permutation.Length];

            idx = _permutation[(idx + (int)ijk.Y) % _permutation.Length];
            idx = _permutation[(idx + (int)ijk.Z) % _permutation.Length];

            Vector3 grad = _gradients[idx % _gradients.Length];
            Scalar u = x - ijk.X;
            Scalar v = y - ijk.Y;
            Scalar w = z - ijk.Z;

            total += Drop(u) * Drop(v) * Drop(w) * (grad * (u, v, w));
        }

        return total.Clamp(-1, 1);
    }

    public Scalar[] GenerateNoiseMap1D(int width)
    {
        Scalar[,,] noise = GenerateNoiseMap3D(width, 1, 1);
        Scalar[] data = new Scalar[width];

        Parallel.For(0, width, i => data[i] = noise[i, 0, 0]);

        return data;
    }

    public Scalar[,] GenerateNoiseMap2D(int width, int height)
    {
        Scalar[,,] noise = GenerateNoiseMap3D(width, height, 1);
        Scalar[,] data = new Scalar[width, height];

        Parallel.For(0, width * height, i => data[i % width, i / width] = noise[i % width, i / width, 0]);

        return data;
    }

    public Scalar[,,] GenerateNoiseMap3D(int width, int height, int depth)
    {
        Scalar[,,] data = new Scalar[width, height, depth];
        Scalar freq = Settings.Frequency;
        Scalar amp = Settings.Amplitude;
        Scalar min = Scalar.MaxValue;
        Scalar max = Scalar.MinValue;

        for (int octave = 0; octave < Settings.Octaves; ++octave)
        {
            Parallel.For(0, width * height, z =>
            {
                int i = z % width;
                int j = z / width;

                for (int k = 0; k < depth; ++k)
                {
                    Scalar noise = GetValue(i * freq / width, j * freq / height, k * freq / depth);

                    noise = data[i, j, k] += noise * amp;
                    min = min.Min(noise);
                    max = max.Max(noise);
                }
            });

            freq *= 2;
            amp /= 2;
        }

        return data;
    }
}

public class PerlinNoiseSettings
{
    public long InitialSeed => RandomNumberGenerator.Seed;
    public Random RandomNumberGenerator { get; }
    public Scalar Frequency { set; get; } = 1;
    public Scalar Amplitude { set; get; } = 1;
    public int Octaves { set; get; } = 16;


    public PerlinNoiseSettings(Random rng) => RandomNumberGenerator = rng;
}
