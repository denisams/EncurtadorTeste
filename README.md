# EncurtadorTeste

Encurtador de URLs em .NET 9 (Minimal API) desenhado para suportar **alto volume de redirecionamentos**, que é o padrão de tráfego típico desse tipo de serviço (leituras/redirects >> escritas).

## Decisões de arquitetura para alto volume

| Problema | Decisão | Por quê |
|---|---|---|
| Gerar códigos únicos sem gargalo no banco | Contador atômico no Redis (`INCR`) + codificação Base62 | Redis processa comandos em uma única thread, então `INCR` é atômico sem lock e sustenta uma taxa de escrita muito maior do que um `AUTO_INCREMENT` do MySQL sob concorrência alta. Não depende de round-trip ao banco para saber se o código já existe. |
| Redirecionamento rápido sob carga | Cache-aside com Redis (TTL de 6h) na frente do MySQL | Redirects são a esmagadora maioria do tráfego; atender a maior parte deles direto do cache evita que o MySQL vire gargalo. |
| Registrar cliques sem atrasar o redirect | Contagem de cliques processada de forma assíncrona via Hangfire (fire-and-forget) | O `UPDATE` de contador de cliques nunca fica no caminho crítico do redirect — se o banco estiver lento, o usuário ainda é redirecionado instantaneamente. |
| Escalar horizontalmente | API stateless (sem sessão em memória); estado vive em Redis/MySQL | Permite subir N instâncias atrás de um load balancer sem coordenação. |
| Consultas de leitura eficientes | Índice único em `Code`, `AsNoTracking()`, `ExecuteUpdateAsync` para incremento atômico | Minimiza custo de I/O e overhead do EF Core no caminho quente. |

### Caminho para escalar ainda mais além de uma única instância de Redis/MySQL
- **Sharding do contador**: múltiplos contadores Redis (`url:code:counter:0..N`), um por partição/instância, evitando qualquer contenção mesmo com muitos nós de API.
- **Réplicas de leitura do MySQL**: como o cache absorve a maior parte das leituras, as poucas que chegam ao banco (cache miss) podem ser direcionadas a réplicas.
- **CDN / edge redirect**: para casos extremos, os redirects mais acessados podem ser cacheados na borda (CDN) usando os headers HTTP corretos.

## Estrutura do projeto

```
src/
  Encurtador.Domain          # Entidades e utilitários puros (ex.: Base62)
  Encurtador.Application     # Casos de uso, DTOs e abstrações (interfaces)
  Encurtador.Infrastructure  # EF Core (MySQL), Redis, Hangfire
  Encurtador.Api             # Minimal API, endpoints, composição/DI
tests/
  Encurtador.Tests           # Testes unitários (xUnit + Moq)
```

## Endpoints

- `POST /api/urls` — encurta uma URL.
  ```json
  { "originalUrl": "https://exemplo.com/pagina-longa", "customAlias": null, "timeToLive": null }
  ```
  Retorna `201 Created` com o código gerado, `409 Conflict` se o alias customizado já existir, ou `400 Bad Request` se a URL for inválida.

- `GET /{code}` — redireciona (`302`) para a URL original, ou `404 Not Found`.

- `GET /health` — health check.

- `GET /hangfire` — dashboard do Hangfire (somente em ambiente de desenvolvimento).

Ambos os endpoints têm *rate limiting* próprio (`/api/urls` mais restrito que o redirect, já que o redirect precisa suportar volume muito maior).

## Rodando localmente

Pré-requisitos: Docker e Docker Compose.

```bash
docker compose up --build
```

A API sobe em `http://localhost:8080`. As migrations do EF Core são aplicadas automaticamente ao iniciar em ambiente `Development`.

### Rodando sem Docker (ex.: via F5 no Visual Studio)

1. Suba só a infra: `docker compose up -d mysql redis`.
   - O MySQL fica em `localhost:3306` e o Redis em `localhost:16379` (porta não-padrão só no mapeamento do host, para não colidir com outro Redis que já esteja rodando na máquina). Isso já está refletido em `appsettings.json`.
2. Rode a API normalmente (`dotnet run --project src/Encurtador.Api` ou F5). As migrations do EF Core são aplicadas automaticamente ao iniciar em ambiente `Development`.

## Testes

```bash
dotnet test
```

## Migrations

```bash
dotnet ef migrations add NomeDaMigration --project src/Encurtador.Infrastructure --startup-project src/Encurtador.Api --output-dir Persistence/Migrations
```

## Simplificações conhecidas (fora do escopo deste teste)

- Não há autenticação/autorização nos endpoints de criação de link.
- Alias customizado e código auto-gerado compartilham o mesmo espaço de nomes; em produção, vale prefixar ou validar para evitar colisão entre os dois.
- Analytics de clique é apenas um contador; um sistema real de analytics em alto volume normalmente usaria um pipeline de eventos (ex.: fila + processamento em lote) em vez de um `UPDATE` por clique.
