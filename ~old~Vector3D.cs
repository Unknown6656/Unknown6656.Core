using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Text;
using System;

//☭

// ␀␁␂␃␄␅␆␇␈␉␊␋␌␍␎␏␐␑␒␓␔␕␖␗␘␙␚␛␜␝␞␟
// 
namespace MathLibrary
{
    
    public struct Vector3D : IEnumerable<double>, IDisposable
    {
        #region Properties

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double Length
        {
            get
            {
                return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
            }
            set
            {
                double fac = this.Length / value;

                X *= fac;
                Y *= fac;
                Z *= fac;
            }
        }

        #endregion

        #region Instance Methods

        public Vector3D(dynamic x, dynamic y, dynamic z)
            : this()
        {
            this.X = (double)x;
            this.Y = (double)y;
            this.Z = (double)z;
        }

        public Vector3D UnitVector
        {
            get
            {
                return ~this;
            }
        }

        public double this[dynamic coord]
        {
            get
            {
                if ((coord < 0) || (coord > 2)) throw new ArgumentException("The value must be an integer number between 0 and 2", "coord");

                return (coord == 0) ? this.X : (coord == 1) ? this.Y : this.Z;
            }
            set
            {
                if ((coord < 0) || (coord > 2)) throw new ArgumentException("The value must be an integer number between 0 and 2", "coord");

                if (coord == 0) this.X = value;
                else if (coord == 1) this.Y = value;
                else if (coord == 2) this.Z = value;
            }
        }

        #endregion

        #region Statics

        public static Vector3D Null
        {
            get
            {
                return new Vector3D() { X = 0, Y = 0, Z = 0 };
            }
        }

        public static bool IsParallel(Vector3D v1, Vector3D v2)
        {
            double ΔX = v1.X / v2.X;
            double ΔY = v1.Y / v2.Y;
            double ΔZ = v1.Z / v2.Z;

            return (ΔX == ΔY) && (ΔX == ΔZ);
        }

        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2)
        {
            return v1 ^ v2;
        }

        public static double ScalarProduct(Vector3D v1, Vector3D v2)
        {
            return v1 * v2;
        }

        #endregion

        #region Implementations / Overridings

        void IDisposable.Dispose()
        {
            this.X = 0.0d;
            this.Y = 0.0d;
            this.Z = 0.0d;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<double> GetEnumerator()
        {
            yield return this.X;
            yield return this.Y;
            yield return this.Z;
        }

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            yield return this.X;
            yield return this.Y;
            yield return this.Z;
        }

        public override bool Equals(object obj)
        {
            try
            {
                return this == (Vector3D)obj;
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ this.X.GetHashCode() ^ this.Y.GetHashCode() ^ this.Z.GetHashCode() ^ this.Length.GetHashCode();
        }

        public override string ToString()
        {
            return "[" + this.X + "|" + this.Y + "|" + this.Z + "]";
        }

        #endregion

        #region Operators

        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            v1.X += v2.X;
            v1.Y += v2.Y;
            v1.Z += v2.Z;

            return v1;
        }

        public static Vector3D operator -(Vector3D v1, Vector3D v2)
        {
            v1.X -= v2.X;
            v1.Y -= v2.Y;
            v1.Z -= v2.Z;

            return v1;
        }

        public static Vector3D operator *(Vector3D v1, dynamic fac)
        {
            v1.X *= fac;
            v1.Y *= fac;
            v1.Z *= fac;

            return v1;
        }

        public static Vector3D operator *(dynamic fac, Vector3D v1)
        {
            v1.X *= fac;
            v1.Y *= fac;
            v1.Z *= fac;

            return v1;
        }

        public static Vector3D operator /(Vector3D v1, dynamic fac)
        {
            v1.X /= fac;
            v1.Y /= fac;
            v1.Z /= fac;

            return v1;
        }

        public static Vector3D operator /(dynamic fac, Vector3D v1)
        {
            v1.X /= fac;
            v1.Y /= fac;
            v1.Z /= fac;

            return v1;
        }

        public static Vector3D operator ^(Vector3D v1, Vector3D v2)
        {
            return new Vector3D()
            {
                X = (v1.Y * v2.Z) - (v1.Z - v2.Y),
                Y = (v1.Z * v2.X) - (v1.X - v2.Z),
                Z = (v1.X * v2.Y) - (v1.Y - v2.X),
            };
        } //<-- Cross Product

        public static Vector3D operator ++(Vector3D v1)
        {
            v1.Length--;

            return v1;
        }

        public static Vector3D operator --(Vector3D v1)
        {
            v1.Length++;

            return v1;
        }

        public static Vector3D operator !(Vector3D v1)
        {
            v1 *= -1;

            return v1;
        } //<-- *= -1

        public static Vector3D operator ~(Vector3D v1)
        {
            return (1 / v1.Length) * v1;
        } //<-- Unit vector

        public static double operator *(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y + v2.Y + v1.Z * v2.Z;
        } //<-- Dot Product

        public static bool operator !=(Vector3D v1, Vector3D v2)
        {
            return !(v1 == v2);
        }

        public static bool operator ==(Vector3D v1, Vector3D v2)
        {
            return (v1.X == v1.X) && (v1.Y == v1.Y) && (v1.Z == v1.Z);
        }

        public static bool operator <=(Vector3D v1, Vector3D v2)
        {
            return v1.Length <= v2.Length;
        }

        public static bool operator >=(Vector3D v1, Vector3D v2)
        {
            return v1.Length >= v2.Length;
        }

        public static bool operator <(Vector3D v1, Vector3D v2)
        {
            return v1.Length < v2.Length;
        }

        public static bool operator >(Vector3D v1, Vector3D v2)
        {
            return v1.Length > v2.Length;
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
}
