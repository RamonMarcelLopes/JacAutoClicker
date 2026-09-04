# Architecture

JacAutoClicker follows Clean Architecture in a single project, organized as `src/JacAutoClicker/<Layer>/...` plus a sibling `tests/JacAutoClicker.Tests` project. See [ADR 0001](./adr/0001-adopt-clean-architecture.md) for why, and [CONTEXT.md](../CONTEXT.md) for the domain vocabulary used below (Trigger, Click Button, Clicker Config, Click Session, Click Limit).

## Layers

```
src/JacAutoClicker/
├── Domain/            entities, value objects, and port interfaces — zero external dependencies
│   ├── Entities/       ClickSession
│   ├── ValueObjects/   Trigger (KeyTrigger | MouseTrigger), TriggerButton, ClickButton, ClickerConfig
│   ├── Repositories/   ISettingsRepository
│   └── Services/       IClickSimulator, ITriggerListener
├── Application/        use cases — one class per action, orchestrating Domain via the ports above
│   └── UseCases/        StartClickingUseCase, StopClickingUseCase, ResetClickCountUseCase,
│                        CaptureTriggerUseCase, LoadConfigUseCase, SaveConfigUseCase
├── Infrastructure/     port implementations — the only layer allowed to touch Win32/Registry
│   ├── Input/           Win32ClickSimulator, Win32TriggerListener, MouseVirtualKeyCodes
│   └── Persistence/     RegistrySettingsRepository
└── Presentation/       WinForms UI — the only layer allowed to touch System.Windows.Forms
    ├── Controls/         RoundPanel, DarkNumeric, DarkRadio
    ├── MainForm.cs, DwmHelper.cs, TriggerLabelFormatter.cs
```

## Dependency rule

Dependencies point inward only: `Presentation → Application → Domain` and `Infrastructure → Domain`. `Program.cs` is the composition root — the only place that references all four layers, wiring Infrastructure implementations to Domain ports via `Microsoft.Extensions.DependencyInjection`.

Domain has no reference to `System.Windows.Forms` or Win32 — `Trigger`'s `KeyTrigger` stores a raw virtual-key code (`int`), not WinForms' `Keys` enum, so it stays framework-agnostic. Converting a `Trigger` to a Win32 `Keys` value or a display label is a Presentation/Infrastructure concern (`TriggerLabelFormatter`, `RegistrySettingsRepository`).

## Ports (3, deliberately minimal)

Only capabilities the Application layer actually orchestrates get a port. Pure UI chrome (`RoundPanel`, `DarkNumeric`, `DarkRadio`, `DwmHelper`) lives directly in Presentation with no interface — Application never depends on it, so there's nothing to invert.

- **`IClickSimulator`** — simulates a mouse click. Implemented by `Win32ClickSimulator` (`mouse_event`).
- **`ITriggerListener`** — polls whether the configured Trigger is currently pressed (`IsTriggered`), and captures the next key/mouse-button press when the user rebinds (`BeginCapture`/`EndCapture`). Implemented by `Win32TriggerListener` using `GetAsyncKeyState` for polling and low-level mouse + keyboard hooks (`WH_MOUSE_LL`, `WH_KEYBOARD_LL`) for capture — capture no longer depends on `Form.KeyDown`, so it works independently of window focus.
- **`ISettingsRepository`** — loads/saves a `ClickerConfig`. Implemented by `RegistrySettingsRepository` (`HKCU\Software\JacaAutoClicker`), keeping the original registry value names for backward compatibility with existing installs.

`MainForm` is also allowed to depend on `ITriggerListener` directly (not through a use case) for the 30ms activation poll — it's a plain read, not a state-changing action, so wrapping it in a use case would just be a pass-through.

## Concurrency

The click loop runs as a cancellable `Task` (`StartClickingUseCase`), not a raw `Thread`. `MainForm` owns the `CancellationTokenSource`/`Task` pair for the currently running session and reports progress back via `IProgress<ClickSession>`, which marshals to the UI thread automatically — no manual `Invoke` calls.

## Runtime state vs. persisted config

`ClickerConfig` (Trigger, interval, click button, click limit) is the persisted, immutable value object. `ClickSession` (click count, click rate) is a long-lived, mutable Domain entity that tracks the current run — it is never persisted and is not reset when clicking stops, only via the Reset button or app restart.

## Adding a new feature

- A new user-visible action → a new `Application/UseCases` class (one class, one public `Execute`/`ExecuteAsync` method).
- A new external capability the Application needs → a new interface in `Domain/Repositories` or `Domain/Services`, implemented in `Infrastructure`.
- A new domain concept or renamed term → update [CONTEXT.md](../CONTEXT.md) in the same change, not after.
- A decision that's hard to reverse, surprising without context, and the result of a real trade-off → a new ADR in `docs/adr/`.
