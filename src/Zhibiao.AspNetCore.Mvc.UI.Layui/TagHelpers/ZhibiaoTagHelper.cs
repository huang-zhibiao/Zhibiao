using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Volo.Abp.DependencyInjection;

namespace Zhibiao.AspNetCore.Mvc.UI.Layui.TagHelpers
{
    public abstract class ZhibiaoTagHelper: TagHelper, ITransientDependency
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
    }
}
