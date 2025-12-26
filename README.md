# TesteSenff

## NexusBus (RabbitMQ ou Kafka)

O `NexusBus` suporta seleção do provider via configuração em `appsettings.json`.

### RabbitMQ (padrão)

```json
{
	"NexusBus": {
		"Provider": "RabbitMQ",
		"RabbitMq": {
			"HostName": "localhost",
			"Port": 5672,
			"UserName": "guest",
			"Password": "guest",
			"VirtualHost": "/"
		}
	}
}
```

### Kafka

```json
{
	"NexusBus": {
		"Provider": "Kafka",
		"Kafka": {
			"BootstrapServers": "localhost:9092",
			"ClientId": "nexusbus",
			"GroupId": "nexusbus",
			"AutoOffsetReset": "Earliest"
		}
	}
}
```