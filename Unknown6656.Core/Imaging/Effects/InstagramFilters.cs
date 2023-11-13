// #define USE_HSL_BRIGHTNESS
// #define USE_HSL_SATURATION

using System.Drawing;
using System.Linq;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Imaging.Effects.Instagram;


/// <summary>
/// A static class containing all known instagram bitmap filters.
/// </summary>
/// <completionlist cref="InstagramFilters"/>
public abstract class InstagramFilter
    : PartialBitmapEffect
{
    public static _1977Filter _1977 { get; } = new();
    public static AdenFilter Aden { get; } = new();
    public static AmaroFilter Amaro { get; } = new();
    public static AshbyFilter Ashby { get; } = new();
    public static BrannanFilter Brannan { get; } = new();
    public static BrooklynFilter Brooklyn { get; } = new();
    public static ClarendonFilter Clarendon { get; } = new();
    public static CremaFilter Crema { get; } = new();
    public static CharmesFilter Charmes { get; } = new();
    public static DogpatchFilter Dogpatch { get; } = new();
    public static GinzaFilter Ginza { get; } = new();
    public static HefeFilter Hefe { get; } = new();
    public static HelenaFilter Helena { get; } = new();
    public static LudwigFilter Ludwig { get; } = new();
    public static SierraFilter Sierra { get; } = new();
    public static SutroFilter Sutro { get; } = new();
    public static VesperFilter Vesper { get; } = new();
    public static EarlybirdFilter Earlybird { get; } = new();
    public static GinghamFilter Gingham { get; } = new();
    public static HudsonFilter Hudson { get; } = new();
    public static InkwellFilter Inkwell { get; } = new();
    public static JunoFilter Juno { get; } = new();
    public static KelvinFilter Kelvin { get; } = new();
    public static LarkFilter Lark { get; } = new();
    public static LofiFilter Lofi { get; } = new();
    public static MavenFilter Maven { get; } = new();
    public static MayfairFilter Mayfair { get; } = new();
    public static MoonFilter Moon { get; } = new();
    public static PoprocketFilter Poprocket { get; } = new();
    public static PerpetuaFilter Perpetua { get; } = new();
    public static ReyesFilter Reyes { get; } = new();
    //public static RiseFilter Rise { get; } = new();
    public static SlumberFilter Slumber { get; } = new();
    public static StinsonFilter Stinson { get; } = new();
    public static ToasterFilter Toaster { get; } = new();
    public static ValenciaFilter Valencia { get; } = new();
    public static WaldenFilter Walden { get; } = new();
    public static WillowFilter Willow { get; } = new();
    public static XPro2Filter XPro2 { get; } = new();
    public static LegacyNashvilleFilter LegacyNashville { get; } = new();


    protected abstract PartialBitmapEffect[] Effects { get; }

    public Scalar Intensity { get; init; } = .5;

    private PartialBitmapEffect? _fx = null;


    private sealed protected override Bitmap Process(Bitmap bmp, Rectangle region)
    {
        if (_fx is null)
        {
            bool acc = Effects.All(e => e is Accelerated);

            _fx = acc ? new AcceleratedChainedPartialBitmapEffect(Effects.Cast<Accelerated>())
                      : new ChainedPartialBitmapEffect(Effects);
        }

        return _fx.ApplyTo(bmp, region, Intensity);
    }
}

/// <summary>
/// Represents the Instagram '1977' bitmap filter.
/// </summary>
#pragma warning disable IDE1006 // Naming Styles
public sealed class _1977Filter
#pragma warning restore IDE1006
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x4cf36abcu) { Blending = BlendMode.Screen },
        new Contrast(1.1),
        new Brightness(1.1),
        new Saturation(1.3),
    ];
}

/// <summary>
/// Represents the Instagram 'Aden' bitmap filter.
/// </summary>
public sealed class AdenFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
            {
                EndIntensity = 1,
                StartIntensity = .8
            });
            using Bitmap darkened = bmp.ApplyEffect(new ConstantColor(0xff420a0eu) { Blending = BlendMode.Darken }, region);

            return mask.Composite(darkened, bmp);
        }),
        new Hue(-20),
        new Contrast(.9),
        new Saturation(.85),
        new Brightness(1.2),
    ];
}

