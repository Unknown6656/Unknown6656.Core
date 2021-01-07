using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.IO;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Computation.ParticleSwarmOptimization
{
    public abstract class PSOProblem<T>
        where T : IComparable<T>
    {
        public abstract int Dimensionality { get; }

        public abstract bool IsValidSearchPosition(VectorN position);
        public abstract T GetValue(VectorN position);


        public PSOSolver<T> CreateSolver(PSOSolverConfiguration configuration) => new PSOSolver<T>(this, configuration);
    }

    public sealed record PSOSolverConfiguration
    {
        public int ParticleCount { get; init; } = 64;
        public int MaxIterationCount { get; init; } = 1000;
        public ParallelOptions ParallelOptions { get; init; } = new ParallelOptions { MaxDegreeOfParallelism = 128 };
        public PSOSolverWeightsConfiguration Weights { get; init; } = new();
        public VectorN? InitialPosition { get; init; }
        public Random RandomNumberGenerator { get; init; } = new XorShift();
    }

    public sealed record PSOSolverWeightsConfiguration
    {
        public Scalar SwarmPositionAttraction { get; init; } = .05;
        public Scalar SwarmVelocityAttraction { get; init; } = .05;
        public Scalar SwarmHistoricBestAttraction { get; init; } = .3;
        public Scalar ParticleInteria { get; init; } = .5;
        public Scalar ParticleHistoricBestAttraction { get; init; } = .1;
        public Scalar ParticleInverseDrag { get; init; } = 1;
        public Scalar RandomizationInfluence { get; init; } = .7;
    }

    public sealed class PSOSolution<T>
        where T : IComparable<T>
    {
        public PSOSolver<T> Solver { get; }
        public VectorN OptimalSolution { get; }
        public T? OptimalValue { get; }
        public int IterationCount { get; }
        public ImmutableArray<(PSOParticle<T> Particle, ImmutableArray<(VectorN Position, VectorN Velocity, T Value)> History)> Histories { get; }


        internal PSOSolution(PSOSolver<T> solver, VectorN solution, T? value, int iterations)
        {
            Solver = solver;
            OptimalValue = value;
            OptimalSolution = solution;
            IterationCount = iterations;
            Histories = solver.Particles.Select(p => (p, p.History)).ToImmutableArray();
        }
    }

    public sealed class PSOSolver<T>
        where T : IComparable<T>
    {
        internal readonly VectorN _null;

        private readonly object _mutex = new();
        private readonly VectorN _init_pos;
        private readonly PSOParticle<T>[] _particles;

        public PSOProblem<T> Problem { get; }
        public PSOSolverConfiguration Configuration { get; }
        public (VectorN Position, T Value)? HistoricBest { get; private set; }
        public ImmutableArray<PSOParticle<T>> Particles => _particles.ToImmutableArray();


        internal PSOSolver(PSOProblem<T> problem, PSOSolverConfiguration configuration)
        {
            Problem = problem;
            Configuration = configuration;
            _particles = new PSOParticle<T>[configuration.ParticleCount];
            _null = VectorN.ZeroVector(problem.Dimensionality);
            _init_pos = configuration.InitialPosition ?? _null;
            HistoricBest = null;

            Reset();
        }

        public IEnumerable<PSOParticle<T>> GetNearest(PSOParticle<T> particle, double max_distance) => from p in _particles
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
                _particles[i] ??= new PSOParticle<T>(this, _init_pos);
                _particles[i].Reset();
            });
        }

        public PSOSolution<T> Solve()
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

                    foreach (PSOParticle<T> particle in _particles)
                    {
                        global_pos += particle.Position * factor;
                        global_vel += particle.Velocity * factor;
                    }

                    Parallel.For(0, _particles.Length, Configuration.ParallelOptions, i =>
                    {
                        VectorN position = _particles[i].Position;
                        VectorN velocity = randomized_weight(weights.SwarmPositionAttraction) * (global_pos - position)
                                         + randomized_weight(weights.SwarmVelocityAttraction) * global_vel
                                         + randomized_weight(weights.ParticleInteria) * _particles[i].Velocity;

                        if (_particles[i].HistoricBest?.Position is VectorN part_best)
                            velocity += randomized_weight(weights.SwarmHistoricBestAttraction) * part_best;

                        if (HistoricBest?.Position is VectorN swarm_best)
                            velocity += randomized_weight(weights.SwarmHistoricBestAttraction) * swarm_best;

                        position += weights.ParticleInverseDrag * velocity;

                        T value = _particles[i].UpdateParticle(position, velocity);

                        if (_particles[i].HistoricBest is null || _particles[i].HistoricBest!.Value.Value.CompareTo(value) > 0)
                            _particles[i].HistoricBest = (position, value);
                    });

                    foreach (PSOParticle<T> particle in _particles)
                        if (particle.HistoricBest is { } p_best)
                            if (HistoricBest is null || HistoricBest!.Value.Value.CompareTo(p_best.Value) > 0)
                                HistoricBest = particle.HistoricBest;
                }
            }

            VectorN solution = HistoricBest?.Position ?? Configuration.InitialPosition ?? _null;

            return new PSOSolution<T>(this, solution, HistoricBest.HasValue ? HistoricBest.Value.Value : default, iter);
        }
    }

    public class PSOParticle<T>
        where T : IComparable<T>
    {
        private readonly List<(VectorN Position, VectorN Velocity, T Value)> _history = new();

        public PSOSolver<T> Solver { get; }
        public T? CachedValue { get; private set; }
        public VectorN InitialPosition { get; }
        public VectorN Position { get; private set; }
        public VectorN Velocity { get; private set; }
        public (VectorN Position, T Value)? HistoricBest { get; internal set; } = null;
        public ImmutableArray<(VectorN Position, VectorN Velocity, T Value)> History => _history.ToImmutableArray();

        public PSOParticle<T>? Nearest => Solver.GetNearest(this, double.MaxValue).FirstOrDefault();
        public Scalar DistanceToHistoricBest => Position.DistanceTo(HistoricBest?.Position ?? Solver._null);
        public Scalar DistanceToSwarmHistoricBest => Position.DistanceTo(Solver.HistoricBest?.Position ?? Solver._null);


        internal PSOParticle(PSOSolver<T> solver, VectorN init_pos)
        {
            Solver = solver;
            InitialPosition = init_pos;
            Position = init_pos;
            Velocity = VectorN.ZeroVector(Solver.Problem.Dimensionality);
            CachedValue = Solver.Problem.GetValue(Position);
        }

        internal void Reset()
        {
            UpdateParticle(InitialPosition, VectorN.ZeroVector(Solver.Problem.Dimensionality));
            HistoricBest = null;
            _history.Clear();
        }

        internal T UpdateParticle(VectorN position, VectorN velocity)
        {
            Position = position;
            Velocity = velocity;
            CachedValue = Solver.Problem.GetValue(Position);
            _history.Add((position, velocity, CachedValue));

            return CachedValue;
        }
    }
}
