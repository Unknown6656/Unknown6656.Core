#nullable enable

using System.Runtime.CompilerServices;
using System.Drawing;
using System;

using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Geometry;
using Unknown6656.Imaging;

using num = System.Numerics;

using static System.Math;

namespace Unknown6656.Mathematics.LinearAlgebra
{
    public partial struct Matrix2
    {
        /// <summary>
        /// Returns a rotation 2x2 matrix which describes the rotation around the origin by the given angle.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns>Rotation matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2 CreateRotation(Scalar angle)
        {
            Scalar c = angle.Cos();
            Scalar s = angle.Sin();

            return new Matrix2(
                c, -s,
                s, c
            );
        }
    }

    public partial struct Matrix3
    {
        /// <summary>
        /// The 3x3 telephone matrix consisting of the values (1,2,3,4,5,6,7,8,9).
        /// </summary>
        public static Matrix3 TelephoneMatrix { get; } = (
            1, 2, 3,
            4, 5, 6,
            7, 8, 9
        );

        /// <summary>
        /// Returns a rotation 3x3 matrix which describes the rotation around the X-axis by the given angle.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns>Rotation matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateRotationX(Scalar angle)
        {
            Scalar c = angle.Cos();
            Scalar s = angle.Sin();

            return new Matrix3(
                1, 0, 0,
                0, c, -s,
                0, s, c
            );
        }

        /// <summary>
        /// Returns a 3x3 rotation matrix which describes the rotation around the Y-axis by the given angle.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns>Rotation matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateRotationY(Scalar angle)
        {
            Scalar c = angle.Cos();
            Scalar s = angle.Sin();

            return new Matrix3(
                c, 0, s,
                0, 1, 0,
                -s, 0, c
            );
        }

        /// <summary>
        /// Returns a 3x3 rotation matrix which describes the rotation around the Z-axis by the given angle.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns>Rotation matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateRotationZ(Scalar angle)
        {
            Scalar c = angle.Cos();
            Scalar s = angle.Sin();

            return new Matrix3(
                c, -s, 0,
                s, c, 0,
                0, 0, 1
            );
        }

        /// <summary
        /// Creates a homogeneous matrix for a 4-corner-pin transformation for 2D vectors using the given corner pin mappings.
        /// </summary>
        /// <param name="corner1">The transformed position of the first corner [x=0,y=0].</param>
        /// <param name="corner2">The transformed position of the second corner [x=1,y=0].</param>
        /// <param name="corner3">The transformed position of the third corner [x=1,y=1].</param>
        /// <param name="corner4">The transformed position of the fourth corner [x=0,y=1].</param>
        /// <returns>The homogeneous transformation matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 Create4CornerPinTransform(Vector2 corner1, Vector2 corner2, Vector2 corner3, Vector2 corner4) => Create4CornerPinTransform(
            (Vector2.Zero, corner1),
            (Vector2.UnitX, corner2),
            (new Vector2(1), corner3),
            (Vector2.UnitY, corner4)
        );

