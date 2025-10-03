using DataProcessorService;
using DataProcessorService.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString: configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddHostedService<DataProcessingWorker>();

var host = builder.Build();

//using (var scope = host.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.EnsureCreated();
//}

host.Run();
