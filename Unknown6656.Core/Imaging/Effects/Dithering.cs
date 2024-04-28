using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;
using Unknown6656.Runtime;
using Unknown6656.Common;

namespace Unknown6656.Imaging.Effects;

using error_diffusion_factor = ValueTuple<int, int, double>;


[SupportedOSPlatform(OS.WIN)]
public unsafe abstract class Dithering
    : PartialBitmapEffect.Accelerated
{
    internal const ColorEqualityMetric COLOR_EQUALITY = ColorEqualityMetric.EucledianRGBALength;

    public ColorPalette ColorPalette { get; }


    public Dithering(ColorPalette target_palette) => ColorPalette = target_palette;

    public Dithering(IEnumerable<RGBAColor> target_palette)
        : this(new ColorPalette(target_palette))
    {
    }

    protected RGBAColor GetColor(RGBAColor input, ref Vector3 error)
    {
        //var i = input;
        //var e = error;

        Vector3 offsetted = Vector3.Add(input, error);
        RGBAColor output = ColorPalette.GetNearestColor<RGBAColor>(offsetted, COLOR_EQUALITY);

        error = Vector3.Subtract(input, output);
        output.A = input.A;

        //Console.WriteLine($"{i,10} {e[0],22} -> {offsetted[0],22} -> {output,10} {error[0],22}");

        return output;
    }
}

