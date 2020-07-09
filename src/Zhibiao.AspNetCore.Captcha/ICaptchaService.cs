namespace Zhibiao.AspNetCore.Captcha
{
    public interface ICaptchaService
    {
        /// <summary>
        /// 获取验证码图片
        /// </summary>
        /// <returns></returns>
        byte[] GetCaptchaImage();
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="inputCaptcha"></param>
        /// <returns></returns>
        bool ValidateCaptcha(string inputCaptcha);
    }
}
