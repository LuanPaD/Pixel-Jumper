using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 basePosition;
    private float phase;
    private bool collected;

    private void Start()
    {
        basePosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        Vector3 p = basePosition;
        p.y += Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
        transform.position = p;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponent<PlayerController>() == null) return;
        collected = true;

        if (GameManager.Instance != null) GameManager.Instance.AddCoin();
        Effects.Puff(transform.position, new Color(1f, 0.85f, 0.25f), 8, 3f, 0.1f, 0.4f);
        Destroy(gameObject);
    }
}
