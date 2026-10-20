---
name: bootstrap-impl
description: Scaffold a new FEATURE-IMPL.md from a one-line feature description by fanning out to parallel subagents — header drafter, clarify-question generator, auto-resolver, phase decomposer, optional codebase grounding. End-to-end automatic: drafts the IMPL, resolves Open Questions with reasoned defaults (operator can override later), updates CONTEXT.md / TODO.md / LOGBOOK.md, and commits. No mid-flow prompts. Chaining into /run-impl is opt-in via --and-run, not the default (PROTOCOL-PORT.2, back-ported 2026-09-10). Per docs/DOCS-PROTOCOL.md Rule 12 + Rule 13 + Rule 14.
---

# bootstrap-impl

Parallel scaffolding for a new FEATURE-IMPL.md. Borrowed shape from Kiro for Claude Code's "✨ New Spec with Agents" pattern — see [docs/research/kiro.md](../../../docs/research/kiro.md) §4.1 — adapted to waremoto's single-IMPL-file convention and existing skill ecosystem.

## Git scope and commit contract

Maintain an explicit inventory of feature-owned files and a snapshot of pre-existing staged, unstaged and untracked changes. Unrelated work stays untouched by default; dirt alone never requires `--force`. A whole-file commit is allowed only when every included hunk belongs to this task. If a file mixes unrelated edits, separate ownership safely or stop with that specific ambiguity; do not commit the whole file merely because its path appears in the tracker.

Use `workflow-git.py` for read-only branch preflight, scoped commits, phase audit diffs and final completeness checks. Resolve the helper as described below. `commit --repo <root> --branch <validated-branch> --message <message-with-trailers> --paths <exact-files>` stages only those files and commits with `git commit --only ... -- <exact-files>` so unrelated pre-staged files are excluded. Use this contract for bootstrap, plan, box, phase, catch-up and promotion commits. Never use a bare commit or a directory/wildcard scope. Use the Rule 15 taxonomy (`feat`, `fix`, `polish`, `refactor`, `docs`, `test`), never a generic preflight `chore` sweep. Already committed work needs no empty commit.

Run `preflight --repo <root> --branch <validated-branch>` before any commit; writer locks, armed targets and human decisions remain the skill's responsibility. `--no-commit` means no commits in any step, including bootstrap, planning and preflight. Use the helper's `--no-commit` preview when inspecting a proposed commit. A preview can never report Shipped or promoted.

Resolve `<workflow-git>` to `D:/ware/GhostDev/template/bin/workflow-git.py` and invoke it with Python. Always pass the target repository explicitly with `--repo`; do not change the working root to the helper's repository.

## When this fires

- Operator runs `/bootstrap-impl <feature description>` (free-form description, e.g. `/bootstrap-impl Example.Morning daily briefing pipeline`).
- Optional flag: `/bootstrap-impl --ground <feature description>` adds the codebase grounding subagent.
- Optional flag: `/bootstrap-impl --root <path> <feature description>` writes under a non-default IMPL root (e.g. `service/docs/`, `web/docs/`, `D:/projects/game/docs/`). Default: `docs/` at workspace root.
- Optional flag: `/bootstrap-impl --no-commit <feature description>` skips the final commit (write + index updates only).
- Optional flag: `/bootstrap-impl --no-resolve <feature description>` leaves Open Questions un-resolved for the operator (legacy behavior).
- **Default (as of PROTOCOL-PORT.2, back-ported here 2026-09-10): stops after the scaffolding commit (Step 9) and does NOT chain into execution.** `--scaffold-only` is now the implicit default, not an opt-out — see the corrected rationale below.
- Optional flag: `/bootstrap-impl --and-run <feature description>` chains into `/run-impl` after the scaffolding commit and drives the tracker under Rule 18. **Opt-in on purpose.**
- Optional flag: `/bootstrap-impl --and-run --no-promote <feature description>` chains into execution but stops at the promotion gate (passed through to `/run-impl`).
- **Do NOT auto-fire.** New IMPL creation is an operator decision; this skill executes the scaffolding once that decision is made.

## Operating principle — automatic by default

