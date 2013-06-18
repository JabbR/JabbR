namespace JabbR.Services
{
    public interface ISettingsManager
    {
        ApplicationSettings Load();
        void Save(ApplicationSettings settings);
    }
}
