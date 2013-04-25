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
        private readonly JabbrContext _context;

        public SettingsManager(ICache cache, JabbrContext context)
        {
            _cache = cache;
            _context = context;
        }

        public ApplicationSettings Load()
        {
            var settings = _cache.Get<ApplicationSettings>(_jabbrSettingsCacheKey);

            if (settings == null)
            {
                Settings dbSettings = _context.Settings.FirstOrDefault();

                if (dbSettings == null)
                {
                    // Create the initial app settings
                    settings = ApplicationSettings.GetDefaultSettings();
                    dbSettings = new Settings
                    {
                        RawSettings = JsonConvert.SerializeObject(settings)
                    };

                    _context.Settings.Add(dbSettings);
                    _context.SaveChanges();
                }
                else
                {
                    try
                    {
                        settings = JsonConvert.DeserializeObject<ApplicationSettings>(dbSettings.RawSettings);
                    }
                    catch
                    {
                        // TODO: Record the exception

                        // We failed to load the settings from the db so go back to using the default
                        settings = ApplicationSettings.GetDefaultSettings();

                        dbSettings.RawSettings = JsonConvert.SerializeObject(settings);
                        _context.SaveChanges();
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
            Settings dbSettings = _context.Settings.FirstOrDefault();

            if (dbSettings == null)
            {
                dbSettings = new Settings
                {
                    RawSettings = rawSettings
                };

                _context.Settings.Add(dbSettings);
            }
            else
            {
                dbSettings.RawSettings = rawSettings;
            }

            _context.SaveChanges();

            // Clear the cache
            _cache.Remove(_jabbrSettingsCacheKey);
        }
    }
}