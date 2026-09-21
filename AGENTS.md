# Agent instructions

Shared rules for Cursor, Cline, and other coding agents. Cursor also loads `.cursor/rules/`. Cline loads `.clinerules/cline-rule.md`. Keep those copies aligned with this file.

## Build

Verify the solution builds with no warnings and no errors before finishing a coding task.

## Documentation

Update documentation when behavior changes, especially `README.md`:

- Refresh the high-level folder structure when projects are added or removed
- Refresh the technology stack when it changes
- Check completed roadmap items

## Commit messages

When a coding task is finished, suggest a commit message in this shape. Use the same shape for `git commit`:

```
What is implemented

- detail 1
- detail 2
```

- The first line states what was implemented
- Leave one blank line after it
- Then bullet details, at most 10
- No extra paragraphs
