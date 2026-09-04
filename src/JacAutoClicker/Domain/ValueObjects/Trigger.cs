namespace JacaAutoClicker.Domain.ValueObjects;

public abstract record Trigger;

public sealed record KeyTrigger(int VirtualKeyCode) : Trigger;

public sealed record MouseTrigger(TriggerButton Button) : Trigger;
