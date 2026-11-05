using UnityEngine;

/// <summary>
/// Camada de fundo com parallax. Fator 1 acompanha a câmera (parece muito distante),
/// fator 0 fica parada no mundo (parece perto).
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField, Range(0f, 1f)] private float factorX = 0.5f;
    [SerializeField, Range(0f, 1f)] private float factorY = 0.9f;

    [Header("Deslocamento automático (nuvens)")]
    [SerializeField] private float autoScroll = 0f;
    [SerializeField] private float wrapWidth = 0f;

    private Vector3 startPosition;
    private Vector3 cameraStart;
    private float drift;

    private void Start()
    {
        if (cam == null) cam = Camera.main.transform;
        startPosition = transform.position;
        cameraStart = cam.position;
    }

    private void LateUpdate()
    {
        drift += autoScroll * Time.deltaTime;
        if (wrapWidth > 0f) drift = Mathf.Repeat(drift, wrapWidth);

        Vector3 delta = cam.position - cameraStart;
        transform.position = new Vector3(
            startPosition.x + delta.x * factorX + drift,
            startPosition.y + delta.y * factorY,
            startPosition.z);
    }
}
