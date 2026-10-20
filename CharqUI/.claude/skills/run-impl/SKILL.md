---
name: run-impl
description: Drive an existing FEATURE-IMPL.md through to completion automatically — one phase, several, or all of them. Handles ongoing/paused IMPLs (resumes from the first unchecked box). Same automatic-by-default principle as /bootstrap-impl, applied to execution. Per-phase plan-mode + commit + LOGBOOK + qa-pass, with a session-safety lock to prevent two operators colliding on the same IMPL. Manual invocation only — never auto-fires.
---

# run-impl

The execution counterpart to `/bootstrap-impl`. Bootstrap turns "I want a feature" into a populated IMPL doc; run-impl turns "the IMPL doc exists" into "the feature shipped." Both share the same operating principle: brute-force the work in flow, surface decisions as commits the operator reviews afterward.

## Git scope and commit contract

Maintain an explicit inventory of feature-owned files and a snapshot of pre-existing staged, unstaged and untracked changes. Unrelated work stays untouched by default; dirt alone never requires `--force`. A whole-file commit is allowed only when every included hunk belongs to this task. If a file mixes unrelated edits, separate ownership safely or stop with that specific ambiguity; do not commit the whole file merely because its path appears in the tracker.

Use `workflow-git.py` for read-only branch preflight, scoped commits, phase audit diffs and final completeness checks. Resolve the helper as described below. `commit --repo <root> --branch <validated-branch> --message <message-with-trailers> --paths <exact-files>` stages only those files and commits with `git commit --only ... -- <exact-files>` so unrelated pre-staged files are excluded. Use this contract for bootstrap, plan, box, phase, catch-up and promotion commits. Never use a bare commit or a directory/wildcard scope. Use the Rule 15 taxonomy (`feat`, `fix`, `polish`, `refactor`, `docs`, `test`), never a generic preflight `chore` sweep. Already committed work needs no empty commit.

Run `preflight --repo <root> --branch <validated-branch>` before any commit; writer locks, armed targets and human decisions remain the skill's responsibility. `--no-commit` means no commits in any step, including bootstrap, planning and preflight. Use the helper's `--no-commit` preview when inspecting a proposed commit. A preview can never report Shipped or promoted.

Resolve `<workflow-git>` to `D:/ware/GhostDev/template/bin/workflow-git.py` and invoke it with Python. Always pass the target repository explicitly with `--repo`; do not change the working root to the helper's repository.

## When this fires

- Operator runs `/run-impl <FEATURE>` — drive the IMPL until the next blocker or until promotion gates pass.
- Optional flags:
  - `/run-impl --one-phase <FEATURE>` — execute exactly one phase, then stop.
  - `/run-impl --phase <PHASE_ID> <FEATURE>` — execute the named phase (e.g. `NDJSON-MIGRATION.3`), even if earlier phases have unchecked boxes (operator override; surfaces a warning).
  - `/run-impl --no-commit <FEATURE>` — execute + edit but never commit. For sandbox runs.
  - `/run-impl --no-autocommit <FEATURE>` — defer catch-up commits of pre-existing feature work until its normal checkpoint; unrelated dirt is always preserved.
  - `/run-impl --force <FEATURE>` — recover a confirmed abandoned session lock. Never override a live writer or armed target; dirty files do not need this flag.
  - `/run-impl --no-promote <FEATURE>` — stop at the promotion gate instead of chaining into `/promote-impl` (Rule 19). For a run whose reference doc you want to read before it moves.
  - `/run-impl --audit <FEATURE>` — after each phase's checkboxes are executed (after `#### 3b`, before `#### 3c`), pause and dispatch a fresh-context auditor subagent. See [docs/reference/audit-mode.md](../../../docs/reference/audit-mode.md) for checkpoint payload shape, Verdict grammar, and revision-loop rules.
- **Manual only.** Never auto-fire. Driving an IMPL is a high-blast-radius decision; the operator owns the call.


**Execution lane:** a person-invoked default run (including `--and-run`) is authorized through promotion even when the person leaves it running. Elapsed time, headless execution and subagent delegation do not turn it into the scheduler's unattended lane. Scheduler/AFK runs remain subject to Rule 24, including their branch, promotion and promoter-preview restrictions. `--no-commit` implies no promotion and no preflight auto-commits; report a preview, never a completed promotion.

