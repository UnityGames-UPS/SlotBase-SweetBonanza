# Sweet Bonanza – Engineering & Architecture Rules

This project-level rules document enforces clean, high-performance, and non-intrusive development standards for **SlotBase-SweetBonanza**. Every assistant operating in this repository must strictly adhere to these guidelines.

---

## 1. Zero Extra GetComponent / AddComponent
- **Direct Serialized References**: Use `[SerializeField]` Inspector references for all UI, Audio, and Game components.
- **No Runtime Lookups**: NEVER call `GetComponent()`, `GetComponentInChildren()`, or `AddComponent()` in runtime loops, slider events, button clicks, or update ticks.
- **Awake-Only Caching**: If a component must be fetched in code, cache it ONCE in `Awake()` and never fetch it again during gameplay.

---

## 2. Strict Variable Count & Prior Permission
- **Exact Counts**: When the developer specifies an exact count of variables (e.g., 3 variables), implement STRICTLY that count.
- **Prior Permission Required**: If analysis suggests additional elements (e.g., 5 elements) might be needed:
  1. **STOP and ASK the developer first.**
  2. Explain the rationale and wait for explicit confirmation.
  3. **NEVER** silently add extra variables, hidden fallback methods (`AutoBind`, `BindPair`), or string searches behind the developer's back.

---

## 3. High-Performance & Optimization (KISS)
- **Keep It Simple, Stupid**: If an implementation can be done cleanly in 10 lines, never write 30.
- **Zero GC Allocations**: Avoid heap allocations in active gameplay, spins, cascades, and UI interactions.
- **Animation Hygiene**: All UI animations and tweens must use DOTween and be killed cleanly (`rect.DOKill()` or `DOTween.Kill(this)`) on `OnDisable()` and `OnDestroy()`.

---

## 4. Strict MVCS Architecture
- **Model** (`Assets/SL-SBZ/Scripts/Model/`): Pure game data state and enums only. Zero UI references.
- **View** (`Assets/SL-SBZ/Scripts/View/`): Presentation only (`MainView`, `GridView`, `CellView`, `UIManager`, `PopupManager`). Renders sprites, plays animations, receives clicks. Zero game math or balance rules.
- **Controller** (`Assets/SL-SBZ/Scripts/Controller/`): Orchestration (`GameManager`). Mediates between Views and Services.
- **Service** (`Assets/SL-SBZ/Scripts/Service/`, `Backend/`): Network transport and data payloads (`BackendService`, `SocketIOManager`, `LocalDemoService`).

---

## 5. Read-Only / Discussion by Default
- **Permission to Edit**: When asked a question, discussing architecture, or reviewing code, remain strictly **Read-Only**.
- **No Unprompted Edits**: Never edit, create, or touch any file unless given an explicit imperative command like *"Fix this"* or *"Edit this"*.

---

## 6. The "Blast Radius" Check & Verification
- **Cross-File Integrity**: Whenever editing a method signature or removing a field, check all other scripts that call it across the project before finishing.
- **Mandatory Compile Check**: Always run `dotnet build Assembly-CSharp.csproj` to guarantee **0 errors and 0 warnings** before reporting a task completed.

---

## 7. Scene & Prefab Protection
- **C# Only**: Code changes belong in C# scripts.
- **No Direct Scene Edits**: Never directly rewrite or modify `.unity` scene files or `.prefab` assets unless the developer explicitly asks for scene modifications.

---

## 8. WebGL Asset & Memory Discipline
- **Texture Compression**: Never introduce uncompressed 2048x2048 PNGs. Use ASTC 6x6 or Crunch compression for WebGL.
- **No Heavy Resources**: Never place heavy assets, fonts, or demos inside `Resources/` folders.
- **Fixed Frame Rates**: Symbol 3D animations must remain locked to a fixed 24–30 FPS (no framerate-dependent drift loops).

---

## 9. Strict Git Boundary
- **Local Only**: Use strictly local git operations (`git status`, `git diff`, `git checkout`).
- **NEVER** run `git push` under any circumstances.

