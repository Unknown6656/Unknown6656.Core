using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging.Plotting;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Geometry;


/// <summary>
/// Represents an abstract two-dimensional shape.
/// </summary>
/// <inheritdoc/>
public abstract class Shape2D
    : DrawableShape
{
    #region INSTANCE PROPERTIES

    /// <summary>
    /// Returns the shape's two-dimensional axis-aligned bounding box.
    /// </summary>
    public abstract AxisAlignedRectangle2D AxisAlignedBoundingBox { get; }

    /// <summary>
    /// Returns the shape's surface area.
    /// </summary>
    public abstract Scalar SurfaceArea { get; }

    /// <summary>
    /// Returns the shape's circumference.
    /// </summary>
    public abstract Scalar Circumference { get; }

    #endregion
    #region INSTANCE METHODS

    public abstract bool Contains(Vector2 point);

    public abstract bool Touches(Vector2 point);

    public abstract Line2D? GetNormalAt(Vector2 point);

    public virtual Line2D? GetTangentAt(Vector2 point) => GetNormalAt(point) is Line2D n ? n.Rotate(point, Scalar.PiHalf) : null;

    public Shape2D IntersectWith(Shape2D other) => new IntersectionShape(this, other);

    public Shape2D UnionWith(Shape2D other) => new UnionShape(this, other);

    public Shape2D Except(Shape2D other) => new DifferenceShape(this, other);

    public Shape2D Xor(Shape2D other) => new ExclusiveOrShape(this, other);

    #endregion
    #region OPERATORS

    public static Shape2D operator &(Shape2D s1, Shape2D s2) => s1.IntersectWith(s2);

    public static Shape2D operator |(Shape2D s1, Shape2D s2) => s1.UnionWith(s2);

    public static Shape2D operator /(Shape2D s1, Shape2D s2) => s1.Except(s2);

    public static Shape2D operator ^(Shape2D s1, Shape2D s2) => s1.Xor(s2);

    #endregion
    #region INTERNAL CLASSES

    internal sealed class ExclusiveOrShape
        : Shape2D
    {
        public Shape2D First { get; }

        public Shape2D Second { get; }

        public UnionShape Union { get; }

        public IntersectionShape Overlap { get; }

        public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Union.AxisAlignedBoundingBox;

        public override Scalar SurfaceArea => Union.SurfaceArea - Overlap.SurfaceArea;

        public override Scalar Circumference => throw new NotImplementedException();


        public ExclusiveOrShape(Shape2D first, Shape2D second)
        {
            First = first;
            Second = second;
            Union = new UnionShape(first, second);
            Overlap = new IntersectionShape(first, second);
        }

        public override Line2D? GetNormalAt(Vector2 point) => throw new NotImplementedException();

        public override bool Contains(Vector2 point) => First.Contains(point) ^ Second.Contains(point);

        public override bool Touches(Vector2 point) => First.Touches(point) ^ Second.Touches(point);

        protected internal override void internal_draw(RenderPass pass, RenderPassDrawMode mode)
        {
            Union.internal_draw(pass, mode);
            Overlap.internal_draw(pass, mode.Invert());
        }
    }

    internal sealed class DifferenceShape
        : Shape2D
    {
        public Shape2D First { get; }

        public Shape2D Second { get; }

        public IntersectionShape Overlap { get; }

        public override AxisAlignedRectangle2D AxisAlignedBoundingBox => throw new NotImplementedException();

        public override Scalar SurfaceArea => First.SurfaceArea - Overlap.SurfaceArea;

        public override Scalar Circumference => throw new NotImplementedException();


        public DifferenceShape(Shape2D first, Shape2D second)
        {
            First = first;
            Second = second;
            Overlap = new IntersectionShape(first, second);
        }

        public override Line2D? GetNormalAt(Vector2 point) => throw new NotImplementedException();

        public override bool Contains(Vector2 point) => First.Contains(point) && !Second.Contains(point);

        public override bool Touches(Vector2 point) => throw new NotImplementedException();

        protected internal override void internal_draw(RenderPass pass, RenderPassDrawMode mode)
        {
            First.internal_draw(pass, mode);
            Second.internal_draw(pass, mode.Invert());
        }
    }

    internal sealed class IntersectionShape
        : Shape2D
    {
        public Shape2D First { get; }

        public Shape2D Second { get; }

        public override AxisAlignedRectangle2D AxisAlignedBoundingBox => throw new NotImplementedException();

        public override Scalar SurfaceArea => throw new NotImplementedException();

        public override Scalar Circumference => throw new NotImplementedException();


        public IntersectionShape(Shape2D first, Shape2D second)
        {
            First = first;
            Second = second;
        }

        public override Line2D? GetNormalAt(Vector2 point) => throw new NotImplementedException();

        public override bool Contains(Vector2 point) => First.Contains(point) && Second.Contains(point);

        public override bool Touches(Vector2 point) => throw new NotImplementedException();

        protected internal override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => throw new NotImplementedException();
    }

    internal sealed class UnionShape
        : Shape2D
    {
        public Shape2D First { get; }

        public Shape2D Second { get; }

        public IntersectionShape Overlap { get; }

        public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Rectangle2D.CreateAxisAlignedBoundingBox(First, Second);

        public override Scalar SurfaceArea => First.SurfaceArea + Second.SurfaceArea - Overlap.SurfaceArea;

        public override Scalar Circumference => throw new NotImplementedException();


        public UnionShape(Shape2D first, Shape2D second)
        {
            First = first;
            Second = second;
            Overlap = new IntersectionShape(First, Second);
        }

        public override Line2D? GetNormalAt(Vector2 point) => throw new NotImplementedException();

        public override bool Contains(Vector2 point) => First.Contains(point) || Second.Contains(point);

        public override bool Touches(Vector2 point) => throw new NotImplementedException();

        protected internal override void internal_draw(RenderPass pass, RenderPassDrawMode mode)
        {
            First.internal_draw(pass, mode);
            Second.internal_draw(pass, mode);
        }
    }

    #endregion
}

