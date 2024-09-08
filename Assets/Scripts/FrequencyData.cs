[System.Serializable]
public struct FrequencyData
{
    public float Frequency;
    public float Amplitude;
    public float Phase;

    public FrequencyData(float frequency, float amplitude, float offset)
    {
        Frequency = frequency;
        Amplitude = amplitude;
        Phase = offset;
    }
}