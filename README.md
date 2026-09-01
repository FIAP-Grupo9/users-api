> **Nota:** Esta e a copia independente do microsservico **UsersAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-users-api . (da raiz deste repo).

---

# UsersAPI — FCG Microsserviço de Usuários

Microsserviço responsável por **cadastro, autenticação (JWT)** e **autorização** de usuários da FIAP Cloud Games. É a fonte de verdade de identidade — emite tokens que são validados localmente pelos demais microsserviços.

---

## Responsabilidades

- Cadastrar/atualizar/excluir (soft delete) usuários
- Login com geração de JWT (HS256, 8h de validade)
- Recuperação e reset de senha via token de uso único (1h)
- Publicar `UserCreatedEvent` no broker após registro bem-sucedido

## Não é responsabilidade

- Validar existência de usuário a pedido de outros serviços (eles confiam no JWT)
- Persistir histórico de logins / auditoria

---

## Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/api/auth/register` | público | Cria usuário; publica `UserCreatedEvent` |
| POST | `/api/auth/login` | público | Retorna JWT + dados do usuário |
| GET | `/api/auth/user/{id}` | público | Detalha um usuário (sem dados sensíveis) |
| POST | `/api/auth/recuperar-senha` | público | Gera token de reset (1h) |
| POST | `/api/auth/resetar-senha` | público | Aplica nova senha usando o token |
| GET | `/api/usuarios` | autenticado | Lista usuários (admins veem todos) |
| GET | `/api/usuarios/{id}` | autenticado | Detalha (admin OU próprio usuário) |
| PUT | `/api/usuarios/{id}` | autenticado | Atualiza (admin OU próprio usuário) |
| DELETE | `/api/usuarios/{id}` | role Admin | Soft delete |
| GET | `/health` | público | Health check |

Swagger UI em `/swagger` quando `Swagger__Enabled=true`.

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `UserCreatedEvent` | Após `POST /api/auth/register` ser persistido | `MessageId, OccurredAt, UserId, Name, Email, Role` |

Consumidores conhecidos: **NotificationsAPI** (envia e-mail de boas-vindas).

## Eventos consumidos

Nenhum — UsersAPI é publisher-only.

---

## Variáveis de ambiente

| Variável | Default | Descrição |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=users_db;...` | Postgres do serviço |
| `JwtSettings__Secret` | dev placeholder | **Mínimo 32 caracteres**. Distribuído via Secret no K8s |
| `JwtSettings__Issuer` | `FCG.UsersAPI` | Issuer do token |
| `JwtSettings__Audience` | `FCG.Services` | Audience dos consumidores |
| `JwtSettings__ExpirationHours` | `8` | TTL do token |
| `RabbitMq__Host` | `localhost` | Host do broker |
| `RabbitMq__Port` | `5672` | Porta AMQP |
| `RabbitMq__Username` / `__Password` | `guest`/`guest` | Credenciais do broker |
| `Swagger__Enabled` | `true` | Liga/desliga UI em prod |

---

## Como rodar localmente

### Pré-requisitos

- .NET 10 SDK (ou superior conforme PDF)
- Postgres rodando (use `Orchestration/docker-compose.yml` para subir só `postgres` e `rabbitmq`)

### Subir só a infra e rodar a API com `dotnet run`

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d postgres rabbitmq
dotnet run --project src/FCG.Users.Api
# Swagger: http://localhost:5001/swagger (porta vem de launchSettings ou da env)
```

### Rodar em contêiner

```powershell
# Da raiz do repo
docker build -f UsersAPI/Dockerfile -t fcg-users-api .
docker run --rm -p 5001:8080 `
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=users_db;Username=users_user;Password=users_pwd" `
  -e JwtSettings__Secret="fcg-dev-secret-change-me-minimum-32-chars" `
  -e JwtSettings__Issuer="FCG.UsersAPI" -e JwtSettings__Audience="FCG.Services" -e JwtSettings__ExpirationHours=8 `
  -e RabbitMq__Host=host.docker.internal `
  fcg-users-api
```

### Deploy K8s

```powershell
kubectl apply -f k8s/
kubectl port-forward svc/users-api -n fcg 5001:8080
```

---

## Testes

```powershell
dotnet test  # (tests aparecem na fase 5)
```

## Decisões arquiteturais

- **EF Core + `EnsureCreated()` no startup** — para o desafio acadêmico, evita o ritual de migrations. Em produção: migrar para `Database.Migrate()` com migrations versionadas.
- **Sem outbox transacional** — usuário é commitado e o evento é publicado; se o publish falhar, log com erro mas request retorna sucesso. Trade-off documentado no README raiz.
- **HS256 com secret simétrico** — chave distribuída via Secret K8s. Produção: RS256 com JWKS endpoint.
- **Soft delete via `IsActive`** — preserva histórico para auditoria.

