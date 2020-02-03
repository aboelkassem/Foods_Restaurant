using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Foods.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.TagHelpers
{
    [HtmlTargetElement("div",Attributes ="page-model")]
    public class PageLinkTageHelper : TagHelper
    {
        //private IUrlHelperFactory urlHelperFactory;
        //[ViewContext]
        //[HtmlAttributeNotBound]
        //public ViewContext ViewContext { get; set; }

        // Properties that will set in using this tag helper like [asp-for="",asp-action="",asp-controller="",asp-validation-summary=""]
        public PagingInfo PageModel{ get; set; }
        public string PageAction { get; set; }
        public bool PageClassesEnabled { get; set; }
        public string PageClass { get; set; }
        public string PageClassNormal { get; set; }
        public string PageClassSelected { get; set; }

        //public PageLinkTageHelper(IUrlHelperFactory helperFactory)
        //{
        //    urlHelperFactory = helperFactory;
        //}

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            //IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);

            // parent div
            TagBuilder result = new TagBuilder("div");

            for (int i = 1; i <= PageModel.TotalPage; i++)
            {
                TagBuilder tag = new TagBuilder("a");
                string url = PageModel.UrlParam.Replace(":", i.ToString());
                tag.Attributes["href"] = url;
                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);
                    tag.AddCssClass(i == PageModel.CurrentPage ? PageClassSelected : PageClassNormal);
                }
                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result);
        }
    }
}