[SupportedOSPlatform(OS.WIN)]
public unsafe class ErrorDiffusionDithering
    : Dithering
{
    public ErrorDiffusionDitheringAlgorithm Algorithm { get; }


    public ErrorDiffusionDithering(ErrorDiffusionDitheringAlgorithm algorithm, ColorPalette target_palette)
        : base(target_palette) => Algorithm = algorithm;

    public ErrorDiffusionDithering(ErrorDiffusionDitheringAlgorithm algorithm, IEnumerable<RGBAColor> target_palette)
        : this(algorithm, new(target_palette))
    {
    }

    internal protected override void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        if (Algorithm switch
        {
            ErrorDiffusionDitheringAlgorithm.Thresholding => new ReduceColorSpace(ColorPalette).Process,
            ErrorDiffusionDitheringAlgorithm.Randomized => RandomDithering,
            ErrorDiffusionDitheringAlgorithm.Simple => SimpleDithering,
            ErrorDiffusionDitheringAlgorithm.HilbertCurve => HilbertCurveDithering,
            _ => (ProcessFunc?)null,
        } is ProcessFunc func)
            func(bmp, source, destination, region);
        else
            Dither(bmp, source, destination, region, Algorithm switch
            {
                ErrorDiffusionDitheringAlgorithm.FloydSteinberg => new error_diffusion_factor[] {
                    (1, 0, .4375),
                    (-1, 1, .1875),
                    (0, 1, .3125),
                    (1, 1, .0625)
                },
                ErrorDiffusionDitheringAlgorithm.FalseFloydSteinberg => [
                    (1, 0, .375),
                    (0, 1, .25),
                    (1, 1, .375)
                ],
                ErrorDiffusionDitheringAlgorithm.Atkinson => [
                    (1, 0, .125),
                    (2, 0, .125),
                    (-1, 1, .125),
                    (0, 1, .125),
                    (1, 1, .125),
                    (0, 2, .125)
                ],
                ErrorDiffusionDitheringAlgorithm.Burkes => [
                    (1, 0, .25),
                    (2, 0, .125),
                    (-2, 1, .0625),
                    (-1, 1, .125),
                    (0, 1, .25),
                    (1, 1, .125),
                    (2, 1, .0625)
                ],
                ErrorDiffusionDitheringAlgorithm.JarvisJudiceNinke => [
                    (1, 0, .1458333333),
                    (2, 0, .1041666667),
                    (-2, 1, .0625),
                    (-1, 1, .1041666667),
                    (0, 1, .1458333333),
                    (1, 1, .1041666667),
                    (2, 1, .0625),
                    (-2, 2, .0208333333),
                    (-1, 2, .0625),
                    (0, 2, .1041666667),
                    (1, 2, .0625),
                    (2, 2, .0208333333)
                ],
                ErrorDiffusionDitheringAlgorithm.Stucki => [
                    (1, 0, .1904761905),
                    (2, 0, .0952380952),
                    (-2, 1, .0476190476),
                    (-1, 1, .0952380952),
                    (0, 1, .1904761905),
                    (1, 1, .0952380952),
                    (2, 1, .0476190476),
                    (-2, 2, .0238095238),
                    (-1, 2, .0476190476),
                    (0, 2, .0952380952),
                    (1, 2, .0476190476),
                    (2, 2, .0238095238)
                ],
                ErrorDiffusionDitheringAlgorithm.Sierra => [
                    (1, 0, .15625),
                    (2, 0, .09375),
                    (-2, 1, .0625),
                    (-1, 1, .125),
                    (0, 1, .15625),
                    (1, 1, .125),
                    (2, 1, .0625),
                    (-1, 2, .0625),
                    (0, 2, .09375),
                    (1, 2, .0625)
                ],
                ErrorDiffusionDitheringAlgorithm.SierraTwoRow => [
                    (1, 0, .1875),
                    (2, 0, .25),
                    (-2, 1, .0625),
                    (-1, 1, .125),
                    (0, 1, .1875),
                    (1, 1, .125),
                    (2, 1, .0625)
                ],
                ErrorDiffusionDitheringAlgorithm.SierraLite => [
                    (1, 0, .5),
                    (-1, 1, .25),
                    (0, 1, .25)
                ],
                ErrorDiffusionDitheringAlgorithm.TwoDimensional => [
                    (1, 0, .5),
                    (0, 1, .5)
                ],
            });
    }

    private void Dither(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region, params (int rel_x, int rel_y, double factor)[] error_diffusion)
    {
        int w = bmp.Width;
        Vector3[,] errors = new Vector3[region.Width, region.Height];

        for (int ry = 0; ry < region.Height; ++ry)
            for (int rx = 0; rx < region.Width; ++rx)
            {
                int x = region.Left + rx;
                int y = region.Top + ry;
                int index = y * w + x;
                ref Vector3 error = ref errors[rx, ry];

                destination[index] = GetColor(source[index], ref error);

                foreach ((int rel_x, int rel_y, double factor) in error_diffusion.AsParallel())
                    if (rx + rel_x is int ex and >= 0 && ex <= region.Width - 1 &&
                        ry + rel_y is int ey and >= 0 && ey <= region.Height - 1)
                        errors[ex, ey] += error * factor;
            }
    }

    private void HilbertCurveDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        Vector3 error = Vector3.Zero;
        int w = bmp.Width;

        foreach ((int x, int y) in gen_hilbert_2d_curve(region.Width, region.Height))
        {
            int index = (x + region.Left) + w * (y + region.Top);

            destination[index] = GetColor(source[index], ref error);
        }
    }

    private void SimpleDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        Vector3 error = Vector3.Zero;

        foreach (int index in GetIndices(bmp, region))
            destination[index] = GetColor(source[index], ref error);
    }

    private void RandomDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        Vector3 error = Vector3.Zero;

        foreach (int index in GetIndices(bmp, region).Shuffle())
            destination[index] = GetColor(source[index], ref error);
    }

    private static IEnumerable<(int x, int y)> gen_hilbert_2d_curve(int w, int h)
    {
        return w >= h ? h2d(0, 0, w, 0, 0, h) : h2d(0, 0, 0, w, h, 0);

        static IEnumerable<(int x, int y)> h2d(int x, int y, int ax, int ay, int bx, int by)
        {
            int w = Math.Abs(ax + ay);
            int h = Math.Abs(bx + by);
            (int dax, int day) = (Math.Sign(ax), Math.Sign(ay));
            (int dbx, int dby) = (Math.Sign(bx), Math.Sign(by));

            if (h is 1)
                for (int i = 0; i < w; ++i)
                {
                    yield return (x, y);

                    x += dax;
                    y += day;
                }
            else if (w is 1)
                for (int i = 0; i < h; ++i)
                {
                    yield return (x, y);

                    x += dbx;
                    y += dby;
                }
            else
            {
                List<(int, int)> coordinates = [];
                (int ax2, int ay2) = (ax / 2, ay / 2);
                (int bx2, int by2) = (bx / 2, by / 2);
                int w2 = Math.Abs(ax2 + ay2);
                int h2 = Math.Abs(bx2 + by2);

                if (2 * w > 3 * h)
                {
                    if ((w2 & 1) != 0 && w > 2)
                    {
                        ax2 += dax;
                        ay2 += day;
                    }

                    coordinates.AddRange(h2d(x, y, ax2, ay2, bx, by));
                    coordinates.AddRange(h2d(x + ax2, y + ay2, ax - ax2, ay - ay2, bx, by));
                }
                else
                {
                    if ((h2 & 1) != 0 && h > 2)
                    {
                        bx2 += dbx;
                        by2 += dby;
                    }

                    coordinates.AddRange(h2d(x, y, bx2, by2, ax2, ay2));
                    coordinates.AddRange(h2d(x + bx2, y + by2, ax, ay, bx - bx2, by - by2));
                    coordinates.AddRange(h2d(x + (ax - dax) + (bx2 - dbx), y + (ay - day) + (by2 - dby), -bx2, -by2, -(ax - ax2), -(ay - ay2)));
                }

                foreach (var coord in coordinates)
                    yield return coord;
            }
        }
    }
}