## Pre-flight (all must pass before any work)

1. **Target IMPL exists.** Glob `**/*-IMPL.md` (exclude `node_modules/`, `bin/`, `obj/`, `.git/`, `**/impl/`). Match `<FEATURE>` case-insensitively against the filename stem. If multiple plausible matches, prefer the one with the most-recent `Updated:` line and report the choice. If none, abort.
2. **Session lock check.** Look for `<root>/.<FEATURE>-IMPL.lock` (sibling to the IMPL).
   - If absent: proceed (will create on entry).
   - If another live session owns the lock: abort with its identity. Age or an advanced HEAD does not prove abandonment. `--force` requires evidence that the old owner is inactive.
   - If the owner is confirmed inactive: note the recovery, then replace the lock only after all read-only preflight checks pass.
3. **Open Questions resolved.** Per Rule 13, every `**Resolution:**` line under `## Open Questions` must be filled. Treat literal `___` or empty as unresolved → abort with "resolve Open Questions first (Rule 13). See `/clarify-impl <FEATURE>`."
   - **Exception:** `**Resolution: DEFERRED**` is allowed but blocks work that depends on that question. Record the dependency and continue independent engineering work under Step 2; selecting `--phase` never waives a prerequisite.
4. **Inventory the working tree (read-only).** Capture status and the index/working diffs; identify feature-owned work and unrelated baseline changes. Do not stage or commit yet. Preserve unrelated dirt by default. Missing ownership information for a mixed file is a specific blocker, not a reason to sweep the tree.
5. **`<TBD>` markers gated by phase.** Scan the IMPL for `<TBD>` strings. If any sit inside the **Key Files Reference** rows for files touched by the next phase to run, abort with the list. `<TBD>` for later phases is fine.
6. **Not an armed foreman target (Rule 23).** Read the project scheduler configuration (if present); if a shift with `enabled: true` and an `enabledUntil` in the future names this IMPL in `targetImpls[]`, **abort**: `<FEATURE> is the target of armed shift <setupId> until <enabledUntil> — disarm it or wait. One writer per tracker (Rule 23).` `--force` never overrides an armed live target.
7. **Integration branch (Rule 21).** Resolve it: `develop`/`development` if it exists and `git rev-list --count <develop>..<default>` is 0; else the default branch. If a dead `develop` exists, print once: `stale develop: N behind <default>, last commit <date> — delete or revive.` If HEAD is not on the integration branch, abort naming both — this skill does not switch branches.

**Preflight ordering:** finish every read-only check above, including branch and armed-target validation, before changing any file or index. A rejected run leaves HEAD, index and working files unchanged. Then acquire the lock, repair authorized prerequisites and optionally checkpoint only existing feature-owned work using the Git scope contract. `--no-autocommit` defers that checkpoint; `--no-commit` suppresses every commit. No global-clean-tree requirement applies.

**General resolver principle:** if a pre-flight condition is cheaply and safely auto-fixable, fix it and proceed — don't stop to ask. Only abort when the issue genuinely requires operator judgment (missing IMPL, unresolved Open Questions, `<TBD>` in active files, merge-state corruption). "Requires operator judgment" means the correct fix isn't obvious from the code alone.


**Prerequisite recovery:** before halting for a missing config, test command or local dependency, inspect the target repository's setup instructions, tracked examples and supported generators. Recover or generate a valid local configuration when its required values are discoverable and setup is within the authorized task. Validate it, then resume the blocked box. Never invent credentials, ownership, server settings or live evidence; stop with the exact missing input when recovery needs those. A recovered prerequisite is not a reason to end the run.

If an unresolvable pre-flight check fails, emit the failure and exit. Do not partial-run.

## Procedure

### Step 1 — Write the session lock

Write `<root>/.<FEATURE>-IMPL.lock` with:

```
started: <ISO-8601 UTC>
session: <this session's UUID, from the scratchpad path; `unknown` if underivable>
branch: <current git branch>
head: <git rev-parse HEAD>
flags: <flags this run was invoked with>
```

The lock is bookkeeping (Rule 14-exempt). It is `.gitignore`-friendly — `*-IMPL.lock` pattern; add to `.gitignore` once on first run if not present.

### Step 2 — Identify the target phase set

