using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;
using Unknown6656.Common;
using Unknown6656.Imaging.Plotting;

namespace Unknown6656.Controls.WinForms;


public partial class FunctionPlotterControl<P>
    : UserControl
    where P : FunctionPlotter
{
    private const int WM_MOUSEHWHEEL = 0x020E;

    private readonly Graphics _graphics;
    private int _mouse_initial_delta;
    private Vector2? _mouse_down;
    private Vector2 _last_relative;
    private P? _plotter;

    private Vector2? _cursorpos = null;
    private Vector2 _offset = Vector2.Zero;
    private Scalar _scale = Scalar.One;


    public KeyMap KeyMap { set; get; } = new();
    public Scalar ScrollSpeed { set; get; } = .1;
    public Scalar ZoomSpeed { set; get; } = .1;

    public bool MouseInteractionEnabled { set; get; } = true;
    public bool KeyboardInteractionEnabled { set; get; } = true;


    public P? Plotter
    {
        get => _plotter;
        set
        {
            if (Interlocked.Exchange(ref _plotter, value) is var old && old != value)
            {
                if (value is { } p && old is { })
                {
                    p.AxisVisible = old.AxisVisible;
                    p.GridVisible = old.GridVisible;
                    p.CursorVisible = old.CursorVisible;

                    // TODO : property value transfer
                }

                InitiateRedraw();
            }
        }
    }


    public FunctionPlotterControl()
    {
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Font;
        _graphics = Graphics.FromHwndInternal(Handle);

        MouseDown += FunctionPlotterControl_MouseDown;
        MouseMove += FunctionPlotterControl_MouseMove;
        MouseEnter += FunctionPlotterControl_MouseEnter;
        MouseLeave += FunctionPlotterControl_MouseLeave;
        MouseWheel += FunctionPlotterControl_MouseWheel;
        MouseUp += FunctionPlotterControl_MouseUp;
        KeyDown += FunctionPlotterControl_KeyDown;
        Load += FunctionPlotterControl_Load;
        ClientSizeChanged += (_, _) => InitiateRedraw();
        Cursor = Cursors.Cross;
    }

    protected void FunctionPlotterControl_KeyDown(object? sender, KeyEventArgs e)
    {
        if (KeyboardInteractionEnabled)
        {
            Keys key = e.KeyCode;
            bool handled = LINQ.Do(delegate
            {
                if (key == KeyMap.MoveLeft)
                    _offset -= (1 / _scale, 0);
                else if (key == KeyMap.MoveRight)
                    _offset += (1 / _scale, 0);
                else if (key == KeyMap.MoveUp)
                    _offset -= (0, 1 / _scale);
                else if (key == KeyMap.MoveDown)
                    _offset += (0, 1 / _scale);
                else if (key == KeyMap.ZoomIn)
                    _scale *= 1.1;
                else if (key == KeyMap.ZoomOut)
                    _scale /= 1.1;
                else if (key == KeyMap.ResetView)
                {
                    _offset = Vector2.Zero;
                    _scale = Scalar.One;
                }
                else if (key == KeyMap.SelectNextFunction)
                {
                    if (Plotter is IMultiFunctionPlotter multi)
                        if (multi.SelectedFunctionIndex < multi.Functions.Length - 1)
                            ++multi.SelectedFunctionIndex;
                        else if (multi.SelectedFunctionIndex is null)
                            multi.SelectedFunctionIndex = 0;
                }
                else if (key == KeyMap.SelectPreviousFunction)
                {
                    if (Plotter is IMultiFunctionPlotter multi)
                        if (multi.SelectedFunctionIndex > 0)
                            --multi.SelectedFunctionIndex;
                        else if (multi.SelectedFunctionIndex is null)
                            multi.SelectedFunctionIndex = 0;
                }
                else if (key == KeyMap.SelectNoFunction)
                {
                    if (Plotter is IMultiFunctionPlotter multi)
                        multi.SelectedFunctionIndex = null;
                }
                else if (key == KeyMap.ToggleAxisVisibility)
                {
                    if (Plotter is FunctionPlotter plotter)
                        plotter.AxisVisible ^= true;
                }
                else if (key == KeyMap.ToggleCursorVisibility)
                {
                    if (Plotter is FunctionPlotter plotter)
                        plotter.CursorVisible ^= true;
                }
                else if (key == KeyMap.ToggleGridVisibility)
                {
                    if (Plotter is FunctionPlotter plotter)
                        plotter.GridVisible ^= true;
                }
                else if (key == KeyMap.TogglePolarGrid)
                {
                    if (Plotter is FunctionPlotter plotter)
                        plotter.AxisType = plotter.AxisType is AxisType.Cartesian ? AxisType.Polar : AxisType.Cartesian;
                }
                else
                    return false;

                return true;
            });

            e.Handled = handled;

            if (handled)
                InitiateRedraw();
        }
    }

    protected void FunctionPlotterControl_Load(object? sender, EventArgs e)
    {
        _mouse_initial_delta = VerticalScroll.Value;
    }

    protected void FunctionPlotterControl_MouseLeave(object? sender, EventArgs e)
    {
        if (MouseInteractionEnabled)
        {
            _cursorpos = null;

            InitiateRedraw();
        }
    }

    protected void FunctionPlotterControl_MouseEnter(object? sender, EventArgs e)
    {
        if (MouseInteractionEnabled)
        {
            _cursorpos = MousePosition;

            InitiateRedraw();
        }
    }

    protected void FunctionPlotterControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (MouseInteractionEnabled)
        {
            _mouse_down = e.Location;
            _last_relative = Vector2.Zero;
            Cursor = Cursors.SizeAll;
        }
    }

    protected void FunctionPlotterControl_MouseUp(object? sender, MouseEventArgs e)
    {
        _mouse_down = null;
        Cursor = Cursors.Cross;
        _last_relative = Vector2.Zero;
    }

    protected void FunctionPlotterControl_MouseMove(object? sender, MouseEventArgs e)
    {
        if (MouseInteractionEnabled && _mouse_down is Vector2 start)
        {
            Vector2 relative = (start.X - e.Location.X, e.Location.Y - start.Y);
            Scalar spacing = Plotter?.DefaultGridSpacing ?? 1;

            _offset += (relative - _last_relative) / (_scale * spacing);
            _last_relative = relative;
            _cursorpos = null;
        }
        else
            _cursorpos = e.Location;

        InitiateRedraw();
    }

    protected void FunctionPlotterControl_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (!MouseInteractionEnabled)
            return;

        Scalar delta = (e.Delta - _mouse_initial_delta) / (_scale * SystemInformation.MouseWheelScrollDelta);

        if (ModifierKeys.HasFlag(Keys.Control) || e is MouseEventArgsExt { IsHorizontal: true })
        {
            delta *= ZoomSpeed;
            //_scale += delta;


            // TODO : zoom
        }
        else
        {
            delta *= ScrollSpeed;

            if (ModifierKeys.HasFlag(Keys.Shift)) // horizontal
                _offset += (delta, 0);
            else
                _offset += (0, delta);
        }

        FunctionPlotterControl_MouseMove(sender, e);
    }

    protected override unsafe void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.HWnd == Handle)
            if (m.Msg is WM_MOUSEHWHEEL && MouseInteractionEnabled)
            {
                static int hi(nint ptr) => ((int)ptr >> 16) & 0xffff;
                static int lo(nint ptr) => (int)ptr & 0xffff;

                int tilt = hi(m.WParam);
                int x = lo(m.LParam);
                int y = hi(m.LParam);

                OnMouseWheel(new MouseEventArgsExt(MouseButtons.None, 0, x, y, tilt, true));

                m.Result = (nint)1;
            }

            // TODO : other messages?
    }

    public void InitiateRedraw()
    {
        Invalidate(ClientRectangle);
        Update();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics ?? _graphics;

        if (Plotter is { } plotter)
        {
            plotter.CenterPoint = _offset;
            plotter.Scale = _scale;

            if (_cursorpos is { } c)
                plotter.CursorPosition = new Vector2(c.X - ClientSize.Width * .5, ClientSize.Height * .5 - c.Y) / ((Plotter?.DefaultGridSpacing ?? 1) * _scale) + _offset;

            plotter.Plot(g, ClientSize.Width, ClientSize.Height);
        }
        else
            g.Clear(BackColor);
    }
}

