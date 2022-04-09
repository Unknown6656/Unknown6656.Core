using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Generics;
using System.Threading.Tasks;

namespace Unknown6656.Mathematics.Statistics;

using _data = Vector3;


/// <summary>
/// Represents an abstract clustering algorithm, which clusters a given dataset based on a specified distance metric.
/// </summary>
/// <completionlist cref="Clustering"/>
public abstract class Clustering<Scalar, Item>
    where Scalar : unmanaged, IScalar<Scalar>
    where Item : Algebra<Scalar>.IMetricVectorSpace<Item>
{
    public IEnumerable<Cluster> Process(IEnumerable<Item> collection) =>
         Process(collection, (x, y) => x?.DistanceTo(in y) ?? y?.DistanceTo(in x) ?? Scalar.NaN);

    public IEnumerable<Cluster> Process(IEnumerable<Item> collection, Func<Item, Item, Scalar> distance_metric)
    {
        var data = collection.ToArray();
        var clustering = process(data);

        throw new NotImplementedException();
        //return from t in clustering.Zip(data)
        //       let cluster = t.First
        //       let item = t.Second
        //       group item by cluster into groups
        //       select new Cluster(..., groups);
    }

    private protected int[] process(Item[] data)
    {
        throw new NotImplementedException();
    }


    public record Cluster(Item Key, IEnumerable<(Item Value, Scalar Distance)> Values)
        : IGrouping<Item, (Item Value, Scalar Distance)>
    {
        public Scalar MeanDistance { get; } = Values.Select(v => v.Distance).Average();


        public IEnumerator<(Item Value, Scalar Distance)> GetEnumerator() => Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
    }
}

public class KMeansClustering<Scalar, Item>
    : Clustering<Scalar, Item>
    where Scalar : unmanaged, IScalar<Scalar>
    where Item : Algebra<Scalar>.IMetricVectorSpace<Item>
{
    IEnumerable<Cluster> ____Cluster(IEnumerable<Item> collection, Func<Item, Item, Scalar> distance_metric)
    {
        //var lol = Item.Dimension;



        throw new NotImplementedException();
    }








    private static int[] Cluster(double[,] data, int K)
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
            success = UpdateMeans(normalized, clustering, means, count, dim, K);
            changed = UpdateClustering(normalized, clustering, means, count, dim, K);
        }

        return clustering;
    }

    private static double[,] Normalized(double[,] rawData, int dim)
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

    private static bool UpdateMeans(double[,] data, int[] clustering, double[,] means, int count, int dim, int K)
    {
        int[] sizes = new int[K];

        for (int i = 0; i < count; ++i)
            ++sizes[clustering[i]];

        if (sizes.Contains(0))
            return false;

        for (int k = 0; k < K * dim; ++k)
            means[k / dim, k % dim] = 0;

        for (int i = 0; i < count; ++i)
        {
            int cluster = clustering[i];

            for (int j = 0; j < dim; ++j)
                means[cluster, j] += data[i, j]; // accumulate sum
        }

        for (int k = 0; k < K; ++k)
            for (int j = 0; j < dim; ++j)
                means[k, j] /= sizes[k]; // danger of div by 0

        return true;
    }

    private static bool UpdateClustering(double[,] data, int[] clustering, double[,] means, int count, int dim, int K)
    {
        bool changed = false;
        int[] updated = new int[count];
        double[] distances = new double[K];

        Array.Copy(clustering, updated, count);

        for (int i = 0; i < count; ++i)
        {
            for (int k = 0; k < K; ++k)
                distances[k] = Distance(data[i], means[k]);

            if (distances.MinIndex() is int @new && @new != updated[i])
            {
                changed = true;
                updated[i] = @new;
            }
        }

        if (changed)
        {
            int[] sizes = new int[K];

            for (int i = 0; i < data.Length; ++i)
                ++sizes[updated[i]];

            if (sizes.Contains(0))
                return false;

            Array.Copy(updated, clustering, updated.Length);
        }

        return changed;
    }

}
