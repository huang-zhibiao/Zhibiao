using System;

namespace Zhibiao.AspNetCore.Captcha
{
    public class CaptchaModel
    {
        public string Captcha { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
    }
}
