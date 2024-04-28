// #define ID_API
#define DEBUG

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Graphs;


public delegate void VertexDataChangedEvent<G, V, E>(Vertex<G, V, E> vertex, V data) where G : Graph<G, V, E>;
public delegate void EdgeDataChangedEvent<G, V, E>(Edge<G, V, E> vertex, E data) where G : Graph<G, V, E>;
public delegate void GraphVertexEvent<G, V, E>(G graph, Vertex<G, V, E> vertex) where G : Graph<G, V, E>;
public delegate void GraphEdgeEvent<G, V, E>(G graph, Edge<G, V, E> edge) where G : Graph<G, V, E>;


public class Vertex<G, V, E>
    where G : Graph<G, V, E>
{
    private V _data;

    public event VertexDataChangedEvent<G, V, E> OnDataChanged;

#if ID_API
    public
#else
    internal
#endif
    int ID { get; }
    public V Data
    {
        set
        {
            _data = value;

            OnDataChanged?.Invoke(this, value);
        }
        get => _data;
    }
    public G Graph { get; }
    public IEnumerable<Edge<G, V, E>> Edges => Graph[this];
    public IEnumerable<Edge<G, V, E>> InboundEdges => Edges.Where(e => Equals(e.To));
    public IEnumerable<Edge<G, V, E>> OutboundEdges => Edges.Where(e => Equals(e.From));
    public int InDegree => InboundEdges.Count();
    public int OutDegree => OutboundEdges.Count();
    public int NeighboursCount => Neighbours.Count();
    public IEnumerable<Vertex<G, V, E>> Neighbours => Edges.SelectMany(e => new[] { e.From, e.To }).Distinct().Except(new[] { this });


    internal Vertex(G graph, int id) => (Graph, ID, _data) = (graph, id, default!);
#if ID_API
    public Edge<G, V, E> CreateEdge(int to) => CreateEdge(Graph[to]);
#endif
    public Edge<G, V, E> CreateEdge(Vertex<G, V, E> to)
    {
        if (!Graph.TryGetEdge(this, to, out Edge<G, V, E>? e))
            e = Graph.AddEdge(this, to);

        return e!;
    }

    public void Remove() => Graph.RemoveVertex(this);

    public void RemoveOutboundEdges()
    {
        foreach (Edge<G, V, E> edge in OutboundEdges)
            edge.Remove();
    }

    public void RemoveInboundEdges()
    {
        foreach (Edge<G, V, E> edge in InboundEdges)
            edge.Remove();
    }

    public void RemoveEdges()
    {
        foreach (Edge<G, V, E> edge in Edges)
            edge.Remove();
    }

    public override string ToString() => $"[{ID}] {Data}";

    public override int GetHashCode() => ID;

    public override bool Equals(object? obj) => obj is Vertex<G, V, E> v && Graph == v.Graph && ID == v.ID;

    public Vertex<G, V, E>? FindNearest(Predicate<Vertex<G, V, E>> selector) => Graph.FindNearest(this, selector);

    public Vertex<G, V, E>? FindNearest(Predicate<Vertex<G, V, E>> selector, SearchStrategy strategy) => Graph.FindNearest(this, selector, strategy);

    public bool TryFindPathTo(Vertex<G, V, E> end, out Path<G, V, E>? path) => TryFindPath(v => v.Equals(end), out path);

    public bool TryFindPathTo(Vertex<G, V, E> end, out Path<G, V, E>? path, SearchStrategy strategy) => TryFindPath(v => v.Equals(end), out path, strategy);

    public bool TryFindPath(Predicate<Vertex<G, V, E>> selector, out Path<G, V, E>? path) => TryFindPath(selector, out path, SearchStrategy.DepthFirst);

    public bool TryFindPath(Predicate<Vertex<G, V, E>> selector, out Path<G, V, E>? path, SearchStrategy strategy)
    {
        List<Vertex<G, V, E>> p = [];
        HashSet<Vertex<G, V, E>> d = [];
        bool res = strategy == SearchStrategy.BreadthFirst ? find_breadth(selector, new Queue<Vertex<G, V, E>>(), d, p) : find_depth(selector, d, p);

        if (res && strategy == SearchStrategy.BreadthFirst)
            p.Add(this);

        p.Reverse();

        path = res ? new Path<G, V, E>(Graph, p.ToArray()) : null;

        return res;
    }

    private bool find_breadth(Predicate<Vertex<G, V, E>> selector, Queue<Vertex<G, V, E>> queue, HashSet<Vertex<G,V,E>> visited, List<Vertex<G, V, E>> path)
    {
        visited.Add(this);
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            Vertex<G, V, E> v = queue.Dequeue();

            if (selector(v))
                return true;

            foreach (Vertex<G, V, E> n in v.Neighbours)
                if (!visited.Contains(n) && n.find_breadth(selector, queue, visited, path))
                {
                    path.Add(n);

                    return true;
                }
        }

        return false;
    }

    private bool find_depth(Predicate<Vertex<G, V, E>> selector, HashSet<Vertex<G, V, E>> visited, List<Vertex<G, V, E>> path)
    {
        visited.Add(this);

        if (selector(this))
        {
            path.Add(this);

            return true;
        }
        else
            foreach(Vertex<G, V, E> n in Neighbours)
                if (!visited.Contains(n))
                    if (n.find_depth(selector, visited, path))
                    {
                        path.Add(this);

                        return true;
                    }

        return false;
    }

    public static bool operator ==(Vertex<G, V, E>? v1, Vertex<G, V, E>? v2) => v1?.Equals(v2) ?? v2 is null;

    public static bool operator !=(Vertex<G, V, E>? v1, Vertex<G, V, E>? v2) => !(v1 == v2);

    public static implicit operator V(Vertex<G, V, E> v) => v.Data;
}

