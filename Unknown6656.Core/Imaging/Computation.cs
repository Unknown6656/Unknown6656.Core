using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Imaging.Effects;

namespace Unknown6656.Imaging;


public abstract class BitmapComputation<T>
{
    public abstract T Compute(Bitmap bitmap);
}

public abstract class PartialBitmapComputation<T>
    : BitmapComputation<T>
{
    public T Compute(Bitmap bmp, Rectangle region)
    {
        bmp = bmp.ApplyEffect(Crop.To(region));

        return Compute(bmp);
    }

    public T Compute(Bitmap bmp, (Range Horizontal, Range Vertical) region) => Compute(bmp, region.Horizontal, region.Vertical);

    public T Compute(Bitmap bmp, Range horizontal, Range vertical)
    {
        int hs = horizontal.Start.GetOffset(bmp.Width);
        int he = horizontal.End.GetOffset(bmp.Width);
        int vs = vertical.Start.GetOffset(bmp.Height);
        int ve = vertical.End.GetOffset(bmp.Height);
        Rectangle rect = new(hs, vs, he - hs, ve - vs);

        return Compute(bmp, rect);
    }
}

public class BitmapTemperature
    : PartialBitmapComputation<double>
{
    public double SamplingRate { get; set; } = .5;


    public override unsafe double Compute(Bitmap bitmap)
    {
        if (SamplingRate > 1 || SamplingRate < 0)
            throw new ArgumentOutOfRangeException(nameof(SamplingRate));

        double[] temperatures = new double[(int)(bitmap.Width * bitmap.Height * SamplingRate)];
        int stepsize = bitmap.Width * bitmap.Height / temperatures.Length;
        BitmapLocker locker = bitmap;

        if (temperatures.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(SamplingRate));

        locker.LockPixels((ptr, w, h) => Parallel.For(0, temperatures.Length, i => temperatures[i] = ptr[i * stepsize]));

        return temperatures.Average();
    }
}