- Default (no flag): inspect all unfinished phases and execute runnable engineering work in dependency order. A pending human gate or DEFERRED dependency blocks its dependents, not the entire tracker.
- `--one-phase`: only the first phase with runnable engineering work.
- `--phase PHASE_ID`: only the named phase; report unmet prerequisites without bypassing them.

Before proceeding past a pending gate, read the tracker dependency statements, acceptance requirements and relevant code. Record each pending gate by box id, its missing input, and the boxes/phases it blocks. Record why each later engineering box is independent. An explicit tracker statement of independence or concrete dependency/code evidence is sufficient; checklist order alone proves neither dependence nor independence. Unknown dependencies remain blocked. Re-evaluate after each completed box; do not require the operator to enumerate runnable phase ids.

Never tick, relabel, delete or move a human box to make the tracker promotable; never invent evidence or treat `--force`, a green unit suite or "until promoted" as an acceptance waiver. Execute independent boxes within a mixed phase, but leave that phase incomplete while any of its boxes remain pending. When no runnable engineering work remains, release the lock and report one consolidated list of pending gates and dependent work, with `Promoted: no`.

Example: cohort recruitment is pending; an offline parser regression explicitly needs no cohort data, so implement and verify it. A cohort acceptance test, its dependent rollout, and promotion remain blocked. If parser requirements depend on cohort findings, leave that work blocked too.

A "phase" is delimited by `### Phase N — <name>` headings under `## Checklist`.

### Step 3 — Per-phase execution loop

For each target phase, in order:

Before the phase's first plan/source commit, capture `phaseBase = git rev-parse HEAD` and record it with the phase plan. Preserve that SHA through review/revision cycles; do not reset it after box commits. Keep the exact phase file inventory, including new and deleted paths.

#### 3-pre. Read the phase's Model + Effort (the PHASE-EFFORT contract)

Read the phase's row in the `## Implementation Status` table and extract its **Model** and **Effort** columns — locate the columns **by header name** (case-insensitive) so column order is irrelevant; unknown/absent value → default `Opus`·`high` (or inherit the session). This is the per-phase reasoning budget, a parseable contract `/bootstrap-impl` emits:

- **Dispatched subagents** (the Plan subagent in 3a, the audit subagent in 3b-audit): pass the phase's `model` + `effort` on the `Agent` call (`model:` / `effort:` opts) so delegated reasoning runs at the recommended tier.
- **Main-session work** (the 3b checkbox edits): run-impl cannot self-switch its own session model/effort mid-run (that is interactive `/model` / `/effort` only). Surface a one-line hint at phase start — `phase <PHASE_ID> recommends <Model>·<Effort> — /effort <tier> /model <model> to match` — and proceed at the current session tier. Full per-phase auto-application of the *main* tier is the autonomous AFK/thread runtime's job: one fresh dispatch per phase with `--model`/`--effort` set from this same table (PHASE-EFFORT Track B / SCHEDULED-THREADS).

Effort vocab → runtime: `med|high|xhigh` → `--effort medium|high|xhigh`; `ultra` → the ultracode workflow tier (`--settings '{"ultracode":true}'`, session-only). `max`/`ultra` are session-only — only ever applied per-dispatch, never persisted.

#### 3a. Count source-content files (Rule 14 gate)

Inspect the phase's checklist text for file references. If the phase will touch **2+ source-content files**, Rule 14 requires Plan mode:

1. Dispatch a `Plan`-subagent with the phase checklist + Key Files Reference rows + Architecture section as context. Ask for a concrete plan naming every file to be touched and why.
2. Append the plan into the IMPL under `## Plan — <PHASE_ID>` (or update existing).
3. Commit the plan as a separate `docs(<feature-slug>):` commit so the operator's review surface is a small diff. The operator can then read the next commit (execution) against the planned scope.
4. Proceed to execution.

