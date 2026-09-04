# Adopt Clean Architecture for JacAutoClicker

**Status**: accepted

The app previously lived entirely in one 559-line file, with Win32 interop, custom WinForms controls, Registry persistence, and the click loop all mixed directly into `MainForm`. We restructured it into a single-project Clean Architecture (Domain / Application / Infrastructure / Presentation folders, dependencies pointing inward), mirroring the layering already used in the `Nest-clean-arquitechure-study` reference repo, so this becomes the reusable pattern for future C# projects.

Key choices within that shape:
- **Single `.csproj`** with folders per layer, not a multi-project solution — no build/reference overhead is justified for an app this size.
- **Three ports** owned by Application and implemented by Infrastructure: `IClickSimulator`, `ITriggerListener`, `ISettingsRepository`. Pure UI chrome (custom controls, dark title bar) stays directly in Presentation with no abstraction, since Application never depends on it.
- **`Task` + `CancellationToken`** for the click loop instead of a raw `Thread` + boolean flag, with progress reported back to Presentation via `IProgress<T>`/events rather than manual `Invoke` calls.
- **`Microsoft.Extensions.DependencyInjection`** wired in a composition root (`Program.cs`), chosen over manual wiring to match patterns used in larger apps as this one grows.

This is more structure than the app strictly needs today; it's a deliberate choice to establish a consistent, testable reference architecture rather than a necessity driven by current scale.