public class Edge<G, V, E>
    where G : Graph<G, V, E>
{
    private readonly int _from, _to;
    private E _data;

    public event EdgeDataChangedEvent<G, V, E> OnDataChanged;

    public E Data
    {
        set
        {
            _data = value;

            OnDataChanged?.Invoke(this, value);
        }
        get => _data;
    }
    public Vertex<G, V, E> From => Graph[_from];
    public Vertex<G, V, E> To => Graph[_to];
    public G Graph { get; }
    public bool IsLoop => _from == _to;

    internal Edge(G graph, int from, int to) => (Graph, _from, _to) = (graph, from, to);

    public void Remove() => Graph.RemoveEdge(this);

    public override string ToString() => $"[{From} --> {To}] {Data}";

    public override bool Equals(object? obj) => obj is Edge<G, V, E> e && Graph == e.Graph && _from == e._from && _to == e._to;

    public override int GetHashCode() => HashCode.Combine(_from, _to);

    public void Deconstruct(out int from, out int to) => (from, to) = (_from, _to);

    public void Deconstruct(out Vertex<G, V, E> from, out Vertex<G, V, E> to) => (from, to) = (From, To);

    public static bool operator ==(Edge<G, V, E>? v1, Edge<G, V, E>? v2) => v1?.Equals(v2) ?? v2 is null;

    public static bool operator !=(Edge<G, V, E>? v1, Edge<G, V, E>? v2) => !(v1 == v2);

    public static implicit operator E(Edge<G, V, E> e) => e.Data;
}

