# Branching convention

Main branch: `main`, always stable and integrated.

Feature branches, one per user story or task:

```
feature/US2.<epic>.<n>-short-description
```

Examples:

```
feature/US2.2.1-view-baggage-manifest
feature/US2.3.1-verify-bag-tag
feature/US2.5.1-supervisor-decision
```

For team-level (T1 to T4) stories, prefix with `feature/T`:

```
feature/T2-hr-login
```

Bug fixes:

```
fix/<short-description>
```

## Workflow

1. Branch off `main`.
2. Commit regularly with meaningful messages, referencing the user story ID.
3. Open a Pull Request into `main` when the story is functionally complete.
4. At least one other team member reviews before merge.
5. Keep the Azure Boards work item linked to the PR where possible.
