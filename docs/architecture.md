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
└── Presentation/       WinForms host + WebView2 bridge — the only layer allowed to touch
                        System.Windows.Forms or Microsoft.Web.WebView2
    ├── MainForm.cs       borderless Form hosting a WebView2 control that renders wwwroot/
    ├── WebViewBridge.cs  receives JSON commands from the page, calls use cases, pushes state back
    └── TriggerLabelFormatter.cs

wwwroot/                 static export of the IdeaDesign React/Tailwind UI (see ADR 0002) — this
                          is committed, buildable output; IdeaDesign itself is gitignored
```

## Dependency rule

Dependencies point inward only: `Presentation → Application → Domain` and `Infrastructure → Domain`. `Program.cs` is the composition root — the only place that references all four layers, wiring Infrastructure implementations to Domain ports via `Microsoft.Extensions.DependencyInjection`.

Domain has no reference to `System.Windows.Forms` or Win32 — `Trigger`'s `KeyTrigger` stores a raw virtual-key code (`int`), not WinForms' `Keys` enum, so it stays framework-agnostic. Converting a `Trigger` to a Win32 `Keys` value or a display label is a Presentation/Infrastructure concern (`TriggerLabelFormatter`, `RegistrySettingsRepository`).

## Ports (3, deliberately minimal)

Only capabilities the Application layer actually orchestrates get a port. Pure UI chrome (buttons, cards, the traffic-light window controls) lives in the React UI (`IdeaDesign` → `wwwroot`) — Application never depends on it, so there's nothing to invert.

- **`IClickSimulator`** — simulates a mouse click. Implemented by `Win32ClickSimulator` (`mouse_event`).
- **`ITriggerListener`** — polls whether the configured Trigger is currently pressed (`IsTriggered`), and captures the next key/mouse-button press when the user rebinds (`BeginCapture`/`EndCapture`). Implemented by `Win32TriggerListener` using `GetAsyncKeyState` for polling and low-level mouse + keyboard hooks (`WH_MOUSE_LL`, `WH_KEYBOARD_LL`) for capture — capture doesn't depend on window focus.
- **`ISettingsRepository`** — loads/saves a `ClickerConfig`. Implemented by `RegistrySettingsRepository` (`HKCU\Software\JacaAutoClicker`), keeping the original registry value names for backward compatibility with existing installs.

`WebViewBridge` is also allowed to depend on `ITriggerListener` directly (not through a use case) for the 30ms activation poll — it's a plain read, not a state-changing action, so wrapping it in a use case would just be a pass-through.

## Concurrency

The click loop runs as a cancellable `Task` (`StartClickingUseCase`), not a raw `Thread`. `WebViewBridge` owns the `CancellationTokenSource`/`Task` pair for the currently running session and reports progress back via `IProgress<ClickSession>`, which triggers a state push into the page on every click — no manual thread marshaling, since the bridge's callbacks always run on the WinForms UI thread already.

## Presentation: WebView2 bridge (see [ADR 0002](./adr/0002-webview2-for-presentation.md))

The UI is the `IdeaDesign` React/Tailwind app, built to static files (`next build`, `output: 'export'`) and shipped as `wwwroot`. `MainForm` hosts a `WebView2` control pointed at `https://jacaclicker.app/index.html` via `SetVirtualHostNameToFolderMapping`, and clips the window to a rounded region matching the page's own `rounded-[22px]` card.

`WebViewBridge` is the only class that talks to both the web page and the use cases:
- Page → C#: `window.chrome.webview.postMessage({ type: "...", ... })` — `toggleClicking`, `resetCount`, `startTriggerCapture`, `updateInterval`, `updateClickButton`, `updateClickLimit`, `minimizeWindow`, `closeWindow`, `startWindowDrag`.
- C# → page: after every state change, `WebViewBridge` serializes `{ running, capturingTrigger, clickCount, cps, triggerLabel, interval, clickButton, clickLimit }` and calls `window.__hostBridge.receive(...)` via `ExecuteScriptAsync`. The page has no local source of truth beyond that snapshot (`lib/use-host-state.ts`).

**After editing `IdeaDesign`**, rebuild and resync before building the .NET app:
```
cd IdeaDesign && pnpm build
rm -rf ../src/JacAutoClicker/wwwroot && cp -r out/. ../src/JacAutoClicker/wwwroot/
```

## Runtime state vs. persisted config

`ClickerConfig` (Trigger, interval, click button, click limit) is the persisted, immutable value object. `ClickSession` (click count, click rate) is a long-lived, mutable Domain entity that tracks the current run — it is never persisted and is not reset when clicking stops, only via the Reset button or app restart.

## Adding a new feature

- A new user-visible action → a new `Application/UseCases` class (one class, one public `Execute`/`ExecuteAsync` method).
- A new external capability the Application needs → a new interface in `Domain/Repositories` or `Domain/Services`, implemented in `Infrastructure`.
- A new domain concept or renamed term → update [CONTEXT.md](../CONTEXT.md) in the same change, not after.
- A decision that's hard to reverse, surprising without context, and the result of a real trade-off → a new ADR in `docs/adr/`.