[SupportedOSPlatform(OS.WIN)]
public unsafe abstract class OrderedDithering
    : Dithering
{
    private readonly double[] _dithering_matrix;
    private readonly int _dithering_width, _dithering_height;

    public OrderedDitheringAlgorithm Algorithm { get; } = OrderedDitheringAlgorithm.__UNDEFINED__;


    public OrderedDithering(OrderedDitheringAlgorithm algorithm, ColorPalette palette)
        : this(GetDitheringMatrix(algorithm), palette) => Algorithm = algorithm;

    public OrderedDithering(int[,] dithering_matrix, ColorPalette palette)
        : base(palette)
    {
        _dithering_width = dithering_matrix.GetLength(0);
        _dithering_height = dithering_matrix.GetLength(1);

        double[] dithering = dithering_matrix.Flatten().ToArray(i => (double)i);
        double max = dithering.Max();
        double min = dithering.Min();

        _dithering_matrix = dithering.ToArray(v => (v - min) / (max - min));
    }

    internal protected override void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;

        Parallel.ForEach(GetIndices(bmp, region), idx =>
        {
            (int x, int y) = GetAbsoluteCoordinates(idx, w);
            double threshold = _dithering_matrix[x % _dithering_width + (y % _dithering_height) * _dithering_width];

            destination[idx] = GetColorFrom01Threshold(source[idx], threshold);
        });
    }

    protected abstract RGBAColor GetColorFrom01Threshold(RGBAColor source, double threshold);

    protected static int[,] GetDitheringMatrix(OrderedDitheringAlgorithm algorithm) => algorithm switch
    {
        OrderedDitheringAlgorithm.Bayer => new int[8, 8]
        {
            { 0, 32,  8, 40,  2, 34, 10, 42},
            { 48, 16, 56, 24, 50, 18, 58, 26},
            { 12, 44,  4, 36, 14, 46,  6, 38},
            { 60, 28, 52, 20, 62, 30, 54, 22},
            { 3, 35, 11, 43,  1, 33,  9, 41},
            { 51, 19, 59, 27, 49, 17, 57, 25},
            { 15, 47,  7, 39, 13, 45,  5, 37},
            { 63, 31, 55, 23, 61, 29, 53, 21}
        },
        OrderedDitheringAlgorithm.Bayer2 => new int[8, 8]
        {
            { 52, 14, 58, 4 , 53, 9 , 59, 1 },
            { 30, 36, 20, 42, 25, 37, 17, 43 },
            { 57, 8 , 48, 16, 61, 5 , 49, 10 },
            { 24, 41, 32, 32, 21, 45, 26, 33 },
            { 54, 11, 62, 2 , 51, 15, 56, 3 },
            { 27, 38, 18, 46, 31, 35, 19, 40 },
            { 60, 6 , 50, 12, 55, 7 , 47, 13 },
            { 22, 44, 28, 34, 23, 39, 29, 31 },
        },
        OrderedDitheringAlgorithm.Bayer3 => new int[4, 4]
        {
            { 5, 9, 6, 10 },
            { 13, 1, 14, 2 },
            { 7, 11, 4, 8 },
            { 15, 3, 12, 0 },
        },
        OrderedDitheringAlgorithm.Halftone => new int[8, 8]
        {
            { 62, 58, 50, 36, 32, 46, 55, 63},
            { 54, 40, 27, 23, 19, 28, 41, 59},
            { 45, 31, 14, 10, 6, 15, 24, 51},
            { 35, 18, 5, 1, 2, 11, 20, 37},
            { 39, 22, 9, 0, 3, 7, 16, 33},
            { 49, 26, 13, 4, 8, 12, 29, 47},
            { 57, 43, 30, 17, 21, 25, 42, 56},
            { 61, 53, 48, 34, 38, 52, 60, 64},
        },
        OrderedDitheringAlgorithm.Ordered_2x8 => new int[2, 8]
        {
            { 14, 12, 10, 8, 9, 11, 13, 15 },
            { 6, 4, 2, 0, 1, 3, 5, 7 },
        },
        OrderedDitheringAlgorithm.Ordered_8x2 => new int[8, 2]
        {
            { 6, 14 },
            { 4, 12 },
            { 2, 10 },
            { 0, 8 },
            { 1, 9 },
            { 3, 11 },
            { 5, 13 },
            { 7, 15 },
        },
        OrderedDitheringAlgorithm.DispersedDots_8 => new int[8, 8]
        {
            { 65, 59, 51, 42, 43, 52, 60, 66 },
            { 58, 36, 29, 21, 22, 30, 37, 61 },
            { 50, 28, 15, 9 , 10, 16, 31, 53 },
            { 41, 20, 8 , 2 , 3 , 11, 23, 45 },
            { 40, 19, 7 , 1 , 4 , 12, 24, 46 },
            { 49, 27, 14, 6 , 5 , 13, 32, 54 },
            { 57, 35, 26, 18, 17, 25, 34, 62 },
            { 64, 56, 48, 39, 38, 47, 55, 63 },
        },
        OrderedDitheringAlgorithm.DispersedDots_6 => new int[6, 6]
        {
            { 32, 24, 16, 20, 31, 35 },
            { 28, 12, 4 , 8 , 15, 27 },
            { 21, 9 , 0 , 2 , 7 , 19 },
            { 17, 5 , 3 , 1 , 11, 23 },
            { 25, 13, 10, 6 , 14, 30 },
            { 33, 29, 22, 18, 26, 34 },
        },
        OrderedDitheringAlgorithm.DispersedDots_4 => new int[4, 4]
        {
            { 15, 8, 5 , 12 },
            { 4 , 0, 3 , 9  },
            { 11, 2, 1 , 6  },
            { 14, 7, 10, 13 },
        },
        OrderedDitheringAlgorithm.DispersedDots_3 => new int[3, 3]
        {
            { 6, 2, 7 },
            { 1, 0, 3 },
            { 5, 4, 8 },
        },
        OrderedDitheringAlgorithm.DispersedDots_2 => new int[2, 2]
        {
            { 1, 2 },
            { 0, 3 },
        },
        OrderedDitheringAlgorithm.WavyHatchet_16 => new int[16, 16]
        {
            { 7 , 2 , 11, 11, 11, 2 , 11, 9 , 11, 11, 2 , 11, 11, 11, 11, 6 },
            { 15, 2 , 20, 27, 2 , 27, 24, 21, 1 , 1 , 1 , 2 , 18, 27, 3 , 21 },
            { 15, 26, 8 , 6 , 2 , 19, 1 , 1 , 5 , 10, 23, 1 , 1 , 3 , 25, 26 },
            { 15, 21, 1 , 1 , 2 , 27, 20, 5 , 16, 21, 10, 27, 2 , 1 , 1 , 27 },
            { 1 , 1 , 13, 13, 8 , 2 , 7 , 13, 13, 13, 13, 6 , 2 , 13, 13, 1 },
            { 2 , 27, 22, 21, 17, 2 , 20, 27, 16, 27, 6 , 21, 2 , 27, 20, 27 },
            { 15, 19, 22, 1 , 1 , 1 , 2 , 19, 16, 3 , 23, 2 , 18, 9 , 25, 2 },
            { 15, 1 , 1 , 5 , 17, 21, 1 , 1 , 3 , 27, 20, 2 , 18, 21, 7 , 2 },
            { 12, 12, 5 , 12, 12, 12, 12, 2 , 1 , 1 , 12, 12, 12, 1 , 1 , 2 },
            { 2 , 6 , 20, 27, 17, 27, 3 , 2 , 16, 10, 1 , 1 , 1 , 27, 25, 21 },
            { 2 , 9 , 22, 19, 17, 6 , 24, 2 , 16, 26, 2 , 4 , 18, 19, 25, 26 },
            { 1 , 2 , 9 , 27, 6 , 27, 2 , 27, 16, 21, 2 , 10, 18, 27, 1 , 1 },
            { 14, 1 , 1 , 3 , 14, 14, 2 , 14, 14, 2 , 14, 14, 1 , 1 , 5 , 14 },
            { 15, 27, 2 , 1 , 1 , 27, 20, 27, 1 , 1 , 23, 21, 18, 5 , 20, 27 },
            { 15, 3 , 2 , 26, 17, 1 , 1 , 1 , 16, 2 , 23, 26, 6 , 26, 10, 19 },
            { 6 , 27, 2 , 27, 17, 2 , 4 , 27, 16, 27, 2 , 7 , 18, 21, 25, 8 },
        },
        OrderedDitheringAlgorithm.__UNDEFINED__ => throw new ArgumentException("A valid ordered dithering algorithm has to be provided.", nameof(algorithm)),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };
}

