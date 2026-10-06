---
name: create-skill
user-invocable: true
description: "Guide the user through creating a new Copilot skill manifest (SKILL.md) for workspace or user-level agent customization."
---

# Create Skill

Use this skill when you want to create or update a Copilot skill manifest (`SKILL.md`) that captures a reusable multi-step workflow for the workspace.

## When to use
- You need a reusable, guided workflow for Copilot customization.
- The task requires multiple steps, decision points, or validation checks.
- You want a workspace-scoped skill that can be invoked from the chat interface.

## What this skill produces
- A new `SKILL.md` manifest in `.github/skills/<skill-name>/SKILL.md`.
- A clear set of instructions for choosing the correct customization primitive.
- A validation checklist to ensure YAML frontmatter and descriptions are correct.

## Steps
1. Clarify the outcome.
   - What should the skill accomplish?
   - Should it be workspace-scoped or user-scoped?
   - Is it a quick checklist or a full multi-step workflow?
2. Choose the right primitive.
   - Use a skill for reusable multi-step workflows.
   - Use a prompt for a focused one-shot task.
   - Use a custom agent when you need isolated tool restrictions or lifecycle hooks.
3. Create the skill file.
   - Workspace: `.github/skills/<skill-name>/SKILL.md`
   - User: `{{VSCODE_USER_PROMPTS_FOLDER}}/<skill-name>/SKILL.md`
   - Include YAML frontmatter: `name`, `description`, `user-invocable`.
4. Validate the file.
   - Confirm the file is in the correct folder.
   - Verify YAML syntax and that `description` contains trigger terms.
   - Check that the skill name matches the folder and is easy to invoke.

## Validation checklist
- [ ] File exists at `.github/skills/<skill-name>/SKILL.md`
- [ ] `name` matches the skill folder and is unique
- [ ] `description` explains the skill and includes trigger phrases
- [ ] `user-invocable` is set to `true`
- [ ] The skill is scoped to the intended audience (workspace or user)

## Example prompts
- "Create a new skill for generating file headers."
- "Build a SKILL.md that helps add release notes to this repo."
- "Generate a workspace skill for project-specific quality checks."
