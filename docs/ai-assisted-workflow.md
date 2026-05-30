# AI-Assisted Development — How This Was Built

Two distinct uses of AI in this project. Both are tasteful and shippable —
not "I copy-pasted from ChatGPT" but a genuine AI-in-the-loop workflow.

## 1. AI inside the running app — `POST /api/v1/ai/infer-schema`

A real onboarding pain in multi-tenant ERP: every new tenant sends their data
in a slightly different shape. Manually mapping their CSV columns
(`CardNo, TimeStamp, DEV, InOut`) onto our canonical schema
(`external_ref, punch_at, device_id, direction`) takes engineering hours per tenant.

This endpoint flips it: paste a CSV sample, get back the mapping + a confidence
score. The implementation lives in
[`Infrastructure/AiAssist/ClaudeSchemaInferenceService.cs`](../src/Attendance.Infrastructure/AiAssist/ClaudeSchemaInferenceService.cs).
It's a single round-trip to Claude Haiku 4.5 with a tight system prompt and
strict JSON output schema.

### Why Claude Haiku 4.5 specifically
- Tiny task (column-name reasoning) → no need for Sonnet/Opus
- Fast (sub-second usually) and cheap
- Reliable structured JSON output

### Failure mode handling
- API key missing → `503 Service Unavailable` with a clear message (not a 500)
- Claude returns bad JSON → throws, caller gets `500` with traceId
- Confidence < 0.7 returned by Claude → the endpoint still returns; the CALLER should decide whether to use it or escalate to a human

## 2. AI assisting the development of this code — `prompts/`

The `prompts/` folder holds the actual Claude prompts I used to scaffold parts
of this repository. They're checked in so a reviewer can see:

- How I structure tasks for an AI assistant (specific, constrained, with examples)
- Which sub-tasks I delegate to AI vs. write myself
- The before/after diff between AI output and what landed in the repo

This is increasingly the way modern engineers actually work — committing the
prompts alongside the code makes the workflow legible and auditable.

### What I delegate to AI

| Task | Why AI is good at it |
|------|----------------------|
| EF Core entity configuration boilerplate | Mechanical, well-defined schemas |
| Validation rules from a textual spec | Structured transformation |
| DTO + record types from a domain entity | Mechanical mapping |
| Test scaffolding (xUnit theory rows) | Pattern repetition |

### What I do myself (don't delegate)

| Task | Why I do this myself |
|------|----------------------|
| Architecture decisions (DB-per-tenant vs shared) | Trade-offs require judgment |
| Naming conventions | Project-wide consistency |
| Error handling strategy | Critical for production |
| Performance-sensitive code | AI tends to over-generalize |
| Security boundaries | Not delegating this — period |

### The interview talking point

> "I treat AI like a fast junior pair: it's great at mechanical work I supervise,
> bad at architectural choices, and I always read the diff before committing.
> The `prompts/` folder in my showcase shows exactly how I use it."

That sentence shows you understand both the upside AND the failure modes of
AI-assisted development. Most candidates only show one side.
