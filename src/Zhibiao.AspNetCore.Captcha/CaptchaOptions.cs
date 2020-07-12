using System;
using System.Reflection;

namespace Zhibiao.AspNetCore.Captcha
{
    public class CaptchaOptions
    {
        private string _sessionKey = Assembly.GetEntryAssembly().GetName().Name + ".CaptchaSessionKey";
        private string _letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
        private short _length = 4;
        private TimeSpan _expire = TimeSpan.FromMinutes(3);
        private int _imageWidth = 120;
        private int _imageHeigth = 38;

        /// <summary>
        /// SessionKey
        /// </summary>
        public string SessionKey
        {
            get { return _sessionKey; }
            set { if (!string.IsNullOrWhiteSpace(value)) _sessionKey = value; }
        }
        /// <summary>
        /// 验证码字符集
        /// </summary>
        public string Letters
        {
            get { return _letters; }
            set { if (!string.IsNullOrWhiteSpace(value)) _letters = value; }
        }
        /// <summary>
        /// 验证码长度
        /// </summary>
        public short Length
        {
            get { return _length; }
            set { if (value > 0) _length = value; }
        }
        /// <summary>
        /// 过期时间，默认3分钟
        /// </summary>
        public TimeSpan Expire
        {
            get { return _expire; }
            set { if (value > _expire) _expire = value; }
        }
        /// <summary>
        /// 验证码图片宽度
        /// </summary>
        public int ImageWidth
        {
            get { return _imageWidth; }
            set { if (value > 0) _imageWidth = value; }
        }
        /// <summary>
        /// 验证码图片高度
        /// </summary>
        public int ImageHeight
        {
            get { return _imageHeigth; }
            set { if (value > 0) _imageHeigth = value; }
        }
    }
}
