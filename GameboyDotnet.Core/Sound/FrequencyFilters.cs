namespace GameboyDotnet.Sound;

public static class FrequencyFilters
{
    /// <summary>
    /// Debug only
    /// </summary>
    /// <returns></returns>
    public static bool IsHighPassFilterActive = true;
    
    private static float _capacitorLeft = 0f;
    private static float _capacitorRight = 0f;
    private const float CapacitorChargingRatio = 0.995948f;

    public static void ApplyHighPassFilter(ref float leftSample, ref float rightSample, bool isAnyDacEnabled = true)
    {
        if (!IsHighPassFilterActive)
        {
            return;
        }
        
        float leftOutput = leftSample - _capacitorLeft;
        float rightOutput = rightSample - _capacitorRight;
        
        _capacitorLeft = leftSample - leftOutput * CapacitorChargingRatio;
        _capacitorRight = rightSample - rightOutput * CapacitorChargingRatio;
        
        leftSample = leftOutput;
        rightSample = rightOutput;
    }
}