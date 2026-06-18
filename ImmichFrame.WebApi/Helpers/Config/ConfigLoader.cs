using System.Collections;
using System.Reflection;
using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

public class ConfigLoader(ILogger<ConfigLoader> _logger)
{
    private string FindConfigFile(string dir, params string[] fileNames)
    {
        if (!Directory.Exists(dir))
        {
            return Path.Combine(dir, fileNames.First());
        }

        return Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => fileNames.Any(name => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
            ?? Path.Combine(dir, fileNames.First());
    }
    public IServerSettings LoadConfig(string configPath)
    {
        var config = LoadConfigRaw(configPath);
        config.Validate();
        return config;
    }
    private IServerSettings LoadConfigRaw(string configPath)
    {
        var jsonConfigPath = FindConfigFile(configPath, "Settings.json");
        if (File.Exists(jsonConfigPath))
        {
            try
            {
                return LoadConfigJson<ServerSettings>(jsonConfigPath);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as current version JSON. ({errorMessage})", e.Message);
            }

            try
            {
                var v1 = LoadConfigJson<ServerSettingsV1>(jsonConfigPath);
                return new ServerSettingsV1Adapter(v1);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as old JSON. ({errorMessage})", e.Message);
            }
        }

        var ymlConfigPath = FindConfigFile(configPath, "Settings.yml", "Settings.yaml");
        if (File.Exists(ymlConfigPath))
        {
            try
            {
                return LoadConfigYaml<ServerSettings>(ymlConfigPath);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as current version YAML. ({errorMessage})", e.Message);
            }

            try
            {
                var v1 = LoadConfigYaml<ServerSettingsV1>(ymlConfigPath);
                return new ServerSettingsV1Adapter(v1);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as old YAML. ({errorMessage})", e.Message);
            }
        }

        try
        {
            var result = LoadConfigFromNewEnvFormat(Environment.GetEnvironmentVariables());
            if (result != null) return result;
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config from namespaced env vars ({errorMessage})", e.Message);
        }

        try
        {
            var v1 = LoadConfigFromDictionary<ServerSettingsV1>(Environment.GetEnvironmentVariables());
            return new ServerSettingsV1Adapter(v1);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config as env vars ({errorMessage})", e.Message);
        }

        throw new ImmichFrameException("Failed to load configuration");
    }

    // Loads settings from env vars using the namespaced format:
    //   General__Interval=45
    //   Accounts__0__ImmichServerUrl=https://...
    //   Accounts__1__ImmichServerUrl=https://...
    // Returns null if no General__ or Accounts__ vars are found.
    internal ServerSettings? LoadConfigFromNewEnvFormat(IDictionary env)
    {
        const string generalPrefix = "General__";
        const string accountsPrefix = "Accounts__";
        const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

        var general = new GeneralSettings();
        var accounts = new SortedDictionary<int, ServerAccountSettings>();
        bool found = false;

        foreach (DictionaryEntry entry in env)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString() ?? string.Empty;
            if (key == null) continue;

            if (key.StartsWith(generalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var propName = key[generalPrefix.Length..];
                var prop = typeof(GeneralSettings).GetProperty(propName, flags);
                if (prop != null) { general.SetValue(prop, value); found = true; }
            }
            else if (key.StartsWith(accountsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var rest = key[accountsPrefix.Length..];
                var sep = rest.IndexOf("__", StringComparison.Ordinal);
                if (sep < 0 || !int.TryParse(rest[..sep], out var idx)) continue;

                var propName = rest[(sep + 2)..];
                if (!accounts.TryGetValue(idx, out var account))
                    accounts[idx] = account = new ServerAccountSettings();

                var prop = typeof(ServerAccountSettings).GetProperty(propName, flags);
                if (prop != null) { account.SetValue(prop, value); found = true; }
            }
        }

        if (!found) return null;

        return new ServerSettings
        {
            GeneralSettingsImpl = general,
            AccountsImpl = accounts.Values.ToList()
        };
    }

    internal T LoadConfigFromDictionary<T>(IDictionary env) where T : IConfigSettable, new()
    {
        var config = new T();
        var propertiesSet = 0;

        foreach (var key in env.Keys)
        {
            if (key == null) continue;

            var propertyInfo = typeof(T).GetProperty(key.ToString() ?? string.Empty);

            if (propertyInfo != null)
            {
                config.SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
                propertiesSet++;
            }
        }

        if (propertiesSet < 2)
        {
            throw new ImmichFrameException("No environment variables found");
        }

        return config;
    }

    internal T LoadConfigJson<T>(string configPath) where T : IConfigSettable, new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var doc = JsonDocument.Parse(json);
                return doc.Deserialize<T>() ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }
    internal T LoadConfigYaml<T>(string configPath) where T : IConfigSettable, new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var yml = File.ReadAllText(configPath);
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                return deserializer.Deserialize<T>(yml) ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }
}
