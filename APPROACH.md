# How I approached it

## Task 1: the defect, then filtering

I started with the defect. The report described an action that did nothing, followed by a later
action that used old data. That looked like state being read from the wrong place, not a problem in
the API. So I reproduced it in the browser with the Network tab open and followed where the filter
values are read when the request gets built.

The effect that fetches the list only listed the search text, the page and the page size as its
dependencies, so a change to the status never re-ran it. The lint rule that would have warned about
it had been switched off with a comment. I fixed that cause. I didn't add a forced reload or a
remount key, because that would only hide the problem. The regression test checks the request that is
sent, and I ran it against the old code first to see it fail.

The API already accepted the filters and then ignored them. I applied them as separate steps on the
query, all in one place, before the count and the paging. That also meant the SLA filter in Task 2
was one more step and not a rewrite. The defect fix and the filtering are separate commits.

## Task 2: escalation and assignment

The rules themselves are simple. What can go wrong is writing them twice, once for create and once
for escalate, or burying them inside a handler. Before writing any code I went through BR-1 to BR-8
and decided which small class owns each one. The rule I set myself was that none of them touch the
database or the real clock.

The order I worked in: domain rules first, then the calculator and the agent selector, then the SLA
evaluator with the clock passed in, then the handlers, endpoints, migration and SQL filter, and the
UI last. Each piece had its tests before I wired it in, so the handlers stayed short.

A few calls I had to make:

- An agent's own ticket shouldn't count against their limit when it's re-checked for escalation.
  Otherwise an agent who is at their limit would always lose a ticket they already hold.
- Escalation moves the due date, so the at-risk calculation needs a new starting point. I store the
  SLA start and work the status out when the ticket is read. A stored status would be out of date a
  minute later.
- The SLA filter has to run in SQL, so I wrote it as a predicate. SQLite can't run it, so the
  automated tests don't cover it and it has to be tried against SQL Server.
- Having no eligible agent is a normal result. The ticket is created unassigned and the triage says
  why.

On the frontend there is one SLA indicator, used by both the list and the detail page, and it uses
text as well as colour. The escalation form checks the same 5 to 500 character rule as the server,
so a bad reason never reaches the API. It shows a pending state and server errors, including the 409,
and refreshes the ticket and its history without reloading the page.

## Task 3: authentication

Every login is an agent and there are no roles, so I added a nullable `PasswordHash` to `Agents`
instead of making a separate users table. Passwords use PBKDF2 with a salt, behind an interface.
Login gives the same error for a wrong email, a wrong password and an inactive agent, so it doesn't
tell anyone which emails exist.

I used a fallback authorization policy so that everything is closed by default. A new endpoint is
protected unless someone opts it out, which feels safer than having to remember to add an attribute.
The `escalatedBy` value now comes from the token, so nobody can claim to be someone else.

On the frontend the token sits in a small session module (memory plus `localStorage`) that
`api/client.ts` reads on every call. A 401 on a signed-in session clears it, and the route guard
sends you to login and back again. The `localStorage` trade-off is written down in the README.

One thing I got wrong: I read the login response as `agent`, but the API sends `user`, so signing in
did nothing and showed no error. Calling the endpoint directly and reading the JSON showed it
quickly. I fixed the type and the tests that used the old shape.

## Task 4: Docker

I wanted one command to give the same backend on any machine. The restore step is its own cached
layer, and the final image is the ASP.NET runtime running as a non-root user on port 8080. Nothing is
baked in. The connection string, key and environment name are set at run time, and `.env.example` is
the list of them. Compose starts the API only once SQL Server is healthy, and `/health` checks the
database. I kept migrations on start-up for the demo, and wrote down why a real deployment should
run them as a separate step.

## What I'd do next

Run the SLA filter against SQL Server in an automated integration test, move migrations to a deploy
step, and add refresh tokens and rate limiting on login.
