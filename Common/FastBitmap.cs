using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public void Makeup(Color background, AppSettings.Border border)
        {
            var trimd = Trim(background);
            int newWidth = trimd.maxX - trimd.minX + border.Width * 2 + border.Padding * 2;
            int newHeight = trimd.maxY - trimd.minY + border.Width * 2 + border.Padding * 2;

            byte[] bytBG = new byte[] { background.R, background.G, background.B, background.A };
            byte[] bytBD = new byte[] { border.BorderColor.R, border.BorderColor.G, border.BorderColor.B, border.BorderColor.A };

            MemoryStream ms = new MemoryStream();

            //write top border
            if (border.Width > 0)
            {
                for (int y = 0; y < border.Width; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        ms.Write(bytBD, 0, 4);
                    }
                }
            }

            //write top padding
            if (border.Padding > 0)
            {
                for (int y = 0; y < border.Padding; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        if (x < border.Width || x >= newWidth - border.Width)
                        {
                            ms.Write(bytBD, 0, 4);
                        }
                        else
                        {
                            ms.Write(bytBG, 0, 4);
                        }
                    }
                }
            }

            for (int y = trimd.minY; y < trimd.maxY; y++)
            {
                //write left border
                for (int x = 0; x < border.Width; x++)
                {
                    ms.Write(bytBD, 0, 4);
                }
                //write left padding
                for (int x = 0; x < border.Padding; x++)
                {
                    ms.Write(bytBG, 0, 4);
                }

                int offset = (trimd.minX * 4) + (y * _width * 4);
                int count = (trimd.maxX - trimd.minX) * 4;
                ms.Write(_bitmapData, offset, count);

                //write right padding
                for (int x = 0; x < border.Padding; x++)
                {
                    ms.Write(bytBG, 0, 4);
                }

                //write right border
                for (int x = 0; x < border.Width; x++)
                {
                    ms.Write(bytBD, 0, 4);
                }
            }

            //write bottom padding
            if (border.Padding > 0)
            {
                for (int y = 0; y < border.Padding; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        if (x < border.Width || x >= newWidth - border.Width)
                        {
                            ms.Write(bytBD, 0, 4);
                        }
                        else
                        {
                            ms.Write(bytBG, 0, 4);
                        }
                    }
                }
            }

            //write bottom border
            if (border.Width > 0)
            {
                for (int y = 0; y < border.Width; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        ms.Write(bytBD, 0, 4);
                    }
                }
            }

            _bitmapData = ms.ToArray();

            _width = newWidth;
            _height = newHeight;
        }

        private (int minX, int minY, int maxX, int maxY) Trim(Color background)
        {
            int minY = -1, maxY = -1, minX = -1, maxX = -1;
            //Top
            for (int y = 0; y < _height; y++)
            {
                bool canTrim = true;
                for (int x = 0; x < _width; x++)
                {
                    var pos = (x * 4) + (y * _width * 4);
                    if (_bitmapData[pos + 3] != 0 &&
                        (_bitmapData[pos] != background.B ||
                        _bitmapData[pos + 1] != background.G) ||
                        _bitmapData[pos + 2] != background.R)
                    {
                        canTrim = false;
                        break;
                    }
                }

                if (canTrim)
                {
                    minY = y;
                }
                else
                {
                    break;
                }
            }

            //bottom

            for (int y = _height - 1; y >= 0; y--)
            {
                bool canTrim = true;
                for (int x = 0; x < _width; x++)
                {
                    var pos = (x * 4) + (y * _width * 4);
                    if (_bitmapData[pos + 3] != 0 &&
                        (_bitmapData[pos] != background.B ||
                        _bitmapData[pos + 1] != background.G) ||
                        _bitmapData[pos + 2] != background.R)
                    {
                        canTrim = false;
                        break;
                    }
                }

                if (canTrim)
                {
                    maxY = y;
                }
                else
                {
                    break;
                }
            }

            //left
            for (int x = 0; x < _width; x++)
            {
                bool canTrim = true;
                for (int y = 0; y < _height; y++)

                {
                    var pos = (x * 4) + (y * _width * 4);
                    if (_bitmapData[pos + 3] != 0 &&
                        (_bitmapData[pos] != background.B ||
                        _bitmapData[pos + 1] != background.G) ||
                        _bitmapData[pos + 2] != background.R)
                    {
                        canTrim = false;
                        break;
                    }
                }

                if (canTrim)
                {
                    minX = x;
                }
                else
                {
                    break;
                }
            }

            //right
            for (int x = _width - 1; x >= 0; x--)
            {
                bool canTrim = true;
                for (int y = 0; y < _height; y++)

                {
                    var pos = (x * 4) + (y * _width * 4);
                    if (_bitmapData[pos + 3] != 0 &&
                        (_bitmapData[pos] != background.B ||
                        _bitmapData[pos + 1] != background.G) ||
                        _bitmapData[pos + 2] != background.R)
                    {
                        canTrim = false;
                        break;
                    }
                }

                if (canTrim)
                {
                    maxX = x;
                }
                else
                {
                    break;
                }
            }

            if (minX == -1 || minX == 0)
            {
                minX = 0;
            }
            else
            {
                minX--;
            }

            if (minY == -1 || minY == 0)
            {
                minY = 0;
            }
            else
            {
                minY--;
            }

            if (maxX == -1 || maxX == _width - 1)
            {
                maxX = _width - 1;
            }
            else
            {
                maxX++;
            }

            if (maxY == -1 || maxY == _height - 1)
            {
                maxY = _height - 1;
            }
            else
            {
                maxY++;
            }

            return (minX, minY, maxX, maxY);
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