using UnityEngine;

/// <summary>Bandeira final: encostar termina a fase.</summary>
[RequireComponent(typeof(Collider2D))]
public class Goal : MonoBehaviour
{
    private bool reached;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || other.GetComponent<PlayerController>() == null) return;
        reached = true;

        Effects.Puff(transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.3f), 24, 5f, 0.14f, 0.9f, 6f);
        if (GameManager.Instance != null) GameManager.Instance.Win();
    }
}
