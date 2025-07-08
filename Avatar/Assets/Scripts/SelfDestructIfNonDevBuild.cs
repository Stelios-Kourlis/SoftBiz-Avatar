using UnityEngine;

public class SelfDestructIfNonDevBuild : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
#pragma warning disable UNT0001 // Empty Unity message
    void Awake()
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        Destroy(gameObject);
#endif
    }

}
