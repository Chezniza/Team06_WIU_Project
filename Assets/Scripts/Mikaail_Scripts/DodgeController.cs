using UnityEngine;
using System.Collections;

public class DodgeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private ComboController comboController;
    [SerializeField] private BlockController blockController;
    [SerializeField] private Animator animator;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 4f;
    [SerializeField] private float minDodgeDistance = 2f;
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private float staminaCost = 20f;
    [SerializeField] private float dodgeCooldown = 0.6f;

    [Header("Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dodgeClip;
    [SerializeField] private GameObject dodgeEffectPrefab;

    private bool isDodging;
    private bool onCooldown;

    public bool IsDodging => isDodging;

    public void TryDodge(Vector3 direction)
    {
        if (isDodging || onCooldown)
            return;

        // Cannot dodge while attacking
        if (comboController != null && comboController.IsAttacking)
            return;

        // Cannot dodge while blocking
        if (blockController != null && blockController.IsBlocking)
            return;

        // Cannot dodge if stamina fails
        if (!staminaSystem.UseStamina(staminaCost))
            return;

        StartCoroutine(PerformDodge(direction.normalized));
    }

    private IEnumerator PerformDodge(Vector3 direction)
    {
        isDodging = true;
        onCooldown = true;

        animator.SetTrigger("Dodge");

        if (audioSource && dodgeClip)
            audioSource.PlayOneShot(dodgeClip);

        if (dodgeEffectPrefab)
            Instantiate(dodgeEffectPrefab, transform.position, Quaternion.identity);

        float distance = Mathf.Max(minDodgeDistance, dodgeDistance);
        float speed = distance / dodgeDuration;

        float timer = 0f;

        while (timer < dodgeDuration)
        {
            controller.Move(direction * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        isDodging = false;

        // Cooldown timer
        yield return new WaitForSeconds(dodgeCooldown);
        onCooldown = false;
    }
}