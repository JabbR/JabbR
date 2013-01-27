using System;
using System.Linq;
using System.Text;
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
            var validationResult = htmlHelper.RenderContext.Context.ModelValidationResult;
            if (validationResult.IsValid)
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var errorsForField =
                validationResult.Errors.Where(
                    x => x.MemberNames.Any(y => y.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)));

            return new NonEncodedHtmlString(errorsForField.First().GetMessage(propertyName));
        }
    }
}