using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SubtitleParser.Common
{
    public class FastBitmap
    {
        private byte[] _bitmapData;

        private int _width = 0;
        private int _height = 0;

        public FastBitmap(Bitmap bmp)
        {
            _width = bmp.Width;
            _height = bmp.Height;

            bool createdNewBitmap = false;
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var newBitmap = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                using (var gr = Graphics.FromImage(newBitmap))
                {
                    gr.DrawImage(bmp, 0, 0);
                }
                bmp = newBitmap;
                createdNewBitmap = true;
            }

            var bitmapData = bmp.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            _bitmapData = new byte[bitmapData.Stride * _height];
            Marshal.Copy(bitmapData.Scan0, _bitmapData, 0, _bitmapData.Length);
            bmp.UnlockBits(bitmapData);
            if (createdNewBitmap)
            {
                bmp.Dispose();
            }
        }

        public FastBitmap(Size size) :
            this(size.Width, size.Height)
        {
        }

        public FastBitmap(int width, int height)
        {
            _width = width;
            _height = height;
            _bitmapData = new byte[_height * _width * 4];
        }

        public void SetPixel(int x, int y, Color color)
        {
            var pos = (x * 4) + (y * _width * 4);
            _bitmapData[pos] = color.B;
            _bitmapData[pos + 1] = color.G;
            _bitmapData[pos + 2] = color.R;
            _bitmapData[pos + 3] = color.A;
        }

        //public void DropSpace()
        //{
        //    for()

        //}

        public void AddMargin(int margin, Color color)
        {
            int newWidth = _width + margin * 2;
            int newHeight = _height + margin * 2;
            var newBitmapData = new byte[newWidth * newHeight * 4];

            byte[] byt = new byte[] { color.R, color.G, color.B, color.A };


            for (int y = 0; y < newHeight; y++)
            {
                //top margin and bottom margin
                if (y < margin || y >= _height + margin)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        Buffer.BlockCopy(byt, 0, newBitmapData, (y * newWidth * 4) + x * 4, 4);
                    }
                }
                else
                {
                    //left Margin
                    for (int x = 0; x < margin; x++)
                    {
                        Buffer.BlockCopy(byt, 0, newBitmapData, y * newWidth * 4 + x * 4, 4);
                    }
                    //body
                    Buffer.BlockCopy(_bitmapData, (y - margin) * _width * 4, newBitmapData, y * newWidth * 4 + margin * 4, _width * 4);

                    //right margin
                    for (int x = _width + margin; x < newWidth; x++)
                    {
                        Buffer.BlockCopy(byt, 0, newBitmapData, y * newWidth * 4 + x * 4, 4);
                    }
                }
            }

            _width = newWidth;
            _height = newHeight;
            _bitmapData = newBitmapData;
        }

        public Bitmap GetBitmap()
        {
            var bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var destination = bitmapData.Scan0;
            Marshal.Copy(_bitmapData, 0, destination, _bitmapData.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public void SaveAs(string fileName)
        {
            var bmp = GetBitmap();

            ImageFormat fmt = ImageFormat.Bmp;

            if (fileName.ToLower().EndsWith(".jpg"))
            {
                fmt = ImageFormat.Jpeg;
            }
            else if (fileName.ToLower().EndsWith(".png"))
            {
                fmt = ImageFormat.Png;
            }
            else if (fileName.ToLower().EndsWith(".gif"))
            {
                fmt = ImageFormat.Gif;
            }
            else if (fileName.ToLower().EndsWith(".tiff") || fileName.ToLower().EndsWith(".tif"))
            {
                fmt = ImageFormat.Tiff;
            }

            bmp.Save(fileName, fmt);
        }
    }
}