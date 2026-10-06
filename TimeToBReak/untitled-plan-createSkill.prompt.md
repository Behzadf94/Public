Guide the user through creating a new Copilot skill manifest (SKILL.md) for workspace or user-level agent customization.

Steps:
1. Review the conversation history and identify whether a reusable workflow or a focused customization prompt is needed.
2. Determine if the customization should be workspace-scoped or user-scoped.
3. Choose the appropriate primitive: skill for multi-step workflows, prompt for single tasks, custom agent for isolated tool restrictions.
4. Create the skill manifest file in the correct location:
   - Workspace: .github/skills/<skill-name>/SKILL.md
   - User: {{VSCODE_USER_PROMPTS_FOLDER}}/<skill-name>/SKILL.md
5. Validate the file:
   - Confirm the file exists in the chosen folder.
   - Verify YAML frontmatter and description syntax.
   - Ensure the skill name is clear and matches the folder.
6. Summarize what the skill produces and suggest example prompts.
