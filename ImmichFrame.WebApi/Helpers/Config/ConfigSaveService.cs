using System.Text.Json;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

public class ConfigSaveService(ConfigPathProvider pathProvider, IServerSettings serverSettings)
{
    /// <summary>
    /// Applies <paramref name="updated"/> in memory immediately, then attempts to persist to disk.
    /// </summary>
    /// <returns>
    /// <c>null</c> on full success; a human-readable warning string when the in-memory update
    /// succeeded but the file could not be written (e.g. read-only filesystem).
    /// </returns>
    public string? Save(ServerSettings updated)
    {
        // Always apply in memory first so settings take effect regardless of disk state.
        UpdateGeneralSettingsInMemory(updated.GeneralSettingsImpl ?? new GeneralSettings());

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

            return null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return $"Settings applied for this session but could not be saved to disk: {ex.Message}";
        }
    }

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
    }
}
