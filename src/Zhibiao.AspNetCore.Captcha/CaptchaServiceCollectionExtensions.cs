using Microsoft.Extensions.DependencyInjection;
using System;

namespace Zhibiao.AspNetCore.Captcha
{
    public static class CaptchaServiceCollectionExtensions
    {
        public static void AddCaptchaService(
            this IServiceCollection services,
            Action<CaptchaOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }

            services.AddScoped<ICaptchaService, CaptchaService>();
        }
    }
}
