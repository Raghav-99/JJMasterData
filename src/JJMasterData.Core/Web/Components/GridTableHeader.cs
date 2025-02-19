using System;
using System.Collections.Generic;
using System.Linq;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.Web.Html;

namespace JJMasterData.Core.Web.Components;

internal class GridTableHeader
{
    private JJGridView GridView { get; }

    public GridTableHeader(JJGridView gridView)
    {
        GridView = gridView;
    }

    public HtmlBuilder GetHtmlElement()
    {
        var html = new HtmlBuilder(HtmlTag.Thead);
        if (GridView.DataSource.Rows.Count == 0 && !GridView.ShowHeaderWhenEmpty)
            return html;

        html.AppendElement(HtmlTag.Tr, tr =>
        {
            tr.AppendElementIf(GridView.EnableMultSelect, GetMultSelectThHtmlElement);
            tr.AppendRange(GetVisibleFieldsThList());
            tr.AppendRange(GetActionsThList());
        });

        return html;
    }

    private IEnumerable<HtmlBuilder> GetActionsThList()
    {
        var basicActions = GridView.GridActions.OrderBy(x => x.Order).ToList();
        var actions = basicActions.FindAll(x => x.IsVisible && !x.IsGroup);
        var actionsWithGroupCount = basicActions.Count(x => x.IsVisible && x.IsGroup);

        foreach (var action in actions)
        {
            var th = new HtmlBuilder(HtmlTag.Th);
            th.AppendTextIf(action.ShowTitle, action.Text);
            yield return th;
        }

        if (actionsWithGroupCount > 0)
        {
            yield return new HtmlBuilder(HtmlTag.Th);
        }
    }

    private IEnumerable<HtmlBuilder> GetVisibleFieldsThList()
    {
        foreach (var field in GridView.VisibleFields)
        {
            var th = new HtmlBuilder(HtmlTag.Th);
            string style = GetFieldStyle(field);

            th.WithAttributeIf(!string.IsNullOrEmpty(style), "style", style);
            th.AppendElement(HtmlTag.Span, span =>
            {
                if (GridView.EnableSorting && field.DataBehavior != FieldBehavior.Virtual)
                {
                    SetSortAttributes(span, field);
                }
                span.AppendElement(HtmlTag.Span, span =>
                {
                    if (!string.IsNullOrEmpty(field.HelpDescription))
                    {
                        span.WithToolTip(Translate.Key(field.HelpDescription));
                    }

                    span.AppendText(field.GetTranslatedLabel());
                });
            });

            if (!string.IsNullOrEmpty(GridView.CurrentOrder))
            {
                foreach (string orderField in GridView.CurrentOrder.Split(','))
                {
                    string order = orderField.Trim();
                    if (string.IsNullOrWhiteSpace(order))
                        break;

                    if (order.StartsWith("["))
                    {
                        order = order.Replace("[", "");
                        order = order.Replace("]", "");
                    }

                    if (order.Equals(field.Name + " DESC"))
                        th.AppendElement(GetDescendingIcon());
                    else if (order.Equals(field.Name + " ASC") || order.Equals(field.Name))
                        th.AppendElement(GetAscendingIcon());
                }
            }
            else
            {
                if (field.Name.EndsWith("::DESC"))
                    th.AppendElement(GetDescendingIcon());
                else if (field.Name.EndsWith("::ASC"))
                    th.AppendElement(GetAscendingIcon());
            }

            bool isAppliedFilter = GridView.CurrentFilter != null &&
                                   field.Filter.Type != FilterMode.None &&
                                   !GridView.RelationValues.ContainsKey(field.Name) &&
                                   (GridView.CurrentFilter.ContainsKey(field.Name) ||
                                    GridView.CurrentFilter.ContainsKey(field.Name + "_from"));

            if (isAppliedFilter)
            {
                th.AppendText("&nbsp;");
                th.AppendElement(new JJIcon("fa fa-filter").GetHtmlBuilder()
                    .WithToolTip(Translate.Key("Applied filter")));
            }

            yield return th;
        }
    }

