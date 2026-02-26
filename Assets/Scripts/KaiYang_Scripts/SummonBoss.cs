using UnityEngine;
using UnityEngine.Events;

public class SummonBoss : MonoBehaviour
{
   
    public BoxCollider triggerArea;
    public GameObject[] Platform;
    public GameObject boss;
    public GameObject bossHpBar;


    [SerializeField] private InputHandler _inputHandler;

    private bool _playerInRange = false;

    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;



    private void Start()
    {
        foreach (var Platform in Platform)
        {
           Platform.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            onTriggerEnter.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            onTriggerExit.Invoke();
        }
    }

    private void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.T))
        {

            SpawnBoss();
          
        }
    }

    private void SpawnBoss()
    {
        boss.SetActive(true);
        bossHpBar.SetActive(true);
        
        foreach (var Platform in Platform)
        {
            
           
            Platform.SetActive(true);
        }
    }
  
}
