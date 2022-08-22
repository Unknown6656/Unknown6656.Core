using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Unknown6656.Controls.Console;

using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Rendering;


public abstract class Renderer
{
    public virtual BitmapRenderingOptions Options { get; }


    public Renderer(BitmapRenderingOptions options) => Options = options;

    public void Render(Bitmap bitmap) => Render(bitmap, Options);

    public void Render(Bitmap bitmap, BitmapRenderingOptions options_override)
    {
        (int width, int height) = GetOutputDimensions();





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

size:
    -conver
    -contain
    -original
    -custom(...)

colors:
    -grayscale
    -original
    -palette(...)

dithering:
    -none
    -ordered(...)
    -unordered(...)

*/

public record BitmapRenderingOptions(BitmapPosition Position, BitmapSize Size, BitmapColors Colors);

public record BitmapPosition(HorizontalPosition HorizontalPosition, VerticalPosition VerticalPosition);

public record BitmapSize
{
    public static BitmapSize Cover { get; } => new() { _cover = true };

    public static BitmapSize Contain { get; } => new() { _contain = true };


    private bool _cover;
    private bool _contain;
    private double _scale;
}

public enum HorizontalPosition
{
    Left,
    Center,
    Right,
}

public enum VerticalPosition
{
    Top,
    Center,
    Bottom,
}
