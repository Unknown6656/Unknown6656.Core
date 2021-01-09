using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Numerics;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Computation.ParticleSwarmOptimization
{
    public abstract class PSOProblem<Codomain, Problem>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Codomain, Problem>
    {
        public abstract int Dimensionality { get; }

        /// <summary>
        /// Returns whether the given candidate solution is a valid search position.
        /// </summary>
        /// <param name="position">Candidate solution.</param>
        /// <returns>Boolean value.</returns>
        internal protected abstract bool IsValidSearchPosition(VectorN position);
        public abstract Codomain GetValue(VectorN position);

        public PSOSolver<Codomain, Problem> CreateSolver(PSOSolverConfiguration configuration) => new PSOSolver<Codomain, Problem>((Problem)this, configuration);
    }

    public sealed record PSOSolverConfiguration(int ParticleCount, int MaxIterationCount, PSOSolverWeightsConfiguration Weights, VectorN? InitialPosition = null)
    {
        public ParallelOptions ParallelOptions { get; init; } = new ParallelOptions { MaxDegreeOfParallelism = 128 };
        public Random RandomNumberGenerator { get; init; } = new XorShift();

        public static PSOSolverConfiguration Default { get; } = new(64, 1000, PSOSolverWeightsConfiguration.Default);
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

    public sealed class PSOSolver<Codomain, Problem>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Codomain, Problem>
    {
        internal readonly VectorN _null;

        private readonly object _mutex = new();
        private readonly VectorN _init_pos;
        private readonly PSOParticle<Codomain, Problem>[] _particles;

        public Problem PSOProblem { get; }
        public PSOSolverConfiguration Configuration { get; }
        public (VectorN Position, Codomain Value)? HistoricBest { get; private set; }
        public ImmutableArray<PSOParticle<Codomain, Problem>> Particles => _particles.ToImmutableArray();


        internal PSOSolver(Problem problem, PSOSolverConfiguration configuration)
        {
            PSOProblem = problem;
            Configuration = configuration;
            _particles = new PSOParticle<Codomain, Problem>[configuration.ParticleCount];
            _null = VectorN.ZeroVector(problem.Dimensionality);
            _init_pos = configuration.InitialPosition ?? _null;
            HistoricBest = null;

            Reset();
        }

        public IEnumerable<PSOParticle<Codomain, Problem>> GetNearest(PSOParticle<Codomain, Problem> particle, double max_distance) =>
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
                _particles[i] ??= new PSOParticle<Codomain, Problem>(this, _init_pos);
                _particles[i].Reset();
            });
        }

        public PSOSolution<Codomain, Problem> Solve()
        {
            Random random = Configuration.RandomNumberGenerator;
            PSOSolverWeightsConfiguration weights = Configuration.Weights;
            Scalar randomized_weight(Scalar weight) => weight * random.NextScalar() * weights.RandomizationInfluence + weight * (1 - weights.RandomizationInfluence);
            int iter = 0;

            lock (_mutex)
            {
                for (int max = Configuration.MaxIterationCount; iter < max; ++iter)
                {
                    VectorN global_pos = Configuration.InitialPosition ?? _null;
                    VectorN global_vel = _null;
                    Scalar factor = 1d / _particles.Length;

                    foreach (PSOParticle<Codomain, Problem> particle in _particles)
                    {
                        global_pos += particle.Position * factor;
                        global_vel += particle.Velocity * factor;
                    }

                    Parallel.For(0, _particles.Length, Configuration.ParallelOptions, (Action<int>)(i =>
                    {
                        VectorN position, velocity;

                        do
                        {
                            position = _particles[i].Position;
                            velocity = randomized_weight(weights.SwarmPositionAttraction) * (global_pos - position)
                                     + randomized_weight(weights.SwarmVelocityAttraction) * global_vel
                                     + randomized_weight(weights.ParticleInteria) * _particles[i].Velocity;

                            if (_particles[i].HistoricBest?.Position is VectorN part_best)
                                velocity += randomized_weight(weights.SwarmHistoricBestAttraction) * part_best;

                            if (HistoricBest?.Position is VectorN swarm_best)
                                velocity += randomized_weight(weights.SwarmHistoricBestAttraction) * swarm_best;

                            position += weights.ParticleInverseDrag * velocity;
                        }
                        while (PSOProblem.IsValidSearchPosition(position));

                        Codomain value = _particles[i].UpdateParticle(position, velocity);

                        if (_particles[i].HistoricBest is null || _particles[(int)i].HistoricBest!.Value.Value.CompareTo(value) > 0)
                            _particles[i].HistoricBest = (position, value);
                    }));

                    foreach (PSOParticle<Codomain, Problem> particle in _particles)
                        if (particle.HistoricBest is { } p_best)
                            if (HistoricBest is null || HistoricBest!.Value.Value.CompareTo(p_best.Value) > 0)
                                HistoricBest = particle.HistoricBest;
                }
            }

            VectorN solution = HistoricBest?.Position ?? Configuration.InitialPosition ?? _null;

            return new PSOSolution<Codomain, Problem>(this, solution, HistoricBest.HasValue ? HistoricBest.Value.Value : default, iter);
        }
    }

    public class PSOParticle<Codomain, Problem>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Codomain, Problem>
    {
        private readonly List<(VectorN Position, VectorN Velocity, Codomain Value)> _history = new();

        public PSOSolver<Codomain, Problem> Solver { get; }
        public Codomain? CachedValue { get; private set; }
        public VectorN InitialPosition { get; }
        public VectorN Position { get; private set; }
        public VectorN Velocity { get; private set; }
        public (VectorN Position, Codomain Value)? HistoricBest { get; internal set; } = null;
        public ImmutableArray<(VectorN Position, VectorN Velocity, Codomain Value)> History => _history.ToImmutableArray();

        public PSOParticle<Codomain, Problem>? Nearest => Solver.GetNearest(this, double.MaxValue).FirstOrDefault();
        public Scalar DistanceToHistoricBest => Position.DistanceTo(HistoricBest?.Position ?? Solver._null);
        public Scalar DistanceToSwarmHistoricBest => Position.DistanceTo(Solver.HistoricBest?.Position ?? Solver._null);


        internal PSOParticle(PSOSolver<Codomain, Problem> solver, VectorN init_pos)
        {
            Solver = solver;
            InitialPosition = init_pos;
            Position = init_pos;
            Velocity = VectorN.ZeroVector(Solver.PSOProblem.Dimensionality);
            CachedValue = Solver.PSOProblem.GetValue(Position);
        }

        internal void Reset()
        {
            UpdateParticle(InitialPosition, VectorN.ZeroVector(Solver.PSOProblem.Dimensionality));
            HistoricBest = null;
            _history.Clear();
        }

        internal Codomain UpdateParticle(VectorN position, VectorN velocity)
        {
            Position = position;
            Velocity = velocity;
            CachedValue = Solver.PSOProblem.GetValue(Position);
            _history.Add((position, velocity, CachedValue));

            return CachedValue;
        }
    }

    public sealed class PSOSolution<Codomain, Problem>
        where Codomain : IComparable<Codomain>
        where Problem : PSOProblem<Codomain, Problem>
    {
        public PSOSolver<Codomain, Problem> Solver { get; }
        public VectorN OptimalSolution { get; }
        public Codomain? OptimalValue { get; }
        public int IterationCount { get; }
        public ImmutableArray<(PSOParticle<Codomain, Problem> Particle, ImmutableArray<(VectorN Position, VectorN Velocity, Codomain Value)> History)> Histories { get; }


        internal PSOSolution(PSOSolver<Codomain, Problem> solver, VectorN solution, Codomain? value, int iterations)
        {
            Solver = solver;
            OptimalValue = value;
            OptimalSolution = solution;
            IterationCount = iterations;
            Histories = solver.Particles.Select(p => (p, p.History)).ToImmutableArray();
        }
    }
}
