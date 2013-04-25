using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;

namespace JabbR.Nancy
{
    public class AdministrationModule : JabbRModule
    {
        public AdministrationModule(ApplicationSettings applicationSettings,
                                    IJabbrRepository repository)
            : base("/administration")
        {
            Get["/"] = _ =>
            {
                var user = repository.GetUserById(Principal.GetUserId());
                if (!IsAuthenticated || !user.IsAdmin)
                {
                    return HttpStatusCode.Forbidden;
                }
                return GetAdminView(applicationSettings);
            };
        }

        private dynamic GetAdminView(ApplicationSettings applicationSettings)
        {
            return View["index", new AdministrationViewModel(applicationSettings)];
        }
    }
}