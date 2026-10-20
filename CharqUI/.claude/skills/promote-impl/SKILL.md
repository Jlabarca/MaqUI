---
name: promote-impl
description: Execute the Rule 12 IMPL → impl/ + reference/ promotion sequence. Manual invocation only — never auto-fires. Use only when an IMPL is genuinely complete (all phases checked, no open bugs, reference doc drafted, tests green or waived). Reads the IMPL, verifies pre-conditions, prints the patch, applies it and commits it (DOCS-PROTOCOL Rule 19 — apply never asks; `--dry-run` previews). Refuses inside an unattended run (Rule 24).
---

# promote-impl

Mechanical promotion of a completed `{FEATURE}-IMPL.md` per [docs/DOCS-PROTOCOL.md](../../../docs/DOCS-PROTOCOL.md) Rule 12. This skill is the **APPLY** half of Rule 19; the **DECIDE** half is a person's invocation — of this skill by hand, or of the default-mode `/run-impl` that chains here. It never fires from an inference that an IMPL "looks done".

## Git scope and commit contract

Maintain an explicit inventory of feature-owned files and a snapshot of pre-existing staged, unstaged and untracked changes. Unrelated work stays untouched by default; dirt alone never requires `--force`. A whole-file commit is allowed only when every included hunk belongs to this task. If a file mixes unrelated edits, separate ownership safely or stop with that specific ambiguity; do not commit the whole file merely because its path appears in the tracker.

Use `workflow-git.py` for read-only branch preflight, scoped commits, phase audit diffs and final completeness checks. Resolve the helper as described below. `commit --repo <root> --branch <validated-branch> --message <message-with-trailers> --paths <exact-files>` stages only those files and commits with `git commit --only ... -- <exact-files>` so unrelated pre-staged files are excluded. Use this contract for bootstrap, plan, box, phase, catch-up and promotion commits. Never use a bare commit or a directory/wildcard scope. Use the Rule 15 taxonomy (`feat`, `fix`, `polish`, `refactor`, `docs`, `test`), never a generic preflight `chore` sweep. Already committed work needs no empty commit.

Run `preflight --repo <root> --branch <validated-branch>` before any commit; writer locks, armed targets and human decisions remain the skill's responsibility. `--no-commit` means no commits in any step, including bootstrap, planning and preflight. Use the helper's `--no-commit` preview when inspecting a proposed commit. A preview can never report Shipped or promoted.

Resolve `<workflow-git>` to `D:/ware/GhostDev/template/bin/workflow-git.py` and invoke it with Python. Always pass the target repository explicitly with `--repo`; do not change the working root to the helper's repository.

## When this fires

- A person runs `/promote-impl FEATURE` (case-insensitive match against `docs/{FEATURE}-IMPL.md`), **or** a default-mode `/run-impl` — which a person invoked — reaches green gates and chains here. Both are the person's DECIDE (Rule 19).
- `--dry-run` prints the pre-condition table and the patch, changes nothing.
- **Never from inference** ("this looks done"). **Never from the unattended lane** (Rule 24): if the worktree is on a `ghostdev/*` branch or `GHOST_AFK_RUN` is set, abort with `promotion is a human act (Rule 24)`.


**Execution lane:** a person-invoked default run (including `--and-run`) is authorized through promotion even when the person leaves it running. Elapsed time, headless execution and subagent delegation do not turn it into the scheduler's unattended lane. Scheduler/AFK runs remain subject to Rule 24, including their branch, promotion and promoter-preview restrictions. `--no-commit` implies no promotion and no preflight auto-commits; report a preview, never a completed promotion.

### Commit completeness before promotion

Before moving the tracker, reconcile its Key Files, phase commits, worker-returned commits and the run's changed-path inventory against `git status --porcelain`, `git diff` and `git diff --cached`. Commit any remaining feature implementation, tests and bookkeeping by exact path with the repository's trailers; do not create empty commits for work already committed. Inspect the index before each commit so unrelated pre-staged changes cannot enter it. Preserve unrelated changes by default, including mixed-file hunks; if ownership cannot be separated safely, report that concrete blocker. Never blanket-add or sweep unrelated work into a promotion.

