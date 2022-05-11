using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Statistics;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Imaging.Plotting;
using Unknown6656.Imaging.Effects;
using Unknown6656.Generics;
using Unknown6656.Runtime;
using Unknown6656.Common;
using Unknown6656.IO;

namespace Unknown6656.Imaging;


[SupportedOSPlatform(OS.WIN)]
public static unsafe class BitmapExtensions
{
    /// <summary>
    /// Converts the given bitmap to an 32-Bit ARGB (alpha, red, green and blue) bitmap
    /// </summary>
    /// <param name="bmp">Input bitmap (any pixel format)</param>
    /// <returns>32-Bit bitmap</returns>
    public static Bitmap ToARGB32(this Bitmap bmp)
    {
        Bitmap res = new(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(res))
            g.DrawImageUnscaled(bmp, 0, 0, bmp.Width, bmp.Height);

        return res;
    }

    public static Bitmap ToRGB24(this Bitmap bmp)
    {
        Bitmap res = new(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);

        using (Graphics g = Graphics.FromImage(res))
            g.DrawImageUnscaled(bmp, 0, 0, bmp.Width, bmp.Height);

        return res;
    }

    public static RGBAColor[] ToPixelArray(this Bitmap bmp) => new BitmapLocker(bmp).ToRGBAPixels();

    public static void SaveAsJPEG(this Bitmap bmp, string path, int quality_level)
    {
        using MemoryStream ms = new();

        bmp.SaveAsJPEG(ms, quality_level);

        DataStream.FromStream(ms).ToFile(path);
    }

