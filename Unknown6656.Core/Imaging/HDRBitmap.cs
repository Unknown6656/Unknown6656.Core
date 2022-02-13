using System.Drawing.Imaging;
using System.Drawing;

using Unknown6656.Mathematics.LinearAlgebra;


namespace Unknown6656.Imaging
{
    // VEC4 == (R, G, B, A) == (X, Y, Z, W)

    public unsafe class HDRBitmap
        : WritableMatrixNM<Vector4>
    {
        public int Width => ColumnCount;
        public int Height => RowCount;


        public HDRBitmap(int width, int height)
            : base(width, height)
        {
        }

        public HDRBitmap(WritableMatrixNM<Vector4> pixels)
            : base(pixels)
        {
        }

        public HDRBitmap(Bitmap bitmap)
            : this(bitmap.Width, bitmap.Height) => ReadFromBitmap(bitmap);

        private void ReadFromBitmap(Bitmap bmp)
        {
            using Bitmap tmp = new(bmp);
            using Bitmap copy = tmp.Clone(new Rectangle(0, 0, tmp.Width, tmp.Height), PixelFormat.Format32bppArgb);
            BitmapLocker lck = copy;
            Scalar factor = new Scalar(255d).Inverse;

            lck.LockPixels((px, w, h) =>
            {
                for (int i = 0, l = w * h; i < l; ++i)
                    _coefficients[i] = new Vector4(
                        px[i * 4 + 2], // r
                        px[i * 4 + 1], // g
                        px[i * 4 + 0], // b
                        px[i * 4 + 3] // a
                    ) * factor;
            });
        }

        public Bitmap ToBitmap()
        {
            Bitmap bmp = new(Width, Height, PixelFormat.Format32bppArgb);
            BitmapLocker lck = bmp;
            Scalar factor = 255;

            lck.LockPixels((px, w, h) =>
            {
                for (int i = 0, l = w * h; i < l; ++i)
                {
                    Vector4 col = _coefficients[i].Value;

                    px[i * 4 + 2] = (byte)col.X.Clamp().Multiply(factor); // r
                    px[i * 4 + 1] = (byte)col.Y.Clamp().Multiply(factor); // g
                    px[i * 4 + 0] = (byte)col.Z.Clamp().Multiply(factor); // b
                    px[i * 4 + 3] = (byte)col.W.Clamp().Multiply(factor); // a
                }
            });

            return bmp;
        }

        public void LockPixels(BitmapLockerCallback<Vector4> callback)
        {
            fixed (Scalar<Vector4>* ptr = _coefficients)
                callback((Vector4*)ptr, Width, Height);
        }


        // TODO

    }
}