This skill runs end-to-end without mid-flow prompts. Brute-force the work with Opus-class subagents in parallel and stitch the result. Where ambiguity exists, pick a default with explicit rationale (Rule 13 allows defaults) and surface it in the IMPL so the operator can override during normal review. **Never** end with "next, run /clarify-impl …" — fold that step inline. The operator's review surface is the resulting commit + diff, not a chain of follow-up commands.

**Corrected 2026-09-10 (PROTOCOL-PORT.2, back-ported):** this skill does **NOT** chain into `/run-impl` by default. It stops after the scaffolding commit (Step 9), full stop. The previous text here said the opposite — that stopping short of a promoted feature "needs a Rule 18 stop-set reason" — and that was the bug. Rule 18 governs a run **already in progress**; it does not follow that *starting* one should be implicit. `/bootstrap-impl` takes a one-line feature description and, chained, would turn it into committed and promoted code with no further human decision — that decision should be typed deliberately via `--and-run`, not defaulted into. `/run-impl` is already that deliberate act; bootstrap is not. Auto-resolved Open Questions are reviewable in the scaffolding commit's diff, same as before.

## Procedure

This skill is the **inverse** of `audit-impl`: instead of one subagent reading a finished IMPL, multiple subagents *write* sections of an IMPL in parallel and the main session stitches them together.

### Step 1 — Resolve the feature name

From the description, derive a SCREAMING-KEBAB feature name suitable for `{FEATURE}-IMPL.md`. Examples:

- "Example.Morning daily briefing pipeline" → `MORNING`
- "rare-suite Bybit funding-rate ingest" → `BYBIT-FUNDING-INGEST`
- "Represent POC #3 — call-graph overlay" → `REPRESENT-CALLGRAPH`

If ambiguous (multiple plausible names), **pick the most specific one** and note the alternative in the IMPL's `## Open Questions` block as a resolvable question. Do not stop to ask.

### Step 2 — Verify nothing collides

Glob `**/*-IMPL.md` (exclude `node_modules/`, `bin/`, `obj/`, `.git/`, `**/impl/`). If a file matching the resolved name exists:

- If active (under `docs/` or `*/docs/` root): **abort** with `IMPL already exists at <path> — did you mean /clarify-impl or /audit-impl?`
- If archived (under `**/impl/`): warn but proceed — the operator may be re-opening a closed feature with a fresh tracker.

### Step 3 — Read steering context

Read once into the main session, **before** dispatching subagents:

- `CLAUDE.md` (workspace root) — for waremoto-wide conventions
- `docs/CONTEXT.md` — for the active sprint and milestone table
- The CLAUDE.md nearest the IMPL root (e.g. `service/CLAUDE.md` if `--root service/docs/`)
- Any `docs/design/*.md` whose filename keyword-matches the feature description
- **Prior-art sweep (DOCS-PROTOCOL Rule 17).** Read `docs/CAPABILITIES.md` (if present) and glob `docs/reference/` + `docs/impl/` filenames for anything the feature's keywords touch. The point is to catch a capability that **already shipped** before the IMPL asserts it's missing or first — the dominant design-blindness failure mode (see [docs/research/ghostdev-design-blindness.md](../../../docs/research/ghostdev-design-blindness.md)). Carry the result into the brief so subagent A can emit the `> Prior-art sweep: <date>` header line and subagent B can target any negative claim that survives it.

This context goes in each subagent's prompt as a brief — they don't re-read it.

### Step 4 — Fan out subagents (parallel, single message)

Dispatch in **one message with multiple `Agent` tool blocks** so they run concurrently. **Pass `model: "opus"` on every Agent call** — bootstrap is a one-shot up-front investment; brute-force the smartest model available. Sonnet/Haiku are not appropriate here.

#### Subagent A — Header drafter (`subagent_type: Plan`, `model: "opus"`)

