---
name: promote-impl
description: Execute the Rule 12 IMPL → impl/ + reference/ promotion sequence. Manual invocation only — never auto-fires. Use only when an IMPL is genuinely complete (all phases checked, no open bugs, reference doc drafted, tests green or waived). Reads the IMPL, verifies pre-conditions, presents a diff preview, requires user confirmation before any file move.
---

# promote-impl

Mechanical promotion of a completed `{FEATURE}-IMPL.md` per [docs/DOCS-PROTOCOL.md](../../../docs/DOCS-PROTOCOL.md) Rule 12. **Destructive** (file moves, multi-file edits) — never auto-fires; only on explicit `/promote-impl FEATURE` invocation.

## When this fires

- **Manual only.** User runs `/promote-impl FEATURE` (case-insensitive match against `docs/{FEATURE}-IMPL.md`).
- **Never auto-trigger.** The LLM must not decide an IMPL is "ready to promote" — that's a senior-dev judgment call. This skill reflects the call into mechanical actions; it does not make the call.

## Pre-conditions (all must pass)

1. **Target IMPL exists.** `Read docs/{FEATURE}-IMPL.md` succeeds.
2. **All checklist items checked.** No `- [ ]` lines in the `## Checklist` section.
3. **No open `Known Bugs`.** The `## Known Bugs` section either has no items or every item is `~~strikethrough~~` with a resolution.
4. **`docs/reference/{feature-slug}.md` already exists.** The reference doc must be drafted; this skill moves things, it doesn't create the reference doc.
5. **Tests-green confirmation OR explicit waiver.** Either:
   - The IMPL header carries `tests-deferred: <reason>` and BACKLOG.md has a corresponding entry, OR
   - The user confirms "tests green" interactively when prompted.
6. **No uncommitted changes** in `docs/` that would conflict (advisory check via `git status`).

If any pre-condition fails, **abort with a punch list** showing exactly what's missing. Do not partially promote.

## Procedure

1. **Read** the target `docs/{FEATURE}-IMPL.md` and verify all pre-conditions above.
2. **Locate** `docs/reference/{feature-slug}.md` — derive the slug from the feature name (lowercase, hyphenated). If multiple candidates, ask which.
3. **Compute the patch set:**
   - `git mv docs/{FEATURE}-IMPL.md docs/impl/{FEATURE}-IMPL.md`
   - `Edit docs/CONTEXT.md`: remove from "Current Sprint" or "Backlog (Gated/Deferred)"; add a milestone-done row if applicable; add `reference/{slug}.md` to Documentation Index.
   - `Edit docs/TODO.md`: remove the feature's checklist section (or mark its sprint-summary block as Done).
   - `Edit docs/README.md`: move the IMPL link from "Root Living Documents" to "Archived IMPL trackers"; add the reference doc under `reference/`.
   - `Append docs/LOGBOOK.md`: a four-section entry with date, "Promoted {FEATURE}-IMPL to impl/ + reference/{slug}.md" in Accomplished, decisions if any, blockers (none), next.
   - `Append docs/CAPABILITIES.md` (if present): add **one** row — `\| {Capability} \| {the verb/command it gives you} \| [reference/{slug}.md](reference/{slug}.md) \| {today} \|`. The shipped-capability index is the counter-entry to promotion moving a capability out of the attention surface, so a later design/research session can find it (paired with the DOCS-PROTOCOL prior-art-sweep rule). Keep it <50 lines; skip silently if the file doesn't exist.
4. **In-flight checklist punch list (warn-only).** After computing the patch, `Grep` the open `*-IMPL.md` trackers (the IMPL root, excluding `**/impl/`) for the promoted capability's keywords — the feature name tokens + the reference-doc slug. For each open IMPL that matches, print a one-line advisory: *"{OTHER-IMPL} mentions {capability} — does its checklist need a step now that {capability} shipped?"* In-flight checklists are frozen at bootstrap, so a capability that promotes mid-flight never retro-injects into other open trackers. **Warn-only** — never blocks promotion, never edits the other IMPL; a false positive costs one printed line. The operator reads the list and amends each tracker by hand.
5. **Present the patch as a unified diff** — every file change visible to the user.
6. **Wait for explicit confirmation.** "Apply" or equivalent. Anything ambiguous → abort.
7. **Apply** in order: git mv first, then Edit calls, then LOGBOOK append. Stop on first error and report.
8. **Show `git status`** at the end so the user can review and commit on their own terms (this skill does not commit).

## Output shape (success)

```
Promoting AUTH-IMPL...

Pre-conditions:
✓ All 23 checklist items checked
✓ No open Known Bugs (3 resolved with strikethrough)
✓ docs/reference/auth.md exists (drafted 2026-04-30)
✓ Tests green per IMPL header (last run: 2026-05-04)
✓ git status clean in docs/

Patch (5 file ops):
  R docs/AUTH-IMPL.md → docs/impl/AUTH-IMPL.md
  M docs/CONTEXT.md (remove from Current Sprint, add milestone-done row, +1 ref link)
  M docs/TODO.md (remove "Auth" section)
  M docs/README.md (move to Archived IMPL trackers, +1 reference/ row)
  + docs/LOGBOOK.md (append 2026-05-06 promotion entry)

Apply? (yes/no)
```

## Output shape (refused)

```
Cannot promote AUTH-IMPL — 2 pre-conditions failed:

✗ Checklist not complete: 3 of 23 items unchecked
  - AUTH.4.4 — Update reference/oauth-flow.md provider table
  - AUTH.5.3 — Migrate IsQuotaException for new SDK exception types
  - AUTH.5.4 — make dev-auth + make dev-session live-OK

✗ docs/reference/auth.md does not exist

Action required: complete checklist + draft reference doc, then re-run.
```

## Constraints

- **`disable-model-invocation: true` semantics** — only fire on explicit user `/promote-impl` request. (Frontmatter doesn't include this field because Anthropic's Skills frontmatter as of 2026-05 ships only `name`, `description`, optional experimental `allowed-tools`. The behavior is enforced via this constraint and the description's "manual invocation only" wording.)
- **Tool allowlist (advisory, when supported):** `Bash(git mv:*), Bash(git status), Read, Edit, Write, Grep, Glob`. Never `Bash(rm:*)`, never `Bash(git commit:*)`, never `Bash(git push:*)`.
- **No commit.** This skill stages a working-tree change; the operator commits separately.
- **Atomic-or-nothing.** If any step fails, report the partial state and stop. Don't try to clean up — the operator inspects.
- **Idempotent on retry.** Re-running after a partial failure picks up from where it stopped (the `git mv` is the irreversible step; everything else is `Edit`).

## Agent execution

Project-level skills under `.claude/skills/` are not exposed via the agent's Skill tool surface as of 2026-05. When the operator runs `/promote-impl FEATURE` and the agent session is the executor, it performs the procedure **inline**: Read this SKILL.md, Read the target IMPL, run the pre-condition checks, present the diff preview, and **always pause for explicit operator confirmation** before any `git mv` or `Edit`. The destructive nature is the entire point — never auto-apply.

## Why this exists

Rule 12 promotion is 6 manual steps. Friction-driven non-promotion is a common failure mode — IMPLs accumulate at root and the lifecycle stops. Mechanizing the steps + gating on real pre-conditions is the only way to keep the lifecycle honest.
