using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class AvatarBlendKeysController : MonoBehaviour
{
    [SerializeField] private float BLINK_DURATION = 0.2f;
    [SerializeField] private float BLINK_INTERVAL = 2f;

    private SkinnedMeshRenderer eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh;
    private readonly List<Coroutine> talkingCoroutines = new(3);
    private readonly List<Tween> activeTalkingTweens = new(3);

    void Awake()
    {
        eyeMesh = transform.Find("Eye_Mesh").GetComponent<SkinnedMeshRenderer>();
        eyeAoMesh = transform.Find("EyeAO_Mesh").GetComponent<SkinnedMeshRenderer>();
        eyelashMesh = transform.Find("Eyelash_Mesh").GetComponent<SkinnedMeshRenderer>();
        headMesh = transform.Find("Head_Mesh").GetComponent<SkinnedMeshRenderer>();
        teethMesh = transform.Find("Teeth_Mesh").GetComponent<SkinnedMeshRenderer>();
        tongueMesh = transform.Find("Tongue_Mesh").GetComponent<SkinnedMeshRenderer>();

        SkinnedMeshRenderer[] meshes = { eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh };
        string[] meshNames = { "Eye_Mesh", "EyeAO_Mesh", "Eyelash_Mesh", "Head_Mesh", "Teeth_Mesh", "Tongue_Mesh" };

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] == null)
            {
                Debug.LogWarning($"{meshNames[i]} is null!");
            }
        }

        StartCoroutine(StartBlinking());
    }

    // private void AnimationStateChanged(AvatarAnimationController.States state)
    // {
    //     if (state == AvatarAnimationController.States.Talking)
    //         StartTalking();
    //     else
    //         StopTalking();

    //     if (state == AvatarAnimationController.States.Thinking)
    //         EyesLookUp();
    //     else
    //         EyesLookDown();
    // }

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

    private void TryApplyBlendShapeWeight(SkinnedMeshRenderer smr, string shapeName, float weight)
    {
        if (!HasBlendShape(smr, shapeName)) return;
        int index = smr.sharedMesh.GetBlendShapeIndex(shapeName);
        smr.SetBlendShapeWeight(index, weight);
    }

    private void TryApplyBlendShapeWeightToAll(string shapeName, float weight)
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

    public void BlendStartTalking()
    {
        talkingCoroutines.Clear();
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthOpen", 0, 50, 15, 0.05f, 0.05f)));
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthFunnel", 0, 70, 40, 0.2f, 0.1f)));
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthPucker", 0, 100, 40, 0.15f, 0.1f)));
    }

    public void BlendStopTalking()
    {
        IEnumerator StopTalkingCoroutines()
        {
            foreach (var tween in activeTalkingTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }

            foreach (var coroutine in talkingCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            yield return null;
            talkingCoroutines.Clear();
            TryApplyBlendShapeWeightToAll("mouthOpen", 0);
            TryApplyBlendShapeWeightToAll("mouthFunnel", 0);
            TryApplyBlendShapeWeightToAll("mouthPucker", 0);
            yield return null;
        }

        StartCoroutine(StopTalkingCoroutines());
    }

    private IEnumerator BlendTalkShapeOverTime(string blendShapeName, int min, int max, int minDiff, float duration, float interval)
    {
        int currentWeight = 0;
        Tween talkTween = null;

        while (true)
        {
            int newWeight;
            do
            {
                newWeight = UnityEngine.Random.Range(min, max);
            } while (Mathf.Abs(currentWeight - newWeight) < minDiff);

            talkTween = DOTween.To(() => currentWeight, weight =>
            {
                TryApplyBlendShapeWeightToAll(blendShapeName, weight);
            }, newWeight, duration).SetEase(Ease.InOutCubic);

            activeTalkingTweens.Add(talkTween);
            yield return talkTween.WaitForCompletion();
            activeTalkingTweens.Remove(talkTween);

            currentWeight = newWeight;
            yield return new WaitForSeconds(interval);
        }
    }

    public void BlendEyesLookUp()
    {
        DOTween.To(() => 0f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
        }, 50f, 0.2f).WaitForCompletion();
    }

    public void BlendEyesLookDown()
    {
        DOTween.To(() => 50f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
        }, 0f, 0.2f).WaitForCompletion();
    }
}
