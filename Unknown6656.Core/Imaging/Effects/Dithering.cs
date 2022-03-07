using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Imaging.Effects;


[SupportedOSPlatform("windows")]
public class Dithering
    : BitmapEffect
{
    public DitheringAlgorithm Algorithm { get; }
    public ColorPalette ColorPalette { get; }


    public Dithering(DitheringAlgorithm algorithm, ColorPalette target_palette)
    {
        Algorithm = algorithm;
        ColorPalette = target_palette;
    }

    public Dithering(DitheringAlgorithm algorithm, params RGBAColor[] target_palette)
        : this(algorithm, new ColorPalette(target_palette))
    {
    }

    public Dithering(DitheringAlgorithm algorithm, IEnumerable<RGBAColor> target_palette)
        : this(algorithm, new ColorPalette(target_palette))
    {
    }

    public override Bitmap ApplyTo(Bitmap bmp)
    {
        throw new NotImplementedException();
    }
}

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

