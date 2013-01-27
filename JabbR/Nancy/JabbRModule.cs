using System;
using Nancy;

namespace JabbR.Nancy
{
    public class JabbRModule : NancyModule
    {
        public JabbRModule()
            : base()
        {
        }

        public JabbRModule(string modulePath)
            : base(modulePath)
        {
        }

        protected void AddValidationError(string propertyName, string errorMessage)
        {
            ModelValidationResult = ModelValidationResult.AddError(propertyName, errorMessage);
        }
    }
}