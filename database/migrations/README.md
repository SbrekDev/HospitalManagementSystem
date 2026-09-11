# Database migrations

EF Core migrations are owned by `SanatorioHMS.Infrastructure.Data/Migrations`.
The `database/migrations` directory is the deployment-facing landing zone for
generated SQL scripts and release notes. Migrations are additive-only: never
rewrite an applied migration; add a new migration for every schema change.

Generate an idempotent deployment script with:

```bash
dotnet ef migrations script --idempotent \
  --project src/Infrastructure/SanatorioHMS.Infrastructure.Data \
  --startup-project src/Api/SanatorioHMS.Api
```