    private void SetSortAttributes(HtmlBuilder span, FormElementField field)
    {
        span.WithCssClass("jjenable-sorting");

        string ajax = GridView.EnableAjax ? "true" : "false";

        span.WithAttribute("onclick",
            $"jjview.doSorting('{GridView.Name}','{ajax}','{field.Name}')");
    }

    private HtmlBuilder GetAscendingIcon() => new JJIcon("fa fa-sort-amount-asc").GetHtmlBuilder()
        .WithToolTip(Translate.Key("Descending order"));

    private HtmlBuilder GetDescendingIcon() => new JJIcon("fa fa-sort-amount-desc").GetHtmlBuilder()
        .WithToolTip(Translate.Key("Ascending order"));

    private string GetFieldStyle(FormElementField field)
    {
        string style = string.Empty;
        switch (field.Component)
        {
            case FormComponent.ComboBox:
            {
                if (field.DataItem is { ShowImageLegend: true, ReplaceTextOnGrid: false })
                {
                    style = "text-align:center";
                }

                break;
            }
            case FormComponent.CheckBox:
                style = "text-align:center;width:60px;";
                break;
            case FormComponent.Cnpj:
                style = "min-width:130px;";
                break;
            default:
            {
                if (field.DataType is FieldType.Float or FieldType.Int)
                {
                    if (!field.IsPk)
                        style = "text-align:right;";
                }

                break;
            }
        }

        return style;
    }

    private HtmlBuilder GetMultSelectThHtmlElement()
    {
        var th = new HtmlBuilder(HtmlTag.Th);

        bool hasPages = true;
        if (!GridView.IsPaggingEnabled())
        {
            hasPages = false;
        }
        else
        {
            int totalPages = (int)Math.Ceiling(GridView.TotalRecords / (double)GridView.CurrentSettings.TotalPerPage);
            if (totalPages <= 1)
                hasPages = false;
        }

        th.WithCssClass("jjselect")
            .AppendElement(HtmlTag.Input, input =>
            {
                input.WithAttribute("type", "checkbox")
                    .WithNameAndId("jjchk_all")
                    .WithCssClass("form-check-input")
                    .WithToolTip(Translate.Key("Mark|Unmark all from page"))
                    .WithAttribute("onclick",
                        "$('td.jjselect input').not(':disabled').prop('checked',$('#jjchk_all').is(':checked')).change();");
            });

        th.AppendElementIf(hasPages, HtmlTag.Span, span =>
        {
            span.WithCssClass("dropdown");
            span.AppendElement(HtmlTag.A, a =>
            {
                a.WithAttribute("href", "#");
                a.WithAttribute(BootstrapHelper.DataToggle, "dropdown");
                a.WithCssClass("dropdown-toggle");
                a.AppendElementIf(BootstrapHelper.Version == 3,
                    new JJIcon("fa fa-caret-down fa-fw fa-lg").GetHtmlBuilder);
            });

            span.AppendElement(HtmlTag.Ul, ul =>
            {
                ul.WithCssClass("dropdown-menu");
                ul.AppendElement(HtmlTag.Li, li =>
                {
                    li.WithCssClass("dropdown-item");
                    li.AppendElement(HtmlTag.A, a =>
                    {
                        a.WithAttribute("href", "javascript:void(0);");
                        a.WithAttribute("onclick", $"jjview.doUnSelectAll('{GridView.Name}')");
                        a.AppendText(Translate.Key("Unmark all selected records"));
                    });
                });
                ul.AppendElementIf(GridView.TotalRecords <= 50000, HtmlTag.Li, li =>
                {
                    li.WithCssClass("dropdown-item");
                    li.AppendElement(HtmlTag.A, a =>
                    {
                        a.WithAttribute("href", "javascript:void(0);");
                        a.WithAttribute("onclick", $"jjview.doSelectAll('{GridView.Name}')");
                        a.AppendText(Translate.Key("Mark all {0} records", GridView.TotalRecords));
                    });
                });
            });
        });

        return th;
    }
}