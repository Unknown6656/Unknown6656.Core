using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Unknown6656.Imaging;


[StructLayout(LayoutKind.Sequential), NativeCppClass]
public record struct RECT(int Left, int Top, int Right, int Bottom)
{
    public int X
    {
        get => Left;
        set
        {
            Right -= Left - value;
            Left = value;
        }
    }

    public int Y
    {
        get => Top;
        set
        {
            Bottom -= Top - value;
            Top = value;
        }
    }

    public int Height
    {
        get => Bottom - Top;
        set => Bottom = value + Top;
    }

    public int Width
    {
        get => Right - Left;
        set => Right = value + Left;
    }

    public Point Location
    {
        get => new(Left, Top);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public Size Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }


    public RECT(Rectangle r)
        : this(r.Left, r.Top, r.Right, r.Bottom)
    {
    }

    public RECT(Point location, Size size)
        : this(new Rectangle(location, size))
    {
    }

    public override string ToString() => $"{{L={Left}, T={Top}, R={Right}, B={Bottom}, W={Width}, H={Height}}}";

    public static implicit operator Rectangle(RECT r) => new(r.Left, r.Top, r.Width, r.Height);

    public static implicit operator RECT(Rectangle r) => new(r);
}
