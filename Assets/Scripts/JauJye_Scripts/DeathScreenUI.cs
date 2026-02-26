using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("UI Elements")]
    [SerializeField] private Image _deathImage;   // your chinese character image
    [SerializeField] private Button _respawnButton;
    [SerializeField] private CanvasGroup _deathImageGroup;
    [SerializeField] private CanvasGroup _respawnButtonGroup;

    [Header("Timing")]
    [SerializeField] private float _imageFadeInDuration = 1.5f;
    [SerializeField] private float _delayBeforeButton = 1.0f;
    [SerializeField] private float _buttonFadeInDuration = 0.8f;

    [Header("Input Handler")]
    [SerializeField] private InputHandler _inputHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _panel.SetActive(false);
        _respawnButton.onClick.AddListener(OnRespawnClicked);
    }

    public void Show()
    {
        _panel.SetActive(true);
        _inputHandler?.LockControls();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Start fully invisible
        _deathImageGroup.alpha = 0f;
        _respawnButtonGroup.alpha = 0f;
        _respawnButton.interactable = false;

        StartCoroutine(FadeSequence());
    }

    public void Hide()
    {
        StopAllCoroutines();
        _panel.SetActive(false);
        _inputHandler?.UnlockControls();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator FadeSequence()
    {
        // Fade in death image
        yield return StartCoroutine(FadeCanvasGroup(_deathImageGroup, 0f, 1f, _imageFadeInDuration));

        // Wait before showing button
        yield return new WaitForSeconds(_delayBeforeButton);

        // Fade in respawn button
        _respawnButton.interactable = true;
        yield return StartCoroutine(FadeCanvasGroup(_respawnButtonGroup, 0f, 1f, _buttonFadeInDuration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so it works if timescale = 0
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    public bool IsOpen() => _panel.activeSelf;

    private void OnRespawnClicked()
    {
        RespawnAltar.Instance?.Respawn();
    }
}