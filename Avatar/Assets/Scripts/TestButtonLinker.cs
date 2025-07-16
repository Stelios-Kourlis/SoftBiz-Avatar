using UnityEngine;

public class TestButtonLinker : MonoBehaviour
{
    [SerializeField] private string blendShapeName;
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
}
