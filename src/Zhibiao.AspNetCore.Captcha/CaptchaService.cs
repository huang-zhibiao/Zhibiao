using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Zhibiao.AspNetCore.Captcha
{
    public class CaptchaService : ICaptchaService
    {
        private static readonly Random _random = new Random();
        private readonly CaptchaOptions _captchaOptions;
        private readonly IHttpContextAccessor _contextAccessor;

        public CaptchaService(IHttpContextAccessor httpContextAccessor
            , IOptions<CaptchaOptions> captchaOptions)
        {
            _contextAccessor = httpContextAccessor;
            _captchaOptions = captchaOptions.Value;
        }


        public byte[] GetCaptchaImage()
        {
            var captcha = this.GetCaptchaText();
            var captchaModel = new CaptchaModel()
            {
                Captcha = captcha,
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(_captchaOptions.Expire)
            };
            _contextAccessor.HttpContext.Session.SetString(_captchaOptions.SessionKey
                , JsonConvert.SerializeObject(captchaModel));

            return this.GetEnDigitalCaptchaByte(captcha);
        }

        public bool ValidateCaptcha(string inputCaptcha)
        {
            try
            {
                var json = _contextAccessor.HttpContext.Session.GetString(_captchaOptions.SessionKey);
                var captchaModel = JsonConvert.DeserializeObject<CaptchaModel>(json);
                return string.Equals(inputCaptcha, captchaModel?.Captcha, StringComparison.OrdinalIgnoreCase)
                    && captchaModel?.AbsoluteExpiration > DateTimeOffset.UtcNow;
            }
            catch (Exception)
            {
                return false;
            }
        }


        private string GetCaptchaText()
        {
            var length = _captchaOptions.Length;
            var letters = _captchaOptions.Letters;

            var sb = new StringBuilder();
            if (length > 0)
            {
                do
                {
                    sb.Append(letters[_random.Next(0, letters.Length)]);
                }
                while (--length > 0);
            }

            return sb.ToString();
        }

        private byte[] GetEnDigitalCaptchaByte(string captcha)
        {
            using var bitmap = new Bitmap(_captchaOptions.ImageWidth, _captchaOptions.ImageHeight);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.Clear(GetRandomLightColor());

            DrawCaptcha();
            DrawDisorderLine();
            AdjustRippleEffect();

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();

            void DrawCaptcha()
            {
                var fontBrush = new SolidBrush(Color.Black);
                var width = _captchaOptions.ImageWidth / _captchaOptions.Length;
                var fontSize = width > _captchaOptions.ImageHeight ? _captchaOptions.ImageHeight : width;
                var font = new Font(FontFamily.GenericSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                for (var i = 0; i < _captchaOptions.Length; i++)
                {
                    fontBrush.Color = this.GetRandomDeepColor();

                    var shiftPx = width / 6;
                    float x = i * width + _random.Next(-shiftPx, shiftPx) + _random.Next(-shiftPx, shiftPx);

                    graphics.DrawString(captcha[i].ToString(), font, fontBrush, x, 0);
                }
            }

            void DrawDisorderLine()
            {
                var linePen = new Pen(new SolidBrush(Color.Black), 2);
                for (var i = 0; i < _random.Next(3, 5); i++)
                {
                    linePen.Color = this.GetRandomDeepColor();

                    var startPoint = new Point(_random.Next(0, _captchaOptions.ImageWidth), _random.Next(0, _captchaOptions.ImageHeight));
                    var endPoint = new Point(_random.Next(0, _captchaOptions.ImageWidth), _random.Next(0, _captchaOptions.ImageHeight));
                    graphics.DrawLine(linePen, startPoint, endPoint);
                }
            }

            void AdjustRippleEffect()
            {
                short nWave = 6;
                var nWidth = bitmap.Width;
                var nHeight = bitmap.Height;

                var pt = new Point[nWidth, nHeight];

                for (var x = 0; x < nWidth; ++x)
                {
                    for (var y = 0; y < nHeight; ++y)
                    {
                        var xo = nWave * Math.Sin(2.0 * 3.1415 * y / 128.0);
                        var yo = nWave * Math.Cos(2.0 * 3.1415 * x / 128.0);

                        var newX = x + xo;
                        var newY = y + yo;

                        if (newX > 0 && newX < nWidth)
                        {
                            pt[x, y].X = (int)newX;
                        }
                        else
                        {
                            pt[x, y].X = 0;
                        }


                        if (newY > 0 && newY < nHeight)
                        {
                            pt[x, y].Y = (int)newY;
                        }
                        else
                        {
                            pt[x, y].Y = 0;
                        }
                    }
                }

                var bSrc = (Bitmap)bitmap.Clone();

                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                var bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                var scanline = bitmapData.Stride;

                var scan0 = bitmapData.Scan0;
                var srcScan0 = bmSrc.Scan0;

                unsafe
                {
                    var p = (byte*)(void*)scan0;
                    var pSrc = (byte*)(void*)srcScan0;

                    var nOffset = bitmapData.Stride - bitmap.Width * 3;

                    for (var y = 0; y < nHeight; ++y)
                    {
                        for (var x = 0; x < nWidth; ++x)
                        {
                            var xOffset = pt[x, y].X;
                            var yOffset = pt[x, y].Y;

                            if (yOffset >= 0 && yOffset < nHeight && xOffset >= 0 && xOffset < nWidth)
                            {
                                if (pSrc != null)
                                {
                                    if (p != null)
                                    {
                                        p[0] = pSrc[yOffset * scanline + xOffset * 3];
                                        p[1] = pSrc[yOffset * scanline + xOffset * 3 + 1];
                                        p[2] = pSrc[yOffset * scanline + xOffset * 3 + 2];
                                    }
                                }
                            }

                            p += 3;
                        }
                        p += nOffset;
                    }
                }

                bitmap.UnlockBits(bitmapData);
                bSrc.UnlockBits(bmSrc);
                bSrc.Dispose();
            }
        }

        private Color GetRandomLightColor()
        {
            int low = 180;
            int high = 255;

            var nRend = _random.Next(high) % (high - low) + low;
            var nGreen = _random.Next(high) % (high - low) + low;
            var nBlue = _random.Next(high) % (high - low) + low;

            return Color.FromArgb(nRend, nGreen, nBlue);
        }

        private Color GetRandomDeepColor()
        {
            int redlow = 160, greenLow = 100, blueLow = 160;
            return Color.FromArgb(_random.Next(redlow), _random.Next(greenLow), _random.Next(blueLow));
        }
    }
}
