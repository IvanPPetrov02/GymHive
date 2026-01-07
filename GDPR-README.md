# GymHive — GDPR / Privacy Notes (Current State)

This document describes **privacy- and GDPR-aligned measures implemented in the GymHive codebase**.

It is **not** a guarantee of full GDPR compliance. GDPR compliance also depends on operational processes (policies, contracts, incident response, data retention decisions), hosting configuration, and legal review.

## Implemented measures

### 1) Transparency

- The frontend contains **Privacy Policy** and **Terms** pages/routes.
- These pages are intended as a starting point for GDPR transparency requirements.

### 2) Right to erasure (account deletion) across services

GymHive uses an event-driven deletion cascade (a choreographed “deletion SAGA” style flow) to remove user-linked data in multiple services when a user is deleted.

Implemented cleanup includes:

- Authentication service publishes a user deletion event with a `SagaId`.
- Consumers in downstream services handle the deletion event and remove user-linked records.

### 3) Reliability of deletion propagation

Deletion propagation is designed so that:

- If a consuming service is offline, deletion events can be **queued** in RabbitMQ and processed when the service comes back online.
- Consumers use explicit acknowledgement behavior (messages are not silently discarded if processing fails).
- The shared publisher publishes messages with **persistent** delivery mode to reduce loss on broker restart.

This relies on the assumption that:

- Services declare/bind their queues (durable) and are connected to the same RabbitMQ instance.
- Internal services are not directly exposed to the internet (the API Gateway is the trust boundary).

### 4) Data minimization in logs

Several services were updated to reduce personal data exposure in logs:

- Avoid logging email addresses, raw `userId` values, or gateway-injected `X-User-*` header values in routine `Information` logs.
- Prefer non-PII identifiers such as event id / saga id, and aggregated counts.

### 5) Authorization hardening (IDOR risk reduction)

To reduce “Insecure Direct Object Reference” style issues:

- User-scoped endpoints are guarded so the caller can only access their own resources unless they are an Admin.
- Moderator actions are scoped where possible to the moderator’s assigned gym and/or owned resources.

## Where these are implemented (high-level pointers)

- Frontend pages/routes: `GymHiveFrontend/src/lib/pages/Privacy.svelte`, `GymHiveFrontend/src/lib/pages/Terms.svelte`.
- Messaging reliability (publisher): `GymHiveBackend/GymHive.Messaging/RabbitMQ/RabbitMQEventPublisher.cs`.
- Deletion consumers (examples):
  - `GymHiveBackend/WorkoutLoggingService/Services/UserDeletionEventConsumer.cs`
  - `GymHiveBackend/GymService/Services/GymEventConsumer.cs`
- Authorization hardening (examples):
  - `GymHiveBackend/AuthenticationService/AuthenticationController.cs`
  - `GymHiveBackend/MembershipService/Controllers/MembershipsController.cs`
  - `GymHiveBackend/GymService/Controllers/GymGroupsController.cs`

## Remaining gaps / not implemented yet

These are common GDPR requirements that are **not fully implemented** in this repository at the time of writing:

- **DSAR export (“download my data”)** across all services.
- **Retention policy enforcement** (automatic deletion/archival rules, retention windows, backups retention alignment).
- **Consent / preference management** (where applicable) and explicit processing-purpose tracking.
- **Comprehensive authorization review** for all endpoints in all services.
- **Operational requirements**: records of processing, DPIA (if required), breach notification workflow, DPA/vendor contracts, access control for logs/monitoring data, etc.

## Assumptions / trust boundaries

- Backend services trust identity/role headers (`X-User-*`) injected by the API Gateway.
- This model assumes internal service endpoints are not reachable directly by untrusted clients.

## Date

Last updated: 2026-01-06
