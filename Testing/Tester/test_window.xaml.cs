using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Unknown6656.Controls.WPF;
using Unknown6656.Imaging;
using Unknown6656.Mathematics.Analysis;

namespace Tester
{
    public partial class test_window
        : Window
    {
        public test_window()
        {
            InitializeComponent();
        }
    }

    public sealed class plotter : FunctionPlotterControl<CartesianFunctionPlotter<ScalarFunction>>
    {
        public plotter() => Plotter = new(
            (ScalarFunction.Identity, RGBAColor.Black),
            (ScalarFunction.UnitParabola, RGBAColor.Red),
            (ScalarFunction.Sine, RGBAColor.Green)
        );
    }
}
