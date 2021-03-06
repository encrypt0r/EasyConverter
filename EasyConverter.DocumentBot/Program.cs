using EasyConverter.Shared;
using EasyConverter.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyConverter.DocumentBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MessageQueueService>();
                    services.AddSingleton<IStorageProvider>(p => MinioStorageProviderFactory.Create(p.GetService<IConfiguration>()));
                    services.AddHostedService<Worker>();
                });
    }
}
