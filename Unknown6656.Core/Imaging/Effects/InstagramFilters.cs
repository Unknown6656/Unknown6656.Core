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
using System.Security.Cryptography;

namespace Unknown6656.Imaging.Effects;


/// <summary>
/// A static class containing all known instagram bitmap filters.
/// </summary>
/// <completionlist cref="InstagramFilters"/>
public abstract class InstagramFilter
    : PartialBitmapEffect
{
    public static _1977Filter _1977 { get; } = new();
    public static AdenFilter Aden { get; } = new();
    public static BrannanFilter Brannan { get; } = new();
    public static BrooklynFilter Brooklyn { get; } = new();
    public static ClarendonFilter Clarendon { get; } = new();
    public static EarlybirdFilter Earlybird { get; } = new();
    public static GinghamFilter Gingham { get; } = new();
    public static HudsonFilter Hudson { get; } = new();
    public static InkwellFilter Inkwell { get; } = new();
    //public static JunoFilter Juno { get; } = new();
    public static KelvinFilter Kelvin { get; } = new();
    public static LarkFilter Lark { get; } = new();
    //public static LofiFilter Lofi { get; } = new();
    public static MavenFilter Maven { get; } = new();
    //public static MayfairFilter Mayfair { get; } = new();
    public static MoonFilter Moon { get; } = new();
    //public static PerpetuaFilter Perpetua { get; } = new();
    //public static ReyesFilter Reyes { get; } = new();
    //public static RiseFilter Rise { get; } = new();
    public static SlumberFilter Slumber { get; } = new();
    public static StinsonFilter Stinson { get; } = new();
    //public static ToasterFilter Toaster { get; } = new();
    //public static ValenciaFilter Valencia { get; } = new();
    //public static WaldenFilter Walden { get; } = new();
    //public static WillowFilter Willow { get; } = new();
    //public static XPro2Filter XPro2 { get; } = new();
    public static LegacyNashvilleFilter LegacyNashville { get; } = new();
}

/// <summary>
/// Represents the Instagram '1977' bitmap filter.
/// </summary>
public sealed class _1977Filter
    : AcceleratedChainedPartialBitmapEffect
{
    public _1977Filter()
        : base(
            new ConstantColor(0x4cf36abcu) { Blending = BlendMode.Screen },
            new Contrast(1.1),
            new Brightness(1.1),
            new Saturation(1.3)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Aden' bitmap filter.
/// </summary>
public sealed class AdenFilter
    : ChainedPartialBitmapEffect
{
    public AdenFilter()
        : base(
            PartialBitmapEffect.FromDelegate((bmp, region) =>
            {
                BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
                {
                    EndIntensity = 1,
                    StartIntensity = .8
                });
                Bitmap darkened = bmp.ApplyEffect(new ConstantColor(0xff420a0eu) { Blending = BlendMode.Darken });

                return mask.Composite(darkened, bmp);
            }),
            new Hue(-20),
            new Contrast(.9),
            new Saturation(.85),
            new Brightness(1.2)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Brooklyn' bitmap filter.
/// </summary>
public sealed class BrooklynFilter
    : ChainedPartialBitmapEffect
{
    public BrooklynFilter()
        : base(
            FromDelegate((bmp, region) =>
            {
                BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
                {
                    Radius = .7 * new Vector2(bmp.Width, bmp.Height).Length,
                });
                Bitmap b1 = bmp.ApplyEffect(new ConstantColor(0x66A8DFC1) { Blending = BlendMode.Overlay });
                Bitmap b2 = bmp.ApplyEffect(new ConstantColor(0xFFC4B7C8) { Blending = BlendMode.Overlay });

                return mask.Composite(b1, b2);
            }),
            new Contrast(.9),
            new Brightness(1.1)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Brannan' bitmap filter.
/// </summary>
public sealed class BrannanFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public BrannanFilter()
        : base(
            new ConstantColor(0x4FA12CC7) { Blending = BlendMode.Lighten },
            new Sepia(.5),
            new Contrast(1.4)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Clarendon' bitmap filter.
/// </summary>
public sealed class ClarendonFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public ClarendonFilter()
        : base(
            new ConstantColor(0x337FBBE3) { Blending = BlendMode.Overlay },
            new Contrast(1.2),
            new Saturation(1.35)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Earlybird' bitmap filter.
/// </summary>
public sealed class EarlybirdFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public EarlybirdFilter()
        : base(
            new RadialGradient(null, null,
                0x33D0BA8Eu,
                0xCE360309u,
                0xFF1D0210u
            ) { Blending = BlendMode.Overlay },
            new Contrast(.9),
            new Sepia(.2)
        )
    {
    }
}

public sealed class GinghamFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public GinghamFilter()
        : base(
            new ConstantColor(0xFFE6E6FA) { Blending = BlendMode.Overlay },
            new Brightness(1.05),
            new Hue(-10)
        )
    {
    }
}

public sealed class HudsonFilter
    : ChainedPartialBitmapEffect
{
    public HudsonFilter()
        : base(
            FromDelegate((bmp, region) => new BitmapBlend(bmp, BlendMode.Normal, .5).ApplyTo(new RadialGradient(null, null,
                0x7FA6B1FFu,
                0x19342134u
            ) { Blending = BlendMode.Multiply }.ApplyTo(bmp))),
            new Brightness(1.2),
            new Contrast(.9),
            new Saturation(1.1)
        )
    {
    }
}

public sealed class InkwellFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public InkwellFilter()
        : base(
            new Sepia(.3),
            new Contrast(1.1),
            new Brightness(1.1),
            new Grayscale()
        )
    {
    }
}

public sealed class KelvinFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public KelvinFilter()
        : base(
            new ConstantColor(0x382C34) { Blending = BlendMode.ColorDodge },
            new ConstantColor(0xB77D21) { Blending = BlendMode.Overlay }
        )
    {
    }
}

public sealed class LarkFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public LarkFilter()
        : base(
            new ConstantColor(0xFF22253F) { Blending = BlendMode.ColorDodge },
            new ConstantColor(0xCCF2F2F2) { Blending = BlendMode.Darken },
            new Contrast(.9)
        )
    {
    }
}

public sealed class MavenFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public MavenFilter()
        : base(
            new ConstantColor(0x3303E61A) { Blending = BlendMode.Hue },
            new Sepia(.25),
            new Brightness(.95),
            new Contrast(.95),
            new Saturation(1.5)
        )
    {
    }
}

