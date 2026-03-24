# Lehrerleicht - Development Documentation

> This document tracks all architectural decisions, implementation notes, and progress throughout development.

Update this document when something changes and make sure this can be handed in as a university project documentation

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture Decisions](#2-architecture-decisions)
3. [Technology Choices](#3-technology-choices)
4. [Implementation Progress](#4-implementation-progress)
5. [API Documentation](#5-api-documentation)
6. [Database Migrations](#6-database-migrations)
7. [Testing Notes](#7-testing-notes)
8. [Deployment Notes](#8-deployment-notes)
9. [Known Issues & TODOs](#9-known-issues--todos)
10. [Meeting Notes](#10-meeting-notes)

---

## 1. Project Overview

### 1.1 What We're Building

**Lehrerleicht** is a SaaS platform that automates administrative tasks for Austrian teachers by:

1. Connecting to school tools (SchoolFox, WebUntis, Email)
2. Using AI to classify incoming messages and propose actions
3. Presenting actions to teachers for approval (Human-in-the-Loop)
4. Executing approved actions automatically

### 1.2 University Requirements (C# Microservice)

The C# Approval Service must fulfill these requirements:

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| ASP.NET API Controllers | `ApprovalsController`, `TeachersController`, etc. | ⬜ TODO |
| EF Core + PostgreSQL | `ApprovalDbContext` with Npgsql | ⬜ TODO |
| ASP.NET Identity | Extended `IdentityUser` as `Teacher` | ⬜ TODO |
| SignalR (Two-way communication) | `ApprovalHub` for real-time updates | ⬜ TODO |
| Minimum 4 Entities | Teacher, School, Approval, PendingAction, ActionOption, ActionHistory, NotificationPreference (7 total) | ⬜ TODO |
| Endpoint documentation | See Section 5 | ⬜ TODO |

### 1.3 Repository Structure

```
lehrerleicht/
├── services/
│   ├── approval-service/          # C# ASP.NET (Uni project)
│   ├── ingestion-service/         # TypeScript
│   ├── ai-service/                # TypeScript + Mastra.ai
│   ├── execution-service/         # TypeScript
│   └── web-frontend/              # SvelteKit
├── packages/
│   └── shared/                    # Shared TypeScript types
├── scripts/
│   └── init-db.sql               # Database initialization
├── docker-compose.yml
├── .env.example
├── documentation.md              # THIS FILE
└── README.md
```

---

## 2. Architecture Decisions

### ADR-001: Microservice Split

**Date:** 2026-01-15  
**Status:** Accepted

**Context:**  
We need to build a system that processes messages from external systems, classifies them with AI, and allows teachers to approve actions. The university requires a C# component.

**Decision:**  
Split the system into focused microservices:
- **Approval Service (C#)** - Human-in-the-loop workflow
- **Ingestion Service (Node.js)** - External API polling
- **AI Service (Node.js)** - Message classification
- **Execution Service (Node.js)** - Action execution

**Rationale:**
1. Satisfies university C# requirement
2. Allows Node.js for AI/API work (better library ecosystem)
3. Clear boundaries and responsibilities
4. Independent scaling

**Consequences:**
- Need message queue for communication (RabbitMQ)
- More operational complexity
- Need shared type definitions

---

### ADR-002: Message Queue Technology

**Date:** 2026-01-15  
**Status:** Accepted

**Context:**  
Services need to communicate asynchronously.

**Decision:**  
- **Redis + BullMQ** for internal Node.js queues (ingestion, AI processing)
- **RabbitMQ** for cross-service communication (Node.js ↔ C#)

**Rationale:**
- BullMQ is simpler for Node.js-only workflows
- RabbitMQ has excellent .NET support and provides routing features
- Both are battle-tested

---

### ADR-003: Authentication Strategy

**Date:** 2026-01-15  
**Status:** Accepted

**Context:**  
Need authentication for API and SignalR.

**Decision:**  
Use ASP.NET Identity with JWT tokens.

**Rationale:**
1. Satisfies university requirement
2. Works with both REST API and SignalR
3. Standard approach, well-documented

**Token Flow:**
```
1. User logs in → POST /api/auth/login
2. Server returns JWT access token + refresh token
3. Client stores tokens
4. Client sends Authorization: Bearer <token>
5. SignalR: ?access_token=<token> in connection URL
```

---

### ADR-004: Database Strategy

**Date:** 2026-01-15  
**Status:** Accepted

**Context:**  
Multiple services need database access.

**Decision:**  
Single PostgreSQL instance with separate schemas/tables per service.

**Schema Ownership:**
- `approval_service` - Approvals, Teachers, Schools, etc.
- `ingestion_service` - Raw messages, sync state
- `ai_service` - Classifications, embeddings (future)

**Rationale:**
- Simpler ops for a university project
- Can split later if needed
- PostgreSQL handles the load easily

---

## 3. Technology Choices

### 3.1 C# Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime |
| ASP.NET Core | 10.0 | Web API framework |
| Entity Framework Core | 10.0 | ORM |
| ASP.NET Identity | 10.0 | Authentication |
| SignalR | 10.0 | WebSocket communication |
| Npgsql | Latest | PostgreSQL driver |
| RabbitMQ.Client | 6.x | Message queue |
| Serilog | 8.x | Logging |

### 3.2 Node.js Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| Node.js | 22 LTS | Runtime |
| TypeScript | 5.x | Language |
| Mastra.ai | Latest | AI agent orchestration |
| AI SDK | Latest | LLM integration |
| BullMQ | Latest | Redis-based queues |
| amqplib | Latest | RabbitMQ client |
| Drizzle ORM | Latest | Database access |

### 3.3 Frontend Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| SvelteKit | 2.x | Framework |
| TailwindCSS | 3.x | Styling |
| @microsoft/signalr | Latest | SignalR client |
| Superforms | Latest | Form handling |

### 3.4 Infrastructure

| Technology | Version | Purpose |
|------------|---------|---------|
| PostgreSQL | 16 | Database |
| Redis | 7.x | Cache + BullMQ |
| RabbitMQ | 3.x | Message broker |
| Docker | Latest | Containerization |

---

## 4. Implementation Progress

### 4.1 Phase 1: Foundation (Current)

| Task | Assignee | Status | Notes |
|------|----------|--------|-------|
| Create repository structure | - | ⬜ TODO | |
| Set up docker-compose | - | ⬜ TODO | |
| C#: Create solution and projects | - | ⬜ TODO | |
| C#: Define entities | - | ⬜ TODO | |
| C#: Set up EF Core + Identity | - | ⬜ TODO | |
| C#: Create initial migration | - | ⬜ TODO | |
| C#: Implement Auth endpoints | Member 1 | ⬜ TODO | |
| C#: Implement Teacher endpoints | Member 1 | ⬜ TODO | |
| C#: Implement Approval endpoints | Member 2 | ⬜ TODO | |
| C#: Implement SignalR Hub | Member 4 | ⬜ TODO | |
| C#: Implement RabbitMQ consumer | - | ⬜ TODO | |
| C#: Implement RabbitMQ publisher | - | ⬜ TODO | |
| Node: Set up shared package | - | ⬜ TODO | |
| Frontend: Create SvelteKit project | - | ⬜ TODO | |
| Frontend: Login page | - | ⬜ TODO | |
| Frontend: Approvals list | - | ⬜ TODO | |
| Frontend: SignalR integration | - | ⬜ TODO | |

### 4.2 Phase 2: Integration

| Task | Assignee | Status | Notes |
|------|----------|--------|-------|
| Node: Ingestion service skeleton | - | ⬜ TODO | |
| Node: AI service skeleton | - | ⬜ TODO | |
| Node: Execution service skeleton | - | ⬜ TODO | |
| Integration: End-to-end flow test | - | ⬜ TODO | |

### 4.3 Phase 3: Polish

| Task | Assignee | Status | Notes |
|------|----------|--------|-------|
| Unit tests | - | ⬜ TODO | |
| Integration tests | - | ⬜ TODO | |
| API documentation (Swagger) | - | ⬜ TODO | |
| README and setup docs | - | ⬜ TODO | |

---

## 5. API Documentation

### 5.1 Endpoint Summary

#### Auth Endpoints (Member 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new teacher |
| POST | `/api/auth/login` | Login, returns JWT |
| POST | `/api/auth/refresh` | Refresh token |
| POST | `/api/auth/logout` | Invalidate token |
| GET | `/api/auth/me` | Current user info |

#### Teacher Endpoints (Member 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teachers` | List teachers (admin) |
| GET | `/api/teachers/{id}` | Get teacher |
| PUT | `/api/teachers/{id}` | Update teacher |
| DELETE | `/api/teachers/{id}` | Deactivate teacher |

#### Approval Endpoints (Member 2)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/approvals` | List pending approvals |
| GET | `/api/approvals/{id}` | Get approval details |
| POST | `/api/approvals/{id}/approve` | Approve action |
| POST | `/api/approvals/{id}/reject` | Reject action |
| POST | `/api/approvals/{id}/read` | Mark as read |
| GET | `/api/approvals/stats` | Statistics |

#### Action Endpoints (Member 3)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/actions/history` | Action history |
| GET | `/api/actions/history/{id}` | Single history entry |
| GET | `/api/actions/by-student/{id}` | Actions for student |

#### Notification Endpoints (Member 3)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications/preferences` | Get preferences |
| PUT | `/api/notifications/preferences` | Update preferences |
| POST | `/api/notifications/register-device` | Register push token |
| DELETE | `/api/notifications/unregister-device` | Remove token |

#### School Endpoints (Member 4)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/schools` | List schools |
| GET | `/api/schools/{id}` | Get school |
| POST | `/api/schools` | Create school |
| PUT | `/api/schools/{id}` | Update school |

#### Health Endpoints (Member 4)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Liveness |
| GET | `/health/ready` | Readiness |

### 5.2 SignalR Hub

**Endpoint:** `/hubs/approvals`

| Method | Direction | Payload |
|--------|-----------|---------|
| `NewApproval` | Server → Client | `ApprovalDto` |
| `ApprovalProcessed` | Server → Client | `{id, status}` |
| `ApprovalExpired` | Server → Client | `{id}` |
| `SubscribeToSchool` | Client → Server | `{schoolId}` |

---

## 6. Database Migrations

### Migration History

| Date | Name | Description |
|------|------|-------------|
| - | `InitialCreate` | Initial schema with all 6 entities |

### Running Migrations

```bash
# Create new migration
dotnet ef migrations add <Name> \
  -p src/Lehrerleicht.Approval.Infrastructure \
  -s src/Lehrerleicht.Approval.Api

# Apply migrations
dotnet ef database update \
  -p src/Lehrerleicht.Approval.Infrastructure \
  -s src/Lehrerleicht.Approval.Api

# Generate SQL script
dotnet ef migrations script \
  -p src/Lehrerleicht.Approval.Infrastructure \
  -s src/Lehrerleicht.Approval.Api \
  -o migrations.sql
```

---

## 7. Testing Notes

### 7.1 Test Structure

```
tests/
├── Lehrerleicht.Approval.UnitTests/
│   ├── Domain/
│   │   └── ApprovalTests.cs
│   └── Application/
│       └── ApproveActionHandlerTests.cs
│
└── Lehrerleicht.Approval.IntegrationTests/
    ├── Api/
    │   └── ApprovalsControllerTests.cs
    └── Infrastructure/
        └── ApprovalRepositoryTests.cs
```

### 7.2 Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test tests/Lehrerleicht.Approval.UnitTests
```

### 7.3 Integration Test Setup

Using Testcontainers for PostgreSQL and RabbitMQ:

```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer Postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();
        
    protected RabbitMqContainer RabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();
        
    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await RabbitMq.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}
```

---

## 8. Deployment Notes

### 8.1 Local Development

```bash
# Start infrastructure
docker-compose up -d postgres redis rabbitmq

# Run C# service
cd services/approval-service
dotnet run --project src/Lehrerleicht.Approval.Api

# Run Node.js services (in separate terminals)
cd services/ingestion-service && pnpm dev
cd services/ai-service && pnpm dev
cd services/execution-service && pnpm dev

# Run frontend
cd services/web-frontend && pnpm dev
```

### 8.2 Full Docker Setup

```bash
# Build and start everything
docker-compose up --build

# View logs
docker-compose logs -f approval-service

# Stop
docker-compose down

# Reset (including volumes)
docker-compose down -v
```

### 8.3 Environment Variables

Required environment variables:

```bash
# Database
POSTGRES_PASSWORD=

# RabbitMQ  
RABBITMQ_PASSWORD=

# JWT
JWT_SECRET_KEY=  # Min 32 characters

# AI (required for AI service)
ANTHROPIC_API_KEY=

# External APIs (optional)
SCHOOLFOX_API_KEY=
WEBUNTIS_USERNAME=
WEBUNTIS_PASSWORD=
```

---

## 9. Known Issues & TODOs

### Critical

- [ ] None yet

### High Priority

- [ ] Implement rate limiting on API
- [ ] Add request validation with FluentValidation
- [ ] Set up CI/CD pipeline

### Medium Priority

- [ ] Add Swagger/OpenAPI documentation
- [ ] Implement email notifications
- [ ] Add audit logging

### Low Priority

- [ ] Add metrics endpoint (Prometheus)
- [ ] Implement batch approval
- [ ] Mobile app (React Native)

### Technical Debt

- [ ] None yet

---

## 10. Meeting Notes

### 2026-01-15 - Initial Planning

**Attendees:** Team

**Decisions:**
1. Split into microservices with C# for approval workflow
2. Use RabbitMQ for cross-service communication
3. PostgreSQL as primary database
4. SvelteKit for frontend

**Action Items:**
- [ ] Set up repository
- [ ] Create docker-compose
- [ ] Begin C# service implementation

---

*Last Updated: 2026-01-15*
