using DataProcessorService;
using DataProcessorService.Data;
using DataProcessorService.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDatabaseConfiguration(configuration);
builder.Services.AddHostedService<DataProcessingWorker>();

builder.Services.AddRabbitMqConfiguration(configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

host.Run();