using GameboyDotnet;
using GameboyDotnet.Common;
using GameboyDotnet.SDL;
using Microsoft.Extensions.Configuration;
using static SDL2.SDL;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var emulatorSettings = new EmulatorSettings();
configuration.GetSection("EmulatorSettings").Bind(emulatorSettings);
var logger = LoggerHelper.GetLogger<Gameboy>(emulatorSettings.LogLevel);

var (renderer, window) = Renderer.InitializeRendererAndWindow(logger, emulatorSettings);
var gameboy = new Gameboy(logger);
var keyboardMapper = new KeyboardMapper(emulatorSettings.Keymap);
var romPath = Path.IsPathRooted(emulatorSettings.RomPath)
    ? Path.Combine(emulatorSettings.RomPath)
    : Path.Combine(Directory.GetCurrentDirectory(), emulatorSettings.RomPath);

var stream = File.OpenRead(romPath);
gameboy.LoadProgram(stream);

var cts = new CancellationTokenSource();
bool running = true;
int framesRequested = 0;

gameboy.ExceptionOccured += (_, _) =>
{
    cts.Cancel();
    running = false;
};
gameboy.DisplayUpdated += (_, _) =>
{
    Interlocked.Increment(ref framesRequested);
};

Task.Run(() => gameboy.RunAsync(emulatorSettings.FrameLimitEnabled, cts.Token));

// Main SDL loop
while (running && !cts.IsCancellationRequested)
{
    if (SDL_PollEvent(out SDL_Event e) == 1)
    {
        switch (e.type)
        {
            case SDL_EventType.SDL_KEYDOWN:
                if (keyboardMapper.TryGetGameboyKey(e.key.keysym.sym, out var keyPressed))
                    gameboy.PressButton(keyPressed);
                if (e.key.keysym.sym is SDL_Keycode.SDLK_p)
                    gameboy.SwitchDebugMode();
                break;
            case SDL_EventType.SDL_KEYUP:
                if (keyboardMapper.TryGetGameboyKey(e.key.keysym.sym, out var keyReleased))
                    gameboy.ReleaseButton(keyReleased);
                break;
            case SDL_EventType.SDL_QUIT:
                cts.Cancel();
                running = false;
                Renderer.Destroy(renderer, window);
                break;
        }
    }

    if (framesRequested > 0)
    {
        Interlocked.Decrement(ref framesRequested);
        Renderer.RenderStates(ref renderer, gameboy.Ppu.Lcd, ref window);
    }
}

Renderer.Destroy(renderer, window);