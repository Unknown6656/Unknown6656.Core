using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Imaging.Effects;


#if false

[SupportedOSPlatform(OS.WIN)]
public class Dithering
    : PartialBitmapEffect.Accelerated
{
    public ColorEqualityMetric EqualityMetric { get; }
    public DitheringAlgorithm Algorithm { get; }
    public ColorPalette ColorPalette { get; }


    public Dithering(DitheringAlgorithm algorithm, ColorPalette target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
    {
        Algorithm = algorithm;
        ColorPalette = target_palette;
        EqualityMetric = equality_metric;
    }

    public Dithering(DitheringAlgorithm algorithm, IEnumerable<RGBAColor> target_palette, ColorEqualityMetric equality_metric = ColorEqualityMetric.RGBChannels)
        : this(algorithm, new ColorPalette(target_palette), equality_metric)
    {
    }

    private protected override unsafe void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region)
    {
        int w = bmp.Width;

        foreach (int index in GetIndices(bmp, region))
        {
            destination[index] = ColorPalette.GetNearestColor(source[index], EqualityMetric, out double distance);
            (int x, int y) = GetAbsoluteCoordinates(index, w);

            int i1 = GetIndex(x + 1, y, w, region, EdgeHandlingMode.Extend).Value;
            int i2 = GetIndex(x - 1, y + 1, w, region, EdgeHandlingMode.Extend).Value;
            int i3 = GetIndex(x, y + 1, w, region, EdgeHandlingMode.Extend).Value;
            int i4 = GetIndex(x + 1, y + 1, w, region, EdgeHandlingMode.Extend).Value;

            destination[i1] += distance
        }
        /*
            for each y from top to bottom do
                for each x from left to right do
                    oldpixel := pixels[x][y]
                    newpixel := find_closest_palette_color(oldpixel)
                    pixels[x][y] := newpixel
                    quant_error := oldpixel - newpixel
                    pixels[x + 1][y    ] := pixels[x + 1][y    ] + quant_error × 7 / 16
                    pixels[x - 1][y + 1] := pixels[x - 1][y + 1] + quant_error × 3 / 16
                    pixels[x    ][y + 1] := pixels[x    ][y + 1] + quant_error × 5 / 16
                    pixels[x + 1][y + 1] := pixels[x + 1][y + 1] + quant_error × 1 / 16
        //*/
    }
}

#endif

public enum DitheringAlgorithm
{
    Thresholding,
    Randomized,
    Patterning,
    Halftone,
    Bayer,
    VoidAndCluster,
    FloydSteinberg,
    JarvisJudiceNinke,
    Stucki,
    Burkes,
    Sierra,
    TwoRowSierra,
    SierraLite,
    Atkinson,
    GradientBased,
}

