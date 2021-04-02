using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Unknown6656.Optimization.ParticleSwarmOptimization;
using Unknown6656.Mathematics.Graphs.Computation;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Graphs;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Geometry;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Mathematics;
using Unknown6656.Controls.Console;
using Unknown6656.Imaging.Effects;
using Unknown6656.Imaging;
using Unknown6656.Common;
using Unknown6656.IO;

using Random = Unknown6656.Mathematics.Numerics.Random;
using System.Threading.Tasks;
using System.Reflection;
using Unknown6656;

namespace Testing
{
    using static Complex;

    public static unsafe class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            ConsoleExtensions.WriteBlock(new string('w', 1000), 20, 10, 50, 30, true);

            ConsoleExtensions.WriteBlock(new string('@', 123), 80, 20, 10, 50, true);

            //Main_PSO();
            return;
            Main_BMP_effects();
            Main_Math();
            Main_BMP1();
            Main_ConsoleUI();
            Main_Statistics();
            Main_LinearAlgebra();
            Main_Automaton();
            Main_Graph();
        }

        private static void evaluate_random<random>()
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

        //class pso_problem : PSOProblem<Scalar>
        //{
        //    public override int Dimensionality => 1;
        //    public override Scalar GetValue(VectorN position) { Scalar x = position[0]; return (x - 3).Tanh() + x * x - x * x * x; }
        //    public override bool IsValidSearchPosition(VectorN position) => true;
        //}
        //private static void Main_PSO()
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

        private static void Main_Statistics()
        {
            XorShift sh = new XorShift();
            int i = 20;

            while (i-- > 0)
            {
                string s = From.Bytes(sh.NextBytes(512)).ToDrunkBishop(100, 50, " .,-~+=´'*\"/!?lI$#&%@BGWO", false);
                Console.Clear();
                Console.WriteLine(s);
                System.Threading.Thread.Sleep(200);
            }

        }

        private static void Main_ConsoleUI()
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



            ControlHost host = new ControlHost();

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
                    c.Options = new[] { "top", "kaikokek", "jej !null", "ebin h4x" };
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

        private static void Main_Math()
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

        private static void Main_BMP1()
        {
            double total = 1000;

            VideoAssembler.CreateVideoParallel(
                new FileInfo("render.mp4"),
                (int)total,
                new(1920, 1080),
                (index, frame) =>
                {
                    double time = index / total;
                    using Graphics g = Graphics.FromImage(frame);

                    new ComplexFunctionPlotter<ComplexFunction>(
                        new ComplexFunction(c =>
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
                frame_rate: 60
            );
        }

        private static void Main_complex_plotter_ui()
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
        }

        private static void Main_complex_plotter_bmp()
        {
            new ComplexFunctionPlotter<ComplexFunction>(
                new ComplexFunction(c =>
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

        private static void Main_BMP_complex_function_plotter_animation()
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
                new ComplexFunctionPlotter<ComplexFunction>(
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

        private static void Main_BMP_mandelbrot_plotter()
        {
            int i = 0;

            for (double f = -2; f <= 2; f += 5e-3)
                new ComplexFunctionPlotter<ComplexFunction>(
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

        private static void Main_BMP_draw_triangles()
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

        private static void Main_BMP_plot_transformation_function()
        {
            new Transformation2DPlotter<Function<Vector2>>(
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

        private static void Main_BMP_colormaps()
        {
            ColorMap[] maps = typeof(ColorMap).GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty)
                                              .Select(p => (p as PropertyInfo)?.GetValue(null) as ColorMap)
                                              .Where(m => m is { })
                                              .ToArray()!;
            Bitmap img = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                .ApplyEffect(new LinearGradient((Vector2.Zero, RGBAColor.Black), ((0, 1080), RGBAColor.White)));
            int width = img.Width / maps.Length;

            for (int i = 0; i < maps.Length; ++i)
                img = img.ApplyEffect(new Colorize(maps[i]), ((i * width)..((i + 1) * width), ..));

            img.Save("conv.png");
        }

        private static void Main_BMP_effects()
        {
            var sw = new Stopwatch();
            sw.Start();


            var reg = /*(360..1560, 200..880); */(960.., ..);

            var im1 = ((Bitmap)Image.FromFile("img1.png")).ToARGB32();
            var im2 = ((Bitmap)Image.FromFile("img2.png")).ToARGB32();
            var im3 = ((Bitmap)Image.FromFile("img3.png")).ToARGB32();
            Bitmap img = im2;

            // img = BitmapMask.FromLuma(im1).ApplyTo(im1).ApplyEffect(new BlendEffect(im2, BlendMode.Screen, 1));
            // img = im2.ApplyEffect(new ChainedPartialBitmapEffect(
            //     new ColorKey(0xf000, 0)
            //     new MultiPointGradient(
            //         ((500, 200), 0xff00),
            //         ((1000, 200), 0xf00f),
            //         ((700, 900), 0xf0f0)
            //     ),
            //     new PerlinNoiseEffect(new PerlinNoiseSettings(new System.Random(0x12345678))
            //     {
            //     })
            //     new HexagonalPixelation(10)
            //      new SobelEdgeDetection()
            // ), reg, .5);

            //img = img
            //    .ApplyEffect(new HexagonalPixelation(10), reg, .6)
            //    .ApplyEffect(new HexagonalPixelation(20), (1440.., ..), .6)
            //    ;

            img = img.ApplyEffect(new ChainedBitmapEffect(
                    new BoxBlur(5),
                    new Colorize(ColorMap.BlackbodyHeat)
                ));


            //res = res.ToLumaInvertedMask().Colorize(ColorMap.Spectral);


            sw.Stop();
            Console.WriteLine($"effects: {sw.ElapsedMilliseconds:F2} ms");
            sw.Restart();
            img.Save("conv.png");
            sw.Stop();
            Console.WriteLine($"saving: {sw.ElapsedMilliseconds:F2} ms");
        }

        private static void Main_Automaton()
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


            DirectedGraph<int, IEnumerable<char>> dg = new DirectedGraph<int, IEnumerable<char>>
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

            DeterministicFiniteAutomaton<int, char> dfa = new DeterministicFiniteAutomaton<int, char>(dg, dg[0]!);

            dfa.Accepted[dg[3]!] = true;

            var res = dfa.Parse("baaabbbabbaaa", out var path);


            foreach (var w in dfa.GenerateWords().Take(20))
                Console.WriteLine(new string(w));
        }

        private static void Main_LinearAlgebra()
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
            var b64 = From.Bytes(ccs).Compress(CompressionFunction.GZip).ToBase64();
            var bts = From.Base64(b64).Uncompress(CompressionFunction.GZip).ToBytes();
            var ccsx = CompressedStorageFormat<Scalar>.FromBytes(bts);
            var m2 = Matrix7.FromCompressedStorageFormat(ccsx);

            var s = m.GetEigenspace(1);

            Console.WriteLine(m.ToShortString());
            Console.WriteLine();
            Console.WriteLine(From.Unmanaged(m).ToBase64());
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

        private static void Main_Graph()
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
}
