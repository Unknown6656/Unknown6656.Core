using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics;
using Unknown6656.Generics;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Imaging.Plotting;


public abstract class Plotter
{
    /// <summary>
    /// Determines the cursor's position in the function space - NOT in the pixel space.
    /// </summary>
    public virtual Vector2 CursorPosition { set; get; } = Vector2.Zero;

    /// <summary>
    /// The function space's center point (default = <see cref="Vector2.Zero"/>).
    /// <para/>
    /// A value of i.e. (2, -3) would move the function space such that the coordinates (2, -3) would be located in the middle of the plotter's canvas.
    /// </summary>
    public virtual Vector2 CenterPoint { set; get; } = Vector2.Zero;

    /// <summary>
    /// The function space's scale (default = <see cref="Scalar.One"/>).
    /// </summary>
    public virtual Scalar Scale { set; get; } = Scalar.One;

    /// <summary>
    /// Determines the default grid spacing in pixels.
    /// <para/>
    /// This means that one unit in the function space will have the size of <see cref="DefaultGridSpacing"/> pixels in the pixel space when assuming a <see cref="Scale"/> of one.
    /// </summary>
    public virtual Scalar DefaultGridSpacing { set; get; } = 50;

    /// <summary>
    /// Determines the plot's background color.
    /// </summary>
    public virtual RGBAColor BackgroundColor { set; get; } = RGBAColor.WhiteSmoke;

    /// <summary>
    /// Determines the plot's font size in pixels.
    /// </summary>
    public virtual float FontSize { set; get; } = 15;

    /// <summary>
    /// Determines the axis type (cartesian or polar).
    /// </summary>
    public virtual AxisType AxisType { set; get; } = AxisType.Cartesian;

    /// <summary>
    /// Determines the axis' color.
    /// </summary>
    public virtual RGBAColor AxisColor { set; get; } = RGBAColor.Black;

    /// <summary>
    /// Determines the grid's color.
    /// </summary>
    public virtual RGBAColor GridColor { set; get; } = RGBAColor.Gray;

    /// <summary>
    /// Determines the cursor's color.
    /// </summary>
    public virtual RGBAColor CursorColor { set; get; } = RGBAColor.MediumBlue;

    /// <summary>
    /// Determines the axis' thickness (in pixels).
    /// </summary>
    public virtual Scalar AxisThickness { set; get; } = Scalar.Two;

    /// <summary>
    /// Determines the grid's thickness (in pixels).
    /// </summary>
    public virtual Scalar GridThickness { set; get; } = Scalar.One;

    /// <summary>
    /// Determines the cursors's thickness (in pixels).
    /// </summary>
    public virtual Scalar CursorThickness { set; get; } = Scalar.Two;

    /// <summary>
    /// Determines whether the axis are currently visible.
    /// </summary>
    public virtual bool AxisVisible { set; get; } = true;

    /// <summary>
    /// Determines whether the grid is visible (does not affect complex function plots)
    /// </summary>
    public virtual bool GridVisible { set; get; } = true;

    /// <summary>
    /// Determines whether the cursor is currently visible.
    /// </summary>
    public virtual bool CursorVisible { set; get; } = false;

    /// <summary>
    /// The optional comment to be displayed in the top-left corner of the rendered plot.
    /// <para/>
    /// A value of <see langword="null"/> will represent an absent comment.
    /// </summary>
    public virtual (string Text, RGBAColor Color)? OptionalComment { set; get; } = null;

    protected virtual FontFamily FontFamily { get; set; } = FontFamily.GenericMonospace;

    public abstract void Plot(Graphics g, int width, int height);

    public Bitmap Plot(int width, int height)
    {
        Bitmap bmp = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bmp);

        Plot(g, width, height);

        return bmp;
    }


    internal protected enum PlottingOrder
    {
        Graph_Grid_Axes,
        Grid_Graph_Axes,
        Grid_Axes_Graph,
    }
}

