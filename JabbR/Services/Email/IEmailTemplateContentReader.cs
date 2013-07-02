namespace JabbR.Services
{
    public interface IEmailTemplateContentReader
    {
        string Read(string templateName, string suffix = null);
    }
}