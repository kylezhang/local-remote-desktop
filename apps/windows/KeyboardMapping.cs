namespace LocalRemoteDesktop;

// Protocol flags: Shift=1, Control=2, Option=4, Command=8.
// The existing Mac Host maps Windows VK_CONTROL to Control and VK_L/RWIN
// to Command. Transform both the modifier key AND flags for consistent chords.
static class KeyboardMapping
{
    internal static (int VirtualKey, int Flags) Map(int virtualKey, int flags, bool windowsShortcuts)
    {
        if (!windowsShortcuts) return (virtualKey, flags);
        int mapped = virtualKey switch
        {
            17 or 162 => 91, 163 => 92, // Control -> Command, preserve side
            91 => 162, 92 => 163,       // Windows -> Control
            _ => virtualKey
        };
        int modifiers = (flags & ~10) | ((flags & 2) << 2) | ((flags & 8) >> 2);
        return (mapped, modifiers);
    }
}
