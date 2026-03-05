using CallCenter.PbxService;
using CallCenter.PbxService.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("CallCenter PBX Service baslatiliyor...");

    var builder = Host.CreateApplicationBuilder(args);

    // Serilog
    builder.Services.AddSerilog(config => config
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/pbx-.log", rollingInterval: RollingInterval.Day));

    // Configuration
    builder.Services.Configure<PbxConfig>(builder.Configuration.GetSection("Pbx"));
    builder.Services.Configure<SipConfig>(builder.Configuration.GetSection("Sip"));
    builder.Services.Configure<ApiConfig>(builder.Configuration.GetSection("Api"));

    // HttpClient (API iletisimi)
    builder.Services.AddHttpClient("CallCenterApi", (sp, client) =>
    {
        var apiConfig = builder.Configuration.GetSection("Api").Get<ApiConfig>();
        if (apiConfig != null && !string.IsNullOrEmpty(apiConfig.BaseUrl))
            client.BaseAddress = new Uri(apiConfig.BaseUrl);
    });

    // PBX Worker
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PBX Service baslatma hatasi");
}
finally
{
    Log.CloseAndFlush();
}
