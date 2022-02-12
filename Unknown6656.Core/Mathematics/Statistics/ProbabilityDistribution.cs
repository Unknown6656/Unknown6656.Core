namespace Unknown6656.Mathematics.Statistics
{
    // TODO

    public abstract class ProbabilityDistribution<T>
        where T : ProbabilityDistribution<T>
    {
    }

    public class GaussianDistribution
        : ProbabilityDistribution<GaussianDistribution>
    {
    }

    public class PoissonDistribution
        : ProbabilityDistribution<GaussianDistribution>
    {
    }
}