public class Path<G, V, E>
    : IEnumerable<Edge<G, V, E>>
    where G : Graph<G, V, E>
{
    public Edge<G, V, E>[] Edges { get; }
    public Vertex<G, V, E>[] Vertices { get; }
    public Vertex<G, V, E> Last => Vertices[Vertices.Length - 1];
    public Vertex<G, V, E> First => Vertices[0];
    public bool HasCycle => Vertices.Length - Vertices.Distinct().Count() > 0;
    public bool IsCycle => First == Last;
    public G Graph { get; }


    internal Path(G graph, params int[] vertices)
        : this(graph, vertices.ToArray(id => graph[id]))
    {
    }

    internal Path(G graph, params Vertex<G, V, E>[] vertices)
    {
        Graph = graph;
        Vertices = vertices;
        Edges = vertices.Skip(1).ToArray((v, i) => graph[vertices[i], v]);
    }

    public IEnumerator<Edge<G, V, E>> GetEnumerator() => Edges.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Edges.GetEnumerator();

    public Path<G, V, E> Prepend(params Vertex<G, V, E>[] vertices) => Concat(vertices, this);

    public Path<G, V, E> Append(params Vertex<G, V, E>[] vertices) => Concat(this, vertices);

    public Path<G, V, E> PrependCreate(params Vertex<G, V, E>[] vertices) => ConcatCreate(vertices, this);

    public Path<G, V, E> AppendCreate(params Vertex<G, V, E>[] vertices) => ConcatCreate(this, vertices);

    // implement sub-path/range

    public override string ToString() => string.Join(" --> ", Vertices.Select(v => v.ToString()));

    public static Path<G, V, E> Concat(params Path<G, V, E>[] paths) => paths.SelectMany(p => p.Vertices).ToArray();

    public static Path<G, V, E> ConcatCreate(params Path<G, V, E>[] paths)
    {
        Vertex<G, V, E>[] vertices = paths.SelectMany(p => p.Vertices).ToArray();
        G[] gs = vertices.Select(v => v.Graph).Distinct().ToArray();

        if (gs.Length != 1)
            throw new ArgumentException("All vertices must be part of the same graph.", nameof(vertices));

        return gs[0].AddPath(vertices);
    }

    public static Path<G, V, E> FromVertices(IEnumerable<Vertex<G, V, E>> vertices)
    {
        G[] gs = vertices.Select(v => v.Graph).Distinct().ToArray();

        if (gs.Length != 1)
            throw new ArgumentException("All vertices must be part of the same graph.", nameof(vertices));

        return new Path<G, V, E>(gs[0], vertices.ToArray());
    }

    public static implicit operator Edge<G, V, E>[] (Path<G, V, E> path) => path.Edges;

    public static implicit operator Vertex<G, V, E>[] (Path<G, V, E> path) => path.Vertices;

    public static implicit operator Path<G, V, E>(Vertex<G, V, E>[] vertices) => FromVertices(vertices);
}