public sealed class KeyMap
{
    public Keys MoveLeft { set; get; } = Keys.Left;
    public Keys MoveRight { set; get; } = Keys.Right;
    public Keys MoveDown { set; get; } = Keys.Down;
    public Keys MoveUp { set; get; } = Keys.Up;
    public Keys ZoomIn { set; get; } = Keys.Oemplus;
    public Keys ZoomOut { set; get; } = Keys.OemMinus;
    public Keys ResetView { set; get; } = Keys.R;
    public Keys TogglePolarGrid { set; get; } = Keys.P;
    public Keys ToggleAxisVisibility { set; get; } = Keys.X;
    public Keys ToggleGridVisibility { set; get; } = Keys.G;
    public Keys ToggleCursorVisibility { set; get; } = Keys.C;
    public Keys SelectPreviousFunction { set; get; } = Keys.B;
    public Keys SelectNoFunction { set; get; } = Keys.N;
    public Keys SelectNextFunction { set; get; } = Keys.M;
}

public sealed class MouseEventArgsExt
    : MouseEventArgs
{
    public bool IsHorizontal { get; }


    public MouseEventArgsExt(MouseButtons button, int clicks, int x, int y, int delta, bool horizontal)
        : base(button, clicks, x, y, delta) => IsHorizontal = horizontal;
}