    public static void SaveAsJPEG(this Bitmap bmp, Stream stream, int quality_level)
    {
        quality_level = quality_level switch { < 0 => 0, > 100 => 100, _ => quality_level };

        if (ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid) is ImageCodecInfo encoder)
        {
            EncoderParameters @params = new(1);

            @params.Param[0] = new EncoderParameter(Encoder.Quality, quality_level);

            bmp.Save(stream, encoder, @params);
        }
        else
            throw new InvalidOperationException("The JPEG codec could not be found in the list of available image codecs.");
    }

    public static void LockRGBAPixels(this Bitmap bmp, BitmapLockerCallback<RGBAColor> callback) => new BitmapLocker(bmp).LockRGBAPixels(callback);

    public static T LockRGBAPixels<T>(this Bitmap bmp, BitmapLockerCallback<RGBAColor, T> callback) => new BitmapLocker(bmp).LockRGBAPixels(callback);

    /// <summary>
    /// Returns the normalized zero-mean normalized cross-correlation (ZNCC).
    /// This is a value in the interval [0..1] which indicates the correlation of two grayscale images.
    /// <br/>
    /// The two images are required to have the same pixel resolution.
    /// <para/>
    /// See <see href="https://martin-thoma.com/zero-mean-normalized-cross-correlation/"/> for more information.
    /// </summary>
    /// <param name="first">First image.</param>
    /// <param name="second">Second image</param>
    /// <returns>Value between 0 and 1 which indicates the correlation (0: images are identical, 1: high discrepancy).</returns>
    public static Scalar GetZeroMeanNormalizedCrossCorrelation(this Bitmap first, Bitmap second)
    {
        if (first.Width != second.Width || first.Height != second.Height)
            throw new ArgumentException($"The second bitmap is required to thave the resolution {first.Width}px x {first.Height}px.", nameof(second));

        return first.LockRGBAPixels((fst, w, h) =>
            second.LockRGBAPixels((snd, _, _) =>
            {
                int total = w * h;
                double avg1 = 0;
                double avg2 = 0;
                double σ1 = 0;
                double σ2 = 0;
                double zncc = 0;

                for (int i = 0; i < total; ++i)
                {
                    avg1 += fst[i].Average;
                    avg2 += snd[i].Average;
                }

                avg1 /= total;
                avg2 /= total;

                for (int i = 0; i < total; ++i)
                {
                    σ1 += Math.Pow(fst[i].Average - avg1, 2);
                    σ2 += Math.Pow(snd[i].Average - avg2, 2);
                }

                σ1 = Math.Sqrt(σ1 / total);
                σ2 = Math.Sqrt(σ2 / total);

                for (int i = 0; i < total; ++i)
                    zncc += (fst[i].Average - avg1) * (snd[i].Average - avg2);

                return zncc / (σ1 * σ2 * total);
            }));
    }

    public static HDRBitmap ToHDR(this Bitmap bmp) => new(bmp);

    public static BitmapMask ToMask(this Bitmap bmp, Func<RGBAColor, Scalar> func, bool ignore_alpha = false) => BitmapMask.FromBitmap(bmp, func, ignore_alpha);

    public static BitmapMask ToApproximationMask(this Bitmap bmp, DiscreteColorMap colormap) => BitmapMask.FromApproximation(bmp, colormap);

    public static BitmapMask ToLumaMask(this Bitmap bmp) => BitmapMask.FromLuma(bmp);

    public static BitmapMask ToLumaInvertedMask(this Bitmap bmp) => BitmapMask.FromLumaInverted(bmp);

    public static BitmapMask ToAlphaMask(this Bitmap bmp) => BitmapMask.FromAlpha(bmp);

    public static BitmapMask ToAlphaInvertedMask(this Bitmap bmp) => BitmapMask.FromAlphaInverted(bmp);

    public static BitmapMask ToSaturationMask(this Bitmap bmp) => BitmapMask.FromSaturation(bmp);

    public static BitmapMask ToHueMask(this Bitmap bmp) => BitmapMask.FromHue(bmp);

    public static BitmapMask ToChannelMask(this Bitmap bmp, params ColorChannel[] channels) => BitmapMask.FromChannels(bmp, channels);

    public static ColorPalette ToColorPalette(this Bitmap bmp) => ColorPalette.FromImage(bmp);

    public static Bitmap ReduceColorSpace(this Bitmap bmp, ColorPalette palette, ColorEqualityMetric metric = ColorEqualityMetric.RGBChannels) =>
        new ReduceColorSpace(palette, metric).ApplyTo(bmp);

    public static DensityFunction<RGBAColor> GetHistogram(this Bitmap bmp) => bmp.ToPixelArray().GenerateDensityFunction();

    public static RegressionDataSet1D GetHistogram(this Bitmap bmp, params ColorChannel[] channels) => bmp.ToChannelMask(channels).GetHistogram();

    public static RegressionDataSet1D GetLumaHistogram(this Bitmap bmp) => bmp.ToLumaMask().GetHistogram();

    public static RegressionDataSet1D GetAlphaHistogram(this Bitmap bmp) => bmp.ToAlphaMask().GetHistogram();

    public static RegressionDataSet1D GetSaturationHistogram(this Bitmap bmp) => bmp.ToSaturationMask().GetHistogram();

    public static Shape2DRasterizer GetShape2DRasterizer(this Bitmap bmp) => new(bmp);

    /// <summary>
    /// Crops or extends the bitmap to the given dimensions.
    /// The extended regions will be filled with the color <see cref="RGBAColor.Transparent"/>.
    /// This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="width">New bitmap width. This value must be greater than zero.</param>
    /// <param name="height">New bitmap height. This value must be greater than zero.</param>
    /// <returns>Cropped/Extended bitmap.</returns>
    public static Bitmap CropTo(this Bitmap bmp, int width, int height) => CropTo(bmp, 0, 0, width - bmp.Width, height - bmp.Height);

    /// <summary>
    /// Crops or extends the bitmap bounds by the given offsets.
    /// The extended regions will be filled with the color <see cref="RGBAColor.Transparent"/>.
    /// This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="left">The left offset. A negative amount crops the bitmap on the left side by the given value. A positive amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="top">The top offset. A negative amount crops the bitmap on the top side by the given value. A positive amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="right">The right offset. A negative amount crops the bitmap on the right side by the given value. A positive amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <param name="bottom">The bottom offset. A negative amount crops the bitmap on the bottom side by the given value. A positive amount extends the bound by the given value. The extended region will be filled with the color <see cref="RGBAColor.Transparent"/>.</param>
    /// <returns>Cropped/Extended bitmap.</returns>
    public static Bitmap CropBy(this Bitmap bmp, int left, int top, int right, int bottom) => Crop.By(left, top, right, bottom).ApplyTo(bmp);

    public static Bitmap CropTo(this Bitmap bmp, int left, int top, int right, int bottom) => Crop.To(left, top, right, bottom).ApplyTo(bmp);

    public static Bitmap CropTo(this Bitmap bmp, Range horizontal, Range vertical)
    {
        int hs = horizontal.Start.GetOffset(bmp.Width);
        int he = horizontal.End.GetOffset(bmp.Width);
        int vs = vertical.Start.GetOffset(bmp.Height);
        int ve = vertical.End.GetOffset(bmp.Height);
        Rectangle rect = new(hs, vs, he - hs, ve - vs);

        return CropTo(bmp, rect);
    }

    public static Bitmap CropTo(this Bitmap bmp, (Range horizontal, Range vertical) region) => CropTo(bmp, region.horizontal, region.vertical);

    public static Bitmap CropTo(this Bitmap bmp, Rectangle region) => CropTo(bmp, region.Left, region.Top, region.Right, region.Bottom);

    /// <summary>
    /// Scales the bitmap uniformly by the given scaling factors. This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="factor">The scaling factor. This value must be greater than zero.</param>
    /// <returns>The scaled/resized bitmap.</returns>
    public static Bitmap Scale(this Bitmap bmp, Scalar factor) => Scale(bmp, factor, factor);

    /// <summary>
    /// Scales the bitmap by the given X- and Y-dimension factors. This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="factor">A composite of the X and Y scaling factors. These values must be greater than zero.</param>
    /// <returns>The scaled/resized bitmap.</returns>
    public static Bitmap Scale(this Bitmap bmp, Vector2 factor) => Scale(bmp, factor.X, factor.Y);

    /// <summary>
    /// Scales the bitmap by the given X- and Y-dimension factors. This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="xfactor">Scaling factor in X-dimension. This value must be greater than zero.</param>
    /// <param name="yfactor">Scaling factor in Y-dimension. This value must be greater than zero.</param>
    /// <returns>The scaled/resized bitmap.</returns>
    public static Bitmap Scale(this Bitmap bmp, Scalar xfactor, Scalar yfactor) => new Scale(xfactor, yfactor).ApplyTo(bmp);

    /// <summary>
    /// Resizes the bitmap to match the given new dimensions. This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="width">The new bitmap width.</param>
    /// <param name="height">The new bitmap height.</param>
    /// <returns>The resized bitmap.</returns>
    public static Bitmap ResizeBitmap(this Bitmap bmp, int width, int height) => new(bmp, width, height);

    /// <summary>
    /// Resizes the bitmap to match the given new aspect ratio. This is a non-destructive operation.
    /// </summary>
    /// <param name="bmp">Input bitmap.</param>
    /// <param name="aspect_ratio">New aspect ratio (width/height).</param>
    /// <returns>The resized bitmap.</returns>
    public static Bitmap ChangeAspectRatio(this Bitmap bmp, Scalar aspect_ratio)
    {
        Scalar r = aspect_ratio.MultiplicativeInverse * bmp.Width / bmp.Height;

        if (r.IsOne)
            return bmp;
        else if (r < 1)
            return Scale(bmp, r, 1);
        else
            return Scale(bmp, 1, r.MultiplicativeInverse);
    }

    public static Bitmap ApplyEffect<T>(this Bitmap bmp) where T : BitmapEffect, new() => bmp.ApplyEffect(new T());

    public static Bitmap ApplyEffect(this Bitmap bmp, BitmapEffect effect) => effect.ApplyTo(bmp);

    /// <summary>
    /// Applies the given bitmap effect to a given region of the given bitmap.
    /// </summary>
    /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
    /// <param name="effect">Bitmap effect</typeparam>
    /// <param name="region">Region, in which the effect shall be applied.</param>
    /// <returns>Processed bitmap bitmap</returns>
    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, Rectangle region) => effect.ApplyTo(bmp, region);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, Rectangle region, Scalar intensity) => effect.ApplyTo(bmp, region, intensity);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, Scalar intensity) => effect.ApplyTo(bmp, intensity);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, Range horizontal, Range vertical, Scalar intensity) => effect.ApplyTo(bmp, horizontal, vertical, intensity);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, Range horizontal, Range vertical) => effect.ApplyTo(bmp, horizontal, vertical);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, (Range Horizontal, Range Vertical) region, Scalar intensity) => effect.ApplyTo(bmp, region, intensity);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, (Range Horizontal, Range Vertical) region) => effect.ApplyTo(bmp, region);

    public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, BitmapMask mask) => effect.ApplyTo(bmp, mask);

    public static int CountPixel(this Bitmap bmp, RGBAColor color) => CountPixels(bmp, color)[color];

    public static ReadOnlyDictionary<RGBAColor, int> CountPixels(this Bitmap bmp, params RGBAColor[] colors) => CountPixels(bmp, colors as IEnumerable<RGBAColor>);

    public static ReadOnlyDictionary<RGBAColor, int> CountPixels(this Bitmap bmp, IEnumerable<RGBAColor> colors)
    {
        ConcurrentDictionary<RGBAColor, int> counts = new();

        bmp.LockRGBAPixels((ptr, w, h) => Parallel.For(0, w * h, idx =>
        {
            if (!counts.TryGetValue(ptr[idx], out int count))
                count = 0;

            counts[ptr[idx]] = count + 1;
        }));

        return counts.AsReadOnly();
    }

    public static Bitmap Replace(this Bitmap bmp, RGBAColor search, RGBAColor replace) => bmp.ApplyEffect(new ReplaceColor(search, replace));

    public static Bitmap Replace(this Bitmap bmp, RGBAColor search, RGBAColor replace, ColorTolerance tolerance) => bmp.ApplyEffect(new ReplaceColor(search, replace, tolerance));

    public static Bitmap Replace(this Bitmap bmp, IEnumerable<(RGBAColor search, RGBAColor replace)> pairs) => bmp.ApplyEffect(new ReplaceColor(pairs));

    public static Bitmap Replace(this Bitmap bmp, IEnumerable<(RGBAColor search, RGBAColor replace)> pairs, ColorTolerance tolerance) => bmp.ApplyEffect(new ReplaceColor(pairs, tolerance));

    public static Bitmap Replace(this Bitmap bmp, IEnumerable<RGBAColor> search, RGBAColor replace) => bmp.ApplyEffect(new ReplaceColor(search, replace));

    public static Bitmap Replace(this Bitmap bmp, IEnumerable<RGBAColor> search, RGBAColor replace, ColorTolerance tolerance) => bmp.ApplyEffect(new ReplaceColor(search, replace, tolerance));

    public static Bitmap Remove(this Bitmap bmp, RGBAColor color) => bmp.ApplyEffect(new RemoveColor(color));

    public static Bitmap Remove(this Bitmap bmp, RGBAColor color, ColorTolerance tolerance) => bmp.ApplyEffect(new RemoveColor(color, tolerance));

    public static Bitmap Remove(this Bitmap bmp, IEnumerable<RGBAColor> colors) => bmp.ApplyEffect(new RemoveColor(colors));

    public static Bitmap Remove(this Bitmap bmp, IEnumerable<RGBAColor> colors, ColorTolerance tolerance) => bmp.ApplyEffect(new RemoveColor(colors, tolerance));

    public static Bitmap Replace(this Bitmap bmp, Bitmap replacement, Rectangle region) =>
        Blend(bmp, replacement, BlendMode.Top, region);

    public static Bitmap Replace(this Bitmap bmp, Bitmap replacement, BitmapMask mask) =>
        Blend(bmp, replacement, BlendMode.Top, mask);

    public static Bitmap Replace(this Bitmap bmp, Bitmap replacement, (Range Horizontal, Range Vertical) region) =>
        Blend(bmp, replacement, BlendMode.Top, region);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode) => Blend(bottom_layer, top_layer, mode, Scalar.One);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, Scalar amount) =>
        new BitmapBlend(bottom_layer, mode, 1).ApplyTo(top_layer, amount);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, BitmapMask mask) =>
        new BitmapBlend(bottom_layer, mode, 1).ApplyTo(top_layer, mask);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, Rectangle region) =>
        Blend(bottom_layer, top_layer, mode, region, Scalar.One);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, Rectangle region, Scalar amount) =>
        new BitmapBlend(bottom_layer, mode, 1).ApplyTo(top_layer, region, amount);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, (Range Horizontal, Range Vertical) region) =>
        Blend(bottom_layer, top_layer, mode, region, Scalar.One);

    public static Bitmap Blend(this Bitmap bottom_layer, Bitmap top_layer, BlendMode mode, (Range Horizontal, Range Vertical) region, Scalar amount) =>
        new BitmapBlend(bottom_layer, mode, 1).ApplyTo(top_layer, region, amount);

    /// <summary>
    /// Sets the EXIF tag of the given bitmap to the given value.
    /// </summary>
    /// <param name="bmp">The bitmap.</param>
    /// <param name="tag">The EXIF tag.</param>
    /// <param name="value">The new EXIF value.</param>
    /// <param name="type">The new EXIF data type.</param>
    public static void SetExifData(this Bitmap bmp, ExifTag tag, byte[]? value, ExifDataType type = ExifDataType.ByteArray)
    {
        PropertyItem prop = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));

        prop.Id = (int)tag;
        prop.Type = (short)type;
        prop.Value = value;
        prop.Len = value?.Length ?? 0;

        bmp.SetPropertyItem(prop);
    }

    /// <summary>
    /// Sets the EXIF tag of the given bitmap to the given string value.
    /// </summary>
    /// <param name="bmp">The bitmap.</param>
    /// <param name="tag">The EXIF tag.</param>
    /// <param name="value">The new EXIF value.</param>
    public static void SetExifData(this Bitmap bmp, ExifTag tag, string value) => bmp.SetExifData(tag, BytewiseEncoding.Instance.GetBytes(value + '\0'), ExifDataType.NullTerminatedAsciiString);

    public static void SetExifData(this Bitmap bmp, ExifTag tag, ushort value) => SetExifData(bmp, tag, new[] { value });

    public static void SetExifData(this Bitmap bmp, ExifTag tag, uint value) => SetExifData(bmp, tag, new[] { value });

    public static void SetExifData(this Bitmap bmp, ExifTag tag, int value) => SetExifData(bmp, tag, new[] { value });

    public static void SetExifData(this Bitmap bmp, ExifTag tag, Fraction value) => SetExifData(bmp, tag, new[] { value });

    public static void SetExifData(this Bitmap bmp, ExifTag tag, ushort[] value) => bmp.SetExifData(tag, DataStream.FromArray(value), ExifDataType.UInt16Array);

    public static void SetExifData(this Bitmap bmp, ExifTag tag, uint[] value) => bmp.SetExifData(tag, DataStream.FromArray(value), ExifDataType.UInt32Array);

    public static void SetExifData(this Bitmap bmp, ExifTag tag, int[] value) => bmp.SetExifData(tag, DataStream.FromArray(value), ExifDataType.Int32Array);

    public static void SetExifData(this Bitmap bmp, ExifTag tag, Fraction[] value) =>
        bmp.SetExifData(tag, DataStream.FromArray(value.Select(f => ((int)f.Numerator, (int)f.Denominator))), ExifDataType.Int32FractionArray);

    /// <summary>
    /// Fetches the raw EXIF value in byte from the given bitmap.
    /// </summary>
    /// <param name="bmp">The bitmap.</param>
    /// <param name="tag">The EXIF tag.</param>
    /// <param name="value">The associated EXIF value.</param>
    /// <param name="type">The EXIF data type.</param>
    /// <returns>Indicates whether the value could be successfully fetched.</returns>
    public static bool GetRawExifData(this Bitmap bmp, ExifTag tag, [NotNullWhen(true)] out byte[]? value, out ExifDataType type)
    {
        value = null;
        type = ExifDataType.ByteArray;

        if (bmp.GetPropertyItem((int)tag) is PropertyItem property)
        {
            value = property.Value?[..property.Len] ?? Array.Empty<byte>();
            type = (ExifDataType)property.Type;
        }

        return value is { };
    }

    /// <summary>
    /// Fetches the EXIF value associated with the given EXIF tag and returns whether the operation was successful.
    /// The <paramref name="value"/>-parameter will be populated with the EXIF data.
    /// It is guaranteed to have one of the following types:
    /// <list type="bullet">
    /// <item><see cref="string"/></item>
    /// <item><see cref="ushort"/>[]</item>
    /// <item><see cref="uint"/>[]</item>
    /// <item><see cref="int"/>[]</item>
    /// <item><see cref="Fraction"/>[]</item>
    /// <item><see cref="byte"/>[]</item>
    /// </list>
    /// </summary>
    /// <param name="bmp">The bitmap.</param>
    /// <param name="tag">The EXIF tag.</param>
    /// <param name="value">The associated EXIF value.</param>
    /// <returns>Indicates whether the value could be successfully fetched.</returns>
    public static bool GetExifData(this Bitmap bmp, ExifTag tag, [NotNullWhen(true)] out object? value)
    {
        value = null;

        if (bmp.GetRawExifData(tag, out byte[]? bytes, out ExifDataType type))
            value = type switch
            {
                ExifDataType.NullTerminatedAsciiString => BytewiseEncoding.Instance.GetString(bytes[..^1]),
                ExifDataType.UInt16Array => DataStream.FromBytes(bytes).ToArray<ushort>(),
                ExifDataType.UInt32Array => DataStream.FromBytes(bytes).ToArray<uint>(),
                ExifDataType.Int32Array => DataStream.FromBytes(bytes).ToArray<int>(),
                ExifDataType.UInt32FractionArray => DataStream.FromBytes(bytes).ToArray<(uint n, uint d)>().ToArray(t => new Fraction(t.n, t.d)),
                ExifDataType.Int32FractionArray => DataStream.FromBytes(bytes).ToArray<(int n, int d)>().ToArray(t => new Fraction(t.n, (ulong)t.d)),
                ExifDataType.ByteArray or ExifDataType.Arbitrary or _ => bytes,
            };

        return value is { };
    }

    [return: MaybeNull]
    public static T? GetExifData<T>(this Bitmap bmp, ExifTag tag) => bmp.GetExifData(tag, out object? value) ? (T)value : default;
}

