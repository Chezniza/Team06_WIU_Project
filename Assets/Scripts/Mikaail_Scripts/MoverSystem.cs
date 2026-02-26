using UnityEngine;
using System.Collections;

public class MoverSystem : MonoBehaviour
{
    [SerializeField] private CharacterController controller;

    public IEnumerator MoveToTarget(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPos = Vector3.Lerp(start, target, t);
            Vector3 move = newPos - transform.position;

            controller.Move(move);

            yield return null;
        }
    }
}