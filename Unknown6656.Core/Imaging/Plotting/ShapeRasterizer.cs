using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging.Effects;
using Unknown6656.Generics;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Plotting;


public sealed class ShapeDrawingSettings
{
    public RGBAColor LineColor { set; get; } = RGBAColor.Red;
    public RGBAColor FillColor { set; get; } = RGBAColor.Salmon;
    public Scalar LineThickness { set; get; } = Scalar.One;
}

public abstract class DrawableShape
{
    public void DrawTo(Bitmap bitmap, RGBAColor color) => DrawTo(bitmap, new ShapeDrawingSettings
    {
        FillColor = color,
        LineColor = color,
        LineThickness = 0,
    });

    public void DrawTo(Bitmap bitmap, ShapeDrawingSettings settings) => DrawTo(new Shape2DRasterizer(bitmap), settings);

    public void DrawTo(Shape2DRasterizer rasterizer, ShapeDrawingSettings settings) => rasterizer.Draw(this, settings);

    internal protected abstract void internal_draw(RenderPass pass, RenderPassDrawMode mode);
}

public sealed class Shape2DRasterizer
{
    private readonly Graphics _graphics;
    private readonly Bitmap? _bitmap;

    public int CanvasWidth { get; }
    public int CanvasHeight { get; }
    public Scalar ZoomFactor { get; private set; } = Scalar.One;
    public Scalar GlobalRotation { get; private set; }
    public Vector2 GlobalTransalation { get; private set; }
    public Matrix3 CoordinateSpace { get; }
    public bool Isometric { get; set; } = true;


    public Shape2DRasterizer(Bitmap existing)
        : this(Graphics.FromImage(existing), existing.Width, existing.Height) => _bitmap = existing;

    public Shape2DRasterizer(Graphics graphics, int width, int height)
    {
        _graphics = graphics;
        _graphics.CompositingMode = CompositingMode.SourceOver;
        CanvasWidth = width;
        CanvasHeight = height;

        Scalar w = .5 * width;
        Scalar h = .5 * height;

        CoordinateSpace = (
            w, 0, w,
            0, h, h,
            0, 0, 1
        );
    }

    public void Draw(DrawableShape shape, ShapeDrawingSettings settings) => Draw(new[] { shape }, settings);

    public void Draw(IDictionary<DrawableShape, ShapeDrawingSettings> shapes) => Draw(shapes.Select(kvp => (kvp.Key, kvp.Value)));

    public void Draw(IEnumerable<(DrawableShape shape, ShapeDrawingSettings settings)> shapes)
    {
        foreach ((DrawableShape shape, ShapeDrawingSettings settings) in shapes)
            Draw(shape, settings);
    }

    public void Draw(IEnumerable<DrawableShape?> shapes, ShapeDrawingSettings settings) => CreatePass(settings, pass =>
    {
        foreach (DrawableShape? s in shapes)
            if (s is { })
                s.internal_draw(pass, RenderPassDrawMode.Additive);
    });

    private unsafe void CreatePass(ShapeDrawingSettings settings, Action<RenderPass> callback)
    {
        RGBAColor line_clr = settings.LineColor;
        RGBAColor fill_clr = settings.FillColor;
        using RenderPass pass = new(this, settings.LineThickness);

        callback(pass);

        using Bitmap bmp = pass.Bitmap;
        using Bitmap contours = bmp.ApplyEffect(new AcceleratedChainedPartialBitmapEffect(
            new EdgeDetection(),
            // new BoxBlur(),
            new ColorEffect.Delegated(c => new RGBAColor
            {
                Af = line_clr.Af * ((Vector3)c).CoefficientMax, // TODO : radius thresholding
                Rf = line_clr.Rf,
                Gf = line_clr.Gf,
                Bf = line_clr.Bf,
            })
        ));
        using Bitmap fill = bmp.ApplyEffect(new ColorEffect.Delegated(col =>
        {
            Scalar add = 1 - col.EucledianRGBDistanceTo(RenderPass.COLOR_ADD).Clamp();
            Scalar sub = 1 - col.EucledianRGBDistanceTo(RenderPass.COLOR_SUB).Clamp();

            return new RGBAColor
            {
                Af = fill_clr.Af * add / (add + sub),
                Rf = fill_clr.Rf,
                Gf = fill_clr.Gf,
                Bf = fill_clr.Bf,
            };
        }));

        _graphics.DrawImage(fill, 0, 0);
        _graphics.DrawImage(contours, 0, 0);
    }

