using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
//using Unknown6656.Mathematics.Geometry;
using Unknown6656.Mathematics.Statistics;
using Unknown6656.Imaging.Effects;
using Unknown6656.Runtime;

namespace Unknown6656.Imaging;


[SupportedOSPlatform(OS.WIN)]
public sealed unsafe class BitmapMask
    : PartialBitmapEffect.Accelerated
{
    public Bitmap Bitmap { get; }


    private BitmapMask(Bitmap bitmap) => Bitmap = bitmap;

    protected internal override void Process(Bitmap bmp, RGBAColor* source, RGBAColor* destination, Rectangle region) => Bitmap.LockRGBAPixels((mask, w, h) =>
    {
        int[] indices = GetIndices(bmp, region);
        int bw = bmp.Width;

        Parallel.For(0, indices.Length, i =>
        {
            int x = i % bw;
            int y = i / bw;

            if (x < w && y < h)
            {
                RGBAColor m = mask[y * w + x];

                destination[i] = source[i];
                destination[i].Af *= m.Af * m.CIEGray / 3;
            }
        });
    });

    public Bitmap ApplyTo(Bitmap bitmap, Vector2 offset) => new BitmapMask(Bitmap.ApplyEffect(new AffinePixelTransform(Matrix2.Identity, offset))).ApplyTo(bitmap);

    public BitmapMask Invert() => FromLumaInverted(Bitmap);

    public BitmapMask BlendWith(BitmapMask second, BlendMode mode) => BlendMasks(this, second, mode);

    public Bitmap Composite(Bitmap bottom, Bitmap top)
    {
        Bitmap destination = new(bottom.Width, bottom.Height, PixelFormat.Format32bppArgb);

        Bitmap.LockRGBAPixels((mask, mw, mh) =>
        bottom.LockRGBAPixels((bottom, bw, bh) =>
        top.LockRGBAPixels((top, tw, th) =>
        destination.LockRGBAPixels((dest, _, _) =>
        {
            int w = Math.Min(bw, tw);
            int h = Math.Min(bh, th);

            Parallel.For(0, w * h, i =>
            {
                int x = i % w;
                int y = i / w;

                if (x < mw && y < mh)
                {
                    int mi = GetIndex(x, y, mw);
                    int ti = GetIndex(x, y, tw);
                    int bi = GetIndex(x, y, bw);
                    double factor = mask[mi].Af * mask[mi].CIEGray;

                    dest[bi] = RGBAColor.LinearInterpolate(bottom[bi], top[ti], factor);
                }
            });
        }))));

        return destination;
    }

    public (Bitmap masked, Bitmap inverted) Split(Bitmap bitmap)
    {
        Bitmap destination = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        Bitmap inverted = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

        Bitmap.LockRGBAPixels((mask, mw, mh) =>
        bitmap.LockRGBAPixels((source, w, h) =>
        destination.LockRGBAPixels((dest1, w, h) =>
        inverted.LockRGBAPixels((dest2, w, h) =>
        Parallel.For(0, w * h, i =>
        {
            int x = i % w;
            int y = i / w;

            if (x < mw && y < mh)
            {
                RGBAColor m = mask[y * w + x];

                dest1[i] = source[i];
                dest1[i].Af *= m.Af * m.CIEGray / 3;

                dest2[i] = source[i];
                dest2[i].A = (byte)(255 - dest1[i].A);
            }
        })))));

        return (destination, inverted);
    }

    public Bitmap Colorize(ColorMap map) => Bitmap.ApplyEffect(new ColorEffect.Delegated(c => map[c.CIEGray]));

    public RegressionDataSet1D GetHistogram()
    {
        RegressionDataSet1D? hist = null;

        Bitmap.LockRGBAPixels((ptr, w, h) => hist = new RegressionDataSet1D(Enumerable.Range(0, w * h).Select(i => (Scalar)ptr[i].Average)));

        while (hist is null)
            ;

        return hist;
    }

    public BitmapMask ToBinaryMask(Scalar threshold) => new(Bitmap.ApplyEffect(new ColorEffect.Delegated(c => new RGBAColor(c.Average < threshold ? 0 : 1, c.Af))));

    // entire picture
    // rectangle
    // ellipse
    // text/path
    // xor
    // and
    // or
    // blend

    // TODO:
    //public static BitmapMask FromShape<T>(T shape) where T : Shape2D<T> => ;

    public static BitmapMask FromApproximation(Bitmap bitmap, DiscreteColorMap colormap) => FromBitmap(bitmap, colormap.Approximate);

    public static BitmapMask FromChannels(Bitmap bitmap, params ColorChannel[] channels)
    {
        channels = channels.Distinct().ToArray();

        bool α = channels.Contains(ColorChannel.A);

        return FromBitmap(bitmap, c =>
        {
            Scalar v = 0;

            for (int i = 0; i < channels.Length; ++i)
                v += c[channels[i]] / 255d;

            return v / channels.Length;
        }, α);
    }

    public static BitmapMask FromAlpha(Bitmap bitmap) => FromBitmap(bitmap, c => c.Af);

    public static BitmapMask FromAlphaInverted(Bitmap bitmap) => FromBitmap(bitmap, c => 1 - c.Af);

    public static BitmapMask FromLuma(Bitmap bitmap) => FromBitmap(bitmap, c => c.HSL.Luminosity);

    public static BitmapMask FromLumaInverted(Bitmap bitmap) => FromBitmap(bitmap, c => 1 - c.HSL.Luminosity);

    public static BitmapMask FromSaturation(Bitmap bitmap) => FromBitmap(bitmap, c => 1 - c.HSL.Saturation);

    public static BitmapMask FromHue(Bitmap bitmap) => FromBitmap(bitmap, c => c.ToHSL().H / Scalar.Tau);

    public static BitmapMask BlendMasks(BitmapMask bottom, BitmapMask top, BlendMode mode) => new(new BitmapBlend(bottom, mode, 1).ApplyTo(top));

    public static BitmapMask Radial(int width, int height, RadialMaskConfiguration configuration)
    {
        RGBAColor start = new(configuration.StartIntensity);
        RGBAColor end = new(configuration.EndIntensity);
        using Bitmap bmp = new(width, height, PixelFormat.Format32bppRgb);
        Bitmap intensities = bmp.ApplyEffect(new RadialGradient(configuration.Center, configuration.Radius, new DiscreteColorMap(
            (Scalar.Zero, start),
            (configuration.StartOffset.Clamp(), start),
            (Scalar.One - configuration.EndOffset.Clamp(), end),
            (Scalar.One, end)
        )));

        return new(intensities);
    }

    public static BitmapMask FromBitmap(Bitmap bitmap, Func<RGBAColor, Scalar> func, bool ignore_alpha = false) =>
        new(bitmap.ApplyEffect(new ColorEffect.Delegated(c => func(c).Clamp() * new Vector4(1, 1, 1, 0) + (0, 0, 0, ignore_alpha ? c.Af : 1))));

    public static implicit operator Bitmap(BitmapMask mask) => mask.Bitmap;
}

public sealed record RadialMaskConfiguration
{
    public Vector2? Center { get; init; } = null;
    public Scalar? Radius { get; init; } = null;
    public Scalar StartOffset { get; init; } = Scalar.Zero;
    public Scalar EndOffset { get; init; } = Scalar.Zero;
    public Scalar StartIntensity { get; init; } = Scalar.One;
    public Scalar EndIntensity { get; init; } = Scalar.Zero;
}
