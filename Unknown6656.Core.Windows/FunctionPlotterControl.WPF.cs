using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Drawing;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging;
using Unknown6656.Common;
using Unknown6656.IO;

namespace Unknown6656.Controls.WPF
{
    public class FunctionPlotterControl<P>
        : UserControl
        where P : FunctionPlotter
    {
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
            MouseDown += FunctionPlotterControl_MouseDown;
            MouseMove += FunctionPlotterControl_MouseMove;
            MouseEnter += FunctionPlotterControl_MouseEnter;
            MouseLeave += FunctionPlotterControl_MouseLeave;
            MouseWheel += FunctionPlotterControl_MouseWheel;
            MouseUp += FunctionPlotterControl_MouseUp;
            KeyDown += FunctionPlotterControl_KeyDown;
            SizeChanged += (_, _) => InitiateRedraw();
            Cursor = Cursors.Cross;
        }

        private void FunctionPlotterControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (KeyboardInteractionEnabled)
            {
                Key key = e.Key;
                bool handled = FunctionExtensions.Do(delegate
                {
                    if (e.Key == KeyMap.MoveLeft)
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
                        if (Plotter is IMultiFunctionPlotter multi && multi.SelectedFunctionIndex < multi.Functions.Length - 1)
                            ++multi.SelectedFunctionIndex;
                    }
                    else if (key == KeyMap.SelectPreviousFunction)
                    {
                        if (Plotter is IMultiFunctionPlotter { SelectedFunctionIndex: > 0 } multi)
                            --multi.SelectedFunctionIndex;
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

        private void FunctionPlotterControl_MouseLeave(object? sender, MouseEventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                _cursorpos = null;

                InitiateRedraw();
            }
        }

        private void FunctionPlotterControl_MouseEnter(object? sender, MouseEventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                System.Windows.Point point = e.GetPosition(this);

                _cursorpos = (point.X, point.Y);

                InitiateRedraw();
            }
        }

        private void FunctionPlotterControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (MouseInteractionEnabled)
            {
                System.Windows.Point point = e.GetPosition(this);

                _mouse_down = (point.X, point.Y);
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
            System.Windows.Point point = e.GetPosition(this);

            if (MouseInteractionEnabled && _mouse_down is Vector2 start)
            {
                Vector2 relative = (start.X - point.X, point.Y - start.Y);

                _offset += (relative - _last_relative) / (_scale * spacing);
                _last_relative = relative;
            }
            else
                _cursorpos = ((point.X, point.Y) - new Vector2(ActualWidth * .5, ActualHeight * .5) + _offset) / (spacing * _scale);

            InitiateRedraw();
        }

        private void FunctionPlotterControl_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (!MouseInteractionEnabled)
                return;

            Scalar delta = e.Delta / (_scale * 3);

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                delta *= ZoomSpeed;
                //_scale += delta;


                // TODO : zoom
            }
            else
            {
                delta *= ScrollSpeed;

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) // horizontal
                    _offset += (delta, 0);
                else
                    _offset += (0, delta);
            }

            InitiateRedraw();
        }

        public void InitiateRedraw() => InvalidateVisual(); // TODO : ??

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);

            if (Plotter is { } plotter)
            {
                plotter.CenterPoint = _offset;
                plotter.Scale = _scale;

                if (_cursorpos is { } c)
                    plotter.CursorPosition = (c.X, -c.Y);

                using Bitmap bitmap = plotter.Plot((int)ActualWidth, (int)ActualHeight);

                BitmapImage image = new();

                image.BeginInit();
                image.StreamSource = DataStream.FromBitmap(bitmap, System.Drawing.Imaging.ImageFormat.Bmp).ToStream();
                image.EndInit();

                context.DrawImage(image, new(0, 0, bitmap.Width, bitmap.Height));
            }
        }
    }

    public sealed class KeyMap
    {
        public Key MoveLeft { set; get; } = Key.A;
        public Key MoveRight { set; get; } = Key.D;
        public Key MoveDown { set; get; } = Key.S;
        public Key MoveUp { set; get; } = Key.W;
        public Key ZoomIn { set; get; } = Key.OemPlus;
        public Key ZoomOut { set; get; } = Key.OemMinus;
        public Key ResetView { set; get; } = Key.R;
        public Key TogglePolarGrid { set; get; } = Key.P;
        public Key ToggleAxisVisibility { set; get; } = Key.X;
        public Key ToggleGridVisibility { set; get; } = Key.G;
        public Key ToggleCursorVisibility { set; get; } = Key.C;
        public Key SelectPreviousFunction { set; get; } = Key.E;
        public Key SelectNoFunction { set; get; } = Key.R;
        public Key SelectNextFunction { set; get; } = Key.T;
    }
}