    public void SetZoom(Scalar factor)
    {
        if (factor.IsFinite && factor > Scalar.ComputationalEpsilon)
            ZoomFactor = factor;
    }

    public void SetGlobalRotation(Scalar angle)
    {
        if (angle.IsFinite)
            GlobalRotation = angle.Modulus(Scalar.Tau);
    }

    public void SetGlobalTranslation(Vector2 offset) => GlobalTransalation = offset;

    public Bitmap Render()
    {
        if (_bitmap is { })
            return _bitmap;

        nint hdc = _graphics.GetHdc();
        nint hbitmap = NativeInterop.IntGetCurrentObject(new HandleRef(null, hdc), 7 /*OBJ_BITMAP*/);
        Bitmap bitmap = Image.FromHbitmap(hbitmap);

        _graphics.ReleaseHdc(hdc);

        return bitmap;
    }
}

public sealed class RenderPass
    : IDisposable
{
    internal static readonly RGBAColor COLOR_ADD = (1d, 1d, 1d);
    internal static readonly RGBAColor COLOR_SUB = (0d, 0d, 0d);

    private readonly Graphics _graphics;
    private readonly SolidBrush _brush_add;
    private readonly SolidBrush _brush_sub;
    private readonly Matrix3 _matrix;

    public Shape2DRasterizer Rasterizer { get; }
    public Bitmap Bitmap { get; }


    // TODO : use line thickness
    internal RenderPass(Shape2DRasterizer rasterizer, Scalar line_thickness)
    {
        Rasterizer = rasterizer;
        Bitmap = new Bitmap(rasterizer.CanvasWidth, rasterizer.CanvasHeight);
        _brush_add = new SolidBrush(COLOR_ADD);
        _brush_sub = new SolidBrush(COLOR_SUB);
        _graphics = Graphics.FromImage(Bitmap);
        _graphics.SmoothingMode = SmoothingMode.AntiAlias;
        _graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        _graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        _graphics.CompositingMode = CompositingMode.SourceOver;
        _graphics.CompositingQuality = CompositingQuality.HighQuality;
        _graphics.Clear(COLOR_SUB);

        _matrix = Rasterizer.CoordinateSpace;

        if (Rasterizer.Isometric)
        {
            Scalar w = Rasterizer.CanvasWidth;
            Scalar h = Rasterizer.CanvasHeight;
            Matrix3 iso = w >= h ? new Matrix3(h / w, 1, 1) : new Matrix3(1, w / h, 1);

            _matrix *= iso;
        }

        _matrix *= (
            rasterizer.ZoomFactor, 0, rasterizer.GlobalTransalation[0],
            0, rasterizer.ZoomFactor, rasterizer.GlobalTransalation[1],
            0, 0, 1
        );
        _matrix *= Matrix3.CreateRotationZ(rasterizer.GlobalRotation);
    }

    public void Dispose()
    {
        _brush_add.Dispose();
        _brush_sub.Dispose();
        _graphics.Dispose();
    }

    public void DrawPolygon(RenderPassDrawMode mode, bool looped, params Vector2[] points)
    {
        looped &= points.Length > 2;

        // TODO : looped ????

        Draw(mode, points, _graphics.FillPolygon, _graphics.DrawLines);
    }

    public void DrawEllipse(RenderPassDrawMode mode, params Vector2[] points) => Draw(mode, points, _graphics.FillClosedCurve, _graphics.DrawClosedCurve);

    private void Draw(RenderPassDrawMode mode, IEnumerable<Vector2> points, Action<Brush, PointF[]> fill, Action<Pen, PointF[]> draw)
    {
        bool additive = mode != RenderPassDrawMode.Subtractive;
        PointF[] gdi_points = points.ToArray(p => _matrix.HomogeneousMultiply(p).ToPointF());

        using Pen pen = new(additive ? _brush_add : _brush_sub, 1);

        fill(additive ? _brush_add : _brush_sub, gdi_points);
        draw(pen, gdi_points);
    }
}

internal static class RenderPassDrawModeExtensions
{
    public static RenderPassDrawMode Invert(this RenderPassDrawMode mode) => mode switch
    {
        RenderPassDrawMode.Additive => RenderPassDrawMode.Subtractive,
        _ => RenderPassDrawMode.Additive
    };
}

public enum RenderPassDrawMode
{
    Additive,
    Subtractive
}