Run the required verification on the final implementation state after these commits, and refresh commit-pinned verification using actual runner results when required. Then run the promotion gate. After applying and committing promotion, verify the commit exists, the archived tracker and reference/index changes are tracked in HEAD, and no staged, unstaged or untracked **feature-owned** changes remain. Report implementation commit(s) and the promotion SHA. A failed commit or leftover feature work means promotion is incomplete: do not emit success or `loop.done`. Unrelated dirt may remain.

Use `<workflow-git> check-complete --repo <root> --commit <promotion-sha> --paths <all-feature-files-old-and-new> --required <archived-tracker-reference-and-index-files>` for the final check. Include every changed file from the run inventory, not only the promotion docs. All tracked housekeeping, including branch maintenance notes, must be finished before this check. After it, only ignored runtime records/lock release and the report are allowed; any new tracked-file edit invalidates success and must be committed and checked again. Emit `loop.done` only after this final successful check.

`--dry-run` changes and commits nothing. An explicit `--no-commit` run cannot promote. The scheduler's diff-only promoter carve-out stays an uncommitted preview and must never report committed promotion.

## Pre-conditions (all must pass)

1. **Target IMPL exists.** `Read docs/{FEATURE}-IMPL.md` succeeds.
2. **All checklist items checked.** No `- [ ]` lines in the `## Checklist` section.
2b. **No "do not promote" prose.** Nothing in the IMPL says not to promote: no `do not promote`, `promotion is|remains|stays blocked` or `not (yet) ready for/to promot…` anywhere in the document (status line, header, body), matched case-insensitively. An all-ticked checklist does not override the author's own words, so this **blocks** and a person has to read the line before promoting. `bin/promote-check.py` check 2b blocks on it, and the dashboard readiness chip (`no-do-not-promote`) and methrep's `promotion.doNotPromote` read the same fact — the chip must never say *ready* over a doc that says the opposite.
3. **No open `Known Bugs`.** The `## Known Bugs` section either has no items or every item is `~~strikethrough~~` with a resolution.
4. **`docs/reference/{feature-slug}.md` already exists.** The reference doc must be drafted; this skill moves things, it doesn't create the reference doc. **Shape check (machine):** the Rule 12 required sections present (Overview, Architecture, Key Files, API Surface / Code Examples, Usage Guide, Caveats, Related Docs), no `<TBD>`, ≥1 `file:line` cite. **Content is not machine-checkable** — print `reference/{slug}.md: shape OK — content unreviewed by a person` as a warn line, never a prompt.
5. **Behavioral verify provenance — the `loop.verify` gate (DISCIPLINE-HARDENING).** Replaces the old verbal "tests green" claim with a commit-pinned artifact. Resolve the target repository explicitly. On this operator's installation run `python D:/ware/bin/ware.py loop-verify-check --repo <target-root> --suite {FEATURE}`; never assume the target has `bin/ware.py` or let the command default to checking a different repository. Apply, per OQ1 (enforce *honesty of claims*, not *presence of tests*):
   - **Block (hard)** when any phase declares a behavioral `verify:` tier (`unit|integration|e2e`) but the gate is not `pass` (exit ≠ 0) — a tier is *claimed* but no green `loop.verify` line at HEAD backs it (the lie case). Abort with the gate's detail.
   - **Warn without a prompt** when no phase declares a behavioral tier and the gate is `absent` (exit 1): record the absence honestly; do not invent a tier or ask for confirmation. Stale (green but behind HEAD) → require a fresh `python D:/ware/bin/ware.py loop-verify --repo <target-root> --suite {FEATURE} --passed N --total M [--skipped K]` run first.
   - **Pass silently** when the gate returns `pass` (exit 0 — a green run whose `commit == HEAD`), OR the IMPL header carries `tests-deferred: <reason>` + a BACKLOG.md entry (the shipped waiver path is preserved).

   The gate reads the latest `loop.verify` line on `.runs` (`{commit, suite, passed, total, skipped, confidence, tier}`). **Transcribe the runner's numbers verbatim, skips included** — the denominator is `total - skipped`, so `--passed 3219 --total 3222 --skipped 3` is green. Never narrow `--total` by hand to make the gate pass: that is how a `"source": "synthetic"` record with unmeasured counts came to stand as GHOSTSHELL-PARITY's provenance before 2026-09-07. See [DISCIPLINE-HARDENING-IMPL.md](../../../docs/DISCIPLINE-HARDENING-IMPL.md).
