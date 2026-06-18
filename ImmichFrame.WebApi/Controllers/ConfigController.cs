using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IGeneralSettings _settings;
        private readonly IServerSettings _serverSettings;
        private readonly ConfigSaveService _configSaveService;

        public ConfigController(ILogger<AssetController> logger, IGeneralSettings settings, IServerSettings serverSettings, ConfigSaveService configSaveService)
        {
            _logger = logger;
            _settings = settings;
            _serverSettings = serverSettings;
            _configSaveService = configSaveService;
        }

        [HttpGet(Name = "GetConfig")]
        public ClientSettingsDto GetConfig(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Config requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            var dto = ClientSettingsDto.FromGeneralSettings(_settings);
            dto.Configured = _serverSettings.Accounts.Any();
            return dto;
        }

        [HttpGet("Version", Name = "GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }

        [HttpGet("Full", Name = "GetFullConfig")]
        [Authorize]
        public IActionResult GetFullConfig()
        {
            var general = _serverSettings.GeneralSettings as GeneralSettings
                ?? MapGeneralSettings(_serverSettings.GeneralSettings);

            var accounts = _serverSettings.Accounts
                .Select(a => a as ServerAccountSettings ?? MapAccountSettings(a))
                .ToList();

            return Ok(new ServerSettings
            {
                GeneralSettingsImpl = general,
                AccountsImpl = accounts
            });
        }

        [HttpPut(Name = "UpdateConfig")]
        [Authorize]
        public IActionResult UpdateConfig([FromBody] ServerSettings settings)
        {
            try
            {
                var result = _configSaveService.Save(settings);
                if (result.Warning is not null)
                    _logger.LogWarning("Settings not fully persisted: {Warning}", result.Warning);
                return Ok(new
                {
                    message = result.Warning ?? "Settings saved. Account changes require a server restart.",
                    envVars = result.EnvVars
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                return StatusCode(500, new { message = $"Failed to save settings: {ex.Message}" });
            }
        }

        private static GeneralSettings MapGeneralSettings(IGeneralSettings src) => new()
        {
            DownloadImages = src.DownloadImages,
            Language = src.Language,
            ImageLocationFormat = src.ImageLocationFormat,
            PhotoDateFormat = src.PhotoDateFormat,
            Interval = src.Interval,
            TransitionDuration = src.TransitionDuration,
            ShowClock = src.ShowClock,
            ClockFormat = src.ClockFormat,
            ClockDateFormat = src.ClockDateFormat,
            ShowProgressBar = src.ShowProgressBar,
            ShowPhotoDate = src.ShowPhotoDate,
            ShowImageDesc = src.ShowImageDesc,
            ShowPeopleDesc = src.ShowPeopleDesc,
            ShowTagsDesc = src.ShowTagsDesc,
            ShowAlbumName = src.ShowAlbumName,
            ShowImageLocation = src.ShowImageLocation,
            PrimaryColor = src.PrimaryColor,
            SecondaryColor = src.SecondaryColor,
            Style = src.Style,
            BaseFontSize = src.BaseFontSize,
            ShowWeatherDescription = src.ShowWeatherDescription,
            WeatherIconUrl = src.WeatherIconUrl,
            ImageZoom = src.ImageZoom,
            ImagePan = src.ImagePan,
            ImageFill = src.ImageFill,
            PlayAudio = src.PlayAudio,
            Layout = src.Layout,
            SnapAudio = src.SnapAudio,
            SnapserverUrl = src.SnapserverUrl,
            RenewImagesDuration = src.RenewImagesDuration,
            Webcalendars = src.Webcalendars?.ToList() ?? [],
            RefreshAlbumPeopleInterval = src.RefreshAlbumPeopleInterval,
            WeatherApiKey = src.WeatherApiKey,
            UnitSystem = src.UnitSystem,
            WeatherLatLong = src.WeatherLatLong,
            Webhook = src.Webhook,
            AuthenticationSecret = src.AuthenticationSecret,
            OidcAuthority = src.OidcAuthority,
            OidcClientId = src.OidcClientId,
            OidcScopes = src.OidcScopes,
            OidcProtectFrame = src.OidcProtectFrame,
        };

        private static ServerAccountSettings MapAccountSettings(IAccountSettings src) => new()
        {
            ImmichServerUrl = src.ImmichServerUrl,
            ApiKey = src.ApiKey,
            ShowMemories = src.ShowMemories,
            ShowFavorites = src.ShowFavorites,
            ShowArchived = src.ShowArchived,
            ShowVideos = src.ShowVideos,
            ImagesFromDays = src.ImagesFromDays,
            ImagesFromDate = src.ImagesFromDate,
            ImagesUntilDate = src.ImagesUntilDate,
            Albums = src.Albums?.ToList() ?? [],
            ExcludedAlbums = src.ExcludedAlbums?.ToList() ?? [],
            People = src.People?.ToList() ?? [],
            Tags = src.Tags?.ToList() ?? [],
            Rating = src.Rating,
        };
    }
}
