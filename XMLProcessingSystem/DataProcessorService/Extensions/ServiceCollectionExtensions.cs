using DataProcessorService.Data;
using DataProcessorService.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DataProcessorService.Extensions
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
                    Port = rabbitMQSettings.Port,
                    UserName = rabbitMQSettings.UserName,
                    Password = rabbitMQSettings.Password,
                    AutomaticRecoveryEnabled = rabbitMQSettings.AutomaticRecoveryEnabled,
                    TopologyRecoveryEnabled = rabbitMQSettings.TopologyRecoveryEnabled,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(rabbitMQSettings.NetworkRecoveryInterval)
                };
            });

            services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().Result;
            });

            return services;
        }

        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(connectionString: configuration.GetConnectionString("DefaultConnection"));
            });

            return services;
        }
    }
}