        /// <summary
        /// Creates a homogeneous matrix for a 4-corner-pin transformation for 2D vectors using the given corner pin mappings.
        /// </summary>
        /// <param name="corner1">A mapping for the first corner pin consisting of the original vector and target vector.</param>
        /// <param name="corner2">A mapping for the second corner pin consisting of the original vector and target vector.</param>
        /// <param name="corner3">A mapping for the third corner pin consisting of the original vector and target vector.</param>
        /// <param name="corner4">A mapping for the fourth corner pin consisting of the original vector and target vector.</param>
        /// <returns>The homogeneous transformation matrix</returns>
        public static Matrix3 Create4CornerPinTransform((Vector2 from, Vector2 to) corner1, (Vector2 from, Vector2 to) corner2, (Vector2 from, Vector2 to) corner3, (Vector2 from, Vector2 to) corner4)
        {
            Matrix8 A = (
                corner1.from.X, corner1.from.Y, 1, 0, 0, 0, -corner1.from.X * corner1.to.X, -corner1.from.Y * corner1.to.X,
                0, 0, 0, corner1.from.X, corner1.from.Y, 1, -corner1.from.X * corner1.to.Y, -corner1.from.Y * corner1.to.Y,
                corner2.from.X, corner2.from.Y, 1, 0, 0, 0, -corner2.from.X * corner2.to.X, -corner2.from.Y * corner2.to.X,
                0, 0, 0, corner2.from.X, corner2.from.Y, 1, -corner2.from.X * corner2.to.Y, -corner2.from.Y * corner2.to.Y,
                corner3.from.X, corner3.from.Y, 1, 0, 0, 0, -corner3.from.X * corner3.to.X, -corner3.from.Y * corner3.to.X,
                0, 0, 0, corner3.from.X, corner3.from.Y, 1, -corner3.from.X * corner3.to.Y, -corner3.from.Y * corner3.to.Y,
                corner4.from.X, corner4.from.Y, 1, 0, 0, 0, -corner4.from.X * corner4.to.X, -corner4.from.Y * corner4.to.X,
                0, 0, 0, corner4.from.X, corner4.from.Y, 1, -corner4.from.X * corner4.to.Y, -corner4.from.Y * corner4.to.Y
            );
            Vector8 b = (
                corner1.to.X,
                corner1.to.Y,
                corner2.to.X,
                corner2.to.Y,
                corner3.to.X,
                corner3.to.Y,
                corner4.to.X,
                corner4.to.Y
            );
            VectorSpace8 s = A | b;
            Vector8 x = s.Basis[0];

            return new Matrix3(
                x[0], x[1], x[2],
                x[3], x[4], x[5],
                x[6], x[7], 1
            );
        }
    }

    public partial struct Matrix4
    {
        /// <summary>
        /// Applies a translation transformation to matrix <paramref name="m"/> by vector <paramref name="v"/>.
        /// </summary>
        /// <param name="m">The matrix to transform.</param>
        /// <param name="v">The vector to translate by.</param>
        /// <returns><paramref name="m"/> translated by <paramref name="v"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 Translate(Vector3 v)
        {
            Matrix4 result = this;
            Vector4 t = this[0] * v[0] + this[1] * v[1] + this[2] * v[2] + this[3];

            return result[1, t];
        }

        /// <summary>
        /// Creates a frustrum projection matrix.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
        /// <param name="top">The top.</param>
        /// <param name="nearVal">The near val.</param>
        /// <param name="farVal">The far val.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Frustum(Scalar left, Scalar right, Scalar bottom, Scalar top, Scalar nearVal, Scalar farVal) => (
            2 * nearVal / (right - left), 0, 0, 0,
            0, 2 * nearVal / (top - bottom), 0, 0,

            (right + left) / (right - left),
            (top + bottom) / (top - bottom),
            -(farVal + nearVal) / (farVal - nearVal),
            -1,

            0, 0, -(2 * farVal * nearVal) / (farVal - nearVal), 0
        );

        /// <summary>
        /// Creates a matrix for a symmetric perspective-view frustum with far plane at infinite.
        /// </summary>
        /// <param name="fovy">The fovy.</param>
        /// <param name="aspect">The aspect.</param>
        /// <param name="zNear">The z near.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 InfinitePerspective(Scalar fovy, Scalar aspect, Scalar zNear)
        {
            Scalar range = Tan(fovy / 2f) * zNear;
            Scalar left = -range * aspect;
            Scalar right = range * aspect;

            return (
                2 * zNear / (right - left), 0, 0, 0,
                0, 2 * zNear / (2 * range), 0, 0,
                0, 0, -1, -1,
                0, 0, -2 * zNear, 0
            );
        }

        /// <summary>
        /// Build a look at view matrix.
        /// </summary>
        /// <param name="eye">The eye.</param>
        /// <param name="center">The center.</param>
        /// <param name="up">Up.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 LookAt(Vector3 eye, Vector3 center, Vector3 up)
        {
            Vector3 f = (center - eye).Normalized;
            Vector3 s = f.Cross(up).Normalized;
            Vector3 u = new Vector3(s.Cross(f));

            return (
                s.X, u.X, -f.X, 0,
                s.Y, u.Y, -f.Y, 0,
                s.Z, u.Z, -f.Z, 0,
                -s.Dot(eye), -u.Dot(eye), f.Dot(eye), 1
            );
        }

        /// <summary>
        /// Creates a matrix for an orthographic parallel viewing volume.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
        /// <param name="top">The top.</param>
        /// <param name="zNear">The z near.</param>
        /// <param name="zFar">The z far.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Ortho(Scalar left, Scalar right, Scalar bottom, Scalar top, Scalar zNear, Scalar zFar) =>
        (
            2 / (right - left), 0, 0, 0,
            0, 2 / (top - bottom), 0, 0,
            0, 0, -2 / (zFar - zNear), 0,

            -(right + left) / (right - left),
            -(top + bottom) / (top - bottom),
            -(zFar + zNear) / (zFar - zNear), 1
        );

        /// <summary>
        /// Creates a matrix for projecting two-dimensional coordinates onto the screen.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
        /// <param name="top">The top.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Ortho(Scalar left, Scalar right, Scalar bottom, Scalar top) => (
            2 / (right - left), 0, 0, 0,
            0, 2 / (top - bottom), 0, 0,
            0, 0, -1, 0,
            -(top + bottom) / (top - bottom), -(right + left) / (right - left), 0, 1
        );

        /// <summary>
        /// Creates a perspective transformation matrix.
        /// </summary>
        /// <param name="fovy">The field of view angle, in radians.</param>
        /// <param name="aspect">The aspect ratio.</param>
        /// <param name="zNear">The near depth clipping plane.</param>
        /// <param name="zFar">The far depth clipping plane.</param>
        /// <returns>A <see cref="Matrix4"/> that contains the projection matrix for the perspective transformation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Perspective(Scalar fovy, Scalar aspect, Scalar zNear, Scalar zFar)
        {
            Scalar tanHalfFovy = Tan(fovy / 2);

            return (
                1 / (aspect * tanHalfFovy), 0, 0, 0,
                0, 1 / tanHalfFovy, 0, 0,
                0, 0, -(zFar + zNear) / (zFar - zNear), -1,
                0, 0, -(2 * zFar * zNear) / (zFar - zNear), 0
            );
        }

        /// <summary>
        /// Builds a perspective projection matrix based on a field of view.
        /// </summary>
        /// <param name="fov">The fov (in radians).</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="zNear">The z near.</param>
        /// <param name="zFar">The z far.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PerspectiveFOV(Scalar fov, Scalar width, Scalar height, Scalar zNear, Scalar zFar)
        {
            if (width <= 0 || height <= 0 || fov <= 0)
                throw new ArgumentOutOfRangeException();

            Scalar h = Cos(fov / 2) / Sin(fov / 2);
            Scalar w = h * height / width;

            return new Matrix4(0)
                [0, 0, w]
                [1, 1, h]
                [2, 2, -(zFar + zNear) / (zFar - zNear)]
                [2, 3, -1]
                [3, 2, -(2 * zFar * zNear) / (zFar - zNear)]
            ;
        }

        /// <summary>
        /// Define a picking region.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="delta">The delta.</param>
        /// <param name="viewport">The viewport.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PickMatrix(Vector2 center, Vector2 delta, Vector4 viewport)
        {
            if (delta.X <= 0 || delta.Y <= 0)
                throw new ArgumentOutOfRangeException();

            Vector3 tmp = (
                (viewport[2] - 2 * (center.X - viewport[0])) / delta.X,
                (viewport[3] - 2 * (center.Y - viewport[1])) / delta.Y,
                0
            );

            return Identity.Translate(tmp) * new Matrix4(viewport[2] / delta.X, viewport[3] / delta.Y, 1, 1);
        }

        /// <summary>
        /// Builds a rotation 4 * 4 matrix created from an axis vector and an angle.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="v">The v.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Rotate(Matrix4 m, Scalar angle, Vector3 v)
        {
            Scalar c = Cos(angle);
            Scalar s = Sin(angle);
            Vector3 axis = v.Normalized;
            Vector3 tmp = (1 - c) * axis;
            Matrix4 rot = (
                c + tmp[0] * axis[0],
                0 + tmp[0] * axis[1] + s * axis[2],
                0 + tmp[0] * axis[2] - s * axis[1],
                0,
                0 + tmp[1] * axis[0] - s * axis[2],
                c + tmp[1] * axis[1],
                0 + tmp[1] * axis[2] + s * axis[0],
                0,
                0 + tmp[2] * axis[0] + s * axis[1],
                0 + tmp[2] * axis[1] - s * axis[0],
                c + tmp[2] * axis[2],
                0,
                0, 0, 0, 1
            );

            return new Matrix4(
                m[0] * rot[0][0] + m[1] * rot[0][1] + m[2] * rot[0][2],
                m[0] * rot[1][0] + m[1] * rot[1][1] + m[2] * rot[1][2],
                m[0] * rot[2][0] + m[1] * rot[2][1] + m[2] * rot[2][2],
                m[3]
            );
        }

        //  TODO: this is actually defined as an extension, put in the right file.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 Rotate(Scalar angle, Vector3 v) => Rotate(Identity, angle, v);

        /// <summary>
        /// Creates a matrix for a symmetric perspective-view frustum with far plane 
        /// at infinite for graphics hardware that doesn't support depth clamping.
        /// </summary>
        /// <param name="fovy">The fovy.</param>
        /// <param name="aspect">The aspect.</param>
        /// <param name="zNear">The z near.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 TweakedInfinitePerspective(Scalar fovy, Scalar aspect, Scalar zNear)
        {
            Scalar range = Tan(fovy / 2) * zNear;
            Scalar left = -range * aspect;
            Scalar right = range * aspect;
            Scalar bottom = -range;
            Scalar top = range;

            return new Matrix4(0f)
                [0, 0, 2 * zNear / (right - left)]
                [1, 1, 2 * zNear / (top - bottom)]
                [2, 2, 0.0001f - 1f]
                [2, 3, -1]
                [3, 2, -(0.0001f - 2) * zNear]
            ;
        }
    }

    public partial struct Vector2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Rotate(Scalar angle)
        {
            Scalar s = angle.Sin();
            Scalar c = angle.Cos();

            return new Vector2(X * c - Y * s, X * s + Y * c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Rotate(Vector2 center, Scalar angle) => Subtract(center).Rotate(angle).Add(center);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Line2D To(Vector2 end) => new Line2D(this, end);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Line2D AltitudeFrom(Line2D line) => line.AltitudeTo(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar DistanceTo(Line2D line) => line.DistanceTo(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 MirrorAt(Line2D axis)
        {
            Vector2 d = axis.NormalizedDirection;

            return Reflect((-d.Y, d.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex ToComplex() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly PointF ToPointF() => new PointF(_0, _1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Point ToPoint() => new Point((int)_0, (int)_1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SizeF ToSizeF() => new SizeF(_0, _1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Size ToSize() => new Size((int)_0, (int)_1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FromComplex(Complex c) => new Vector2(c.Real, c.Imaginary);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FromSize(Size sz) => new Vector2(sz.Width, sz.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FromSize(SizeF sz) => new Vector2(sz.Width, sz.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FromPoint(Point pt) => new Vector2(pt.X, pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FromPoint(PointF pt) => new Vector2(pt.X, pt.Y);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Point(Vector2 v) => v.ToPoint();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Size(Vector2 v) => v.ToSize();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PointF(Vector2 v) => v.ToPointF();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SizeF(Vector2 v) => v.ToSizeF();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(PointF p) => FromPoint(p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(SizeF s) => FromSize(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Point p) => FromPoint(p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Size s) => FromSize(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(num.Vector2 v) => new Vector2(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator num.Vector2(Vector2 v) => new num.Vector2(v.X, v.Y);
    }

    public partial struct Vector3
    {
        public readonly (Scalar α, Scalar β, Scalar γ) DirectionalAngle
        {
            get
            {
                (Scalar x, Scalar y, Scalar z) = Normalized;

                return (x.Acos(), y.Acos(), z.Acos());
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RGBAColor ToRGBAColor() => (RGBAColor)this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromDirectionalAngle(Scalar α, Scalar β, Scalar γ) => (α.Cos(), β.Cos(), γ.Cos());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromDirectionalAngle(Scalar α, Scalar β, Scalar γ, Scalar length) => FromDirectionalAngle(α, β, γ) * length;

        /// <summary>
        /// Returns a random unit vector with an uniform spherical probability distribution.
        /// </summary>
        /// <returns>Random unit vector</returns>
        public static Vector3 GetRandomUnitVector()
        {
            XorShift rng = new XorShift();
            double φ = rng.NextDouble() * PI * 2;
            double θ = Acos(rng.NextDouble() * 2 - 1);

            return (
                Sin(θ) * Cos(φ),
                Sin(θ) * Sin(φ),
                Cos(θ)
            );
        }

        /// <summary>
        /// Map the specified object coordinates (obj.x, obj.y, obj.z) into window coordinates.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="model">The model.</param>
        /// <param name="proj">The proj.</param>
        /// <param name="viewport">The viewport.</param>
        /// <returns></returns>
        public static Vector3 Project(in Vector3 obj, in Matrix4 model, in Matrix4 proj, in Vector4 viewport)
        {
            Vector4 tmp = proj * model * new Vector4(obj, 1);

            tmp /= tmp.W * 2;
            tmp += .5;

            return new Vector3(tmp.X * viewport.Z + viewport.X, tmp.Y * viewport.W + viewport.Y, tmp.Z);
        }

        /// <summary>
        /// Map the specified window coordinates (win.x, win.y, win.z) into object coordinates.
        /// </summary>
        /// <param name="win">The win.</param>
        /// <param name="model">The model.</param>
        /// <param name="proj">The proj.</param>
        /// <param name="viewport">The viewport.</param>
        /// <returns></returns>
        public static Vector3 Unproject(in Vector3 win, in Matrix4 model, in Matrix4 proj, in Vector4 viewport)
        {
            Matrix4 inv = (proj * model).Inverse;
            Vector4 tmp = new Vector4(
                (win.X - viewport[0]) / viewport[2],
                (win.Y - viewport[1]) / viewport[3],
                win.Z,
                1
            ) * 2 - 1;

            Vector4 obj = inv * tmp;

            return new Vector3(obj / obj.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(num.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator num.Vector3(Vector3 v) => new num.Vector3(v.X, v.Y, v.Z);
    }

    public partial struct Vector4
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RGBAColor ToRGBAColor() => (RGBAColor)this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4(num.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator num.Vector4(Vector4 v) => new num.Vector4(v.X, v.Y, v.Z, v.W);
    }
}
