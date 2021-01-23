using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System;


namespace Unknown6656.Imaging
{
    public unsafe delegate void BitmapLockerCallback<T>(T* pixels, int width, int height) where T : unmanaged;

    public unsafe delegate U BitmapLockerCallback<T, U>(T* pixels, int width, int height) where T : unmanaged;

    public unsafe class BitmapLocker
    {
        public Bitmap Bitmap { get; }

        public int Width => Bitmap.Width;

        public int Height => Bitmap.Height;

        public PixelFormat PixelFormat => Bitmap.PixelFormat;


        public BitmapLocker(Bitmap bitmap) => Bitmap = bitmap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LockPixels(BitmapLockerCallback<byte> callback)
        {
            BitmapData dat = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadWrite, Bitmap.PixelFormat);

            try
            {
                callback((byte*)dat.Scan0, Bitmap.Width, Bitmap.Height);
            }
            finally
            {
                Bitmap.UnlockBits(dat);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LockPixels<T>(BitmapLockerCallback<T> callback)
            where T : unmanaged
        {
            BitmapData dat = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadWrite, Bitmap.PixelFormat);

            if (dat.Stride / dat.Width != sizeof(T))
                throw new InvalidOperationException($"A bitmap with the pixel format {Bitmap.PixelFormat} cannot be locked using a struct of the type '{typeof(T)}' as the sizes do not match ('{typeof(T)}' must have a size of {dat.Stride / dat.Width} bytes, however, it is {sizeof(T)} bytes large).");

            try
            {
                callback((T*)dat.Scan0, Bitmap.Width, Bitmap.Height);
            }
            finally
            {
                Bitmap.UnlockBits(dat);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LockRGBAPixels(BitmapLockerCallback<RGBAColor> callback) => LockPixels(callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LockRGBAPixels<T>(BitmapLockerCallback<RGBAColor, T> callback)
        {
            T value = default!;

            LockRGBAPixels((ptr, w, h) => value = callback(ptr, w, h));

            return value;
        }

        public RGBAColor[] ToRGBAPixels()
        {
            RGBAColor[]? arr = null;

            LockRGBAPixels((ptr, w, h) =>
            {
                arr = new RGBAColor[w * h];

                Parallel.For(0, arr.Length, i => arr[i] = ptr[i]);
            });

            return arr!;
        }

        public static implicit operator BitmapLocker(Bitmap bmp) => new BitmapLocker(bmp);

        public static implicit operator Bitmap(BitmapLocker lck) => lck.Bitmap;
    }
}
