# Podman Deployment Notes

## Local full-stack run

From the repo root:

```powershell
cd d:\Sandesh\Projects\Personal_Fincance_Tracker
podman compose up --build
```

Services exposed by `compose.yaml`:
- frontend: `http://localhost:8080`
- backend: `http://localhost:8081`
- postgres: `localhost:5432`

## Environment setup

### Backend local `.env`
Fill or verify these values in [backend/.env](/d:/Sandesh/Projects/Personal_Fincance_Tracker/backend/.env):

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Frontend__AllowedOrigins__0`
- `Email__Enabled`
- `Email__FromAddress`
- `Email__FromName`
- `Email__SmtpHost`
- `Email__Port`
- `Email__Username`
- `Email__Password`
- `Email__UseSsl`

Reference example: [backend/.env.example](/d:/Sandesh/Projects/Personal_Fincance_Tracker/backend/.env.example)

## Migration flow

Container startup does not auto-run EF Core migrations.
Apply them explicitly before first real use or after schema changes:

```powershell
cd d:\Sandesh\Projects\Personal_Fincance_Tracker\backend
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=finance_tracker;Username=postgres;Password=1234"
dotnet ef database update --project src/FinanceTracker.Infrastructure --startup-project src/FinanceTracker.Api
```

## Safe production-minded sequence

1. Set real environment values.
2. Confirm PostgreSQL is reachable.
3. Run EF migrations explicitly.
4. Start backend.
5. Start frontend.
6. Verify `/health` and sign-in flow.
7. Verify recurring automation is enabled only with intended polling settings.

## Useful commands

### Stop stack

```powershell
podman compose down
```

### Rebuild stack after code changes

```powershell
podman compose up --build
```

### Backend only local run

```powershell
cd d:\Sandesh\Projects\Personal_Fincance_Tracker\backend
dotnet run --project src/FinanceTracker.Api
```

### Frontend only local run

```powershell
cd d:\Sandesh\Projects\Personal_Fincance_Tracker\frontend
npm run dev
```

## Notes

- Forgot-password email delivery requires valid SMTP settings.
- Background recurring automation uses the `Automation` config section.
- For local development, explicit host-based runs are still the fastest debugging path.
