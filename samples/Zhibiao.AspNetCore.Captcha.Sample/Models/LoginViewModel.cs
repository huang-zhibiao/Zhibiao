using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zhibiao.AspNetCore.Captcha.Sample.Models
{
    public class LoginViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Captcha { get; set; }
    }
}