public abstract class Plotter<POIValue>
    : Plotter
    where POIValue : IComparable<POIValue>
{
    #region PROPERTIES / FIELDS

    internal const float MARKING_SIZE = 3;
    internal const int POLAR_DIVISIONS = 8;


    /// <summary>
    /// Determines whether the points of interest's color (zero points and extrema).
    /// </summary>
    public RGBAColor PointsOfInterestColor { set; get; } = RGBAColor.Firebrick;

    public Scalar PointsOfInterestTolerance { set; get; } = 1e-4;

    /// <summary>
    /// Determines whether the points of interest (zero points and extrema) are visible.
    /// </summary>
    public bool PointsOfInterestVisible { set; get; } = false;

    internal protected abstract PlottingOrder Order { get; }

    protected abstract bool IsPolarPlotter { get; }

    #endregion
    #region INSTANCE METHODS

    public override void Plot(Graphics g, int width, int height)
    {
        g.CompositingMode = CompositingMode.SourceOver;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(BackgroundColor);

        float scale = Scale.Max(1e-5) * DefaultGridSpacing.Max(1);
        PointF center = new(width / 2f - CenterPoint.X * scale, height / 2f + CenterPoint.Y * scale);
        List<(Vector2 pos, POIValue desc)> poi = new();

        void plot_graph() => PlotGraph(g, width, height, center.X, center.Y, scale, out poi);

        if (Order is PlottingOrder.Graph_Grid_Axes)
            plot_graph();

        if (GridVisible)
            PlotGrid(g, width, height, center.X, center.Y, scale);

        if (Order is PlottingOrder.Grid_Graph_Axes)
            plot_graph();

        if (AxisVisible)
            PlotAxis(g, width, height, center.X, center.Y, scale);

        if (Order is PlottingOrder.Grid_Axes_Graph)
            plot_graph();

        if (PointsOfInterestVisible)
            PlotPOIs(g, width, height, center.X, center.Y, scale, poi);

        if (CursorVisible)
            PlotCursor(g, width, height, center.X, center.Y, scale);

        DrawComment(g);
    }

    protected virtual void PlotGrid(Graphics g, int w, int h, float x, float y, float s)
    {
        while (s > DefaultGridSpacing)
            s /= 2;

        using Pen pen = new(GridColor, GridThickness);
        int ch = (int)(w / s) + 1;
        int cv = (int)(h / s) + 1;
        float oh = x % s;
        float ov = y % s;

        if (AxisType == AxisType.Cartesian)
        {
            for (int i = 0; i <= ch; ++i)
                g.DrawLine(pen, i * s + oh, 0, i * s + oh, h);

            for (int i = 0; i <= cv; ++i)
                g.DrawLine(pen, 0, i * s + ov, w, i * s + ov);
        }
        else
        {
            Scalar r = new Vector2(w, h).Length;
            int c = (int)(r / s / 2) + 1;

            for (int i = 1; i <= c; ++i)
                g.DrawEllipse(pen, x - i * s, y - i * s, 2 * i * s, 2 * i * s);

            for (int i = 1; i < POLAR_DIVISIONS; ++i)
            {
                Scalar φ = (90d * i / POLAR_DIVISIONS).Radians();

                g.DrawLine(pen, x - r * φ.Sin(), y - r * φ.Cos(), x + r * φ.Sin(), y + r * φ.Cos());
                g.DrawLine(pen, x + r * φ.Cos(), y - r * φ.Sin(), x - r * φ.Cos(), y + r * φ.Sin());
            }
        }
    }

    protected virtual void PlotAxis(Graphics g, int w, int h, float x, float y, float s)
    {
        using Font font = new(FontFamily, FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using Pen pen = new(AxisColor, AxisThickness);
        using Brush brush = new SolidBrush(AxisColor);
        SizeF char_dims = g.MeasureString("W", font);
        int ch = (int)(w / s) + 1;
        int cv = (int)(h / s) + 1;
        float oh = x % s;
        float ov = y % s;

        g.DrawLine(pen, x, 0, x, h);
        g.DrawLine(pen, 0, y, w, y);
        g.DrawString("0", font, brush, x - char_dims.Width, y);
        g.DrawString("X", font, brush, w - char_dims.Width, y - char_dims.Height);
        g.DrawString("Y", font, brush, x, 0);

        int vskip = (int)Math.Ceiling(FontSize * 1.5 / s);
        int hskip = (int)Math.Ceiling(FontSize * 1.5 * Math.Log10(ch / 2) / s);
        int mspacing = s < 2 + AxisThickness ? (int)AxisThickness.Ceiling.Add(2) : 1;

        for (int i = 0; i <= ch; ++i)
            if (i - (int)(w / (s * 2) - CenterPoint.X) is int ix && ix != 0)
            {
                if (i % mspacing == 0)
                    g.DrawLine(pen, i * s + oh, y - MARKING_SIZE, i * s + oh, y + MARKING_SIZE);

                if (hskip == 0 || ix % hskip == 0)
                {
                    string str = ix.ToString();

                    g.DrawString(str, font, brush, i * s + oh - str.Length * char_dims.Width / 2, y + 2);
                }
            }

        for (int i = 0; i <= cv; ++i)
            if ((int)(h / (s * 2) + CenterPoint.Y) - i is int iy && iy != 0)
            {
                if (i % mspacing == 0)
                    g.DrawLine(pen, x - MARKING_SIZE, i * s + ov, x + MARKING_SIZE, i * s + ov);

                if (vskip == 0 || iy % vskip == 0)
                {
                    string str = iy.ToString();

                    g.DrawString(str, font, brush, x - 2 - str.Length * char_dims.Width * .8f, i * s + ov - char_dims.Height / 2);
                }
            }

        if (AxisType == AxisType.Polar)
        {
            using Pen cpen = new(AxisColor, AxisThickness) { DashPattern = [3, 6] };
            Scalar diag = new Vector2(w, h).Length;
            int cskip = Math.Max(vskip, hskip) * 4;
            int c = (int)(diag / s / 2);
            int circle = 0;

            for (int i = 1; i <= c; ++i)
                if (i % cskip == 0)
                {
                    ++circle;

                    float r = i * s;
                    float ri = r - 2 * MARKING_SIZE;
                    float ro = r + 2 * MARKING_SIZE;

                    g.DrawEllipse(cpen, x - r, y - r, 2 * r, 2 * r);

                    // int sskip = (char_dims.Height / s).Ceiling;
                    // int sskip = ((POLAR_DIVISIONS * 4 * char_dims.Height) / (ri * Scalar.Pi)).Ceiling;

                    int sskip = ri * Scalar.Pi < 4 * POLAR_DIVISIONS * char_dims.Height ? (int)(ri * Scalar.Pi / 2 / POLAR_DIVISIONS / char_dims.Height).Ceiling : POLAR_DIVISIONS;

                    for (int j = 0; j < POLAR_DIVISIONS * 4; ++j)
                        if (j % POLAR_DIVISIONS != 0)
                        {
                            Scalar φ = .5 * Scalar.Pi * j / POLAR_DIVISIONS;
                            Scalar px = x + ro * φ.Cos();
                            Scalar py = y - ro * φ.Sin();

                            g.DrawLine(pen, x + ri * φ.Cos(), y - ri * φ.Sin(), px, py);

                            if (j % sskip == 0)
                                continue;

                            string str = circle % 2 == 1 ? $"{j * .5 / POLAR_DIVISIONS}π" : $"{j * 90d / POLAR_DIVISIONS}°";
                            SizeF sdim = g.MeasureString(str, font);
                            Scalar sx = x + ro * φ.Cos();
                            Scalar sy = y - ro * φ.Sin();

                            if (φ < Scalar.Pi)
                            {
                                sy -= sdim.Height;

                                if (φ > Scalar.Pi / 2)
                                    sx -= sdim.Width;
                            }
                            else if (φ < Scalar.Pi * 3 / 2)
                                sx -= sdim.Width;

                            g.DrawString(str, font, brush, sx, sy);
                        }
                }
        }
    }

    protected virtual void PlotCursor(Graphics g, int w, int h, float x, float y, float s)
    {
        using Font font = new(FontFamily, FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using Brush brush = new SolidBrush(CursorColor);
        using Pen pen = new(CursorColor, CursorThickness.Max(1e-3));
        using Pen thin = new(CursorColor, (CursorThickness * .5).Clamp(1e-3, .5));
        using Pen dashed = new(CursorColor, CursorThickness.Max(1e-3))
        {
            DashStyle = DashStyle.DashDot
        };

        float cursorx = CursorPosition.X;
        float cursory = CursorPosition.Y;
        (Scalar φ, RGBAColor col)? deriv = null;
        string? str = null;

        if (GetInformation(CursorPosition) is { } t)
        {
            str = t.Value;
            cursorx = t.Position.X;
            cursory = t.Position.Y;
            deriv = (t.DerivativeAngle, t.Color);
        }

        if (float.IsFinite(cursorx) && float.IsFinite(cursory))
            try
            {
                (float cx, float cy) = ToScreenSpace(new(cursorx, cursory), x, y, s);
                float sz = 2 * MARKING_SIZE;

                if (deriv is (Scalar α, RGBAColor col))
                {
                    Scalar len = new Vector2(w, h).Length * 2;

                    g.DrawLine(new Pen(col, 1),
                        cx - α.Cos() * len,
                        cy + α.Sin() * len,
                        cx + α.Cos() * len,
                        cy - α.Sin() * len
                    );
                }

                Vector2 axis_text_size = g.MeasureString("-10", font);

                if (AxisType is AxisType.Cartesian)
                {
                    if (new Vector2(cy - y, cx - x).Length > .5)
                    {
                        g.DrawLine(dashed, cx, y, cx, cy);
                        g.DrawLine(dashed, x, cy, cx, cy);
                    }

                    g.DrawString(cursorx.ToString(), font, brush, cx - FontSize, y + (FontSize + MARKING_SIZE) * Math.Sign(cursory));

                    if (cursorx > 0)
                    {
                        string text = cursory.ToString();
                        int text_width = (int)Math.Ceiling(g.MeasureString(text, font).Width);

                        g.DrawString(text, font, brush, x - axis_text_size.Y - text_width, cy - FontSize / 2);
                    }
                    else
                        g.DrawString(cursory.ToString(), font, brush, x + MARKING_SIZE, cy - FontSize / 2);
                }
                else if (AxisType is AxisType.Polar)
                {
                    Vector2 diff = (cursorx, cursory) - CenterPoint;
                    Scalar dist = diff.Length * s;
                    Scalar φ = new Complex(diff.X, -diff.Y).Argument.Degrees();
                    Scalar end_φ = φ.IsPositive ? 360 - φ : -φ;

                    if (dist > 1 && φ.DistanceTo(end_φ) > .5)
                    {
                        g.DrawArc(dashed, x - dist, y - dist, dist * 2, dist * 2, φ, end_φ);
                        g.DrawLine(pen, x + dist - 1, y - sz, x + dist - 1, y + sz);
                    }

                    g.DrawLine(dashed, x, y, cx, cy);
                    g.DrawString($"θ = {(Scalar.Tau + ((Complex)diff).Argument) % Scalar.Tau}\nr = {diff.Length}", font, brush, x + dist, y - 2 * FontSize - MARKING_SIZE);
                }

                g.DrawLine(pen, cx, cy - sz, cx, cy + sz);
                g.DrawLine(pen, cx - sz, cy, cx + sz, cy);

                {
                    (float mouse_x, float mouse_y) = ToScreenSpace(CursorPosition, x, y, s);

                    if (this is { AxisType: AxisType.Polar, IsPolarPlotter: false })
                        g.DrawLines(thin, new[] { mouse_y, cy, y }.OrderBy(LINQ.id).ToArray(y => new PointF(mouse_x, y)));
                    else if (this is { AxisType: AxisType.Cartesian, IsPolarPlotter: false })
                        g.DrawLine(thin, mouse_x, mouse_y, mouse_x, Math.Abs(y - mouse_y) > Math.Abs(cy - mouse_y) ? cy : y);
                    else if (this is { AxisType: AxisType.Polar, IsPolarPlotter: true })
                        if (new Vector2(x - mouse_x, y - mouse_y).Length > new Vector2(x - cx, y - cy).Length)
                            g.DrawLine(thin, cx, cy, mouse_x, mouse_y);
                    else if (this is { AxisType: AxisType.Cartesian, IsPolarPlotter: true })
                        if (new Vector2(x - mouse_x, y - mouse_y).Length > new Vector2(x - cx, y - cy).Length)
                            g.DrawLine(thin, x, y, mouse_x, mouse_y);
                        else
                            g.DrawLine(thin, x, y, cx, cy);
                }

                if (!(this is { AxisType: AxisType.Cartesian, IsPolarPlotter: false }))
                    g.DrawString(str, font, brush, cx, cy);
            }
            catch (Exception ex)
            when (ex is OutOfMemoryException or OverflowException)
            {
                Console.WriteLine($"[unknown6656.core, CRITICAL]\n{ex}");
            }
    }

    protected virtual void PlotPOIs(Graphics g, int w, int h, float x, float y, float s, List<(Vector2 pos, POIValue value)> poi)
    {
        using Font font = new(FontFamily, FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using Pen pen = new(PointsOfInterestColor, AxisThickness);
        using Brush brush = new SolidBrush(PointsOfInterestColor);

        var grouped = from @group in poi.GroupBy(p => p.pos, new CustomEqualityComparer<Vector2>((v1, v2) => v1.DistanceTo(v2) < 2 * MARKING_SIZE))
                      let size = @group.Count()
                      where size > 0
                      let first = @group.First()
                      select (first.pos, first.value, size);

        foreach ((Vector2 pos, POIValue val, int count) in grouped)
        {
            g.DrawLine(pen, pos.X - MARKING_SIZE, pos.Y, pos.X + MARKING_SIZE, pos.Y);
            g.DrawLine(pen, pos.X, pos.Y - MARKING_SIZE, pos.X, pos.Y + MARKING_SIZE);
            g.DrawString(val.ToString(), font, brush, pos.X, pos.Y);
        }
    }

    protected virtual void DrawComment(Graphics g)
    {
        if (OptionalComment is (string s, RGBAColor col) && s?.Trim() is string str && (str?.Length ?? 0) > 0)
            using (Font font = new(FontFamily, FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                g.DrawString(str, font, new SolidBrush(col), 0, 0);
    }

    protected internal abstract (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor);

    protected internal abstract void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, POIValue value)> poi);

    protected Vector2 ToScreenSpace(Vector2 vector, float x, float y, float s) => new(x + vector.X * s, y - vector.Y * s);

    protected Vector2 ToFuncSpace(Vector2 vector, float x, float y, float s) => new((vector.X - x) / s, (y - vector.Y) / s);

    protected bool IsInsideScreenSpace(Vector2 vector, int w, int h, float margin = 1) =>
        vector.X > -margin && vector.X < w + margin && vector.Y > -margin && vector.Y < h + margin;

    protected bool IsInsideFuncSpace(Vector2 vector, int w, int h, float x, float y, float s, float margin = 1) =>
        IsInsideScreenSpace(ToScreenSpace(vector, x, y, s), w, h, margin);

    #endregion
}

public class CombinedPlotter<POIValue>
    : Plotter<POIValue>
    where POIValue : IComparable<POIValue>
{
    internal protected override PlottingOrder Order { get; }

    protected override bool IsPolarPlotter => false;

    public Plotter<POIValue>[] Plotters { get; }


    public CombinedPlotter(params Plotter<POIValue>[] plotters)
    {
        Plotters = plotters;
        Order = (PlottingOrder)plotters.Min(p => (int)p.Order);
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => (from p in Plotters
                                                                                                                                               let inf = p.GetInformation(cursor)
                                                                                                                                               where inf.HasValue
                                                                                                                                               select inf.Value).FirstOrDefault();

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, POIValue value)> poi)
    {
        poi = new();

        foreach (Plotter<POIValue> plotter in Plotters)
        {
            plotter.PlotGraph(g, w, h, x, y, s, out List<(Vector2, POIValue)> tmp_poi);
            poi.AddRange(tmp_poi);
        }
    }
}

public interface IMultiPlotter
{
    public (object Item, RGBAColor Color)[] PlottableItems { get; }
    public object? SelectedItem { get; }
    public Scalar SelectedItemThickness { set; get; }
    public Scalar RegularItemThickness { set; get; }
    public int? SelectedIndex { get; set; }
}

public abstract class MultiPlotter<PlotterItem, POIValue>
    : Plotter<POIValue>
    , IMultiPlotter
    where POIValue : IComparable<POIValue>
{
    private (PlotterItem, RGBAColor)[] _items;
    private int? _selidx = null;


    public (PlotterItem Item, RGBAColor Color)[] PlottableItems => _items;

    public PlotterItem? SelectedItem => SelectedIndex is int index ? PlottableItems[index].Item : default;

    (object Item, RGBAColor Color)[] IMultiPlotter.PlottableItems => PlottableItems.ToArray(t => (t.Item as object, t.Color));

    object? IMultiPlotter.SelectedItem => SelectedItem;

    public Scalar SelectedItemThickness { set; get; } = 3;

    public Scalar RegularItemThickness { set; get; } = Scalar.Two;

    public Scalar FillOpacity { set; get; } = .5;

    public int? SelectedIndex
    {
        set => _selidx = value is int i ? i >= 0 && i < PlottableItems.Length ? (int?)i : throw new ArgumentOutOfRangeException(nameof(value), $"The index must be a positive and smaller than {PlottableItems.Length}.") : null;
        get => _selidx;
    }


    public MultiPlotter(PlotterItem item)
        : this(item, RGBAColor.Red)
    {
    }

    public MultiPlotter(PlotterItem item, RGBAColor color)
        : this((item, color)) => SelectedIndex = 0;

    public MultiPlotter(IEnumerable<(PlotterItem item, RGBAColor Color)> items)
        : this(items as (PlotterItem item, RGBAColor Color)[] ?? items.ToArray())
    {
    }

    public MultiPlotter(params (PlotterItem item, RGBAColor Color)[] items)
    {
        _items = items;
        SelectedIndex = null;
    }

    public void AddItem(PlotterItem item, RGBAColor color)
    {
        int index = _items.Length;

        Array.Resize(ref _items, index + 1);

        _items[index] = (item, color);
    }

    // public bool RemoveItem(PlotterItem item)
    // public void RemoveItem(int index)
    // TODO

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, POIValue value)> poi)
    {
        poi = new();

        for (int idx = 0; idx < PlottableItems.Length; ++idx)
        {
            (PlotterItem item, RGBAColor color) = PlottableItems[idx];
            using Pen pen = new(color, idx == SelectedIndex ? SelectedItemThickness : RegularItemThickness);
            using Brush fill = new SolidBrush(new RGBAColor(color, FillOpacity));

            PlotGraph(g, w, h, x, y, s, item, pen, fill, out List<(Vector2 pos, POIValue value)> items);

            poi.AddRange(items);
        }
    }

    protected internal abstract void PlotGraph(Graphics g, int w, int h, float x, float y, float s, PlotterItem item, Pen pen, Brush fill, out List<(Vector2 pos, POIValue value)> poi);
}

