# TODO

## Approval Service - Additional Features to Consider

- [ ] Add Swagger/OpenAPI documentation (`Swashbuckle.AspNetCore`)
- [ ] Add FluentValidation for request DTOs (e.g. validate required fields, email format, password strength)
- [ ] Add rate limiting middleware to protect API endpoints
- [ ] Implement proper refresh token storage in DB with revocation support
- [ ] Add role-based authorization (Admin vs Teacher) instead of just `[Authorize]`
- [ ] Implement batch approval endpoint (approve/reject multiple at once)
- [ ] Add SignalR notification dispatch from `ApprovalConsumerService` when new approvals arrive
- [ ] Add push notification integration (Firebase/APNs) in `ApprovalConsumerService`
- [ ] Add pagination metadata to response headers (X-Total-Count, Link)
- [ ] Add audit logging middleware (who accessed what, when)
- [ ] Add EF Core query filters for soft-delete (e.g. `IsActive` on Teacher)
- [ ] Add database seeding for development (sample schools, teachers, approvals)
- [ ] Add integration tests with Testcontainers (PostgreSQL + RabbitMQ)
- [ ] Add unit tests for `ApprovalService`, `NotificationService`, `ActionHistoryService`
- [ ] Add `.gitignore` for the C# project (bin/, obj/, etc.)
