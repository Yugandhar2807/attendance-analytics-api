# `prompts/` — AI-assisted scaffolding prompts used to build this project

These are the actual Claude prompts I used to generate parts of this codebase.
They're committed for transparency — a reviewer can see how I structure tasks
for an AI assistant and which parts I delegated vs. wrote myself.

## Why commit prompts?

For the same reason we commit migrations alongside schema: **the *process* is
part of the artifact**. If a future contributor wonders "why is this code shaped
this way?", a prompt that explains the constraints can be as valuable as a code
comment.

## How I use these (workflow)

1. Write the prompt as if explaining the task to a careful junior
2. Run it (Claude / Code in IDE / API)
3. **Read the diff before staging** — AI is fast but not infallible
4. Adjust the prompt and re-run if the output drifts from project conventions
5. Commit prompt + code in the same PR

## What's in here

- [`design-tenant-middleware.md`](design-tenant-middleware.md) — designing the
  per-request tenant resolution flow
- [`design-ingestion-strategy.md`](design-ingestion-strategy.md) — the strategy
  pattern for 3 ingestion modes converging on one service

## What I do NOT do

- Paste in proprietary code / employer code / customer data
- Generate without reading the output
- Commit without testing
- Use AI for security-critical decisions
- Use AI for architecture decisions that require domain judgment

> AI is a pairing partner, not an oracle. The repo owner is still on the hook for what ships.