public abstract class MultiFunctionPlotter<Func, POIValue>
    : MultiPlotter<Func, POIValue>
    , IMultiPlotter
    where Func : FieldFunction<POIValue>
    where POIValue : unmanaged, IField<POIValue>, IComparable<POIValue>
{
    private int? _selidx = null;


    public (Func Function, RGBAColor Color)[] Functions => PlottableItems;

    public Func? SelectedFunction => SelectedItem;


    public MultiFunctionPlotter(Func function)
        : base(function)
    {
    }

    public MultiFunctionPlotter(Func function, RGBAColor color)
        : base(function, color)
    {
    }

    public MultiFunctionPlotter(params (Func Function, RGBAColor Color)[] functions)
        : base(functions)
    {
    }

    public MultiFunctionPlotter(IEnumerable<(Func Function, RGBAColor Color)> functions)
        : base(functions)
    {
    }
}

public class ImplicitFunctionPlotter
    : MultiPlotter<ImplicitFunction<Vector2>, Vector2>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = true;
    public Scalar FunctionEvaluationTolerance { set; get; } = 1e-6;
    public int MarchingSquaresPixelStride { set; get; } = 4;


    public ImplicitFunctionPlotter(ImplicitFunction<Vector2> function)
        : base(function)
    {
    }

    public ImplicitFunctionPlotter(ImplicitFunction<Vector2> function, RGBAColor color)
        : base(function, color)
    {
    }

    public ImplicitFunctionPlotter(params (ImplicitFunction<Vector2> Function, RGBAColor Color)[] functions)
        : base(functions)
    {
    }

    public ImplicitFunctionPlotter(IEnumerable<(ImplicitFunction<Vector2> Function, RGBAColor Color)> functions)
        : base(functions)
    {
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO : implement

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, ImplicitFunction<Vector2> func, Pen pen, Brush fill, out List<(Vector2 pos, Vector2 value)> poi)
    {
        int CELLS_Y = Math.Max(2, Math.Min(w, h) / Math.Max(2, MarchingSquaresPixelStride));
        int CELLS_X = CELLS_Y * w / h;

        Vector2 funcspace_min = new Vector2(-x, -y).Divide(s);
        Vector2 funcspace_max = new Vector2(w, h).Divide(s).Add(funcspace_min);
        Vector2 funcspace_step = funcspace_max.Subtract(funcspace_min).ComponentwiseDivide(CELLS_X - 1, CELLS_Y - 1);
        Vector2 screenspace_step = new Vector2(w, h).ComponentwiseDivide(CELLS_X - 1, CELLS_Y - 1);

        (
            Vector2 screenspace_curr_coord,
            Vector2 screenspace_next_coord,
            float corner,
            float? x_intersection,
            float? y_intersection
        )[,] corners = new (Vector2, Vector2, float, float?, float?)[CELLS_X, CELLS_Y];

        Parallel.For(0, CELLS_X * CELLS_Y, i =>
        {
            int index_y = i / CELLS_X;
            int index_x = i % CELLS_X;
            Vector2 funcspace_coord = funcspace_step.ComponentwiseMultiply(index_x, index_y).Add(funcspace_min);
            funcspace_coord = new(funcspace_coord.X, -funcspace_coord.Y);

            corners[index_x, index_y] = (
                screenspace_step.ComponentwiseMultiply(index_x, index_y),
                screenspace_step.ComponentwiseMultiply(index_x + 1, index_y + 1),
                func.EvaluateSignedDifference(funcspace_coord, FunctionEvaluationTolerance),
                null,
                null
            );
        });

        Parallel.For(0, CELLS_X * CELLS_Y, i =>
        {
            int index_y = i / CELLS_X;
            int index_x = i % CELLS_X;

            (Vector2 screenspace_curr_coord, Vector2 screenspace_next_coord, float corner_tl, _, _) = corners[index_x, index_y];

            float corner_tr = index_x == CELLS_X - 1 ? float.NaN : corners[index_x + 1, index_y].corner; // top right
            float corner_bl = index_y == CELLS_Y - 1 ? float.NaN : corners[index_x, index_y + 1].corner; // bottom left

            corners[index_x, index_y] = (
                screenspace_curr_coord,
                screenspace_next_coord,
                corner_tl,
                corner_tl / (corner_tl - corner_tr) is float t and >= 0 and <= 1 && float.IsFinite(t) ? t * screenspace_next_coord.X + (1 - t) * screenspace_curr_coord.X : null,
                corner_tl / (corner_tl - corner_bl) is float l and >= 0 and <= 1 && float.IsFinite(l) ? l * screenspace_next_coord.Y + (1 - l) * screenspace_curr_coord.Y : null
            );
        });

        for (int iy = 0; iy < CELLS_Y - 1; ++iy)
            for (int ix = 0; ix < CELLS_X - 1; ++ix)
            {
                (Vector2 screenspace_coord, Vector2 screenspace_next, _, float? ptx, float? pty) = corners[ix, iy];
                List<PointF> points = new(4);

                if (ptx.HasValue)
                    points.Add(new(ptx.Value, screenspace_coord.Y));
                if (pty.HasValue)
                    points.Add(new(screenspace_coord.X, pty.Value));
                if (corners[ix, iy + 1].x_intersection is float px)
                    points.Add(new(px, screenspace_next.Y));
                if (corners[ix + 1, iy].y_intersection is float py)
                    points.Add(new(screenspace_next.X, py));

                // TODO : fill

                if (points.Count > 1)
                    g.DrawLine(pen, points[0], points[1]);

                if (points.Count > 3)
                    g.DrawLine(pen, points[2], points[3]);
            }

        poi = new(); // TODO
    }
}

