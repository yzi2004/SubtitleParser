using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SubtitleParser.Common
{
    public class FastBitmap
    {
        private Bitmap _bmp = null;
        private BitmapData _bmpData = null;
        private IntPtr _ptr = new IntPtr(0);

        private AppSettings _settings = null;
        private int _xpos = 0;
        private int _ypos = 0;
        private int _width = 0;
        private int _height = 0;

        public FastBitmap(Bitmap bmp, AppSettings appSettings)
        {
            _settings = appSettings;
            _bmp = bmp;
            _width = bmp.Width;
            _height = bmp.Height;
        }

        public FastBitmap(Size size, AppSettings appSettings)
        {
            _settings = appSettings;
            _width = size.Width + (_settings.ImageBorder.HasBorder ? 
                    (_settings.ImageBorder.Width + _settings.ImageBorder.Padding) * 2 : 0);
            _height = size.Height + (_settings.ImageBorder.HasBorder ?
                    (_settings.ImageBorder.Width + _settings.ImageBorder.Padding) * 2 : 0);
            _bmp = new Bitmap(_width, _height);
        }

        public FastBitmap(int width, int height, AppSettings appSettings)
        {
            _settings = appSettings;
            _width = width + (_settings.ImageBorder.HasBorder ? 
                    (_settings.ImageBorder.Width + _settings.ImageBorder.Padding) * 2 : 0);
            _height = height + (_settings.ImageBorder.HasBorder ? 
                    (_settings.ImageBorder.Width + _settings.ImageBorder.Padding) * 2 : 0);
            _bmp = new Bitmap(_width, _height);
        }

        private void LockBits()
        {
            _bmpData = _bmp.LockBits(
                        new Rectangle(0, 0, _bmp.Width, _bmp.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);
            _ptr = _bmpData.Scan0;
        }

        public bool isInEdge() => _settings.ImageBorder.HasBorder &&
                    _xpos < _width && _ypos < _height &&
                (_xpos < _settings.ImageBorder.EdgeWidth ||
                    _xpos >= _width - _settings.ImageBorder.EdgeWidth ||
                    _ypos < _settings.ImageBorder.EdgeWidth ||
                    _ypos >= _height - _settings.ImageBorder.EdgeWidth);

        public bool isInBorder() => _xpos < _settings.ImageBorder.Width ||
                    _xpos >= _width - _settings.ImageBorder.Width ||
                    _ypos < _settings.ImageBorder.Width ||
                    _ypos >= _height - _settings.ImageBorder.Width;

        private void EnsureBorder()
        {
            while (isInEdge())
            {
                if (isInBorder())
                    addPixel(_settings.ImageBorder.color);
                else if (_settings.IsSup)
                {
                    addPixel(_settings.SupBackground);
                }
                else
                {
                    addPixel(_settings.SubCustomColors.backgroud);
                }
            }
        }

        public void AddPixel(Color color)
        {
            EnsureBorder();
            addPixel(color);
        }

        private void addPixel(Color color)
        {
            if (_bmpData == null)
            {
                LockBits();
            }

            byte[] byt = new byte[] { color.R, color.G, color.B, color.A };
            Marshal.Copy(byt, 0, _ptr, 4);
            _ptr += 4;

            ++this._xpos;
            if (this._xpos < this._width)
                return;
            ++this._ypos;
            this._xpos = 0;
        }

        public void Unlock()
        {
            if (_bmpData != null)
            {
                _bmp.UnlockBits(_bmpData);
                _bmpData = null;
            }
        }

        public Bitmap GetBitmap()
        {
            Unlock();
            return _bmp;
        }

        public void SaveAs(string fileName)
        {
            Unlock();

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

            _bmp.Save(fileName, fmt);
        }
    }
}