/// <inheritdoc/>
/// <typeparam name="T">Generic self-referencing shape type parameter.</typeparam>
public abstract class Shape2D<T>
    : Shape2D
    where T : Shape2D<T>
{
    /// <summary>
    /// Returns the shape's center point.
    /// </summary>
    public abstract Vector2 CenterPoint { get; }


    /// <summary>
    /// Mirrors the current shape in a non-destructive fashion at the given mirror axis.
    /// </summary>
    /// <param name="axis">Mirror axis</param>
    /// <returns>Mirrored shape</returns>
    public abstract T MirrorAt(Line2D axis);

    /// <summary>
    /// Moves the current shape in a non-destructive fashion along the given offset vector.
    /// </summary>
    /// <param name="offset">The movement (translation) vector</param>
    /// <returns>The moved shape</returns>
    public abstract T MoveBy(Vector2 offset);

    /// <summary>
    /// Rotates the current shape in a non-destructive fashion around the origin counter-clockwise with the given angle.
    /// </summary>
    /// <param name="angle">Rotation angle in radians.</param>
    /// <returns>Rotated shape</returns>
    public abstract T Rotate(Scalar angle);

    public abstract T Scale(Scalar x, Scalar y);

    public T MirrorAt(Vector2 point) => Scale(point, Scalar.NegativeOne);

    public T MoveBy(Scalar Xoffset, Scalar Yoffset) => MoveBy(new Vector2(Xoffset, Yoffset));

    public T Scale(Scalar factor) => Scale(factor, factor);

    public T Scale(Vector2 origin, Scalar factor) => Scale(origin, factor, factor);

    public T Scale(Scalar origin_x, Scalar origin_y, Scalar factor) => Scale(origin_x, origin_y, factor, factor);

    public T Scale(Vector2 origin, Scalar x, Scalar y) => MoveBy(-origin).Scale(x, y).MoveBy(origin);

    public T Scale(Scalar origin_x, Scalar origin_y, Scalar x, Scalar y) => Scale(new Vector2(origin_x, origin_y), x, y);

    public T Rotate(Vector2 origin, Scalar angle) => MoveBy(-origin).Rotate(angle).MoveBy(origin);

    public T Rotate(Scalar origin_x, Scalar origin_y, Scalar angle) => Rotate(new Vector2(origin_x, origin_y), angle);

    public override sealed int GetHashCode() => 0;

    public override bool Equals(object? other) => other is T t && Equals(t);

    public abstract bool Equals(T? other);

    public bool Is(T other) => Equals(other);

    public bool IsNot(T other) => !Equals(other);

    public static T operator +(Shape2D<T> shape) => (T)shape;

    public static T operator -(Shape2D<T> shape) => shape.Scale(Scalar.NegativeOne);

    public static T operator *(Scalar factor, Shape2D<T> shape) => shape.Scale(factor);

    public static T operator *(Shape2D<T> shape, Scalar factor) => shape.Scale(factor);

    public static T operator +(Vector2 offset, Shape2D<T> shape) => shape.MoveBy(offset);

    public static T operator +(Shape2D<T> shape, Vector2 offset) => shape.MoveBy(offset);

    public static T operator -(Shape2D<T> shape, Vector2 offset) => shape.MoveBy(offset);
}

public abstract class TransformableShape2D<T>
    : Shape2D<T>
    where T : TransformableShape2D<T>
{
    public T Transform(Matrix2 matrix) => TransformHomogeneous(new Matrix3(matrix)[2, 2, 1]);

    public abstract T TransformHomogeneous(Matrix3 matrix);


    public static T operator *(Matrix2 matrix, TransformableShape2D<T> shape) => shape.Transform(matrix);

    public static T operator *(Matrix3 homogeneous_matrix, TransformableShape2D<T> shape) => shape.TransformHomogeneous(homogeneous_matrix);
}

public interface ICuttableShape2D<T>
    where T : Shape2D<T>, ICuttableShape2D<T>
{
    // todo : intersect
    // todo : xor, and, or, except, etc.

    T Except(T second);
    T MergeWith(T second);
    T IntersectWith(T second);
}

public interface ITriangulizable2D<T>
    where T : Shape2D<T>
{
    /// <summary>
    /// Triangulizes the current shape into a collection of triangles.
    /// </summary>
    /// <param name="triangle_count_hint">Triangle count hint (Note: this number is is only a hint - the underlying implementations are not obliged to respect the target triangle count.)</param>
    /// <returns>Collection of triangles</returns>
    Triangle2D[] Triangulize(long triangle_count_hint);
}

