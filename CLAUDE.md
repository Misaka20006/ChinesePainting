# AGENTS.md

## Project Identity

地狱变相是一款第三人称2D横版单人卷轴以毛笔作为武器阴森怖惧气氛的清版街机游戏

The project is maintained by a small team. Treat existing code as active team work: avoid unrelated rewrites, preserve user changes, and ask before broad refactors.

## Environment

- Godot version: `4.6.2-stable`
- Render pipeline: Forward+
- Main project root: `D:\GODOT\work\Godot-test`
- Main gameplay code: `scripts`
- Git is used for source control.

## Default Working Mode

Default to code-first, semi-auto work.

- Read this file first when starting a new conversation in this repository.
- Prefer reading real code before proposing architecture changes.
- Make narrow, task-scoped edits that follow existing local patterns.

## Collaboration Rules

- The architecture is early-stage and imperfect. Do not treat odd code as automatically wrong; it may encode an edge case.
- If a confusing implementation affects the requested change, ask for clarification instead of guessing.
- Ask for confirmation before massive renames, broad search/replace, large refactors, or changes that affect many systems.
- For refactors, prefer a staged plan and small commits/patches.
- Avoid touching unrelated files and avoid formatting churn.
- Preserve user/team edits in the worktree. Never revert changes unless explicitly requested.
- When adding tests or validation, keep scope proportional to the risk of the change.

## Useful First Checks

For most implementation tasks:

1. Inspect `git status`.
2. Search under `scripts` with `rg`.
3. Read nearby classes before editing.
4. Check package/API usage in existing code before introducing a new pattern.

