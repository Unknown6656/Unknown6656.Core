using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Text;
using System;

namespace MathLibrary;

//☭
// ␀␁␂␃␄␅␆␇␈␉␊␋␌␍␎␏␐␑␒␓␔␕␖␗␘␙␚␛␜␝␞␟
// 

public struct Vector3D : IEnumerable<double>, IDisposable
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }


    public static bool IsParallel(Vector3D v1, Vector3D v2)
    {
        double ΔX = v1.X / v2.X;
        double ΔY = v1.Y / v2.Y;
        double ΔZ = v1.Z / v2.Z;

        return (ΔX == ΔY) && (ΔX == ΔZ);
    }

    public static bool operator true(Vector3D v1)
    {
        return v1.Length != 0.0f;
    }

    public static bool operator false(Vector3D v1)
    {
        return v1.Length == 0.0f;
    }

    #endregion
}
