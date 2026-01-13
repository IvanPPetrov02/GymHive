# GymHive — GDPR / privacy notes (current state)

This document summarizes privacy-related measures implemented in the GymHive codebase.
It is not a legal compliance guarantee; GDPR compliance also depends on operational policies, hosting, retention decisions, contracts, and legal review.

## Implemented measures

### Transparency

- Frontend includes Privacy Policy and Terms pages/routes.

### Right to erasure (account deletion) across services

GymHive implements an event-driven deletion cascade to remove user-linked data across services.

- Authentication service publishes a user deletion event (includes a `SagaId`).
- Downstream services consume the event and delete user-linked records.

### Deletion reliability

- RabbitMQ queues events so temporary outages do not lose delete requests.
- Consumers use explicit acknowledgements.
- Publisher uses persistent delivery mode.

### Data minimization in logs

- Avoid routine logging of personal data where possible (emails/user ids).

### Authorization hardening (IDOR risk reduction)

- User-scoped endpoints restrict access to own resources unless Admin.
- Moderator actions are scoped where possible.

## High-level code pointers

- Frontend pages/routes:
  - `GymHiveFrontend/src/lib/pages/Privacy.svelte`
  - `GymHiveFrontend/src/lib/pages/Terms.svelte`
- Messaging reliability (publisher): `GymHiveBackend/GymHive.Messaging/RabbitMQ/RabbitMQEventPublisher.cs`
- Deletion consumers (examples):
  - `GymHiveBackend/WorkoutLoggingService/Services/UserDeletionEventConsumer.cs`
  - `GymHiveBackend/GymService/Services/GymEventConsumer.cs`

## Not implemented yet (common gaps)

- DSAR export (“download my data”) across all services.
- Retention policy enforcement (including backups retention alignment).
- Consent/preferences management (where applicable).
- Full authorization review of all endpoints.

## Assumptions / trust boundaries

- Backend services trust identity/role headers (`X-User-*`) injected by the API Gateway.
- This assumes internal service endpoints are not directly reachable by untrusted clients.

Last updated: 2026-01-06
