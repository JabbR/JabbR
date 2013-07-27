namespace JabbR.Services
{
    public interface IEmailTemplateEngine
    {
        Email RenderTemplate(string templateName, object model = null);
    }
}