public class ImplicitFunctionSignedDistancePlotter
    : Plotter<Vector2>
{
    private readonly ImplicitFunctionPlotter _overlay;


    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = true;

    public ImplicitFunction<Vector2> Function { get; }

    public int SamplingPixelStride { set; get; } = 7;

    public Scalar FunctionEvaluationTolerance { set; get; } = 1e-6;

    public ColorMap ColorMap { get; set; } = ColorMap.Jet;

    public RGBAColor OverlayColor
    {
        get => _overlay.PlottableItems[0].Color;
        set => _overlay.PlottableItems[0] = (Function, value);
    }

    public Scalar OverlayThickness
    {
        get => _overlay.RegularItemThickness;
        set
        {
            _overlay.RegularItemThickness = value;
            _overlay.SelectedItemThickness = value;
        }
    }

    public bool DisplayOverlayFunction { get; set; } = false;

    public Scalar MinimumValueClipping { get; set; } = -100;

    public Scalar MaximumValueClipping { get; set; } = 100;


    public ImplicitFunctionSignedDistancePlotter(ImplicitFunction<Vector2> function)
    {
        Function = function;
        _overlay = new(function);
        OverlayColor = RGBAColor.Red;
        OverlayThickness = 3;
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO : implement

    protected internal override unsafe void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Vector2 value)> poi)
    {
        poi = new();

        int CELLS_Y = h / SamplingPixelStride;
        int CELLS_X = w / SamplingPixelStride;

        Vector2 funcspace_min = new Vector2(-x, -y).Divide(s);
        Vector2 funcspace_max = new Vector2(w, h).Divide(s).Add(funcspace_min);
        Vector2 funcspace_step = funcspace_max.Subtract(funcspace_min).ComponentwiseDivide(CELLS_X - 1, CELLS_Y - 1);
        Vector2 screenspace_step = new Vector2(w, h).ComponentwiseDivide(CELLS_X - 1, CELLS_Y - 1);

        (RGBAColor color, float value)[,] corners = new (RGBAColor, float)[CELLS_X, CELLS_Y];
        float max = MinimumValueClipping;
        float min = MaximumValueClipping;

        Parallel.For(0, CELLS_X * CELLS_Y, i =>
        {
            int index_y = i / CELLS_X;
            int index_x = i % CELLS_X;
            Vector2 funcspace_coord = funcspace_step.ComponentwiseMultiply(index_x, index_y).Add(funcspace_min);
            funcspace_coord = new(funcspace_coord.X, -funcspace_coord.Y);

            corners[index_x, index_y] = (default, Function.EvaluateSignedDifference(funcspace_coord, FunctionEvaluationTolerance));
        });

        for (int iy = 0; iy < CELLS_Y; ++iy)
            for (int ix = 0; ix < CELLS_X; ++ix)
            {
                max = Math.Max(corners[ix, iy].value, max);
                min = Math.Min(corners[ix, iy].value, min);
            }

        Parallel.For(0, CELLS_X * CELLS_Y, i =>
        {
            int index_y = i / CELLS_X;
            int index_x = i % CELLS_X;
            float value = corners[index_x, index_y].value;

            corners[index_x, index_y] = (ColorMap.Interpolate(value, min, max), value);
        });

        using Bitmap bmp = new(w, h, PixelFormat.Format32bppArgb);

        bmp.LockRGBAPixels((ptr, _, _) => Parallel.For(0, w * h, idx =>
        {
            float x = idx % w;
            float y = idx / w;
            int curr_cx = (int)(x / w * (CELLS_X - 1));
            int curr_cy = (int)(y / h * (CELLS_Y - 1));
            int next_cx = (int)((x + 1) / w * (CELLS_X - 1));
            int next_cy = (int)((y + 1) / h * (CELLS_Y - 1));

            RGBAColor tl = corners[curr_cx, curr_cy].color;
            RGBAColor tr = corners[next_cx, curr_cy].color;
            RGBAColor bl = corners[curr_cx, next_cy].color;
            RGBAColor br = corners[next_cx, next_cy].color;

            float px = x / w % 1;
            float py = y / h % 1;

            ptr[idx] = RGBAColor.LinearInterpolate(
                RGBAColor.LinearInterpolate(tl, tr, px),
                RGBAColor.LinearInterpolate(bl, br, px),
                py
            );
        }));
        g.DrawImageUnscaled(bmp, 0, 0);

        if (GridVisible)
            PlotGrid(g, w, h, x, y, s);

        if (DisplayOverlayFunction)
            _overlay.PlotGraph(g, w, h, x, y, s, out poi);
    }
}

