using System.Collections;
using UnityEngine;

public class PillarHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 50;

    [Header("Rise Animation")]
    [SerializeField] private float riseDistance = 2f;   // how far below ground it starts
    [SerializeField] private float riseDuration  = 0.6f; // how long the rise takes

    private int    currentHealth;
    private BossAI boss;
    private bool   isDead = false;

    private Damageable damageable;

    public void Init(BossAI bossRef)
    {
        boss       = bossRef;
        damageable = GetComponent<Damageable>();

        if (damageable == null)
            Debug.LogError($"[PillarHealth] No Damageable found on {gameObject.name}!");

        StartCoroutine(RiseFromGround());
    }

    private IEnumerator RiseFromGround()
    {
        Vector3 finalPos = transform.position;
        Vector3 startPos = finalPos - Vector3.up * riseDistance;

        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration); // smooth ease in/out
            transform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }

        transform.position = finalPos;
    }

    private void Update()
    {
        if (isDead || damageable == null) return;

        if (damageable.GetHealth() <= 0)
        {
            isDead = true;
            boss?.OnPillarDestroyed(this);
            Destroy(gameObject, 0.5f);
        }
    }
}