public abstract class Graph<G, V, E>
    : IEnumerable<Vertex<G, V, E>>
    , IEnumerable<Edge<G, V, E>>
    , ICloneable
    where G : Graph<G, V, E>
{
    private readonly HashSet<Vertex<G, V, E>> _vertices = [];
    private readonly HashSet<Edge<G, V, E>> _edges = [];

    public event GraphVertexEvent<G, V, E> VertexRemoved;
    public event GraphVertexEvent<G, V, E> VertexAdded;
    public event GraphEdgeEvent<G, V, E> EdgeRemoved;
    public event GraphEdgeEvent<G, V, E> EdgeAdded;

#if ID_API
    public
#else
    internal
#endif
    Vertex<G, V, E> this[int id] => TryGetVertex(id, out Vertex<G, V, E>? v) ? v! : throw new KeyNotFoundException($"The graph does not contain any vertex with the ID '{id}'.");
    public Vertex<G, V, E>? this[V data] => GetVerticesByData(data).FirstOrDefault();
    public IEnumerable<Edge<G, V, E>> this[Vertex<G, V, E> v] => GetEdges(v);
#if ID_API
    public Edge<G, V, E> this[int from, int to] => this[this[from], this[to]];
#endif
    public Edge<G, V, E> this[Vertex<G, V, E> from, Vertex<G, V, E> to] => TryGetEdge(from, to, out Edge<G, V, E>? e) ? e! : throw new KeyNotFoundException($"The graph does not contain an edge '{from} --> {to}'.");
    public Vertex<G, V, E>[] Vertices => _vertices.ToArray();
    public Edge<G, V, E>[] Edges => _edges.ToArray();
    public virtual int VertexCount => _vertices.Count;
    public virtual int EdgeCount => _edges.Count;
    protected abstract G Instance { get; }


    internal Graph()
    {
    }

    public bool TryGetVertex(int id, out Vertex<G, V, E>? v)
    {
        v = default;

        if (_vertices.FirstOrDefault(ve => ve.ID == id) is { } _v)
        {
            v = _v;

            return true;
        }

        return false;
    }

    public Vertex<G, V, E> Add(V data) => AddVertex(data);

    public Edge<G, V, E> Add(V from, V to) => AddEdge(from, to);

    public void Add(V from, V to, E data) => AddEdge(from, to).Data = data;

    public Vertex<G, V, E> AddVertex()
    {
        int id = Enumerable.Range(0, VertexCount + 1).Except(_vertices.Select(ve => ve.ID)).Min();
        Vertex<G, V, E> v = new(Instance, id);

        _vertices.Add(v);

        VertexAdded?.Invoke(Instance, v);

        return v;
    }

    public Vertex<G, V, E> AddVertex(V data)
    {
        Vertex<G, V, E> v = AddVertex();

        v.Data = data;

        return v;
    }

    public Vertex<G, V, E>[] AddVertices(int count) => Enumerable.Range(0, count).ToArray(_ => AddVertex());

    public Vertex<G, V, E>[] AddVertices(params V[] data) => data.ToArray(AddVertex);

    public Edge<G, V, E> AddEdge(V from, V to) => AddEdge(this[from] ?? throw new ArgumentException($"No node with the value '{from}' could be found.", nameof(from)),
                                                          this[to] ?? throw new ArgumentException($"No node with the value '{to}' could be found.", nameof(to)));
#if ID_API
    public Edge<G, V, E> AddEdge(int from, int to) => AddEdge(this[from], this[to]);
#endif
    public virtual Edge<G, V, E> AddEdge(Vertex<G, V, E> from, Vertex<G, V, E> to)
    {
        if (!HasVertex(from))
            throw new KeyNotFoundException();
        else if (!HasVertex(to))
            throw new KeyNotFoundException();
        else if (!HasEdge(from, to))
        {
            Edge<G, V, E> e = new(Instance, from.ID, to.ID);

            _edges.Add(e);

            EdgeAdded?.Invoke(Instance, e);

            return e;
        }

        return this[from, to];
    }
#if ID_API
    public Edge<G, V, E> AddEdge(int from, int to, E data) => AddEdge(this[from], this[to], data);
#endif
    public Edge<G, V, E> AddEdge(Vertex<G, V, E> from, Vertex<G, V, E> to, E data)
    {
        Edge<G, V, E> e = AddEdge(from, to);

        e.Data = data;

        return e;
    }
    public bool HasVertex(Vertex<G, V, E> vertex) => vertex.Graph == this && _vertices.Contains(vertex);
#if ID_API
    public bool HasVertex(int id) => TryGetVertex(id, out _);
    
    public
#else
    internal
#endif
    virtual bool TryGetEdge(int from, int to, out Edge<G, V, E> edge) => (edge = _edges.FirstOrDefault(e => e.From.ID == from && e.To.ID == to)) is { };
#if ID_API
    public
#else
    internal
#endif
    virtual bool HasEdge(int from, int to) => _edges.Any(e => e.From.ID == from && e.To.ID == to);

    public bool HasEdge(Vertex<G, V, E> from, Vertex<G, V, E> to) => from.Graph == this && to.Graph == this && HasEdge(from.ID, to.ID);

    public bool TryGetEdge(Vertex<G, V, E> from, Vertex<G, V, E> to, out Edge<G, V, E>? edge)
    {
        edge = null;

        if (from.Graph == this && to.Graph == this)
            TryGetEdge(from.ID, to.ID, out edge);

        return edge is { };
    }

    public IEnumerable<Vertex<G, V, E>> GetVerticesByData(V data) => _vertices.Where(v => v.Data?.Equals(data) ?? data is null);

    public IEnumerable<Edge<G, V, E>> GetEdgesByData(E data) => _edges.Where(v => v.Data?.Equals(data) ?? data is null);
#if ID_API
    public void RemoveVertex(int id)
    {
        if (TryGetVertex(id, out Vertex<G, V, E>? v))
            RemoveVertex(v!);
    }
#endif
    public void RemoveVertex(Vertex<G, V, E> vertex)
    {
        if (vertex.Graph == this)
        {
            RemoveEdgesWhere(e => e.From.ID == vertex.ID || e.To.ID == vertex.ID);

            VertexRemoved?.Invoke(Instance, vertex);

            _vertices.RemoveWhere(v => v.ID == vertex.ID);
        }
    }

    public int RemoveVerticesWhere(Predicate<Vertex<G, V, E>> predicate)
    {
        int cnt = 0;

        foreach (var v in Vertices)
            if (predicate(v))
            {
                RemoveVertex(v);

                ++cnt;
            }

        return cnt;
    }
#if ID_API
    public void RemoveEdge(int from, int to)
    {
        if (TryGetEdge(from, to, out var e))
            RemoveEdge(e);
    }
#endif
    public virtual void RemoveEdge(Vertex<G, V, E> from, Vertex<G, V, E> to)
    {
        if (TryGetEdge(from, to, out Edge<G, V, E>? e))
            RemoveEdge(e!);
    }

    public void RemoveEdge(Edge<G, V, E> edge)
    {
        if (edge.Graph == this)
            if (_edges.Remove(edge))
                EdgeRemoved?.Invoke(Instance, edge);
    }

    public int RemoveEdgesWhere(Predicate<Edge<G,V,E>> predicate)
    {
        int cnt = 0;

        foreach (var e in Edges)
            if (predicate(e))
            {
                RemoveEdge(e);

                ++cnt;
            }

        return cnt;
    }
#if ID_API
    public IEnumerable<Edge<G, V, E>> GetEdges(int id) => GetEdges(this[id]);
#endif
    public IEnumerable<Edge<G, V, E>> GetEdges(Vertex<G, V, E> v) => _edges.Where(e => e.From.Equals(v) || e.To.Equals(v));
#if ID_API
    public Path<G, V, E> AddPath(params int[] ids) => AddPath(ids.ToArray(i => this[i]));
#endif
    public Path<G, V, E> AddPath(params Vertex<G, V, E>[] vertices)
    {
        for (int i = 0; i < vertices.Length - 1; ++i)
            AddEdge(vertices[i], vertices[i + 1]);

        return new Path<G, V, E>(Instance, vertices);
    }
#if ID_API
    public Path<G, V, E> AddCycle(params int[] ids) => AddCycle(ids.ToArray(i => this[i]));
#endif
    public Path<G, V, E> AddCycle(params Vertex<G, V, E>[] vertices) => AddPath([.. vertices, vertices[0]]);

    public G2 Cast<G2, V2, E2>(Func<V, V2> vertex_cast, Func<E, E2> edge_cast)
        where G2 : Graph<G2, V2, E2>, new()
    {
        Dictionary<Vertex<G, V, E>, Vertex<G2, V2, E2>> d = [];
        G2 g = new();

        foreach (Vertex<G, V, E> v in Vertices)
            (d[v] = g.AddVertex()).Data = vertex_cast(v.Data);

        foreach (Edge<G, V, E> e in Edges)
            g.AddEdge(d[e.From], d[e.To]).Data = edge_cast(e.Data);

        return g;
    }

    public WeightedGraph<V> ToWeightedGraph(Func<E, double> weight_function) => Cast<WeightedGraph<V>, V, double>(LINQ.id, weight_function);
#if ID_API
    public void MakeClique(params int[] ids) => MakeClique(ids.ToArray(i => this[i]));
#endif
    public void MakeClique(params Vertex<G, V, E>[] vertices)
    {
        foreach (var e in from v1 in vertices
                          where v1?.Graph == Instance
                          from v2 in vertices
                          where v2?.Graph == Instance
                          where v1 != v2
                          select new { v1, v2 })
            AddEdge(e.v1!, e.v2!);
    }

    public bool TryFindShortestPath(Vertex<G, V, E> start, Vertex<G, V, E> destination, Func<Edge<G, V, E>, double> weight, out Path<G, V, E>? path) =>
        TryFindShortestPath(start, v => v == destination, weight, out path);

    public bool TryFindShortestPath(Vertex<G, V, E> start, Predicate<Vertex<G, V, E>> destination, Func<Edge<G, V, E>, double> weight, out Path<G, V, E>? path)
    {
        if (!HasVertex(start))
            throw new KeyNotFoundException($"The graph does not contain the given start vertex '{start}'.");

        Dictionary<Vertex<G, V, E>, Vertex<G, V, E>?> prev = [];
        Dictionary<Vertex<G, V, E>, double> dist = [];
        HashSet<Vertex<G, V, E>> q = [];
        Vertex<G, V, E>? dest = null;

        foreach (Vertex<G, V, E> v in Vertices)
        {
            dist[v] = double.PositiveInfinity;
            prev[v] = null;
            q.Add(v);
        }

        dist[start] = 0;
        path = null;

        while (q.Count > 0)
        {
            Vertex<G, V, E> u = (from v in q orderby dist[v] ascending select v).First();

            q.Remove(u);

            if (destination(u))
            {
                dest = u;

                break;
            }

            foreach (Vertex<G, V, E> n in u.Neighbours)
                if (HasEdge(u, n) && (dist[u] + weight(this[u, n])) is double d && d < dist[n])
                    (dist[n], prev[n]) = (d, u);
        }

        if (dest is null)
            return false;
        else if (dest == start)
            path = new[] { start, dest };
        else
        {
            List<Vertex<G, V, E>> s = [dest];

            while (prev.TryGetValue(dest, out Vertex<G, V, E>? p) && p is { })
            {
                s.Add(p!);
                dest = p!;
            }

            s.Reverse();
            path = s.ToArray();
        }

        return true;
    }

    public bool TryFindPath(Vertex<G, V, E> start, Predicate<Vertex<G, V, E>> selector, out Path<G, V, E>? path) => start.TryFindPath(selector, out path);

    public bool TryFindPath(Vertex<G, V, E> start, Predicate<Vertex<G, V, E>> selector, out Path<G, V, E>? path, SearchStrategy strategy) => start.TryFindPath(selector, out path, strategy);

    public bool TryFindPath(Vertex<G, V, E> start, Vertex<G, V, E> end, out Path<G, V, E>? path) => TryFindPath(start, v => v.Equals(end), out path);

    public bool TryFindPath(Vertex<G, V, E> start, Vertex<G, V, E> end, out Path<G, V, E>? path, SearchStrategy strategy) => TryFindPath(start, v => v.Equals(end), out path, strategy);

    public Vertex<G, V, E>? FindNearest(Vertex<G, V, E> start, Predicate<Vertex<G, V, E>> selector) => TryFindPath(start, selector, out Path<G, V, E>? path) ? path?.Last : null;

    public Vertex<G, V, E>? FindNearest(Vertex<G, V, E> start, Predicate<Vertex<G, V, E>> selector, SearchStrategy strategy) =>
        TryFindPath(start, selector, out Path<G, V, E>? path, strategy) ? path?.Last : null;

    public IEnumerator<Vertex<G, V, E>> GetEnumerator() => ((IEnumerable<Vertex<G, V, E>>)Vertices).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Vertices.GetEnumerator();

    IEnumerator<Edge<G, V, E>> IEnumerable<Edge<G, V, E>>.GetEnumerator() => ((IEnumerable<Edge<G, V, E>>)Edges).GetEnumerator();

    public object Clone() => MemberwiseClone<DirectedGraph<V, E>>();

    public G2 MemberwiseClone<G2>() where G2 : Graph<G2, V, E>, new() => Cast<G2, V, E>(LINQ.id, LINQ.id);
#if DEBUG
    public void DebugPrintToConsole()
    {
        Dictionary<Vertex<G, V, E>, int> ypos = [];
        string sfx = string.Concat(Enumerable.Repeat("·  ", _edges.Count));
        int wdh = Vertices.Max(v => (int)Math.Log10(v.ID) + 2);
        Edge<G, V, E>[] edges = Edges.Take(0x100).ToArray();

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("\n" + new string('-', wdh + 3 + sfx.Length));
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("V\\E" + new string(' ', wdh - 2));

        int yoffs = Console.CursorTop;

        for (int i = 0; i < edges.Length; ++i)
            Console.Write($" {i:x2}");

        Console.WriteLine();

        foreach (var v in Vertices)
        {
            ypos[v] = Console.CursorTop;

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(v.ID.ToString().PadLeft(wdh) + ": ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(sfx);
        }

        Console.CursorLeft = wdh + 2;

        foreach (var e in edges)
        {
            int from = ypos[e.From];
            int to = ypos[e.To];

            Console.CursorTop = from;

            if (from == to)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write('━');
            }
            else
            {
                Console.ForegroundColor = e.Data is null ? ConsoleColor.DarkGreen : ConsoleColor.Green;
                Console.Write(from < to ? '┓' : '┛');
                Console.CursorLeft--;

                for (int s = from < to ? 1 : -1, i = from + s; i != to; i += s)
                {
                    Console.CursorTop = i;
                    Console.Write('┃');
                    Console.CursorLeft--;
                }

                Console.CursorTop = to;
                Console.Write(from < to ? '┗' : '┏');
            }

            Console.Write('>');
            Console.CursorLeft++;
        }

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.CursorTop = yoffs + _vertices.Count + 2;
        Console.CursorLeft = 0;
        Console.WriteLine("Edges");

        for (int i = 0; i < edges.Length; ++i)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"{i:x2}: ");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (edges[i].Data is null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("[null]");
            }
            else if (edges[i].Data is IEnumerable e)
                Console.WriteLine($"{{ {string.Join(", ", e.Cast<object>())} }}");
            else
                Console.WriteLine(edges[i].Data);
        }

        Console.WriteLine(new string('-', wdh + 3 + sfx.Length));
    }
