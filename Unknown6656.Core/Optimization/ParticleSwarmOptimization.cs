using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Numerics;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Optimization.ParticleSwarmOptimization
{
    public sealed record PSOSolverConfiguration<Domain>(int ParticleCount, int MaxIterationCount, PSOSolverWeightsConfiguration Weights, Domain InitialPosition)
    {
        public ParallelOptions ParallelOptions { get; init; } = new ParallelOptions { MaxDegreeOfParallelism = 128 };
        public Random RandomNumberGenerator { get; init; } = new XorShift();
        public Scalar ResultPrecision { get; init; } = Scalar.ComputationalEpsilon;

        public static PSOSolverConfiguration<Domain> CreateDefault(Domain initial_pos) => new(64, 1000, PSOSolverWeightsConfiguration.Default, initial_pos);
    }

    public sealed record PSOSolverWeightsConfiguration
    {
        public Scalar SwarmPositionAttraction { get; init; }
        public Scalar SwarmVelocityAttraction { get; init; }
        public Scalar SwarmHistoricBestAttraction { get; init; }
        public Scalar ParticleInteria { get; init; }
        public Scalar ParticleHistoricBestAttraction { get; init; }
        public Scalar ParticleInverseDrag { get; init; }
        public Scalar RandomizationInfluence { get; init; }

        public static PSOSolverWeightsConfiguration Default { get; } = new PSOSolverWeightsConfiguration()
        {
            SwarmPositionAttraction = .05,
            SwarmVelocityAttraction = .05,
            SwarmHistoricBestAttraction = .3,
            ParticleInteria = .5,
            ParticleHistoricBestAttraction = .1,
            ParticleInverseDrag = 1,
            RandomizationInfluence = .7,
        };
    }

    public abstract class PSOProblem<Domain, Codomain, Problem>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Domain, Codomain, Problem>
    {
        /// <summary>
        /// Returns whether the given candidate solution is a valid search position.
        /// </summary>
        /// <param name="position">Candidate solution.</param>
        /// <returns>Boolean value.</returns>
        internal protected abstract bool IsValidSearchPosition(Domain position);

        public abstract Domain GetZeroVector();

        public abstract Codomain GetValue(Domain position);

        public PSOSolver<Domain, Codomain, Problem> CreateSolver(PSOSolverConfiguration<Domain> configuration) =>
            new((Problem)this, configuration);
    }

    public sealed class PSOSolver<Domain, Codomain, Problem>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Domain, Codomain, Problem>
    {
        internal readonly Domain _null;

        private readonly object _mutex = new();
        private readonly Domain _init_pos;
        private readonly PSOParticle<Domain, Codomain, Problem>[] _particles;

        public Problem PSOProblem { get; }
        public PSOSolverConfiguration<Domain> Configuration { get; }
        public (Domain Position, Codomain Value)? HistoricBest { get; private set; }

        public ImmutableArray<PSOParticle<Domain, Codomain, Problem>> Particles => _particles.ToImmutableArray();


        internal PSOSolver(Problem problem, PSOSolverConfiguration<Domain> configuration)
        {
            PSOProblem = problem;
            Configuration = configuration;
            _particles = new PSOParticle<Domain, Codomain, Problem>[configuration.ParticleCount];
            _null = problem.GetZeroVector();
            _init_pos = configuration.InitialPosition;
            HistoricBest = null;

            Reset();
        }

        public IEnumerable<PSOParticle<Domain, Codomain, Problem>> GetNearest(PSOParticle<Domain, Codomain, Problem> particle, double max_distance) =>
            from p in _particles
            where p.Solver == this
            let dist = p.Position.DistanceTo(particle.Position)
            where dist <= max_distance
            orderby dist ascending
            select p;

        public void Reset()
        {
            HistoricBest = null;

            Parallel.For(0, _particles.Length, Configuration.ParallelOptions, i =>
            {
                _particles[i] ??= new PSOParticle<Domain, Codomain, Problem>(this, _init_pos);
                _particles[i].Reset();
            });
        }

        public PSOSolution<Domain, Codomain, Problem> Solve()
        {
            Random random = Configuration.RandomNumberGenerator;
            PSOSolverWeightsConfiguration weights = Configuration.Weights;
            Scalar randomized_weight(Scalar weight) => weight * random.NextScalar() * weights.RandomizationInfluence + weight * (1 - weights.RandomizationInfluence);
            int iter = 0;

            lock (_mutex)
                for (int max = Configuration.MaxIterationCount; iter < max; ++iter)
                {
                    Domain global_pos = Configuration.InitialPosition ?? _null;
                    Domain global_vel = _null;
                    Scalar factor = 1d / _particles.Length;

                    foreach (PSOParticle<Domain, Codomain, Problem> particle in _particles)
                    {
                        global_pos = global_pos.Add(particle.Position.Multiply(factor));
                        global_vel = global_vel.Add(particle.Velocity.Multiply(factor));
                    }

                    Parallel.For(0, _particles.Length, Configuration.ParallelOptions, i =>
                    {
                        Domain position, velocity;

                        do
                        {
                            position = _particles[i].Position;
                            velocity = global_pos.Subtract(in position).Multiply(randomized_weight(weights.SwarmPositionAttraction))
                                  .Add(global_vel.Multiply(randomized_weight(weights.SwarmVelocityAttraction)))
                                  .Add(_particles[i].Velocity.Multiply(randomized_weight(weights.ParticleInteria)));

                            if (_particles[i].HistoricBest is { Position: Domain part_best })
                                velocity = velocity.Add(part_best.Multiply(randomized_weight(weights.ParticleHistoricBestAttraction)));

                            if (HistoricBest is { Position: Domain swarm_best })
                                velocity = velocity.Add(swarm_best.Multiply(randomized_weight(weights.SwarmHistoricBestAttraction)));

                            position = position.Add(velocity.Multiply(weights.ParticleInverseDrag));
                        }
                        while (PSOProblem.IsValidSearchPosition(position));

                        Codomain value = _particles[i].UpdateParticle(position, velocity);

                        if (_particles[i].HistoricBest is null || _particles[i].HistoricBest!.Value.Value.CompareTo(value) > 0)
                            _particles[i].HistoricBest = (position, value);
                    });

                    foreach (PSOParticle<Domain, Codomain, Problem> particle in _particles)
                        if (particle.HistoricBest is { } p_best)
                            if (HistoricBest is null || HistoricBest!.Value.Value.CompareTo(p_best.Value) > 0)
                                HistoricBest = particle.HistoricBest;
                }

            Domain? solution = HistoricBest.HasValue ? HistoricBest.Value.Position : Configuration.InitialPosition;

            return new PSOSolution<Domain, Codomain, Problem>(this, solution, HistoricBest.HasValue ? HistoricBest.Value.Value : default, iter);
        }
    }

    public class PSOParticle<Domain, Codomain, Problem>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Domain, Codomain, Problem>
    {
        private readonly List<(Domain Position, Domain Velocity, Codomain Value)> _history = [];

        public PSOSolver<Domain, Codomain, Problem> Solver { get; }
        public Codomain? CachedValue { get; private set; }
        public Domain InitialPosition { get; }
        public Domain Position { get; private set; }
        public Domain Velocity { get; private set; }
        public (Domain Position, Codomain Value)? HistoricBest { get; internal set; } = null;

        public ImmutableArray<(Domain Position, Domain Velocity, Codomain Value)> History => _history.ToImmutableArray();

        public PSOParticle<Domain, Codomain, Problem>? Nearest => Solver.GetNearest(this, double.MaxValue).FirstOrDefault();

        public Scalar DistanceToHistoricBest => Position.DistanceTo(HistoricBest is null ? Solver._null : HistoricBest.Value.Position);

        public Scalar DistanceToSwarmHistoricBest => Position.DistanceTo(HistoricBest is null ? Solver._null : HistoricBest.Value.Position);


        internal PSOParticle(PSOSolver<Domain, Codomain, Problem> solver, Domain init_pos)
        {
            Solver = solver;
            InitialPosition = init_pos;
            Position = init_pos;
            Velocity = Solver._null;
            CachedValue = Solver.PSOProblem.GetValue(Position);
        }

        internal void Reset()
        {
            UpdateParticle(InitialPosition, Solver._null);
            HistoricBest = null;
            _history.Clear();
        }

        internal Codomain UpdateParticle(Domain position, Domain velocity)
        {
            Position = position;
            Velocity = velocity;
            CachedValue = Solver.PSOProblem.GetValue(Position);
            _history.Add((position, velocity, CachedValue));

            return CachedValue;
        }
    }

    public sealed class PSOSolution<Domain, Codomain, Problem>
        where Domain : Algebra<Scalar>.IMetricVectorSpace<Domain>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Domain, Codomain, Problem>
    {
        public PSOSolver<Domain, Codomain, Problem> Solver { get; }
        public Domain OptimalSolution { get; }
        public Codomain? OptimalValue { get; }
        public int IterationCount { get; }
        public ImmutableArray<(PSOParticle<Domain, Codomain, Problem> Particle, ImmutableArray<(Domain Position, Domain Velocity, Codomain Value)> History)> Histories { get; }


        internal PSOSolution(PSOSolver<Domain, Codomain, Problem> solver, Domain solution, Codomain? value, int iterations)
        {
            Solver = solver;
            OptimalValue = value;
            OptimalSolution = solution;
            IterationCount = iterations;
            Histories = solver.Particles.Select(p => (p, p.History)).ToImmutableArray();
        }
    }
}