```
Draft the header sections of a FEATURE-IMPL.md per docs/DOCS-PROTOCOL.md Rule 12.

Feature: <description>
Resolved name: <FEATURE>
Steering context (verbatim from main session):
<CLAUDE.md excerpts + relevant CONTEXT.md milestone row + any design/ doc matches>

Produce these sections, in order, ≤500 words total:
- Title block (`# <Feature> — Implementation Tracker` + the standard 4-line preamble). If the brief carries a prior-art sweep, add a `> Prior-art sweep: <date> — checked CAPABILITIES.md + reference/ + impl/` header line (DOCS-PROTOCOL Rule 17).
- ## AI Continuation Prompt (≤120 words — what files a fresh session must read first)
- ## Architecture (key design decisions; cite reference/ docs by path; include one mermaid diagram only if the architecture is non-trivial)
- ## Current State ({today's date}) — write "Plan-only — no code yet."
- ## Known Bugs — empty list with "_(none yet)_"
- ## What Needs Testing — placeholder list keyed off the phases (subagent C will name them)

Do NOT invent files that don't exist. If a path is uncertain, write `<TBD>` so the operator notices.
```

#### Subagent B — Clarify question generator (`subagent_type: general-purpose`, `model: "opus"`)

```
Generate ≤3 Spec Kit-style clarifying questions for this feature, per docs/DOCS-PROTOCOL.md Rule 13.

Feature: <description>
Steering context: <same brief as A>

