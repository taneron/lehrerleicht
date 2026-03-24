export DOTNET_ROOT := $(HOME)/.dotnet
export PATH := $(HOME)/.dotnet:$(HOME)/.dotnet/tools:$(PATH)

DOTNET := dotnet
APPROVAL_PROJECT := services/approval-service/src/Lehrerleicht.Approval.Api
INFRA_PROJECT := services/approval-service/src/Lehrerleicht.Approval.Infrastructure

.PHONY: dev build infra infra-down migrate clean

# Start the C# approval service
dev:
	$(DOTNET) run --project $(APPROVAL_PROJECT)

# Build without running
build:
	$(DOTNET) build services/approval-service

# Start infrastructure (PostgreSQL, RabbitMQ, Redis)
infra:
	docker compose up -d

# Stop infrastructure
infra-down:
	docker compose down

# Run EF Core migrations
migrate:
	$(DOTNET) ef database update -p $(INFRA_PROJECT) -s $(APPROVAL_PROJECT)

# Clean build artifacts
clean:
	$(DOTNET) clean services/approval-service
