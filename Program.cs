using SmartSyncWorkerService;


public static class Program
{
     public static void Main(string[] args)
    {
        var builder = CreateHostBuilder(args);
    
        builder.ConfigureServices(services =>
        {
          services.AddHttpClient();
        });

        var host = builder.Build();
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging((_, logging) => logging.AddEventLog())
            .ConfigureServices((_, services) => 
            {
                IConfiguration configuration = _.Configuration;
                services.Configure<AppConfiguration>(configuration.GetSection(nameof(AppConfiguration)));
                services.AddHostedService<Worker>();
            });
}