public abstract class Polygon2D<T>
    : TransformableShape2D<T>
    , ITriangulizable2D<T>
    where T : Polygon2D<T>
{
    public abstract Vector2[] Corners { get; }

    public virtual Line2D[] Sides
    {
        get
        {
            Vector2[] c = Corners;
            Line2D[] s = new Line2D[c.Length];

            Parallel.For(0, s.Length, i => s[i] = new Line2D(c[i], c[(i + 1) % c.Length]));

            return s;
        }
    }

    public override Vector2 CenterPoint
    {
        get
        {
            Vector2[] c = Corners;

            return c[0].Add(c[1..]).Divide(c.Length);
        }
    }

    public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Rectangle2D.CreateAxisAlignedBoundingBox(Corners);

    public override Scalar Circumference => Sides.Sum(s => (double)s.Length);

    public override Scalar SurfaceArea => Triangulize().Sum(t => (double)t.SurfaceArea);

    /// <inheritdoc cref="Triangulize(long)"/>
    public Triangle2D[] Triangulize() => Triangulize(0);

    /// <inheritdoc/>
    public virtual Triangle2D[] Triangulize(long triangle_count_hint)
    {
        Vector2[] corners = Corners;
        List<Triangle2D> triangles = [];

        if (corners.Length < 3)
            throw new InvalidOperationException($"A shape with only {corners.Length} corners cannot be triangulized. At least 3 corners are necessary.");
        else if (this is Triangle2D t)
            triangles.Add(t);
        else
        {
            // TODO

            throw new NotImplementedException();
        }

        return [.. triangles];
    }
}

/// <summary>
/// Represents a 1-dimensional, finite-lengthed line in 2D-space.
/// </summary>
public sealed class Line2D
    : TransformableShape2D<Line2D>
{
    /// <summary>
    /// The line's starting point (origin).
    /// </summary>
    public Vector2 From { get; }

    /// <summary>
    /// The line's end point.
    /// </summary>
    public Vector2 To { get; }

    /// <summary>
    /// The line's direction vector.
    /// <para/>
    /// This vector is NOT normalized. See <see cref="NormalizedDirection"/> for the normalized direction vector.
    /// </summary>
    public Vector2 Direction => To - From;

    /// <summary>
    /// The line's normalized direction vector.
    /// </summary>
    public Vector2 NormalizedDirection => ~Direction;

    public Line2D Normalized => new(From, Direction, 1);

    /// <summary>
    /// The line's length.
    /// </summary>
    public Scalar Length => Direction.Length;

    public Scalar OrientationAngle => Vector2.UnitX.AngleTo(Direction);

    /// <inheritdoc/>
    public override Vector2 CenterPoint => From + .5 * Direction;

    /// <inheritdoc/>
    public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Rectangle2D.CreateAxisAlignedBoundingBox(From, To);

    /// <inheritdoc/>
    public override Scalar SurfaceArea => 0;

    /// <inheritdoc/>
    public override Scalar Circumference => Length * 2;


    public Line2D()
        : this(Vector2.Zero, Vector2.Zero)
    {
    }

    public Line2D(Vector2 to)
        : this(Vector2.Zero, to)
    {
    }

    public Line2D(Vector2 from, Vector2 to)
    {
        From = from;
        To = to;
    }

    public Line2D(Vector2 from, Vector2 direction, Scalar length)
        : this(from, from + (~direction * length))
    {
    }

    /// <inheritdoc/>
    public override Line2D TransformHomogeneous(Matrix3 matrix) => new(matrix.HomogeneousMultiply(From), matrix.HomogeneousMultiply(To));

    /// <inheritdoc/>
    public override Line2D MirrorAt(Line2D axis) => new(From.MirrorAt(axis), To.MirrorAt(axis));

    /// <inheritdoc/>
    public override Line2D MoveBy(Vector2 offset) => new(From + offset, To + offset);

    /// <inheritdoc/>
    public override Line2D Rotate(Scalar angle) => Transform(Matrix2.CreateRotation(angle));

    /// <inheritdoc/>
    public override Line2D Scale(Scalar x, Scalar y) => Transform(Matrix2.DiagonalMatrix(x, y));

    public Scalar AngleTo(Line2D other) => ((Direction * other.Direction) / (Length * other.Length)).Acos();

    public Line2D AltitudeTo(Vector2 point)
    {
        Vector2 pa = point - From;
        Vector2 dir = Direction;
        Vector2 alt = pa - (dir * (pa * dir / dir.SquaredNorm));

        return new Line2D(point - alt, point);
    }

    public Scalar DistanceTo(Vector2 point) => AltitudeTo(point).Length;

    public bool Intersects(Line2D other) => GetStrictIntersection(other).HasValue;

    public bool IsParallelTo(Line2D other) => Direction.IsLinearDependant(other.Direction, out _);

    public Vector2? GetStrictIntersection(Line2D other)
    {
        if (GetLenientIntersection(other) is Vector2 x && Contains(x))
            return x;

        return null;
    }

    public Vector2? GetLenientIntersection(Line2D other)
    {
        if (IsParallelTo(other))
            return null;

        Matrix2 A = (other.Direction, -Direction);
        Vector2 b = From - other.From;
        VectorSpace2 x = A | b;

        if (!x.IsEmpty && x.Basis[0] is Vector2 v)
            return other.Interpolate(v[0]);

        return null;
    }

    public override bool Touches(Vector2 point) => Contains(point);

    public override bool Contains(Vector2 point) => (point - From).IsLinearDependant(Direction, out _);

    public override Line2D? GetNormalAt(Vector2 point)
    {
        Vector2 dir;

        if (point.Is(From))
            dir = -Direction;
        else if (point.Is(To))
            dir = Direction;
        else if (Contains(point))
            dir = Direction.Rotate(Scalar.PiHalf);
        else
            return null;

        return new Line2D(point, dir, 1);
    }

    public Vector2 Interpolate(Scalar factor) => From + (factor * Direction);

    internal protected override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => pass.DrawPolygon(mode, false, From, To);

    public override bool Equals(Line2D? other) => (From.Equals(other?.From) && To.Equals(other?.To))
                                               || (From.Equals(other?.To) && To.Equals(other?.From));

    public void Decompose(out Vector2 from, out Vector2 to)
    {
        from = From;
        to = To;
    }

    public void Decompose(out Vector2 from, out Vector2 direction, out Scalar length)
    {
        Vector2 dir = Direction;

        from = From;
        direction = ~dir;
        length = dir.Length;
    }


    public static Line2D Between(Vector2 from, Vector2 to) => new(from, to);

    public static Line2D FromDirection(Vector2 from, Vector2 direction) => new(from, from + direction);

    public static Line2D FromDirection(Vector2 from, Vector2 direction, Scalar length) => FromDirection(from, ~direction * length);

    public static (Line2D first, Line2D second)? GetAngleBisectors(Line2D first, Line2D second)
    {
        if (first.GetLenientIntersection(second) is Vector2 vec)
        {
            Vector2 dir = ~(first.NormalizedDirection + second.NormalizedDirection);
            Line2D bisec = new(vec, vec + dir);

            return (bisec, bisec.Rotate(bisec.From, Scalar.PiHalf));
        }

        return null;
    }


    public static explicit operator Line2D(Vector2 to) => new(to);

    public static implicit operator Line2D((Vector2 from, Vector2 to) end_points) => new(end_points.from, end_points.to);

    public static implicit operator (Vector2 From, Vector2 To)(Line2D line) => (line.From, line.To);

    public static implicit operator (Vector2 From, Vector2 Direction, Scalar Length)(Line2D line)
    {
        line.Decompose(out Vector2 f, out Vector2 d, out Scalar l);

        return (f, d, l);
    }

    public static Line2D operator *(Matrix2 matrix, Line2D line) => line.Transform(matrix);
}

