using System;
using System.Globalization;
using System.IO;

namespace JabbR.Services
{
    public class FileEmailTemplateContentReader : IEmailTemplateContentReader
    {
        private readonly string _templateDirectory;
        private readonly string _fileExtension;

        public FileEmailTemplateContentReader(string templateDirectory, string fileExtension)
        {
            if (String.IsNullOrWhiteSpace(templateDirectory))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, "\"{0}\" cannot be blank.", "templateDirectory"));
            }

            if (!Path.IsPathRooted(templateDirectory))
            {
                templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateDirectory);
            }

            if (!Directory.Exists(templateDirectory))
            {
                throw new DirectoryNotFoundException(String.Format(CultureInfo.CurrentCulture, "\"{0}\" does not exist.", templateDirectory));
            }

            _templateDirectory = templateDirectory;
            _fileExtension = fileExtension;
        }

        public string Read(string templateName, string suffix = null)
        {
            if (String.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, "\"{0}\" cannot be blank.", "templateName"));
            }

            var content = String.Empty;
            var path = BuildPath(templateName, suffix);

            if (File.Exists(path))
            {
                content = File.ReadAllText(path);
            }

            return content;
        }

        protected virtual string BuildPath(string templateName, string suffix)
        {
            var fileName = templateName;

            if (!String.IsNullOrWhiteSpace(suffix))
            {
                fileName += "." + suffix;
            }

            if (!String.IsNullOrWhiteSpace(_fileExtension))
            {
                fileName += _fileExtension;
            }

            return Path.Combine(_templateDirectory, fileName);
        }
    }
}