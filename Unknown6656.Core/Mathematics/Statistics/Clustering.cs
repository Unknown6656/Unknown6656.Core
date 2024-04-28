using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.Numerics;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Statistics;


public record Cluster<Item>(Clustering<Item> Source, int ClusterID, Item[] Values)
    : IEnumerable<Item>
{
    public ClusteringConfiguration<Item> Configuration => Source.Configuration;

    public int ClusterSize => Values.Length;

    public double[] MeanCoefficients { get; } = LINQ.Do(delegate
    {
        int dim = Source.Configuration.InputDimensionality;
        var get = Source.Configuration.GetCoefficients;
        double[] mean = new double[dim];

        foreach (double[] coeff in Values.Select(get))
            for (int i = 0; i < mean.Length; ++i)
                mean[i] += coeff[i];

        for (int i = 0; i < mean.Length; ++i)
            mean[i] /= Values.Length;

        return mean;
    });

    // public double MeanDistance { get; } = Values.Select(v => v.Distance).Average();


    public IEnumerator<Item> GetEnumerator() => ((IEnumerable<Item>)Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();

    public Item GetCenterItem()
    {
        int dim = Configuration.InputDimensionality;
        double[] mean = MeanCoefficients;
        double[] dist = new double[ClusterSize];

        Parallel.For(0, ClusterSize, i =>
        {
            double[] coeff = Configuration.GetCoefficients(Values[i]);
            double sum = 0;

            for (int j = 0; j < dim; ++j)
                sum += (mean[j] - coeff[j]) * (mean[j] - coeff[j]);

            dist[i] = Math.Sqrt(sum);
        });

        return Values[dist.MinIndex()];
    }
}

public record ClusteringConfiguration<Item>(int InputDimensionality, Func<Item, double[]> GetCoefficients);

/// <summary>
/// Represents an abstract clustering algorithm, which clusters a given dataset based on a specified distance metric.
/// </summary>
/// <completionlist cref="Clustering"/>
public abstract class Clustering<Item>
{
    public ClusteringConfiguration<Item> Configuration { get; }


    public Clustering(ClusteringConfiguration<Item> config) => Configuration = config;

    public IEnumerable<Cluster<Item>> Process(IEnumerable<Item>? collection)
    {
        if ((collection as Item[] ?? collection?.ToArray()) is Item[] array)
        {
            int dim = Configuration.InputDimensionality;
            double[,] data = new double[array.Length, dim];

            Parallel.For(0, array.Length, i =>
            {
                double[] coeff = Configuration.GetCoefficients(array[i]);

                if (coeff.Length != dim)
                    throw new ArgumentException($"The item at index {i} has a dimensionality of {coeff.Length}, which conflicts with the expected dimensionality of {dim}.", nameof(collection));

                for (int j = 0; j < dim; ++j)
                    data[i, j] = coeff[j];
            });

            int[] clustering = AssignCluster(data);

            return from t in clustering.Zip(array)
                   let id = t.First
                   group t.Second by id into @group
                   select new Cluster<Item>(this, @group.Key, [.. @group]);
        }
        else
            return [];
    }

    private protected abstract int[] AssignCluster(double[,] data);
}

// TODO : mean shift algorithm
// TODO : DBSCAN algorithm

public class KMeansClustering<Item>
    : Clustering<Item>
{
    public int K { get; }


    public KMeansClustering(int k, ClusteringConfiguration<Item> config) : base(config) => K = k;

    private protected override int[] AssignCluster(double[,] data)
    {
        int count = data.GetLength(0);
        int dim = data.GetLength(1);
        double[,] normalized = Normalized(data, dim);
        int[] clustering = new int[count];
        double[,] means = new double[K, dim];
        XorShift random = new();

        Parallel.For(0, count, i => clustering[i] = i < K ? i : random.NextInt(0, K));

        bool changed = true;
        bool success = true;
        int max_iterations = count * 10;

        while (changed && success && max_iterations --> 0)
        {
            success = UpdateMeans(normalized, clustering, means, count, dim);
            changed = UpdateClustering(normalized, clustering, means, count, dim);
        }

        return clustering;
    }

    private double[,] Normalized(double[,] rawData, int dim)
    {
        int count = rawData.GetLength(0);
        double[,] result = new double[count, dim];

        Array.Copy(rawData, result, rawData.Length);

        for (int j = 0; j < dim; ++j)
        {
            double colSum = 0;

            for (int i = 0; i < count; ++i)
                colSum += result[i, j];

            double mean = colSum / count;
            double sum = 0;

            for (int i = 0; i < count; ++i)
                sum += (result[i, j] - mean) * (result[i, j] - mean);

            double sd = sum / count;

            for (int i = 0; i < count; ++i)
                result[i, j] = (result[i, j] - mean) / sd;
        }

        return result;
    }

    private bool UpdateMeans(double[,] data, int[] clustering, double[,] means, int count, int dim)
    {
        int[] sizes = new int[K];

        for (int i = 0; i < count; ++i)
            ++sizes[clustering[i]];

        if (sizes.Contains(0))
            return false;

        Parallel.For(0, K * dim, i => means[i / dim, i % dim] = 0);

        for (int i = 0; i < count; ++i)
        {
            int cluster = clustering[i];

            for (int j = 0; j < dim; ++j)
                means[cluster, j] += data[i, j]; // accumulate sum
        }

        Parallel.For(0, K * dim, i => means[i / dim, i % dim] /= sizes[i / dim]); // danger of div by 0

        return true;
    }

    private bool UpdateClustering(double[,] data, int[] clustering, double[,] means, int count, int dim)
    {
        bool changed = false;
        int[] updated = new int[count];
        double[] distances = new double[K];

        Array.Copy(clustering, updated, count);

        for (int i = 0; i < count; ++i)
        {
            Parallel.For(0, K, k =>
            {
                double sum = 0;

                for (int j = 0; j < dim; ++j)
                    sum += (data[i, j] - means[k, j]) * (data[i, j] - means[k, j]);

                distances[k] = Math.Sqrt(sum);
            });

            if (distances.MinIndex() is int @new && @new != updated[i])
            {
                changed = true;
                updated[i] = @new;
            }
        }

        if (changed)
        {
            int[] sizes = new int[K];

            for (int i = 0; i < count; ++i)
                ++sizes[updated[i]];

            if (sizes.Contains(0))
                return false;

            Array.Copy(updated, clustering, count);
        }

        return changed;
    }
}
