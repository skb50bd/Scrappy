using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Scrappy.Core;

public interface IQueueClient<T>
{
    void Publish(T message);
    void Consume(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken);
    bool QueueExists();
    bool EnsureQueueExists();
}

public class RabbitMqConfig
{
    public required string HostName { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class QueueClient<T>(
        IOptions<RabbitMqConfig> rabbitMqOptions,
        ILogger<QueueClient<T>> logger
    ) : IQueueClient<T>
{
    private readonly ConnectionFactory _connFactory = new()
    {
            HostName                 = rabbitMqOptions.Value.HostName,
            Port                     = rabbitMqOptions.Value.Port,
            UserName                 = rabbitMqOptions.Value.Username,
            Password                 = rabbitMqOptions.Value.Password,
            VirtualHost              = "/",
            DispatchConsumersAsync   = true,
            AutomaticRecoveryEnabled = true
    };

    private static string Topic => typeof(T).Name;

    public bool QueueExists()
    {
        try
        {
            using var connection = _connFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclarePassive(Topic);
            return true;
        }
        catch (OperationInterruptedException)
        {
            return false;
        }
    }

    public bool EnsureQueueExists()
    {
        try
        {
            if (QueueExists())
            {
                return true;
            }

            using var connection = _connFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(
                queue      : Topic,
                durable    : false,
                exclusive  : false,
                autoDelete : false,
                arguments  : null
            );

            return true;
        }
        catch (OperationInterruptedException)
        {
            return false;
        }
    }

    public void Consume(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connFactory.CreateConnection();
            using var channel = connection.CreateModel();
            var consumer = new AsyncEventingBasicConsumer(channel);
            string? message = null;

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                message = Encoding.UTF8.GetString(body);
                var txn =
                    JsonSerializer.Deserialize<T>(message)
                    ?? throw new Exception($"Failed to deserialize message: {message}");

                await handler(txn, CancellationToken.None);
            };

            channel.BasicConsume(
                queue: Topic,
                autoAck: true,
                consumer: consumer
            );
        }
        catch (BrokerUnreachableException exn)
        {
            logger.LogWarning(
                exn,
                "RabbitMQ Connection Failed: {exnMessage}",
                exn.Message
            );
        }
        catch (Exception exn)
        {
            logger.LogCritical(
                exn,
                "Cannot consume messages from message queue."
            );
        }
    }

    public void Publish(T obj)
    {
        try
        {
            using var connection = _connFactory.CreateConnection();
            using var channel = connection.CreateModel();
            var message = JsonSerializer.Serialize(obj);
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(
                exchange        : "",
                routingKey      : Topic,
                basicProperties : null,
                body            : body
            );
        }
        catch (BrokerUnreachableException exn) // failed to connect to RabbitMQ
        {
            // TODO: add retry logic
            logger.LogWarning(
                "RabbitMQ Connection Failed: {exnMessage}",
                exn.Message
            );
        }
        catch (Exception exn)
        {
            logger.LogError(
                exn,
                "Failed to publish msg: {msg}",
                obj
            );
        }
    }
}
