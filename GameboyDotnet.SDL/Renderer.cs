using System.Diagnostics;
using GameboyDotnet.Components;
using GameboyDotnet.Graphics;
using Microsoft.Extensions.Logging;
using SDL2;
using static SDL2.SDL;

namespace GameboyDotnet.SDL;

public static class Renderer
{
    private const int ScreenWidth = 160;
    private const int ScreenHeight = 144;

    // Constants for colors
    private static readonly SDL_Color White = new() { r = 255, g = 255, b = 255, a = 255 };
    private static readonly SDL_Color LightGray = new() { r = 170, g = 170, b = 170, a = 255 };
    private static readonly SDL_Color DarkGray = new() { r = 85, g = 85, b = 85, a = 255 };
    private static readonly SDL_Color Black = new() { r = 0, g = 0, b = 0, a = 255 };
    
    public static void RenderStates(nint renderer, Lcd lcd, nint window)
    {
        try
        {
            SDL_GetWindowSize(window, out int windowWidth, out int windowHeight);

            // Calculate scaling factors for width and height
            float scaleX = (float)windowWidth / ScreenWidth;
            float scaleY = (float)windowHeight / ScreenHeight;

            // Determine the smaller scaling factor to maintain aspect ratio
            double scale = Math.Ceiling(Math.Min(scaleX, scaleY));

            // Calculate scaled window size
            int scaledWidth = (int)(ScreenWidth * scale);
            int scaledHeight = (int)(ScreenHeight * scale);

            SDL_SetRenderDrawColor(renderer, White.r, White.g, White.b, 255); // Clear the renderer
            SDL_RenderClear(renderer);
            SDL_RenderSetLogicalSize(renderer, scaledWidth, scaledHeight);

            // Draw the scanlines
            for (int y = 0; y < ScreenHeight; y++)
            {
                for (int x = 0; x < ScreenWidth; x++)
                {
                    if (lcd.Buffer[x, y] != 0)
                    {
                        var color = lcd.Buffer[x, y] switch
                        {
                            0 => White,
                            1 => LightGray,
                            2 => DarkGray,
                            3 => Black,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, color.a);
                        SDL_Rect rect = new SDL_Rect
                        {
                            x = (int)(x * scale),
                            y = (int)(y * scale),
                            w = (int)scale,
                            h = (int)scale
                        };
                        SDL_RenderFillRect(renderer, ref rect); // Draw a rectangle
                    }
                }
            }

            SDL_RenderPresent(renderer); // Render the frame;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }

    public static (nint renderer, nint window) InitializeRendererAndWindow(ILogger<Gameboy> logger, EmulatorSettings emulatorSettings)
    {
        if (SDL_Init(SDL_INIT_VIDEO) < 0)
        {
            logger.LogCritical("There was an issue initializing SDL. {SDL_GetError()}", SDL_GetError());
        }

        var window = SDL_CreateWindow("GameboyDotnet",
            SDL_WINDOWPOS_UNDEFINED,
            SDL_WINDOWPOS_UNDEFINED,
            w: emulatorSettings.WindowWidth,
            h: emulatorSettings.WindowHeight,
            SDL_WindowFlags.SDL_WINDOW_SHOWN);

        if (window == IntPtr.Zero)
        {
            logger.LogCritical("There was an issue creating the window. {SDL_GetError()}", SDL_GetError());
        }

        var renderer = SDL_CreateRenderer(window,
            -1,
            SDL_RendererFlags.SDL_RENDERER_ACCELERATED 
            // | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC
            );

        if (renderer == IntPtr.Zero)
        {
            logger.LogCritical("There was an issue creating the renderer. {SDL_GetError()}", SDL_GetError());
        }

        if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) == 0)
        {
            logger.LogCritical("There was an issue initializing SDL2_Image {SDL_image.IMG_GetError()}",
                SDL_image.IMG_GetError());
        }

        return (renderer, window);
    }

    public static void Destroy(IntPtr renderer, IntPtr window)
    {
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }
}