6. **Feature work committed.** Apply the commit-completeness contract before gating and moving the tracker. Unrelated dirty files are not a global-clean-tree requirement.

If any pre-condition fails, **abort with a punch list** showing exactly what's missing — an abort is not a prompt; never ask "proceed anyway?". Do not partially promote. Prefer the target's `bin/promote-check.py --repo <target-root> --suite {FEATURE}` when present; otherwise use `python D:/ware/bin/ware.py promote-check --repo <target-root> --suite {FEATURE}`. The shared checker has shipped; do not stop to wait for PROTOCOL-AFK.3.6.

## Procedure

1. **Read** the target `docs/{FEATURE}-IMPL.md` and verify all pre-conditions above.
2. **Locate** `docs/reference/{feature-slug}.md` — derive the slug from the feature name (lowercase, hyphenated). If multiple candidates, ask which.
3. **Compute the patch set:**
   - `git mv docs/{FEATURE}-IMPL.md docs/impl/{FEATURE}-IMPL.md`
   - `Edit docs/CONTEXT.md`: remove from "Current Sprint" or "Backlog (Gated/Deferred)"; add a milestone-done row if applicable; add `reference/{slug}.md` to Documentation Index.
   - `Edit docs/TODO.md`: remove the feature's checklist section (or mark its sprint-summary block as Done).
   - `Edit docs/README.md`: move the IMPL link from "Root Living Documents" to "Archived IMPL trackers"; add the reference doc under `reference/`.
   - `Append docs/LOGBOOK.md`: a four-section entry with date, "Promoted {FEATURE}-IMPL to impl/ + reference/{slug}.md" in Accomplished, decisions if any, blockers (none), next.
   - `Append docs/CAPABILITIES.md` (CAPABILITIES-INDEX C1): add **one** row to the table — `\| {Capability} \| {the verb/command it gives you} \| [reference/{slug}.md](reference/{slug}.md) \| {today} \|`. This is the counter-entry to promotion moving the capability OUT of the attention surface (see [docs/research/ghostdev-design-blindness.md](../../../docs/research/ghostdev-design-blindness.md)). Keep the file <50 lines; if it would exceed, flag for a split (OQ-2) rather than truncating. Create the standard capability table if this file is missing; every promotion records its capability row.
4. **In-flight checklist punch list (CAPABILITIES-INDEX C1, warn-only).** After computing the patch, `Grep` the open `*-IMPL.md` trackers (the IMPL root, excluding `**/impl/`) for the promoted capability's keywords — the feature name tokens + the reference-doc slug. For each open IMPL that matches, print a one-line advisory: *"{OTHER-IMPL} mentions {capability} — does its checklist need a step now that {capability} shipped?"* This is the mechanism-#5 mitigation: in-flight checklists are frozen at bootstrap, so a capability that promotes mid-flight (e.g. the WPM x GhostDev bridge landing while POCOCK-WAVE1 was open) never retro-injects. **Warn-only** — never blocks promotion, never edits the other IMPL; a false positive costs one printed line. The operator reads the list and amends (or ignores) each tracker by hand.
5. **Print the patch as a unified diff** — the review surface is this plus the commit.
6. `--dry-run` → stop here.
7. **Apply** in order: git mv first, then Edit calls, then LOGBOOK append, then the CAPABILITIES row. Stop on first error and report the partial state.
8. **Commit** (Rule 20): use the Git scope contract for exactly the files in the patch (including the old and new tracker paths); `docs(<feature-slug>): promote <FEATURE> per Rule 12` with the two trailers (`Co-Authored-By`, `Session-Id` — omit the latter when empty). After step 9 verifies success, run `python <ware-root>/bin/ware.py loop-done --repo <root> --feature <FEATURE> --reason "promoted <sha>"` so the rail's awaiting-approval rows clear (Rule 19 evidence; `--reason` is required).
9. **Verify commit completeness** using the contract above before reporting success or emitting `loop.done`. Only after that check may the step 8 completion event be emitted.
10. **Report:** the commit sha, the CAPABILITIES row, the in-flight punch list from step 4, and `git revert <sha>` as the undo.

