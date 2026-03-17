# Future Documentation Roadmap: Building the "Ultimate DX" UI

To complete your transition into a high-performance, developer-friendly UI system, the following 10 technical documents are recommended. These cover architecture, performance, and modern Unity workflows (UI Toolkit).

## 1. [UI-System-Bootstrap-Flow.md](UI-System-Bootstrap-Flow.md)
**Context**: How the UI initializes when the game starts.
**Why**: Ensures that `EventMessenger` and `UIFramework` are correctly ordered before any View tries to access them.

## 2. [Payload-Contract-Library.md](Payload-Contract-Library.md)
**Context**: A catalog of all existing `IPayload` types.
**Why**: Serves as the "API Documentation" for your UI. Devs need to know what payloads exist to trigger specific UI behaviors.

## 3. [UI-Toolkit-Integration-Guide.md](UI-Toolkit-Integration-Guide.md)
**Context**: How to create a "Bridge View" that uses UI Toolkit inside the current uGUI framework.
**Why**: Performance! You need a clear path to migrate heavy elements (Inventories, Leaderboards) to the faster UI Toolkit engine.

## 4. [Procedural-Visuals-Standards.md](Procedural-Visuals-Standards.md)
**Context**: Guidelines on when to use `Gradient.cs` vs. Sprites vs. Shaders.
**Why**: Prevents "Component Rot" and ensures the UI remains performant (batch-friendly).

## 5. [View-Animation-States.md](View-Animation-States.md)
**Context**: Detailed specification for `DisplayOptions` and procedural transitions.
**Why**: Standardizes how views "enter" and "exit" the stage, ensuring a consistent user feel.

## 6. [Input-Management-Layer.md](Input-Management-Layer.md)
**Context**: How the UI intercepts or passes through input to the game world.
**Why**: Integration with the New Input System and handling "Global Blocking" when popups are open.

## 7. [UI-Localization-Workflow.md](UI-Localization-Workflow.md)
**Context**: Integrating with Unity Localization or custom JSON-based systems.
**Why**: Critical for high-DX; devs should never hardcode strings in the Inspector.

## 8. [Rendering-Optimization-Audit.md](Rendering-Optimization-Audit.md)
**Context**: Profile analysis of Canvas Rebuilds and Draw Calls.
**Why**: Identifies "Silent Performance Killers" in the existing kits and sets limits for future UI elements.

## 9. [Developer-Onboarding-Playbook.md](Developer-Onboarding-Playbook.md)
**Context**: A step-by-step guide for a new developer to add a "Settings Menu."
**Why**: This IS the Developer Experience (DX). A good system is one that a newcomer can contribute to in under 30 minutes.

## 10. [System-Upgrade-Roadmap.md](System-Upgrade-Roadmap.md)
**Context**: Technical debt tracking and planned refactors (e.g., removing static dependencies).
**Why**: Keeps the architecture clean over time and prevents the project from becoming a "Legacy UI Mess."

---

# 🚀 Next Stage: System Design
Now that you have audited the existing tools, you are ready to design your own.
- [UI Architectural Concepts Comparison](design/ui-architectural-concepts-comparison.md)
- [Detailed UI Recommendations](design/ui-architectural-concepts-recommendation.md)
- [Concept Re-conditioning Template](design/ui-architectural-concepts-template.md)
- [OneUI vs. Hybrid Recommendation](design/oneui-vs-hybrid-recommendation.md)
- [Dual-Input System Abstraction](design/dual-input-system-abstraction.md)
- [CharqUI Final Architecture](design/charqui-architecture.md)
- [CharqUI Implementation Logbook](design/charqui-logbook.md)
- [Visual FX Optimization](design/charqui-visual-fx-optimization.md)
- [CharqUI Plugin Ecosystem](design/charqui-plugins.md)
- [Le Tai Assets Architecture](plugins/le-tai-assets-architecture.md)
- [AwesomeAttributes Architecture](plugins/awesome-attributes-architecture.md)
- [Attributes Utility Architecture](plugins/attributes-utility-architecture.md)
- [BetterFolders Architecture](plugins/better-folders-architecture.md)
- [LokoSolo Architecture](plugins/lokosolo-architecture.md)
- [UI Particles Architecture](plugins/ui-particles-architecture.md)

---

> [!TIP]
> **Priority Recommendation**: Start with **#1 (Bootstrap)** and **#9 (Onboarding)**. These define the "Soul" of your DX by making the system stable and easy to teach.
