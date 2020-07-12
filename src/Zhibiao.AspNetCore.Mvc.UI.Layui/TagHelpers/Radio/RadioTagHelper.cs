using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;

namespace Zhibiao.AspNetCore.Mvc.UI.Layui.TagHelpers.Radio
{
    /// <summary>
    /// 单选框
    /// </summary>
    [HtmlTargetElement(RadioTagName)]
    public class RadioTagHelper : ZhibiaoTagHelper
    {
        private const string RadioTagName = "zb-radio";
        private const string ForAttributeName = "asp-for";
        private const string ItemsAttributeName = "asp-items";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(ItemsAttributeName)]
        public IEnumerable<SelectListItem> Items { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For == null)
            {
                throw new ArgumentException("必须绑定模型");
            }
            foreach (var item in Items)
            {
                var radio = new TagBuilder("input");
                radio.TagRenderMode = TagRenderMode.SelfClosing;
                radio.Attributes.Add("id", ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name));
                radio.Attributes.Add("name", ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name));
                radio.Attributes.Add("value", item.Value);
                radio.Attributes.Add("title", item.Text);
                radio.Attributes.Add("type", "radio");
                if (item.Disabled)
                {
                    radio.Attributes.Add("disabled", "disabled");
                }
                if (item.Selected || item.Value == For.Model?.ToString())
                {
                    radio.Attributes.Add("checked", "checked");
                }
                output.Content.AppendHtml(radio);
            }
            output.TagName = "";
        }
    }
}
