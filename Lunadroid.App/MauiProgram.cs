using CommunityToolkit.Maui;
using Lunadroid.App.Services;
using Lunadroid.App.ViewModels;
using Lunadroid.Core.Api;
using Lunadroid.Core.Services;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using UraniumUI;
using Environment = Android.OS.Environment;
using File = Java.IO.File;

namespace Lunadroid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(false) // false to avoid Android foreground service crash
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                fonts.AddMaterialSymbolsFonts();
            });

        builder.Services.AddCommunityToolkitDialogs();

        // Database
        File? publicDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments);
        string dbPath = Path.Combine(publicDir!.AbsolutePath, "com.lunadroid.app", "lunadroid.db");
        builder.Services.AddSingleton(new DatabaseService(dbPath));

        // Config
        builder.Services.AddSingleton(new AppConfigService(Path.Combine(publicDir!.AbsolutePath, "com.lunadroid.app")));

        // 影视资源查找
        builder.Services.AddVideoServices();

        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<TermsViewModel>();
        builder.Services.AddTransient<AppLockViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<PlayerViewModel>();

        return builder.Build();
    }

    /// <summary>
    ///     注入通用服务
    /// </summary>
    /// <param name="serviceCollection"></param>
    public static void AddVideoServices(this IServiceCollection serviceCollection)
    {
        // 影视资源查找
        serviceCollection.AddScoped<MovieTvService>();

        //影视资源
        // Configure Refit and Resilience
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        var defaultRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        };

        // Refit settings for IApiFactory
        JsonSerializerOptions defaultSystemTextJsonSettings = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        defaultSystemTextJsonSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        var apiFactoryRefitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(defaultSystemTextJsonSettings),
            ExceptionFactory = async response =>
            {
                if (!response.IsSuccessStatusCode)
                    // var error = await response.Content.ReadAsStringAsync();
                {
                    Console.WriteLine($"API 错误: {response.StatusCode}");
                }

                return null;
            }
        };

        // Add Refit client factory
        serviceCollection
            .AddSingleton<IApiFactory, ApiFactory>(provider =>
                new ApiFactory(
                    provider.GetRequiredService<IHttpClientFactory>()
                )
                {
                    RefitSettings = apiFactoryRefitSettings
                })
            .ConfigureHttpClientDefaults(config => config.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    AllowAutoRedirect = true
                })
            );

        serviceCollection
            .AddRefitClient<IWebApi>(defaultRefitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://movie.douban.com");
                c.Timeout = TimeSpan.FromHours(1);
                c.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
                c.DefaultRequestHeaders.Add("User-Agent", UserAgent.GetRandomUserAgent());
                c.DefaultRequestHeaders.Add("Referer", "https://movie.douban.com/");
                c.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
                c.Timeout = TimeSpan.FromSeconds(20);
            })
            .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);  // 总的超时时间
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);        //每次重试的超时时间
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30); //熔断时间
                }
            );
    }
}