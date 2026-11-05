using UnityEngine;

/// <summary>Perigo (espinhos): encostar mata o jogador.</summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.Kill();
    }
}
