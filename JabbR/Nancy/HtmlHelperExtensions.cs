using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JabbR.Infrastructure;
using Nancy.Validation;
using Nancy.ViewEngines.Razor;
using PagedList;
using AntiXSS = Microsoft.Security.Application;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString CheckBox<T>(this HtmlHelpers<T> helper, string Name, bool value)
        {
            string input = String.Empty;
            
            var checkBoxBuilder = new StringBuilder();

            checkBoxBuilder.Append(@"<input data-name=""");
            checkBoxBuilder.Append(AntiXSS.Encoder.HtmlAttributeEncode(Name));
            checkBoxBuilder.Append(@""" type=""checkbox""");
            if (value)
            {
                checkBoxBuilder.Append(@" checked=""checked"" />");
            }
            else
            {
                checkBoxBuilder.Append(" />");
            }

            checkBoxBuilder.Append(@"<input name=""");
            checkBoxBuilder.Append(AntiXSS.Encoder.HtmlAttributeEncode(Name));
            checkBoxBuilder.Append(@""" type=""hidden"" value=""");
            checkBoxBuilder.Append(value.ToString().ToLowerInvariant());
            checkBoxBuilder.Append(@""" />");

            return new NonEncodedHtmlString(checkBoxBuilder.ToString());
        }

        public static IHtmlString ValidationSummary<TModel>(this HtmlHelpers<TModel> htmlHelper)
        {
            var validationResult = htmlHelper.RenderContext.Context.ModelValidationResult;
            if (validationResult.IsValid)
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var summaryBuilder = new StringBuilder();

            summaryBuilder.Append(@"<ul class=""validation-summary-errors"">");
            foreach (var modelValidationError in validationResult.Errors)
            {
                foreach (var memberName in modelValidationError.MemberNames)
                {
                    summaryBuilder.AppendFormat("<li>{0}</li>", modelValidationError.GetMessage(memberName));
                }
            }
            summaryBuilder.Append(@"</ul>");

            return new NonEncodedHtmlString(summaryBuilder.ToString());
        }

        public static IHtmlString ValidationMessage<TModel>(this HtmlHelpers<TModel> htmlHelper, string propertyName)
        {
            var errorsForField = htmlHelper.GetErrorsForProperty(propertyName).ToList();

            if (!errorsForField.Any())
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            return new NonEncodedHtmlString(errorsForField.First().GetMessage(propertyName));
        }

        public static IHtmlString AlertMessages<TModel>(this HtmlHelpers<TModel> htmlHelper)
        {
            const string message = @"<div class=""alert alert-{0}"">{1}</div>";
            var alertsDynamicValue = htmlHelper.RenderContext.Context.ViewBag.Alerts;
            var alerts = (AlertMessageStore)(alertsDynamicValue.HasValue ? alertsDynamicValue.Value : null);

            if (alerts == null || !alerts.Messages.Any())
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var builder = new StringBuilder();

            foreach (var messageDetail in alerts.Messages)
            {
                builder.AppendFormat(message, messageDetail.Key, messageDetail.Value);
            }

            return new NonEncodedHtmlString(builder.ToString());
        }

        internal static IEnumerable<ModelValidationError> GetErrorsForProperty<TModel>(this HtmlHelpers<TModel> htmlHelper,
                                                                         string propertyName)
        {
            var validationResult = htmlHelper.RenderContext.Context.ModelValidationResult;
            if (validationResult.IsValid)
            {
                return Enumerable.Empty<ModelValidationError>();
            }

            var errorsForField =
                validationResult.Errors.Where(
                    x => x.MemberNames.Any(y => y.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)));

            return errorsForField;
        }

        public static IHtmlString SimplePager<TModel>(this HtmlHelpers<TModel> htmlHelper, IPagedList pagedList, string baseUrl)
        {
            var pagerBuilder = new StringBuilder();

            pagerBuilder.Append(@"<div class=""pager"">");
            pagerBuilder.Append(@"<ul>");

            pagerBuilder.AppendFormat(@"<li class=""previous {0}"">", !pagedList.HasPreviousPage ? "disabled" : "");
            pagerBuilder.AppendFormat(@"<a href=""{0}"">&larr; Prev</a>", pagedList.HasPreviousPage ? String.Format("{0}page={1}", baseUrl, pagedList.PageNumber - 1) : "#");
            pagerBuilder.Append(@"</li>");

            pagerBuilder.AppendFormat(@"<li class=""next {0}"">", !pagedList.HasNextPage ? "disabled" : "");
            pagerBuilder.AppendFormat(@"<a href=""{0}"">Next &rarr;</a>", pagedList.HasNextPage ? String.Format("{0}page={1}", baseUrl, pagedList.PageNumber + 1) : "#");
            pagerBuilder.Append(@"</li>");

            pagerBuilder.Append(@"</ul>");
            pagerBuilder.Append(@"</div>");

            return new NonEncodedHtmlString(pagerBuilder.ToString());
        }

        public static IHtmlString DisplayNoneIf<TModel>(this HtmlHelpers<TModel> htmlHelper, Expression<Func<TModel, bool>> expression)
        {
            if (expression.Compile()(htmlHelper.Model))
                return new NonEncodedHtmlString(@" style=""display:none;"" ");

            return NonEncodedHtmlString.Empty;
        }
    }
}