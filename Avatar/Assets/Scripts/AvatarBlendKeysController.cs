using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Used to control the BlendShapes of the model
/// </summary>
public class AvatarBlendKeysController : MonoBehaviour
{
    /// <summary> The duration of a single a blink. This is the combined time for both the opening and closing of the eyes. </summary>
    [SerializeField] private float BLINK_DURATION = 0.2f;
    /// <summary> The interval from the completion of a blink to the beggining of the next one </summary>
    [SerializeField] private float BLINK_INTERVAL = 2f;
    /// <summary> The weight of a viseme BlendShape when talking </summary>
    [SerializeField] private float LIP_SYNC_MAX_WEIGHT = 70f;
    /// <summary> The duration of the transition from 1 viseme BlendShape to the next one </summary>
    [SerializeField] private float LIP_SYNC_TRANSITION_TIME = 0.1f;
    /// <summary> The ease type of the transtition when entering a new viseme BlendShape </summary>
    [SerializeField] private Ease LIP_SYNC_FADE_IN_EASE = Ease.InCubic;
    /// <summary> The ease type of the transtition when exiting the previous viseme BlendShape </summary>
    [SerializeField] private Ease LIP_SYNC_FADE_OUT_EASE = Ease.OutCubic;

    /// <summary>
    /// The SkinnedMeshRenderers that have the BlendShapes for the eyes, eyelids, eyelashes, head, teeth and tongue.
    /// This assumes the model is an Avaturn T2 model.
    /// </summary>
    private SkinnedMeshRenderer eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh;

    [Obsolete("This field has been deprecated since it does not provide any lip sync. Use StartLipSync instead.")]
    private readonly List<Coroutine> talkingCoroutines = new(3);
    [Obsolete("This field has been deprecated since it does not provide any lip sync. Use StartLipSync instead.")]
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

    /// <summary>
    /// Check if the specified SkinnedMeshRenderers have the specified blend shape.
    /// </summary>
    /// <param name="smr">The SkinnedMeshRenderer to check</param>
    /// <param name="shapeName">The name of the BlendShape to check for</param>
    /// <returns>If the SkinnedMeshRenderer had a BlendShape named shapeName</returns>
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

    /// <summary>
    /// Try to apply the specified weight to the specified BlendShape of the SkinnedMeshRenderer.
    /// If the BlendShape does not exist, it will do nothing.
    /// </summary>
    /// <param name="smr">The SkinnedMeshRenderer that has the BlendShape</param>
    /// <param name="shapeName">The name of the BlendShape</param>
    /// <param name="weight">The new weight of the BlendShape</param>
    private void TryApplyBlendShapeWeight(SkinnedMeshRenderer smr, string shapeName, float weight)
    {
        if (!HasBlendShape(smr, shapeName)) return;
        int index = smr.sharedMesh.GetBlendShapeIndex(shapeName);
        smr.SetBlendShapeWeight(index, weight);
    }

    /// <summary>
    /// Apply the specified weight to the specified BlendShape to all SkinnedMeshRenderers that have it.
    /// Any SkinnedMeshRenderers that do not have the BlendShape will have no change applied to them.
    /// </summary>
    /// <param name="shapeName">The name of the BlendShape</param>
    /// <param name="weight">The new weight of the BlendShape</param>
    private void TryApplyBlendShapeWeightToAll(string shapeName, float weight)
    {
        TryApplyBlendShapeWeight(eyeMesh, shapeName, weight);
        TryApplyBlendShapeWeight(eyeAoMesh, shapeName, weight);
        TryApplyBlendShapeWeight(eyelashMesh, shapeName, weight);
        TryApplyBlendShapeWeight(headMesh, shapeName, weight);
        TryApplyBlendShapeWeight(teethMesh, shapeName, weight);
        TryApplyBlendShapeWeight(tongueMesh, shapeName, weight);
    }

    /// <summary>
    /// Get the current weight of the specified BlendShape from all SkinnedMeshRenderers.
    /// This assumes that all SkinnedMeshRenderers that have this BlendShape have the same weight for it.
    /// </summary>
    /// <param name="shapeName">The name of the BlendShape</param>
    /// <returns>The value of the BlendShape on the first SkinnedMeshRenderer that has it. Null if no SkinnedMeshRenderers have the BlendShape</returns>
    private float? GetBlendShapeWeight(string shapeName)
    {

        SkinnedMeshRenderer[] meshes = { eyeMesh, eyeAoMesh, eyelashMesh, headMesh, teethMesh, tongueMesh };

        foreach (SkinnedMeshRenderer mesh in meshes)
        {
            if (HasBlendShape(mesh, shapeName))
                return mesh.GetBlendShapeWeight(mesh.sharedMesh.GetBlendShapeIndex(shapeName));
        }
        return null;
    }

