# ServicoPix

Projeto de exemplo (API + Worker) demonstrando um fluxo assíncrono de PIX com:

- API em ASP.NET Core
- CQRS com MediatR + FluentValidation
- Persistência com EF Core + Postgres
- Mensageria com **RabbitMQ** (comandos) e **Kafka/Redpanda** (eventos), via **NexusBus**

---

## Visão geral da arquitetura

Camadas/projetos:

- **ServicoPix.Api**: endpoints HTTP e Swagger
- **ServicoPix.Application**: UseCases (Commands/Queries) + validações
- **ServicoPix.Domain**: entidades, eventos, interfaces e regras
- **ServicoPix.Infrastructure**: EF Core, repositórios, UnitOfWork e Adapter de mensageria (`IMensageriaService` → NexusBus)
- **ServicoPix.Worker**: consumo RabbitMQ + publicação/consumo Kafka (demonstração)

Fluxo principal:

1. `POST /api/v1/pix` (API) cria um protocolo e publica um **comando** em RabbitMQ
2. Worker consome a fila e, ao “processar”, publica um **evento** no Kafka
3. Worker também assina o tópico para confirmar o evento (demo)

---

## Endpoints

### Solicitar PIX

- Método: `POST`
- Rota: `/api/v1/pix`
- Resposta: `202 Accepted` com `Protocolo`

Exemplo de body:

```json
{
  "contaOrigemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "contaDestinoId": "3fa85f64-5717-4562-b3fc-2c963f66af16",
  "valor": 123
}
```

### Consultar status

- Método: `GET`
- Rota: `/api/v1/pix/{id}`
- Resposta: `200` com DTO ou `404`

---

## Mensageria (contratos e canais)

### Comando (RabbitMQ)

- Fila: `queue.pix.processar`
- Publicado pelo UseCase `SolicitarPixHandler`
- Payload (anônimo):
  - `Id` (Guid) = protocolo
  - `Dados` = `SolicitarPixCommand`

### Evento (Kafka)

- Tópico (processado): `topic.pix.processado`
  - Publicado pelo Worker após consumir a fila
  - Consumido pelo próprio Worker (demo)

- Tópico (fatos): `topic.pix.fatos`
  - Publicado pelo UseCase `EfetivarPixHandler` com `PixRealizadoEvent`
  - Status: `SUCESSO` ou `FALHA`

---

## Rodando local com Docker

Pré-requisitos:

- Docker Desktop
- .NET SDK 10

Subir a stack:

```powershell
docker compose up -d --build
```

Serviços e portas (host):

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ:
  - AMQP: `localhost:5672`
  - Management: `http://localhost:15672` (user/pass: `servicopix`/`servicopix`)
- Kafka (Redpanda): `localhost:19092`
- Kafka UI: `http://localhost:8081`

> Observação: o Postgres **não publica porta no host** (para evitar conflito). A API acessa via rede Docker (`Host=postgres;Port=5432`).

---

## Configuração

### API

- String de conexão: `ConnectionStrings:DefaultConnection`
- Config de mensageria via env vars no Docker Compose:
  - `NexusBus__RabbitMq__HostName=rabbitmq` etc.
  - `NexusBus__Kafka__BootstrapServers=kafka:9092`

### Worker

- Usa `AddNexusBus(builder.Configuration)`
- Em execução via Docker, recebe env vars apontando para `rabbitmq` e `kafka`

---

## Logs e observabilidade

Logs foram intencionalmente padronizados para ficar claro qual transporte está sendo usado:

- No NexusBus:
  - `NexusBus[RabbitMQ]: ...`
  - `NexusBus[Kafka]: ...`
- Na camada de Adapter (API/Infrastructure):
  - `Mensageria[RabbitMQ]: Publicando comando (...)`
  - `Mensageria[Kafka]: Publicando evento (...)`
- No Worker:
  - `Mensagem recebida [RabbitMQ] ...`
  - `Evento recebido [Kafka] ...`

Comandos úteis:

```powershell
docker compose logs -f servicopix.api
```

```powershell
docker compose logs -f servicopix.worker
```

---

## Testes

O repositório possui testes unitários para:

- NexusBus (DI e helpers)
- ServicoPix (Adapter, Worker e UseCases do Application)

Executar:

```powershell
dotnet test .\TesteSenff.slnx -v minimal
```

---

## Troubleshooting

- **HTTP 500 após subir só API/Worker**: normalmente é porque `rabbitmq`/`kafka` não estão disponíveis na rede do container. Suba o compose completo (`docker compose up -d`).
- **Kafka “unhealthy” bloqueando o worker**: nesta stack o worker depende de `kafka: service_started` para evitar bloqueio durante bootstrap.
- **Conflito de porta do Postgres no host**: a stack não publica porta do Postgres por padrão; se você publicar manualmente e já tiver Postgres local rodando, vai conflitar.
