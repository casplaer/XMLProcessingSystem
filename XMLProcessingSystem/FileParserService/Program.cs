using FileParserService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ParsingWorker>();

var host = builder.Build();
host.Run();