## Output shape (success)

```
Promoting AI-CLIENTS-MIGRATION-IMPL...

Pre-conditions:
✓ All 23 checklist items checked
✓ No open Known Bugs (3 resolved with strikethrough)
✓ docs/reference/ai-clients.md exists (drafted 2026-04-30)
✓ Tests green per IMPL header (last run: 2026-05-04)
✓ git status clean in docs/

Patch (5 file ops):
  R docs/AI-CLIENTS-MIGRATION-IMPL.md → docs/impl/AI-CLIENTS-MIGRATION-IMPL.md
  M docs/CONTEXT.md (remove from Current Sprint, add milestone-done row, +1 ref link)
  M docs/TODO.md (remove "AI Clients Migration" section)
  M docs/README.md (move to Archived IMPL trackers, +1 reference/ row)
  + docs/LOGBOOK.md (append 2026-05-06 promotion entry)
  M docs/CAPABILITIES.md (+1 shipped-capability row)

Applied + committed a1b2c3d — `git revert a1b2c3d` undoes it.
reference/ai-clients.md: shape OK — content unreviewed by a person
```

## Output shape (refused)

```
Cannot promote AI-CLIENTS-MIGRATION-IMPL — 2 pre-conditions failed:

✗ Checklist not complete: 3 of 23 items unchecked
  - AI-CLIENTS.4.4 — Update reference/ai-system.md provider table
  - AI-CLIENTS.5.3 — Update AIRequestThrottle.IsQuotaException for new SDK exception types
  - AI-CLIENTS.5.4 — make dev-rare + make dev-morning live-OK

✗ docs/reference/ai-clients.md does not exist

Action required: complete checklist + draft reference doc, then re-run.
```

## Constraints

- **Person-invoked, or person-invoked-run only (Rule 19).** Never from inference; never on `ghostdev/*` (Rule 24). (Frontmatter cannot express this — Skills frontmatter as of 2026-05 ships only `name`, `description`, optional `allowed-tools` — so it is enforced by this constraint.)
- **Tool allowlist (advisory, when supported):** `Bash(git mv:*), Bash(git add:*), Bash(git commit:*), Bash(git status), Read, Edit, Write, Grep, Glob`. Never `Bash(rm:*)`, never `Bash(git push:*)`.
- **One commit (Rule 20).** The promotion is exactly one revertible commit; the operator's review surface is `git show <sha>`.
- **Atomic-or-nothing.** If any step fails, report the partial state and stop. Don't try to clean up — the operator inspects.
- **Idempotent on retry.** Re-running after a partial failure picks up from where it stopped (the `git mv` is the one step that is not a plain `Edit`; `git mv` back undoes it).

## Agent execution

Project-level skills under `d:/ware/.claude/skills/` are not exposed via the agent's Skill tool surface as of 2026-05. When the operator runs `/promote-impl FEATURE` and the agent session is the executor, it performs the procedure **inline**: Read this SKILL.md, Read the target IMPL, run the pre-condition checks, print the patch, apply, commit, report. There is no confirmation step (Rule 19); `--dry-run` is the preview. Abort — never prompt — on any failed pre-condition or on a `ghostdev/*` branch.

## Why this exists

Rule 12 promotion is 6 manual steps. One adopter has 13 active IMPLs but only 1 archived — friction-driven non-promotion is the dominant failure mode. Mechanizing the steps + gating on real pre-conditions is the only way to keep the lifecycle honest. See [docs/research/ai-driven-methodology-study.md](../../../docs/research/ai-driven-methodology-study.md) § "Where the protocol is bending under usage" item 2.