public sealed class Triangle2D
    : Polygon2D<Triangle2D>
{
    public Vector2 CornerA { get; }
    public Vector2 CornerB { get; }
    public Vector2 CornerC { get; }

    public Line2D SideA => CornerB.To(CornerC);
    public Line2D SideB => CornerC.To(CornerA);
    public Line2D SideC => CornerA.To(CornerB);

    public override Vector2[] Corners => new[] { CornerA, CornerB, CornerC };

    public Line2D MedianA => CornerA.To(SideA.CenterPoint);
    public Line2D MedianB => CornerB.To(SideB.CenterPoint);
    public Line2D MedianC => CornerC.To(SideC.CenterPoint);

    public Line2D AltitudeA => SideA.AltitudeTo(CornerA);
    public Line2D AltitudeB => SideB.AltitudeTo(CornerB);
    public Line2D AltitudeC => SideC.AltitudeTo(CornerC);

    public Scalar AngleA => SideC.Direction.AngleTo(-SideB.Direction);
    public Scalar AngleB => SideA.Direction.AngleTo(-SideC.Direction);
    public Scalar AngleC => SideB.Direction.AngleTo(-SideA.Direction);

    /// <inheritdoc/>
    public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Rectangle2D.CreateAxisAlignedBoundingBox(CornerA, CornerB, CornerC);

    /// <inheritdoc/>
    public override Scalar Circumference => SideA.Length + SideB.Length + SideC.Length;

    /// <inheritdoc/>
    public override Scalar SurfaceArea => .5 * AltitudeA.Length * SideA.Length;

    /// <inheritdoc/>
    public override Vector2 CenterPoint => MedianA.GetLenientIntersection(MedianB).Value; // TODO

    public Vector2 Centroid => CenterPoint;

    public Vector2 OrthoCenter => AltitudeA.GetLenientIntersection(AltitudeB).Value; // TODO

    public Vector2 CircumCenter => throw new NotImplementedException(); // TODO


    /// <summary>
    /// Creates a new triangle from the given corner points.
    /// </summary>
    /// <param name="a">First corner point ("A")</param>
    /// <param name="b">Second corner point ("B")</param>
    /// <param name="c">Third corner point ("C")</param>
    public Triangle2D(Vector2 a, Vector2 b, Vector2 c)
    {
        CornerA = a;
        CornerB = b;
        CornerC = c;
    }

    public (Scalar A, Scalar B, Scalar C) GetBarycentricCoordinates(Vector2 point)
    {
        Matrix3 A = (
            CornerA.ToHomogeneousCoordinates(),
            CornerB.ToHomogeneousCoordinates(),
            CornerC.ToHomogeneousCoordinates()
        );
        Vector3 b = point.ToHomogeneousCoordinates();

        if ((A | b) is VectorSpace3 { IsEmpty: false, Basis: var x })
            return x[0];

        return (Scalar.NaN, Scalar.NaN, Scalar.NaN);
    }

    public Vector2 FromBarycentricCoordinates(Scalar a, Scalar b, Scalar c) => (a * CornerA) + (b * CornerB) + (c * CornerC);

    public Vector2 FromBarycentricCoordinates(Vector3 coordinates) => FromBarycentricCoordinates(coordinates.X, coordinates.Y, coordinates.Z);

    public override bool Contains(Vector2 point)
    {
        (Scalar A, Scalar B, Scalar C) = GetBarycentricCoordinates(point);

        return A.Min(B).Min(C) >= 0;
    }

    public override bool Touches(Vector2 point) => Sides.Any(s => s.Contains(point));

    public override Line2D? GetNormalAt(Vector2 point)
    {
        Vector2 dir;

        if (point.Is(CornerA))
            dir = SideB.Direction - SideC.Direction;
        else if (point.Is(CornerB))
            dir = SideC.Direction - SideA.Direction;
        else if (point.Is(CornerC))
            dir = SideA.Direction - SideB.Direction;
        else
            return Sides.Aggregate(null as Line2D, (n, s) => n ?? s.GetNormalAt(point));

        return new Line2D(point, dir, 1);
    }

    public override bool Equals(Triangle2D? other) => Corners.SetEquals(other?.Corners);

    public override Triangle2D MirrorAt(Line2D axis) => new(CornerA.MirrorAt(axis), CornerB.MirrorAt(axis), CornerC.MirrorAt(axis));

    public override Triangle2D MoveBy(Vector2 offset) => new(CornerA + offset, CornerB + offset, CornerC + offset);

    public override Triangle2D Rotate(Scalar angle) => new(CornerA.Rotate(angle), CornerB.Rotate(angle), CornerC.Rotate(angle));

    public override Triangle2D Scale(Scalar x, Scalar y) => Transform((x, 0, 0, y));

    public override Triangle2D TransformHomogeneous(Matrix3 matrix)
    {
        Vector2 a = matrix.HomogeneousMultiply(CornerA);
        Vector2 b = matrix.HomogeneousMultiply(CornerB);
        Vector2 c = matrix.HomogeneousMultiply(CornerC);

        return new Triangle2D(a, b, c);
    }

    internal protected override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => pass.DrawPolygon(mode, true, CornerA, CornerB, CornerC);

    public void Decompose(out Vector2 a, out Vector2 b, out Vector2 c)
    {
        a = CornerA;
        b = CornerB;
        c = CornerC;
    }


    public static implicit operator Triangle2D((Vector2 a, Vector2 b, Vector2 c) corners) => new(corners.a, corners.b, corners.c);

    public static implicit operator (Vector2 a, Vector2 b, Vector2 c)(Triangle2D triangle) => (triangle.CornerA, triangle.CornerB, triangle.CornerC);
}

