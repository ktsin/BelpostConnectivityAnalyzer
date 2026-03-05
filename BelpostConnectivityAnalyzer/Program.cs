using BelpostConnectivityAnalyzer;
using BelpostConnectivityAnalyzer.Api;
using BelpostConnectivityAnalyzer.Configuration;
using BelpostConnectivityAnalyzer.Data;
using BelpostConnectivityAnalyzer.Notifications;
using BelpostConnectivityAnalyzer.Parsing;
using BelpostConnectivityAnalyzer.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection("SyncSettings"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// HTTP client with required Belpost API headers
builder.Services.AddHttpClient("Belpost", client =>
{
    client.BaseAddress = new Uri("https://api.belpost.by/");
    client.DefaultRequestHeaders.Add("Origin", "https://blog.belpost.by");
    client.DefaultRequestHeaders.Add("Referer", "https://blog.belpost.by/");
    client.DefaultRequestHeaders.Add("platform", "web");
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
});

// Data layer
var syncSettings = builder.Configuration.GetSection("SyncSettings").Get<SyncSettings>() ?? new SyncSettings();
builder.Services.AddSingleton(new DbConnectionFactory(syncSettings.DatabasePath));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<CountryRepository>();
builder.Services.AddSingleton<SyncLogRepository>();
builder.Services.AddSingleton<NotificationLogRepository>();

// API + parsing
builder.Services.AddSingleton<BelpostApiClient>();
builder.Services.AddSingleton<PostalServiceAnnouncementHtmlParser>();

// Notifications
builder.Services.AddSingleton<INotificationSender, MailStatusChangeNotificationSender>();

// Services
builder.Services.AddSingleton<SyncService>();
builder.Services.AddSingleton<SchedulerService>();

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Initialize database schema before starting
host.Services.GetRequiredService<DatabaseInitializer>().Initialize();

host.Run();