using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Common;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging.Effects;

#if !false


[SupportedOSPlatform(OS.WIN)]
public unsafe class Dithering
    : PartialBitmapEffect.Accelerated
{
    public DitheringAlgorithm Algorithm { get; }
    public ColorPalette ColorPalette { get; }


    public Dithering(DitheringAlgorithm algorithm, ColorPalette target_palette)
    {
        Algorithm = algorithm;
        ColorPalette = target_palette;
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
            DitheringAlgorithm.Atkinson => AtkinsonDithering,
            DitheringAlgorithm.Randomized => RandomDithering,
            DitheringAlgorithm.Simple => SimpleDithering,

            //DitheringAlgorithm.Patterning => ,
            //DitheringAlgorithm.Halftone => ,
            //DitheringAlgorithm.Bayer => ,
            //DitheringAlgorithm.VoidAndCluster => ,
            //DitheringAlgorithm.JarvisJudiceNinke => ,
            //DitheringAlgorithm.Stucki => ,
            //DitheringAlgorithm.Burkes => ,
            //DitheringAlgorithm.Sierra => ,
            //DitheringAlgorithm.TwoRowSierra => ,
            //DitheringAlgorithm.SierraLite => ,
            //DitheringAlgorithm.GradientBased => ,

        };
        func(bmp, source, destination, region);
    }

    private void ErrorDiffusionDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region, params (int rel_x, int rel_y, double factor)[] error_diffusion)
    {
        int w = bmp.Width;

        foreach (int index in GetIndices(bmp, region))
        {
            Vector3 err3 = Vector3.Zero;

            destination[index] = GetColor(source[index], ref err3);
            (int x, int y) = GetAbsoluteCoordinates(index, w);
            Vector4 err4 = new(in err3, 0);

            foreach ((int rel_x, int rel_y, double factor) in error_diffusion)
            {
                int idx = GetIndex(x + rel_x, y + rel_y, w, region, EdgeHandlingMode.Extend)!.Value;

                source[idx] += err4 * .4375;
            }
        }
    }

    private void FloydSteinbergDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        ( 1, 0, .4375),
        (-1, 1, .1875),
        ( 1, 1, .3125),
        ( 1, 1, .0625)
    );

    private void AtkinsonDithering(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => ErrorDiffusionDithering(bmp, source, destination, region,
        ( 1, 0, .125),
        ( 2, 0, .125),
        (-1, 1, .125),
        ( 0, 1, .125),
        ( 1, 1, .125),
        ( 0, 2, .125)
    );

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
        var i = input;
        var e = error;

        Vector3 offsetted = Vector3.Add(input, error);
        RGBAColor output = ColorPalette.GetNearestColor<RGBAColor>(offsetted, ColorEqualityMetric.RGBChannels);

        error = Vector3.Subtract(input, output);
        output.A = input.A;

        Console.WriteLine($"{i} {e,70} -> {offsetted,70} -> {output} {error,70}");

        return output;
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
    JarvisJudiceNinke,
    Stucki,
    Burkes,
    Sierra,
    TwoRowSierra,
    SierraLite,
    Atkinson,
    GradientBased,
}

#endif