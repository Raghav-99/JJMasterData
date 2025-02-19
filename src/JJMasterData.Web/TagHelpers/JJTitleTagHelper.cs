using JJMasterData.Core.Web.Components;

namespace JJMasterData.Web.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

public class JJTitleTagHelper : TagHelper
{
    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    [HtmlAttributeName("subtitle")]
    public string? SubTitle { get; set; }

    [HtmlAttributeName("size")]
    public HeadingSize? Size { get; set; }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var title = new JJTitle(Title ?? string.Empty, SubTitle ?? string.Empty);

        if (Size is not null)
        {
            title.Size = Size.Value;
        }

        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(title.GetHtml());

        return Task.CompletedTask;
    }
}