public sealed class AmaroFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x337D6918u) { Blending = BlendMode.Overlay },
        new Sepia(.35),
        new Contrast(1.1),
        new Brightness(1.2),
        new Saturation(1.3),
    ];
}

public sealed class AshbyFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x597D6918) { Blending = BlendMode.Lighten },
        new Sepia(.5),
        new Contrast(1.2),
        new Saturation(1.8),
    ];
}

/// <summary>
/// Represents the Instagram 'Brooklyn' bitmap filter.
/// </summary>
public sealed class BrooklynFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
            {
                Radius = .7 * new Vector2(bmp.Width, bmp.Height).Length,
            });
            using Bitmap b1 = bmp.ApplyEffect(new ConstantColor(0x66A8DFC1) { Blending = BlendMode.Overlay }, region);
            using Bitmap b2 = bmp.ApplyEffect(new ConstantColor(0xFFC4B7C8) { Blending = BlendMode.Overlay }, region);

            return mask.Composite(b1, b2);
        }),
        new Contrast(.9),
        new Brightness(1.1),
    ];
}

/// <summary>
/// Represents the Instagram 'Brannan' bitmap filter.
/// </summary>
public sealed class BrannanFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x4FA12CC7) { Blending = BlendMode.Lighten },
        new Sepia(.5),
        new Contrast(1.4),
    ];
}

/// <summary>
/// Represents the Instagram 'Clarendon' bitmap filter.
/// </summary>
public sealed class ClarendonFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x337FBBE3) { Blending = BlendMode.Overlay },
        new Contrast(1.2),
        new Saturation(1.35),
    ];
}

public sealed class CremaFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x337D6918) { Blending = BlendMode.Multiply },
        new Sepia(.5),
        new Contrast(1.25),
        new Brightness(1.15),
        new Saturation(.9),
        new Hue(-2),
    ];
}

public sealed class CharmesFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x3F7D6918u) { Blending = BlendMode.Darken },
        new Sepia(.25),
        new Contrast(1.25),
        new Brightness(1.25),
        new Saturation(1.35),
        new Hue(-5),
    ];
}

public sealed class DogpatchFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new Sepia(.35),
        new Saturation(1.1),
        new Contrast(1.5),
    ];
}

/// <summary>
/// Represents the Instagram 'Earlybird' bitmap filter.
/// </summary>
public sealed class EarlybirdFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null,
            0x33D0BA8Eu,
            0xCE360309u,
            0xFF1D0210u
        ) { Blending = BlendMode.Overlay },
        new Contrast(.9),
        new Sepia(.2),
    ];
}

public sealed class GinghamFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0xFFE6E6FA) { Blending = BlendMode.Overlay },
        new Brightness(1.05),
        new Hue(-10),
    ];
}

public sealed class GinzaFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x267D6918u) { Blending = BlendMode.Darken },
        new Sepia(.25),
        new Contrast(1.15),
        new Brightness(1.2),
        new Saturation(1.35),
        new Hue(-5),
    ];
}

public sealed class HefeFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null,
            0x00000000u,
            0x40000000u
        ) { Blending = BlendMode.Multiply },
        new Sepia(.4),
        new Contrast(1.5),
        new Brightness(1.2),
        new Saturation(1.4),
        new Hue(-10),
    ];
}

public sealed class HelenaFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x3f9EAF1Eu) { Blending = BlendMode.Overlay },
        new Sepia(.5),
        new Contrast(1.05),
        new Brightness(1.05),
        new Saturation(1.35),
    ];
}

public sealed class HudsonFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) => new BitmapBlend(bmp, BlendMode.Normal, .5).ApplyTo(new RadialGradient(null, null,
            0x7FA6B1FFu,
            0x19342134u
        ) { Blending = BlendMode.Multiply }.ApplyTo(bmp, region), region)),
        new Brightness(1.2),
        new Contrast(.9),
        new Saturation(1.1),
    ];
}

public sealed class InkwellFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new Sepia(.3),
        new Contrast(1.1),
        new Brightness(1.1),
        new Grayscale(),
    ];
}

public sealed class KelvinFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x382C34) { Blending = BlendMode.ColorDodge },
        new ConstantColor(0xB77D21) { Blending = BlendMode.Overlay },
    ];
}

