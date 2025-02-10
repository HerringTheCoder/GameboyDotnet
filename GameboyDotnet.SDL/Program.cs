using System.Diagnostics;
using GameboyDotnet;
using GameboyDotnet.Common;
using GameboyDotnet.SDL;
using GameboyDotnet.SDL.SaveStates;
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
gameboy.FrameLimiterSwitched += (_, _) =>
{
    framesRequested = 0;
};

Task.Run(() => gameboy.RunAsync(emulatorSettings.FrameLimitEnabled, cts.Token));

int frameCounter = 0;
long lastFpsUpdate = Stopwatch.GetTimestamp();

// Main SDL loop
while (running && !cts.IsCancellationRequested)
{
    if (SDL_PollEvent(out SDL_Event e) == 1)
    {
        switch (e.type)
        {
            case SDL_EventType.SDL_KEYDOWN:
                if (e.key.keysym.sym is SDL_Keycode.SDLK_F5)
                    SaveDumper.SaveState(gameboy, romPath);
                if (e.key.keysym.sym is SDL_Keycode.SDLK_F8)
                    SaveDumper.LoadState(gameboy, romPath);
                if (e.key.keysym.sym is SDL_Keycode.SDLK_p)
                    gameboy.SwitchFramerateLimiter();
                if (keyboardMapper.TryGetGameboyKey(e.key.keysym.sym, out var keyPressed))
                    gameboy.PressButton(keyPressed);
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
        long now = Stopwatch.GetTimestamp();
        if ((now - lastFpsUpdate) >= TimeSpan.FromSeconds(1).Ticks) // 1 second has passed
        {
            lastFpsUpdate = now;
            framesRequested = 0;
        }
        
        string bufferedFramesText = $"Buffered frames: {framesRequested}";
        Renderer.RenderStates(ref renderer, gameboy.Ppu.Lcd, ref window, bufferedFramesText);
        framesRequested--;
    }
}

Renderer.Destroy(renderer, window);