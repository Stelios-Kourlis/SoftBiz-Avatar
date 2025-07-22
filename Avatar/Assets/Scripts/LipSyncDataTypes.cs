using System;

[Serializable]
public class Metadata
{
    public string soundFile;
    public float duration;
}

[Serializable]
public class MouthCue
{
    public float start;
    public float end;
    public string value;
}

[Serializable]
public class LipSyncData
{
    public Metadata metadata;
    public MouthCue[] mouthCues;
}
