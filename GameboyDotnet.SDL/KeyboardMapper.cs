namespace GameboyDotnet.SDL;

using static SDL2.SDL;

public class KeyboardMapper
{
    private readonly IDictionary<SDL_Keycode, string> _keymap;


    public KeyboardMapper(IDictionary<SDL_Keycode, string> keymap)
    {
        _keymap = keymap;
    }

    public bool TryGetGameboyKey(SDL_Keycode keyValue, out string result)
    {
        return _keymap.TryGetValue(keyValue, out result);
    }
}