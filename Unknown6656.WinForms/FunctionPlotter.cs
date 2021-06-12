using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging;
using Unknown6656.Common;
using System.Reflection;

namespace Unknown6656.Controls.WinForms
{
    using Console = System.Console;

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

        private void FunctionPlotterControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (KeyboardInteractionEnabled)
            {
                bool handled = FunctionExtensions.Do(delegate
                {
                    switch (e.KeyCode)
                    {
                        case Keys.W:
                            _offset -= (0, 1 / _scale);

                            return true;
                        case Keys.A:
                            _offset -= (1 / _scale, 0);

                            return true;
                        case Keys.S:
                            _offset += (0, 1 / _scale);

                            return true;
                        case Keys.D:
                            _offset += (1 / _scale, 0);

                            return true;
                        case Keys.R:
                            _offset = Vector2.Zero;
                            _scale = Scalar.One;

                            return true;
                        case Keys.P:
                            {
                                if (Plotter is FunctionPlotter plotter)
                                    plotter.AxisType = plotter.AxisType is AxisType.Cartesian ? AxisType.Polar : AxisType.Cartesian;
                            }
                            return true;
                        case Keys.X:
                            {
                                if (Plotter is FunctionPlotter plotter)
                                    plotter.AxisVisible ^= true;
                            }
                            return true;
                        case Keys.G:
                            {
                                if (Plotter is FunctionPlotter plotter)
                                    plotter.GridVisible ^= true;
                            }
                            return true;
                        case Keys.C:
                            {
                                if (Plotter is FunctionPlotter plotter)
                                    plotter.CursorVisible ^= true;
                            }
                            return true;
                        case Keys.NumPad1:
                        case Keys.NumPad2:
                        case Keys.NumPad3:
                        case Keys.NumPad4:
                        case Keys.NumPad5:
                        case Keys.NumPad6:
                        case Keys.NumPad7:
                        case Keys.NumPad8:
                        case Keys.NumPad9:
                            {
                                if (Plotter is IMultiFunctionPlotter multi && Math.Min(e.KeyCode - Keys.NumPad1, multi.Functions.Length - 1) is int index and >= 0)
                                    multi.SelectedFunctionIndex = index;
                            }
                            return true;
                        case Keys.D1:
                        case Keys.D2:
                        case Keys.D3:
                        case Keys.D4:
                        case Keys.D5:
                        case Keys.D6:
                        case Keys.D7:
                        case Keys.D8:
                        case Keys.D9:
                            {
                                if (Plotter is IMultiFunctionPlotter multi && Math.Min(e.KeyCode - Keys.D1, multi.Functions.Length - 1) is int index and >= 0)
                                    multi.SelectedFunctionIndex = index;
                            }
                            return true;
                        case Keys.NumPad0:
                        case Keys.D0:
                            {
                                if (Plotter is IMultiFunctionPlotter multi)
                                    multi.SelectedFunctionIndex = -1;
                            }
                            return true;
                        case Keys.H:
                        case Keys.OemQuestion:
                            // TODO : draw help

                            return true;
                        case Keys.Oemplus:
                            _scale *= 1.1;

                            return true;
                        case Keys.OemMinus:
                            _scale /= 1.1;

                            return true;
                    }

                    return false;
                });

                e.Handled = handled;

                if (handled)
                    InitiateRedraw();
            }
        }

        private void FunctionPlotterControl_Load(object? sender, EventArgs e)
        {
            _mouse_initial_delta = VerticalScroll.Value;
        }

        private void FunctionPlotterControl_MouseLeave(object? sender, EventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                _cursorpos = null;

                InitiateRedraw();
            }
        }

        private void FunctionPlotterControl_MouseEnter(object? sender, EventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                _cursorpos = MousePosition;

                InitiateRedraw();
            }
        }

        private void FunctionPlotterControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                _mouse_down = e.Location;
                _last_relative = Vector2.Zero;
                Cursor = Cursors.SizeAll;
            }
        }

        private void FunctionPlotterControl_MouseUp(object? sender, MouseEventArgs e)
        {
            _mouse_down = null;
            Cursor = Cursors.Cross;
            _last_relative = Vector2.Zero;
        }

        private void FunctionPlotterControl_MouseMove(object? sender, MouseEventArgs e)
        {
            Scalar spacing = Plotter?.DefaultGridSpacing ?? 1;

            if (MouseInteractionEnabled && _mouse_down is Vector2 start)
            {
                Vector2 relative = (start.X - e.Location.X, e.Location.Y - start.Y);

                _offset += (relative - _last_relative) / (_scale * spacing);
                _last_relative = relative;
            }
            else
                _cursorpos = (e.Location - new Vector2(ClientSize.Width * .5, ClientSize.Height * .5) + _offset) / (spacing  * _scale);

            InitiateRedraw();
        }

        private void FunctionPlotterControl_MouseWheel(object? sender, MouseEventArgs e)
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

            InitiateRedraw();
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
                    plotter.CursorPosition = (c.X, -c.Y);

                plotter.Plot(g, ClientSize.Width, ClientSize.Height);
                //plotter.Plot(g, e.ClipRectangle.Width, e.ClipRectangle.Height);
            }
            else
                g.Clear(BackColor);
        }
    }

    public sealed class MouseEventArgsExt
        : MouseEventArgs
    {
        public bool IsHorizontal { get; }


        public MouseEventArgsExt(MouseButtons button, int clicks, int x, int y, int delta, bool horizontal)
            : base(button, clicks, x, y, delta) => IsHorizontal = horizontal;
    }
}