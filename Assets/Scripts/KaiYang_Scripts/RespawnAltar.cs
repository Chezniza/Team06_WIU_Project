using System.Collections;
using UnityEngine;

// ============================================================
//  RespawnAltar.cs
//  Place this on your altar GameObject.
//  Watches for player death and respawns them on top of altar.
//
//  SETUP:
//    1. Add this component to your altar GameObject
//    2. Player auto-found via "Player" tag, or assign manually
//    3. Place the altar where you want the player to respawn
//    4. Adjust Spawn Height Offset so player appears above altar surface
// ============================================================

public class RespawnAltar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject      playerObject;
    [SerializeField] private Damageable      playerDamageable;
    [SerializeField] private PlayerController playerController;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay      = 2f;
    [SerializeField] private float spawnHeightOffset = 1f;  // raise if player clips through altar

    [Header("Optional VFX")]
    [SerializeField] private GameObject respawnVFXPrefab;

    // ── State ─────────────────────────────────────────────
    private bool isRespawning = false;

    // ─────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("[RespawnAltar] No player found — tag your player 'Player' or assign manually.");
            return;
        }

        if (playerDamageable == null)
            playerDamageable = playerObject.GetComponent<Damageable>();

        if (playerController == null)
            playerController = playerObject.GetComponent<PlayerController>();
    }

    // ─────────────────────────────────────────────
    // WATCH FOR DEATH
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (isRespawning || playerObject == null) return;

        // OnDeathAnimationFinished() sets the player inactive
        if (!playerObject.activeSelf)
        {
            isRespawning = true;
            StartCoroutine(RespawnSequence());
        }
    }

    // ─────────────────────────────────────────────
    // RESPAWN SEQUENCE
    // ─────────────────────────────────────────────

    private IEnumerator RespawnSequence()
    {
        Debug.Log($"[RespawnAltar] Player died — respawning in {respawnDelay}s...");

        yield return new WaitForSeconds(respawnDelay);

        Vector3 respawnPos = transform.position + Vector3.up * spawnHeightOffset;

        // Must disable CharacterController before moving the transform,
        // otherwise it fights the position change and snaps back
        CharacterController cc = playerObject.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObject.transform.position = respawnPos;

        if (cc != null) cc.enabled = true;

        // Reactivate player — this re-enables input, physics, and animator
        playerObject.SetActive(true);

        // Restore full HP and update healthbar
        if (playerDamageable != null)
            playerDamageable.ResetHealth();

        // Spawn VFX at respawn point if assigned
        if (respawnVFXPrefab != null)
            Instantiate(respawnVFXPrefab, respawnPos, Quaternion.identity);

        Debug.Log($"[RespawnAltar] Player respawned at {respawnPos}");

        isRespawning = false;
    }

    // ─────────────────────────────────────────────
    // GIZMOS — yellow sphere marks the respawn point
    // ─────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}