public class Quadrilateral2D
{
     // TODO
}

/// <summary>
/// Represents a general two-dimensional parallelogram defined by the four corner points
/// A (<see cref="Parallelogram2D.BottomLeft"/>), B (<see cref="Parallelogram2D.BottomRight"/>), C (<see cref="Parallelogram2D.TopRight"/>), and D (<see cref="Parallelogram2D.TopLeft"/>).
/// <para/>
/// <code>
/// D  C<br/>
/// +-+ <br/>
/// |  |<br/>
/// +-+ <br/>
/// A  B<br/>
/// </code>
/// </summary>
/// <inheritdoc/>
public class Parallelogram2D
    : Polygon2D<Parallelogram2D>
{
    private readonly Vector2 _bl_corner;
    internal readonly Vector2 _right_dir;
    private readonly Vector2 _up_dir;

    public Vector2 BottomLeft => _bl_corner;
    public Vector2 BottomRight => _bl_corner + _right_dir;
    public Vector2 TopRight => _bl_corner + _right_dir + _up_dir;
    public Vector2 TopLeft => _bl_corner + _up_dir;

    public override Vector2[] Corners => new[] { BottomLeft, BottomRight, TopRight, TopLeft };
    public override Line2D[] Sides => new[] { BottomSide, RightSide, TopSide, LeftSide };

    public override Vector2 CenterPoint => _bl_corner + .5 * (_right_dir + _up_dir);

    public Line2D LeftSide => BottomLeft.To(TopLeft);
    public Line2D RightSide => BottomRight.To(TopRight);
    public Line2D BottomSide => BottomLeft.To(BottomRight);
    public Line2D TopSide => TopLeft.To(TopRight);
    public Line2D Diagonal1 => BottomLeft.To(TopRight);
    public Line2D Diagonal2 => TopLeft.To(BottomRight);

    public virtual Scalar BottomLeftAngle => BottomSide.AngleTo(LeftSide);
    public virtual Scalar BottomRightAngle => Scalar.Pi - BottomLeftAngle;
    public virtual Scalar TopRightAngle => BottomLeftAngle;
    public virtual Scalar TopLeftAngle => BottomRightAngle;
    public virtual bool IsRectangular => BottomLeftAngle.IsMultipleOf(Scalar.PiHalf);
    public virtual bool IsAxisAligned => IsRectangular && LeftSide.OrientationAngle.IsMultipleOf(Scalar.PiHalf);

    public virtual Scalar Width => BottomLeft.DistanceTo(RightSide);
    public virtual Scalar Height => BottomLeft.DistanceTo(TopSide);

    public Line2D VerticalAltitude => BottomSide.AltitudeTo(TopRight);
    public Line2D HorizontalAltitude => LeftSide.AltitudeTo(TopRight);

    public Scalar SmallestWidth => HorizontalAltitude.Length;
    public Scalar SmallestHeight => VerticalAltitude.Length;

    public override AxisAlignedRectangle2D AxisAlignedBoundingBox => Rectangle2D.CreateAxisAlignedBoundingBox(BottomLeft, BottomRight, TopLeft, TopRight);
    public override Scalar SurfaceArea => SmallestWidth * LeftSide.Length;
    public override Scalar Circumference => 2 * (LeftSide.Length + BottomSide.Length);


    public Parallelogram2D(Vector2 bottom_left, Vector2 right_dir, Vector2 up_dir)
    {
        _bl_corner = bottom_left;
        _right_dir = right_dir;
        _up_dir = up_dir;
    }

    public Parallelogram2D(Vector2 bottom_left, Vector2 bottom_right, Vector2 top_right, Vector2 top_left)
    {
        Vector2 right = bottom_right - bottom_left;
        Vector2 up = top_left - bottom_left;

        if (!right.IsLinearDependant(top_right - top_left, out _))
            throw new ArgumentException("The top and bottom sides are not parallel.", nameof(top_right));

        if (!up.IsLinearDependant(top_right - bottom_right, out _))
            throw new ArgumentException("The left and right sides are not parallel.", nameof(top_right));

        _bl_corner = bottom_left;
        _right_dir = right;
        _up_dir = up;
    }

    public override bool Touches(Vector2 point) => Sides.Any(s => s.Contains(point));

    public override bool Contains(Vector2 point)
    {
        Vector2 b = point - BottomLeft;
        Matrix2 A = (_right_dir, _up_dir);
        VectorSpace2 solution = A | b;

        if (solution.IsEmpty)
            return false;

        Vector2 x = solution.Basis[0];

        return x.X.IsBetween(0, 1) && x.Y.IsBetween(0, 1);
    }

    public override Line2D? GetNormalAt(Vector2 point)
    {
        Vector2 dir;

        if (point.Is(BottomLeft))
            dir = -BottomSide.Direction - LeftSide.Direction;
        else if (point.Is(BottomRight))
            dir = BottomSide.Direction - LeftSide.Direction;
        else if (point.Is(TopRight))
            dir = BottomSide.Direction + LeftSide.Direction;
        else if (point.Is(TopLeft))
            dir = -BottomSide.Direction + LeftSide.Direction;
        else if ((LeftSide.GetNormalAt(point) ?? TopSide.GetNormalAt(point)) is Line2D n)
            return n;
        else if ((BottomSide.GetNormalAt(point) ?? RightSide.GetNormalAt(point)) is { Direction: Vector2 d })
            dir = -d;
        else
            return null;

        return new Line2D(point, dir, 1);
    }

    public override bool Equals(Parallelogram2D? other) => Corners.SetEquals(other?.Corners);

    public override Parallelogram2D MirrorAt(Line2D axis) => new(BottomLeft.MirrorAt(axis), BottomRight.MirrorAt(axis), TopRight.MirrorAt(axis), TopLeft.MirrorAt(axis));

    public override Parallelogram2D MoveBy(Vector2 offset) => new(_bl_corner + offset, _right_dir, _up_dir);

    public override Parallelogram2D Rotate(Scalar angle) => new(BottomLeft.Rotate(angle), BottomRight.Rotate(angle), TopRight.Rotate(angle), TopLeft.Rotate(angle));

    public override Parallelogram2D Scale(Scalar x, Scalar y) => Transform(Matrix2.DiagonalMatrix(x, y));

    internal protected override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => pass.DrawPolygon(mode, true, BottomLeft, BottomRight, TopRight, TopLeft);

    public override Parallelogram2D TransformHomogeneous(Matrix3 matrix)
    {
        Vector2 bl = matrix.HomogeneousMultiply(BottomLeft);
        Vector2 br = matrix.HomogeneousMultiply(BottomRight);
        Vector2 tr = matrix.HomogeneousMultiply(TopRight);
        Vector2 tl = matrix.HomogeneousMultiply(TopLeft);

        return new Parallelogram2D(bl, br, tr, tl);
    }
}

