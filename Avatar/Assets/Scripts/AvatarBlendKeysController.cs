using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(AvatarAnimationController))]
public class AvatarBlendKeysController : MonoBehaviour
{
    [SerializeField] private float BLINK_DURATION = 0.2f;
    [SerializeField] private float BLINK_INTERVAL = 2f;

    private AvatarAnimationController avatarAnimationController;

    private SkinnedMeshRenderer eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh;
    private readonly List<Coroutine> talkingCoroutines = new();

    void Awake()
    {
        eyeMesh = transform.Find("Eye_Mesh").GetComponent<SkinnedMeshRenderer>();
        eyeAoMesh = transform.Find("EyeAO_Mesh").GetComponent<SkinnedMeshRenderer>();
        eyelashMesh = transform.Find("Eyelash_Mesh").GetComponent<SkinnedMeshRenderer>();
        headMesh = transform.Find("Head_Mesh").GetComponent<SkinnedMeshRenderer>();
        teethMesh = transform.Find("Teeth_Mesh").GetComponent<SkinnedMeshRenderer>();
        tongueMesh = transform.Find("Tongue_Mesh").GetComponent<SkinnedMeshRenderer>();

        avatarAnimationController = gameObject.GetComponent<AvatarAnimationController>();

        SkinnedMeshRenderer[] meshes = { eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh };
        string[] meshNames = { "Eye_Mesh", "EyeAO_Mesh", "Eyelash_Mesh", "Head_Mesh", "Teeth_Mesh", "Tongue_Mesh" };

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] == null)
            {
                Debug.LogWarning($"{meshNames[i]} is null!");
            }
        }

        avatarAnimationController.OnStateChanged += AnimationStateChanged;

        StartCoroutine(StartBlinking());
    }

    private void AnimationStateChanged(AvatarAnimationController.States state)
    {
        if (state == AvatarAnimationController.States.Talking)
            StartTalking();
        else
            StopTalking();
    }

    private bool HasBlendShape(SkinnedMeshRenderer smr, string shapeName)
    {
        var mesh = smr.sharedMesh;
        int count = mesh.blendShapeCount;
        for (int i = 0; i < count; i++)
        {
            if (mesh.GetBlendShapeName(i) == shapeName)
                return true;
        }
        return false;
    }

    public void TryApplyBlendShapeWeight(SkinnedMeshRenderer smr, string shapeName, float weight)
    {
        if (!HasBlendShape(smr, shapeName)) return;
        int index = smr.sharedMesh.GetBlendShapeIndex(shapeName);
        smr.SetBlendShapeWeight(index, weight);
    }

    public void TryApplyBlendShapeWeightToAll(string shapeName, float weight)
    {
        TryApplyBlendShapeWeight(eyeMesh, shapeName, weight);
        TryApplyBlendShapeWeight(eyeAoMesh, shapeName, weight);
        TryApplyBlendShapeWeight(eyelashMesh, shapeName, weight);
        TryApplyBlendShapeWeight(headMesh, shapeName, weight);
        TryApplyBlendShapeWeight(teethMesh, shapeName, weight);
        TryApplyBlendShapeWeight(tongueMesh, shapeName, weight);
    }

    private IEnumerator StartBlinking()
    {
        while (true)
        {
            yield return DOTween.To(() => 0f, weight =>
            {
                TryApplyBlendShapeWeightToAll("eyeBlinkLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeBlinkRight", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintRight", weight);
            }, 100f, BLINK_DURATION).WaitForCompletion();

            yield return DOTween.To(() => 100f, weight =>
            {
                TryApplyBlendShapeWeightToAll("eyeBlinkLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeBlinkRight", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintRight", weight);
            }, 0f, BLINK_DURATION).WaitForCompletion();

            yield return new WaitForSeconds(BLINK_INTERVAL);
        }
    }

    public void StartTalking()
    {
        talkingCoroutines.Clear();
        talkingCoroutines.Add(StartCoroutine(BlendShapeOverTime("mouthOpen", 0, 50, 15, 0.05f, 0.05f)));
        talkingCoroutines.Add(StartCoroutine(BlendShapeOverTime("mouthFunnel", 0, 70, 40, 0.2f, 0.1f)));
        talkingCoroutines.Add(StartCoroutine(BlendShapeOverTime("mouthPucker", 0, 100, 40, 0.15f, 0.1f)));
    }

    public void StopTalking()
    {
        IEnumerator StopTalkingCoroutines()
        {
            foreach (var coroutine in talkingCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            yield return null;
            talkingCoroutines.Clear();
            TryApplyBlendShapeWeightToAll("mouthOpen", 0);
            TryApplyBlendShapeWeightToAll("mouthFunnel", 0);
            TryApplyBlendShapeWeightToAll("mouthPucker", 0);
        }

        StartCoroutine(StopTalkingCoroutines());
    }

    private IEnumerator BlendShapeOverTime(string blendShapeName, int min, int max, int minDiff, float duration, float interval)
    {
        int currentWeight = 0;

        while (true)
        {
            int newWeight;
            do
            {
                newWeight = Random.Range(min, max);
            } while (Mathf.Abs(currentWeight - newWeight) < minDiff);

            yield return DOTween.To(() => currentWeight, weight =>
            {
                TryApplyBlendShapeWeightToAll(blendShapeName, weight);
            }, newWeight, duration).SetEase(Ease.InOutCubic).WaitForCompletion();

            currentWeight = newWeight;

            yield return new WaitForSeconds(interval);
        }
    }
}
