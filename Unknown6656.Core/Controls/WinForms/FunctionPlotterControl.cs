using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;

using Unknown6656.Imaging;

namespace Unknown6656.Controls.WinForms
{
    public partial class FunctionPlotterControl<P>
        : UserControl
        where P : FunctionPlotter
    {
        private readonly Semaphore _semaphore = new Semaphore(1, 1);
        private readonly Graphics _graphics;


        public P? Plotter { set; get; }


        public FunctionPlotterControl()
        {
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Font;
            _graphics = Graphics.FromHwndInternal(Handle);

            Click += (_, e) =>
            {
                // TODO
            };
            MouseDown += (_, e) =>
            {
                // TODO
            };
            MouseMove += (_, e) =>
            {
                // TODO
            };
            MouseUp += (_, e) =>
            {
                // TODO
            };
            Scroll += (_, e) =>
            {
                // TODO
            };
            SizeChanged += (_, e) => InitiateRedraw();
        }

        public void InitiateRedraw()
        {
            Invalidate(ClientRectangle);
            Update();
        }

        protected override async void OnPaint(PaintEventArgs e)
        {
            if (Plotter is { } p && e.ClipRectangle == ClientRectangle)
                await Task.Factory.StartNew(() =>
                {
                    if (_semaphore.WaitOne(500))
                    {
                        p.Plot(_graphics, e.ClipRectangle.Width, e.ClipRectangle.Height);
                        _semaphore.Release();
                    }
                });
        }
    }
}