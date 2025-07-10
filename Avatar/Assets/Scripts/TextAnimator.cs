using System.Collections;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Text))]
public class TextAnimator : MonoBehaviour
{

    private TMP_Text tmpText;
    private TMP_TextInfo textInfo;
    public int TmpCharCount => GetComponent<TMP_Text>().textInfo.characterCount;
    public float jumpHeight = 2f;
    public float jumpDuration = 0.25f;
    public float delayBetweenLoops = 0.5f;
    public float fadeInDuration = 0.5f;
    public float delayBetweenFades = 0.3f;
    Vector3[][] originalVertices;
    public bool isAnimationRunning = false;
    private Coroutine textBounceCoroutine;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void AnimateTextBouncing(float waveDuration, int startIndex = 0)
    {
        textBounceCoroutine = StartCoroutine(AnimateTextBounce(waveDuration, startIndex));
    }

    public void StopAnimatingTextBounce()
    {
        if (textBounceCoroutine != null) StopCoroutine(textBounceCoroutine);
        isAnimationRunning = false;
        tmpText.ForceMeshUpdate();
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = originalVertices[i];
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    public IEnumerator AnimateTextLoop(float waveDuration)
    {
        isAnimationRunning = true;
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < originalVertices.Length; i++)
            originalVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];

        // float waveDuration = jumpDuration + (textInfo.characterCount - 1) * delayBetweenJumps;
        float delayBetweenJumps = (waveDuration - jumpDuration) / (textInfo.characterCount - 1);
        if (delayBetweenJumps < 0) delayBetweenJumps = 0.01f;
        Debug.Log($"Wave Duration: {waveDuration}");

        while (true)
        {
            float elapsedTime = 0f;

            while (elapsedTime < waveDuration)
            {
                if (tmpText == null)
                {
                    yield break;
                }

                tmpText.ForceMeshUpdate();
                textInfo = tmpText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    var charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float charStartTime = i * delayBetweenJumps;
                    float charElapsed = elapsedTime - charStartTime;
                    if (charElapsed < 0 || charElapsed > jumpDuration) continue;

                    float progress = Mathf.Clamp01(charElapsed / jumpDuration);
                    float jumpOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] sourceVertices = originalVertices[materialIndex];
                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    Vector3 offset = new(0, jumpOffset, 0);

                    for (int j = 0; j < 4; j++)
                        destinationVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + offset;
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(delayBetweenLoops); // delay between waves
        }
    }

    private IEnumerator AnimateTextBounce(float waveDuration, int startIndex = 0)
    {
        isAnimationRunning = true;
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < originalVertices.Length; i++)
            originalVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];

        float delayBetweenJumps = (waveDuration - jumpDuration) / (textInfo.characterCount - 1);
        if (delayBetweenJumps < 0) delayBetweenJumps = 0.05f;
        // waveDuration = jumpDuration + (textInfo.characterCount - 1) * delayBetweenJumps;

        float elapsedTime = 0f;
        Debug.Log($"Wave Duration: {waveDuration}");

        while (elapsedTime < waveDuration)
        {
            if (tmpText == null)
            {
                yield break;
            }

            tmpText.ForceMeshUpdate();
            textInfo = tmpText.textInfo;

            for (int i = startIndex; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                float charStartTime = i * delayBetweenJumps;
                float charElapsed = elapsedTime - charStartTime;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                var colors = tmpText.textInfo.meshInfo[materialIndex].colors32;

                if (charElapsed < 0)
                {
                    for (int color = 0; color < 4; color++)
                    {
                        colors[vertexIndex + color].a = 0; // set alpha to 0
                    }
                    continue; // skip this character until its time to animate
                }

                for (int color = 0; color < 4; color++)
                {
                    colors[vertexIndex + color].a = 255;
                }

                if (charElapsed > jumpDuration) continue;

                float progress = Mathf.Clamp01(charElapsed / jumpDuration);
                float jumpOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;


                Vector3[] sourceVertices = originalVertices[materialIndex];
                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = new(0, jumpOffset, 0);

                for (int j = 0; j < 4; j++)
                    destinationVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + offset;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            elapsedTime += Time.deltaTime;
            tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }

        // Ensure final position reset to original (optional)
        tmpText.ForceMeshUpdate();
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = originalVertices[i];
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        isAnimationRunning = false;
    }

}