public sealed class MoonFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public MoonFilter()
        : base(
            new ConstantColor(0xA0A0A0) { Blending = BlendMode.SoftLight },
            new ConstantColor(0x383838) { Blending = BlendMode.Lighten },
            new Grayscale(),
            new Contrast(1.1),
            new Brightness(1.1)
        )
    {
    }
}

/// <summary>
/// Represents the Instagram 'Nashville' bitmap filter.
/// </summary>
public sealed class NashvilleFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public NashvilleFilter()
        : base(
            new ConstantColor(0x8EF7B099u) { Blending = BlendMode.Darken },
            new ConstantColor(0x66004696u) { Blending = BlendMode.Lighten },
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
            new ConstantColor(0x47804e0f) { Blending = BlendMode.Screen }
        )
    {
    }
}

/// <summary>
/// Represents the Instagram smooth 'Walden' bitmap effect
/// </summary>
public sealed unsafe class SmoothWaldenBitmapEffect
     : ChainedPartialBitmapEffect
{
    // -webkit-filter: brightness(1.1) hue-rotate(-10deg) sepia(.3) saturate(1.6);
    // screen #04c .3

    public SmoothWaldenBitmapEffect()
        : base(
            new Brightness(1.1),
            new Hue(-Scalar.Pi),
            new Sepia(.3),
            new Saturation(1.6),
            new ConstantColor(0x4c001339) { Blending = BlendMode.Screen }
        )
    {
    }
 }

public sealed class SlumberFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public SlumberFilter()
        : base(
            new ConstantColor(0x6645290Cu) { Blending = BlendMode.Lighten },
            new ConstantColor(0x7F7D6918u) { Blending = BlendMode.SoftLight },
            new Saturation(.66),
            new Brightness(1.05)
        )
    {
    }
}

public sealed class StinsonFilter
    : AcceleratedChainedPartialBitmapEffect
{
    public StinsonFilter()
        : base(
            new ConstantColor(0x33F09580u) { Blending = BlendMode.SoftLight },
            new Contrast(.75),
            new Saturation(.85),
            new Brightness(1.15)
        )
    {
    }
}

// JUNO
