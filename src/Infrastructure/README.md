# Infrastructure conventions

Each bounded context owns a separate EF Core `DbContext` and schema. Entity mappings
are explicit `IEntityTypeConfiguration` classes; contexts do not discover mappings
from unrelated bounded contexts.

Database changes are additive-only: add tables, columns, indexes, or nullable/online
backfills. Do not rename or drop clinical, audit, or patient data in a normal release;
use a staged deprecation and a separately reviewed destructive migration when needed.
Concurrency guarantees that affect correctness (for example, the `Turns` unique slot
index and rowversion) remain in the database rather than relying only on application
locks.
