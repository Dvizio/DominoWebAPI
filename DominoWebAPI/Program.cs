using DominoWebAPI.Hubs;
using DominoWebAPI.Services;
using DominoWebAPI.Extensions;
using Serilog;  

var builder = WebApplication.CreateBuilder(args);


try
{
    builder.Host.ConfigureLogging();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<DominoWebAPI.Services.GameSessionManager>();
    builder.Services.AddHostedService<DominoWebAPI.Services.GameSessionCleanupService>();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    //Cors
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            // policy.WithOrigins("https://domino.lets-see-frrl.work")
            policy.SetIsOriginAllowed(_ => true)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
        });
    });
    
    var app = builder.Build();
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<GameHub>("/gameHub");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}