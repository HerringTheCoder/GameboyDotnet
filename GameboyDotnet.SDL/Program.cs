using Chip8Emu.SDL;
using GameboyDotnet;
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
gameboy.ExceptionOccured += (_, _) =>
{
    cts.Cancel();
    running = false;
};
gameboy.DisplayUpdated += (_, _) => { Renderer.RenderStates(renderer, gameboy.Lcd, window); };

Task.Run(() => gameboy.RunAsync(emulatorSettings.CyclesPerSecond, emulatorSettings.OperationsPerCycle, emulatorSettings.GpuTickRate, cts.Token));

// Main SDL loop
while (running && !cts.IsCancellationRequested)
{
    while (SDL_PollEvent(out SDL_Event e) == 1)
    {
        switch (e.type)
        {
            case SDL_EventType.SDL_QUIT:
                cts.Cancel();
                running = false;
                break;
            case SDL_EventType.SDL_KEYDOWN:
                gameboy.PressedKeyValue =
                    keyboardMapper.TryGetGameboyKey(e.key.keysym.sym, out var result)
                        ? result
                        : null;
                break;
            case SDL_EventType.SDL_KEYUP:
                gameboy.PressedKeyValue = null;
                break;
        }
    }
}

Renderer.Destroy(renderer, window);