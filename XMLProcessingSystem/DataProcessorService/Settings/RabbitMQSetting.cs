namespace DataProcessorService.Settings
{
    public class RabbitMQSetting 
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; }
        public bool TopologyRecoveryEnabled { get; set; }
        public int NetworkRecoveryInterval { get; set; }
    }
}