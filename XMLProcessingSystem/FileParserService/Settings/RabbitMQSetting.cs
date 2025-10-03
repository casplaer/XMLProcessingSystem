using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace FileParserService.Settings
{
    public class RabbitMQSetting 
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
