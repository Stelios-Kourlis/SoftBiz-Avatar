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

    public void StartSitting()
    {
        model.GetComponent<AvatarAnimationController>().StartSitting();
    }

    public void StartThinking()
    {
        model.GetComponent<AvatarAnimationController>().StartThinking();
    }

    public void StartTalking()
    {
        model.GetComponent<AvatarAnimationController>().StartTalking();
    }

    public void StartLookingAround()
    {
        model.GetComponent<AvatarAnimationController>().StartLookingAround();
    }

    public void SetBlendShapeWeight(float weight)
    {
        if (blendShapeName == null || blendShapeName == string.Empty)
        {
            Debug.LogWarning("Blend shape name is not set.");
            return;
        }

        if (model.TryGetComponent<AvatarBlendKeysController>(out var blendKeysController))
        {
            blendKeysController.TryApplyBlendShapeWeightToAll(blendShapeName, weight);
        }
        else
        {
            Debug.LogError("AvatarBlendKeysController not found in the scene.");
        }
    }
}