public class Rectangle2D
    : Parallelogram2D
{
    public Scalar OrientationAngle => Vector2.UnitX.AngleTo(_right_dir);

    public override Scalar BottomLeftAngle => Scalar.PiHalf;
    public override Scalar BottomRightAngle => Scalar.PiHalf;
    public override Scalar TopRightAngle => Scalar.PiHalf;
    public override Scalar TopLeftAngle => Scalar.PiHalf;
    public override bool IsRectangular => true;
    public override bool IsAxisAligned => OrientationAngle.IsMultipleOf(Scalar.PiHalf);

    public override Scalar Width { get; }
    public override Scalar Height { get; }
    public bool IsSquare => Width == Height;


    public Rectangle2D(Vector2 bottom_left, Scalar width, Scalar height)
        : this(bottom_left, Vector2.UnitX, width, height)
    {
    }

    public Rectangle2D(Vector2 bottom_left, Vector2 right_dir, Scalar width, Scalar height)
        : base(bottom_left, ~right_dir * width, ~right_dir.Rotate(Scalar.PiHalf) * height)
    {
        if (width.IsNegative)
            throw new ArgumentException("The width must not be negative.", nameof(width));
        else if (height.IsNegative)
            throw new ArgumentException("The height must not be negative.", nameof(height));

        Width = width;
        Height = height;
    }

    public Rectangle2D(Vector2 bottom_left, Vector2 bottom_right, Vector2 top_right, Vector2 top_left)
        : base(bottom_left, bottom_right, top_right, top_left)
    {
        if (!base.BottomLeftAngle.IsMultipleOf(Scalar.PiHalf) || !base.BottomRightAngle.IsMultipleOf(Scalar.PiHalf))
            throw new ArgumentException("The corner angles must be π/2 (90°).");

        Width = bottom_left.DistanceTo(bottom_right);
        Height = bottom_left.DistanceTo(top_left);
    }

    public override Rectangle2D MirrorAt(Line2D axis) => new(BottomLeft.MirrorAt(axis), BottomRight.MirrorAt(axis), TopRight.MirrorAt(axis), TopLeft.MirrorAt(axis));

    public override Rectangle2D MoveBy(Vector2 offset) => new(BottomLeft.MoveBy(offset), BottomRight.MoveBy(offset), TopRight.MoveBy(offset), TopLeft.MoveBy(offset));

    public override Rectangle2D Rotate(Scalar angle) => new(BottomLeft.Rotate(angle), BottomRight.Rotate(angle), TopRight.Rotate(angle), TopLeft.Rotate(angle));

    public override Rectangle2D Scale(Scalar x, Scalar y) => new(BottomLeft.Multiply(x, y), BottomRight.Multiply(x, y), TopRight.Multiply(x, y), TopLeft.Multiply(x, y));

    public static AxisAlignedRectangle2D CreateAxisAlignedBoundingBox(params Vector2[] vectors)
    {
        Scalar x_min = Scalar.PositiveInfinity;
        Scalar x_max = Scalar.NegativeInfinity;
        Scalar y_min = Scalar.PositiveInfinity;
        Scalar y_max = Scalar.NegativeInfinity;

        foreach (Vector2 vec in vectors)
        {
            x_min = x_min.Min(vec.X);
            x_max = x_max.Max(vec.X);
            y_min = y_min.Min(vec.Y);
            y_max = y_max.Max(vec.Y);
        }

        return new AxisAlignedRectangle2D((x_min, y_min), (x_max, y_max));
    }

    public static AxisAlignedRectangle2D CreateAxisAlignedBoundingBox(params Shape2D[] shapes) => CreateAxisAlignedBoundingBox(shapes.SelectMany(s => s.AxisAlignedBoundingBox.Corners).ToArray());
}

