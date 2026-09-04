# Frontier Draw — Project Instructions for Claude Code

I'm building a mobile Unity game called "Frontier Draw" — a real-time 1v1 online PvP
Western quick-draw duel game (not the open-world game shown in the reference/attached
doc — we only borrowed the duel mechanic's feel from it).

The file `frontier-draw-plan-v0.2.md` in this project is our single source of truth
for scope, architecture, folder structure, script list, and the milestone roadmap.
Read it fully before doing anything, and re-check it whenever a decision needs to
match the agreed scope.

## How I want you to work with me

- I'm learning Unity/C# as we go — explain concepts simply, don't assume I already
  know a Unity term or pattern.
- Work ONE step at a time. After each step, stop and wait for me to confirm before
  moving to the next one. Don't do multiple milestones or unrelated tasks in one go.
- Do not write large amounts of code before we've agreed on the exact approach for
  that step.
- Whenever you create a script: give me the exact file path, exact filename, what it
  does, the complete code, where/how to attach it in Unity, and exactly what to set in
  the Inspector.
- Whenever you create a Unity object/GameObject: tell me its name, components,
  hierarchy, inspector values, tag, and layer.
- After every meaningful step, give me a short PROJECT STATE: Completed / Currently
  working on / Next / Known issues.
- If you notice a decision now will cause a problem later (e.g. in the networking
  layer), tell me immediately — don't wait.
- If I send you a screenshot or an error, diagnose the actual root cause before
  suggesting a fix.

## Where to start

Begin with Milestone 0 (Project Setup) exactly as defined in the plan doc — folder
structure, Git repo, URP configuration. Walk me through it step by step. Do not start
Milestone 1 (the local draw-signal prototype) until M0 is confirmed done.
