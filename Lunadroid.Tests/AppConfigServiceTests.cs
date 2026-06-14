using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.Tests;

public class AppConfigServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly AppConfigService _service;

    public AppConfigServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"lunadroid_config_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _service = new AppConfigService(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void DefaultConfig_ShouldHaveCorrectDefaults()
    {
        var config = _service.Config;
        Assert.False(config.OnboardingCompleted);
        Assert.False(config.TermsAccepted);
        Assert.Equal("System", config.ThemeMode);
        Assert.False(config.SecurityLockEnabled);
        Assert.Null(config.PinCode);
        Assert.Equal("https://pz.v88.qzz.io/?format=0&source=full", config.CloudSourceUrl);
    }

    [Fact]
    public void UpdateConfig_ShouldModifyAndPersist()
    {
        _service.UpdateConfig(c =>
        {
            c.OnboardingCompleted = true;
            c.TermsAccepted = true;
            c.ThemeMode = "Dark";
        });

        Assert.True(_service.Config.OnboardingCompleted);
        Assert.True(_service.Config.TermsAccepted);
        Assert.Equal("Dark", _service.Config.ThemeMode);

        // Create new service to verify persistence
        var reloaded = new AppConfigService(_testDir);
        Assert.True(reloaded.Config.OnboardingCompleted);
        Assert.True(reloaded.Config.TermsAccepted);
        Assert.Equal("Dark", reloaded.Config.ThemeMode);
    }

    [Fact]
    public void ResetConfig_ShouldRestoreDefaults()
    {
        _service.UpdateConfig(c =>
        {
            c.OnboardingCompleted = true;
            c.ThemeMode = "Light";
            c.PinCode = "1234";
            c.SecurityLockEnabled = true;
        });

        _service.ResetConfig();

        Assert.False(_service.Config.OnboardingCompleted);
        Assert.Equal("System", _service.Config.ThemeMode);
        Assert.Null(_service.Config.PinCode);
        Assert.False(_service.Config.SecurityLockEnabled);
    }

    [Fact]
    public void SaveConfig_ShouldCreateFile()
    {
        _service.UpdateConfig(c => c.ThemeMode = "Dark");

        var configFile = Path.Combine(_testDir, "appconfig.json");
        Assert.True(File.Exists(configFile));

        var content = File.ReadAllText(configFile);
        Assert.Contains("Dark", content);
    }

    [Fact]
    public void PinCode_ShouldPersistCorrectly()
    {
        _service.UpdateConfig(c =>
        {
            c.PinCode = "123456";
            c.SecurityLockEnabled = true;
        });

        var reloaded = new AppConfigService(_testDir);
        Assert.Equal("123456", reloaded.Config.PinCode);
        Assert.True(reloaded.Config.SecurityLockEnabled);
    }
}
