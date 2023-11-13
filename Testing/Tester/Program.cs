using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Unknown6656.Mathematics.Graphs.Computation;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Graphs;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Geometry;
using Unknown6656.Controls.WinForms;
using Unknown6656.Controls.Console;
using Unknown6656.Imaging.Plotting;
using Unknown6656.Imaging.Effects;
using Unknown6656.Imaging.Effects.Instagram;
using Unknown6656.Imaging.Video;
using Unknown6656.Imaging;
using Unknown6656.Generics;
using Unknown6656.Runtime;
using Unknown6656.Common;
using Unknown6656.IO;
using Unknown6656;

using Random = Unknown6656.Mathematics.Numerics.Random;
using winforms = System.Windows.Forms;

namespace Testing;


static class test
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void m0(this string? s);
    public static void m1(object a) { }
    public static void m2(object? a) { }
    public static void m5(object? a, object? b) { }
    public static object? m6(object? a, object? b) => a;
    public static object[]? m8(object?[] a) => null;
    public static object?[] m9(object[]? a) => a;
    public static object?[]? m10(object?[]? a) => a;
    public static void m11((object?, (object, object))? a) { }

    public static void m12(A<object>? a) { }
    public static void m13(A<object?> a) { }
    public static void m14(A<object?>? a) { }
    public static void m15(A<A<A<A<object?>>>> a) { }

    public static Task m16() => Task.CompletedTask;
    public static async Task m17() => await Task.CompletedTask;

    public class A<T>{}
}

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Main_BMP_effects();

        return;
        var prov = new CSharpSignatureProvider();

        typeof(test).GetMembers().Append(typeof(test)).Do(m => Console.WriteLine(prov.GenerateSignature(m) + "\n"));

        //Main_PSO();
    }

    //class pso_problem : PSOProblem<Scalar>
    //{
    //    public override int Dimensionality => 1;
    //    public override Scalar GetValue(VectorN position) { Scalar x = position[0]; return (x - 3).Tanh() + x * x - x * x * x; }
    //    public override bool IsValidSearchPosition(VectorN position) => true;
    //}
    //public static void Main_PSO()
    //{
    //    var pp = new Unknown6656.Computation.ParticleSwarmOptimization.

    //    var p = new pso_problem();
    //    var c = new PSOSolverConfiguration
    //    {
    //    };
    //    var s = p.CreateSolver(c);
    //    var o = s.Solve();
    //    var v = o.OptimalValue;
    //}

    public static void Main_QOIF()
    {
        var file = "img2.png";
        var img = ((Bitmap)Image.FromFile(file)).ToARGB32();

        img.ApplyEffect(new QOIFCorruptedEffect(20) { FormatVersion = QOIFVersion.V2 })
           .Save("conv.png");

        //img.SaveQOIFImage(file + ".qoi", QOIFVersion.Original);
        //DataStream.FromFile(file + ".qoi").ToQOIFBitmap().Save(file + "-qoi-v1.png");
        //img.SaveQOIFImage(file + ".qoi2", QOIFVersion.V2);
        //DataStream.FromFile(file + ".qoi2")/*.HexDump()*/.ToQOIFBitmap().Save(file + "-qoi-v2.png");
    }

    public static void Main_BMP_effects_3()
    {
        var reg = (360..1560, 200..880);
        var img = ((Bitmap)Image.FromFile("img4.png")).ToARGB32();
        var sw = Stopwatch.StartNew();


        for (int i = 0; i <= 50; ++i)
            img.ApplyEffect(
                new JPEGCompressionEffect(i / 50d)
            ).Save($"compr-{i:d2}.png");

        sw.Stop();
        Console.WriteLine($"effects: {sw.ElapsedMilliseconds:F2} ms");
        sw.Restart();
        sw.Stop();
        Console.WriteLine($"saving: {sw.ElapsedMilliseconds:F2} ms");
    }

    public static void Main_BMP_effects_2()
    {
        var reg = (360..1560, 200..880);
        var img = ((Bitmap)Image.FromFile("img4.png")).ToARGB32();
        var sw = Stopwatch.StartNew();
        
        foreach (var fx in new InstagramFilter[]
        {
            InstagramFilter._1977,
            InstagramFilter.Aden,
            InstagramFilter.Ashby,
            InstagramFilter.Brannan,
            InstagramFilter.Brooklyn,
            InstagramFilter.Clarendon,
            InstagramFilter.Crema,
            InstagramFilter.Charmes,
            InstagramFilter.Dogpatch,
            InstagramFilter.Ginza,
            //InstagramFilter.Hefe,
            InstagramFilter.Helena,
            InstagramFilter.Ludwig,
            //InstagramFilter.Poprocket,
            //InstagramFilter.Sierra,
            InstagramFilter.Sutro,
            InstagramFilter.Vesper,
            InstagramFilter.Earlybird,
            InstagramFilter.Gingham,
            InstagramFilter.Hudson,
            InstagramFilter.Inkwell,
            InstagramFilter.Juno,
            InstagramFilter.Kelvin,
            InstagramFilter.Lark,
            //InstagramFilter.Lofi,
            InstagramFilter.Maven,
            //InstagramFilter.Mayfair,
            InstagramFilter.Moon,
            //InstagramFilter.Perpetua,
            //InstagramFilter.Reyes,
            //InstagramFilter.Rise,
            InstagramFilter.Slumber,
            InstagramFilter.Stinson,
            //InstagramFilter.Toaster,
            //InstagramFilter.Valencia,
            //InstagramFilter.Walden,
            //InstagramFilter.Willow,
            InstagramFilter.XPro2,
            InstagramFilter.LegacyNashville,
        })
        {
            string name = fx.GetType().Name;
            Console.WriteLine(name);
            img.ApplyEffect(fx, reg).Save($"ig-{name}.png");
        }

        sw.Stop();
        Console.WriteLine($"effects: {sw.ElapsedMilliseconds:F2} ms");
        sw.Restart();
        sw.Stop();
        Console.WriteLine($"saving: {sw.ElapsedMilliseconds:F2} ms");
    }

    public static void evaluate_random<random>()
        where random : Random, new()
    {
        int runs = 30_000_000;
        int[] buckets = new int[50];
        var rng = new random();
        for (int i = 0; i < runs; ++i)
            ++buckets[(int)(rng.NextDouble() * buckets.Length)];
        int total = buckets.Sum();

        Console.WriteLine($"{typeof(random)}:\n-------------------------\n bucket       variation from expected probability");
        Console.WriteLine(buckets.Select((v, i) => $"{i,3} => {((double)v / total * buckets.Length - 1) * 100,20:F4} %").StringJoin("\n"));
    }

    public static void Main_Statistics()
    {
        XorShift sh = new();
        int i = 20;

        while (i-- > 0)
        {
            string s = DataStream.FromBytes(sh.NextBytes(512)).ToDrunkBishop(100, 50, " .,-~+=´'*\"/!?lI$#&%@BGWO", false);
            Console.Clear();
            Console.WriteLine(s);
            System.Threading.Thread.Sleep(200);
        }
    }

    public static void Main_ConsoleUI()
    {
        for (int i = 0; i < 56; ++i)
            Console.WriteLine($"{i,3}:\x1b[{i}m top kkk kek      lol \x1b[0m");
        for (int i = 0; i < 256; ++i)
        {
            // Console.Write($"\x1b[38;5;{i}m█\x1b[0m"); // fg
            Console.Write($"\x1b[48;5;{i}m {i:x2}\x1b[0m");
            if (((i + 1) % 16) == 0)
                Console.WriteLine();
        }



        ControlHost host = new();

        host.AddNewChild<StackPanel>(p =>
        {
            p.Position = new Point(5, 5);
            p.Orientation = Orientation.Vertical;

            p.AddNewChild<Button>(c =>
            {
                c.Text = "topkek jej";
                c.RelativeTabIndex = 1;
                c.Clicked += _ => c.Text = c.Text.Length > 20 ? "topkek jej" : c.Text + "_";
            });
            p.AddNewChild<Button>(c =>
            {
                c.Text = "lb2";
                c.Foreground = RGBAColor.Cyan;
                c.RelativeTabIndex = 2;
            });
            p.AddNewChild<Button>(c =>
            {
                c.Text = "idx3";
                c.Foreground = RGBAColor.Green;
                c.RelativeTabIndex = 3;
            });
            p.AddNewChild<TextBox>(c =>
            {
                c.Text = "content lol";
                c.Foreground = RGBAColor.Yellow;
                c.RelativeTabIndex = 4;
            });
            p.AddNewChild<CheckBox>(c =>
            {
                c.Text = "cb1";
                c.Foreground = RGBAColor.Yellow;
            });
            p.AddNewChild<CheckBox>(c =>
            {
                c.Text = "\x7f";
            });
            p.AddNewChild<OptionBox>(c =>
            {
                c.Text = "Select lel";
                c.Options = ["top", "kaikokek", "jej !null", "ebin h4x"];
            });
            p.AddNewChild<ProgressBar>(c =>
            {
                c.Width = 102;
                c.DisplayPercentage = true;
                c.Foreground = RGBAColor.YellowGreen;
            });
            p.AddNewChild<ColorPicker>();
        });
        host.KeyMapping[KeyAction.Quit] = ConsoleKey.Q;
        host.FocusedControlChanged += (_, s) => host.Text = s?.ToString();
        host.SizeChanged += (_, s) => host.Text = s.ToString();
        host.Run();

        Console.Write("new");
    }

    public static void Main_Math()
    {
        Fraction f = Scalar.Pi.ToFraction(1e-300);

        var s = f.ToPrettyString();

        Matrix3 m = (
            1, 0, 0,
            2, 1, 0,
            3, 5, 1
        );
        Polynomial p = m.CharacteristicPolynomial!;
        var r = p.Degree;
        var d = p.Derivative;

        var x1 = p + d;
        var x2 = p * d;
        var x3 = p / d;
        var x4 = p % d;
        var x5 = p - d;

    }

    public static void Main_BMP1()
    {
        double total = 1000;

        VideoAssembler.CreateVideo(
            new FileInfo("render.mp4"),
            (int)total,
            new(1920, 1080),
            (index, frame) =>
            {
                double time = index / total;
                using Graphics g = Graphics.FromImage(frame);

                new ComplexFunctionPlotter(
                    new(c =>
                    {
                        return (c * c - time) * ((c - (2, 1 - 2 * time)) ^ 2) / (c * c + (2 * time, 2 * time)) + time;
                    })
                )
                {
                    Scale = 2,
                    AxisType = AxisType.Polar,
                    AxisVisible = true,
                    AxisColor = RGBAColor.Black,
                    GridVisible = false,
                    OptionalComment = ($"", RGBAColor.Black),
                }
                .Plot(g, frame.Width, frame.Height);
            },
            new VideoAssemblerOptions
            {
                FrameRate = 60,
                Parallelized = true,
            }
        );
    }

    public static void Main_heatmap_plotter_ui()
    {
        var lol = new XorShift().NextScalars(500);
        var f = new ScalarFunction(x => lol[(int)((x % lol.Length + lol.Length) % lol.Length)]);

        using var plotter = new FunctionPlotterControl<DiscretizedRecurrencePlotter>()
        {
            Dock = winforms.DockStyle.Fill,
            Plotter = new(f)
            {
                CursorVisible = true,
                PointsOfInterestVisible = false,
                DefaultGridSpacing = 50,
                WindowResolution = 500,
                WindowSize = 20,
                BackgroundColor = RGBAColor.Black,
                AxisColor = RGBAColor.DarkGray,
                GridVisible = false,
            },
        };
        using var form = new winforms.Form()
        {
            Width = 800,
            Height = 600,
            BackColor = Color.Teal,
        };
        bool open = true;
        Task.Factory.StartNew(async delegate
        {
            await Task.Delay(500);

            while (open)
            {
                await Task.Delay(10);

                plotter.Plotter.WindowOffset = Math.Sin(DateTime.Now.Ticks * .00000005) * 1 - plotter.Plotter.WindowSize * .5;
                plotter.Invoke(plotter.Invalidate);
            }
        });
        form.Controls.Add(plotter);
        form.ShowDialog();
        open = false;
    }

    public static void Main_evolution_plotter_ui()
    {
        using var plotter = new FunctionPlotterControl<EvolutionFunctionPlotter<EvolutionFunction2D>>()
        {
            Dock = winforms.DockStyle.Fill,
            //Plotter = new(() => new(v => (v.Rotate(.1 / v.Length) + (v.Angle * .1).Sin() * .1) * .999))
            Plotter = new(() => EvolutionFunction2D.HenonMap(1.4, 0.3))
            {
                CursorVisible = true,
                PointsOfInterestVisible = false,
                SamplingMethod = VectorFieldSamplingMethod.HexagonalGrid,
                DefaultGridSpacing = 50,
                TrajectoryLifetime = 60,
                TrajectoryCount = 200,
                TrajectoryThickness = 1,
                //DisplayTrajectoryEndPoint = true,
                BackgroundColor = RGBAColor.Black,
                AxisColor = RGBAColor.DarkGray,
                GridVisible = false,
            },
        };
        using var form = new winforms.Form()
        {
            Width = 800,
            Height = 600,
            BackColor = Color.Teal,
        };
        bool open = true;
        Task.Factory.StartNew(async delegate
        {
            await Task.Delay(500);

            while (open)
            {
                await Task.Delay(10);

                plotter.Plotter.EvolutionFunction.Iterate();
                plotter.Invoke(plotter.Invalidate);
            }
        });
        form.Controls.Add(plotter);
        form.ShowDialog();
        open = false;
    }

    public static void Main_complex_plotter_ui()
    {
        //using var f = new Form
        //{
        //    Width = 500,
        //    Height = 300,
        //};
        //f.Controls.Add(new FunctionPlotterControl<ComplexFunctionPlotter<ComplexMap>>
        //{
        //    Dock = DockStyle.Fill,
        //    Plotter = new ComplexFunctionPlotter<ComplexMap>(
        //        new ComplexMap(x => x.Acos().Multiply(10).Cos() + (8, 4) - x * x * x)
        //    )
        //    {
        //        Scale = 12,
        //        AxisType = AxisType.Cartesian,
        //        AxisVisible = true,
        //        GridVisible = false,
        //        CursorVisible = true,
        //        CursorPosition = (-2, -2),
        //        CursorColor = RGBAColor.Blue,
        //    },
        //});
        //f.ShowDialog();


        using var plotter = new FunctionPlotterControl<CartesianFunctionPlotter<ScalarFunction>>()
        {
            Dock = winforms.DockStyle.Fill,
            Plotter = new(new[] {
                (ScalarFunction.Sine, RGBAColor.Green),
                (ScalarFunction.UnitParabola, RGBAColor.Cyan),
                (new ScalarFunction(Scalar.Sqrt), RGBAColor.Red),
            })
            {
                CursorVisible = true,
            },
        };
        using var form = new winforms.Form()
        {
            Width = 800,
            Height = 600,
            BackColor = Color.Teal,
        };
        form.Controls.Add(plotter);
        form.ShowDialog();

    }

    public static void Main_implicit_plotter_ui()
    {
        var f1 = ImplicitScalarFunction2D.Cartesian(new(Scalar.Cos))
               * ImplicitScalarFunction2D.Cartesian(new(x => .95 - .5 * x * x))
               * ImplicitScalarFunction2D.Cartesian(new(x => .5 * (x + Scalar.Pi).Power(2) - .95))
               * ImplicitScalarFunction2D.Cartesian(new(x => .5 * (x - Scalar.Pi).Power(2) - .95));
        var f2 = ImplicitScalarFunction2D.Rectangle((0, 0), 5, 3)
               * ImplicitScalarFunction2D.Cartesian(new(x => x % 1));
        var f3 = ImplicitScalarFunction2D.RoundHeart().Scale(2).Shift((0, -1))
               * ImplicitScalarFunction2D.Heart().Shift((-5, -2))
               * ImplicitScalarFunction2D.Heart().Shift((5, -2))
               * ImplicitScalarFunction2D.Heart().Shift((-4, 2))
               * ImplicitScalarFunction2D.Heart().Shift((4, 2));
        var cm = ColorMap.Jet + ColorMap.Jet.Reverse();

        f1 = ImplicitScalarFunction2D.RoundHeart().Scale(2).Shift((0, -1))
               * ImplicitScalarFunction2D.Heart().Shift((-5, -2));
        f2 = ImplicitScalarFunction2D.Cartesian(ScalarFunction.Sine);
        f3 = new ImplicitScalarFunction2D(v => v.X.Exp().Sin() - v.Y * v.Y);

        int count = 40;
        for (int i = 0; i <= count; ++i)
        {
            var p0 = i / (float)count;


            // ImplicitScalarFunction2D f(Scalar p) => new(p <= .5 ? ImplicitScalarFunction2D.LinearInterpolate(f1, f2, p * 2)
            //                                                     : ImplicitScalarFunction2D.LinearInterpolate(f2, f3, p * 2 - 1));
            ImplicitScalarFunction2D f(Scalar p) => new(ImplicitScalarFunction2D.LinearInterpolate(f1, f3, p));




            var p1 = AnimationFunction.Smoothstep[p0];
            var p2 = AnimationFunction.Smootherstep[p0];
            var p3 = AnimationFunction.Sin_01[p0];


            new ImplicitFunctionPlotter(
                //ImplicitScalarFunction2D.StretchBlend(f1, new(f2), p3)
                (f(p0), RGBAColor.Red),
                (f(p1), RGBAColor.Purple),
                (f(p2), RGBAColor.Orange),
                (f(p3), RGBAColor.Blue)
            )
            {
                //ColorMap = cm,
                //DisplayOverlayFunction = true,

                //MarchingSquaresPixelStride = 3,
                Scale = 4,
                // FunctionThickness = 3,
                AxisType = AxisType.Cartesian,
                AxisVisible = true,
                GridVisible = false,
                CursorVisible = false,
                //OptionalComment = ($"{2 + b:F4} * x * cos(x + sin(2y) + {a:F4} * y^2) - y^3 + {c:F4} / x - sin(10x)", RGBAColor.Firebrick),
            }
            .Plot(1920, 1080)
            .Save($"anim/frame-{i:D4}.png");

            //return;
        }

        //using var plotter = new FunctionPlotterControl<ImplicitCartesianFunctionPlotter>()
        //{
        //    Dock = winforms.DockStyle.Fill,
        //    Plotter = new(
        //        new ImplicitScalarFunction2D((x, y) => y * (y + x + x * x).Cos() - x.Power(3))
        //    )
        //    {
        //        CursorVisible = true,
        //    },
        //};
        //using var form = new winforms.Form()
        //{
        //    Width = 800,
        //    Height = 600,
        //    BackColor = Color.Teal,
        //};
        //form.Controls.Add(plotter);
        //form.ShowDialog();
    }

    public static void Main_complex_plotter_bmp()
    {
        new ComplexFunctionPlotter(
            new(c =>
            {
                var z = c;

                c *= (0, 1);

                var c2 = c * c;
                var c3 = c2 * c;

                var y = (c2 - 2) * (c - 3) * (-c2) / (c3 - Complex.i);
                var x = y;

                x /= c.Sin() - (1, 1) + c3;
                x *= (0, 1);
                x += c.Sin() * .3
                   + (c * (0, -1)).Cos();
                x += y / 4;

                x -= (z * z - 2) * (z - 3) * (-z * z) / (z * z * z - Complex.i);

                return x;
            })
        )
        {
            Scale = 7,
            AxisType = AxisType.Polar,
            AxisVisible = false,
            AxisColor = RGBAColor.White,
            GridVisible = false,
        }
        .Plot(1920 * 2, 1080 * 2)
        .Save("conv.png");
    }

    public static void Main_BMP_complex_function_plotter_animation()
    {
        for (int i = 0; i <= 800; ++i)
        {
            var (c, s) = i switch
            {
                int _ when i < 200 => (i / 20d, 0d),
                int _ when i < 400 => (10d, (i - 200) / 20d),
                int _ when i < 600 => ((600 - i) / 20d, 10d),
                _ => (0d, (800 - i) / 20d)
            };
            new ComplexFunctionPlotter(
                new(x => x.Acos().Multiply(c).Cos() + x.Asin().Multiply(s).Sin())
            )
            {
                Scale = 12,
                AxisType = AxisType.Cartesian,
                AxisVisible = true,
                GridVisible = false,
                CursorVisible = false,
                CursorPosition = (-2, -2),
                CursorColor = RGBAColor.Blue,
                OptionalComment = ($"cos({c:F2} * arccos(c)) + sin({s:F2} * arcsin(c))", RGBAColor.Black)
            }
            .Plot(1920, 1080)
            .Save($"anim-5/frame-{i:D4}.png");
        }
    }

    public static void Main_BMP_mandelbrot_plotter()
    {
        int i = 0;

        for (double f = -2; f <= 2; f += 5e-3)
            new ComplexFunctionPlotter(
                new(c => c.ApplyRecursively(z => z.Power(f) + c, 5))
            )
            {
                Scale = 15,
                AxisType = AxisType.Polar,
                AxisVisible = true,
                GridVisible = false,
                OptionalComment = ($"(z, c) -> z^{f:F3} + c", RGBAColor.Black)
            }
            .Plot(1920, 1080)
            .Save($"mandelbrot-3/frame-{++i:D4}.png");
    }

    public static void Main_BMP_draw_triangles()
    {
        var sw = new Stopwatch();
        sw.Start();

        var img = ((Bitmap)Image.FromFile("img2.png")).ToARGB32();
        var s2dr = img.GetShape2DRasterizer();
        var tri =
        (
            .5, 0, .3,
            0, .5, 0,
            0, 0, 1
        ) * new Triangle2D(
            (-.5, -.2),
            (.5, .5),
            (.2, -.7)
        );
        //new AxisAlignedRectangle((.1,.1),(.9,.9));


        s2dr.Draw(new DrawableShape[]
        {
            new Line2D((-1, 0), (1, 0)),
            new Line2D((0, -1), (0, 1)),
            tri,
            tri.Rotate(Scalar.Pi/2)
        }, new ShapeDrawingSettings
        {
            LineColor = 0xff00,
            FillColor = 0x40f0,
        });
        s2dr.Draw(tri.Sides.SelectMany(s => Enumerable.Range(0, 11).Select(i => tri.GetNormalAt(s.Interpolate(i / 10d)))), new ShapeDrawingSettings { LineColor = RGBAColor.Blue });

        img.Save("conv.png");
        sw.Stop();

        Console.WriteLine($"{sw.ElapsedMilliseconds:F2} ms");
    }

    public static void Main_BMP_plot_transformation_function()
    {
        new Transformation2DPlotter(
            // (Polynomial.Parse(".1x^2-5"), RGBAColor.Green),
            // (Polynomial.Parse(".01x³-.2x²+3"), RGBAColor.Firebrick)
            new(v => new Matrix2(1, 0, 0, -1).Multiply(v).Normalized - v.Normalized)
        )
        {
            Scale = 5,
            AxisType = AxisType.Cartesian,
            AxisVisible = true,
            GridVisible = false,
            CursorVisible = false,
            CursorPosition = (-2, -2),
            CursorColor = RGBAColor.Blue,
            // SelectedFunctionIndex = 0
        }
        .Plot(1920, 1080)
        //.ToMask(c =>
        //{
        //    c.ToHSL(out double h, out _, out _);
        //
        //    return Scalar.Cos(h + Scalar.Pi).Divide(Scalar.Two) + .5;
        //})
        //.Colorize(ColorMap.Spectral)
        .Save("conv.png");
    }

    public static void Main_BMP_dithering()
    {
        var reg = (.., ..); // (960.., ..);
        var img = ((Bitmap)Image.FromFile("img4.png")).ToARGB32();
        var pal = ColorPalette.PrimaryAndComplementaryColors;

        foreach (var alg in new[] {
            ErrorDiffusionDitheringAlgorithm.Thresholding,
            ErrorDiffusionDitheringAlgorithm.Burkes,
            ErrorDiffusionDitheringAlgorithm.JarvisJudiceNinke,
            ErrorDiffusionDitheringAlgorithm.Stucki,

            ErrorDiffusionDitheringAlgorithm.HilbertCurve,
            ErrorDiffusionDitheringAlgorithm.FloydSteinberg,
            ErrorDiffusionDitheringAlgorithm.FalseFloydSteinberg,
            ErrorDiffusionDitheringAlgorithm.Atkinson,
            ErrorDiffusionDitheringAlgorithm.Randomized,
            ErrorDiffusionDitheringAlgorithm.Simple,
            ErrorDiffusionDitheringAlgorithm.Sierra,
            ErrorDiffusionDitheringAlgorithm.SierraTwoRow,
            ErrorDiffusionDitheringAlgorithm.SierraLite,
            ErrorDiffusionDitheringAlgorithm.TwoDimensional,

            //ErrorDiffusionDitheringAlgorithm.GradientBased,
        })
        {
            Console.WriteLine(alg);
            img.ApplyEffect(new ErrorDiffusionDithering(alg, pal), reg).Save($"dithering-{alg}.png");
        }

        foreach (var alg in new[] {
            OrderedDitheringAlgorithm.Halftone,
            OrderedDitheringAlgorithm.Bayer,
            OrderedDitheringAlgorithm.Bayer2,
            OrderedDitheringAlgorithm.Bayer3,
            OrderedDitheringAlgorithm.DispersedDots_8,
            OrderedDitheringAlgorithm.DispersedDots_6,
            OrderedDitheringAlgorithm.DispersedDots_4,
            OrderedDitheringAlgorithm.DispersedDots_3,
            OrderedDitheringAlgorithm.DispersedDots_2,

            OrderedDitheringAlgorithm.Ordered_2x8,
            OrderedDitheringAlgorithm.Ordered_8x2,
            OrderedDitheringAlgorithm.WavyHatchet_16,
        })
        {
            Console.WriteLine(alg);
            img.ApplyEffect(new BlackWhiteOrderedDithering(alg), reg).Save($"dithering-bw-{alg}.png");
            img.ApplyEffect(new ColoredOrderedDithering(alg, 16), reg).Save($"dithering-col-{alg}.png");
        }
    }

    public static void Main_BMP_colormaps()
    {
        ColorMap[] maps = typeof(ColorMap).GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty)
                                          .Select(p => (p as PropertyInfo)?.GetValue(null) as ColorMap)
                                          .Where(m => m is { })
                                          .ToArray()!;
        Bitmap img = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            .ApplyEffect(new LinearGradient(Vector2.Zero, (0, 1080), RGBAColor.Black, RGBAColor.White));
        int width = img.Width / maps.Length;

        for (int i = 0; i < maps.Length; ++i)
            img = img.ApplyEffect(new Colorize(maps[i]), ((i * width)..((i + 1) * width), ..));

        img.Save("conv.png");
    }

    public static void Main_BMP_effects()
    {
        var sw = new Stopwatch();
        sw.Start();


        var reg = /*(360..1560, 200..880); */(960.., ..);

        var im1 = ((Bitmap)Image.FromFile("img1.png")).ToARGB32();
        var im2 = ((Bitmap)Image.FromFile("img2.png")).ToARGB32();
        var im3 = ((Bitmap)Image.FromFile("img3.png")).ToARGB32();
        Bitmap img = im2;

        // img = BitmapMask.FromLuma(im1).ApplyTo(im1).ApplyEffect(new BlendEffect(im2, BlendMode.Screen, 1));
        //img = im2.ApplyEffect(new ChainedPartialBitmapEffect(
        //    new ColorKey(0xf000, 0),
        //    new MultiPointGradient(
        //        ((500, 200), 0xff00),
        //        ((1000, 200), 0xf00f),
        //        ((700, 900), 0xf0f0)
        //    ),
        //    new PerlinNoiseEffect(new PerlinNoiseSettings(new System.Random(0x12345678))
        //    {
        //    }),
        //    new HexagonalPixelation(10),
        //    new SobelEdgeDetection()
        //), reg, .5);

        //img = img
        //    .ApplyEffect(new HexagonalPixelation(10), reg, .6)
        //    .ApplyEffect(new HexagonalPixelation(20), (1440.., ..), .6)
        //    ;

        //img = img.ApplyEffect(new ChainedPartialBitmapEffect(
        //        new BoxBlur(5),
        //        new Colorize(ColorMap.BlackbodyHeat)
        //    ), reg);


        //res = res.ToLumaInvertedMask().Colorize(ColorMap.Spectral);


        sw.Stop();
        Console.WriteLine($"effects: {sw.ElapsedMilliseconds:F2} ms");
        sw.Restart();
        img.Save("conv.png");
        sw.Stop();
        Console.WriteLine($"saving: {sw.ElapsedMilliseconds:F2} ms");
    }

    public static void Main_Automaton()
    {
        var parser = new ParserBuilder<char>("abc")
                     .Start()
                     .ExactlyOne('a')
                     .OneOrMore('b')
                     .AtLeast(3, 'a')
                     .Accept()
                     .Not('a')
                     .Exactly(1, 'b')
                     .Accept()
                     .Not('b')
//                         .LoopOn("a", bs => bs.ExactlyOne('b'))
//                         .Accept()
                     .GenerateParser()
                     .GenerateRegularExpression(c => c.ToString())
//                         .GenerateWords()
//                         .Select(c => new string(c))
//                         .ToArray()
                     ;


        //var xxx = new[]
        //{
        //    "abaaa",
        //    "abaaab",
        //    "abaaabb",
        //    "abaaabbb",
        //    "aabaaa",
        //    "abbbbaaa",
        //}.Select(s => parser.Parse(s)).ToArray();


        DirectedGraph<int, IEnumerable<char>> dg = new()
        {
            0,
            1,
            2,
            3,
            { 0, 1, "a" },
            { 0, 2, "b" },
            { 1, 1, "b" },
            { 1, 3, "a" },
            { 2, 0, "b" },
            { 2, 2, "a" },
            { 3, 3, "a" },
        };
        dg.DebugPrintToConsole();

        DeterministicFiniteAutomaton<int, char> dfa = new(dg, dg[0]!);

        dfa.Accepted[dg[3]!] = true;

        var res = dfa.Parse("baaabbbabbaaa", out var path);


        foreach (var w in dfa.GenerateWords().Take(20))
            Console.WriteLine(new string(w));
    }

    public static void Main_LinearAlgebra()
    {
        var π = Math.PI;
        var τ = 2 * π;
        var e = Math.E;
        var m = new Matrix7(
            3, 0, π, 0, 0, 0, 4,
            0, 0, 0, τ, 1, 0, 0,
            0, 0, 0,-2, 0, 0,-3,
            1, 0, 0, e, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0,
            π, 0, 0,-τ, 0, 0,-e
        );
        var ccs = m.ToCompressedStorageFormat();
        var b64 = DataStream.FromBytes(ccs).Compress(CompressionFunction.GZip).ToBase64();
        var bts = DataStream.FromBase64(b64).Uncompress(CompressionFunction.GZip).ToBytes();
        var ccsx = CompressedStorageFormat<Scalar>.FromBytes(bts);
        var m2 = Matrix7.FromCompressedStorageFormat(ccsx);

        var s = m.GetEigenspace(1);

        Console.WriteLine(m.ToShortString());
        Console.WriteLine();
        Console.WriteLine(DataStream.FromUnmanaged(m).ToBase64());
        Console.WriteLine();
        Console.WriteLine(b64);
        Console.WriteLine();
        Console.WriteLine(m2.ToShortString());

        Vector5 v = (0, -1, 0, 1, -.2); // -x + x^3 -0.2x^4
        var mat = v.HouseholderMatrix;
        Polynomial p = v;

        Console.WriteLine(p);
        Console.WriteLine(p.Derivative);
        Console.WriteLine(p.Integral);
        Console.WriteLine(p.Derivative.Derivative);
        Console.WriteLine(mat);
    }

    public static void Main_Graph()
    {
        var g = new DirectedGraph<string, char>();

        g.AddVertices("a", "b", "c", "d", "e", "f", "g", "h");
        g.AddEdge("a", "a").Data = 'a';
        g.AddEdge("a", "b").Data = 'b';
        g.AddEdge("a", "c").Data = 'a';
        g.AddEdge("b", "f").Data = 'c';
        g.AddEdge("c", "c").Data = 'b';
        g.AddEdge("c", "d").Data = 'b';
        g.AddEdge("d", "b").Data = 'c';
        g.AddEdge("d", "e").Data = 'b';
        g.AddEdge("e", "d").Data = 'a';
        g.AddEdge("e", "f").Data = 'a';
        g.AddEdge("e", "h").Data = 'c';
        g.AddEdge("f", "e").Data = 'a';
        g.AddEdge("f", "a").Data = 'd';
        g.AddEdge("g", "f").Data = 'c';

        _ = g["a"]!.TryFindPathTo(g["h"]!, out var p2, SearchStrategy.DepthFirst);
        _ = g["a"]!.TryFindPathTo(g["h"]!, out var p1, SearchStrategy.BreadthFirst);
        _ = g.TryFindShortestPath(g["a"]!, g["h"]!, _ => 1, out var p3);
    }
}