public sealed class AxisAlignedRectangle2D
    : Rectangle2D
{
    public Scalar Xmin { get; }
    public Scalar Ymin { get; }
    public Scalar Xmax { get; }
    public Scalar Ymax { get; }
    public override bool IsAxisAligned => true;


    public AxisAlignedRectangle2D(Vector2 bottom_left, Scalar width, Scalar height)
        : this(bottom_left, bottom_left + (width, height))
    {
    }

    public AxisAlignedRectangle2D(Vector2 bottom_left, Vector2 top_right)
        : this(bottom_left.X, bottom_left.Y, top_right.X, top_right.Y)
    {
    }

    public AxisAlignedRectangle2D(Scalar x_min, Scalar y_min, Scalar x_max, Scalar y_max)
        : this(x_min.Min(x_max), y_min.Min(y_max), x_min.Max(x_max), y_min.Max(y_max), default)
    {
    }

    private AxisAlignedRectangle2D(Scalar x_min, Scalar y_min, Scalar x_max, Scalar y_max, __empty _)
        : base((x_min, y_min), x_max - x_min, y_max - y_min)
    {
        Xmin = x_min;
        Ymin = y_min;
        Xmax = x_max;
        Ymax = y_max;
    }

    public override bool Contains(Vector2 point) => point.X.IsBetween(Xmin, Xmax) && point.Y.IsBetween(Ymin, Ymax);

    public override bool Equals(Parallelogram2D? other) => other is AxisAlignedRectangle2D aabb && Xmin == aabb.Xmin && Xmax == aabb.Xmax && Ymin == aabb.Ymin && Ymax == aabb.Ymax;

    public new AxisAlignedRectangle2D MirrorAt(Line2D axis)
    {
        if (axis.OrientationAngle.IsMultipleOf(Scalar.PiHalf))
            return CreateAxisAlignedBoundingBox(Corners.ToArray(c => c.MirrorAt(axis)));
        else
            throw new ArgumentException("The mirror axis must be either the horizontal (X) or vertical (Y) axis.");
    }

    public new AxisAlignedRectangle2D MoveBy(Vector2 offset) => new(BottomLeft + offset, Width, Height);

    public new AxisAlignedRectangle2D Scale(Scalar x, Scalar y) => new(BottomLeft, Width * x, Height * y);

    //convert to rectangle, rectanglef, bounds

}

