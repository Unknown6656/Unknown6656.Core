using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Geometry;
using Unknown6656.Generics;
using Unknown6656.Imaging;

using static System.Math;

namespace Unknown6656.Mathematics.LinearAlgebra;


public partial struct Matrix2
{
    /// <summary>
    /// Returns a rotation 2x2 matrix which describes the rotation around the origin by the given angle.
    /// </summary>
    /// <param name="angle">Angle in radians</param>
    /// <returns>Rotation matrix</returns>
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

    public static Matrix3 CreateRotationXYZ(Scalar euler_x, Scalar euler_y, Scalar euler_z)
    {
        Scalar sx = euler_x.Sin();
        Scalar cx = euler_x.Cos();
        Scalar sy = euler_y.Sin();
        Scalar cy = euler_y.Cos();
        Scalar sz = euler_z.Sin();
        Scalar cz = euler_z.Cos();

        return new Matrix3(
            cy* cz, sx * sy * cz - cx * sz, cx* sy * cz + sx * sz,
            cx* sz, sx * sy * sz + cx * cz, cx* sy * sz - sy * cz,
            sy,     sx* cy,                 cx * cy
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
    public readonly Matrix4 Translate(Vector3 v)
    {
        Matrix4 result = this;
        Vector4 t = this[0] * v[0] + this[1] * v[1] + this[2] * v[2] + this[3];

        return result[1, t];
    }

    public static Matrix4 Rotate(Vector3 axis, Scalar angle) => CreateRotation(Identity, axis, angle);

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
    public static Matrix4 CreateFrustum(Scalar left, Scalar right, Scalar bottom, Scalar top, Scalar nearVal, Scalar farVal) => (
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
    public static Matrix4 CreateInfinitePerspective(Scalar fovy, Scalar aspect, Scalar zNear)
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
    /// Creates a look-at view matrix.
    /// </summary>
    /// <param name="eye">The eye.</param>
    /// <param name="center">The center.</param>
    /// <param name="up">Up.</param>
    /// <returns></returns>
    public static Matrix4 CreateLookAt(Vector3 eye, Vector3 center, Vector3 up)
    {
        Vector3 f = (center - eye).Normalized;
        Vector3 s = f.Cross(up).Normalized;
        Vector3 u = new(s.Cross(f));

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
    public static Matrix4 CreateOrtho(Scalar left, Scalar right, Scalar bottom, Scalar top, Scalar zNear, Scalar zFar) =>
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
    public static Matrix4 CreateOrtho(Scalar left, Scalar right, Scalar bottom, Scalar top) => (
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
    public static Matrix4 CreatePerspective(Scalar fovy, Scalar aspect, Scalar zNear, Scalar zFar)
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
    public static Matrix4 CreatePerspectiveFOV(Scalar fov, Scalar width, Scalar height, Scalar zNear, Scalar zFar)
    {
        if (width <= 0 || height <= 0 || fov <= 0)
            throw new ArgumentOutOfRangeException();

        Scalar h = (fov * .5).Cot();
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
    /// Creates a matrix for a symmetric perspective-view frustum with far plane 
    /// at infinite for graphics hardware that doesn't support depth clamping.
    /// </summary>
    /// <param name="fovy">The fovy.</param>
    /// <param name="aspect">The aspect.</param>
    /// <param name="zNear">The z near.</param>
    /// <returns></returns>
    public static Matrix4 CreateTweakedInfinitePerspective(Scalar fovy, Scalar aspect, Scalar zNear)
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

    /// <summary>
    /// Builds a rotation 4x4 matrix created from an axis vector and an angle.
    /// </summary>
    /// <param name="matrix">The matrix to be rotated</param>
    /// <param name="angle">The rotation angle (in radian).</param>
    /// <param name="axis">The rotation axis.</param>
    /// <returns></returns>
    public static Matrix4 CreateRotation(Matrix4 matrix, Vector3 axis, Scalar angle)
    {
        axis = axis.Normalized;

        Scalar c = angle.Cos();
        Scalar s = angle.Sin();
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
            matrix[0] * rot[0][0] + matrix[1] * rot[0][1] + matrix[2] * rot[0][2],
            matrix[0] * rot[1][0] + matrix[1] * rot[1][1] + matrix[2] * rot[1][2],
            matrix[0] * rot[2][0] + matrix[1] * rot[2][1] + matrix[2] * rot[2][2],
            matrix[3]
        );
    }

    public static Matrix4 CreateRotation(Vector3 axis, Scalar angle) => CreateRotation(Identity, axis, angle);

    public static Matrix4 CreateRotation(Scalar euler_x, Scalar euler_y, Scalar euler_z) =>
        Matrix3.CreateRotationXYZ(euler_x, euler_y, euler_z).ToHomogeneousTransformationMatrix();

    public static Matrix4 CreateRotation((Scalar X, Scalar Y, Scalar Z) euler_angles) => CreateRotation(euler_angles.X, euler_angles.Y, euler_angles.Z);

    public static Matrix4 CreateTranslation(Vector3 translation) => Identity.Translate(translation);
}

public partial struct Vector2
{
    private static string _PLUSCODE_CHARS = "23456789CFGHJMPQRVWX";
    private static readonly double[] _PLUSCODE_LEVELS =
    [
        20,
        1,
        .05,
        .0025,
        .000125,
        .00003125,
        .00000625,
    ];
    private static readonly Regex _REGEX_PLUSCODE = new(
        @$"^(([{_PLUSCODE_CHARS}]{{2}}){{1,6}}|[{_PLUSCODE_CHARS}]?([{_PLUSCODE_CHARS}]{{2}}){{1,4}}\+[{_PLUSCODE_CHARS}]{{2}}[{_PLUSCODE_CHARS}]?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );


    public readonly Scalar Angle => Scalar.Atan2(Y, X);

    /// <summary>
    /// Tries to convert the current vector interpreted as [lat, lon] coordinates into the corresponding plus-code (as used by Google Maps) using the given precision level.
    /// The coordinates are interpreted as WGS84 coordinates in degrees.
    /// </summary>
    /// <param name="precision"></param>
    /// <returns>The generated plus-code.</returns>
    public readonly string ToPlusCode(PlusCodePreicsionLevel precision = PlusCodePreicsionLevel.Highest)
    {
        if (!Enum.IsDefined(precision))
            throw new ArgumentOutOfRangeException(nameof(precision));

        (Scalar lat, Scalar lon) = this;
        char[] chars = new char[2 * (int)precision];
        Scalar adjust(Scalar s, Scalar wrap) => (s + wrap) % wrap + (wrap / 2);

        lat = adjust(lat, 180);
        lon = adjust(lon, 360);

        for (int level = 0; level < (int)precision; ++level)
        {
            int f_lat = (int)(lat / _PLUSCODE_LEVELS[level]);
            int f_lon = (int)(lon / _PLUSCODE_LEVELS[level]);

            if (f_lat >= _PLUSCODE_CHARS.Length)
                throw new InvalidOperationException($"The latitue value of {X}° cannot be serialized to a plus-code due to an arithmethic overflow on precision level {level}.");
            else if (f_lon >= _PLUSCODE_CHARS.Length)
                throw new InvalidOperationException($"The longitude value of {Y}° cannot be serialized to a plus-code due to an arithmethic overflow on precision level {level}.");

            chars[2 * level] = _PLUSCODE_CHARS[f_lat];
            chars[2 * level + 1] = _PLUSCODE_CHARS[f_lon];
            lat -= f_lat * _PLUSCODE_LEVELS[level];
            lon -= f_lon * _PLUSCODE_LEVELS[level];
        }

        return new(chars);
    }

    public static bool TryFromPlusCode(string plus_code, out Vector2 coordinates)
    {
        plus_code = plus_code.Trim();
        coordinates = default;

        if (_REGEX_PLUSCODE.Match(plus_code) is { Success: true, Value: string match })
        {
            if (match.IndexOf('+') is int split and >= 0)
            {
                string pre = match[..split];
                string post = match[(split + 1)..];

                if ((pre.Length % 2) == 1)
                    pre += pre[^1];

                if ((post.Length % 2) == 1)
                    post += post[^1];

                match = pre + post;
            }

            double lat = -90, lon = -180;

            foreach ((char[] chunk, int level) in match.Chunk(2).WithIndex())
            {
                lat += _PLUSCODE_CHARS.IndexOf(chunk[0]) * _PLUSCODE_LEVELS[level];
                lon += _PLUSCODE_CHARS.IndexOf(chunk[1]) * _PLUSCODE_LEVELS[level];
            }

            coordinates = (lat, lon);

            return true;
        }
        else
            return false;
    }

    public readonly Vector2 Rotate(Scalar angle)
    {
        Scalar s = angle.Sin();
        Scalar c = angle.Cos();

        return new Vector2(X * c - Y * s, X * s + Y * c);
    }

    public readonly Vector2 Rotate(Vector2 center, Scalar angle) => Subtract(center).Rotate(angle).Add(center);

    public readonly Line2D To(Vector2 end) => new(this, end);

    public readonly Line2D AltitudeFrom(Line2D line) => line.AltitudeTo(this);

    public readonly Scalar DistanceTo(Line2D line) => line.DistanceTo(this);

    public readonly Vector2 MirrorAt(Line2D axis)
    {
        Vector2 d = axis.NormalizedDirection;

        return Reflect((-d.Y, d.X));
    }

    public readonly Complex ToComplex() => this;

    public readonly PointF ToPointF() => new(_0, _1);

    public readonly Point ToPoint() => new((int)_0, (int)_1);

    public readonly SizeF ToSizeF() => new(_0, _1);

    public readonly Size ToSize() => new((int)_0, (int)_1);

    public readonly Vector2 ToPolar() => (Angle, Length);

    public static Vector2 FromPolar(Vector2 polar) => FromPolar(polar.X, polar.Y);

    public static Vector2 FromPolar(Scalar θ, Scalar r) => new(θ.Cos() * r, θ.Sin() * r);

    public static Vector2 FromComplex(Complex c) => new(c.Real, c.Imaginary);

    public static Vector2 FromSize(Size sz) => new(sz.Width, sz.Height);

    public static Vector2 FromSize(SizeF sz) => new(sz.Width, sz.Height);

    public static Vector2 FromPoint(Point pt) => new(pt.X, pt.Y);

    public static Vector2 FromPoint(PointF pt) => new(pt.X, pt.Y);


    public static explicit operator Point(Vector2 v) => v.ToPoint();

    public static explicit operator Size(Vector2 v) => v.ToSize();

    public static implicit operator PointF(Vector2 v) => v.ToPointF();

    public static implicit operator SizeF(Vector2 v) => v.ToSizeF();

    public static implicit operator Vector2(PointF p) => FromPoint(p);

    public static implicit operator Vector2(SizeF s) => FromSize(s);

    public static implicit operator Vector2(Point p) => FromPoint(p);

    public static implicit operator Vector2(Size s) => FromSize(s);

    public static implicit operator Vector2(num.Vector2 v) => new(v.X, v.Y);

    public static implicit operator num.Vector2(Vector2 v) => new(v.X, v.Y);
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


    public readonly RGBAColor ToRGBAColor() => (RGBAColor)this;

    public static Vector3 FromDirectionalAngle(Scalar α, Scalar β, Scalar γ) => new(α.Cos(), β.Cos(), γ.Cos());

    public static Vector3 FromDirectionalAngle(Scalar α, Scalar β, Scalar γ, Scalar length) => FromDirectionalAngle(α, β, γ) * length;

    /// <summary>
    /// Returns a random unit vector with an uniform spherical probability distribution.
    /// </summary>
    /// <returns>Random unit vector</returns>
    public static Vector3 GetRandomUnitVector()
    {
        XorShift rng = new();
        double φ = rng.NextDouble() * PI * 2;
        double θ = Acos(rng.NextDouble() * 2 - 1);

        return new(
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

        return new(tmp.X * viewport.Z + viewport.X, tmp.Y * viewport.W + viewport.Y, tmp.Z);
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

    public static implicit operator Vector3(num.Vector3 v) => new(v.X, v.Y, v.Z);

    public static implicit operator num.Vector3(Vector3 v) => new(v.X, v.Y, v.Z);
}

public partial struct Vector4
{
    public readonly RGBAColor ToRGBAColor() => (RGBAColor)this;

    public static implicit operator Vector4(num.Vector4 v) => new(v.X, v.Y, v.Z, v.W);

    public static implicit operator num.Vector4(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}

public enum PlusCodePreicsionLevel
    : int
{
    Lowest = 1,
    Low = 2,
    MidLow = 3,
    MidHigh = 4,
    High = 5,
    Highest = 6,
}
