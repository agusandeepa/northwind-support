# Northwind Support Desk

A support ticket system: raise tickets, search and filter them, assign them to agents, escalate them
and move them through their lifecycle. .NET 10 Web API, React 19 + TypeScript, SQL Server, one repo.

The assignment is in [`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md). How I approached each task
is in [`APPROACH.md`](APPROACH.md).

## Where things stand

Tasks 1 to 4 are done. I did not build the optional SLA summary report (2.5). The time spent on each
task is in the note that goes with my submission.

## Running it

You need the .NET 10 SDK, Node 22 and Docker Desktop.

```bash
cp .env.example .env     # optional, everything has a working default
npm install
npm run db:up            # SQL Server on localhost:1433
npm run api              # applies migrations, seeds, serves http://localhost:5043
npm run web              # http://localhost:5173
```

The web app proxies `/api` to the API. By default it points at `https://localhost:7043`. If you see
"Bad Gateway", put `VITE_API_PROXY_TARGET=http://localhost:5043` in `apps/web/.env.local` and restart
`npm run web`.

Development login (a local fixture, only seeded in Development):

- `alex.turner@northwind-support.example` / `LocalDevOnly!1`
- The same password works for `ben.osei@`, `priya.nair@`, `sara.lindqvist@` and `marcus.doyle@` at
  `northwind-support.example`. Yuki Tanaka is inactive, so she can't sign in.

To run the API in Docker instead:

```bash
cp .env.example .env     # then set MSSQL_SA_PASSWORD and Jwt__Key to your own values
docker compose up --build
VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
```

## What I built and how I read the rules

Task 1: I fixed DEFECT-117 and moved all the list filters into SQL, so the count and paging are
right for the filtered set.

Task 2: when a ticket is created the system picks the priority, due date and owner and returns a
`triage` object saying why. `POST /api/tickets/{id}/escalate` raises the priority one level, works
out a new due date from the moment of escalation, keeps the agent if they can still take the ticket
(otherwise picks another) and writes a history row. `GET /api/tickets/{id}/escalations` lists the
history, newest first. SLA status is worked out when a ticket is read, and `slaStatus` filters on it.

Task 3: agents sign in and get a JWT. Everything is closed except login, Swagger and `/health`. The
agent who escalates comes from the token.

Task 4: a multi-stage Dockerfile, an `api` service in compose and a `/health` endpoint.

Places where the spec left room, and what I decided:

- The open-ticket count for BR-5 ignores Resolved and Closed. When a ticket is re-checked on
  escalation, its own agent's count doesn't include that ticket, otherwise someone at 6/6 would
  always lose a ticket they already have.
- Escalation recalculates the due date from "now", so I also reset the SLA start to that moment.
  Without this the at-risk check would measure the new due date against the old start.
- Ties between agents go to the lower open count, then the lower id, so the result is always the same.
- The base windows, the premium multiplier (0.5), the 1 hour floor and the 25% at-risk threshold are
  in the `Sla` config section. Categories are matched by their flags, never by name.

## Design decisions

The thing I cared about most was keeping each rule in one small place that doesn't touch the database
or the clock, so create and escalate share it and it can be tested alone.

- Priority from the category: `Category.ResolvePriority`
- Due date: `SlaCalculator`, which reads its numbers from the `Sla` options
- Can this agent take this ticket: `Agent.CanTake`
- Which agent: `AgentSelector` (`Choose`, and `KeepOrChoose` for escalation)
- Escalation: `Ticket.PriorityAfterEscalation` and `Ticket.ApplyEscalation`
- SLA status: `SlaEvaluator`, which gets "now" passed in

The command handlers (`RaiseTicketCommandHandler`, `EscalateTicketCommandHandler`) only load data, call
these and save. I thought about putting the rules in the handlers, but then create and escalate would
each have their own copy and I could only test them with a database. I also thought about a generic
rules engine and dropped it, because there is only one set of rules.

Patterns I used, and why:

1. Small policy classes (`AgentSelector`, `SlaCalculator`). Choosing an agent and doing the due-date
   maths are things that could change, so the handlers depend on them rather than doing it inline.
2. Composable query steps in `TicketQueries`. Each filter is one step that narrows the query. When
   `slaStatus` came along I added one step and left the others alone (FL-6).
3. Repositories only where the handlers really need to load and save (agents, tickets, escalations).
   I didn't wrap everything else in an interface.

SLA status is not stored. It depends on the current time, so a stored value would be wrong a moment
later. I store the due date and the SLA start, and work the status out when reading.

The SLA filter is a SQL predicate (it uses `EF.Functions.DateDiffSecond`), so filtering, sorting,
paging and the count all happen in the database. SQLite can't translate that function, so my unit
tests can't run it. It only runs against SQL Server (see Testing).

For credentials I added a nullable `PasswordHash` to `Agents` rather than a separate users table.
Every login is an agent and there are no roles, so a second table would only add a join. If roles
or non-agent users show up, that should be split out.

## The defect (DEFECT-117)

Root cause: in `useTicketList`, the effect that fetches the list only depended on `filters.search`,
`filters.page` and `filters.pageSize`. Changing the status (or priority, category and so on) did not
re-run the effect, so no request went out. The eslint rule that would have flagged this was switched
off with a comment. The next time the search text changed, the effect did run and sent whatever the
other filters happened to be at that moment, which is why the old status seemed to come back.

Fix: the effect now depends on the real filter values, so any single filter change sends a request,
and only the search text is debounced so typing doesn't send one request per keystroke. The
regression test is in `useTicketList.test.ts`. It checks the request that gets sent when only the
status changes, and it fails on the original code.

To see it for yourself, change the Status dropdown with the Network tab open. One request should go
out straight away. Typing in search shouldn't send a request per keystroke.

## Filtering

The filters are applied in `TicketQueries` before the count and before `Skip/Take`, so `totalCount`
and `totalPages` match the filtered list. Filters combine with AND, a blank search is ignored, and
search looks at title, reference and customer name. The backend tests run the queries against a
real (SQLite) database, so the count and the page both come from SQL and not from filtering in C#.
To add another filter, add one step to the chain in `TicketQueries` and one property on `TicketQuery`.

## Authentication

- Credentials: a nullable `PasswordHash` on `Agents`, added by the `AddAgentPasswordHash` migration.
  The dev password is hashed when the data is seeded.
- Hashing: PBKDF2 with a random salt per password, behind `IPasswordHasher`. Nothing is stored in
  plain text.
- Login: `POST /api/auth/login` returns `{ token, expiresAtUtc, user }`. A wrong email, a wrong
  password and an inactive agent all give the same 401 and the same message. `GET /api/auth/me` looks
  the agent up in the database, so a deactivated agent is refused there.
- Token: a JWT signed with HS256, valid for 8 hours, no refresh token. The key comes from `Jwt__Key`
  (listed in `.env.example`). In Development, if it isn't set, a temporary key is made for that run, so
  everyone has to sign in again after a restart. A real deployment has to set one.
- Access control: a fallback policy requires a signed-in user on every endpoint. Login, Swagger and
  `/health` are the only exceptions. `escalatedBy` comes from the token.
- Frontend: the session is kept in memory and copied to `localStorage` under `northwind.session`.
  `api/client.ts` adds the `Authorization` header to every call. The downside of `localStorage` is
  that any XSS bug could read the token. An HttpOnly cookie avoids that but needs CSRF protection
  and the API and web app on the same site. A 401 on a signed-in session clears it and sends you to
  `/login`, and after signing in you go back to the page you wanted.
- For production I'd want a real key from a secret store, shorter tokens with refresh, HTTPS,
  rate limiting on login and a decision on cookies versus storage.

## Docker

```bash
docker compose down -v && docker compose up --build
```

Variables: `ASPNETCORE_ENVIRONMENT` (default Development), `MSSQL_SA_PASSWORD` (a local default,
change it) and `Jwt__Key` (empty means a temporary key).

The Dockerfile is `apps/api/Dockerfile`, built from the repo root. It copies the project files and
restores first, so the restore layer is cached, then publishes. The final image is the ASP.NET
runtime, runs as a non-root user on port 8080, has no certificates and no config inside. The
connection string, key and environment name are passed in at run time. Compose publishes the API on
`localhost:5080` and starts it only when SQL Server is healthy. `GET /health` checks the database and
needs no token.

Migrations: on an empty volume the API migrates and seeds on start-up, which is fine for a demo. In a
real deployment I'd run migrations as a separate step before the new version starts. If several
instances start together they would all try to migrate, the app shouldn't need schema-changing rights
while running, and a failed migration should stop the release instead of leaving a crash-looping
container.

How to check it:

- `docker compose down -v && docker compose up --build`, then open `http://localhost:5080/health`.
  It should report healthy. Sign in and call `/api/tickets` to see the seeded data.
- Change one `.cs` file and run `docker compose build api` again. The restore step shows as cached
  and only the build and publish steps run.
- `docker images` shows the runtime image next to the SDK image. The runtime one is much smaller,
  because it has no SDK or build tools in it.
- `docker compose exec api whoami` prints the non-root user, not `root`.

## Database

- `Agents.PasswordHash`, nullable, from the `AddAgentPasswordHash` migration.
- `TicketEscalations` (Id, TicketId, From/To Priority, From/To AgentId, From/To DueAtUtc, Reason up
  to 500, EscalatedBy up to 100, EscalatedAtUtc), from its own migration. I added an index on
  `TicketId`, since history is only ever read as "the rows for one ticket, newest first".
- Reads use `AsNoTracking` and select only the columns they need.

## Testing

`npm test` runs the .NET tests and then the web tests.

Backend (xUnit, Moq, a fixed clock): SLA windows per priority and tier including the 1 hour floor,
categories that force Critical, agent selection (fewest open, specialists, limits, inactive, ties,
nobody eligible), escalation (one step up, new due date, owner kept or changed, history row,
rejections), SLA status at the exact boundaries, wrong password giving 401 and no token, an inactive
agent being refused, password hashing, and a protected endpoint returning 401 without a token and
200 with one.

Frontend (Vitest, Testing Library): a short reason blocks submit and never calls the API, a 409 is
shown and the ticket stays as it was, the SLA indicator shows every state in a way you can tell
apart, an unauthenticated visitor is redirected to login and sent back afterwards, a 401 clears the
session, and the DEFECT-117 regression test.

What I didn't test automatically: the SLA filter against a real SQL Server, because the test database
is SQLite and can't run `DateDiffSecond`. To try it, start the SQL Server container and filter by
each SLA status in the app. I also didn't test Docker itself or the visual styling.


## AI Usage

I used Claude as an AI assistant during this assessment to understand errors, troubleshoot build and test failures, and get help fixing issues in the code. I also used it to suggest code changes when I was unsure how to resolve a problem.

I reviewed the suggested changes, applied the relevant fixes in my editor, and tested them to check that they worked as expected. I used `dotnet build`, `dotnet test`, frontend checks, API requests, and manual browser testing to verify the implementation.

AI was helpful in identifying possible causes of errors and suggesting solutions, but I also encountered suggestions that needed adjustments to fit the existing codebase. I corrected these issues and verified the final changes through testing.

## Known limitations

- No refresh tokens. A token works until it expires (8 hours) and can't be revoked. Only `/me`
  checks that the agent is still active.
- No lockout or rate limiting on login.
- One shared HS256 key, and the token is in `localStorage`.
- No roles. Any signed-in agent can work on any ticket.
- The SLA filter isn't covered by the automated tests, because SQLite can't run it.
- Migrations run at start-up in the demo. In production they should be a separate step.
- The container speaks plain HTTP on 8080. TLS belongs in front of it.
- The SLA summary report (2.5) isn't built.
- Left out as out of scope: registration, password reset, SSO, MFA, notifications, caching.
- With 10 million tickets the ticket search (`LIKE` on title, reference and customer name) and the
  SLA predicate would be too slow, because neither can use an index as written. I'd move search to
  full-text search and store a computed due-date or SLA column that can be indexed.

## Commands

- `npm run db:up` / `db:down` / `db:reset`: start, stop or wipe SQL Server
- `npm run api` / `npm run web`: run the API or the web app
- `npm run build`: build both
- `npm run lint` / `typecheck` / `format`: web lint and types, `dotnet format`
- `npm run ef -- migrations add <Name>`: add an EF Core migration

## API

- `POST /api/auth/login`, `GET /api/auth/me`
- `GET /api/tickets` with `search`, `status`, `priority`, `categoryId`, `customerId`,
  `assignedAgentId`, `unassignedOnly`, `slaStatus`, `sortBy`, `sortDirection`
- `GET /api/tickets/{id}`, `POST /api/tickets` (returns the ticket and its `triage`)
- `POST /api/tickets/{id}/escalate`, `GET /api/tickets/{id}/escalations`
- `PATCH /api/tickets/{id}/status`, `PATCH /api/tickets/{id}/assignment`
- `GET /api/customers`, `/api/agents`, `/api/categories`
- `GET /health`

Errors come back as ProblemDetails: 400 for validation, 401 when not signed in or credentials are
wrong, 404 not found, 409 for conflicts and rule violations (like escalating a Critical ticket).
