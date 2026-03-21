# Maqui — Documentation Protocol

> Rules for maintaining docs/. Follow these so any contributor (human or AI) knows exactly where to find and place information.

## Structure

```
docs/
├── CONTEXT.md           ← THE living doc (state, decisions, next steps)
├── LOGBOOK.md           ← Append-only session history
├── DOCS-PROTOCOL.md     ← This file
├── README.md            ← Index with links to all docs
│
├── reference/           ← System deep-dives (HOW things work, no status)
├── design/              ← Architecture decision records (frozen at decision time)
├── guides/              ← Practical how-to docs
├── migration/           ← Temporary plans (delete when complete)
├── research/            ← Vision docs, studies, comparisons, explorations
├── tools/               ← Third-party library/tool documentation
└── archive/             ← Old docs kept for historical reference
```

## The 8 Rules

### 1. CONTEXT.md is the Single Source of Truth
All project state, milestone status, architecture decisions, known gaps, and next steps live here. When state changes, update CONTEXT.md FIRST. No other file tracks status.

### 2. Reference docs don't track status
Files in `reference/` explain HOW systems work. No TODOs, no milestone percentages, no progress tracking. Those go in CONTEXT.md.

### 3. LOGBOOK.md is append-only
Each work session appends: date, accomplishments, decisions made, blockers, next steps. Never edit previous entries.

### 4. One update point per change
When something changes: (1) update CONTEXT.md, (2) append to LOGBOOK.md, (3) update the relevant reference doc ONLY if the system description itself changed. Don't scatter the same update across 5 files.

### 5. Temporary docs self-delete
Migration plans and spike docs include at the top: `> Temporary document. Delete when [condition] is met.`

### 6. Design docs are frozen
Files in `design/` capture WHY a decision was made at a point in time. They don't get updated when state changes — they're snapshots.

### 7. README.md is the index
A directory listing with one-line descriptions. Links to CONTEXT.md as the entry point. Under 60 lines.

### 8. Root is ALL CAPS only
The `docs/` root contains ONLY uppercase filenames (CONTEXT.md, LOGBOOK.md, etc). Everything else goes in a subfolder.

## What Goes Where

| I need to... | Put it in... |
|---|---|
| Record current project state, gaps, or next steps | `CONTEXT.md` |
| Log what I did this session | `LOGBOOK.md` (append) |
| Explain how a system works (architecture, API, data flow) | `reference/` |
| Record a one-time architectural decision with rationale | `design/` |
| Write a step-by-step tutorial | `guides/` |
| Document a temporary migration or transition plan | `migration/` |
| Explore a technology, compare options, or research a topic | `research/` |
| Document a third-party tool or library | `tools/` |
| Keep an outdated doc for historical reference | `archive/` |

## Decision Flowchart for New Docs

```
Is it about current status, gaps, or next steps?
  └─ YES → Update CONTEXT.md (don't create a new file)

Is it explaining how an existing system works?
  └─ YES → reference/

Is it a decision with rationale that won't change?
  └─ YES → design/

Is it a how-to for developers?
  └─ YES → guides/

Is it temporary (spike, migration, experiment)?
  └─ YES → migration/ (add deletion condition at top)

Is it exploring options or comparing tools?
  └─ YES → research/

Is it about a third-party tool?
  └─ YES → tools/

None of the above?
  └─ Ask whether it belongs in CONTEXT.md or a new reference doc
```

## Naming Conventions

- Root files: ALL CAPS (`CONTEXT.md`, `LOGBOOK.md`)
- Subfolder files: kebab-case (`routing-commands.md`, `oro-adaptation.md`)
- No numbered prefixes (the old `01-`, `02-` pattern is retired)
- Design docs: include the decision topic in the name (`foundation-decisions.md`, `oro-adaptation.md`)
