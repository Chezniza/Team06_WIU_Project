using System.Collections;
using UnityEngine;

public class RespawnAltar : MonoBehaviour
{
    public static RespawnAltar Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Damageable playerDamageable;
    [SerializeField] private PlayerController playerController;

    [Header("Respawn Settings")]
    [SerializeField] private float spawnHeightOffset = 1f;

    [Header("Optional VFX")]
    [SerializeField] private GameObject respawnVFXPrefab;

    private bool _waitingForRespawn = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerDamageable == null && playerObject != null)
            playerDamageable = playerObject.GetComponent<Damageable>();

        if (playerController == null && playerObject != null)
            playerController = playerObject.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (_waitingForRespawn || playerObject == null) return;

        // Player went inactive = death animation finished
        if (!playerObject.activeSelf)
        {
            _waitingForRespawn = true;
            DeathScreenUI.Instance?.Show();
        }
    }

    // Called by DeathScreenUI respawn button
    public void Respawn()
    {
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // Hide death screen first
        DeathScreenUI.Instance?.Hide();

        yield return null; // one frame gap

        Vector3 respawnPos = transform.position + Vector3.up * spawnHeightOffset;

        CharacterController cc = playerObject.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerObject.transform.position = respawnPos;
        if (cc != null) cc.enabled = true;

        playerObject.SetActive(true);

        if (playerDamageable != null)
            playerDamageable.ResetHealth();

        if (respawnVFXPrefab != null)
            Instantiate(respawnVFXPrefab, respawnPos, Quaternion.identity);

        Debug.Log($"[RespawnAltar] Player respawned at {respawnPos}");
        _waitingForRespawn = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}