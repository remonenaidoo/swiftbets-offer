# swiftbets-offer

[![ci](https://github.com/remonenaidoo/swiftbets-offer/actions/workflows/ci.yml/badge.svg)](https://github.com/remonenaidoo/swiftbets-offer/actions/workflows/ci.yml)

Fixtures, odds and results for SwiftBets: a replay feed that streams a real historical EPL season (prices and results) on a compressed loop, the Redis offer store, and the read API. It also owns market suspension.

## Hosts

- `SwiftBets.Offer.Api`: offer read API (Phase 1: `GET /fixtures`, `GET /fixtures/{id}`, operator suspend/resume).

## Data and events

- **Owns:** Redis (offer store, one hash per fixture with an `offerVersion`).
- **Events:** Produces `offer.fixture-changed`, `offer.price-changed`, `offer.result-published`.

## Layout

Clean Architecture, enforced by project references and `*.ArchitectureTests`:

```
src/*.Domain          pure domain, no references
src/*.Application     use cases and ports; depends on Domain and contracts only
src/*.Infrastructure  adapters (Dapper + embedded .sql, Kafka, Redis); implements Application ports
src/*.Api | *.Worker  composition root: observability, error envelope, health, metrics
src/*.Migrator        DbUp scripts under Migrations/, run once before the host starts
```

Every host exposes `/health/live`, `/health/ready` (checks its real dependencies), `/metrics` (Prometheus), logs compact JSON with correlation ids, and exports traces over OTLP.

## Build and test

```bash
../swiftbets-platform/scripts/fetch-shared-packages.sh .   # or pack-local.sh for unreleased shared changes
dotnet test SwiftBets.Offer.slnx
```

Integration tests use Testcontainers and need Docker. The whole platform runs from `swiftbets-platform` with `make up`.

## Images

Multi-arch (amd64 + arm64), non-root, chiseled runtime:

- `ghcr.io/remonenaidoo/swiftbets-offer`

## License

MIT
