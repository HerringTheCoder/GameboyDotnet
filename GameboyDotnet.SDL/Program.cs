using System.Diagnostics;
using GameboyDotnet;
using GameboyDotnet.Common;
using GameboyDotnet.Graphics;
using GameboyDotnet.SDL;
using GameboyDotnet.SDL.SaveStates;
using GameboyDotnet.Sound;
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
var (renderer, window) = SdlRenderer.InitializeRendererAndWindow(logger, emulatorSettings);

var gameboy = new Gameboy(logger);
var audioPlayer = new SdlAudio(gameboy.Apu.AudioBuffer);
audioPlayer.Initialize();

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

var userActionText = string.Empty;
var userActionTextFrameCounter = 0;

// Main SDL loop
while (running && !cts.IsCancellationRequested)
{
    if (SDL_PollEvent(out SDL_Event e) == 1)
    {
        switch (e.type)
        {
            case SDL_EventType.SDL_KEYDOWN:
                switch (e.key.keysym.sym)
                {
                    case SDL_Keycode.SDLK_F5:
                        gameboy.IsMemoryDumpRequested = true;
                        break;
                    case SDL_Keycode.SDLK_F8:
                        SaveDumper.LoadState(gameboy, romPath);
                        break;
                    case SDL_Keycode.SDLK_p:
                        gameboy.SwitchFramerateLimiter();
                        break;
                    case SDL_Keycode.SDLK_1:
                        gameboy.Apu.SquareChannel1.IsDebugEnabled = !gameboy.Apu.SquareChannel1.IsDebugEnabled;
                        userActionText = $"CH1: {(gameboy.Apu.SquareChannel1.IsDebugEnabled ? "on" : "off")}";
                        userActionTextFrameCounter = 120;
                        break;
                    case SDL_Keycode.SDLK_2:
                        gameboy.Apu.SquareChannel2.IsDebugEnabled = !gameboy.Apu.SquareChannel2.IsDebugEnabled;
                        userActionText = $"CH2: {(gameboy.Apu.SquareChannel2.IsDebugEnabled ? "on" : "off")}";
                        userActionTextFrameCounter = 120;
                        break;
                    case SDL_Keycode.SDLK_3:
                        gameboy.Apu.WaveChannel.IsDebugEnabled = !gameboy.Apu.WaveChannel.IsDebugEnabled;
                        userActionText = $"CH3: {(gameboy.Apu.WaveChannel.IsDebugEnabled ? "on" : "off")}";
                        userActionTextFrameCounter = 120;
                        break;
                    case SDL_Keycode.SDLK_4:
                        gameboy.Apu.NoiseChannel.IsDebugEnabled = !gameboy.Apu.NoiseChannel.IsDebugEnabled;
                        userActionText = $"CH4: {(gameboy.Apu.NoiseChannel.IsDebugEnabled ? "on" : "off")}";
                        userActionTextFrameCounter = 120;
                        break;
                    case SDL_Keycode.SDLK_5:
                        FrequencyFilters.IsHighPassFilterActive = !FrequencyFilters.IsHighPassFilterActive;
                        userActionText = $"High-Pass filter: {(FrequencyFilters.IsHighPassFilterActive ? "on" : "off")}";
                        userActionTextFrameCounter = 120;
                        break;
                }
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
                SdlRenderer.Destroy(renderer, window);
                break;
        }
    }
    
    // Handle frame rendering based on CGB mode
    bool isCgbMode = gameboy.Ppu.FrameBuffer.IsCgbMode;
    
    if (isCgbMode)
    {
        // CGB mode: render color frames
        if (gameboy.Ppu.FrameBuffer.TryDequeueColorFrame(out var colorFrame))
        {
            string bufferedFramesText = $"Speed: {gameboy.Ppu.FrameBuffer.Fps / 60.0 * 100.0:0.0}% / {gameboy.Ppu.FrameBuffer.Fps:0} FPS [CGB]";
            if (userActionTextFrameCounter > 0)
            {
                userActionTextFrameCounter--;
            }
            else
            {
                userActionText = string.Empty;
            }
            SdlRenderer.RenderColorStates(ref renderer, ref window, colorFrame!, string.Join(" \n ", bufferedFramesText, userActionText));
            
            // Also dequeue DMG frame to keep queues in sync
            gameboy.Ppu.FrameBuffer.TryDequeueFrame(out _);
        }
    }
    else
    {
        // DMG mode: render grayscale frames
        if (gameboy.Ppu.FrameBuffer.TryDequeueFrame(out var frame))
        {
            string bufferedFramesText = $"Speed: {gameboy.Ppu.FrameBuffer.Fps / 60.0 * 100.0:0.0}% / {gameboy.Ppu.FrameBuffer.Fps:0} FPS";
            if (userActionTextFrameCounter > 0)
            {
                userActionTextFrameCounter--;
            }
            else
            {
                userActionText = string.Empty;
            }
            SdlRenderer.RenderStates(ref renderer, ref window, frame!, string.Join(" \n ", bufferedFramesText, userActionText));
        }
    }
}

audioPlayer.Cleanup();
SdlRenderer.Destroy(renderer, window);