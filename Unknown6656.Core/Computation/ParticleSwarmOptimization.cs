using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Unknown6656.IO;
using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Computation.ParticleSwarmOptimization
{
    public abstract class Problem<T>
        where T : IComparable<T>
    {
        public abstract int Dimensionality { get; }

        public abstract bool IsValidSearchPosition(VectorN position);
        public abstract T GetValue(VectorN position);


        public Solver<T> InitSolver(OptimizerConfiguration configuration) => throw new NotImplementedException();
    }

    public class OptimizerConfiguration
    {
        public int ParticleCount { get; init; } = 64;
        public int MaxIterationCount { get; init; } = 1000;
        public ParallelOptions ParallelOptions { get; init; } = new ParallelOptions { MaxDegreeOfParallelism = 128 };
        public VectorN? InitialPosition { get; init; }
    }

    public sealed class Solver<T>
        where T : IComparable<T>
    {
        private readonly VectorN _init_pos;
        private readonly Particle<T>[] _particles;
        private (VectorN pos, T val) _global_best;

        public Problem<T> Problem { get; }
        public OptimizerConfiguration Configuration { get; }



        private Solver(Problem<T> problem, OptimizerConfiguration configuration)
        {
            Problem = problem;
            Configuration = configuration;
            _particles = new Particle<T>[configuration.ParticleCount];
            _init_pos = configuration.InitialPosition ?? VectorN.ZeroVector(problem.Dimensionality);

            Reset();
        }

        public IEnumerable<Particle<T>> GetNearest(Particle<T> particle, double max_distance) => from p in _particles
                                                                                                 where p.Solver == this
                                                                                                 let dist = p.Position.DistanceTo(particle.Position)
                                                                                                 where dist <= max_distance
                                                                                                 orderby dist ascending
                                                                                                 select p;

        public void Reset()
        {
            Parallel.For(0, _particles.Length, Configuration.ParallelOptions, i => _particles[i].Reset(_init_pos));

            _global_best = (_init_pos, default);
        }

        public VectorN Solve()
        {
            lock (this)
            {

                // TODO

                throw new NotImplementedException();
            }
        }
    }

    public class Particle<T>
        where T : IComparable<T>
    {
        public Solver<T> Solver { get; }
        public (VectorN Position, T Value) HistoricBest { get; }
        public VectorN Position { get; private set; }
        public VectorN Velocity { get; private set; }

        public Particle<T>? Nearest => Solver.GetNearest(this, double.MaxValue).FirstOrDefault();


        internal Particle(Solver<T> solver, VectorN init_pos)
        {
            Solver = solver;

            Reset(init_pos);
        }

        public void Reset(VectorN position)
        {
            Position = position;
            Velocity = VectorN.ZeroVector(Solver.Problem.Dimensionality);
        }
    }
}