[SupportedOSPlatform(OS.WIN)]
public unsafe class ColoredOrderedDithering
    : OrderedDithering
{
    private readonly (int r, int g, int b) _color_steps;

    public double Bias { set; get; } = 0;
    public double Spread { set; get; } = .5;


    public ColoredOrderedDithering(OrderedDitheringAlgorithm algorithm, int color_steps)
        : this(algorithm, color_steps, color_steps, color_steps)
    {
    }

    public ColoredOrderedDithering(OrderedDitheringAlgorithm algorithm, int red_color_steps, int green_color_steps, int blue_color_steps)
        : this(GetDitheringMatrix(algorithm), red_color_steps, green_color_steps, blue_color_steps)
    {
    }

    public ColoredOrderedDithering(int[,] dithering_matrix, int color_steps)
        : this(dithering_matrix, color_steps, color_steps, color_steps)
    {
    }

    public ColoredOrderedDithering(int[,] dithering_matrix, int red_color_steps, int green_color_steps, int blue_color_steps)
        : base(dithering_matrix, null!)
    {
        if (red_color_steps <= 0 || red_color_steps >= 256)
            throw new ArgumentOutOfRangeException(nameof(red_color_steps), "The number of color steps must be >0 and <256.");
        else if (green_color_steps <= 0 || green_color_steps >= 256)
            throw new ArgumentOutOfRangeException(nameof(green_color_steps), "The number of color steps must be >0 and <256.");
        else if (blue_color_steps <= 0 || blue_color_steps >= 256)
            throw new ArgumentOutOfRangeException(nameof(blue_color_steps), "The number of color steps must be >0 and <256.");

        _color_steps = (red_color_steps, green_color_steps, blue_color_steps);
    }

    protected override RGBAColor GetColorFrom01Threshold(RGBAColor source, double threshold)
    {
        Vector3 rgb = source;
        int rsteps = _color_steps.r - 1;
        int gsteps = _color_steps.g - 1;
        int bsteps = _color_steps.b - 1;

        rgb += Spread * (threshold - .5) + Bias;

        return new(
            (int)(rsteps * rgb.X + .5) / rsteps,
            (int)(gsteps * rgb.Y + .5) / gsteps,
            (int)(bsteps * rgb.Z + .5) / bsteps,
            source.Af
        );
    }
}

