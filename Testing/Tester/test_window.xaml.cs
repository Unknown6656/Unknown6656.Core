using System.Windows;
using System.Windows.Input;

using Unknown6656.Controls.WPF;
using Unknown6656.Imaging;
using Unknown6656.Imaging.Plotting;
using Unknown6656.Mathematics.Analysis;

namespace Tester
{
    public partial class test_window
        : Window
    {
        public test_window()
        {
            InitializeComponent();
            plotter.Focus();
            KeyDown += plotter.KeyDown;
        }
    }

    public sealed class plotter : FunctionPlotterControl<CartesianFunctionPlotter<ScalarFunction>>
    {
        public plotter() => Plotter = new(
            (ScalarFunction.Identity, RGBAColor.Black),
            (ScalarFunction.UnitParabola, RGBAColor.Red),
            (ScalarFunction.Sine, RGBAColor.Green)
        );

        public void KeyDown(object? sender, KeyEventArgs args) => base.FunctionPlotterControl_KeyDown(sender, args);
    }
}
