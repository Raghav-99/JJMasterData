﻿using System;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.Web.Html;

namespace JJMasterData.Core.Web.Components;

public class JJTitle : JJBaseView
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public HeadingSize Size { get; set; }

    private HtmlTag Tag => Size switch
    {
        HeadingSize.H1 => HtmlTag.H1,
        HeadingSize.H2 => HtmlTag.H2,
        HeadingSize.H3 => HtmlTag.H3,
        HeadingSize.H4 => HtmlTag.H4,
        HeadingSize.H5 => HtmlTag.H5,
        HeadingSize.H6 => HtmlTag.H6,
        _ => throw new ArgumentOutOfRangeException()
    };

    public JJTitle(string title, string subtitle)
    {
        Title = title;
        SubTitle = subtitle;
        Size = HeadingSize.H1;
    }

    public JJTitle(FormElement form)
    {
        if (form == null)
            throw new ArgumentNullException(nameof(form));

        Title = form.Title;
        SubTitle = form.SubTitle;
        Size = HeadingSize.H1;
    }

    internal override HtmlBuilder RenderHtml()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithNameAndId(Name)
            .WithAttributes(Attributes)
            .WithCssClass(CssClass)
            .WithCssClass(BootstrapHelper.PageHeader)
            .AppendElement(Tag, e =>
            {
                e.AppendText(Title);
                e.AppendElement(HtmlTag.Small, e =>
                {
                    e.AppendText(" " + SubTitle);
                });
            });
    }


    internal HtmlBuilder GetHtmlBlockquote()
    {
        var row = new HtmlBuilder(HtmlTag.Div)
            .WithCssClass("row")
            .AppendElement(HtmlTag.Blockquote, block =>
            {
                block.WithCssClass("blockquote mb-1");
                block.AppendElementIf(!string.IsNullOrEmpty(Title), HtmlTag.P, p =>
                {
                    p.AppendText(Title);
                });
                block.AppendElementIf(!string.IsNullOrEmpty(SubTitle), HtmlTag.Footer, p =>
                {
                    p.WithCssClass("blockquote-footer");
                    p.WithCssClassIf(string.IsNullOrEmpty(Title), "mt-1");
                    p.AppendText(SubTitle);
                });
            });

        return row;
    }

}