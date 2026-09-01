using RabbitMQ.Client;

namespace PaymentGateway.Api.Messaging;

public class RabbitMqConnection : IRabbitMqConnection, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1,1); // aynı anda birden fazla thread'in bağlantıyı açmasını engellemek için
    
    public RabbitMqConnection(IConfiguration config)
    {
        _factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"]!,
            UserName = config["RabbitMQ:UserName"]!,
            Password = config["RabbitMQ:Password"]!
        };
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