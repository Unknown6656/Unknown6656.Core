using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Optimization.ParticleSwarmOptimization
{
    // TODO : maximum / minimum finder
    // TODO : complex solver
    // TODO : matrixNM solver



    public class YValueFinder<Func, Domain, Codomain>
        : PSOProblem<Domain, Scalar, YValueFinder<Func, Domain, Codomain>>
        where Func : Function<Func, Domain, Codomain>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : Algebra<Scalar>.IMetricVectorSpace<Codomain>, IComparable<Codomain>
    {
        public Func Function { get; }
        public Codomain YValue { get; }


        public YValueFinder(Func function, Codomain y)
        {
            Function = function;
            YValue = y;
        }

        public override Scalar GetValue(Domain x) => Function.Evaluate(x).DistanceTo(YValue).Abs();

        internal protected override bool IsValidSearchPosition(Domain position)
        {
            try
            {
                _ = Function.Evaluate(position);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override Domain GetZeroVector() => IGroup<Domain>.ZeroElement!;
    }

    public class YValueFinder<Domain, Codomain>
        : YValueFinder<Function<Domain, Codomain>, Domain, Codomain>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : Algebra<Scalar>.IMetricVectorSpace<Codomain>, IComparable<Codomain>
    {
        public YValueFinder(Function<Domain, Codomain> function, Codomain y)
            : base(function, y)
        {
        }
    }

    public class YValueFinder<Domain>
        : YValueFinder<Domain, Domain>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>, IComparable<Domain>
    {
        public YValueFinder(Function<Domain, Domain> function, Domain y)
            : base(function, y)
        {
        }
    }
}
