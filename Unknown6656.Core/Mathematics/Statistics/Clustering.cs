using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Statistics;


public record Cluster<Scalar, Item>(Item Key, IEnumerable<(Item Value, Scalar Distance)> Values)
    : IGrouping<Item, (Item Value, Scalar Distance)>
    where Item : Algebra<Scalar>.IMetricVectorSpace<Item>
    where Scalar : unmanaged, IScalar<Scalar>
{
    public Scalar MeanDistance { get; } = Values.Select(v => v.Distance).Average();


    public IEnumerator<(Item Value, Scalar Distance)> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
}

public abstract class Clustering
{
    public IEnumerable<Cluster<Scalar, Item>> Cluster<Scalar, Item>(IEnumerable<Item> collection)
        where Scalar : unmanaged, IScalar<Scalar>
        where Item : Algebra<Scalar>.IMetricVectorSpace<Item> =>
         Cluster<Scalar, Item>(collection, (x, y) => x?.DistanceTo(y) ?? y?.DistanceTo(x) ?? Scalar.NaN);

    public abstract IEnumerable<Cluster<Scalar, Item>> Cluster<Scalar, Item>(IEnumerable<Item> collection, Func<Item, Item, Scalar> distance_metric)
        where Scalar : unmanaged, IScalar<Scalar>
        where Item : Algebra<Scalar>.IMetricVectorSpace<Item>;
}

public class KMeansClustering
    : Clustering
{
    public override IEnumerable<Cluster<Scalar, Item>> Cluster<Scalar, Item>(IEnumerable<Item> collection, Func<Item, Item, Scalar> distance_metric)
    {


        throw new NotImplementedException();
    }
}
