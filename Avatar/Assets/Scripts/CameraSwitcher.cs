using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera closeUpCamera;

    void Start()
    {
        // Start with main camera on
        mainCamera.enabled = true;
        closeUpCamera.enabled = false;
    }

    public void ToggleCameras()
    {
        if (mainCamera.enabled)
            SwitchToCloseUp();
        else
            SwitchToMain();
    }

    public void SwitchToCloseUp()
    {
        closeUpCamera.enabled = true;
        mainCamera.enabled = false;
    }

    public void SwitchToMain()
    {
        mainCamera.enabled = true;
        closeUpCamera.enabled = false;
    }
}