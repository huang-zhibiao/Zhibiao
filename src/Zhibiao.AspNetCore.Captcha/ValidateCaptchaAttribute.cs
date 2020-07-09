using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

namespace Zhibiao.AspNetCore.Captcha
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ValidateCaptchaAttribute : ActionFilterAttribute
    {
        public string PropertyName { get; set; } = "Captcha";
        public string ErrorMessage { get; set; } = "验证码错误！";

        public ValidateCaptchaAttribute(string propertyName = "Captcha", string errorMessage = "验证码错误")
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var inputCaptcha = this.GetInputCaptcha(context);
            if (string.IsNullOrWhiteSpace(inputCaptcha))
            {
                ErrorMessage = "请输入验证码！";
            }
            else
            {
                var captchaService = context.HttpContext.RequestServices.GetRequiredService<ICaptchaService>();
                if (captchaService.ValidateCaptcha(inputCaptcha))
                {
                    base.OnActionExecuting(context);
                    return;
                }
            }

            var captchaOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<CaptchaOptions>>();
            context.HttpContext.Session.Remove(captchaOptions?.Value?.SessionKey);

            var isAjax = context.HttpContext.Request.Headers != null && "XMLHttpRequest".Equals(context.HttpContext.Request.Headers["X-Requested-With"], StringComparison.OrdinalIgnoreCase);
            if (isAjax)
            {
                context.Result = new JsonResult(new { msg = ErrorMessage, code = -2 });
            }
            else
            {
                var controllerBase = context.Controller as ControllerBase;
                controllerBase?.ModelState.AddModelError(PropertyName, ErrorMessage);
                base.OnActionExecuting(context);
            }
        }

        private string GetInputCaptcha(ActionExecutingContext context)
        {
            var value = string.Empty;
            var httpContext = context.HttpContext;
            if (httpContext.Request.HasFormContentType)
            {
                value = httpContext.Request.Form[PropertyName];
            }
            else
            {
                foreach (var arg in context.ActionArguments)
                {
                    if (arg.Key.Equals(PropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = arg.Value.ToString();
                    }
                    else if (arg.Value != null)
                    {
                        var obj = arg.Value.GetType()
                            .GetProperty(PropertyName, BindingFlags.Public | BindingFlags.IgnoreCase)
                            .GetValue(arg.Value, null);

                        if (obj != null)
                            value = obj.ToString();
                    }
                }
            }

            return value;
        }
    }
}
