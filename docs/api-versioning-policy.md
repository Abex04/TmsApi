# TMS API Versioning Policy

## What counts as a breaking change
A breaking change is any modification that forces existing clients to update their code:
- Removing a field from a response
- Renaming a field in a response
- Changing a status code (e.g. 200 → 204, or 409 → 400)
- Tightening validation (e.g. adding a required field, shrinking a max length)
- Changing the default sort order of a collection

Any of these requires a new version.

## What counts as non-breaking (additive)
These changes are safe to ship without a new version:
- Adding a new optional field to a response
- Adding a new endpoint
- Adding a new optional query parameter
- Relaxing validation (e.g. increasing a max length)

## Sunset window
The TMS commits to a minimum 6-month sunset window after a new version ships.
This gives rural training centres on quarterly maintenance schedules enough time
to migrate. V1 sunset date: 31 December 2026.

## Communication plan
From day one of V2:
- Every V1 response carries three headers: Deprecation: true, Sunset: <date>,
  Link: <V2 URL>; rel="successor-version"
- A CHANGELOG entry is added describing what changed and why
- An email is sent to every team holding an API key
- A calendar invite is sent for the V1 shutdown date

## Skipping versions
Clients are not required to migrate through every version sequentially.
A client on V1 may migrate directly to V3 when it ships, skipping V2 entirely.
