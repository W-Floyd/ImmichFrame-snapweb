namespace ImmichFrame.WebApi.Helpers.Config;

public class ConfigPathProvider(string configPath)
{
    public string ConfigPath { get; } = configPath;
}
