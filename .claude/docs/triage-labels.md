# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used on GitHub for this repo.

| Canonical role    | Label string in our tracker | Meaning                                  |
| ----------------- | --------------------------- | ---------------------------------------- |
| `needs-triage`    | `needs-triage`              | Maintainer needs to evaluate this issue  |
| `needs-info`      | `needs-info`                | Waiting on reporter for more information |
| `ready-for-agent` | `ready-for-agent`           | Fully specified, ready for an AFK agent  |
| `ready-for-human` | `ready-for-human`           | Requires human implementation            |
| `wontfix`         | `wontfix`                   | Will not be actioned                     |

We use the canonical names verbatim — there is no remapping. If any of these labels do not yet exist on the GitHub repo, create them on first use:

```bash
gh label create needs-triage    --description "Maintainer needs to evaluate this issue" --color "fbca04"
gh label create needs-info      --description "Waiting on reporter for more information" --color "d4c5f9"
gh label create ready-for-agent --description "Fully specified, ready for an AFK agent" --color "0e8a16"
gh label create ready-for-human --description "Requires human implementation"            --color "1d76db"
gh label create wontfix         --description "Will not be actioned"                     --color "ffffff"
```

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from the table above.
