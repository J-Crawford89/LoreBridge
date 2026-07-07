# LoreBridge Project Instructions

## What LoreBridge Is

LoreBridge is a local-first worldbuilding and campaign knowledge management system.

It helps users collect, organize, review, canonize, and publish lore from multiple sources such as:

- Obsidian-style Markdown vaults
- ChatGPT brainstorming conversations
- World Anvil articles and exports
- Fantasy Grounds campaign/module data
- Loose notes and imported documents

The initial concrete workspace is Arkellion, a D&D/fantasy campaign setting, but the software must remain generic enough to support other worlds, campaigns, fiction projects, or RPG systems.

## Core Product Concept

LoreBridge manages a structured vault.

The vault is a durable data store, but not everything in the vault is canon.

Expected vault areas:

- Inbox: raw notes
- Staging: brainstorms, imports, unreviewed ideas
- Review: items awaiting human decision
- Canon: authoritative source of truth
- Sources: imported source material from tools like World Anvil, Fantasy Grounds, and ChatGPT
- Exports: generated files/drafts for outside systems
- System: workspace configuration and templates

Only the Canon area is authoritative canon.

## Core Workflow

The intended workflow is:

1. Import or create raw material from external sources.
2. Store new ideas and chat summaries in Staging.
3. Compare Staging material against Canon.
4. Surface conflicts, overlaps, missing details, and possible improvements.
5. Let the user review and promote finalized material into Canon.
6. Generate drafts or exports for outside systems such as World Anvil and Fantasy Grounds.
7. Never automatically overwrite Canon or publish externally without explicit user action.

## Important Safety Rule

LoreBridge should default to:

- Read broadly.
- Write cautiously.
- Never overwrite Canon automatically.
- Never delete notes automatically.
- Never modify a live Fantasy Grounds campaign database directly.
- Prefer generating importable/exportable drafts or modules instead of mutating external systems directly.
- Prefer explicit review queues and user confirmation before publication.

## Key Integrations

### Obsidian / Markdown Vaults

The first storage target is a local folder of Markdown files, compatible with Obsidian.

LoreBridge should treat folder structure as configurable. Do not hardcode folder names like `03_Canon` except as defaults.

Semantic note identity comes from frontmatter, not from the folder path.

Example frontmatter:

```yaml
id: npc.aline_soyer
title: Aline Soyer
type: npc
subtype: mage
status: canon
tags:
  - arkellion
  - npc
```

The folder path may help organize notes, but `type`, `subtype`, and `status` should come from frontmatter.

### ChatGPT / MCP

LoreBridge will expose a controlled MCP host so ChatGPT can search/read vault content and create staging/review notes.

MCP tools should be narrow and safe.

Allowed early MCP tools:

- search notes
- read note
- create staging note
- create review item

Forbidden early MCP tools:

- overwrite canon
- delete notes
- publish to World Anvil
- mutate Fantasy Grounds live campaign data
- run arbitrary shell commands
- write arbitrary file paths

### World Anvil

World Anvil support is planned later.

Initial behavior should prefer:

- import/export/draft generation
- user-reviewed publication
- explicit publish confirmation

Do not implement World Anvil API integration unless explicitly instructed.

### Fantasy Grounds

Fantasy Grounds support is planned later.

Initial behavior should prefer:

- backing up campaign/module data
- parsing copied/exported XML
- generating importable module/export files

Do not directly modify a live Fantasy Grounds campaign `db.xml`.

Do not implement Fantasy Grounds integration unless explicitly instructed.

## Application Architecture

LoreBridge is a series of related applications and libraries.

Recommended projects:

- LoreBridge.Core
  - pure domain models
  - no infrastructure dependencies

- LoreBridge.Application
  - service interfaces
  - use-case/application services
  - depends on Core

- LoreBridge.Infrastructure
  - filesystem vault access
  - Markdown/frontmatter parsing
  - SQLite/indexing later
  - World Anvil/Fantasy Grounds integrations later
  - depends on Core and Application

- LoreBridge.SharedUi
  - shared Blazor/Razor components
  - used by Desktop, Web, and Mobile shells

- LoreBridge.Desktop
  - .NET MAUI Blazor Hybrid desktop app
  - initially uses local services directly
  - later can start/control local MCP host

- LoreBridge.Mobile
  - future .NET MAUI Blazor Hybrid mobile app
  - likely talks to API rather than local campaign files

- LoreBridge.Web
  - future Blazor Web App
  - likely talks to API

- LoreBridge.Api
  - future ASP.NET Core API for hosted/networked mode

- LoreBridge.McpHost
  - ASP.NET Core MCP server
  - ChatGPT-facing controlled tool surface

## Dependency Rules

Allowed dependency direction:

- Core depends on nothing else in the solution.
- Application depends on Core.
- Infrastructure depends on Core and Application.
- SharedUi depends on Core and Application.
- Desktop depends on SharedUi, Application, Core, and Infrastructure.
- McpHost depends on Application, Core, and Infrastructure.
- Future Web/Mobile should use SharedUi and either local services or API-backed services.

Forbidden dependency direction:

- Core must not depend on Application, Infrastructure, UI, Desktop, or MCP.
- Application must not depend on Infrastructure, UI, Desktop, or MCP.
- SharedUi must not depend on Infrastructure.
- Infrastructure must not depend on Desktop or SharedUi.
- MCP host must not contain business logic that belongs in Application/Core.

## Configuration Rules

The app must support configurable workspaces.

The Arkellion workspace is just one configured workspace, not hardcoded global behavior.

Avoid spaces in generated folder names, config keys, enum-like values, and default paths.

Use defaults like:

- `00_Inbox`
- `01_Staging`
- `02_Review`
- `03_Canon`
- `04_Sources`
- `05_Exports`
- `99_System`

Prefer machine-readable config files such as YAML or JSON for app configuration.

Do not use Markdown notes as machine configuration except for human-readable documentation.

## Note Rules

Notes are Markdown files with optional YAML frontmatter.

Use string values for configurable fields like `type`, `subtype`, and `status`; do not use hardcoded enums unless explicitly requested.

Use safe IDs and paths.

Reject:

- absolute paths from untrusted input
- paths containing `..`
- writes outside the configured vault
- writes to Canon from MCP tools
- delete operations unless explicitly implemented later with review/confirmation

## Current MVP Goal

The first vertical slice is:

1. Create/open an Obsidian-style vault.
2. Search Markdown notes from the Desktop app.
3. Read note details from the Desktop app.
4. Create staging notes from the Desktop app.
5. Run a local MCP host.
6. Let ChatGPT search/read notes through MCP.
7. Let ChatGPT create staging notes through MCP.
8. Show ChatGPT-created staging notes in the Desktop review queue.

Do not implement Fantasy Grounds or World Anvil until the vault/MCP loop works.

## Engineering Style

Keep early code simple and boring.

Do not add a database, embeddings, vector search, AI conflict detection, cloud sync, authentication, or external integrations until requested.

Prefer clear interfaces and small services.

When asked to implement a step, do only that step unless explicitly told to continue.