public class CartesianFunctionPlotter<Func>
    : MultiFunctionPlotter<Func, Scalar>
    where Func : FieldFunction<Scalar>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = false;


    public CartesianFunctionPlotter(Func function)
        : base(function)
    {
    }

    public CartesianFunctionPlotter(Func function, RGBAColor color)
        : base(function, color)
    {
    }

    public CartesianFunctionPlotter(params (Func Function, RGBAColor Color)[] functions)
        : base(functions)
    {
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
    {
        if (SelectedIndex is int i)
        {
            Func f = Functions[i].Function;
            Scalar y = f[cursor.X];
            Scalar h = (Scale * DefaultGridSpacing).Inverse;

            h = .5 * (f[cursor.X + h] - f[cursor.X - h]) / h;

            return ((cursor.X, y), Functions[i].Color, y.ToString(), h.Atan());
        }

        return null;
    }

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, Func func, Pen pen, Brush fill, out List<(Vector2 pos, Scalar value)> poi)
    {
        poi = new List<(Vector2, Scalar)>();

        float last = y - func[-x / s] * s;
        float curr = last;
        Scalar fx;

        for (int i = 0; i <= w; ++i)
        {
            fx = func[(i - x) / s];
            curr = y - fx * s;

            if (last >= -s && last <= h + s)
                g.DrawLine(pen, i - 1, last, i, curr);

            last = curr;

            if (Math.Abs(curr) < PointsOfInterestTolerance)
                poi.Add(((i, curr), fx));
        }
    }
}

public class PolarFunctionPlotter<Func>
    : MultiFunctionPlotter<Func, Scalar>
    where Func : FieldFunction<Scalar>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = true;
    public Scalar MinAngle { set; get; } = Scalar.Zero;
    public Scalar MaxAngle { set; get; } = Scalar.Tau * 4;
    public Scalar AngleStep { set; get; } = 1e-5;


    public PolarFunctionPlotter(Func function)
        : base(function)
    {
    }

    public PolarFunctionPlotter(Func function, RGBAColor color)
        : base(function, color)
    {
    }

    public PolarFunctionPlotter(params (Func Function, RGBAColor Color)[] functions)
        : base(functions)
    {
        AxisType = AxisType.Polar;
        AngleStep = (Scale * DefaultGridSpacing).Inverse;
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
    {
        if (SelectedIndex is int i)
        {
            Scalar φ = (((Complex)cursor).Argument + Scalar.Tau) % Scalar.Tau;
            Func f = Functions[i].Function;
            Scalar r = f[φ];
            Scalar h = (Scale * DefaultGridSpacing * 4).Inverse;
            Scalar fs(Scalar x) => f[x] * x.Sin();
            Scalar fc(Scalar x) => f[x] * x.Cos();
            Scalar δ = new Complex(
                fc(φ + h) - fc(φ - h),
                fs(φ + h) - fs(φ - h)
            ).Argument;

            return (Complex.FromPolarCoordinates(r, φ), Functions[i].Color, null, δ);
        }

        return null;
    }

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, Func func, Pen pen, Brush fill, out List<(Vector2 pos, Scalar value)> poi)
    {
        poi = new();

        Scalar rad = new Vector2(w, h).SquaredLength;
        Scalar last = func[MinAngle] * s;
        Scalar curr = last;

        for (Scalar φ = MinAngle + AngleStep; φ <= MaxAngle; φ += AngleStep)
        {
            curr = func[φ] * s;

            float x1 = x + Math.Cos(φ - AngleStep - 1e-5) * last.Clamp(-rad, rad);
            float y1 = y - Math.Sin(φ - AngleStep - 1e-5) * last.Clamp(-rad, rad);
            float x2 = x + Math.Cos(φ) * curr.Clamp(-rad, rad);
            float y2 = y - Math.Sin(φ) * curr.Clamp(-rad, rad);

            g.DrawLine(pen, x1, y1, x2, y2);

            last = curr;

            if (Math.Abs(curr) < PointsOfInterestTolerance)
                poi.Add(((x2, y2), curr / s));
        }
    }
}

