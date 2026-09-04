# JacAutoClicker

A Windows desktop utility (.NET 8) that simulates repeated mouse clicks, started/stopped by a user-assigned trigger key or mouse button. The UI is a React/Tailwind app (source in `IdeaDesign`, gitignored) hosted in a WebView2 control; a thin WinForms `MainForm` provides the window, and `Presentation/WebViewBridge.cs` is the only bridge between the page and the C# use cases. See [ADR 0002](./docs/adr/0002-webview2-for-presentation.md).

Read [CONTEXT.md](./CONTEXT.md) for the domain vocabulary (Trigger, Click Button, Clicker Config, Click Session, Click Limit) before touching Domain or Application code. Read [docs/architecture.md](./docs/architecture.md) for the full layer breakdown before adding or moving code. Check [docs/adr/](./docs/adr/) for recorded architectural decisions before revisiting one.

This is not a Lama project — Lama-specific conventions (branch/commit naming with `lama2026`, PR-without-description default, etc.) do not apply here.

## Commands

```
dotnet build JacAutoClicker.slnx      # build everything
dotnet test JacAutoClicker.slnx       # run the xUnit suite
dotnet run --project src/JacAutoClicker   # run the app
```

After changing the UI (`IdeaDesign`), rebuild and resync the static output before building the .NET app:
```
cd IdeaDesign && pnpm build
rm -rf ../src/JacAutoClicker/wwwroot && cp -r out/. ../src/JacAutoClicker/wwwroot/
```

## Architecture rules

- Clean Architecture, single project: `src/JacAutoClicker/{Domain,Application,Infrastructure,Presentation}`. Dependencies point inward only — `Presentation`/`Infrastructure` → `Application` → `Domain`. Never the reverse.
- `Domain` has zero dependency on `System.Windows.Forms`, Win32, or WebView2. If a change would make Domain reference any of those, the conversion belongs in `Presentation` or `Infrastructure` instead.
- One `Application/UseCases` class per user-facing action, with a single public `Execute`/`ExecuteAsync` method. Don't fold multiple actions into one use case class.
- Only give something a port (`Domain/Repositories` or `Domain/Services` interface) when `Application` actually needs to orchestrate it. Pure UI chrome lives in the React app with no interface.
- `Program.cs` is the only composition root — it's the one place allowed to reference all four layers and wire `Microsoft.Extensions.DependencyInjection`.
- New UI-triggered behavior means: add a message type in `WebViewBridge.HandleMessage`, call the matching use case, add the field to the pushed state if the page needs to see it — don't put use-case logic in the React app, and don't put display formatting/JSX in C#.

## Conventions

- Comments and all `.md` files in English, even mid-Portuguese conversations. Comment the non-obvious *why*, not the *what* — identifiers should already say what the code does.
- No unrequested abstractions, feature flags, or defensive error handling for cases that can't happen — this is a small personal utility, not a platform.
- When a term/behavior changes, update `CONTEXT.md` in the same change, not after. Offer an ADR only when a decision is hard to reverse, surprising without context, and the result of a real trade-off.

## Testing

- `tests/JacAutoClicker.Tests` mirrors `src/JacAutoClicker`'s folder structure (`Domain/`, `Application/`).
- Cover `Domain` and `Application` (pure logic, no Win32/Registry). Don't unit-test `Infrastructure` directly — it's a thin wrapper over Win32 APIs; verify it manually by running the app.
- Use hand-written fakes for the 3 ports (`Tests/Fakes`) instead of a mocking library — they're small enough not to need one.
