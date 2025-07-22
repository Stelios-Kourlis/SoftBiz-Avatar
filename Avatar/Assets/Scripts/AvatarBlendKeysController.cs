using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class AvatarBlendKeysController : MonoBehaviour
{
    [SerializeField] private float BLINK_DURATION = 0.2f;
    [SerializeField] private float BLINK_INTERVAL = 2f;
    [SerializeField] private float LIP_SYNC_MAX_WEIGHT = 70f;
    [SerializeField] private float LIP_SYNC_TRANSITION_TIME = 0.05f;
    [SerializeField] private Ease LIP_SYNC_FADE_IN_EASE = Ease.InCubic;
    [SerializeField] private Ease LIP_SYNC_FADE_OUT_EASE = Ease.OutCubic;


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
        if (GetBlendShapeWeight("eyesLookUp") == 50f) return;
        DOTween.To(() => 0f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
            TryApplyBlendShapeWeightToAll("browOuterUpRight", weight * 2);
        }, 50f, 0.2f).WaitForCompletion();
    }

    public void BlendEyesLookDown()
    {
        if (GetBlendShapeWeight("eyesLookUp") == 0f) return;
        DOTween.To(() => 50f, weight =>
        {
            TryApplyBlendShapeWeightToAll("eyesLookUp", weight);
            TryApplyBlendShapeWeightToAll("browOuterUpRight", weight * 2);
        }, 0f, 0.2f).WaitForCompletion();
    }

    public LipSyncData ParseLipSyncData(string json)
    {
        LipSyncData data = JsonUtility.FromJson<LipSyncData>(json);

        Dictionary<string, string> visemeMap = new()
        {
            { "A", "PP" },
            { "B", "kk" }, //Yes the kk viseme is lowercase
            { "C", "E" }, //?
            { "D", "AA" },
            { "E", "O" },
            { "F", "U" },
            { "G", "FF" },
            { "H", "" },
            { "X", "sil"}
        };

        foreach (MouthCue cue in data.mouthCues)
        {
            if (visemeMap.TryGetValue(cue.value, out string viseme))
            {
                Debug.Log($"Mapped {cue.value} to viseme_{viseme}");
                cue.value = "viseme_" + viseme;
            }
            else
            {
                Debug.LogWarning($"Unknown viseme type: {cue.value}. Defaulting to 'sil'");
                cue.value = "sil"; // Default to silence if unknown
            }
        }

        return data;
    }

    public void StartLipSync(string jsonData)
    {
        LipSyncData lsd = ParseLipSyncData(jsonData); //Bad abbrevation, I know
        StartCoroutine(LipSyncCoroutine());
        Debug.Log($"Starting lip sync with {lsd.mouthCues.Length} cues.");

        IEnumerator LipSyncCoroutine()
        {
            MouthCue previousCue = null;
            foreach (MouthCue cue in lsd.mouthCues)
            {
                float duration = cue.end - cue.start;
                Sequence seq = DOTween.Sequence();

                Debug.Log($"Cue: {cue.value}, Start: {cue.start}, End: {cue.end}, Duration: {duration}");
                Debug.Log($"Previous Cue: {previousCue?.value}");
                if (previousCue != null && previousCue.value != cue.value)
                {
                    seq.Join(DOTween.To(() => LIP_SYNC_MAX_WEIGHT, weight =>
                    {
                        TryApplyBlendShapeWeightToAll(previousCue.value, weight);
                    }, 0f, LIP_SYNC_TRANSITION_TIME).SetEase(Ease.OutCubic));
                }

                seq.Join(DOTween.To(() => 0f, weight =>
                {
                    TryApplyBlendShapeWeightToAll(cue.value, weight);
                }, LIP_SYNC_MAX_WEIGHT, LIP_SYNC_TRANSITION_TIME).SetEase(Ease.OutCubic));

                seq.AppendInterval(Mathf.Max(duration - LIP_SYNC_TRANSITION_TIME, 0.1f));

                seq.Play();
                yield return seq.WaitForCompletion();

                previousCue = cue;
            }
        }
    }

}