public class CartesianFunctionFamilyPlotter
    : CartesianFunctionPlotter<ScalarFunction>
{
    public CartesianFunctionFamilyPlotter(Function<Vector2, Scalar> function, Scalar start, Scalar end, int count, ColorMap colormap)
        : base(GenerateFunctions(function, start, end, count, colormap))
    {
    }

    internal static (ScalarFunction function, RGBAColor color)[] GenerateFunctions(Function<Vector2, Scalar> function, Scalar start, Scalar end, int count, ColorMap colormap)
    {
        (ScalarFunction function, RGBAColor color)[] functions = new (ScalarFunction, RGBAColor)[count];

        for (int i = 0; i < count; ++i)
        {
            Scalar progress = i / (Scalar)(count - 1);

            functions[i] = (
                new(x => function.Evaluate(new(x, end * progress + start * (1 - progress)))),
                colormap[progress]
            );
        }

        return functions;
    }
}

public class PolarFunctionFamilyPlotter
    : PolarFunctionPlotter<ScalarFunction>
{
    public PolarFunctionFamilyPlotter(Function<Vector2, Scalar> function, Scalar start, Scalar end, int count, ColorMap colormap)
        : base(CartesianFunctionFamilyPlotter.GenerateFunctions(function, start, end, count, colormap))
    {
    }
}

public class ComplexFunctionPlotter
    : Plotter<Complex>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Graph_Grid_Axes;
    protected override bool IsPolarPlotter { get; } = false;

    public ComplexColorStyle Style { set; get; } = ComplexColorStyle.Wrapped;
    public bool PhaseLinesVisible { set; get; } = false;
    public RGBAColor PhaseLineColor { set; get; } = RGBAColor.White;
    public int PhaseLineSteps { set; get; } = POLAR_DIVISIONS * 4;
    public Scalar PhaseLineTolerance { set; get; } = 1e-2;
    public bool UseInterpolation { set; get; } = false;
    public Function<Complex> Function { get; }


    public ComplexFunctionPlotter(Function<Complex> function) => Function = function;

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
    {
        Complex c = Function[cursor];
        Scalar dir = 0;
        Scalar mag = 0;

        for (Scalar φ = 0, s = (Scale * DefaultGridSpacing * 4).Inverse; φ < Scalar.Tau; φ += s)
        {
            Vector2 offs = Complex.FromPolarCoordinates(s, φ);
            Scalar h = (c - Function[cursor + offs]).Length / s;

            if (h >= mag)
            {
                mag = h;
                dir = φ;
            }
        }

        return (cursor, CursorColor, $"re = {c.Real}\nim = {c.Imaginary}i\n θ = {c.Argument}\n r = {c.Length}", dir);
    }

    protected internal override unsafe void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Complex value)> poi)
    {
        ConcurrentBag<(Vector2, Complex)> bag = new();
        Func<Complex, RGBAColor> color = Style == ComplexColorStyle.Wrapped ? RGBAColor.FromComplexWrapped : RGBAColor.FromComplexSmooth;
        Scalar phasediv = Scalar.Tau / Math.Max(PhaseLineSteps, 0);
        Vector3 plotter(int u, int v)
        {
            float re = (u - x) / s;
            float im = (v - y) / s;
            Complex res = Function[(re, -im)];
            Vector3 col = color(res);
            Scalar len = res.Length;

            if (PhaseLinesVisible && PhaseLineSteps > 0)
            {
                Scalar r = (res.Argument + PhaseLineTolerance + Scalar.Tau) % Scalar.Tau % phasediv;

                if (r <= 2 * PhaseLineTolerance)
                {
                    r *= Scalar.Pi / 2 / PhaseLineTolerance;
                    col = Vector3.LinearInterpolate(col, PhaseLineColor, r.Sin().Power(3));
                    col *= len.Min(1);
                }
            }

            if (PointsOfInterestVisible && len.Abs() < PointsOfInterestTolerance /*|| len.Abs().Inverse > PointsOfInterestTolerance*/)
                bag.Add(((u, v), (re, -im)));

            return col;
        }

        bool intp = UseInterpolation;
        using Bitmap plot = new(w, h, PixelFormat.Format32bppArgb);
        new BitmapLocker(plot).LockRGBAPixels((ptr, _w, _h) => Parallel.For(0, _w * _h, i =>
        {
            int u = i % _w;
            int v = i / _w;

            if (intp)
                ptr[i] = (plotter(u, v)
                        + plotter(u + 1, v)
                        + plotter(u, v + 1)
                        + plotter(u + 1, v + 1)) / 4;
            else
                ptr[i] = plotter(u, v);
        }));

        g.DrawImageUnscaled(plot, 0, 0);

        poi = bag.ToList();
    }
}

public class PointCloud2DPlotter
    : MultiPlotter<IEnumerable<Vector2>, Vector2>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Axes_Graph;
    protected override bool IsPolarPlotter { get; } = false;

    public Scalar PointSize { get; set; } = 8;
    public Scalar DecayFactor { get; set; } = Scalar.Zero;


    public PointCloud2DPlotter(IEnumerable<Vector2> points)
        : base(points)
    {
    }

    public PointCloud2DPlotter(IEnumerable<Vector2> points, RGBAColor color)
        : base(points, color)
    {
    }

    public PointCloud2DPlotter(IEnumerable<(IEnumerable<Vector2> points, RGBAColor Color)> points)
        : base(points)
    {
    }

    public PointCloud2DPlotter(params (IEnumerable<Vector2> points, RGBAColor Color)[] points)
        : base(points)
    {
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, IEnumerable<Vector2> points, Pen pen, Brush fill, out List<(Vector2 pos, Vector2 value)> poi)
    {
        poi = new();

        float α = 1;

        foreach (Vector2 point in points)
        {
            (float px, float py) = ToScreenSpace(point, x, y, s);

            if (α < 0.001)
                break;
            else if (DecayFactor.IsPositiveDefinite && fill is SolidBrush sb)
            {
                fill = new SolidBrush(new RGBAColor(sb.Color, α));
                α *= 1 - DecayFactor;
            }

            if (IsInsideScreenSpace((px, py), w, h, PointSize))
            {
                g.FillEllipse(fill, px - PointSize * .5f, py - PointSize * .5f, PointSize, PointSize);
                poi.Add((new(px, py), point));
            }
        }
    }
}

