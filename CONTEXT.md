# JacAutoClicker

A Windows desktop utility that simulates repeated mouse clicks, started and stopped by a user-assigned trigger key or mouse button.

## Language

**Trigger**:
The key or mouse button assigned to start/stop a Click Session. Exactly one of a keyboard key or a mouse button, never both.
_Avoid_: Bind, hotkey

**Trigger Button**:
The specific mouse button (left/right/middle) used as a Trigger, when the Trigger is a mouse button rather than a keyboard key.
_Avoid_: Mouse button (ambiguous with Click Button)

**Click Button**:
The mouse button (left/right) that gets simulated repeatedly while a Click Session is active.
_Avoid_: Mouse button (ambiguous with Trigger Button)

**Clicker Config**:
The persisted configuration for the clicker: Trigger, click interval, Click Button, and Click Limit. Immutable value object, loaded/saved as a whole.
_Avoid_: Settings (too generic on its own)

**Click Limit**:
The maximum number of clicks a Click Session performs before stopping itself automatically. A value of 0 means unlimited.

**Click Session**:
A single active run of auto-clicking: tracks Click Count and Click Rate while running. Not persisted — it exists only while clicking is active.
_Avoid_: Clicking state

**Click Rate**:
The number of clicks performed in the last second during a Click Session (displayed as CPS).
_Avoid_: CPS (fine as UI label, not as the canonical term)
