# SQLite database

| Path | Contents |
| --- | --- |
| `Scripts/01_schema.sql` | Tables, CHECK constraints, indexes and triggers (hard-delete guards, audit trail) |
| `Scripts/02_views.sql` | Views used by the API and reports; they exclude soft-deleted providers and compute effective license status |
| `Scripts/03_seed.sql` | Sample data covering every required scenario (expiration dates are relative to the build date) |
| `Scripts/04_support_queries.sql` | Ad-hoc queries for support staff (not run by the app) |
| `Data/providers.db` | The database built from scripts 01–03 |

The API creates `Data/providers.db` from scripts 01–03 when the file does not exist. To rebuild it, stop the API, delete the file and start the API again. You can also run the scripts yourself in order with the `sqlite3` CLI:

```bash
sqlite3 Data/providers.db < Scripts/01_schema.sql
sqlite3 Data/providers.db < Scripts/02_views.sql
sqlite3 Data/providers.db < Scripts/03_seed.sql
```

Commit the `.db` file only after the API has stopped, so there are no `-wal`, `-shm` or `-journal` files (Git ignores those anyway).

Design decisions, including the soft-delete approach, are documented in the root [README](../../../README.md).