public sealed class LarkFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0xFF22253F) { Blending = BlendMode.ColorDodge },
        new ConstantColor(0xCCF2F2F2) { Blending = BlendMode.Darken },
        new Contrast(.9),
    ];
}

public sealed class LofiFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
            {
                EndIntensity = .7,
                StartIntensity = 0
            });
            using Bitmap darkened = bmp.ApplyEffect(new ConstantColor(0xff222222u) { Blending = BlendMode.Multiply }, region);

            return mask.Composite(darkened, bmp);
        }),
        new Saturation(1.1),
        new Contrast(1.5),
    ];
}

public sealed class LudwigFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x177D6918u) { Blending = BlendMode.Overlay },
        new Sepia(.25),
        new Contrast(1.05),
        new Brightness(1.05),
        new Saturation(2),
    ];
}

public sealed class MavenFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x3303E61A) { Blending = BlendMode.Hue },
        new Sepia(.25),
        new Brightness(.95),
        new Contrast(.95),
        new Saturation(1.5),
    ];
}

public sealed class MayfairFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            BitmapMask mask1 = BitmapMask.Radial(bmp.Width, bmp.Height, new() { Radius = .3 });
            BitmapMask mask2 = BitmapMask.Radial(bmp.Width, bmp.Height, new() { Radius = .6 });
            using Bitmap img1 = bmp.ApplyEffect(new ConstantColor(0xccffffffu) { Blending = BlendMode.Overlay }, region);
            using Bitmap img2 = bmp.ApplyEffect(new ConstantColor(0x99ffc8c8u) { Blending = BlendMode.Overlay }, region);
            using Bitmap img3 = bmp.ApplyEffect(new ConstantColor(0xff111111u) { Blending = BlendMode.Overlay }, region);

            return bmp.Blend(mask2.Composite(mask1.Composite(img1, img2), img3), BlendMode.Normal, .4);
        }),
        new Contrast(1.1),
        new Saturation(1.1),
    ];
}

public sealed class MoonFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0xA0A0A0) { Blending = BlendMode.SoftLight },
        new ConstantColor(0x383838) { Blending = BlendMode.Lighten },
        new Grayscale(),
        new Contrast(1.1),
        new Brightness(1.1),
    ];
}

/// <summary>
/// Represents the Instagram 'Nashville' bitmap filter.
/// </summary>
public sealed class NashvilleFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x8EF7B099u) { Blending = BlendMode.Darken },
        new ConstantColor(0x66004696u) { Blending = BlendMode.Lighten },
        new Sepia(.2),
        new Contrast(1.2),
        new Brightness(1.05),
        new Saturation(1.2),
    ];
}

public sealed class LegacyNashvilleFilter
    : InstagramFilter
{
    // filter: sepia(.25) contrast(1.5) brightness(.9) hue-rotate(-15deg)
    // before: radial-gradient(circle closest-corner, rgba(128, 78, 15, .5) 0, rgba(128, 78, 15, .65) 100%)
    //         [screen]


    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new Sepia(.25),
        new Contrast(1.5),
        new Brightness(.9),
        new Hue(-.261799),
        new ConstantColor(0x47804e0f) { Blending = BlendMode.Screen },
    ];
}

public sealed class PerpetuaFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) => bmp.ApplyEffect(new RadialGradient(null, null,
            0xFF005B9Au,
            0xFFE6C13Du
        ) { Blending = BlendMode.SoftLight }, region, .5))
    ];
}

public sealed class PoprocketFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null,
            0xBFCE2746u,
            0xFF000000u
        ) { Blending = BlendMode.Screen },
        new Sepia(.15),
        new Contrast(1.2),
    ];
}

public sealed class ReyesFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) => bmp.ApplyEffect(new ConstantColor(0xFFEFCDADu) { Blending = BlendMode.SoftLight }, region, .5)),
        new Sepia(.22),
        new Brightness(1.1),
        new Contrast(.85),
        new Saturation(.75),
    ];
}

public sealed class SierraFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null,
            0x7F804E0Fu,
            0xA5000000u
        ) { Blending = BlendMode.Screen },
        new Sepia(.25),
        new Contrast(1.5),
        new Brightness(.9),
        new Hue(-15),
    ];
}

