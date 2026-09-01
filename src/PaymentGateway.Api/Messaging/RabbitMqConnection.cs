using RabbitMQ.Client;

namespace PaymentGateway.Api.Messaging;

public class RabbitMqConnection : IRabbitMqConnection, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1,1); // aynı anda birden fazla thread'in bağlantıyı açmasını engellemek için
    
    public RabbitMqConnection(IConfiguration config)
    {
        var uri = config["RabbitMq:Uri"];

        if (!string.IsNullOrWhiteSpace(uri))
        {
            // Prod (CloudAMQP): tek AMQP URI — host/port/user/pass/vhost/TLS hepsi içinde
            _factory = new ConnectionFactory { Uri = new Uri(uri) };
        }
        else
        {
            // Local / compose: ayrı ayrı host/user/pass
            _factory = new ConnectionFactory
            {
                HostName = config["RabbitMq:HostName"]!,
                UserName = config["RabbitMq:UserName"]!,
                Password = config["RabbitMq:Password"]!
            };
        }
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        // zaten açık bir bağlantı varsa onu döndür
        if (_connection is { IsOpen: true })
            return _connection;
        
        await _lock.WaitAsync(cancellationToken);

        try
        {
            // kilidi beklerken başka bir thread bağlantıyı açmış olabilir, tekrar kontrol et
            if (_connection is { IsOpen: true })
                return _connection;

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock .Release();
        } 
    }
    
    public async ValueTask DisposeAsync()
    {
        if(_connection is not null)
            await  _connection.DisposeAsync();
    }
}