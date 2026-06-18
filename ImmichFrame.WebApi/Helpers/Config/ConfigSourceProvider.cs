namespace ImmichFrame.WebApi.Helpers.Config;

public enum ConfigSource { Unknown, File, NewEnvVars, LegacyEnvVars }

public class ConfigSourceProvider
{
    public ConfigSource Source { get; set; } = ConfigSource.Unknown;
}
