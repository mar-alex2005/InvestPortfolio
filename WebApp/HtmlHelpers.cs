using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Invest.WebApp
{
    public static class HtmlHelpers
    {
        public static IHtmlContent DrawSortColumnImg(this IHtmlHelper html, string colName, string sortColumn, string orderBy)
        {
            if (colName == sortColumn)
                return
                    orderBy != "desc"
                        ? new HtmlString("<span class='icon-sort-alt-up'></span>")
                        : new HtmlString("<span class='icon-sort-alt-down'></span>");

            return null;
        }

        public static IHtmlContent DrawSortColumnImg(this IHtmlHelper html, string colName)
        {
            var sortColumn = "";
            var orderBy = "asc";

            if (html.ViewBag.SortColumn != null)
                sortColumn = (string)html.ViewBag.SortColumn;

            if (html.ViewBag.OrderBy != null)
                orderBy = (string)html.ViewBag.OrderBy;

            if (colName == sortColumn)
                return new HtmlString(
                    orderBy != "desc"
                        ? "<img src='/img/asc.png' />"
                        : "<img src='/img/desc.png' />"
                );

            return null;
        }
    }
}