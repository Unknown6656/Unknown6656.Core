using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Common;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Effects;


[SupportedOSPlatform(OS.WIN)]
public unsafe class Dithering
    : PartialBitmapEffect.Accelerated
{
    internal const ColorEqualityMetric COLOR_EQUALITY = ColorEqualityMetric.EucledianRGBALength;

    private readonly Lazy<(RGBAColor mid, RGBAColor low, RGBAColor high)[]> _ordered;

    public DitheringAlgorithm Algorithm { get; }
    public ColorPalette ColorPalette { get; }


    public Dithering(DitheringAlgorithm algorithm, ColorPalette target_palette)
    {
        Algorithm = algorithm;
        ColorPalette = target_palette;
        _ordered = new(() => (from low in ColorPalette
                              from high in ColorPalette
                              where low != high
                              where low <= high
                              let mid = (RGBAColor)(.5 * ((Vector4)low + (Vector4)high))
                              select (mid, low, high)).DistinctBy(t => (t.low, t.high)).ToArray());
    }

    public Dithering(DitheringAlgorithm algorithm, IEnumerable<RGBAColor> target_palette)
        : this(algorithm, new ColorPalette(target_palette))
    {
    }

    internal protected override void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        ProcessFunc func = Algorithm switch
        {
            DitheringAlgorithm.Thresholding => new ReduceColorSpace(ColorPalette).Process,
            DitheringAlgorithm.FloydSteinberg => FloydSteinbergDithering,
            DitheringAlgorithm.FalseFloydSteinberg => FalseFloydSteinbergDithering,
            DitheringAlgorithm.Atkinson => AtkinsonDithering,
            DitheringAlgorithm.Randomized => RandomDithering,
            DitheringAlgorithm.Simple => SimpleDithering,
            DitheringAlgorithm.Burkes => BurkesDithering,
            DitheringAlgorithm.JarvisJudiceNinke => JarvisJudiceNinkeDithering,
            DitheringAlgorithm.Stucki => StuckiDithering,
            DitheringAlgorithm.Sierra => SierraDithering,
            DitheringAlgorithm.SierraTwoRow => SierraTwoRowDithering,
            DitheringAlgorithm.SierraLite => SierraLiteDithering,
            DitheringAlgorithm.HilbertCurve => HilbertCurveDithering,
            DitheringAlgorithm.TwoDimensional => TwoDimensionalDithering,
            //DitheringAlgorithm.Patterning => ,
            //DitheringAlgorithm.Halftone => ,
            //DitheringAlgorithm.Bayer => ,
            //DitheringAlgorithm.VoidAndCluster => ,
            //DitheringAlgorithm.GradientBased => ,
        };
        func(bmp, source, destination, region);
    }


    private void BayerDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => OrderedDithering(bmp, source, destination, region, new double[8, 8]
    {
        { 0, 32,  8, 40,  2, 34, 10, 42},
        { 48, 16, 56, 24, 50, 18, 58, 26},
        { 12, 44,  4, 36, 14, 46,  6, 38},
        { 60, 28, 52, 20, 62, 30, 54, 22},
        { 3, 35, 11, 43,  1, 33,  9, 41},
        { 51, 19, 59, 27, 49, 17, 57, 25},
        { 15, 47,  7, 39, 13, 45,  5, 37},
        { 63, 31, 55, 23, 61, 29, 53, 21}
    });

    private void FloydSteinbergDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        ( 1, 0, .4375),
        (-1, 1, .1875),
        ( 0, 1, .3125),
        ( 1, 1, .0625)
    );

    private void FalseFloydSteinbergDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        (1, 0, .375),
        (0, 1, .25),
        (1, 1, .375)
    );

    private void JarvisJudiceNinkeDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
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
    );

    private void SierraLiteDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        (1, 0, .5),
        (-1, 1, .25),
        (0, 1, .25)
    );

    private void SierraTwoRowDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        (1, 0, .1875),
        (2, 0, .25),
        (-2, 1, .0625),
        (-1, 1, .125),
        (0, 1, .1875),
        (1, 1, .125),
        (2, 1, .0625)
    );

    private void StuckiDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
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
    );

    private void SierraDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
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
    );

    private void BurkesDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        (1, 0, .25),
        (2, 0, .125),
        (-2, 1, .0625),
        (-1, 1, .125),
        (0, 1, .25),
        (1, 1, .125),
        (2, 1, .0625)
    );

    private void AtkinsonDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        ( 1, 0, .125),
        ( 2, 0, .125),
        (-1, 1, .125),
        ( 0, 1, .125),
        ( 1, 1, .125),
        ( 0, 2, .125)
    );

    private void TwoDimensionalDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        (1, 0, .5),
        (0, 1, .5)
    );


    private void OrderedDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region, double[,] dithering_matrix)
    {
        int w = bmp.Width;
        int dw = dithering_matrix.GetLength(0);
        int dh = dithering_matrix.GetLength(1);

        Parallel.ForEach(GetIndices(bmp, region), idx =>
        {
            (int x, int y) = GetAbsoluteCoordinates(idx, w);
            double threshold = dithering_matrix[x % dw, y % dh];
            RGBAColor src = source[idx];

            destination[idx] = (from t in _ordered.Value
                                let dist = src.DistanceTo(t.mid, COLOR_EQUALITY)
                                orderby dist descending
                                let low_dist = src.DistanceTo(t.low, COLOR_EQUALITY)
                                let high_dist = src.DistanceTo(t.high, COLOR_EQUALITY)
                                let l = low_dist / (low_dist + high_dist)
                                let activate = l > threshold
                                select activate ? t.high : t.low).First();
        });
    }

    private void ErrorDiffusionDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region, params (int rel_x, int rel_y, double factor)[] error_diffusion)
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

    static IEnumerable<(int x, int y)> hilbert2D(int w, int h) => w >= h ? hilbert2D(0, 0, w, 0, 0, h) : hilbert2D(0, 0, 0, w, h, 0);

    static IEnumerable<(int x, int y)> hilbert2D(int x, int y, int ax, int ay, int bx, int by)
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
            List<(int, int)> coordinates = new();
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

                coordinates.AddRange(hilbert2D(x, y, ax2, ay2, bx, by));
                coordinates.AddRange(hilbert2D(x + ax2, y + ay2, ax - ax2, ay - ay2, bx, by));
            }
            else
            {
                if ((h2 & 1) != 0 && h > 2)
                {
                    bx2 += dbx;
                    by2 += dby;
                }

                coordinates.AddRange(hilbert2D(x, y, bx2, by2, ax2, ay2));
                coordinates.AddRange(hilbert2D(x + bx2, y + by2, ax, ay, bx - bx2, by - by2));
                coordinates.AddRange(hilbert2D(x + (ax - dax) + (bx2 - dbx), y + (ay - day) + (by2 - dby), -bx2, -by2, -(ax - ax2), -(ay - ay2)));
            }

            foreach (var coord in coordinates)
                yield return coord;
        }
    }


    private void HilbertCurveDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        Vector3 error = Vector3.Zero;
        int w = bmp.Width;

        foreach ((int x, int y) in hilbert2D(region.Width, region.Height))
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

    private RGBAColor GetColor(RGBAColor input, ref Vector3 error)
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
public sealed class DitheringError
    : PartialBitmapEffect
{
    private readonly Dithering _dithering;

    public DitheringAlgorithm Algorithm => _dithering.Algorithm;
    public ColorPalette ColorPalette => _dithering.ColorPalette;


    public DitheringError(DitheringAlgorithm algorithm, ColorPalette target_palette) => _dithering = new(algorithm, target_palette);

    public DitheringError(DitheringAlgorithm algorithm, IEnumerable<RGBAColor> target_palette) => _dithering = new(algorithm, target_palette);

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
}

public enum DitheringAlgorithm
{
    Simple,
    [Obsolete($"Use the effect '{nameof(ReduceColorSpace)}' instead.")]
    Thresholding,
    Randomized,
    Patterning,
    Halftone,
    Bayer,
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
}
