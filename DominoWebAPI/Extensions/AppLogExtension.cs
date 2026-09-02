using Serilog;
using Serilog.Formatting.Compact; 

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
                    .Enrich.FromLogContext() 
                    .WriteTo.Console(new CompactJsonFormatter())
                    .WriteTo.File(
                        new CompactJsonFormatter(), 
                        "logs/log.json", 
                        rollingInterval: RollingInterval.Day);
            });
        }
    }
}
