using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Geometry
{
    public abstract class Shape3D
    {
        /// <summary>
        /// Returns the shape's surface area.
        /// </summary>
        public abstract Scalar SurfaceArea { get; }


        public abstract bool Contains(Vector3 point);

        public abstract bool Touches(Vector3 point);

        public abstract Line3D? GetNormalAt(Vector3 point);

        public virtual Line3D? GetTangentAt(Vector3 point) => GetNormalAt(point) is Line3D n ? n.Rotate(point, Scalar.PiHalf) : null;
    }

    public abstract class Shape3D<T>
        : Shape3D
        where T : Shape3D<T>
    {
        /// <summary>
        /// Returns the shape's center point.
        /// </summary>
        public abstract Vector3 CenterPoint { get; }


        public abstract T MirrorAt(Plane3D plane);

        /// <summary>
        /// Moves the current shape in a non-destructive fashion along the given offset vector.
        /// </summary>
        /// <param name="offset">The movement (translation) vector</param>
        /// <returns>The moved shape</returns>
        public abstract T MoveBy(Vector3 offset);

        /// <summary>
        /// Rotates the current shape in a non-destructive fashion around the origin using the three given euler angles.
        /// </summary>
        /// <param name="angle_x">Rotation angle around the X-axis (1,0,0) in radians.</param>
        /// <param name="angle_y">Rotation angle around the Y-axis (0,1,0) in radians.</param>
        /// <param name="angle_z">Rotation angle around the Z-axis (0,0,1) in radians.</param>
        /// <returns>Rotated shape</returns>
        public abstract T Rotate(Scalar euler_x, Scalar euler_y, Scalar euler_z);

        public abstract T Scale(Scalar x, Scalar y, Scalar z);

        public T MirrorAt(Vector3 point) => Scale(point, Scalar.NegativeOne);

        public T MoveBy(Scalar Xoffset, Scalar Yoffset, Scalar Zoffset) => MoveBy(new Vector3(Xoffset, Yoffset, Zoffset));

        public T Scale(Scalar factor) => Scale(factor, factor, factor);

        public T Scale(Vector3 origin, Scalar factor) => Scale(origin, factor, factor, factor);

        public T Scale(Scalar origin_x, Scalar origin_y, Scalar origin_z, Scalar factor) => Scale(origin_x, origin_y, origin_z, factor, factor, factor);

        public T Scale(Vector3 origin, Scalar x, Scalar y, Scalar z) => MoveBy(-origin).Scale(x, y, z).MoveBy(origin);

        public T Scale(Scalar origin_x, Scalar origin_y, Scalar origin_z, Scalar x, Scalar y, Scalar z) => Scale(new Vector3(origin_x, origin_y, origin_z), x, y, z);

        public T Rotate(Vector3 origin, Scalar angle) => MoveBy(-origin).Rotate(angle).MoveBy(origin);

        public T Rotate(Scalar origin_x, Scalar origin_y, Scalar origin_z, Scalar angle) => Rotate(new Vector3(origin_x, origin_y, origin_z), angle);

        public override sealed int GetHashCode() => 0;

        public override bool Equals(object? other) => other is T t && Equals(t);

        public abstract bool Equals(T? other);

        public bool Is(T other) => Equals(other);

        public bool IsNot(T other) => !Equals(other);

        public static T operator +(Shape3D<T> shape) => (T)shape;

        public static T operator -(Shape3D<T> shape) => shape.Scale(Scalar.NegativeOne);

        public static T operator *(Scalar factor, Shape3D<T> shape) => shape.Scale(factor);

        public static T operator *(Shape3D<T> shape, Scalar factor) => shape.Scale(factor);

        public static T operator +(Vector3 offset, Shape3D<T> shape) => shape.MoveBy(offset);

        public static T operator +(Shape3D<T> shape, Vector3 offset) => shape.MoveBy(offset);

        public static T operator -(Shape3D<T> shape, Vector3 offset) => shape.MoveBy(offset);
    }

    public abstract class TransformableShape3D<T>
        : Shape3D<T>
        where T : TransformableShape3D<T>
    {
        public T Transform(Matrix3 matrix) => TransformHomogeneous(matrix.ToHomogeneousTransformationMatrix());

        public abstract T TransformHomogeneous(Matrix4 matrix);

        public override T MirrorAt(Plane3D plane) => ;

        public override T MoveBy(Vector3 offset) => TransformHomogeneous(Matrix4.CreateTranslation(offset));

        public override T Rotate(Scalar x, Scalar y, Scalar z) => TransformHomogeneous(Matrix4.DiagonalMatrix(x, y, z, 1));

        public override T Scale(Scalar euler_x, Scalar euler_y, Scalar euler_z) => Transform(Matrix3.CreateRotationXYZ(euler_x, euler_y, euler_z));

        public static T operator *(Matrix3 matrix, TransformableShape3D<T> shape) => shape.Transform(matrix);

        public static T operator *(Matrix4 homogeneous_matrix, TransformableShape3D<T> shape) => shape.TransformHomogeneous(homogeneous_matrix);
    }

    public interface ITriangulizable3D<T>
        where T : Shape3D<T>
    {
        /// <summary>
        /// Triangulizes the current shape into a collection of triangles.
        /// </summary>
        /// <param name="triangle_count_hint">Triangle count hint (Note: this number is is only a hint - the underlying implementations are not obliged to respect the target triangle count.)</param>
        /// <returns>Collection of triangles</returns>
        Triangle3D[] Triangulize(long triangle_count_hint);
    }

    public abstract class Polygon3D<T>
        : TransformableShape3D<T>
        , ITriangulizable3D<T>
        where T : Polygon3D<T>
    {
        public abstract Vector3[] Corners { get; }

        public abstract Line3D[] Sides { get; }

        // public override AxisAlignedRectangle3D AxisAlignedRectangle3D => Rectangle3D.CreateAxisAlignedBoundingBox(Corners);

        public virtual Scalar SurfaceArea => Triangulize().Sum(t => (double)t.SurfaceArea);


        /// <inheritdoc cref="Triangulize(long)"/>
        public Triangle3D[] Triangulize() => Triangulize(0);

        public virtual Triangle3D[] Triangulize(long triangle_count_hint)
        {
            Vector3[] corners = Corners;
            List<Triangle3D> triangles = new();

            if (corners.Length < 3)
                throw new InvalidOperationException($"A shape with only {corners.Length} corners cannot be triangulized. At least 3 corners are necessary.");
            else if (this is Triangle3D t)
                triangles.Add(t);
            else
            {
                // TODO

                throw new NotImplementedException();
            }

            return triangles.ToArray();
        }
    }

    /// <summary>
    /// Represents a 1-dimensional, finite-lengthed line in 3D-space.
    /// </summary>
    public sealed class Line3D
        : TransformableShape3D<Line3D>
    {
        /// <summary>
        /// The line's starting point (origin).
        /// </summary>
        public Vector3 From { get; }

        /// <summary>
        /// The line's end point.
        /// </summary>
        public Vector3 To { get; }

        /// <summary>
        /// The line's direction vector.
        /// <para/>
        /// This vector is NOT normalized. See <see cref="NormalizedDirection"/> for the normalized direction vector.
        /// </summary>
        public Vector3 Direction => To - From;

        /// <summary>
        /// The line's normalized direction vector.
        /// </summary>
        public Vector3 NormalizedDirection => ~Direction;

        public Line3D Normalized => new(From, Direction, 1);

        /// <summary>
        /// The line's length.
        /// </summary>
        public Scalar Length => Direction.Length;

        public Scalar OrientationAngle => Vector3.UnitX.AngleTo(Direction);

        public override Vector3 CenterPoint => From + .5 * Direction;

        // public override AxisAlignedRectangle3D AxisAlignedBoundingBox => Rectangle3D.CreateAxisAlignedBoundingBox(From, To);

        public override Scalar SurfaceArea => 0;


        public Line3D()
            : this(Vector3.Zero, Vector3.Zero)
        {
        }

        public Line3D(Vector3 to)
            : this(Vector3.Zero, to)
        {
        }

        public Line3D(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }

        public Line3D(Vector3 from, Vector3 direction, Scalar length)
            : this(from, from + (~direction * length))
        {
        }

        public override Line3D TransformHomogeneous(Matrix3 matrix) => new(matrix.HomogeneousMultiply(From), matrix.HomogeneousMultiply(To));

        public override Line3D MirrorAt(Line3D axis) => new(From.MirrorAt(axis), To.MirrorAt(axis));

        public override Line3D MoveBy(Vector3 offset) => new(From + offset, To + offset);

        public override Line3D Rotate(Scalar angle) => Transform(Matrix3.CreateRotation(angle));

        public override Line3D Scale(Scalar x, Scalar y, Scalar z) => Transform(Matrix3.DiagonalMatrix(x, y, z));

        public Scalar AngleTo(Line3D other) => ((Direction * other.Direction) / (Length * other.Length)).Acos();

        public Line3D AltitudeTo(Vector3 point)
        {
            Vector3 pa = point - From;
            Vector3 dir = Direction;
            Vector3 alt = pa - (dir * (pa * dir / dir.SquaredNorm));

            return new Line3D(point - alt, point);
        }

        public Scalar DistanceTo(Vector3 point) => AltitudeTo(point).Length;

        public bool Intersects(Line3D other) => GetStrictIntersection(other).HasValue;

        public bool IsParallelTo(Line3D other) => Direction.IsLinearDependant(other.Direction, out _);

        public Vector3? GetStrictIntersection(Line3D other)
        {
            if (GetLenientIntersection(other) is Vector3 x && Contains(x))
                return x;

            return null;
        }

        public Vector3? GetLenientIntersection(Line3D other)
        {
            if (IsParallelTo(other))
                return null;

            Matrix3 A = (other.Direction, -Direction);
            Vector3 b = From - other.From;
            VectorSpace3 x = A | b;

            if (!x.IsEmpty && x.Basis[0] is Vector3 v)
                return other.Interpolate(v[0]);

            return null;
        }

        public override bool Touches(Vector3 point) => Contains(point);

        public override bool Contains(Vector3 point) => (point - From).IsLinearDependant(Direction, out _);

        public override Line3D? GetNormalAt(Vector3 point)
        {
            Vector3 dir;

            if (point.Is(From))
                dir = -Direction;
            else if (point.Is(To))
                dir = Direction;
            else if (Contains(point))
                dir = Direction.Rotate(Scalar.PiHalf);
            else
                return null;

            return new Line3D(point, dir, 1);
        }

        public Vector3 Interpolate(Scalar factor) => From + (factor * Direction);

        internal protected override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => pass.DrawPolygon(mode, false, From, To);

        public override bool Equals(Line3D? other) => (From.Equals(other?.From) && To.Equals(other?.To))
                                                   || (From.Equals(other?.To) && To.Equals(other?.From));

        public void Decompose(out Vector3 from, out Vector3 to)
        {
            from = From;
            to = To;
        }

        public void Decompose(out Vector3 from, out Vector3 direction, out Scalar length)
        {
            Vector3 dir = Direction;

            from = From;
            direction = ~dir;
            length = dir.Length;
        }


        public static Line3D Between(Vector3 from, Vector3 to) => new Line3D(from, to);

        public static Line3D FromDirection(Vector3 from, Vector3 direction) => new Line3D(from, from + direction);

        public static Line3D FromDirection(Vector3 from, Vector3 direction, Scalar length) => FromDirection(from, ~direction * length);

        public static (Line3D first, Line3D second)? GetAngleBisectors(Line3D first, Line3D second)
        {
            if (first.GetLenientIntersection(second) is Vector3 vec)
            {
                Vector3 dir = ~(first.NormalizedDirection + second.NormalizedDirection);
                Line3D bisec = new Line3D(vec, vec + dir);

                return (bisec, bisec.Rotate(bisec.From, Scalar.PiHalf));
            }

            return null;
        }


        public static explicit operator Line3D(Vector3 to) => new Line3D(to);

        public static implicit operator Line3D((Vector3 from, Vector3 to) end_points) => new Line3D(end_points.from, end_points.to);

        public static implicit operator (Vector3 From, Vector3 To)(Line3D line) => (line.From, line.To);

        public static implicit operator (Vector3 From, Vector3 Direction, Scalar Length)(Line3D line)
        {
            line.Decompose(out Vector3 f, out Vector3 d, out Scalar l);

            return (f, d, l);
        }

        public static Line3D operator *(Matrix3 matrix, Line3D line) => line.Transform(matrix);
    }

    public sealed class Triangle3D
        : Polygon3D<Triangle3D>
    {
        public Vector3 CornerA { get; }
        public Vector3 CornerB { get; }
        public Vector3 CornerC { get; }

        public Line3D SideA => CornerB.To(CornerC);
        public Line3D SideB => CornerC.To(CornerA);
        public Line3D SideC => CornerA.To(CornerB);

        public override Vector3[] Corners => new[] { CornerA, CornerB, CornerC };

        public Line3D MedianA => CornerA.To(SideA.CenterPoint);
        public Line3D MedianB => CornerB.To(SideB.CenterPoint);
        public Line3D MedianC => CornerC.To(SideC.CenterPoint);

        public Line3D AltitudeA => SideA.AltitudeTo(CornerA);
        public Line3D AltitudeB => SideB.AltitudeTo(CornerB);
        public Line3D AltitudeC => SideC.AltitudeTo(CornerC);

        public Scalar AngleA => SideC.Direction.AngleTo(-SideB.Direction);
        public Scalar AngleB => SideA.Direction.AngleTo(-SideC.Direction);
        public Scalar AngleC => SideB.Direction.AngleTo(-SideA.Direction);

        public override AxisAlignedRectangle3D AxisAlignedBoundingBox => Rectangle3D.CreateAxisAlignedBoundingBox(CornerA, CornerB, CornerC);

        public override Scalar Circumference => SideA.Length + SideB.Length + SideC.Length;

        public override Scalar SurfaceArea => .5 * AltitudeA.Length * SideA.Length;

        public override Vector3 CenterPoint => MedianA.GetLenientIntersection(MedianB).Value; // TODO

        public Vector3 Centroid => CenterPoint;

        public Vector3 OrthoCenter => AltitudeA.GetLenientIntersection(AltitudeB).Value; // TODO

        public Vector3 CircumCenter => throw new NotImplementedException(); // TODO


        /// <summary>
        /// Creates a new triangle from the given corner points.
        /// </summary>
        /// <param name="a">First corner point ("A")</param>
        /// <param name="b">Second corner point ("B")</param>
        /// <param name="c">Third corner point ("C")</param>
        public Triangle3D(Vector3 a, Vector3 b, Vector3 c)
        {
            CornerA = a;
            CornerB = b;
            CornerC = c;
        }

        public (Scalar A, Scalar B, Scalar C) GetBarycentricCoordinates(Vector3 point)
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

        public Vector3 FromBarycentricCoordinates(Scalar a, Scalar b, Scalar c) => (a * CornerA) + (b * CornerB) + (c * CornerC);

        public Vector3 FromBarycentricCoordinates(Vector3 coordinates) => FromBarycentricCoordinates(coordinates.X, coordinates.Y, coordinates.Z);

        public override bool Contains(Vector3 point)
        {
            (Scalar A, Scalar B, Scalar C) = GetBarycentricCoordinates(point);

            return A.Min(B).Min(C) >= 0;
        }

        public override bool Touches(Vector3 point) => Sides.Any(s => s.Contains(point));

        public override Line3D? GetNormalAt(Vector3 point)
        {
            Vector3 dir;

            if (point.Is(CornerA))
                dir = SideB.Direction - SideC.Direction;
            else if (point.Is(CornerB))
                dir = SideC.Direction - SideA.Direction;
            else if (point.Is(CornerC))
                dir = SideA.Direction - SideB.Direction;
            else
                return Sides.Aggregate(null as Line3D, (n, s) => n ?? s.GetNormalAt(point));

            return new Line3D(point, dir, 1);
        }

        public override bool Equals(Triangle3D? other) => Corners.SetEquals(other?.Corners);

        public override Triangle3D MirrorAt(Line3D axis) => new Triangle3D(CornerA.MirrorAt(axis), CornerB.MirrorAt(axis), CornerC.MirrorAt(axis));

        public override Triangle3D MoveBy(Vector3 offset) => new Triangle3D(CornerA + offset, CornerB + offset, CornerC + offset);

        public override Triangle3D Rotate(Scalar angle) => new Triangle3D(CornerA.Rotate(angle), CornerB.Rotate(angle), CornerC.Rotate(angle));

        public override Triangle3D Scale(Scalar x, Scalar y) => Transform((x, 0, 0, y));

        public override Triangle3D TransformHomogeneous(Matrix3 matrix)
        {
            Vector3 a = matrix.HomogeneousMultiply(CornerA);
            Vector3 b = matrix.HomogeneousMultiply(CornerB);
            Vector3 c = matrix.HomogeneousMultiply(CornerC);

            return new Triangle3D(a, b, c);
        }

        internal protected override void internal_draw(RenderPass pass, RenderPassDrawMode mode) => pass.DrawPolygon(mode, true, CornerA, CornerB, CornerC);

        public void Decompose(out Vector3 a, out Vector3 b, out Vector3 c)
        {
            a = CornerA;
            b = CornerB;
            c = CornerC;
        }


        public static implicit operator Triangle3D((Vector3 a, Vector3 b, Vector3 c) corners) => new Triangle3D(corners.a, corners.b, corners.c);

        public static implicit operator (Vector3 a, Vector3 b, Vector3 c)(Triangle3D triangle) => (triangle.CornerA, triangle.CornerB, triangle.CornerC);
    }


















}