[SupportedOSPlatform(OS.WIN)]
public unsafe class BlackWhiteOrderedDithering
    : OrderedDithering
{
    public BlackWhiteOrderedDithering(OrderedDitheringAlgorithm algorithm)
        : base(algorithm, ColorPalette.BlackAndWhite)
    {
    }

    public BlackWhiteOrderedDithering(int[,] dithering_matrix)
        : base(dithering_matrix, ColorPalette.BlackAndWhite)
    {
    }

    protected override RGBAColor GetColorFrom01Threshold(RGBAColor source, double threshold) => source.CIEGray < threshold ? RGBAColor.Black : RGBAColor.White;
}

[SupportedOSPlatform(OS.WIN)]
public sealed class DitheringError
    : PartialBitmapEffect
{
    private readonly Dithering _dithering;

    public ColorPalette ColorPalette => _dithering.ColorPalette;


    public DitheringError(Dithering dithering) => _dithering = dithering;

    private protected unsafe override Bitmap Process(Bitmap bmp, Rectangle region)
    {
        Bitmap result = _dithering.ApplyTo(bmp, region);

        bmp.LockRGBAPixels((src, w, h) => result.LockRGBAPixels((dst, _, _) => Parallel.ForEach(GetIndices(bmp, region), idx =>
        {
            double gray = dst[idx].DistanceTo(src[idx], Dithering.COLOR_EQUALITY);

            dst[idx] = new(gray, gray, gray, src[idx].Af);
        })));

        return result;
    }

    public static implicit operator DitheringError(Dithering dithering) => new(dithering);

    public static implicit operator Dithering(DitheringError dithering) => dithering._dithering;
}

public enum ErrorDiffusionDitheringAlgorithm
{
    Simple,
    [Obsolete($"Use the effect '{nameof(ReduceColorSpace)}' instead.")]
    Thresholding,
    Randomized,
    VoidAndCluster,
    FloydSteinberg,
    FalseFloydSteinberg,
    JarvisJudiceNinke,
    Stucki,
    Burkes,
    Sierra,
    SierraTwoRow,
    SierraLite,
    Atkinson,
    TwoDimensional,
    GradientBased,
    HilbertCurve,

    // VoidAndCluster,
    // GradientBased,
}

public enum OrderedDitheringAlgorithm
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    __UNDEFINED__ = -1,
    Halftone,
    Bayer,
    Bayer2,
    Bayer3,
    DispersedDots_8,
    DispersedDots_6,
    DispersedDots_4,
    DispersedDots_3,
    DispersedDots_2,
    Ordered_2x8,
    Ordered_8x2,
    WavyHatchet_16,
}
