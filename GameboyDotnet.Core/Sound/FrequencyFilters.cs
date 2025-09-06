namespace GameboyDotnet.Sound;

public static class FrequencyFilters
{
    /// <summary>
    /// Debug only
    /// </summary>
    /// <returns></returns>
    public static bool IsHighPassFilterActive = true;
    
    public static float HighPassFilter(this float pcmSample, ref float capacitor)
    {
        if (!IsHighPassFilterActive)
        {
            return pcmSample;
        }
        
        float output = pcmSample - capacitor;
        // capacitor slowly charges to 'in' via their difference
        capacitor = pcmSample - output * 0.995948f;
        return output;
    }
}