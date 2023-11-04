using System.Runtime.Versioning;
using System.Drawing.Drawing2D;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging.Effects;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Rendering;


[SupportedOSPlatform(OS.WIN)]
public abstract class Renderer
{
    public virtual RenderingOptions Options { get; }


    public Renderer(RenderingOptions options) => Options = options;

    public void Render(Bitmap bitmap) => Render(bitmap, Options);

    public virtual void Render(Bitmap bitmap, RenderingOptions options_override)
    {
        (int canv_w, int canv_h) = GetOutputDimensions();
        (int src_w, int src_h) = (bitmap.Width, bitmap.Height);
        (float sx, float sy) = options_override.Size._scale;

        if (options_override.Size._stretch)
        {
            sx = (float)canv_w / src_w;
            sy = (float)canv_h / src_h;
        }

        if (options_override.Size._contain)
            sx = sy = Math.Min((float)canv_w / src_w, (float)canv_h / src_h);

        if (options_override.Size._cover)
            sx = sy = Math.Max((float)canv_w / src_w, (float)canv_h / src_h);

        float img_w = src_w * sx;
        float img_h = src_h * sy;
        float px = options_override.Position.HorizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => .5f,
            HorizontalAlignment.Right => 1,
        } * (canv_w - img_w) - options_override.Position.HorizontalOffset;
        float py = options_override.Position.VerticalAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => .5f,
            VerticalAlignment.Bottom => 1,
        } * (canv_h - img_h) - options_override.Position.VerticalOffset;

        Bitmap canvas = new(canv_w, canv_h);
        using Graphics g = Graphics.FromImage(canvas);

        g.CompositingMode = CompositingMode.SourceOver;
        (g.InterpolationMode, g.SmoothingMode, g.CompositingQuality) = options_override.Interpolation switch
        {
            BitmapInterpolation.Bicubic => (InterpolationMode.HighQualityBicubic, SmoothingMode.AntiAlias, CompositingQuality.HighQuality),
            BitmapInterpolation.Bilinear => (InterpolationMode.HighQualityBilinear, SmoothingMode.HighQuality, CompositingQuality.AssumeLinear),
            BitmapInterpolation.NearestNeighbor => (InterpolationMode.NearestNeighbor, SmoothingMode.HighSpeed, CompositingQuality.HighSpeed),
        };
        g.Clear(options_override.Colors.BackgroundColor);
        g.DrawImage(bitmap, px, py, img_w, img_h);

        if (options_override.Colors._effect is { } fx)
            canvas = fx.ApplyTo(canvas);

        RGBAColor[,] colors = canvas.ToPixelArray2D();

        canvas.Dispose();

        RenderBitmap(colors, options_override);
    }

    protected abstract (int width, int height) GetOutputDimensions();

    protected abstract void RenderBitmap(RGBAColor[,] colors, RenderingOptions options_override);
}

[SupportedOSPlatform(OS.WIN)]
public unsafe class GDIWindowRenderer
    : Renderer
{
    public void* HWND { get; }


    public GDIWindowRenderer(void* hwnd, RenderingOptions options)
        : base(options) => HWND = hwnd;

    protected override (int width, int height) GetOutputDimensions()
    {
        NativeInterop.GetClientRect(HWND, out RECT rect);

        return (rect.Width, rect.Height);
    }

    protected override void RenderBitmap(RGBAColor[,] colors, RenderingOptions options_override)
    {
        using Bitmap bmp = BitmapExtensions.FromPixelArray(colors);
        using Graphics g = Graphics.FromHwnd((nint)HWND);

        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.HighSpeed;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.SmoothingMode = SmoothingMode.HighSpeed;
        g.PixelOffsetMode = PixelOffsetMode.None;
        g.DrawImageUnscaled(bmp, 0, 0);
    }

    public static GDIWindowRenderer FromHwnd(nint hwnd, RenderingOptions options) => new((void*)hwnd, options);

    public static GDIWindowRenderer FromHwnd(void* hwnd, RenderingOptions options) => new(hwnd, options);

    public static GDIWindowRenderer FromConsoleWindow(RenderingOptions options) => new(NativeInterop.GetConsoleWindow(), options);
}

