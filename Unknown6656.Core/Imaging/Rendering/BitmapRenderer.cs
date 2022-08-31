using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Unknown6656.Controls.Console;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Rendering;


public abstract class Renderer
{
    public virtual BitmapRenderingOptions Options { get; }


    public Renderer(BitmapRenderingOptions options) => Options = options;

    public void Render(Bitmap bitmap) => Render(bitmap, Options);

    public void Render(Bitmap bitmap, BitmapRenderingOptions options_override)
    {
        (int canv_w, int canv_h) = GetOutputDimensions();
        (int img_w, int img_h) = GetOutputDimensions();
        (double sx, double sy) = options_override.Size._scale;

        if (options_override.Size._stretch)
        {
            sx = (double)canv_w / img_w;
            sy = (double)canv_h / img_h;
        }

        if (options_override.Size._contain)
            sx = sy = Math.Min((double)canv_w / img_w, (double)canv_h / img_h);

        if (options_override.Size._cover)
            sx = sy = Math.Max((double)canv_w / img_w, (double)canv_h / img_h);

        //double

        throw new NotImplementedException();
    }



    protected abstract (int width, int height) GetOutputDimensions();

    protected abstract void RenderBitmap(RGBAColor[,] colors);
}

[SupportedOSPlatform(OS.WIN)]
public unsafe class GDIWindowRenderer
    : Renderer
{
    public void* HWND { get; }


    public GDIWindowRenderer(void* hwnd, BitmapRenderingOptions options)
        : base(options) => HWND = hwnd;

    protected override (int width, int height) GetOutputDimensions()
    {
        NativeInterop.GetClientRect(HWND, out RECT rect);

        return (rect.Width, rect.Height);
    }

    protected override void RenderBitmap(RGBAColor[,] colors)
    {
        using Graphics g = Graphics.FromHwnd((nint)HWND);


        throw new NotImplementedException();

    }

    public static GDIWindowRenderer FromConsoleWindow(BitmapRenderingOptions options) => new(NativeInterop.GetConsoleWindow(), options);
}



/*
colors:
    -grayscale
    -original
    -palette(...)

dithering:
    -none
    -ordered(...)
    -unordered(...)
*/

public sealed record BitmapRenderingOptions
{
    public BitmapPosition Position { get; init; } = BitmapPosition.Center;
    public BitmapSize Size { get; init; } = BitmapSize.Contain;
    public BitmapColors Colors { get; init; } = BitmapColors.Original;
    public BitmapDithering Dithering { get; init; }
}

public sealed record BitmapDithering
{

}

public sealed record BitmapColors
{
    public static BitmapColors Original { get; } = new() { _original = true };


    internal bool _original;

    public RGBAColor BackgroundColor { get; set; } = RGBAColor.Transparent;


}

public sealed record BitmapPosition(HorizontalAlignment HorizontalAlignment, double HorizontalOffset, VerticalAlignment VerticalAlignment, double VerticalOffset)
{
    public static BitmapPosition Center { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Center);

    public static BitmapPosition CenterLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Center);

    public static BitmapPosition CenterRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Center);

    public static BitmapPosition CenterTop { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Top);

    public static BitmapPosition CenterBottom { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Bottom);

    public static BitmapPosition BottomLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Bottom);

    public static BitmapPosition BottomRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Bottom);

    public static BitmapPosition TopLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Top);

    public static BitmapPosition TopRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Top);


    public BitmapPosition(HorizontalAlignment HorizontalAlignment, VerticalAlignment VerticalAlignment)
        : this(HorizontalAlignment, 0, VerticalAlignment, 0)
    {
    }

    public BitmapPosition(HorizontalAlignment HorizontalAlignment, VerticalAlignment VerticalAlignment, Vector2 Offset)
        : this(HorizontalAlignment, Offset.X, VerticalAlignment, Offset.Y)
    {
    }
}

public sealed record BitmapSize
{
    public static BitmapSize Cover { get; } = new() { _cover = true };

    public static BitmapSize Contain { get; } = new() { _contain = true };

    public static BitmapSize Original { get; } = Uniform(1);

    public static BitmapSize Stretch { get; } = new() { _stretch = true };


    internal bool _cover;
    internal bool _contain;
    internal bool _stretch;
    internal Vector2 _scale;

    public static BitmapSize Uniform(double scale) => Anisotropic(scale, scale);

    public static BitmapSize Anisotropic(double horizontal_scale, double vertical_scale) => new() { _scale = (horizontal_scale, vertical_scale) };
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right,
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom,
}
