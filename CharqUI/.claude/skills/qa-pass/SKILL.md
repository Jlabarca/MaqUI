---
name: qa-pass
description: End-of-phase QA audit. Run this when an IMPL phase is about to flip from in-progress to complete (last checkbox getting ticked). Re-reads the phase checklist, verifies tests were touched in this session via git diff, verifies LOGBOOK was appended, outputs a punch list of what would block phase completion or future promotion. Borrows from BMAD's QA persona pattern without the multi-persona ceremony.
---

# qa-pass

End-of-phase QA pass. Operator runs this before flipping the last checkbox of a phase to verify the phase actually shipped what it claimed.

## When this fires

- User runs `/qa-pass` with an optional `PHASE_ID` argument (e.g., `/qa-pass AUTH.4` or `/qa-pass` to audit the most-recently-touched phase).
- Recommended at the end of every phase before crossing into the next phase's checkboxes.

## Procedure

1. **Identify the target phase.**
   - If user supplied `PHASE_ID`, parse it (e.g., `AUTH.4` → IMPL `AUTH-IMPL.md`, phase `4`).
   - Else: `Glob docs/*-IMPL.md`, find the IMPL with the most recent commit touching it, infer the in-progress phase from the first un-checked checkbox.
2. **Audit the phase's checklist.** For each checkbox in the phase:
   - If checked: spot-check that the work appears in `git log` since the IMPL's previous `Updated:` date. Live shell: `` !`git log --since="{prev-update}" --oneline -- {referenced-files}` ``
   - If unchecked: note as "remaining."
3. **Verify tests were touched.** Live shell: `` !`git diff --stat {prev-update}..HEAD -- tests/` `` (or equivalent test-folder pattern). If the phase claims `What Needs Testing` items and the test diff is empty, flag it.
4. **Verify LOGBOOK was appended this session.** Live shell: `` !`git log --since="1 day ago" --oneline -- docs/LOGBOOK.md` ``. If empty and phase had any commits, flag it.
5. **Verify Updated: date in IMPL was bumped.** Compare the IMPL's `Updated: YYYY-MM-DD` line against today.
6. **Verify index write-ordering (GDC.5.5).** If docs/README.md gained an "Archived IMPL Trackers" row this session, the referenced docs/impl/ file must exist on disk NOW - a row written ahead of its promotion poisons the Rule 17 prior-art sweep. Flag any row without a file behind it.
7. **Compile and emit the punch list.**

## Output shape

```
QA pass: AUTH-IMPL.md, phase P2 (core endpoints)

Checklist (5 of 6 done):
  ✓ AUTH.2.1 login endpoint            — committed b3a4c5e
  ✓ AUTH.2.2 logout endpoint           — committed b3a4c5e
  ✓ AUTH.2.3 refresh token endpoint    — committed b3a4c5e
  ✓ AUTH.2.4 session validation        — committed b3a4c5e
  ✓ AUTH.2.5 rate limiting             — committed b3a4c5e
  ✗ AUTH.2.6 Integration test against staging — REMAINING

Tests touched: NO files under tests/ in this session.
   → P2 has TEST.1–TEST.4 enumerated; consider whether smoke tests against
     staging count as "touched" before promoting.

LOGBOOK appended: NO entry since 2026-05-03.
   → Append a P2 ship entry before flipping the phase to complete.

IMPL Updated: line: stale (still 2026-05-03; today is 2026-05-06).

Verdict: 1 checkbox + LOGBOOK + Updated line outstanding before phase P2 closes.
```

## Constraints

- **Read-only.** No edits, no git mutations. Operator acts on the punch list.
- **Live shell injection** is the load-bearing primitive — static "tests last touched" lines drift; `git log` is fresh.
- **Persona separation, single-operator-style.** This skill is the BMAD "QA persona" pass without the multi-persona ceremony — operator wears the QA hat for 30 seconds, then moves on. No roleplay required.
- **Cap output at ~250 words.** Long punch lists get ignored.

## Tool allowlist (advisory)

`Read, Grep, Glob, Bash(git log:*), Bash(git diff:*), Bash(git status)`. No mutating tools.

## Agent execution

Project-level skills under `.claude/skills/` are not exposed via the agent's Skill tool surface as of 2026-05. When invoked inline, the agent Reads this SKILL.md, runs the `git log` / `git diff` / `Glob` calls itself (no Skill-tool dispatch), and emits the punch list as a chat message. Read-only by procedure shape — the operator decides what to fix.

## Why this exists

Test-after culture is honest but creates a gap: phases ship without tests being written, then promotion gates demand them and the phase rolls back. A QA pass at the end of every phase makes the gap visible *before* it costs a rollback.