public record RenderingOptions
{
    public PositionOptions Position { get; init; } = PositionOptions.Center;
    public SizeOptions Size { get; init; } = SizeOptions.Contain;
    public ColorOptions Colors { get; init; } = ColorOptions.OriginalColors;
    public BitmapInterpolation Interpolation { get; init; } = BitmapInterpolation.Bilinear;
}

public class ColorOptions
{
    public static ColorOptions OriginalColors => new(null);
    public static ColorOptions BlackAndWhite => FromPalette(ColorPalette.BlackAndWhite);
    public static ColorOptions Grayscale => FromPalette(ColorPalette.Grayscale[256]);

    public RGBAColor BackgroundColor { get; set; } = RGBAColor.Transparent;

    internal readonly BitmapEffect? _effect;


    private ColorOptions(BitmapEffect? effect) => _effect = effect;

    public static ColorOptions FromPalette(ColorPalette palette) => new(new ReduceColorSpace(palette));

    public static ColorOptions Dithered(ErrorDiffusionDitheringAlgorithm algorithm, ColorPalette palette) =>
        new(new ErrorDiffusionDithering(algorithm, palette));

    public static ColorOptions Dithered(OrderedDitheringAlgorithm algorithm) => new(new BlackWhiteOrderedDithering(algorithm));

    public static ColorOptions Dithered(OrderedDitheringAlgorithm algorithm, int color_steps) => Dithered(algorithm, color_steps, color_steps, color_steps);

    public static ColorOptions Dithered(OrderedDitheringAlgorithm algorithm, int red_color_steps, int green_color_steps, int blue_color_steps) =>
        new(new ColoredOrderedDithering(algorithm, red_color_steps, green_color_steps, blue_color_steps));
}

public record PositionOptions(HorizontalAlignment HorizontalAlignment, float HorizontalOffset, VerticalAlignment VerticalAlignment, float VerticalOffset)
{
    public static PositionOptions Center { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Center);

    public static PositionOptions CenterLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Center);

    public static PositionOptions CenterRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Center);

    public static PositionOptions CenterTop { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Top);

    public static PositionOptions CenterBottom { get; } = new(HorizontalAlignment.Center, VerticalAlignment.Bottom);

    public static PositionOptions BottomLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Bottom);

    public static PositionOptions BottomRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Bottom);

    public static PositionOptions TopLeft { get; } = new(HorizontalAlignment.Left, VerticalAlignment.Top);

    public static PositionOptions TopRight { get; } = new(HorizontalAlignment.Right, VerticalAlignment.Top);


    public PositionOptions(HorizontalAlignment HorizontalAlignment, VerticalAlignment VerticalAlignment)
        : this(HorizontalAlignment, 0, VerticalAlignment, 0)
    {
    }

    public PositionOptions(HorizontalAlignment HorizontalAlignment, VerticalAlignment VerticalAlignment, Vector2 Offset)
        : this(HorizontalAlignment, Offset.X, VerticalAlignment, Offset.Y)
    {
    }
}

public class SizeOptions
{
    public static SizeOptions Cover { get; } = new() { _cover = true };

    public static SizeOptions Contain { get; } = new() { _contain = true };

    public static SizeOptions Original { get; } = Uniform(1);

    public static SizeOptions Stretch { get; } = new() { _stretch = true };


    internal bool _cover;
    internal bool _contain;
    internal bool _stretch;
    internal Vector2 _scale;

    private SizeOptions()
    {
    }

    public static SizeOptions Uniform(double scale) => Anisotropic(scale, scale);

    public static SizeOptions Anisotropic(double horizontal_scale, double vertical_scale) => new() { _scale = (horizontal_scale, vertical_scale) };
}

public enum BitmapInterpolation
{
    Bilinear,
    Bicubic,
    NearestNeighbor,
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
