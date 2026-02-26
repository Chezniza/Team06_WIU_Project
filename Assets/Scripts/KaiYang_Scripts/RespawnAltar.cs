using System.Collections;
using UnityEngine;


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

    private bool isRespawning = false;


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

   
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}
