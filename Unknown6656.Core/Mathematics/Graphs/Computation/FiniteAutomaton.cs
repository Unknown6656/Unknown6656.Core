using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Graphs.Computation;


public class DeterministicFiniteAutomaton<S, T>
{
    private readonly HashSet<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>> _accepted;
    private readonly DirectedGraph<S, IEnumerable<T>?> _graph;


    public Indexer<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>, bool> Accepted { get; }
    public Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> Start { get; }


    public DeterministicFiniteAutomaton(DirectedGraph<S, IEnumerable<T>?> graph, Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> start)
    {
        _accepted = [];
        _graph = graph;

        Start = _graph.HasVertex(start) ? start : throw new ArgumentException("The start vertex is not part of the underlying graph.", nameof(start));
        Accepted = new Indexer<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>, bool>(
            _accepted.Contains,
            (v, a) =>
            {
                if (!_graph.HasVertex(v))
                    throw new ArgumentException("The given vertex is not part of the underlying graph.", "vertex");

                if (a && !_accepted.Contains(v))
                    _accepted.Add(v);
                else if (!a && _accepted.Contains(v))
                    _accepted.Remove(v);
            }
        );
    }

    public DeterministicFiniteAutomaton<S, T> Minimize()
    {
        var dic = new Dictionary<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>, Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>>();
        var copy = new DirectedGraph<S, IEnumerable<T>?>();
        bool changes = true;

        foreach (var v in _graph.Vertices)
            dic[v] = copy.AddVertex(v.Data);

        foreach (var e in _graph.Edges)
            copy.AddEdge(dic[e.From], dic[e.To]).Data = e.Data;

        var accepted = _accepted.Select(a => dic[a]);
        var start = dic[Start];

        while (changes)
        {
            changes = copy.RemoveVerticesWhere(v => (v.OutDegree == 0 && !accepted.Contains(v))
                                                 || (v.InDegree == 0 && !start.Equals(v))) > 0;

            foreach (var e in copy.Edges)
                if (e.Data is null)
                {
                    if (!e.IsLoop)
                        foreach (var end in e.To.OutboundEdges.ToArray())
                            copy.AddEdge(e.From, end.To).Data = end.Data;

                    e.Remove();

                    changes = true;
                }
        }

        var dfa = new DeterministicFiniteAutomaton<S, T>(copy, start);

        foreach (var a in accepted)
            dfa.Accepted[a] = true;

        return dfa;
    }

    public DirectedGraph<S, IEnumerable<T>?> GetUnderlyingGraph() => _graph.MemberwiseClone<DirectedGraph<S, IEnumerable<T>?>>();

    public string GenerateRegularExpression(Func<T, string> printer)
    {
        string gen_for(Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> vertex)
        {
            var edges = vertex.OutboundEdges.ToArray();
            List<string> branches = [];
            string res;

            foreach (var e in edges)
            {
                var d = e.Data?.ToArray(printer) ?? [];
                var r_e = d.Length == 1 ? Regex.Escape(d[0])
                                        : d.All(s => s.Length == 1) ? $"[{string.Concat(d.Select(Regex.Escape))}]"
                                                                    : $"({string.Join("|", d.Select(Regex.Escape))})";

                if (r_e.Length > 0 && e.To.Equals(vertex))
                    r_e += "*";
                else
                    r_e += gen_for(e.To);

                branches.Add(r_e);
            }

            if (branches.Count == 1)
                res = branches[0];
            else if (branches.All(s => s.Length == 1))
                res = $"[{string.Concat(branches)}]";
            else
                res = $"({string.Join("|", branches)})";

            return res.Replace("()", "")
                      .Replace("[]", "");
        }

        return gen_for(Start);
    }

    public IEnumerable<T[]> GenerateWords()
    {
        IEnumerable<IEnumerable<IEnumerable<T>>> gen_paths(
            Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> from,
            IEnumerable<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>> to
        )
        {
            foreach (Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> t in to)
                if (_graph.TryFindShortestPath(from, t, v => 1, out Path<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>? path))
                    yield return from e in path!.Edges
                                 where e.Data is { }
                                 select e.Data;
        }

        IEnumerable<IEnumerable<IEnumerable<T>>> paths = from v in _graph.Vertices
                                                         where !v.Equals(Start)
                                                         where !_accepted.Contains(v)
                                                         let p = gen_paths(Start, new[] { v }).FirstOrDefault()
                                                         where p is { }
                                                         from s in gen_paths(v, _accepted)
                                                         select p.Concat(s);

        return paths.Concat(gen_paths(Start, _accepted))
                    .SelectMany(LINQ.CartesianProduct)
                    .SequentialDistinct()
                    .Select(Enumerable.ToArray);
    }

    public AutomatonResult Parse(IEnumerable<T> input) => Parse(input, out _);

    public AutomatonResult Parse(IEnumerable<T> input, out Path<DirectedGraph<S, IEnumerable<T>>, S, IEnumerable<T>>? states)
    {
        states = null;

        if (_accepted.Count == 0)
            return AutomatonResult.Reject;

        List<Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?>> path = [Start];
        Vertex<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> last = Start;

        foreach (T i in input)
        {
            foreach (Edge<DirectedGraph<S, IEnumerable<T>?>, S, IEnumerable<T>?> e in last.OutboundEdges)
                if (e.Data?.Contains(i) ?? true)
                {
                    path.Add(last = e.To);

                    goto next;
                }

            return AutomatonResult.Reject;
next:;
        }

        return Accepted[last] ? AutomatonResult.Accept : AutomatonResult.Reject;
    }
}

