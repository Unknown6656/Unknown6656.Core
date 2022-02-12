using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
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
using Unknown6656.Common;

namespace Unknown6656.Imaging
{
    public abstract class FunctionPlotter
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
        public virtual Scalar DefaultGridSpacing { set; get; } = 30;

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
    }

    public abstract class FunctionPlotterPOI
        : FunctionPlotter
    {
        /// <summary>
        /// Determines whether the points of interest's color (zero points and extrema).
        /// </summary>
        public RGBAColor PointsOfInterestColor { set; get; } = RGBAColor.Firebrick;

        public Scalar PointsOfInterestTolerance { set; get; } = 1e-4;

        /// <summary>
        /// Determines whether the points of interest (zero points and extrema) are visible.
        /// </summary>
        public bool PointsOfInterestVisible { set; get; } = false;
    }

    public abstract class FunctionPlotter<Func, Value>
        : FunctionPlotterPOI
        where Func : FieldFunction<Value>
        where Value : unmanaged, IField<Value>, IComparable<Value>
    {
        #region PROPERTIES / FIELDS

        internal const float MARKING_SIZE = 3;
        internal const int POLAR_DIVISIONS = 8;

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
            PointF center = new((width / 2f) - CenterPoint.X * scale, (height / 2f) + CenterPoint.Y * scale);

            PlotGraph(g, width, height, center.X, center.Y, scale, out List<(Vector2 pos, Value desc)> poi);

            if (GridVisible)
                PlotGrid(g, width, height, center.X, center.Y, scale);

            if (AxisVisible)
                PlotAxis(g, width, height, center.X, center.Y, scale);

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
                using Pen cpen = new(AxisColor, AxisThickness) { DashPattern = new float[] { 3, 6 } };
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
                    float cx = x + cursorx * s;
                    float cy = y - cursory * s;
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
                        float mouse_x = x + CursorPosition.X * s;
                        float mouse_y = y - CursorPosition.Y * s;

                        if (this is { AxisType: AxisType.Polar } and ICartesianPlotter)
                            g.DrawLines(thin, new[] { mouse_y, cy, y }.OrderBy(LINQ.id).ToArray(y => new PointF(mouse_x, y)));
                        else if (this is { AxisType: AxisType.Cartesian } and ICartesianPlotter)
                            g.DrawLine(thin, mouse_x, mouse_y, mouse_x, Math.Abs(y - mouse_y) > Math.Abs(cy - mouse_y) ? cy : y);
                        else if (this is { AxisType: AxisType.Polar } and IPolarPlotter)
                        {
                            if (new Vector2(x - mouse_x, y - mouse_y).Length > new Vector2(x - cx, y - cy).Length)
                                g.DrawLine(thin, cx, cy, mouse_x, mouse_y);
                        }
                        else if (this is { AxisType: AxisType.Cartesian } and IPolarPlotter)
                            if (new Vector2(x - mouse_x, y - mouse_y).Length > new Vector2(x - cx, y - cy).Length)
                                g.DrawLine(thin, x, y, mouse_x, mouse_y);
                            else
                                g.DrawLine(thin, x, y, cx, cy);
                    }

                    if (!(this is { AxisType: AxisType.Cartesian } and ICartesianPlotter { SelectedFunction: { } }))
                        g.DrawString(str, font, brush, cx, cy);
                }
                catch (Exception ex)
                when (ex is OutOfMemoryException or OverflowException)
                {
                    Console.WriteLine($"[unknown6656.core, CRITICAL]\n{ex}");
                }
        }

        protected virtual void PlotPOIs(Graphics g, int w, int h, float x, float y, float s, List<(Vector2 pos, Value value)> poi)
        {
            using Font font = new(FontFamily, FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            using Pen pen = new(PointsOfInterestColor, AxisThickness);
            using Brush brush = new SolidBrush(PointsOfInterestColor);

            var grouped = poi.GroupBy(p => p.pos, new CustomEqualityComparer<Vector2>((v1, v2) => v1.DistanceTo(v2) < 2 * MARKING_SIZE))
                .Select(g => (g.Select(t => (Complex)t.pos).Average(), g.Select(t => t.value).Average(), g.Count()));

            foreach ((Vector2 pos, Value val, int count) in grouped)
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

        protected abstract (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor);

        protected abstract void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Value value)> poi);

        #endregion
    }

    public interface IMultiFunctionPlotter
    {
        Scalar SelectedFunctionThickness { set; get; }
        Scalar FunctionThickness { set; get; }
        int? SelectedFunctionIndex { set; get; }
        public object? SelectedFunction { get; }
        public (object Function, RGBAColor Color)[] Functions { get; }
    }

    public abstract class MultiFunctionPlotter<Func, Value>
        : FunctionPlotter<Func, Value>
        , IMultiFunctionPlotter
        where Func : FieldFunction<Value>
        where Value : unmanaged, IField<Value>, IComparable<Value>
    {
        private int? _selidx = null;


        public (Func Function, RGBAColor Color)[] Functions { get; }

        public Func? SelectedFunction => SelectedFunctionIndex is int index ? Functions[index].Function : null;

        object? IMultiFunctionPlotter.SelectedFunction => SelectedFunction;

        (object Function, RGBAColor Color)[] IMultiFunctionPlotter.Functions => Functions.ToArray(f => (f.Function as object, f.Color));

        public Scalar SelectedFunctionThickness { set; get; } = 3;

        public Scalar FunctionThickness { set; get; } = Scalar.Two;

        public int? SelectedFunctionIndex
        {
            set => _selidx = value is int i ? i >= 0 && i < Functions.Length ? (int?)i : throw new ArgumentOutOfRangeException(nameof(value), $"The function index must be a positive and smaller than {Functions.Length}.") : null;
            get => _selidx;
        }


        public MultiFunctionPlotter(Func function)
            : this(function, RGBAColor.Red)
        {
        }

        public MultiFunctionPlotter(Func function, RGBAColor color)
            : this((function, color)) => SelectedFunctionIndex = 0;

        public MultiFunctionPlotter(params (Func Function, RGBAColor Color)[] functions)
        {
            Functions = functions;
            SelectedFunctionIndex = null;
        }

        // public void AddFunction(Func function, RGBAColor color)
        // public bool RemoveFunction(Func function)
        // public void RemoveFunction(int index)
        // TODO
    }

    // TODO : implicit cartesian/polar plotter

    internal interface IPolarPlotter
        : IMultiFunctionPlotter
    {
    }

    internal interface ICartesianPlotter
        : IMultiFunctionPlotter
    {
    }

    public class CartesianFunctionPlotter<Func>
        : MultiFunctionPlotter<Func, Scalar>
        , ICartesianPlotter
        where Func : FieldFunction<Scalar>
    {
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

        protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
        {
            if (SelectedFunctionIndex is int i)
            {
                Func f = Functions[i].Function;
                Scalar y = f[cursor.X];
                Scalar h = (Scale * DefaultGridSpacing).Inverse;

                h = .5 * (f[cursor.X + h] - f[cursor.X - h]) / h;

                return ((cursor.X, y), Functions[i].Color, y.ToString(), h.Atan());
            }

            return null;
        }

        protected override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Scalar value)> poi)
        {
            poi = new List<(Vector2, Scalar)>();

            for (int idx = 0; idx < Functions.Length; ++idx)
            {
                (Func f, RGBAColor c) = Functions[idx];
                using Pen pen = new(c, idx == SelectedFunctionIndex ? SelectedFunctionThickness : FunctionThickness);
                float last = y - f[-x / s] * s;
                float curr = last;
                Scalar fx;

                for (int i = 0; i <= w; ++i)
                {
                    fx = f[(i - x) / s];
                    curr = y - fx * s;

                    if (last >= -s && last <= h + s)
                        g.DrawLine(pen, i - 1, last, i, curr);

                    last = curr;

                    if (Math.Abs(curr) < PointsOfInterestTolerance)
                        poi.Add(((i, curr), fx));
                }
            }
        }
    }

    public class PolarFunctionPlotter<Func>
        : MultiFunctionPlotter<Func, Scalar>
        , IPolarPlotter
        where Func : FieldFunction<Scalar>
    {
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

        protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
        {
            if (SelectedFunctionIndex is int i)
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

        protected override void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Scalar value)> poi)
        {
            poi = new List<(Vector2, Scalar)>();

            for (int idx = 0; idx < Functions.Length; ++idx)
            {
                (Func f, RGBAColor c) = Functions[idx];
                using Pen pen = new(c, idx == SelectedFunctionIndex ? SelectedFunctionThickness : FunctionThickness);
                Scalar rad = new Vector2(w, h).SquaredLength;
                Scalar last = f[MinAngle] * s;
                Scalar curr = last;

                for (Scalar φ = MinAngle + AngleStep; φ <= MaxAngle; φ += AngleStep)
                {
                    curr = f[φ] * s;

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
    }

    public class ComplexFunctionPlotter<Func>
        : FunctionPlotter<Func, Complex>
        where Func : FieldFunction<Complex>
    {
        public ComplexColorStyle Style { set; get; } = ComplexColorStyle.Wrapped;
        public bool PhaseLinesVisible { set; get; } = false;
        public RGBAColor PhaseLineColor { set; get; } = RGBAColor.White;
        public int PhaseLineSteps { set; get; } = POLAR_DIVISIONS * 4;
        public Scalar PhaseLineTolerance { set; get; } = 1e-2;
        public bool UseInterpolation { set; get; } = false;
        public Func Function { get; }


        public ComplexFunctionPlotter(Func function) => Function = function;

        protected override (Vector2 Position, RGBAColor Color, string? Value, Scalar DerivativeAngle)? GetInformation(Vector2 cursor)
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

        protected override unsafe void PlotGraph(Graphics g, int w, int h, float x, float y, float s, out List<(Vector2 pos, Complex value)> poi)
        {
            ConcurrentBag<(Vector2, Complex)> bag = new();
            Func<Complex, RGBAColor> color = Style == ComplexColorStyle.Wrapped ? (Func<Complex, RGBAColor>)RGBAColor.FromComplexWrapped : RGBAColor.FromComplexSmooth;
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
                    Scalar r = ((res.Argument + PhaseLineTolerance + Scalar.Tau) % Scalar.Tau) % phasediv;

                    if (r <= 2 * PhaseLineTolerance)
                    {
                        r *= Scalar.Pi / 2 / PhaseLineTolerance;
                        col = Vector3.LinearInterpolate(col, PhaseLineColor, r.Sin().Power(3));
                        col *= len.Min(1);
                    }
                }

                if (PointsOfInterestVisible && (len.Abs() < PointsOfInterestTolerance /*|| len.Abs().Inverse > PointsOfInterestTolerance*/))
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

            g.DrawImage(plot, 0, 0);

            poi = bag.ToList();
        }
    }

    public class Transformation2DPlotter<Func>
        : ComplexFunctionPlotter<ComplexFunction>
        where Func : Function<Vector2>
    {
        public new Func Function { get; }


        public Transformation2DPlotter(Func function)
            : base(new ComplexFunction(c => function[c])) => Function = function;
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
}
