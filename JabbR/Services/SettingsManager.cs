using System;
using System.Linq;
using JabbR.Models;
using Newtonsoft.Json;

namespace JabbR.Services
{
    public class SettingsManager : ISettingsManager
    {
        private static readonly TimeSpan _settingsCacheTimespan = TimeSpan.FromDays(1);
        private static readonly string _jabbrSettingsCacheKey = "jabbr.settings";

        private readonly ICache _cache;
        private readonly IJabbrRepository _repository;

        public SettingsManager(ICache cache, IJabbrRepository repository)
        {
            _cache = cache;
            _repository = repository;
        }

        public ApplicationSettings Load()
        {
            var settings = _cache.Get<ApplicationSettings>(_jabbrSettingsCacheKey);

            if (settings == null)
            {
                Settings dbSettings = _repository.Settings.FirstOrDefault();

                if (dbSettings == null)
                {
                    // Create the initial app settings
                    settings = ApplicationSettings.GetDefaultSettings();
                    dbSettings = new Settings
                    {
                        RawSettings = JsonConvert.SerializeObject(settings)
                    };

                    _repository.Add(dbSettings);
                }
                else
                {
                    try
                    {
                        settings = JsonConvert.DeserializeObject<ApplicationSettings>(dbSettings.RawSettings);
                        if (settings.ContentProviders == null)
                        {
                            // this will apply the default for the case where ApplicationSettings exists from prior to
                            // when this property was introduced.
                            settings.ContentProviders = ContentProviderSetting.GetDefaultContentProviders();
                        }
                    }
                    catch
                    {
                        // TODO: Record the exception

                        // We failed to load the settings from the db so go back to using the default
                        settings = ApplicationSettings.GetDefaultSettings();

                        dbSettings.RawSettings = JsonConvert.SerializeObject(settings);
                        _repository.CommitChanges();
                    }
                }

                // Cache the settings forever (until it changes)
                _cache.Set(_jabbrSettingsCacheKey, settings, _settingsCacheTimespan);
            }

            return settings;
        }

        public void Save(ApplicationSettings settings)
        {
            string rawSettings = JsonConvert.SerializeObject(settings);

            // Update the database
            Settings dbSettings = _repository.Settings.FirstOrDefault();

            if (dbSettings == null)
            {
                dbSettings = new Settings
                {
                    RawSettings = rawSettings
                };

                _repository.Add(dbSettings);
            }
            else
            {
                dbSettings.RawSettings = rawSettings;
            }

            _repository.CommitChanges();

            // Clear the cache
            _cache.Remove(_jabbrSettingsCacheKey);
        }
    }
}