public sealed class ParserBuilder<T>
{
    private readonly DirectedGraph<string, IEnumerable<T>?> _graph;
    private readonly HashSet<Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?>> _accepted;
    private readonly Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?> _start;
    private readonly T[] _alphabet;


    public ParserBuilder(IEnumerable<T> alphabet)
    {
        _accepted = [];
        _graph = [];
        _alphabet = alphabet?.Distinct()?.ToArray() ?? [];
        _start = _graph.AddVertex("[start]");
    }

    public BuilderState Start() => new(this, _start);


    public sealed class BuilderState
    {
        private readonly Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?> _current;


        internal DirectedGraph<string, IEnumerable<T>?> Graph => Builder._graph;

        public ParserBuilder<T> Builder { get; }


        internal BuilderState(ParserBuilder<T> pb, Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?> vertex) =>
            (Builder, _current) = (pb, vertex);

        public BuilderState ExactlyOne(params T[] input)
        {
            Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?> v = Graph.AddVertex($"exactly one of '{string.Join("', '", input.Select(o => o?.ToString() ?? "[null]"))}'");

            return GoTo(v, input);
        }

        private BuilderState GoTo(Vertex<DirectedGraph<string, IEnumerable<T>?>, string, IEnumerable<T>?> vertex, IEnumerable<T>? data = null)
        {
            Graph.AddEdge(_current, vertex).Data = data;

            return new BuilderState(Builder, vertex);
        }

        public BuilderState ZeroOrMore(params T[] input) => AtLeast(0, input);

        public BuilderState OneOrMore(params T[] input) => AtLeast(1, input);

        public BuilderState Exactly(uint count, params T[] input)
        {
            BuilderState bs = this;

            while (count > 0)
            {
                bs = bs.ExactlyOne(input);
                --count;
            }

            return new BuilderState(Builder, bs._current);
        }

        public BuilderState AtLeast(uint count, params T[] input)
        {
            BuilderState bs = Exactly(count, input);

            return bs.GoTo(bs._current, input);
        }

        public BuilderState Range(uint min, uint max, params T[] input)
        {
            if (max < min)
                throw new ArgumentException("The maximum input character count must be greater or equal to the minimum character count.", nameof(max));

            BuilderState bs = min > 0 ? Exactly(min, input) : this;

            if (max - min is uint cnt && cnt > 0)
            {
                BuilderState[] intermediate = new BuilderState[cnt];
                var vertex = Graph.AddVertex($"0..{cnt} of '{string.Join("', '", input.Select(o => o?.ToString() ?? "[null]"))}'");

                intermediate[0] = bs;

                for (int i = 1; i < cnt; ++i)
                    intermediate[i] = intermediate[i - 1].ExactlyOne(input);

                for (int i = 0; i < cnt - 1; ++i)
                    Graph.AddEdge(intermediate[i]._current, vertex).Data = Builder._alphabet.Except(input);

                Graph.AddEdge(intermediate[intermediate.Length - 1]._current, vertex).Data = Builder._alphabet;

                return new BuilderState(Builder, vertex);
            }
            else
                return bs;
        }

        public BuilderState Not(params T[] input) => ExactlyOne(Builder._alphabet.Except(input).ToArray());

        public BuilderState Accept()
        {
            Builder._accepted.Add(_current);

            return this;
        }

        public BuilderState InvertAll()
        {
            var acc = Builder._accepted.ToArray();

            foreach (var v in Graph.Vertices)
                Builder._accepted.Add(v);

            foreach (var v in acc)
                Builder._accepted.Remove(v);

            return this;
        }

        public BuilderState Split(IDictionary<IEnumerable<T>, Func<BuilderState, BuilderState>> branches)
        {
            var vertex = Graph.AddVertex("common split end");

            foreach (var input in branches.Keys)
                if (input.Any())
                    branches[input](ExactlyOne(input.ToArray())).GoTo(vertex, null);

            return new BuilderState(Builder, vertex);
        }

        public BuilderState LoopOn(IEnumerable<T> input, Func<BuilderState, BuilderState> body)
        {
            var vertex = Graph.AddVertex("common loop end");

            return Split(new Dictionary<IEnumerable<T>, Func<BuilderState, BuilderState>>
            {
                [input] = bs => body(bs).GoTo(vertex, null),
                [Builder._alphabet.Except(input)] = bs => bs.GoTo(vertex, null),
            });
        }

        public BuilderState DontLoopOn(IEnumerable<T> input, Func<BuilderState, BuilderState> body) => LoopOn(Builder._alphabet.Except(input), body);

        public DeterministicFiniteAutomaton<string, T> GenerateParser()
        {
            DeterministicFiniteAutomaton<string, T> dfa = new(Graph, Builder._start);

            foreach (var v in Builder._accepted)
                dfa.Accepted[v] = true;

            return dfa.Minimize();
        }

        // TODO:
        //
        // public builder_state ExactlyOne(params T[] input) => ;
        // public builder_state AtLeast(uint count, params T[] input) => ;
        // public builder_state Invert();
        // public builder_state Accept();
        //
        // same for loops somehow
    }
}

public enum AutomatonResult
{
    Accept,
    Reject
}
