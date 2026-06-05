> **Nota:** Esta e a copia independente do microsservico **UsersAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-users-api . (da raiz deste repo).

---

# UsersAPI â€” FCG MicrosserviÃ§o de UsuÃ¡rios

MicrosserviÃ§o responsÃ¡vel por **cadastro, autenticaÃ§Ã£o (JWT)** e **autorizaÃ§Ã£o** de usuÃ¡rios da FIAP Cloud Games. Ã‰ a fonte de verdade de identidade â€” emite tokens que sÃ£o validados localmente pelos demais microsserviÃ§os.

---

## Responsabilidades

- Cadastrar/atualizar/excluir (soft delete) usuÃ¡rios
- Login com geraÃ§Ã£o de JWT (HS256, 8h de validade)
- RecuperaÃ§Ã£o e reset de senha via token de uso Ãºnico (1h)
- Publicar `UserCreatedEvent` no broker apÃ³s registro bem-sucedido

## NÃ£o Ã© responsabilidade

- Validar existÃªncia de usuÃ¡rio a pedido de outros serviÃ§os (eles confiam no JWT)
- Persistir histÃ³rico de logins / auditoria

---

## Endpoints

| MÃ©todo | Rota | Auth | DescriÃ§Ã£o |
|---|---|---|---|
| POST | `/api/auth/register` | pÃºblico | Cria usuÃ¡rio; publica `UserCreatedEvent` |
| POST | `/api/auth/login` | pÃºblico | Retorna JWT + dados do usuÃ¡rio |
| GET | `/api/auth/user/{id}` | pÃºblico | Detalha um usuÃ¡rio (sem dados sensÃ­veis) |
| POST | `/api/auth/recuperar-senha` | pÃºblico | Gera token de reset (1h) |
| POST | `/api/auth/resetar-senha` | pÃºblico | Aplica nova senha usando o token |
| GET | `/api/usuarios` | autenticado | Lista usuÃ¡rios (admins veem todos) |
| GET | `/api/usuarios/{id}` | autenticado | Detalha (admin OU prÃ³prio usuÃ¡rio) |
| PUT | `/api/usuarios/{id}` | autenticado | Atualiza (admin OU prÃ³prio usuÃ¡rio) |
| DELETE | `/api/usuarios/{id}` | role Admin | Soft delete |
| GET | `/health` | pÃºblico | Health check |

Swagger UI em `/swagger` quando `Swagger__Enabled=true`.

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `UserCreatedEvent` | ApÃ³s `POST /api/auth/register` ser persistido | `MessageId, OccurredAt, UserId, Name, Email, Role` |

Consumidores conhecidos: **NotificationsAPI** (envia e-mail de boas-vindas).

## Eventos consumidos

Nenhum â€” UsersAPI Ã© publisher-only.

---

## VariÃ¡veis de ambiente

| VariÃ¡vel | Default | DescriÃ§Ã£o |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=users_db;...` | Postgres do serviÃ§o |
| `JwtSettings__Secret` | dev placeholder | **MÃ­nimo 32 caracteres**. DistribuÃ­do via Secret no K8s |
| `JwtSettings__Issuer` | `FCG.UsersAPI` | Issuer do token |
| `JwtSettings__Audience` | `FCG.Services` | Audience dos consumidores |
| `JwtSettings__ExpirationHours` | `8` | TTL do token |
| `RabbitMq__Host` | `localhost` | Host do broker |
| `RabbitMq__Port` | `5672` | Porta AMQP |
| `RabbitMq__Username` / `__Password` | `guest`/`guest` | Credenciais do broker |
| `Swagger__Enabled` | `true` | Liga/desliga UI em prod |

---

## Como rodar localmente

### PrÃ©-requisitos

- .NET 10 SDK (ou superior conforme PDF)
- Postgres rodando (use `Orchestration/docker-compose.yml` para subir sÃ³ `postgres` e `rabbitmq`)

### Subir sÃ³ a infra e rodar a API com `dotnet run`

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d postgres rabbitmq
dotnet run --project src/FCG.Users.Api
# Swagger: http://localhost:5001/swagger (porta vem de launchSettings ou da env)
```

### Rodar em contÃªiner

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

## DecisÃµes arquiteturais

- **EF Core + `EnsureCreated()` no startup** â€” para o desafio acadÃªmico, evita o ritual de migrations. Em produÃ§Ã£o: migrar para `Database.Migrate()` com migrations versionadas.
- **Sem outbox transacional** â€” usuÃ¡rio Ã© commitado e o evento Ã© publicado; se o publish falhar, log com erro mas request retorna sucesso. Trade-off documentado no README raiz.
- **HS256 com secret simÃ©trico** â€” chave distribuÃ­da via Secret K8s. ProduÃ§Ã£o: RS256 com JWKS endpoint.
- **Soft delete via `IsActive`** â€” preserva histÃ³rico para auditoria.

