using FileParserService.Dto;
using FileParserService.Settings;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Xml.Serialization;

namespace FileParserService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqConfiguration(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<RabbitMQSetting>(cfg.GetSection("RabbitMQ"));

            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var rabbitMQSettings = sp.GetRequiredService<IOptions<RabbitMQSetting>>().Value;
                return new ConnectionFactory
                {
                    HostName = rabbitMQSettings.HostName,
                    UserName = rabbitMQSettings.UserName,
                    Password = rabbitMQSettings.Password
                };
            });

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().Result;
            });

            return services;
        }

        public static IServiceCollection AddInstrumentStatusXmlSerializer(this IServiceCollection services)
        {
            services.AddSingleton<XmlSerializer>(sp =>
            {
                var factory = new XmlSerializerFactory();
                return factory.CreateSerializer(typeof(InstrumentStatusDto));
            });

            return services;
        }
    }
}
