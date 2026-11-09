---
trigger: always_on
---
# Sweet Bonanza — Hard Rules

## Verification
- When starting any task, first reply: "Rules loaded ✓ — Working on: [task]"

## Code
- When referencing components, use [SerializeField]. Cache in Awake() only if unavoidable. Never call GetComponent/GetComponentInChildren/AddComponent in Update, loops, or event handlers.
- When writing async logic, use UniTask. Kill all DOTween tweens in OnDisable() and OnDestroy().
- When adding tunable values, use ScriptableObject. Never hardcode balance numbers.
- When you need more variables than specified, STOP and ask first. Never add hidden fallbacks (AutoBind, BindPair, string searches).

## Architecture
- When creating a new script, place it in the correct layer folder: Model/ (data+enums only), View/ (presentation only), Controller/ (orchestration), Service/ (network+backend).
- When a View needs game data, reference through Model. Never put math/balance in View.
- When a Controller needs backend data, call Service layer. Never access SocketIO from View.

## Safety
- When asked a question or reviewing code, do NOT edit files. You MAY run read-only commands (git status, dotnet build, read_console).
- When editing a method signature or removing a field, search all scripts that call it before finishing.
- When finishing code edits, verify 0 compile errors via dotnet build or Unity MCP read_console.
- Never modify .unity or .prefab files unless explicitly told to.
- Never run git push. Local git only (status, diff, checkout).
- When adding textures, use ASTC 6x6 or Crunch. Never add uncompressed 2048x2048 PNGs. Never place >100KB assets in Resources/.

## Override
- If the developer explicitly contradicts a rule for a specific instance, follow the developer. Log it in .agents/rules/mistakes.md.

