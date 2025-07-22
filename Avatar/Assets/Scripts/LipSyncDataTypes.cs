using System;

/// <summary>
/// Represents the data structure for RhubarbLipSync JSON output. Used to parse the JSON lip sync data
/// </summary>
[Serializable]
public class LipSyncData
{
    public Metadata metadata;
    public MouthCue[] mouthCues;
}

/// <summary>
/// Represents metadata for the lip sync data, such as sound file and duration.
/// </summary>
[Serializable]
public class Metadata
{
    public string soundFile;
    public float duration;
}

/// <summary>
/// Represents a single mouth cue in the lip sync data, containing the start and end times and the value of the cue.
/// </summary>
[Serializable]
public class MouthCue
{
    public float start;
    public float end;
    public string value;
}

