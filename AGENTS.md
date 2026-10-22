# Agent instructions

## Branch hygiene

- **Use the configured default branch.** Check the repository setting; it may be `main`, `master`, or something else. Base ordinary work on it and merge approved changes back into it. Target release branches only when required. Respect reviews, checks, and branch protection.
- **Create only needed branches.** Check `git status` and worktrees first. Use one short-lived branch per change, reuse it for revisions, and preserve others' work. Avoid speculative or duplicate branches.
- **Clean up after a verified merge.** Confirm the changes landed, including after squash/rebase, and that no open PR or unmerged work needs the branch. Delete only your completed task's remote/local branch and worktree; prune stale refs. Never delete default, protected, shared, or unrelated branches.
- **Leave a clean environment.** Check for leftover branches, worktrees, and uncommitted changes. Clean up safely or explain what must remain; never merge or delete when authorization or safety is uncertain.
