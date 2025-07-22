using UnityEngine;

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
