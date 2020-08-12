using System.Collections.Generic;
using System.Text;
using System;


namespace Unknown6656.Mathematics.Graphs.Computation
{
    public delegate TuringAction DeterministicTouringDelegate<S, I, O>(S old_state, I input, out S new_state, out O output);

    public class DeterministicTuringMachine<S, I, O>
    {
        public readonly DirectedGraph<S, DeterministicTouringDelegate<S, I, O>> _dg;


        public DeterministicTuringMachine(DirectedGraph<S, DeterministicTouringDelegate<S, I, O>> graph) => _dg = graph;

        // TODO
    }

    public enum TuringAction
    {
        Halt,
        Left,
        Right,
    }
}
