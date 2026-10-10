# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database server. The ticket query tests run
against in-memory SQLite.

## DEFECT-117: filters not taking effect

**Root cause.** The `useEffect` in `useTicketList` only had `search`, `page` and `pageSize` in its
dependency array. So when I changed status (or priority, category, customer, agent, unassigned,
sort) the effect never ran again and no request was sent. It only caught up on the next search
keystroke, which is why the status looked stale.

**Fix.** The request now depends on the whole filter object, so any filter change sends a request
straight away. Search is handled separately: what the user types is kept in its own state and only
copied into the filters 300 ms after they stop typing, and the page goes back to 1 at that point.
I also switched the fetching to the existing `useAsyncData` hook instead of the manual
loading/error code, and removed the `eslint-disable` comment that was hiding the problem.

**How to check.** Run `npm run test --workspace apps/web`. The tests in `useTicketList.test.ts`
check the arguments of the request that is sent. On the original hook 8 of them failed (status,
priority, category, customer, agent, unassigned only, sort and reset), and they all pass now.
Manually: open the Network tab, change Status and a request goes out immediately, then type in the
search box and only one request goes out after you stop typing.

## Filtering (Task 1.2)

**Where.** The filters are applied in `TicketQueries.ApplyFilters`
(`SupportDesk.Infrastructure/Queries`). Each filter is one `WhereIf(condition, predicate)` line, and
they are chained, so a ticket has to match all the filters that were supplied (AND). The result of
that is what gets counted, sorted and paged, so `totalCount` and `totalPages` are for the filtered
set and not for all tickets. Nothing is filtered in C#. Trimming the search text and treating a blank
search as "no search" is done in one place, `TicketQuery.Normalized()`.

**Rules as I understood them.** Status, priority, category, customer and agent are exact matches, so
priority `High` does not also return `Critical`. Search is a "contains" match on the ticket title, the
reference or the customer name. `unassignedOnly=true` together with `assignedAgentId` is an AND, so it
returns nothing.

**How I checked the SQL.** Development mode logs every EF Core command. I ran the API, changed the
filters in the UI and read the log. The `COUNT(*)` statement and the page statement both have the same
`WHERE`, and it comes before `OFFSET/FETCH`. `TicketQueriesFilterTests` also runs the same LINQ as
real SQL on in-memory SQLite.

**Adding the next filter.** For `slaStatus` in Task 2, add the property to `TicketQuery` and one
`WhereIf` line to `ApplyFilters`. Counting, sorting and paging stay as they are.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. Every filter is applied in the database and they combine with AND. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