public class Trajectory2DPlotter
    : PointCloud2DPlotter
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;

    public bool PlotPoints { get; set; } = false;


    public Trajectory2DPlotter(IEnumerable<Vector2> trajectory)
        : base(trajectory)
    {
    }

    public Trajectory2DPlotter(IEnumerable<Vector2> trajectory, RGBAColor color)
        : base(trajectory, color)
    {
    }

    public Trajectory2DPlotter(IEnumerable<(IEnumerable<Vector2> trajectory, RGBAColor Color)> trajectories)
        : base(trajectories)
    {
    }

    public Trajectory2DPlotter(params (IEnumerable<Vector2> trajectory, RGBAColor Color)[] trajectories)
        : base(trajectories)
    {
    }

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, IEnumerable<Vector2> trajectory, Pen pen, Brush fill, out List<(Vector2 pos, Vector2 value)> poi)
    {
        if (PlotPoints)
            base.PlotGraph(g, w, h, x, y, s, trajectory, pen, fill, out poi);
        else
            poi = new(); // TODO

        Vector2? last = null;
        float α = 1;

        foreach (Vector2 point in trajectory)
            if (last is null)
                last = point;
            else
            {
                (float px, float py) = ToScreenSpace(point, x, y, s);
                (float lx, float ly) = ToScreenSpace(last.Value, x, y, s);

                if (α < 0.001)
                    break;
                else if (DecayFactor.IsPositiveDefinite)
                {
                    pen = new(new RGBAColor(pen.Color, α), pen.Width);
                    α *= 1 - DecayFactor;
                }

                if (IsInsideScreenSpace((px, py), w, h, PointSize) || IsInsideScreenSpace((lx, ly), w, h, PointSize))
                    g.DrawLine(pen, px, py, lx, ly);

                last = point;
            }
    }
}

public class Heatmap2DPlotter
    : Plotter<Vector2>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Graph_Grid_Axes;
    protected override bool IsPolarPlotter { get; } = false;

    public ColorMap ColorMap { get; set; } = ColorMap.Jet;
    public Scalar MinValue { get; set; } = short.MinValue;
    public Scalar MaxValue { get; set; } = short.MaxValue;
    public bool DynamicMinMaxValue { get; set; } = true;
    public bool UseInterpolation { set; get; } = false;
    public Function<Vector2, Scalar> Function { get; }

    private double[]? _cache = null;


    public Heatmap2DPlotter(Function<Complex, Scalar> function)
        : this(new Function<Vector2, Scalar>(v => function.Evaluate(v)))
    {
    }

    public Heatmap2DPlotter(Function<Vector2, Scalar> function) => Function = function;

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override unsafe void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Vector2 value)> poi)
    {
        bool intp = UseInterpolation;

        double get_value(int u, int v)
        {
            float re = (u - x) / s;
            float im = (v - y) / s;

            return Function[new(re, -im)];
        }
        double get_intp_value(int index)
        {
            int u = index % w;
            int v = index / w;

            return intp ? (get_value(u, v) +
                           get_value(u + 1, v) +
                           get_value(u, v + 1) +
                           get_value(u + 1, v + 1)) / 4
                        : get_value(u, v);
        }

        Debugger.Break(); // TODO : improve performance

        using Bitmap plot = new(w, h, PixelFormat.Format32bppArgb);
        new BitmapLocker(plot).LockRGBAPixels((ptr, _, _) =>
        {
            _cache ??= new double[w * h];

            if (_cache.Length != w * h)
                Array.Resize(ref _cache, w * h);

            double min = MinValue;
            double max = MaxValue;

            if (DynamicMinMaxValue)
            {
                Parallel.For(0, w * h, i => _cache[i] = get_intp_value(i));

                (min, max) = (max, min);

                for (int i = 0; i < w * h; ++i)
                {
                    min = Math.Min(min, _cache[i]);
                    max = Math.Max(max, _cache[i]);
                }

                Parallel.For(0, w * h, i => ptr[i] = ColorMap[_cache[i], min, max]);
            }
            else
                Parallel.For(0, w * h, i => ptr[i] = ColorMap[get_intp_value(i), min, max]);
        });

        g.DrawImageUnscaled(plot, 0, 0);
        poi = new();

        // TODO
    }
}

public class Transformation2DPlotter
    : ComplexFunctionPlotter
{
    public new Function<Vector2> Function { get; }


    public Transformation2DPlotter(Function<Vector2> function)
        : base(new ComplexFunction(c => function[c])) => Function = function;

    // TODO : optional : checkerboard rendering?
}

public static class PlotterSamplingPointGenerator
{
    public static List<Vector2> GenerateSamplingPoints(Scalar width, Scalar height, int count, VectorFieldSamplingMethod method) =>
        GenerateSamplingPoints(width, height, count, method, out _);

    public static List<Vector2> GenerateSamplingPoints(Scalar width, Scalar height, int count, VectorFieldSamplingMethod method, out double mindist)
    {
        List<Vector2> samples = new(count);

        if (method is VectorFieldSamplingMethod.SquareGrid)
        {
            double length = Math.Sqrt(width * height / count);

            for (double i = length * .5; i < width; i += length)
                for (double j = length * .5; j < height; j += length)
                    samples.Add(new(i, j));

            mindist = length;
        }
        else if (method is VectorFieldSamplingMethod.Randomized)
        {
            Random random = Random.BuiltinRandom;

            for (int i = 0; i < samples.Capacity; ++i)
                samples.Add(new(random.NextScalar() * width, random.NextScalar() * height));

            mindist = Math.Sqrt(width * height * .5 / count);
        }
        else if (method is VectorFieldSamplingMethod.HexagonalGrid)
        {
            Scalar φ = Math.Cos(45d.Radians());
            Scalar mh = height / φ;
            double δx = Math.Sqrt(width * mh / count);
            double δy = δx * φ;
            double cx = (int)((width - δx * .5) / δx + 1) * δx;

            for (double i = δx * .5; i < width; i += δx)
                for (double j = δy * .5; j < mh; j += δy)
                    samples.Add(new((i + width + j * φ) % cx, j));

            mindist = δx;
        }
        else
            throw new NotImplementedException();

        return samples;
    }
}

public class VectorFieldPlotter
    : Plotter<Vector2>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = false;

    public Function<Vector2, Vector2> Function { get; }
    public int SampleCount { get; set; } = 100;
    public bool PreventVectorOverlap { get; set; } = false;
    public Scalar PointThickness { get; set; } = 5;
    public Scalar VectorThickness { get; set; } = 3;
    public VectorFieldSamplingMethod SamplingMethod { set; get; } = VectorFieldSamplingMethod.SquareGrid;


    public VectorFieldPlotter(Function<Vector2, Vector2> function) => Function = function;

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Vector2 value)> poi)
    {
        poi = new();

        List<Vector2> samples = PlotterSamplingPointGenerator.GenerateSamplingPoints(w, h, SampleCount, SamplingMethod, out double mindist);
        Vector2[] length = new Vector2[samples.Count];
        Scalar maxlength = Scalar.Zero;

        for (int i = 0; i < samples.Count; ++i)
        {
            Vector2 screen_space = samples[i];
            Vector2 func_space = ToFuncSpace(screen_space, x, y, s);
            Vector2 res_func = Function.Evaluate(func_space);

            length[i] = ToScreenSpace(res_func, 0, 0, s);
            maxlength = maxlength.Max(length[i].Length);

            poi.Add((screen_space, res_func));
        }

        for (int i = 0; i < samples.Count; ++i)
        {
            Vector2 screen_space = samples[i];
            Vector2 len = length[i];
            RGBAColor color = RGBAColor.FromHSV(new Complex(len).Argument, 1, len.Length / maxlength);
            using SolidBrush brush = new(color);
            using Pen pen = new(brush, VectorThickness);

            if (PreventVectorOverlap)
                len *= mindist * .5 / maxlength;

            g.FillEllipse(brush, screen_space.X - PointThickness * .5, screen_space.Y - PointThickness * .5, PointThickness, PointThickness);
            g.DrawLine(pen, screen_space, screen_space + len);
        }
    }
}

