# Court Finance (Angular)

Shell for the Court Finance reference app. Development server listens on **http://localhost:4300** and proxies `/api` to the .NET API on **http://localhost:5244**.

```bash
npm install
npm start
```

Ensure SQL Server has the `CourtFinance` database from `database/CourtFinance.sql` and the API connection string matches your environment.
