# TrainingApp - Project Bible

## Overview
Adaptive workout app with periodized training, autoregulation (RPE), and metabolic modeling.

## Architecture

```
src/
├── TrainingApp.Api          # REST API, controllers, middleware
├── TrainingApp.Core         # Domain entities, interfaces, DTOs
├── TrainingApp.Infrastructure # EF Core, external APIs (wger)
└── TrainingApp.Orchestration  # Background jobs, workout generation
```

## Key Patterns

- **Clean Architecture**: Core has no external dependencies
- **Repository Pattern**: Via EF Core DbContext
- **Autoregulation**: RPE-based load adjustment
- **e1RM Calculation**: Brzycki formula in WorkoutSet

## Tech Stack

- .NET 10, C# 13
- PostgreSQL (via docker-compose)
- EF Core 10
- wger API for exercise data

## Commands

```bash
dotnet build                    # Build all
dotnet test                     # Run tests
docker-compose up -d postgres   # Start DB
```

## External APIs

- **wger**: https://wger.de/api/v2/ (no auth for reads)

## Conventions

- File-scoped namespaces
- Nullable reference types enabled
- Warnings as errors
