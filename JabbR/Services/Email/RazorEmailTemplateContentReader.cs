namespace JabbR.Services
{
    public class RazorEmailTemplateContentReader : FileEmailTemplateContentReader
    {
        public const string DefaultTemplateDirectory = "views/emailtemplates";
        public const string DefaultFileExtension = ".cshtml";

        public RazorEmailTemplateContentReader()
            : base(DefaultTemplateDirectory, DefaultFileExtension)
        {

        }
    }
}