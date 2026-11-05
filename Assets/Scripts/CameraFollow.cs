using UnityEngine;

/// <summary>Câmera 2D suave que segue o jogador, olha um pouco à frente e treme em impactos.</summary>
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.8f);
    [SerializeField] private float lookAhead = 1.5f;

    [Header("Limites da fase")]
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 98f;
    [SerializeField] private float minY = 1f;

    private Camera cam;
    private Rigidbody2D targetBody;
    private Vector3 velocity;
    private float lookX;
    private float shakeTime;
    private float shakeDuration = 1f;
    private float shakeMagnitude;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null) targetBody = target.GetComponent<Rigidbody2D>();
        Snap();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (targetBody != null && Mathf.Abs(targetBody.velocity.x) > 0.5f)
            lookX = Mathf.Lerp(lookX, Mathf.Sign(targetBody.velocity.x) * lookAhead, Time.deltaTime * 2f);

        Vector3 desired = Clamp(DesiredPosition());
        Vector3 pos = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        pos.z = -10f;

        if (shakeTime > 0f)
        {
            pos += (Vector3)(Random.insideUnitCircle * shakeMagnitude * (shakeTime / shakeDuration));
            shakeTime -= Time.deltaTime;
        }

        transform.position = pos;
    }

    private Vector3 DesiredPosition()
    {
        return new Vector3(target.position.x + offset.x + lookX, target.position.y + offset.y, -10f);
    }

    private Vector3 Clamp(Vector3 p)
    {
        float halfWidth = cam.orthographicSize * cam.aspect;
        float lo = minX + halfWidth;
        float hi = maxX - halfWidth;
        p.x = lo > hi ? (lo + hi) * 0.5f : Mathf.Clamp(p.x, lo, hi);
        p.y = Mathf.Max(p.y, minY);
        return p;
    }

    /// <summary>Posiciona a câmera imediatamente, sem suavização.</summary>
    public void Snap()
    {
        if (target == null) return;
        lookX = 0f;
        velocity = Vector3.zero;
        transform.position = Clamp(DesiredPosition());
    }

    public void Shake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTime = duration;
    }
}
