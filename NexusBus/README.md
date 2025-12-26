# NexusBus

Biblioteca de mensageria para .NET com uma abstração simples de **publish/subscribe**, suportando **RabbitMQ** e **Kafka** (via Confluent.Kafka). O objetivo é facilitar o uso de mensageria em aplicações sem amarrar o código do domínio a um broker específico.

Esta lib foi desenhada para dois cenários:

1. **Selecionar um provider “default”** via configuração (`NexusBus:Provider`) e injetar apenas `INexusBus`.
2. **Usar RabbitMQ e Kafka ao mesmo tempo no mesmo projeto**, injetando as interfaces tipadas `IRabbitMqNexusBus` e `IKafkaNexusBus`.

---

## Instalação

- **Via referência de projeto**: adicione referência ao projeto `NexusBus`.
- **Via NuGet**: o projeto possui CI/CD que empacota e publica o `NexusBus` automaticamente.

### CI/CD (publicação NuGet)

Este repositório inclui um workflow do GitHub Actions que:

- Roda em push na branch `master` quando houver mudanças em `NexusBus/**` (e também pode ser disparado manualmente)
- Executa `dotnet pack` gerando o `.nupkg`
- Publica via `dotnet nuget push` usando o segredo `NUGET_API_KEY`

Arquivo do workflow: [.github/workflows/publish-nuget.yml](../.github/workflows/publish-nuget.yml)

---

## Conceitos e contratos

### Interfaces

- `INexusBus`
  - `PublishAsync<T>(topicOrQueue, message, cancellationToken)`
  - `SubscribeAsync<T>(topicOrQueue, handler, cancellationToken)`
- `IRabbitMqNexusBus : INexusBus`
- `IKafkaNexusBus : INexusBus`

> Importante: `SubscribeAsync` inicia o consumo e **retorna imediatamente**. O loop de consumo roda em background.

---

## Dependency Injection

A integração é via `NexusBus.Extensions.ServiceCollectionExtensions`.

### Registrar os dois providers (recomendado para apps que usam ambos)

```csharp
services.AddNexusBus(configuration);
```

- Registra `IRabbitMqNexusBus` e `IKafkaNexusBus`.
- Registra `INexusBus` como **alias para o provider default**, escolhido por `NexusBus:Provider`.

Seleção:
- Se `NexusBus:Provider` = `Kafka` → `INexusBus` resolve para `IKafkaNexusBus`.
- Caso contrário → `INexusBus` resolve para `IRabbitMqNexusBus`.

### Registrar somente RabbitMQ

```csharp
services.AddNexusBusRabbitMq(configuration);
```

### Registrar somente Kafka

```csharp
services.AddNexusBusKafka(configuration);
```

---

## Configuração

A seção padrão é `NexusBus`.

### NexusOptions

- `Provider` (string): default `RabbitMQ`
- `RabbitMq`: opções do RabbitMQ
- `Kafka`: opções do Kafka

### RabbitMqOptions

Principais campos:
- `HostName` (default: `localhost`)
- `Port` (default: `5672`)
- `UserName` / `Password`
- `VirtualHost` (default: `/`)
- `RetryCount` (default: `3`)

Topologia/publicação:
- `ExchangeName` (default: `nexusbus`)
- `ExchangeType` (default: `direct`)
- `DeclareTopology` (default: `true`)

Dead-letter (opcional):
- `EnableDeadLetter` (default: `false`)
- `DeadLetterExchangeName` (default: `nexusbus.dlx`)
- `DeadLetterQueueSuffix` (default: `.dlq`)

### KafkaOptions

- `BootstrapServers` (default: `localhost:9092`)
- `ClientId` (default: `nexusbus`)
- `GroupId` (default: `nexusbus`)
- `AutoOffsetReset` (default: `Earliest`)

Dead-letter (opcional):
- `EnableDeadLetter` (default: `false`)
- `DeadLetterTopicSuffix` (default: `.dlq`)

Segurança (opcional):
- `SecurityProtocol`: ex. `Plaintext`, `Ssl`, `SaslPlaintext`, `SaslSsl`
- `SaslMechanism`: ex. `Plain`, `ScramSha256`, `ScramSha512`
- `SaslUsername` / `SaslPassword`

---

## Exemplos

### Publicar usando o provider default

```csharp
public sealed class MeuServico(INexusBus bus)
{
    public Task EnviarAsync(CancellationToken ct)
        => bus.PublishAsync("queue.ou.topic", new { Hello = "World" }, ct);
}
```

### Publicar comando em RabbitMQ e evento em Kafka no mesmo serviço

```csharp
public sealed class MeuServico(IRabbitMqNexusBus rabbit, IKafkaNexusBus kafka)
{
    public Task EnviarAsync(CancellationToken ct)
    {
        rabbit.PublishAsync("queue.minha-fila", new { Tipo = "Comando" }, ct);
        return kafka.PublishAsync("topic.meu-topico", new { Tipo = "Evento" }, ct);
    }
}
```

### Consumir mensagens

```csharp
await bus.SubscribeAsync<MeuContrato>(
    "queue.ou.topic",
    async msg =>
    {
        // processa
        await Task.CompletedTask;
    },
    stoppingToken);
```

---

## Logs

A lib emite logs com prefixos para facilitar a identificação do transporte:

- `NexusBus[RabbitMQ]: ...`
- `NexusBus[Kafka]: ...`

Exemplos comuns:
- RabbitMQ: conexão e consumo com `host:port` e `vhost`
- Kafka: consumo com `bootstrapServers` e `groupId`

---

## Observações e limitações

- O consumo Kafka roda em background e faz `Commit` manual após o handler concluir com sucesso.
- Se `EnableDeadLetter` estiver habilitado no Kafka, falhas no handler podem encaminhar a mensagem para o tópico `topic + DeadLetterTopicSuffix`.
- A topologia no RabbitMQ (exchange/binds) é controlada por `DeclareTopology`.

---

## Projeto de referência

O repositório inclui um exemplo real usando a lib em `ServicoPix` (API + Worker) e uma stack Docker local com Postgres, RabbitMQ e Redpanda (Kafka).
