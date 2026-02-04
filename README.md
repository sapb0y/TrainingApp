# TrainingApp

Adaptive workout application with periodized training, autoregulation (RPE-based), and metabolic modeling.

## Features

- Exercise library via [wger API](https://wger.de)
- RPE-based autoregulation
- e1RM tracking
- Periodized program generation

## Quick Start

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Build
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/TrainingApp.Api
```

## Project Structure

```
src/
├── TrainingApp.Api            # REST API
├── TrainingApp.Core           # Domain models
├── TrainingApp.Infrastructure # Data access
└── TrainingApp.Orchestration  # Background jobs
```

## License

MIT
