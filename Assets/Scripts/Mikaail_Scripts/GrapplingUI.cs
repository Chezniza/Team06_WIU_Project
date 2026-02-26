using UnityEngine;
using UnityEngine.UI;

public class GrappleUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private GrappleController grappleController;

    private bool lastState = false;

    void Update()
    {
        if (grappleController == null || image == null) return;

        bool shouldShow = grappleController.HasTarget;

        // Only change state when it actually changes
        if (shouldShow != lastState)
        {
            image.enabled = shouldShow;
            lastState = shouldShow;

            Debug.Log("Grapple UI " + (shouldShow ? "ENABLED" : "DISABLED"));
        }
    }
}