/// <summary>
/// Represents the Instagram smooth 'Walden' bitmap effect
/// </summary>
public sealed unsafe class SmoothWaldenBitmapEffect
     : InstagramFilter
{
    // -webkit-filter: brightness(1.1) hue-rotate(-10deg) sepia(.3) saturate(1.6);
    // screen #04c .3

    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new Brightness(1.1),
        new Hue(-Scalar.Pi),
        new Sepia(.3),
        new Saturation(1.6),
        new ConstantColor(0x4c001339) { Blending = BlendMode.Screen },
    ];
 }

public sealed class SlumberFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x6645290Cu) { Blending = BlendMode.Lighten },
        new ConstantColor(0x7F7D6918u) { Blending = BlendMode.SoftLight },
        new Saturation(.66),
        new Brightness(1.05),
    ];
}

public sealed class StinsonFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x33F09580u) { Blending = BlendMode.SoftLight },
        new Contrast(.75),
        new Saturation(.85),
        new Brightness(1.15),
    ];
}

public sealed class SutroFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null, new(0d, .5), new(0d, .4)) { Blending = BlendMode.Darken },
        new Sepia(.4),
        new Contrast(1.2),
        new Brightness(.9),
        new Saturation(1.4),
        new Hue(-10),
    ];
}

public sealed class ToasterFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new RadialGradient(null, null,
            0xFF804E0Fu,
            0xFF38003Bu
        ) { Blending = BlendMode.Screen },
        new Contrast(1.5),
        new Brightness(.9),
    ];
}

public sealed class JunoFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x337fBBE3) { Blending = BlendMode.Overlay },
        new Sepia(.35),
        new Contrast(1.15),
        new Brightness(1.15),
        new Saturation(1.8),
    ];
}

public sealed class ValenciaFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            using Bitmap cs = bmp.ApplyEffect(new ConstantColor(0xff3A0339u));
            using Bitmap cm = BitmapExtensions.Blend(bmp, cs, BlendMode.Exclusion, region);

            return BitmapExtensions.Blend(bmp, cm, BlendMode.Normal, region, .5);
        }),
        new Contrast(1.08),
        new Brightness(1.08),
        new Sepia(.08),
    ];
}

public sealed class VesperFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        new ConstantColor(0x3F7D6918u) { Blending = BlendMode.Overlay },
        new Sepia(.35),
        new Contrast(1.15),
        new Brightness(1.2),
        new Saturation(1.3),
    ];
}

public sealed class WaldenFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            using Bitmap cs = bmp.ApplyEffect(new ConstantColor(0xff0044CCu));
            using Bitmap cm = BitmapExtensions.Blend(bmp, cs, BlendMode.Screen, region);

            return BitmapExtensions.Blend(bmp, cm, BlendMode.Normal, region, .3);
        }),
        new Brightness(1.1),
        new Hue(-10),
        new Sepia(.3),
        new Saturation(1.6),
    ];
}

public sealed class WillowFilter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            using Bitmap cs1 = bmp.ApplyEffect(new RadialGradient(null, null, 0xFFD4A9AFu, 0xFFD4A9AFu, 0xFF0B427Du));

            return bmp.Blend(cs1, BlendMode.Overlay);
        }),
        new ConstantColor(0xFFD8CDCBu) { Blending = BlendMode.Color },
        new Grayscale(.5),
        new Contrast(.95),
        new Brightness(.9),
    ];
}

public sealed class XPro2Filter
    : InstagramFilter
{
    protected override PartialBitmapEffect[] Effects { get; } =
    [
        FromDelegate((bmp, region) =>
        {
            using Bitmap cs1 = bmp.ApplyEffect(new ConstantColor(0xFFE6E7E0u) { Blending = BlendMode.Top }, region);
            using Bitmap cs2 = bmp.ApplyEffect(new ConstantColor(0x992B2AA1u) { Blending = BlendMode.Normal }, region);
            BitmapMask mask = BitmapMask.Radial(bmp.Width, bmp.Height, new()
            {
                StartOffset = .6,
            });

            using Bitmap cs = mask.Composite(cs1, cs2);
            using Bitmap cm1 = BitmapExtensions.Blend(bmp, cs, BlendMode.ColorBurn, region);
            using Bitmap cm2 = BitmapExtensions.Blend(bmp, cm1, BlendMode.Normal, region, .6);

            return mask.Composite(cm1, cm2);
        }),
        new Sepia(.3),
    ];
}
