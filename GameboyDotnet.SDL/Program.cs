using System.Diagnostics;
using GameboyDotnet;
using GameboyDotnet.Common;
using GameboyDotnet.Graphics;
using GameboyDotnet.SDL;
using GameboyDotnet.SDL.SaveStates;
using Microsoft.Extensions.Configuration;
using static SDL2.SDL;

// Load emulator settings from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
var emulatorSettings = new EmulatorSettings();
configuration.GetSection("EmulatorSettings").Bind(emulatorSettings);
var logger = LoggerHelper.GetLogger<Gameboy>(emulatorSettings.LogLevel);
var keyboardMapper = new KeyboardMapper(emulatorSettings.Keymap);
var romPath = Path.IsPathRooted(emulatorSettings.RomPath)
    ? Path.Combine(emulatorSettings.RomPath)
    : Path.Combine(Directory.GetCurrentDirectory(), emulatorSettings.RomPath);

//Initialize SDL renderer and window
var (renderer, window) = Renderer.InitializeRendererAndWindow(logger, emulatorSettings);

var gameboy = new Gameboy(logger);
var stream = File.OpenRead(romPath);
gameboy.LoadProgram(stream);

var cts = new CancellationTokenSource();
bool running = true;

gameboy.ExceptionOccured += (_, _) =>
{
    cts.Cancel();
    running = false;
};


Task.Run(() => gameboy.RunAsync(emulatorSettings.FrameLimitEnabled, cts.Token));

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
                    gameboy.IsMemoryDumpRequested = true;
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
    
    if(gameboy.Ppu.FrameBuffer.TryDequeueFrame(out var frame))
    {
        //Format double to string with 1 decimal place
        string bufferedFramesText = $"Speed: {gameboy.Ppu.FrameBuffer.SpeedPercentage:0.0}% / {gameboy.Ppu.FrameBuffer.SpeedPercentage * 0.6:0} FPS";
        Renderer.RenderStates(ref renderer, ref window, frame!, bufferedFramesText);
    }
}

Renderer.Destroy(renderer, window);