public enum ExifDataType
    : short
{
    ByteArray = 1,
    NullTerminatedAsciiString = 2,
    UInt16Array = 3,
    UInt32Array = 4,
    UInt32FractionArray = 5,
    Arbitrary = 6,
    Int32Array = 7,
    Int32FractionArray = 10,
}

public enum ExifTag
    : int
{
    GpsVer = 0x0000,
    GpsLatitudeRef = 0x0001,
    GpsLatitude = 0x0002,
    GpsLongitudeRef = 0x0003,
    GpsLongitude = 0x0004,
    GpsAltitudeRef = 0x0005,
    GpsAltitude = 0x0006,
    GpsGpsTime = 0x0007,
    GpsGpsSatellites = 0x0008,
    GpsGpsStatus = 0x0009,
    GpsGpsMeasureMode = 0x000A,
    GpsGpsDop = 0x000B,
    GpsSpeedRef = 0x000C,
    GpsSpeed = 0x000D,
    GpsTrackRef = 0x000E,
    GpsTrack = 0x000F,
    GpsImgDirRef = 0x0010,
    GpsImgDir = 0x0011,
    GpsMapDatum = 0x0012,
    GpsDestLatRef = 0x0013,
    GpsDestLat = 0x0014,
    GpsDestLongRef = 0x0015,
    GpsDestLong = 0x0016,
    GpsDestBearRef = 0x0017,
    GpsDestBear = 0x0018,
    GpsDestDistRef = 0x0019,
    GpsDestDist = 0x001A,
    NewSubfileType = 0x00FE,
    SubfileType = 0x00FF,
    ImageWidth = 0x0100,
    ImageHeight = 0x0101,
    BitsPerSample = 0x0102,
    Compression = 0x0103,
    PhotometricInterp = 0x0106,
    ThreshHolding = 0x0107,
    CellWidth = 0x0108,
    CellHeight = 0x0109,
    FillOrder = 0x010A,
    DocumentName = 0x010D,
    ImageDescription = 0x010E,
    EquipMake = 0x010F,
    EquipModel = 0x0110,
    StripOffsets = 0x0111,
    Orientation = 0x0112,
    SamplesPerPixel = 0x0115,
    RowsPerStrip = 0x0116,
    StripBytesCount = 0x0117,
    MinSampleValue = 0x0118,
    MaxSampleValue = 0x0119,
    XResolution = 0x011A,
    YResolution = 0x011B,
    PlanarConfig = 0x011C,
    PageName = 0x011D,
    XPosition = 0x011E,
    YPosition = 0x011F,
    FreeOffset = 0x0120,
    FreeByteCounts = 0x0121,
    GrayResponseUnit = 0x0122,
    GrayResponseCurve = 0x0123,
    T4Option = 0x0124,
    T6Option = 0x0125,
    ResolutionUnit = 0x0128,
    PageNumber = 0x0129,
    TransferFunction = 0x012D,
    SoftwareUsed = 0x0131,
    DateTime = 0x0132,
    Artist = 0x013B,
    HostComputer = 0x013C,
    Predictor = 0x013D,
    WhitePoint = 0x013E,
    PrimaryChromaticities = 0x013F,
    ColorMap = 0x0140,
    HalftoneHints = 0x0141,
    TileWidth = 0x0142,
    TileLength = 0x0143,
    TileOffset = 0x0144,
    TileByteCounts = 0x0145,
    InkSet = 0x014C,
    InkNames = 0x014D,
    NumberOfInks = 0x014E,
    DotRange = 0x0150,
    TargetPrinter = 0x0151,
    ExtraSamples = 0x0152,
    SampleFormat = 0x0153,
    SMinSampleValue = 0x0154,
    SMaxSampleValue = 0x0155,
    TransferRange = 0x0156,
    JPEGProc = 0x0200,
    JPEGInterFormat = 0x0201,
    JPEGInterLength = 0x0202,
    JPEGRestartInterval = 0x0203,
    JPEGLosslessPredictors = 0x0205,
    JPEGPointTransforms = 0x0206,
    JPEGQTables = 0x0207,
    JPEGDCTables = 0x0208,
    JPEGACTables = 0x0209,
    YCbCrCoefficients = 0x0211,
    YCbCrSubsampling = 0x0212,
    YCbCrPositioning = 0x0213,
    REFBlackWhite = 0x0214,
    Gamma = 0x0301,
    ICCProfileDescriptor = 0x0302,
    SRGBRenderingIntent = 0x0303,
    ImageTitle = 0x0320,
    ResolutionXUnit = 0x5001,
    ResolutionYUnit = 0x5002,
    ResolutionXLengthUnit = 0x5003,
    ResolutionYLengthUnit = 0x5004,
    PrintFlags = 0x5005,
    PrintFlagsVersion = 0x5006,
    PrintFlagsCrop = 0x5007,
    PrintFlagsBleedWidth = 0x5008,
    PrintFlagsBleedWidthScale = 0x5009,
    HalftoneLPI = 0x500A,
    HalftoneLPIUnit = 0x500B,
    HalftoneDegree = 0x500C,
    HalftoneShape = 0x500D,
    HalftoneMisc = 0x500E,
    HalftoneScreen = 0x500F,
    JPEGQuality = 0x5010,
    GridSize = 0x5011,
    ThumbnailFormat = 0x5012,
    ThumbnailWidth = 0x5013,
    ThumbnailHeight = 0x5014,
    ThumbnailColorDepth = 0x5015,
    ThumbnailPlanes = 0x5016,
    ThumbnailRawBytes = 0x5017,
    ThumbnailSize = 0x5018,
    ThumbnailCompressedSize = 0x5019,
    ColorTransferFunction = 0x501A,
    ThumbnailData = 0x501B,
    ThumbnailImageWidth = 0x5020,
    ThumbnailImageHeight = 0x5021,
    ThumbnailBitsPerSample = 0x5022,
    ThumbnailCompression = 0x5023,
    ThumbnailPhotometricInterp = 0x5024,
    ThumbnailImageDescription = 0x5025,
    ThumbnailEquipMake = 0x5026,
    ThumbnailEquipModel = 0x5027,
    ThumbnailStripOffsets = 0x5028,
    ThumbnailOrientation = 0x5029,
    ThumbnailSamplesPerPixel = 0x502A,
    ThumbnailRowsPerStrip = 0x502B,
    ThumbnailStripBytesCount = 0x502C,
    ThumbnailResolutionX = 0x502D,
    ThumbnailResolutionY = 0x502E,
    ThumbnailPlanarConfig = 0x502F,
    ThumbnailResolutionUnit = 0x5030,
    ThumbnailTransferFunction = 0x5031,
    ThumbnailSoftwareUsed = 0x5032,
    ThumbnailDateTime = 0x5033,
    ThumbnailArtist = 0x5034,
    ThumbnailWhitePoint = 0x5035,
    ThumbnailPrimaryChromaticities = 0x5036,
    ThumbnailYCbCrCoefficients = 0x5037,
    ThumbnailYCbCrSubsampling = 0x5038,
    ThumbnailYCbCrPositioning = 0x5039,
    ThumbnailRefBlackWhite = 0x503A,
    ThumbnailCopyRight = 0x503B,
    LuminanceTable = 0x5090,
    ChrominanceTable = 0x5091,
    FrameDelay = 0x5100,
    LoopCount = 0x5101,
    GlobalPalette = 0x5102,
    IndexBackground = 0x5103,
    IndexTransparent = 0x5104,
    PixelUnit = 0x5110,
    PixelPerUnitX = 0x5111,
    PixelPerUnitY = 0x5112,
    PaletteHistogram = 0x5113,
    Copyright = 0x8298,
    ExifExposureTime = 0x829A,
    ExifFNumber = 0x829D,
    ExifIFD = 0x8769,
    ICCProfile = 0x8773,
    ExifExposureProg = 0x8822,
    ExifSpectralSense = 0x8824,
    GpsIFD = 0x8825,
    ExifISOSpeed = 0x8827,
    ExifOECF = 0x8828,
    ExifVer = 0x9000,
    ExifDTOrig = 0x9003,
    ExifDTDigitized = 0x9004,
    ExifCompConfig = 0x9101,
    ExifCompBPP = 0x9102,
    ExifShutterSpeed = 0x9201,
    ExifAperture = 0x9202,
    ExifBrightness = 0x9203,
    ExifExposureBias = 0x9204,
    ExifMaxAperture = 0x9205,
    ExifSubjectDist = 0x9206,
    ExifMeteringMode = 0x9207,
    ExifLightSource = 0x9208,
    ExifFlash = 0x9209,
    ExifFocalLength = 0x920A,
    ExifMakerNote = 0x927C,
    ExifUserComment = 0x9286,
    ExifDTSubsec = 0x9290,
    ExifDTOrigSS = 0x9291,
    ExifDTDigSS = 0x9292,
    ExifFPXVer = 0xA000,
    ExifColorSpace = 0xA001,
    ExifPixXDim = 0xA002,
    ExifPixYDim = 0xA003,
    ExifRelatedWav = 0xA004,
    ExifInterop = 0xA005,
    ExifFlashEnergy = 0xA20B,
    ExifSpatialFR = 0xA20C,
    ExifFocalXRes = 0xA20E,
    ExifFocalYRes = 0xA20F,
    ExifFocalResUnit = 0xA210,
    ExifSubjectLoc = 0xA214,
    ExifExposureIndex = 0xA215,
    ExifSensingMethod = 0xA217,
    ExifFileSource = 0xA300,
    ExifSceneType = 0xA301,
    ExifCfaPattern = 0xA302,
}