#endif

    // TODO:
    //  - find next node
    //  - find path if existing
    //  - cycle detection
    //  - adjacency matrix
    //  - incidence matrix
    //  - tree detection
    //  - bipartition
    //  - NPC problems (clique, HC, TSP, ....)
}

public class DirectedGraph<V, E>
    : Graph<DirectedGraph<V, E>, V, E>
{
    protected override DirectedGraph<V, E> Instance => this;
}

public class WeightedGraph<V>
    : Graph<WeightedGraph<V>, V, double>
{
    protected override WeightedGraph<V> Instance => this;


    public DirectedGraph<V, double> ToDirectedGraph() => Cast<DirectedGraph<V, double>, V, double>(LINQ.id, LINQ.id);

    public bool TryFindShortestPath(Vertex<WeightedGraph<V>, V, double> start, Vertex<WeightedGraph<V>, V, double> destination, out Path<WeightedGraph<V>, V, double>? path) =>
        TryFindShortestPath(start, v => v == destination, out path);

    public bool TryFindShortestPath(Vertex<WeightedGraph<V>, V, double> start, Predicate<Vertex<WeightedGraph<V>, V, double>> destination, out Path<WeightedGraph<V>, V, double>? path) =>
        TryFindShortestPath(start, destination, e => e.Data, out path);

    public static implicit operator DirectedGraph<V, double>(WeightedGraph<V> graph) => graph.ToDirectedGraph();
}

