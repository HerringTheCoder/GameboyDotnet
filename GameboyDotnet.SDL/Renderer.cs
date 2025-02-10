using System.Diagnostics;
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
    
    private static IntPtr _font;
    
    public static void RenderStates(ref IntPtr renderer, ref IntPtr window, byte[,] frame, string fpsText)
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
            
            SDL_RenderSetLogicalSize(renderer, scaledWidth, scaledHeight);
            SDL_SetRenderDrawColor(renderer, DarkGray.r, DarkGray.g, DarkGray.b, 128); // Clear the renderer
            SDL_RenderClear(renderer); //Clear everything with black color
            
            // Draw the Gameboy screen area as a white rectangle
            // Draw the Gameboy screen area as a white rectangle
            SDL_Rect gameboyScreenRect = new SDL_Rect
            {
                x = 0,
                y = 0,
                w = scaledWidth,
                h = scaledHeight
            };
            SDL_SetRenderDrawColor(renderer, White.r, White.g, White.b, White.a);
            SDL_RenderFillRect(renderer, ref gameboyScreenRect);

            // Draw the scanlines
            for (int y = 0; y < ScreenHeight; y++)
            {
                for (int x = 0; x < ScreenWidth; x++)
                {
                    if (frame[x, y] != 0)
                    {
                        var color = frame[x, y] switch
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

            // Display FPS
            RenderText(ref renderer, fpsText, 15, 50);

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
            SDL_WindowFlags.SDL_WINDOW_SHOWN
            | SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            | SDL_WindowFlags.SDL_WINDOW_VULKAN
            );

        if (window == IntPtr.Zero)
        {
            logger.LogCritical("There was an issue creating the window. {SDL_GetError()}", SDL_GetError());
        }

        var renderer = SDL_CreateRenderer(window,
            -1,
            SDL_RendererFlags.SDL_RENDERER_ACCELERATED
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
        
        if(SDL_ttf.TTF_Init() < 0)
        {
            logger.LogCritical("There was an issue initializing SDL2_TTF {SDL_ttf.TTF_GetError()}", SDL_ttf.TTF_GetError());
        }
        
        _font = SDL_ttf.TTF_OpenFont("arial.ttf", 16); // Load font
        if (_font == IntPtr.Zero)
        {
            logger.LogCritical("Failed to load font: {SDL_ttf.TTF_GetError()}", SDL_ttf.TTF_GetError());
        }

        return (renderer, window);
    }

    public static void Destroy(IntPtr renderer, IntPtr window)
    {
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }

    public static void RenderText(ref IntPtr renderer, string text, int x, int y)
    {
        if (_font == IntPtr.Zero) return; // Ensure font is loaded

        SDL_Color redColor = new SDL_Color { r = 255, g = 0, b = 0, a = 255 };

        IntPtr surface = SDL_ttf.TTF_RenderText_Solid(_font, text, redColor);
        if (surface == IntPtr.Zero) return;

        IntPtr texture = SDL_CreateTextureFromSurface(renderer, surface);
        if (texture == IntPtr.Zero)
        {
            SDL_FreeSurface(surface);
            return;
        }

        SDL_QueryTexture(texture, out _, out _, out int textWidth, out int textHeight);
        SDL_Rect textRect = new SDL_Rect { x = x, y = y, w = textWidth*2, h = textHeight*2 };

        SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref textRect);

        SDL_FreeSurface(surface);
        SDL_DestroyTexture(texture);
    }
}