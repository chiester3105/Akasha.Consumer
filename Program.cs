using Akasha.Consumer.Services;
using Akasha.Consumer.Workers;
using DbUp;

namespace Akasha.Consumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.Services.AddSingleton<MatchResultRepository>();
            builder.Services.AddHostedService<KafkaConsumerWorker>();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            
            var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var connectionString = config.GetConnectionString("Postgres");

                var upgrader = DeployChanges.To.PostgresqlDatabase(connectionString)
                    .WithScriptsFromFileSystem("./Migrations")
                    .LogToConsole()
                    .Build();

                var result = upgrader.PerformUpgrade();

                if(!result.Successful)
                {
                    Console.WriteLine($"Migration failed {result.Error}");
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("Migration applied");
                }
            }

            host.Run();
        }
    }
}