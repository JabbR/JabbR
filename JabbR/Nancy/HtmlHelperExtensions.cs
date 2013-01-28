using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JabbR.Infrastructure;
using Nancy.Validation;
using Nancy.ViewEngines.Razor;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
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
    }
}