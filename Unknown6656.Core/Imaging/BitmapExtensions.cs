using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Statistics;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Common;
using Unknown6656.Imaging.Effects;

namespace Unknown6656.Imaging
{
    public static unsafe class BitmapExtensions
    {
        /// <summary>
        /// Converts the given bitmap to an 32-Bit ARGB (alpha, red, green and blue) bitmap
        /// </summary>
        /// <param name="bmp">Input bitmap (any pixel format)</param>
        /// <returns>32-Bit bitmap</returns>
        public static Bitmap ToARGB32(this Bitmap bmp)
        {
            Bitmap res = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(res))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            return res;
        }

        public static RGBAColor[] ToPixelArray(this Bitmap bmp) => new BitmapLocker(bmp).ToRGBAPixels();

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

            return
                first.LockRGBAPixels((fst, w, h) =>
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

        public static HDRBitmap ToHDR(this Bitmap bmp) => new HDRBitmap(bmp);

        public static BitmapMask ToMask(this Bitmap bmp, Func<RGBAColor, Scalar> func, bool ignore_alpha = false) => BitmapMask.FromBitmap(bmp, func, ignore_alpha);

        public static BitmapMask ToApproximationMask(this Bitmap bmp, ColorMap colormap) => BitmapMask.FromApproximation(bmp, colormap);

        public static BitmapMask ToLumaMask(this Bitmap bmp) => BitmapMask.FromLuma(bmp);

        public static BitmapMask ToLumaInvertedMask(this Bitmap bmp) => BitmapMask.FromLumaInverted(bmp);

        public static BitmapMask ToAlphaMask(this Bitmap bmp) => BitmapMask.FromAlpha(bmp);

        public static BitmapMask ToAlphaInvertedMask(this Bitmap bmp) => BitmapMask.FromAlphaInverted(bmp);

        public static BitmapMask ToSaturationMask(this Bitmap bmp) => BitmapMask.FromSaturation(bmp);

        public static BitmapMask ToHueMask(this Bitmap bmp) => BitmapMask.FromHue(bmp);

        public static BitmapMask ToChannelMask(this Bitmap bmp, params BitmapChannel[] channels) => BitmapMask.FromChannels(bmp, channels);

        public static DensityFunction<RGBAColor> GetHistogram(this Bitmap bmp) => bmp.ToPixelArray().GenerateDensityFunction();

        public static RegressionDataSet1D GetHistogram(this Bitmap bmp, params BitmapChannel[] channels) => bmp.ToChannelMask(channels).GetHistogram();

        public static RegressionDataSet1D GetLumaHistogram(this Bitmap bmp) => bmp.ToLumaMask().GetHistogram();

        public static RegressionDataSet1D GetAlphaHistogram(this Bitmap bmp) => bmp.ToAlphaMask().GetHistogram();

        public static RegressionDataSet1D GetSaturationHistogram(this Bitmap bmp) => bmp.ToSaturationMask().GetHistogram();

        public static Shape2DRasterizer GetShape2DRasterizer(this Bitmap bmp) => new Shape2DRasterizer(bmp);

        // TODO : scale up/down
        // TODO : cut image

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

        public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, (Range Horizontal, Range Vertical) region, Scalar intensity) => effect.ApplyTo(bmp, region, intensity);

        public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, (Range Horizontal, Range Vertical) region) => effect.ApplyTo(bmp, region);

        public static Bitmap ApplyEffect(this Bitmap bmp, PartialBitmapEffect effect, BitmapMask mask) => effect.ApplyTo(bmp, mask);
    }

}
