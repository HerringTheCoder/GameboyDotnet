using System.Runtime.InteropServices;
using GameboyDotnet.Sound;
using static SDL2.SDL;

namespace GameboyDotnet.SDL;

public class SdlAudio
{
    private readonly AudioBuffer _audioBuffer;
    private uint _audioDevice;
    private SDL_AudioCallback _audioCallbackDelegate;

    public SdlAudio(AudioBuffer audioBuffer)
    {
        _audioBuffer = audioBuffer;
    }

    public void Initialize()
    {
        _audioCallbackDelegate = AudioCallbackHandler;
        
        var desiredSpec = new SDL_AudioSpec
        {
            freq = 48000,
            format = AUDIO_F32SYS, //TODO: Check which format should be set
            channels = 2,
            samples = 1024,
            callback = _audioCallbackDelegate
        };

        //Device: null uses default device
        _audioDevice = SDL_OpenAudioDevice(
            device: null, 
            iscapture: 0, 
            ref desiredSpec, 
            out _,
            allowed_changes: 0);
        
        if (_audioDevice <= 0)
        {
            Console.WriteLine($"SDL OpenAudioDevice Error: {SDL_GetError()}");
            return;
        }
        
        SDL_PauseAudioDevice(_audioDevice, 0);
    }
    
    private void AudioCallbackHandler(IntPtr userdata, IntPtr stream, int len)
    {
        int sampleCount = len / sizeof(float);
        float[] samples = new float[sampleCount];
        int offset = 0;
        
        while(offset < sampleCount)
        {
            if (!_audioBuffer.TryDequeueSamples(out float[]? sampleBatch) || sampleBatch == null)
            {
                break;
            }

            int samplesToCopy = Math.Min(sampleBatch.Length, sampleCount - offset);
            Array.Copy(sampleBatch, 0, samples, offset, samplesToCopy);
            offset += samplesToCopy;
        }
        
        Marshal.Copy(samples, 0, stream, samples.Length);
    }

    public void Cleanup()
    {
        SDL_CloseAudioDevice(_audioDevice);
        SDL_Quit();
    }
}