Target ambiguity in:
- Scope (what's IN vs OUT of this IMPL)
- Decision gaps (an asserted choice with no named alternative)
- Source-audit gaps (a cited doc/file that may not exist — use Glob to verify)
- Test gates (claimed observable behavior that isn't reachable from the IMPL's surface)
- Dependency assumptions (this feature presumes another shipped behavior)

Output the exact markdown block per Rule 13:

## Open Questions

> Spec Kit-style clarify step. ≤3 items. Mark resolved or accept default with rationale before P1 starts.

1. **Q:** {question}
   **Resolution:** ___
2. ...

If the feature is unambiguous, output: "## Open Questions\n\n_No clarifications needed — feature surface is unambiguous._"
```

#### Subagent C — Phase decomposer (`subagent_type: Plan`, `model: "opus"`)

```
Decompose this feature into 3–6 phases, each one mergeable as a single PR.

Feature: <description>
Steering context: <same brief as A>

For each phase produce:
- Phase ID: `<FEATURE>.<N>` (e.g., MORNING.1, MORNING.2, ...)
- One-line description
- 2–6 checkbox items with sub-IDs (e.g., MORNING.1.1, MORNING.1.2)
- **Model** hint: "Sonnet" if it's wiring/execution following existing patterns; "Opus" if it requires novel architecture.
- **Effort** hint (per-phase reasoning budget — a parseable contract a `/run-impl` or autonomous thread run reads to set the agent's `--effort`/model per phase): one of `med` · `high` · `xhigh` · `ultra`. Heuristic: mechanical/wiring/scaffold → `high`; standard feature (auth, data-access, analytics) → `xhigh`; the hardest phases where a second independent pass pays (novel domain port, E2E parity, final hardening + review) → `ultra` (the "ultracode" workflow tier). Default when unsure → `high`.

Output two markdown blocks per Rule 12. The **Model + Effort columns are a parseable contract** (header-name keyed) — the methodology parser + dashboard read them, and `/run-impl` applies them per phase:

## Implementation Status

| Phase | State | Model | Effort | Description |
|---|---|---|---|---|
| <FEATURE>.1 | [ ] not started | Sonnet | high | ... |
| <FEATURE>.2 | [ ] not started | Opus | xhigh | ... |
| <FEATURE>.3 | [ ] not started | Opus | ultra | ... |
...

## Checklist

### Phase 1 — <name>
- [ ] <FEATURE>.1.1 ...
- [ ] <FEATURE>.1.2 ...

### Phase 2 — <name>
...

Phases must compose: phase N's checklist outputs become phase N+1's preconditions. P1 should always be the smallest shippable slice — bias toward something testable in <1 day.
```

#### Subagent D — Codebase grounding (ONLY if `--ground` flag passed; `subagent_type: Explore`, `model: "opus"`)

```
Read-only scan: identify existing code in this repo relevant to <feature description>.

Steering context: <same brief>

Approach:
- Grep for likely keywords (feature name tokens, domain nouns)
- Glob for files matching obvious patterns (e.g., `**/*Morning*.cs`, `**/*briefing*`)
- Skim the top 3–5 hits

Report ≤200 words:
- Existing related code (file:line cites)
- Existing reference/ docs that overlap
- Patterns already used in this area (DI registration shape, naming, test layout)
- "Reuse vs replace" candidates — what existing piece could this feature extend instead of greenfield?

Do NOT propose design — just enumerate what's already there. Output goes into the Architecture section as an "Existing surface" subsection so subagent A can reference it… but A has already started. Ground findings get *appended* by the main session in Step 5.
```

### Step 5 — Auto-resolve Open Questions (second dispatch, unless `--no-resolve`)

Once A/B/C (and D if requested) return, immediately dispatch **Subagent E** — auto-resolver. This is a second parallel burst (single message, single Agent call; can fan out further if many questions). Keep this in-flow — **do not stop and ask the operator**.

#### Subagent E — Auto-resolver (`subagent_type: general-purpose`, `model: "opus"`)

```
Resolve each Open Question for FEATURE-IMPL.md per docs/DOCS-PROTOCOL.md Rule 13. Rule 13 explicitly allows "accept default with rationale" — do exactly that.

Feature: <description>
Steering context: <same brief as A>
Phase plan (from subagent C): <C's output, verbatim>
Existing surface (if --ground): <D's output, verbatim>

Open Questions to resolve (from subagent B):
<B's output, verbatim>

For each question, output:

**Q:** <question>
**Resolution:** <chosen default in one or two sentences>
**Rationale:** <why this default — cite Rule, file path, or steering-context line>
**Override hint:** <one-line description of when the operator should flip this>

Pick the **least-surprising** default — match patterns already in the codebase, prefer the simpler scope, prefer reusing existing infrastructure over greenfield. Never pick a default that requires new infrastructure unless the feature description explicitly demands it. If a question is genuinely 50/50 and the wrong answer would be expensive to reverse, output **Resolution: DEFERRED — operator must answer** with a one-line explanation; this is the only escape hatch.
```

### Step 6 — Stitch and write

Once E returns:

1. Concatenate in this order: A (title + header) → B's question block, **with each `Resolution: ___` line replaced by E's resolution + a `> Rationale:` blockquote + a `> Override: …` blockquote** → C (Implementation Status + Checklist) → A again (Key Files Reference + Architecture Notes — empty stubs).
2. If `--ground` ran: append D's findings as a `### Existing surface` subsection inside Architecture, **before** Current State.
3. Resolve `<TBD>` markers — main session does a final read-through and fills them in from the steering context. Anything still ambiguous becomes an extra Open Question with an auto-resolution attached.
4. Write the assembled markdown to `<root>/<FEATURE>-IMPL.md` (default root: `docs/`).

### Step 7 — Update index docs (bookkeeping, Rule 14-exempt)

Rule 14 (architect mode) exempts bookkeeping per the METH.7.5 amendment in [docs/impl/METHODOLOGY-IMPL.md](../../../docs/impl/METHODOLOGY-IMPL.md). The following are bookkeeping and should be applied automatically:

1. **`<root-of-impl>/../CONTEXT.md`** — add (or update) the milestone row referencing the new IMPL. Insert under the active-sprint section with state "in-flight" and today's date.
2. **`<root-of-impl>/../TODO.md`** — append a phase-1 checklist mirror (just P1's checkboxes; full list lives in the IMPL).
3. **`<root-of-impl>/../LOGBOOK.md`** — append a four-section entry dated today (`### YYYY-MM-DD — bootstrap <FEATURE>`):
   - **Accomplished:** "Bootstrapped <FEATURE>-IMPL.md via /bootstrap-impl — N phases, M open questions auto-resolved."
   - **Decisions:** one bullet per non-trivial auto-resolution from E.
   - **Blockers:** any DEFERRED questions from E, or "none".
   - **Next:** "P1.1 — <first-checkbox-text>"
4. **README.md (if one exists at IMPL root and indexes IMPLs)** — add a one-line entry.

Use Glob to confirm CONTEXT.md / TODO.md / LOGBOOK.md exist at the chosen IMPL root before editing. If they don't, skip silently — not every sub-root has the full set, and bootstrapping is not the time to create them.

### Step 8 — Commit (unless `--no-commit`)

Per DOCS-PROTOCOL.md Rule 15, bootstrapping introduces a feature surface (the IMPL artifact). Use `feat(<feature-slug>):` — **not** `docs(...)`. The CONTEXT/TODO/LOGBOOK updates piggyback on the same commit (they're bookkeeping for the same introduction event); they don't change the commit type.

`<feature-slug>` is the resolved `<FEATURE>` name lowercased and trimmed (`MORNING` → `morning`, `BYBIT-FUNDING-INGEST` → `bybit-funding-ingest`).

Use `<workflow-git> commit --repo <target-root> --branch <validated-branch> --message <bootstrap-message-with-required-trailers> --paths <exact-tracker-and-bookkeeping-files>`. Resolve all paths from the actual files written; do not guess parent-relative doc paths. The message starts with `feat(<feature-slug>): bootstrap <FEATURE>-IMPL`.


Subsequent commits during the feature's lifecycle follow the same Rule 15 taxonomy: `polish(<feature-slug>): …` for tweaks, `fix(<feature-slug>): …` for bugfixes, `feat(<feature-slug>): …` only for genuinely new sub-surfaces, `docs(<feature-slug>): …` for IMPL-doc-only edits (checkbox flips after a phase ships, etc.).

Use the Git scope contract at commit time for exactly the files this skill touched. If the commit's pre-commit hook fails, **do not amend** — fix and recommit per CLAUDE.md guidance.

### Step 9 — Scaffolding report

Single terse message marking the scaffolding milestone (the run is not done — Step 10 follows):

```
Wrote docs/<FEATURE>-IMPL.md, updated CONTEXT/TODO/LOGBOOK, committed <sha>.
N phases · M open questions auto-resolved (K deferred for operator) · J <TBD> remaining.

Auto-resolved defaults to review:
- Q1: <one-line resolution>
- Q2: <one-line resolution>

Chaining into /run-impl <FEATURE> — driving all phases through promotion (--no-promote to stop at the gate).
```

Do **not** suggest running `/clarify-impl` — its work is folded in.

### Step 10 — Chain into execution (only if `--and-run` was passed)

**Corrected 2026-09-10 (PROTOCOL-PORT.2, back-ported): this step is opt-in, not the default.** By
default, skip this step entirely. The Step 9 report is then the final report — its last line should
read `Next phase: <FEATURE>.1.1 — <first checkbox>. Run /run-impl <FEATURE> when ready.`

If (and only if) `--and-run` was passed, invoke the `/run-impl` procedure on the just-created IMPL:

1. Re-read [`../run-impl/SKILL.md`](../run-impl/SKILL.md) and execute it inline in **default mode** (no `--one-phase`, no `--phase`) against `<FEATURE>`. Default mode drives every phase through promotion, stopping only for a Rule 18 stop-set member.
2. Pass through `--no-commit` and `--no-promote` if the operator passed them to `/bootstrap-impl`. Pass through `--root` context so run-impl's IMPL glob resolves to the right file.
3. A DEFERRED Open Question does **not** block the chain. If a phase's checklist depends on a deferred question, run-impl's own pre-flight skips that phase with a note (per its Step 2) — the chain still runs every non-blocked phase. The deferred question surfaces in the final report.
4. run-impl owns its own session lock, per-phase commits, LOGBOOK appends, and final report. bootstrap does not duplicate that bookkeeping — it just hands off.

The final operator-facing report is then run-impl's Step 6 report, not Step 9's. Step 9's message is an in-flight checkpoint.

run-impl chains into `/promote-impl` when the gates are green (Rule 19). The boundary that IS unchanged: this skill never promotes from an inference, and the unattended lane never promotes at all (Rule 24).


**Execution lane:** a person-invoked default run (including `--and-run`) is authorized through promotion even when the person leaves it running. Elapsed time, headless execution and subagent delegation do not turn it into the scheduler's unattended lane. Scheduler/AFK runs remain subject to Rule 24, including their branch, promotion and promoter-preview restrictions. `--no-commit` implies no promotion and no preflight auto-commits; report a preview, never a completed promotion.

## Constraints

- **Parallel dispatch is mandatory** when ≥2 subagents fire. Sequential subagent calls defeat the purpose. Single message, multiple `Agent` tool blocks.
- **Always pass `model: "opus"`** to every Agent call this skill spawns. Bootstrap is a one-shot up-front investment; brute-force the smartest model.
- **Subagent context isolation is the load-bearing primitive** — same as `audit-impl`. The main session must not duplicate the work the subagents do; it stitches and reports only.
- **Don't fabricate file paths.** Subagent A's prompt explicitly forbids this. If you don't know a path, write `<TBD>` — auto-resolver (E) treats `<TBD>` like an Open Question.
- **Don't chain unless asked.** By default this skill creates a tracker, not a shipped feature (corrected 2026-09-10, PROTOCOL-PORT.2 back-ported). `--and-run` is the opt-in, and even then `/promote-impl` remains the only path to `impl/` + `reference/`.
- **Promotion is APPLY; your invocation is DECIDE (Rule 19).** Starting a bootstrap is not the same decision as starting an execution run — `--and-run` is what turns "draft a tracker" into "and drive it," and that has to be typed deliberately.
- **Don't run when an IMPL collision is detected.** Step 2 abort is non-negotiable — silent overwrites destroy in-progress work.
- **`--ground` is opt-in, not default.** Reasoning: greenfield IMPLs benefit from grounding; pure design-doc IMPLs (e.g., new architecture proposals) over-constrain on existing code if you ground them. The 30s wall-clock cost is cheap; the risk of over-constraint is real. Operator chooses.
- **No mid-flow prompts.** Pick defaults, surface them in the IMPL, commit. The operator's review surface is the commit diff, not a chain of `/clarify-impl` follow-ups.

## Tool allowlist (advisory, when supported)

Main session: `Read, Glob, Grep, Write, Edit, Bash(git add:*), Bash(git commit:*), Agent`. Subagents inherit their `subagent_type` defaults (Plan / general-purpose / Explore are all read-only by configuration except for the eventual `Write` / `Edit` / `git` steps done by the main session in Steps 6–8).

## Why this exists

Two failure modes this skill targets:

1. **Cold-start drag.** Drafting a new IMPL from scratch in a single session takes 30–60 min before any real design work begins, because the same context window is being used for header boilerplate, phase decomposition, and ambiguity-hunting simultaneously. Each task contaminates the others.
2. **Skipped clarify ceremony.** Even with `clarify-impl` available, operators tend to skip it when the IMPL is freshly drafted — the questions feel premature. Generating the Open Questions block as part of the *initial* draft makes it part of the artifact, not an opt-in step.

The pattern is borrowed from Kiro for Claude Code's "✨ New Spec with Agents" feature — see [docs/research/kiro.md](../../../docs/research/kiro.md) for the full study and "do not adopt the extension" verdict. This skill is the cherry-pick: the one Kiro idea worth porting, in waremoto's existing markdown + skills shape, with no IDE coupling and no Windows/PowerShell exclusion.

## Agent execution

Project-level skills under `d:/ware/.claude/skills/` aren't exposed via the Skill tool surface as of 2026-05. When an agent session encounters `/bootstrap-impl …`, it reads this SKILL.md and executes the procedure inline: Read steering context, dispatch subagents in a single parallel `Agent` call burst, stitch, write, commit, then STOP (Step 9's report is final) unless `--and-run` was explicitly passed, in which case chain into the `/run-impl` procedure (also executed inline by re-reading its SKILL.md). No Skill-tool dispatch.

Companion skills:
- `/clarify-impl <FEATURE>` — only when the operator wants to re-open Open Questions on an already-bootstrapped IMPL. Bootstrap folds clarify in by default.
- `/audit-impl <FEATURE>` — drift check once code starts landing
- `/qa-pass` — end-of-phase audit when a phase flips to complete
- `/promote-impl <FEATURE>` — Rule 12 promotion; chained automatically by run-impl when the gates are green, or by hand after `--no-promote`
