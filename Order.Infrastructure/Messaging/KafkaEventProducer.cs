using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Order.Application.Messaging;

namespace Order.Infrastructure.Messaging;

public class KafkaEventProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;

    public KafkaEventProducer(IProducer<string, string> producer, ILogger<KafkaEventProducer> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string topicName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(@event);
            var key = GetEventKey(@event);
            var message = new Message<string, string>
            {
                Key = key,
                Value = json,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var result = await _producer.ProduceAsync(topicName, message, cancellationToken);

            _logger.LogInformation("Event published successfully to topic {Topic} at offset {Offset}",
                topicName, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError("Failed to publish event to topic {Topic}. Reason {ex}", topicName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while publishing event to topic {Topic}", topicName);
            throw; 
        }
    }

    private string GetEventKey<TEvent>(TEvent @event)
    {
        var idProperty = typeof(TEvent).GetProperty("Id");
        if(idProperty == null)
            return idProperty.GetValue(@event).ToString() ?? Guid.NewGuid().ToString();

        return Guid.NewGuid().ToString();
    }
    
    public void Dispose() => _producer.Dispose();
}