    /// <summary>
    /// Periodically blink the avatar. To tweak blinking parameters, change BLINK_DURATION and BLINK_INTERVAL preferably from the Unity Editor.
    /// </summary>
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
            }, 100f, BLINK_DURATION / 2).WaitForCompletion();

            yield return DOTween.To(() => 100f, weight =>
            {
                TryApplyBlendShapeWeightToAll("eyeBlinkLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeBlinkRight", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintLeft", weight);
                TryApplyBlendShapeWeightToAll("eyeSquintRight", weight);
            }, 0f, BLINK_DURATION / 2).WaitForCompletion();

            yield return new WaitForSeconds(BLINK_INTERVAL);
        }
    }

    [Obsolete("This function has been deprecated since it does not provide any lip sync. Use StartLipSync instead.")]
    public void BlendStartTalking()
    {
        talkingCoroutines.Clear();
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthOpen", 0, 50, 15, 0.05f, 0.05f)));
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthFunnel", 0, 70, 40, 0.2f, 0.1f)));
        talkingCoroutines.Add(StartCoroutine(BlendTalkShapeOverTime("mouthPucker", 0, 100, 40, 0.15f, 0.1f)));
    }

    [Obsolete("This function has been deprecated since it does not provide any lip sync. Use StartLipSync instead.")]
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
            activeTalkingTweens.Clear();
            TryApplyBlendShapeWeightToAll("mouthOpen", 0);
            TryApplyBlendShapeWeightToAll("mouthFunnel", 0);
            TryApplyBlendShapeWeightToAll("mouthPucker", 0);
            yield return null;
        }

        StartCoroutine(StopTalkingCoroutines());
    }

    [Obsolete("This function has been deprecated due to its parent function being deprecated. See BlendStartTalking for more details.")]
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

    /// <summary>
    /// Smoothly blend the eyes to look upwards and to raise the right eyebrow. Used to better represent a thining pose
    /// </summary>
    public void BlendEyesLookUp()
    {
        if (GetBlendShapeWeight("eyesLookUp") == 50f) return;
        DOTween.To(() => 0f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
            TryApplyBlendShapeWeightToAll("browOuterUpRight", weight * 2);
        }, 50f, 0.2f).WaitForCompletion();
    }

    /// <summary>
    /// Smoothly blend the eyes to look straight again and lower the right eyebrow. Used to recover from a thining pose
    /// </summary>
    public void BlendEyesLookDown()
    {
        if (GetBlendShapeWeight("eyesLookUp") == 0f) return;
        DOTween.To(() => 50f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
            TryApplyBlendShapeWeightToAll("browOuterUpRight", weight * 2);
        }, 0f, 0.2f).WaitForCompletion();
    }

    /// <summary>
    /// Parse the RhubarbLipSync JSON results into a LipSyncData object.
    /// <para>Using any other file except a RhubarbLipSync with JSON output may result in undefined behaviour and is not recommended.
    /// This method will do nothing to handle any potential errors from invalid inputs</para>
    /// </summary>
    /// <param name="json">The RhubarbLipSync JSON output</param>
    /// <returns>The parsed data as a LipSyncData object.</returns>
    private LipSyncData ParseLipSyncData(string json)
    {
        LipSyncData data = JsonUtility.FromJson<LipSyncData>(json);

        Dictionary<string, string> visemeMap = new()
        {
            { "A", "PP" },
            { "B", "E" },
            { "C", "aa" }, //Not sure aa is the optimal connection here
            { "D", "aa" }, //Also yes aa is lowercase, not a typo
            { "E", "O" },
            { "F", "U" },
            { "G", "FF" },
            { "H", "aa" },
            { "X", "sil"}
        };

        foreach (MouthCue cue in data.mouthCues)
        {
            if (visemeMap.TryGetValue(cue.value, out string viseme))
                cue.value = "viseme_" + viseme;
            else
                Debug.LogWarning($"Unknown viseme '{cue.value}' in lip sync data");
        }

        return data;
    }

    /// <summary>
    /// Starts lip sync animation based on the provided JSON data generated by RhubarbLipSync. Use json as output from the RhubarbLipSync tool.
    /// <para>Supports RhubarbLipSync extended shapes</para>
    /// </summary>
    /// <param name="jsonData">The content of the JSON file</param>
    public void StartLipSync(string jsonData)
    {
        LipSyncData lsd = ParseLipSyncData(jsonData); //Bad abbrevation, I know
        StartCoroutine(LipSyncCoroutine(lsd));
        Debug.Log($"Starting lip sync with {lsd.mouthCues.Length} cues.");


    }

    private IEnumerator LipSyncCoroutine(LipSyncData lsd)
    {
        MouthCue previousCue = null;
        foreach (MouthCue cue in lsd.mouthCues)
        {
            float duration = cue.end - cue.start;
            Sequence seq = DOTween.Sequence();

            if (previousCue != null && previousCue.value != cue.value)
            {
                seq.Join(DOTween.To(() => LIP_SYNC_MAX_WEIGHT, weight =>
                {
                    TryApplyBlendShapeWeightToAll(previousCue.value, weight);
                }, 0f, LIP_SYNC_TRANSITION_TIME).SetEase(LIP_SYNC_FADE_OUT_EASE));
            }

            seq.Join(DOTween.To(() => 0f, weight =>
            {
                TryApplyBlendShapeWeightToAll(cue.value, weight);
            }, LIP_SYNC_MAX_WEIGHT, LIP_SYNC_TRANSITION_TIME).SetEase(LIP_SYNC_FADE_IN_EASE));

            if (duration - LIP_SYNC_TRANSITION_TIME > 0)
                seq.AppendInterval(duration - LIP_SYNC_TRANSITION_TIME);

            seq.Play();
            yield return seq.WaitForCompletion();

            previousCue = cue;
        }
    }
}