If the phase is **single-source-content** or **pure docs** (per the METH.7.5 carve-out — IMPL checkbox flips, Updated-line bumps, CONTEXT/TODO/LOGBOOK/README bookkeeping don't count toward the threshold), post a one-line waiver into `## Plan — <PHASE_ID>` per Rule 14 and skip the Plan-subagent dispatch.

#### 3b. Execute checkboxes one at a time

For each unchecked box in the phase, in order:

0. **Pending gate (Rule 18 ii).** Never execute or tick a `human:` box. Record it and its blocked dependents under Step 2, then continue the next demonstrably independent engineering box/phase. Leave unknown dependencies blocked. If nothing runnable remains, report the pending gates and stop without promotion. A ticked human box is a person's evidence record; never manufacture or infer that record.
1. Read the checkbox text. Resolve any `<TBD>` markers in the referenced files first (Glob/Grep to locate; if unresolvable, halt the phase and surface the box as a blocker).
2. Apply the change. Default to `Edit` over `Write` (existing-file edits send only the diff).
3. After the edit, run **the cheapest verification available** (the per-box *continue* gate — build/typecheck must pass to keep going; behavioral tests are the *phase* gate, step 3b-verify):
   - C# project: `dotnet build` of **every csproj whose files this box touched, test projects included** (Rule 22 ii — BUG.A3's production code built while its tests did not). Not the full solution unless needed.
   - TypeScript project: `pnpm tsc --noEmit` of the touched workspace.
   - Python under `codebase-digest/`: `python -m py_compile <file>` then any `pytest -k <relevant>` if tests exist.
   - Pure-docs phase: no build step; skip.
4. If verification fails, attempt **one** auto-fix pass. If still failing, halt the phase and surface the error verbatim.
5. Flip the checkbox `[ ]` → `[x]` **and append its evidence on the same line** (Rule 22 i): `— evidence: <test name | command → exit 0 | path>`. A box with no evidence stays `[ ]`.
6. **Commit the box (Rule 20)** if it changed a source-content file: use the Git scope contract (`workflow-git.py commit --paths` of that box's files + the IMPL). Subject `<type>(<slug>): <BOX-ID> — <what>`, type per Rule 15. A box with no diff folds into the next commit. Skip under `--no-commit`.

#### 3b-verify. Phase verify tier + `loop.verify` emit (DISCIPLINE-HARDENING)

Runs once at the **phase boundary** (after the last checkbox, before 3c QA). It turns the syntax-only build into a behavioral gate and makes run-impl a `.runs` rail producer.

1. **Read the phase's `verify:` tier.** A phase MAY declare `verify: build|unit|integration|e2e` in its `### Phase N` header (or the `## Implementation Status` row); default **`build`** for genuinely non-behavioral phases (docs, scaffolding, schema-only). `build` = the step-3b.3 check already passed → nothing more.
2. **For a behavioral tier (`unit|integration|e2e`):** run the named suite (e.g. `python -m unittest`, `pnpm test`, `dotnet test`). It must be green to **complete** the phase — a build-green-tests-unrun phase does not pass. If `GHOSTDEV_TDD_REQUIRE_RED=1`, drive `bin/tdd-require-red.{ps1,sh} --target <new-test> --expect red` *before* the implementing box and `--expect green` here, so an empty/never-failing test can't satisfy the tier.
3. **On green, emit the provenance line:** `python D:/ware/bin/ware.py loop-verify --repo <target-root> --suite {FEATURE} --passed N --total M [--skipped K] [--confidence C]` (omit `--confidence` to take the synthetic passed/executed score). Copy the runner's counts verbatim — **skips included**; the denominator is `total - skipped`, so a suite with deliberate skips and zero failures is green without anyone editing a number. This is the commit-pinned artifact `/promote-impl`'s pre-condition 5 reads — without it, promotion of a tier-claiming phase blocks (the lie case, OQ1).
4. If the tier suite fails after one auto-fix pass, **halt the phase** (a behavioral verify failure is a hard blocker, same as build).

#### 3b-audit. `--audit` checkpoint (fires after 3b, before 3c, if flag is set)

Skip entirely if `--audit` was not passed.

1. Assemble the checkpoint payload:
   ```yaml
   checkpoint:
     diff: |
       <committed_diff from workflow-git.py audit-diff --repo <root> --base <phaseBase> --paths <phase-files>>
     working_diff: |
       <working_diff from the same command; include contents of any returned untracked files after checking ownership>
     base: <phaseBase>
     head: <current HEAD>
     what_i_did: |
       <one paragraph: what checkboxes were executed and what changed>
     options_i_see:
       - option: A
         description: "proceed to QA as completed"
   ```
   For fork situations (≥2 viable paths the worker cannot resolve), add additional entries to `options_i_see`.

2. Dispatch an `Agent`-tool subagent. Pass **only** the checkpoint payload YAML as the subagent's entire prompt — no session history, no prior context.

3. Parse the returned Verdict block (fenced ` ```verdict ` YAML). Required fields: `option`, `confidence`, `rationale`, `override_hint`. `chosen_option` required when `option: choose`.

4. Branch on `option`:
   - `approve` → proceed to `#### 3c. End-of-phase QA`.
   - `revise` → increment per-checkpoint revision counter (starts at 0, resets each phase).
     - Counter ≤ 2: re-execute phase checkboxes from scratch (`#### 3b`), then re-run this checkpoint.
     - Counter = 3: auto-escalate regardless of auditor output (round cap exhausted).
   - `choose` → resume with `chosen_option` from `options_i_see`; do not increment counter; proceed to 3c.
   - `escalate` → surface the rationale and `override_hint` to the operator; block until operator responds.

5. Budget envelope and recursion guard apply as in the non-audit path (see `## Pre-flight` and the budget note in [docs/reference/audit-mode.md §4.3](../../../docs/reference/audit-mode.md)).

#### 3c. End-of-phase QA

Invoke the `/qa-pass` procedure inline (re-read its SKILL.md, run the punch-list checks). **A non-empty punch list does NOT halt the run.** Record each item into a session-level QA-punch-list accumulator, keyed by phase ID. The accumulator is appended to the LOGBOOK entry (3d, **Blockers** section) and surfaced in full in the Step 6 report. The operator reviews it after the run completes and decides what to address.

Only **hard blockers** halt the run — build/typecheck failure after one retry, `<TBD>` in a file the current checkbox needs, an unresolvable pre-commit hook failure. See `## Stopping conditions`. qa-pass advisory items are not hard blockers.

Write qa-pass's verdict onto the phase's status row: **Shipped** (every source box committed, every touched project incl. tests built, every box evidenced) or **Executed, unverified** (anything missing). A phase that is not Shipped keeps `[~]` in its status row (Rule 22 iii).

#### 3d. Bookkeeping (Rule 14-exempt)

1. Bump the IMPL's `> Updated: YYYY-MM-DD` line to today.
2. Preserve the QA verdict: set `[x] complete` only when QA reported **Shipped** with the required commits and verification. Otherwise keep `[~] Executed, unverified`; `--no-commit` always takes this path. Repair missing requirements before promotion, never overwrite the verdict during bookkeeping.
3. Append a four-section LOGBOOK entry per [docs/LOGBOOK.md](../../../docs/LOGBOOK.md) conventions:
   - **Accomplished:** "Phase `<PHASE_ID>`: <Shipped or Executed, unverified, exactly as QA reported> — N checkboxes, files touched."
   - **Decisions:** any non-trivial choice made during execution.
   - **Blockers:** "none" or anything that surfaced.
   - **Next:** the first checkbox of the next phase, or "Promotion gates" if this was the last phase.
4. Update CONTEXT.md using the same QA verdict; only verified committed phases get a shipped milestone.

#### 3e. Commit (unless `--no-commit`)

Source boxes were committed in 3b.6. This commit is the phase's **bookkeeping** (Rule 20): `docs(<feature-slug>): close <PHASE_ID> — <one line>` — IMPL status row, `Updated:`, LOGBOOK, CONTEXT. If an atomic/single-file checkpoint was explicitly selected, commit and verify that source checkpoint before QA can report Shipped; this is the phase's only commit, typed per Rule 15 from the phase's primary output, and its body lists every box id.

Commit using the Git scope contract and exact phase files, including at commit time. Do **not** blanket-add.

If the pre-commit hook fails: **fix and recommit, do not amend** (per CLAUDE.md guidance).

#### 3f. Advance to the next phase — no pause

If runnable engineering work remains under Step 2, immediately begin the next runnable phase at `#### 3a`, preserving pending gates and incomplete phase status. **Do not stop, do not emit the Step 6 report, do not wait for operator input between phases.** The per-phase commit (3e) is a checkpoint for the operator's later review. Report once after the runnable set is exhausted or a hard failure halts the run. A completed phase is never a stopping condition.


### Commit completeness before promotion

Before moving the tracker, reconcile its Key Files, phase commits, worker-returned commits and the run's changed-path inventory against `git status --porcelain`, `git diff` and `git diff --cached`. Commit any remaining feature implementation, tests and bookkeeping by exact path with the repository's trailers; do not create empty commits for work already committed. Inspect the index before each commit so unrelated pre-staged changes cannot enter it. Preserve unrelated changes by default, including mixed-file hunks; if ownership cannot be separated safely, report that concrete blocker. Never blanket-add or sweep unrelated work into a promotion.

Run the required verification on the final implementation state after these commits, and refresh commit-pinned verification using actual runner results when required. Then run the promotion gate. After applying and committing promotion, verify the commit exists, the archived tracker and reference/index changes are tracked in HEAD, and no staged, unstaged or untracked **feature-owned** changes remain. Report implementation commit(s) and the promotion SHA. A failed commit or leftover feature work means promotion is incomplete: do not emit success or `loop.done`. Unrelated dirt may remain.

Use `<workflow-git> check-complete --repo <root> --commit <promotion-sha> --paths <all-feature-files-old-and-new> --required <archived-tracker-reference-and-index-files>` for the final check. Include every changed file from the run inventory, not only the promotion docs. All tracked housekeeping, including branch maintenance notes, must be finished before this check. After it, only ignored runtime records/lock release and the report are allowed; any new tracked-file edit invalidates success and must be committed and checked again. Emit `loop.done` only after this final successful check.

`--dry-run` changes and commits nothing. An explicit `--no-commit` run cannot promote. The scheduler's diff-only promoter carve-out stays an uncommitted preview and must never report committed promotion.

### Step 4 — Promotion gate (only if all phases now complete)

Before the promotion gate, finish any project-supported Rule 21 branch maintenance for this tracker, within its existing authorization and restrictions. Record results in LOGBOOK and commit that bookkeeping before the final verification. If the project has no reaper command, record that fact here; do not invent a `make` target. Never defer tracked-file writes until after promotion. Then re-run required verification for the final implementation commit.

After the last phase ships, check whether `/promote-impl` pre-conditions hold:

- All checklist items checked.
- No open `Known Bugs` (resolved entries with `~~strikethrough~~` are fine).
- `docs/reference/<feature-slug>.md` exists.
- Tests-green confirmation OR `tests-deferred:` flag in IMPL header.

If they all hold: **chain into `/promote-impl <FEATURE>`** (re-read its SKILL.md, execute inline) unless `--no-promote` or `--no-commit` was passed. Rule 19: apply never asks — the DECIDE was your invocation of this run. Promotion is one commit; `git revert` undoes it. Under `--no-promote`, surface `Promotion gates green — run /promote-impl <FEATURE>` and stop (a Rule 18 stop: the operator withheld the decision).

If gates fail on recoverable in-scope documentation or bookkeeping, complete it, commit it, rerun the affected checks and retry the gate. Draft a missing reference from verified implementation evidence in this run. Halt only for an actual stop-set member, naming the missing evidence or decision; never waive a failed test or invent live proof.

### Step 5 — Release the lock

Delete `<root>/.<FEATURE>-IMPL.lock`. Always — even on halt-and-surface paths. If the conversation crashes before this runs, the next session verifies that the old owner is inactive before recovering it.

### Step 6 — Report

Single terse message, mirroring `/qa-pass` style:

```
/run-impl NDJSON-MIGRATION

Started: NDJSON-MIGRATION.3 (Ghost AI trace persistence)
Shipped: NDJSON-MIGRATION.3 (4/4 checkboxes, 5 files, commit a3f9c12)
Shipped: NDJSON-MIGRATION.4 (4/4 checkboxes, 7 files, commits 8b2d44e..c1d0e2f)
Promoted: no — halted before the gate (a clean run ends `Promoted: <sha> — git revert <sha> undoes it`)
Halted at: NDJSON-MIGRATION.5.2 — NatsTracePublisher path resolution failed (file not at expected location; <TBD>)

Phases complete: 3, 4
Phases remaining: 5 (2 of 4 boxes done), 6
Promotion gates: not yet — phases 5, 6 remain

QA punch list (non-blocking, collected across phases):
- NDJSON-MIGRATION.3 — no test touched for the new trace-persistence path
- NDJSON-MIGRATION.4 — LOGBOOK entry missing a Decisions bullet

Next: resolve <TBD> in Key Files Reference row 11 (the event publisher source file path), then re-run /run-impl NDJSON-MIGRATION
```

The report is emitted **once per run** — never between phases. If the run completed all phases cleanly, the "Halted at" line is omitted and the QA punch list (if any) is the operator's post-run review surface.

### Step 6b — Inbox rail line (interactive-halt producer, GHOSTDEV-COCKPIT-INBOX.5)

Whenever the Step 6 report contains a **"Halted at"** line, OR the run ends with the IMPL
unfinished and something concrete waiting on the operator (an operator-action blocker, a
`Promotion gates green — run /promote-impl` surface), ALSO emit one halt pair onto the
repo's `.runs` rail so the GhostDev cockpit lands this thread in the **Needs-you** lane
with an unread marker (without this, an interactive halt is invisible to the inbox —
only AFK runs produce terminal artifacts):

```bash
python <ware-root>/bin/ware.py loop-halt --repo <root> --feature <FEATURE> \
  --reason "<the Halted-at / Next line, one sentence>" \
  --phase <PHASE_ID> \
  --session-id <this session's UUID> \
  --command "<each exact operator command, repeatable>" \
  --offer "<the offered next step, if the report makes one>"
```

- `<this session's UUID>` — derive it from your scratchpad directory path (the harness
  scratchpad is `...\<project-slug>\<session-uuid>\scratchpad`); it lets the operator
  continue THIS session from the cockpit card. Omit the flag if you cannot determine it.
- `<ware-root>` — `d:/ware` (the CLI is root-only; adopter repos without `bin/ware.py`
  reach it by absolute path).
- Skip this step entirely when the run completed all phases with nothing waiting on the
  operator — a clean finish is `done`, not `awaiting-operator`; a spurious halt line
  would arm a false unread marker.
- This complements (does not replace) the `3b-verify` `loop-verify` line: verify lines
  carry behavioral provenance; the halt pair carries operator attention.
- **Structured-halt contract (INBOX.5.1):** `loop-halt` also upserts a `halt:` block
  (`reason` / `remainingPhase` / `operatorCommands[]` / `offer`) into
  `docs/handoffs/<FEATURE>.md` frontmatter (gitignored/ephemeral) — methrep surfaces it on
  `GET /threads/{id}/handoff` (INBOX.5.2) so the cockpit Report section prefers the contract
  over heuristic extraction of the run's final message (INBOX.5.3). Pass `--no-handoff` to
  skip it; a rich handoff written by `/handoff` keeps its body (only the `halt:` block is
  rewritten).

## Stopping conditions

This is the **stop set** (Rule 18). The skill halts and surfaces (does **not** silently skip) when a member fires mid-run. These are the *only* things that stop a default-mode run — completing a phase is not one of them, nor is a green promotion gate — and every one writes a `loop-halt` record (Step 6b) naming its clause. Every halt path still runs Step 5 (lock release), Step 6 (report), and Step 6b (the inbox rail line):

- **No runnable engineering work remains (Rule 18 ii).** Pending human gates, DEFERRED questions and their known or uncertain dependents stay unchecked. Continue independent work first, then report all remaining gates together. A pending human box alone is not an immediate run-wide stop.
- **Armed foreman target (Rule 18 iii / 23).** Pre-flight 6 aborts before the lock is written.
- **DEFERRED dependency.** Block its dependents and continue independent work in default mode; report unmet prerequisites if `--phase` targets blocked work.
- **`<TBD>` blocking the current checkbox.** Halt, surface the row.
- **Verification (build/typecheck) failed after one retry.** Halt, surface the error.
- **Pre-commit hook failure that auto-fix cannot resolve.** Halt, surface the hook output.
- **More than 6 phases queued in a single run.** Refuse — that's a sign the IMPL needs to be split. Operator can `--phase` through them one by one.
- **Operator interrupt (Ctrl+C / send a new message).** Conversation-level; not in skill control, but the lock-cleanup step (Step 5) won't run, so the next session sees a stale lock and recovers.

**Not a stopping condition:** a non-empty qa-pass punch list (3c). It is collected into the QA accumulator and reported at the end — the run continues to the next phase regardless. The goal of a default run is to reach the promotion gate; advisory QA items are surfaced for the operator, not used to abort.

## Constraints

- **Default mode runs through promotion (Rule 18).** Without `--one-phase`, `--phase` or `--no-promote`, the run ends in a promotion commit or a stop-set halt — nothing else. A completed phase, a per-box commit, a green gate and a non-empty qa-pass punch list are all *not* stopping points. `--one-phase` is the only way to get single-phase behavior.
- **Manual invocation only.** Never auto-fire from observing the user mention an IMPL. The skill commits code; that's not an inference-time decision.
- **A live writer lock is non-negotiable.** `--force` recovers only a confirmed abandoned lock. Two parallel `/run-impl` runs on the same IMPL will corrupt commit history and confuse the LOGBOOK.
- **Rule 14 is non-negotiable.** Multi-source-content phases get a Plan-mode commit before execution, every time. The plan commit is the operator's review surface for the upcoming execution.
- **Per-box commits for source boxes, per-phase for bookkeeping (Rule 20).** A phase is the smallest *mergeable* unit (Rule 12); a box is the smallest *evidenced* unit (Rule 22), and `git log --grep '<BOX-ID>'` must find it.
- **"Shipped" is earned (Rule 22).** The Step 6 report says Shipped only for a phase with commits + green build incl. tests + evidence per box; otherwise Executed, unverified. `--no-commit` runs never say Shipped.
- **Never amend.** Pre-commit hook failures get a fresh commit, per CLAUDE.md.
- **No `<feature-slug>` shorthand.** Always derive: `<FEATURE>` lowercased, hyphens preserved (`NDJSON-MIGRATION` → `ndjson-migration`).

## Tool allowlist (advisory)

Main session: `Read, Glob, Grep, Edit, Write, Bash(git add:*), Bash(git commit:*), Bash(git status:*), Bash(git rev-parse:*), Bash(dotnet build:*), Bash(pnpm tsc:*), Bash(pytest:*), Bash(python -m py_compile:*), Agent (Plan subagent for Rule 14)`.

Never used by this skill: `Bash(git push:*)` (operator decides when to push), `Bash(git merge:*)`, `Bash(git checkout:*)`, `Bash(git switch:*)`, `Bash(git reset:*)`, `Bash(git submodule:*)`. The reaper's `git branch -d` (never `-D`, never on class C or unmerged) is the one deletion this skill performs.

## Why this exists

Three failure modes this skill targets:

1. **Cold-restart drag on paused IMPLs.** Picking up a 3-week-old IMPL costs 20+ min re-reading the header, finding the next box, recalling the conventions. `/run-impl` collapses that into a single command — the skill re-reads the IMPL fresh and starts from the first unchecked box.
2. **Forgotten Rule 14 plan mode.** Multi-file phases routinely skip plan mode under time pressure. Bundling plan mode into the per-phase loop makes the ceremony free.
3. **Drift between IMPL state and git state.** Operators flip checkboxes without committing, or commit without flipping checkboxes. Per-phase commits + automatic `Updated:` bump + automatic LOGBOOK append keep the three in lock-step.

The pattern is the same as `/bootstrap-impl`'s "automatic by default" principle — bootstrap automated the *birth* of an IMPL; run-impl automates the *life* of one. Promotion is chained (Rule 19): it is one revertible commit, and the decision to reach it was made when you invoked this run. What stays a person's act is DECIDING the tracker is done — by invoking a default-mode run, or by hand — and the unattended lane never decides (Rule 24).

## Companion skills

- `/bootstrap-impl <FEATURE-DESCRIPTION>` — creates the IMPL this skill drives.
- `/clarify-impl <FEATURE>` — re-opens Open Questions if a deferred one becomes pressing mid-run.
- `/audit-impl <FEATURE>` — drift check; useful between phases on long-cooked IMPLs.
- `/qa-pass [PHASE_ID]` — folded into Step 3c; can also be run independently.
- `/promote-impl <FEATURE>` — the automatic green-gate continuation of a person-started default run, or a direct person-invoked promotion.

## Agent execution

Project-level skills under `d:/ware/.claude/skills/` aren't exposed via the Skill tool surface as of 2026-05. When an agent session encounters `/run-impl …`, it reads this SKILL.md and executes the procedure inline: pre-flight checks, write the lock, iterate phases (plan → execute → qa-pass → bookkeeping → commit), release the lock, report. No Skill-tool dispatch.
