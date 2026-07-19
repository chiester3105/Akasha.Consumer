using Akasha.Consumer.Services;
using Akasha.Contracts;
using Confluent.Kafka;

namespace Akasha.Consumer.Workers
{
    public class KafkaConsumerWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly MatchResultRepository _repository;

        public KafkaConsumerWorker(ILogger<KafkaConsumerWorker> logger,
            IConfiguration config, MatchResultRepository repository)
        {
            _config = config;
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = _config["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = true,
                SessionTimeoutMs = 60000,
                MaxPollIntervalMs = 300000,
                AllowAutoCreateTopics = false
            };

            using (var consumer = new ConsumerBuilder<string, byte[]>(config).Build())
            {
                consumer.Subscribe(_config["Kafka:Topic"]);
                _logger.LogInformation("Kafka consumer started");

                try
                {
                    while(!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = consumer.Consume(stoppingToken);
                            if (result?.Message == null) continue;

                            using var ms = new MemoryStream(result.Message.Value);
                            var match = ProtoBuf.Serializer.Deserialize<MatchRecord>(ms);

                            await _repository.ProcessMessageAsync(match, stoppingToken);

                            consumer.Commit(result);
                            _logger.LogInformation($"Commited offset for match {match.MatchId}");
                        }
                        catch (ConsumeException ex) when (ex.Error.IsFatal)
                        {
                            _logger.LogCritical(ex, $"Fatal consume error {ex.Error.Reason}");
                            break;
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogWarning(ex, $"Consume error: {ex.Error.Reason}");
                            await Task.Delay(1000, stoppingToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "Processing error");
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation($"Graceful shotdown");
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("Kafka consumer closed");
                }                
            }
        }
    }
}
