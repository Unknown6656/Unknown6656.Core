// #define USE_HSL_BRIGHTNESS
// #define USE_HSL_SATURATION

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics;
using Unknown6656.Runtime;

using Random = Unknown6656.Mathematics.Numerics.Random;

namespace Unknown6656.Imaging.Effects;


public static class InstagramFilters
{
    public static NashvilleFilter Nashville { get; } = new();
    public static LegacyNashvilleFilter LegacyNashville { get; } = new();
}

/// <summary>
/// Represents the Instagram 'Nashville' bitmap filter.
/// </summary>
public sealed class NashvilleFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public NashvilleFilter()
        : base(
            new ConstantColor(0x8EF7B099u, BlendMode.Darken),
            new ConstantColor(0x66004696u, BlendMode.Lighten),
            new Sepia(.2),
            new Contrast(1.2),
            new Brightness(1.05),
            new Saturation(1.2)
        )
    {
    }
}

public sealed class LegacyNashvilleFilter
    : AcceleratedChainedPartialBitmapEffect
{
    // filter: sepia(.25) contrast(1.5) brightness(.9) hue-rotate(-15deg)
    // before: radial-gradient(circle closest-corner, rgba(128, 78, 15, .5) 0, rgba(128, 78, 15, .65) 100%)
    //         [screen]

    public LegacyNashvilleFilter()
        : base(
            new Sepia(.25),
            new Contrast(1.5),
            new Brightness(.9),
            new Hue(-.261799),
            new ConstantColor(0x47804e0f, BlendMode.Screen)
        )
    {
    }
}

// /// <summary>
/// Represents the Instagram smooth 'Walden' bitmap effect
/// </summary>
// [SupportedOSPlatform(OS.WIN)]
// public sealed unsafe class SmoothWaldenBitmapEffect
//     : ChainedPartialBitmapEffect
// {
//     // -webkit-filter: brightness(1.1) hue-rotate(-10deg) sepia(.3) saturate(1.6);
//     // screen #04c .3
// 
//     public SmoothWaldenBitmapEffect()
//         : base(
//             new Brightness(1.1),
//             new Hue(-Scalar.Pi),
//             new SepiaFilter(.3),
//             new Saturation(1.6),
//             new BlendEffect()
// 
//             ,
//             .ApplyEffectRange<ScreenColorBlendEffect>(Range, 0, .075, .225) // (0,¼,¾) * .3
//             .Average(bmp, .3);
//             ,
//         )
//     {
//     }
// }


// ADEN
// INKWELL
// REYES
// WALDEN
// JUNO
// MAVEN
// PERPETUA
// SLUMBER
// LOFI
