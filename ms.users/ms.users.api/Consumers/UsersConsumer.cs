using AutoMapper;
using MediatR;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Events;
using ms.users.application.Commands;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ms.users.api.Consumers
{
    public class UsersConsumer : IConsumer
    {
        private readonly ILogger<UsersConsumer> _logger;
        private readonly IMediator _mediator;
        private IConnection _connection;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UsersConsumer(ILogger<UsersConsumer> logger, IMediator mediator, IMapper mapper, IConfiguration configuration)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _configuration = configuration;
        }

        public void Unsubscribe() => _connection?.Dispose();

        public void Subscribe()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetValue<string>("Communication:EventBus:HostName"),
            };

            _logger.LogInformation($"JGP - {_configuration.GetValue<string>("Communication:EventBus:HostName")}");
            _logger.LogInformation($"JGP - {factory.Port}");
            _logger.LogInformation($"JGP - {factory.UserName}");
            _logger.LogInformation($"JGP - {factory.Password}");
            _logger.LogInformation($"JGP - {factory.HostName}");
            _logger.LogInformation($"JGP - {factory.Endpoint}");

            /*
             * Antes surgía el error: RabbitMQ.Client.Exceptions.BrokerUnreachableException: 'None of the specified endpoints were reachable
             * Para solucionarlo, reinicio el contenedor 'ms.users.api'
             */
            _connection = factory.CreateConnection();
            var channel = _connection.CreateModel();

            var queue = typeof(EmployeeCreatedEvent).Name;

            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ReceivedEvent;

            channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
        }

        private async void ReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey == typeof(EmployeeCreatedEvent).Name)
            {
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var employeeCreatedEvent = JsonSerializer.Deserialize<EmployeeCreatedEvent>(message);

                //var result = await _mediator.Send(_mapper.Map<CreateUserAccountCommand>(employeeCreatedEvent));

                var userAccount = _mapper.Map<CreateUserAccountCommand>(employeeCreatedEvent);
                var result = await _mediator.Send(new CreateUserAccountCommand(userAccount.UserName, userAccount.Password, userAccount.Role));
            }
        }
    }
}