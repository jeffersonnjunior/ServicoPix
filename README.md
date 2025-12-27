## Contexto do teste (resumo)

Objetivo: desenvolver um pacote NuGet de mensageria em **.NET 10** que abstraia o broker e facilite **publicação/consumo** para outros desenvolvedores, com **confiabilidade** (retentativas/boas práticas), uma **aplicação de exemplo** e **infra via Docker Compose**.

Como este repositório atende:

- **Pacote reutilizável**: `NexusBus` fornece uma interface simples (`INexusBus`) e providers para **RabbitMQ** e **Kafka**.
- **Configuração**: `NexusBus:*` suporta parâmetros de conexão, retries e opções de DLQ.
- **Retentativas/confiabilidade**: RabbitMQ com retry/backoff de conexão e logs; Kafka com commit manual e DLQ opcional.
- **Aplicação de teste**: `ServicoPix` demonstra o fluxo **API → RabbitMQ → Worker → Kafka**.
- **Docker Compose**: stack local completa com Postgres, RabbitMQ, Redpanda (Kafka), Kafka UI, API, Worker e Migrator.
- **Testes**: projetos em `tests/` validam publicação/consumo (via fakes), DI e comportamentos principais.

---

## CI/CD (publicação NuGet)

O repositório já possui um pipeline de CI/CD via GitHub Actions que empacota e publica o **NexusBus** em um feed NuGet.

- Workflow: [.github/workflows/publish-nuget.yml](.github/workflows/publish-nuget.yml)
- Gatilhos: push na branch `master` com mudanças em `NexusBus/**` (e também `workflow_dispatch`)
- Publicação: executa `dotnet pack` e faz `nuget push` usando o segredo `NUGET_API_KEY`

---

## Stack e tecnologias

- .NET 10 (`net10.0`)
- Docker Compose (ambiente local)
- Postgres (banco)
- RabbitMQ (comandos)
- Redpanda (Kafka) + Kafka UI (eventos/inspeção)
- MediatR + FluentValidation
- EF Core + Npgsql

---

## Estrutura da solution

- `NexusBus/`: lib de mensageria
- `ServicoPix.Api/`: API HTTP (Swagger)
- `ServicoPix.Worker/`: consumer/worker
- `ServicoPix.Application/`: UseCases (Commands/Queries) + validações
- `ServicoPix.Domain/`: entidades, eventos, contratos
- `ServicoPix.Infrastructure/`: EF Core, repos, UnitOfWork e adapter da mensageria
- `tests/`: testes unitários (NexusBus + ServicoPix)

---

## Como rodar (Docker)

Pré-requisitos:

- Docker Desktop
- .NET SDK 10 (para build/test fora do container)

Subir tudo:

```powershell
docker compose up -d --build
```

Endpoints/portas no host:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- RabbitMQ:
  - AMQP: `localhost:5672`
  - Management: `http://localhost:15672` (user/pass: `servicopix`/`servicopix`)
- Kafka (Redpanda): `localhost:19092`
- Kafka UI: `http://localhost:8081`

Observação importante:

- O Postgres **não publica porta no host** por padrão (evita conflito com Postgres local). Os containers acessam via rede Docker (`Host=postgres;Port=5432`).

---

## Fluxo E2E (demo)

1. `POST /api/v1/pix`
2. API publica comando em RabbitMQ: `queue.pix.processar`
3. Worker consome a fila e publica evento no Kafka: `topic.pix.processado`
4. Worker também assina `topic.pix.processado` (demonstração) e loga o recebimento

Exemplo de request:

```powershell
$body = @{
  contaOrigemId  = '3fa85f64-5717-4562-b3fc-2c963f66afa6'
  contaDestinoId = '3fa85f64-5717-4562-b3fc-2c963f66af16'
  valor          = 123
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri 'http://localhost:8080/api/v1/pix' -ContentType 'application/json' -Body $body
```

---

## Logs (identificação clara de transporte)

Padrões de logs usados:

- **Lib**: `NexusBus[RabbitMQ]` e `NexusBus[Kafka]`
- **API (adapter)**: `Mensageria[RabbitMQ]` e `Mensageria[Kafka]`
- **Worker**: `Mensagem recebida [RabbitMQ]` e `Evento recebido [Kafka]`

Comandos úteis:

```powershell
docker compose logs -f servicopix.api
```

```powershell
docker compose logs -f servicopix.worker
```

---

## Testes

Executar a suite:

```powershell
dotnet test .\TesteSenff.slnx -v minimal
```

---

## Troubleshooting rápido

- **500 na API**: geralmente acontece quando a stack não está completa (ex.: `rabbitmq`/`kafka` não estão acessíveis no container). Suba o compose completo.
- **Kafka demorando para “subir”**: o worker depende de `kafka: service_started` para não bloquear durante o bootstrap.
- **Conflito de porta**: se você publicar Postgres no host e já tiver Postgres local rodando, vai conflitar.