public class EvolutionFunctionPlotter<Func>
    : Plotter<Vector2>
    where Func : EvolutionFunction<Vector2>
{
    private (int w, int h, float x, float y, float s) _last;
    private readonly Func<Func> _constructor;
    private MultiPointEvolutionFunction<Func, Vector2>? _function;

    internal protected override PlottingOrder Order { get; } = PlottingOrder.Grid_Graph_Axes;
    protected override bool IsPolarPlotter { get; } = false;

    public MultiPointEvolutionFunction<Func, Vector2> EvolutionFunction => _function ?? throw new InvalidOperationException("The evolution function has not yet been created");

    public int TrajectoryCount { get; set; } = 200;
    public bool DisplayTrajectoryEndPoint { set; get; } = false;
    public bool AreTrajectoriesDecaying { get; set; } = true;
    public int TrajectoryLifetime { get; set; } = 50;
    public Scalar TrajectoryEndSize { get; set; } = 8;
    public Scalar TrajectoryThickness { get; set; } = 3;
    public VectorFieldSamplingMethod SamplingMethod { set; get; } = VectorFieldSamplingMethod.SquareGrid;


    public EvolutionFunctionPlotter()
        : this(Activator.CreateInstance<Func>)
    {
    }

    public EvolutionFunctionPlotter(Func<Func> constructor) => _constructor = constructor;

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Vector2 value)> poi)
    {
        poi = new();

        List<Vector2> samples = PlotterSamplingPointGenerator.GenerateSamplingPoints(w, h, TrajectoryCount, SamplingMethod);

        Parallel.For(0, samples.Count, i => samples[i] = ToFuncSpace(samples[i], x, y, s));

        if (_function is null)
            _function = new(_constructor, samples);
        else if (_last != (w, h, x, y, s))
        {
            _function.UpdateInitialValues(samples);
            _last = (w, h, x, y, s);
        }

        if (_function.CurrentIteration > 0)
            foreach (Func evolution in _function.Evolutions)
            {
                List<Vector2> trajectory = (List<Vector2>)evolution.PastValues;
                RGBAColor color = RGBAColor.FromHue(trajectory[0]);
                using Pen pen = new(color, TrajectoryThickness);

                for (int i = trajectory.Count - 2; i >= 0; --i)
                {
                    Vector2 curr = ToScreenSpace(trajectory[i], x, y, s);
                    Vector2 next = ToScreenSpace(trajectory[i + 1], x, y, s);

                    if (AreTrajectoriesDecaying)
                    {
                        double offs = Math.Max(trajectory.Count - 1 - TrajectoryLifetime, 0);
                        double α = (i - offs) / TrajectoryLifetime;

                        pen.Color = new RGBAColor(color, α);

                        if (α < .003)
                            break;
                    }

                    if (IsInsideScreenSpace(curr, w, h, TrajectoryThickness) || IsInsideScreenSpace(next, w, h, TrajectoryThickness))
                        g.DrawLine(pen, curr, next);
                }

                if (trajectory.Count > 0)
                {
                    Vector2 point = trajectory[^1];
                    Vector2 screen = ToScreenSpace(point, x, y, s);

                    if (DisplayTrajectoryEndPoint && IsInsideScreenSpace(screen, w, h, TrajectoryEndSize))
                        using (SolidBrush brush = new(color))
                            g.FillEllipse(brush, screen.X - .5 * TrajectoryEndSize, screen.Y - .5 * TrajectoryEndSize, TrajectoryEndSize, TrajectoryEndSize);

                    poi.Add((screen, point));
                }
            }
    }
}

public class ImplicitRecurrencePlotter
    : ImplicitFunctionPlotter
{
    public ImplicitRecurrencePlotter(Function<Scalar, Scalar> function, Scalar window_size)
        : this(function, window_size, RGBAColor.Red)
    {
    }

    public ImplicitRecurrencePlotter(Function<Scalar, Scalar> function, Scalar window_size, RGBAColor color)
        : this(window_size, (function, color))
    {
    }

    public ImplicitRecurrencePlotter(Scalar window_size, params (Function<Scalar, Scalar> function, RGBAColor color)[] functions)
        : base(functions.ToArray(t => (new ImplicitFunction<Vector2>(v => t.function[v.Y + window_size] - t.function[v.X]), t.color)))
    {
    }
}

public class DiscretizedRecurrencePlotter
    : Plotter<(Scalar current, Scalar previous)>
{
    internal protected override PlottingOrder Order { get; } = PlottingOrder.Graph_Grid_Axes;
    protected override bool IsPolarPlotter { get; } = false;

    public Scalar WindowSize { get; set; } = 10;
    public Scalar WindowOffset { get; set; } = -5;
    public int WindowResolution { get; set; } = 256;
    public ColorMap ColorMap { get; set; } = ColorMap.Jet;
    public Function<Scalar, Scalar> Function { get; set; }


    public DiscretizedRecurrencePlotter(Function<Scalar, Scalar> function) => Function = function;

    internal protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor) => null; // TODO

    protected internal override unsafe void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, (Scalar current, Scalar previous) value)> poi)
    {
        poi = new();

        int sz = WindowResolution;
        using Bitmap plot = new(sz, sz, PixelFormat.Format32bppArgb);
        Scalar[] fvalues = new Scalar[sz];

        Parallel.For(0, sz, i => fvalues[i] = Function[i * WindowSize / sz - WindowOffset]);

        plot.LockRGBAPixels((ptr, _, _) => Parallel.For(0, sz * sz, i =>
        {
            int yy = i / sz;
            int xx = i % sz;
            Scalar diff = fvalues[sz - 1 - xx] - fvalues[sz - 1 - yy];

            diff = 1 / (100 * diff * diff + 1);

            ptr[yy * sz + xx] = ColorMap[diff];
            ptr[yy * sz + xx].Af = diff;
        }));

        Vector2 pos = ToScreenSpace(new(WindowOffset, -WindowOffset), x, y, s);
        InterpolationMode imode = g.InterpolationMode;

        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.DrawImage(plot, pos.X, pos.Y, WindowSize * s, WindowSize * s);
        g.InterpolationMode = imode;
    }
}

public enum AxisType
{
    Cartesian,
    Polar
}

public enum ComplexColorStyle
{
    Wrapped,
    Smooth,
}

public enum VectorFieldSamplingMethod
{
    SquareGrid,
    HexagonalGrid,
    Randomized,
}
