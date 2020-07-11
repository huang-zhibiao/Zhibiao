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
            DistortEffect();

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }

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

            void DistortEffect()
            {
                using (var copy = (Bitmap)bitmap.Clone())
                {
                    var distort = _random.Next(1, 6) * (_random.Next(10) == 1 ? 1 : -1);
                    for (int y = 0; y < _captchaOptions.ImageHeight; y++)
                    {
                        for (int x = 0; x < _captchaOptions.ImageWidth; x++)
                        {
                            int newX = (int)(x + (distort * Math.Sin(Math.PI * y / 84.0)));
                            int newY = (int)(y + (distort * Math.Cos(Math.PI * x / 44.0)));
                            if (newX < 0 || newX >= _captchaOptions.ImageWidth) newX = 0;
                            if (newY < 0 || newY >= _captchaOptions.ImageHeight) newY = 0;
                            bitmap.SetPixel(x, y, copy.GetPixel(newX, newY));
                        }
                    }
                }
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
