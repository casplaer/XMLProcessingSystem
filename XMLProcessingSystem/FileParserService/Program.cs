using FileParserService;
using FileParserService.Extensions;

var builder = Host.CreateApplicationBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddHostedService<ParsingWorker>();

builder.Services.AddRabbitMqConfiguration(configuration);
builder.Services.AddInstrumentStatusXmlSerializer();

var host = builder.Build();

host.Run();
