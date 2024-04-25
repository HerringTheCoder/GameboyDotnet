using Microsoft.Extensions.Logging;
using static SDL2.SDL;

namespace GameboyDotnet.SDL;

public class EmulatorSettings
{
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public bool FrameLimitEnabled { get; set; }
    public string RomPath { get; set; } = null!;
    public IDictionary<SDL_Keycode, string> Keymap { get; set; } = null!;
    public LogLevel LogLevel { get; set; }
}