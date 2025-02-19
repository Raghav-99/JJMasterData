﻿using System;
using System.Globalization;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager.Imports;
using JJMasterData.Core.Web.Html;

namespace JJMasterData.Core.Web.Components;

internal class DataImpLog
{
    public DataImpReporter Reporter { get; private set; }

    internal DataImpLog(JJDataImp dataImp)
    {
        Reporter = dataImp.GetCurrentReporter();
    }

    public HtmlBuilder GetHtmlLog()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .AppendElement(GetAlertPanel())
            .AppendElement(GetHtmlResume())
            .AppendElement(HtmlTag.Div, div =>
            {
                div.AppendText("&nbsp;");
            })
            .AppendElement(GetHtmlLogDetails());

        return html;
    }

    public HtmlBuilder GetHtmlResume()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttribute("style", "text-align: center;")
            .WithCssClass("jjlabel-process");

        html.AppendElementIf(Reporter.EndDate != DateTime.MinValue, HtmlTag.Div, div =>
        {
            string elapsedTime = Format.FormatTimeSpan(Reporter.StartDate, Reporter.EndDate);
            div.AppendText(Translate.Key("Process performed on {0}", elapsedTime));
        });

        html.AppendElement(HtmlTag.Span, s =>
        {
            s.WithCssClass(BootstrapHelper.LabelSucess)
             .WithAttribute("id", "lblInsert")
             .WithAttributeIf(Reporter.Insert == 0, "style", "display:none;")
             .AppendText(Translate.Key("Inserted:"))
             .AppendElement(HtmlTag.Span, count =>
             {
                 count.WithAttribute("id", "lblInsertCount")
                      .AppendText(Reporter.Insert.ToString("N0"));
             });
        });
        html.AppendElement(HtmlTag.Span, s =>
        {
            s.WithCssClass(BootstrapHelper.LabelSucess)
             .WithAttribute("id", "lblUpdate")
             .WithAttributeIf(Reporter.Update == 0, "style", "display:none;")
             .AppendText(Translate.Key("Updated:"))
             .AppendElement(HtmlTag.Span, count =>
             {
                 count.WithAttribute("id", "lblUpdateCount")
                      .AppendText(Reporter.Update.ToString("N0"));
             });
        });
        html.AppendElement(HtmlTag.Span, s =>
        {
            s.WithCssClass(BootstrapHelper.LabelDefault)
             .WithAttribute("id", "lblDelete")
             .WithAttributeIf(Reporter.Delete == 0, "style", "display:none;")
             .AppendText(Translate.Key("Deleted:"))
             .AppendElement(HtmlTag.Span, count =>
             {
                 count.WithAttribute("id", "lblDeleteCount")
                      .AppendText(Reporter.Delete.ToString("N0"));
             });
        });

        html.AppendElement(HtmlTag.Span, s =>
        {
            s.WithCssClass(BootstrapHelper.LabelWarning)
             .WithAttribute("id", "lblIgnore")
             .WithAttributeIf(Reporter.Ignore == 0, "style", "display:none;")
             .AppendText(Translate.Key("Ignored:"))
             .AppendElement(HtmlTag.Span, count =>
             {
                 count.WithAttribute("id", "lblIgnoreCount")
                      .AppendText(Reporter.Ignore.ToString("N0"));
             });
        });

        html.AppendElement(HtmlTag.Span, s =>
        {
            s.WithCssClass(BootstrapHelper.LabelDanger)
             .WithAttribute("id", "lblError")
             .WithAttributeIf(Reporter.Error == 0, "style", "display:none;")
             .AppendText(Translate.Key("Errors:"))
             .AppendElement(HtmlTag.Span, count =>
             {
                 count.WithAttribute("id", "lblErrorCount")
                      .AppendText(Reporter.Error.ToString("N0"));
             });
        });

        return html;
    }

    private HtmlBuilder GetHtmlLogDetails()
    {
        var panel = new JJCollapsePanel();
        panel.Title = "(Click here for more details)";
        panel.TitleIcon = new JJIcon(IconType.Film);
        panel.ExpandedByDefault = false;
        panel.HtmlBuilderContent = new HtmlBuilder(HtmlTag.Div)
            .AppendElement(HtmlTag.Label, label =>
            {
                label.AppendText(Translate.Key("Date:"));
            })
            .AppendText("&nbsp;")
            .AppendText(Translate.Key("start"))
            .AppendText("&nbsp;")
            .AppendText(Reporter.StartDate.ToString(CultureInfo.CurrentCulture))
            .AppendText("&nbsp;")
            .AppendText(Translate.Key("end"))
            .AppendText("&nbsp;")
            .AppendText(Reporter.EndDate.ToString(CultureInfo.CurrentCulture))
            .AppendElement(HtmlTag.Br);

        if (!string.IsNullOrEmpty(Reporter.UserId))
        {
            panel.HtmlBuilderContent.AppendElement(HtmlTag.Label, label =>
            {
                label.AppendText(Translate.Key("User Id:"));
            })
            .AppendText("&nbsp;")
            .AppendText(Reporter.UserId)
            .AppendElement(HtmlTag.Br);
        }

        panel.HtmlBuilderContent
                .AppendElement(HtmlTag.Br)
                .AppendText(Reporter.ErrorLog.ToString().Replace("\r\n", "<br>"));

        return panel.RenderHtml();
    }

    private JJAlert GetAlertPanel()
    {
        var alert = new JJAlert();
        alert.CssClass = "text-center";
        alert.ShowIcon = true;

        if (Reporter.HasError || Reporter.TotalProcessed == Reporter.Error)
        {
            alert.Icon = IconType.ExclamationTriangle;
            alert.Color = PanelColor.Danger;
            alert.Title = Translate.Key("Error importing file!");
            alert.Messages.Add(Translate.Key(Reporter.Message));
        }
        else if (Reporter.Error > 0)
        {
            alert.Icon = IconType.InfoCircle;
            alert.Color = PanelColor.Info;
            alert.Title = Translate.Key("File imported with errors!");
            alert.Messages.Add(Translate.Key(Reporter.Message));
        }
        else
        {
            alert.Icon = IconType.Check;
            alert.Color = PanelColor.Success;
            alert.Title = Translate.Key(Reporter.Message);
        }

        return alert;
    }

}