public class Ellipse2D
    : Shape2D<Ellipse2D>
    , ITriangulizable2D<Ellipse2D>
{
    private readonly Vector2 _fp1, _fp2;


    public (Vector2 First, Vector2 Second) FocalPoints => (_fp1, _fp2);

    public (Vector2 First, Vector2 Second) VertexPoints
    {
        get
        {
            Vector2 to = SemiMajorAxis.To;

            return (to, to.Rotate(CenterPoint, Scalar.Pi));
        }
    }

    public (Vector2 First, Vector2 Second) CovertexPoints
    {
        get
        {
            Vector2 to = SemiMinorAxis.To;

            return (to, to.Rotate(CenterPoint, Scalar.Pi));
        }
    }

    public virtual bool IsCircle => _fp1.Is(_fp2);

    public Scalar Distance { get; }

    public Scalar OrientationAngle => SemiMajorAxis.OrientationAngle;

    public Scalar LinearEccentricity => _fp1.DistanceTo(_fp2) / 2;

    public Scalar Eccentricity => LinearEccentricity / Distance;

    public Scalar Width => 2 * Distance;

    public Scalar Height => ((Distance * Distance) - (LinearEccentricity ^ 2)).Sqrt();

    public Scalar SemiLatusRectum => Distance * (1 - (Eccentricity ^ 2));

    public Line2D SemiMajorAxis => new(CenterPoint, _fp1 - CenterPoint, Distance);

    public Line2D SemiMinorAxis => SemiMajorAxis.Rotate(CenterPoint, - Scalar.PiHalf); // TODO

    public override Vector2 CenterPoint => (_fp1 + _fp2) / 2;

    public override AxisAlignedRectangle2D AxisAlignedBoundingBox
    {
        get
        {
            Scalar a = Width;
            Scalar b = Height;
            Scalar φ = OrientationAngle;
            Scalar x = Scalar.Sqrt(φ.Cos().Multiply(a).Power(2) + φ.Sin().Multiply(b).Power(2));
            Scalar y = Scalar.Sqrt(φ.Sin().Multiply(a).Power(2) + φ.Cos().Multiply(b).Power(2));
            (Scalar cx, Scalar cy) = CenterPoint;

            return Rectangle2D.CreateAxisAlignedBoundingBox(
                (cx + x, cy + y),
                (cx + x, cy - y),
                (cx - x, cy + y),
                (cx - x, cy - y)
            );
        }
    }

    public override Scalar SurfaceArea => Scalar.Tau.Multiply(Width, Height);

    // approximation only!
    public override Scalar Circumference
    {
        get
        {
            Scalar a = Width;
            Scalar b = Height;
            Scalar h = ((a - b) / (a + b)) ^ 2;
            Scalar h3 = h * 3;

            return Scalar.Pi.Multiply(a + b, 1 + h3 / (10 + Scalar.Sqrt(4 - h3)));
        }
    }

    // TODO : circular directrix


    public Ellipse2D(Vector2 fp1, Vector2 fp2, Scalar distance)
    {
        if (distance <= fp1.DistanceTo(fp2))
            throw new ArgumentException("The distance must be larger than the eucledian distance between the two given focal points.", nameof(distance));

        _fp1 = fp1;
        _fp2 = fp2;
        Distance = distance;
    }

    public override bool Touches(Vector2 point) => point.DistanceTo(_fp1) + point.DistanceTo(_fp2) == Width;

    public override bool Contains(Vector2 point) => point.DistanceTo(_fp1) + point.DistanceTo(_fp2) <= Width;

    public override bool Equals(Ellipse2D? other) => other is { Distance: var d, _fp1: var f1, _fp2: var f2 } && Distance.Is(d) && new[] { _fp1, _fp2 }.SetEquals(new[] { f1, f2 });

    public override Line2D? GetNormalAt(Vector2 point)
    {
        (Matrix3 matrix, Matrix3 inverse) = GetTransformationMatrix();
        Vector2 p = inverse.HomogeneousMultiply(in point);

        if (p.Length.IsOne)
        {
            Line2D n = matrix * new Line2D(p, p * 2);

            return new Line2D(point, n.Direction, 1);
        }
        else
            return null;
    }

    public override Line2D? GetTangentAt(Vector2 point)
    {
        (Matrix3 matrix, Matrix3 inverse) = GetTransformationMatrix();
        Vector2 p = inverse.HomogeneousMultiply(in point);

        if (p.Length.IsOne)
        {
            Line2D n = matrix * new Line2D(p, p * 2).Rotate(Scalar.PiHalf);

            return new Line2D(point, n.Direction, 1);
        }
        else
            return null;
    }

    public override Ellipse2D MirrorAt(Line2D axis) => new(_fp1.MirrorAt(axis), _fp2.MirrorAt(axis), Distance);

    public override Ellipse2D MoveBy(Vector2 offset) => new(_fp1 + offset, _fp2 + offset, Distance);

    public override Ellipse2D Rotate(Scalar angle) => new(_fp1.Rotate(angle), _fp2.Rotate(angle), Distance);

    public override Ellipse2D Scale(Scalar x, Scalar y) => new(_fp2.ComponentwiseMultiply(x, y), _fp1.ComponentwiseMultiply(x, y), SemiMajorAxis.Scale(x, y).Length);

    public Triangle2D[] Triangulize(long triangle_count_hint)
    {
        if (triangle_count_hint < 1)
            throw new ArgumentOutOfRangeException(nameof(triangle_count_hint), "The triangle count hint must be positive and non-zero.");

        Scalar count = triangle_count_hint + 2;
        (Matrix3 matrix, _) = GetTransformationMatrix();
        Vector2[] corners = Enumerable.Range(0, (int)count).ToArray(i => matrix.HomogeneousMultiply(Vector2.UnitX.Rotate(i / count * Scalar.Tau)));

        return Enumerable.Range(1, (int)triangle_count_hint).ToArray(i => new Triangle2D(corners[0], corners[i], corners[i + 1]));
    }

    protected internal override void internal_draw(RenderPass pass, RenderPassDrawMode mode)
    {
        const int count = 128;
        (Matrix3 matrix, _) = GetTransformationMatrix();
        Vector2[] corners = Enumerable.Range(0, count).ToArray(i => matrix.HomogeneousMultiply(Vector2.UnitX.Rotate(i / count * Scalar.Tau)));

        pass.DrawEllipse(mode, corners);
    }

    internal (Matrix3 matrix, Matrix3 inverse) GetTransformationMatrix()
    {
        Scalar b = Height;
        Scalar a = Width;
        (Scalar px, Scalar py) = CenterPoint;
        Scalar φ = OrientationAngle;
        Scalar s = φ.Sin();
        Scalar c = φ.Cos();
        Scalar f = a.Multiply(b, c, c).Add(s * s);

        return (
            new Matrix3(
                a * c, -s, px,
                s, b * c, py,
                0, 0, 1
            ), new Matrix3(
                b * c, s, (b * px * c) + (py * s),
                -s, a * c, (px * s) - (a * py * c),
                0, 0, f
            ) / f
        );
    }
}

public sealed class Circle2D
    : Ellipse2D
{
    public Scalar Radius { get; }

    public override Vector2 CenterPoint { get; }

    public override bool IsCircle => true;

    public Scalar Diameter => Radius.Multiply(Scalar.Two);

    public override Scalar SurfaceArea => Radius * Radius * Scalar.Pi;

    public override Scalar Circumference => Radius.Multiply(Scalar.Tau);

    public override AxisAlignedRectangle2D AxisAlignedBoundingBox => new(CenterPoint - (Radius, Radius), Diameter, Diameter);


    public Circle2D(Vector2 center, Scalar radius)
        : base(center, center, radius)
    {
        Radius = radius;
        CenterPoint = center;
    }

    public new Circle2D MoveBy(Vector2 offset) => new(CenterPoint + offset, Radius);

    public override bool Touches(Vector2 point) => point.DistanceTo(CenterPoint) == Radius;

    public override bool Contains(Vector2 point) => point.DistanceTo(CenterPoint) <= Radius;

    public override Ellipse2D Rotate(Scalar angle) => new Circle2D(CenterPoint, Radius);
}
