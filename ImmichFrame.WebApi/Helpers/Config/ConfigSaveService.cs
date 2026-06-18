using System.Reflection;
using System.Text.Json;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

public record SaveResult(string? Warning, string? EnvVars);

public class ConfigSaveService(ConfigPathProvider pathProvider, IServerSettings serverSettings, ConfigSourceProvider configSource)
{
    /// <summary>
    /// Applies <paramref name="updated"/> in memory immediately, then attempts to persist to disk.
    /// When the config was loaded from env vars, returns a suggested env var block instead of
    /// (or in addition to) writing a file.
    /// </summary>
    public SaveResult Save(ServerSettings updated)
    {
        // Always apply in memory first so settings take effect regardless of disk state.
        UpdateGeneralSettingsInMemory(updated.GeneralSettingsImpl ?? new GeneralSettings());

        // When using env vars, generate the updated block and return it — no file to write.
        if (configSource.Source is ConfigSource.NewEnvVars or ConfigSource.LegacyEnvVars)
        {
            return new SaveResult(
                Warning: "Settings applied for this session. To persist, update your environment variables as shown below.",
                EnvVars: SerializeToEnvVars(updated)
            );
        }

        try
        {
            var dir = pathProvider.ConfigPath;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var jsonPath = Path.Combine(dir, "Settings.json");
            var ymlPath = Path.Combine(dir, "Settings.yml");
            var yamlPath = Path.Combine(dir, "Settings.yaml");

            string targetPath;
            bool useJson;

            if (File.Exists(jsonPath)) { targetPath = jsonPath; useJson = true; }
            else if (File.Exists(ymlPath)) { targetPath = ymlPath; useJson = false; }
            else if (File.Exists(yamlPath)) { targetPath = yamlPath; useJson = false; }
            else { targetPath = jsonPath; useJson = true; }

            if (useJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(targetPath, JsonSerializer.Serialize(updated, options));
            }
            else
            {
                var serializer = new SerializerBuilder().Build();
                File.WriteAllText(targetPath, serializer.Serialize(updated));
            }

            return new SaveResult(Warning: null, EnvVars: null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return new SaveResult(
                Warning: $"Settings applied for this session but could not be saved to disk: {ex.Message}",
                EnvVars: null
            );
        }
    }

    private static string SerializeToEnvVars(ServerSettings settings)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var lines = new List<string>();

        var general = settings.GeneralSettingsImpl ?? new GeneralSettings();
        foreach (var prop in typeof(GeneralSettings).GetProperties(flags))
        {
            var line = PropToEnvVar($"General__{prop.Name}", prop.GetValue(general));
            if (line != null) lines.Add(line);
        }

        var accounts = (settings.AccountsImpl ?? []).ToList();
        for (int i = 0; i < accounts.Count; i++)
        {
            foreach (var prop in typeof(ServerAccountSettings).GetProperties(flags))
            {
                var line = PropToEnvVar($"Accounts__{i}__{prop.Name}", prop.GetValue(accounts[i]));
                if (line != null) lines.Add(line);
            }
        }

        return string.Join("\n", lines);
    }

    private static string? PropToEnvVar(string key, object? value) => value switch
    {
        null => null,
        string s when string.IsNullOrEmpty(s) => null,
        List<string> list when list.Count == 0 => null,
        List<Guid> list when list.Count == 0 => null,
        List<string> list => $"{key}={string.Join(",", list)}",
        List<Guid> list => $"{key}={string.Join(",", list)}",
        bool b => $"{key}={b.ToString().ToLowerInvariant()}",
        DateTime dt => $"{key}={dt:yyyy-MM-dd}",
        _ => $"{key}={value}"
    };

    private void UpdateGeneralSettingsInMemory(GeneralSettings src)
    {
        if (serverSettings.GeneralSettings is not GeneralSettings dst)
            return;

        dst.DownloadImages = src.DownloadImages;
        dst.Language = src.Language;
        dst.ImageLocationFormat = src.ImageLocationFormat;
        dst.PhotoDateFormat = src.PhotoDateFormat;
        dst.Interval = src.Interval;
        dst.TransitionDuration = src.TransitionDuration;
        dst.ShowClock = src.ShowClock;
        dst.ClockFormat = src.ClockFormat;
        dst.ClockDateFormat = src.ClockDateFormat;
        dst.ShowProgressBar = src.ShowProgressBar;
        dst.ShowPhotoDate = src.ShowPhotoDate;
        dst.ShowImageDesc = src.ShowImageDesc;
        dst.ShowPeopleDesc = src.ShowPeopleDesc;
        dst.ShowTagsDesc = src.ShowTagsDesc;
        dst.ShowAlbumName = src.ShowAlbumName;
        dst.ShowImageLocation = src.ShowImageLocation;
        dst.PrimaryColor = src.PrimaryColor;
        dst.SecondaryColor = src.SecondaryColor;
        dst.Style = src.Style;
        dst.BaseFontSize = src.BaseFontSize;
        dst.ShowWeatherDescription = src.ShowWeatherDescription;
        dst.WeatherIconUrl = src.WeatherIconUrl;
        dst.ImageZoom = src.ImageZoom;
        dst.ImagePan = src.ImagePan;
        dst.ImageFill = src.ImageFill;
        dst.PlayAudio = src.PlayAudio;
        dst.Layout = src.Layout;
        dst.SnapAudio = src.SnapAudio;
        dst.SnapserverUrl = src.SnapserverUrl;
        dst.RenewImagesDuration = src.RenewImagesDuration;
        dst.Webcalendars = src.Webcalendars;
        dst.RefreshAlbumPeopleInterval = src.RefreshAlbumPeopleInterval;
        dst.WeatherApiKey = src.WeatherApiKey;
        dst.UnitSystem = src.UnitSystem;
        dst.WeatherLatLong = src.WeatherLatLong;
        dst.Webhook = src.Webhook;
        dst.AuthenticationSecret = src.AuthenticationSecret;
        dst.OidcAuthority = src.OidcAuthority;
        dst.OidcClientId = src.OidcClientId;
        dst.OidcScopes = src.OidcScopes;
        dst.OidcProtectFrame = src.OidcProtectFrame;
    }
}
