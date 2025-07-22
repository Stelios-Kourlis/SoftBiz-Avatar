using UnityEngine;

/// <summary>
/// This class acts as a bridge between the UI Debug buttons and the AvatarAnimationController.
/// <para>It allows the the buttons to target a consistent GameObject (eg. Canvas) instead of the model GameObject
/// so the model can be freely switched with another. Always name the model GameObject "model" so this script can find it</para>
/// </summary>
public class TestButtonLinker : MonoBehaviour
{
    private GameObject model;

    void Start()
    {
        model = GameObject.Find("model");
    }

    public void StartIdle()
    {
        model.GetComponent<AvatarAnimationController>().StartIdle();
    }

    public void StartThinking()
    {
        model.GetComponent<AvatarAnimationController>().StartThinking();
    }

    public void StartTalking()
    {
        model.GetComponent<AvatarAnimationController>().StartTalking();
    }

    public void StartLipSync(string jsonData)
    {
        model.GetComponent<AvatarBlendKeysController>().StartLipSync(jsonData);
    }
}