public enum SearchStrategy
{
    DepthFirst,
    BreadthFirst
}

/*
    public class UndirectedGraph<V, E>
        : Graph<UndirectedGraph<V, E>, V, E>
    {
        protected override UndirectedGraph<V, E> Instance => this;
        public override int EdgeCount => base.EdgeCount / 2;


        public override Edge<UndirectedGraph<V, E>, V, E> AddEdge(Vertex<UndirectedGraph<V, E>, V, E> from, Vertex<UndirectedGraph<V, E>, V, E> to)
        {
            Edge<UndirectedGraph<V, E>, V, E> e1 = base.AddEdge(from, to);
            Edge<UndirectedGraph<V, E>, V, E> e2 = base.AddEdge(to, from);

            e1.OnDataChanged += (_, e) => e2._data = e;
            e2.OnDataChanged += (_, e) => e1._data = e;

            return e1;
        }

        public override bool HasEdge(int from, int to) => base.HasEdge(from, to) || base.HasEdge(to, from);

        public override bool TryGetEdge(int from, int to, out Edge<UndirectedGraph<V, E>, V, E> edge) => base.TryGetEdge(from, to, out edge) || base.TryGetEdge(to, from, out edge);

        public override void RemoveEdge(Vertex<UndirectedGraph<V, E>, V, E> from, Vertex<UndirectedGraph<V, E>, V, E> to)
        {
            base.RemoveEdge(from, to);
            base.RemoveEdge(to, from);
        }
    }
*/
