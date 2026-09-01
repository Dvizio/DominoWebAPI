using Serilog;

namespace DominoWebAPI.Extensions
{
    public static class AppLogExtension
    {
        public static void ConfigureLogging(this IHostBuilder builder)
        {
            builder.UseSerilog((context, configuration) =>
            {
                configuration
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File(
                        "logs/log.txt",
                        rollingInterval: RollingInterval.Day);
            });
        }
    }
}
