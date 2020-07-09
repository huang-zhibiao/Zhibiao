using Microsoft.Extensions.Options;
using System;

namespace Zhibiao.AspNetCore.Captcha
{
    public class CaptchaOptions: IOptions<CaptchaOptions>
    {
        /// <summary>
        /// SessionKey
        /// </summary>
        public string SessionKey { get; set; } = "Zhibiao.AspNetCore.Captcha.SessionKey";
        /// <summary>
        /// 验证码字符集
        /// </summary>
        public string Letters { get; set; } = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
        /// <summary>
        /// 验证码长度
        /// </summary>
        public short Length { get; set; } = 4;
        /// <summary>
        /// 过期时间，默认5分钟
        /// </summary>
        public TimeSpan Expire { get; set; } = TimeSpan.FromMinutes(5);
        /// <summary>
        /// 验证码图片宽度
        /// </summary>
        public int ImageWidth { get; set; } = 120;
        /// <summary>
        /// 验证码图片高度
        /// </summary>
        public int ImageHeight { get; set; } = 50;

        public CaptchaOptions Value => throw new NotImplementedException();
    }
}
