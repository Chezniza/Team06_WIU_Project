using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hides the cursor
        Cursor.visible = false;

        // Locks the cursor
        Cursor.lockState = CursorLockMode.Locked;

        //// Releases the cursor
        //Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
