using System.Collections;
using UnityEngine;

public class BG3DiceParentController : MonoBehaviour
{
    [Header("Иерархия")]
    public Transform diceMesh;
    public Transform[] faceTransforms = new Transform[20];

    [Header("Камера и Эффекты")]
    public Camera targetCamera;
    public ParticleSystem impactParticles;
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip bounceSound;
    public AudioClip impactSound;

    [Header("Границы перемещения (Плоскость XZ)")]
    public Vector2 minBounds = new Vector2(-4f, -4f);
    public Vector2 maxBounds = new Vector2(4f, 4f);

    [Header("Настройки Броска")]
    public float rollDuration = 2.0f;
    public float initialMoveSpeed = 10f;
    public float rotationSpeed = 1500f;

    [Tooltip("Время возврата кубика в центр экрана (в секундах)")]
    public float returnDuration = 0.5f;
    [Tooltip("Время финального доворота грани. Чем меньше, тем резче защелкивается число.")]
    public float snapDuration = 0.15f;

    [Header("Визуализация Границ (Gizmos)")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0f, 1f, 0.4f, 0.8f);

    private Vector3 startPosition;
    private Vector3 currentVelocity;
    private bool isRolling = false;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        startPosition = transform.position;
    }

    public void RollDiceWrapper()
    {
        int randomFace = Random.Range(1, 21);
        Debug.Log("Выпало число: " + randomFace);
        RollDice(randomFace);
    }

    public void RollDice(int resultValue)
    {
        if (isRolling) return;
        int index = Mathf.Clamp(resultValue - 1, 0, 19);
        StartCoroutine(AnimateBG3Roll(index));
    }

    private IEnumerator AnimateBG3Roll(int targetIndex)
    {
        isRolling = true;

        if (audioSource && spinSound) audioSource.PlayOneShot(spinSound);

        // Расчет итогового поворота под вашу развертку осей (Y - наружу)
        Quaternion targetFaceWorldRot = Quaternion.LookRotation(targetCamera.transform.up, -targetCamera.transform.forward);
        Quaternion finalD20Rotation = targetFaceWorldRot * Quaternion.Inverse(faceTransforms[targetIndex].localRotation);

        // Направление вылета
        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir2D.x, 0f, randomDir2D.y) * initialMoveSpeed;

        float elapsed = 0f;

        // Тайминги начала разных фаз
        float returnStartTime = rollDuration - returnDuration;
        float snapStartTime = rollDuration - snapDuration;

        bool isReturning = false;
        Vector3 returnStartPos = Vector3.zero;

        bool isSnapping = false;
        Quaternion snapStartRot = Quaternion.identity;

        while (elapsed < rollDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // --- 1. ЛОГИКА ПЕРЕМЕЩЕНИЯ ---
            if (elapsed < returnStartTime)
            {
                transform.position += currentVelocity * dt;
                CheckXZBoundariesAndBounce();
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, dt * 1.5f);
            }
            else
            {
                if (!isReturning)
                {
                    isReturning = true;
                    returnStartPos = transform.position;
                }

                float tMove = Mathf.Clamp01((elapsed - returnStartTime) / returnDuration);
                float easeMove = 1f - Mathf.Pow(1f - tMove, 3f); // Кубическое затухание скорости к центру
                transform.position = Vector3.Lerp(returnStartPos, startPosition, easeMove);
            }

            // --- 2. ЛОГИКА ВРАЩЕНИЯ ---
            if (elapsed < snapStartTime)
            {
                // Постоянное, БЕЗ ЗАТУХАНИЯ вращение по всем трем осям (эффект кувыркания)
                diceMesh.Rotate(new Vector3(1.2f, 1.5f, 0.8f) * rotationSpeed * dt, Space.Self);
            }
            else
            {
                // Резкий, почти мгновенный доворот на нужную грань
                if (!isSnapping)
                {
                    isSnapping = true;
                    snapStartRot = diceMesh.rotation;
                }

                float tRot = Mathf.Clamp01((elapsed - snapStartTime) / snapDuration);
                diceMesh.rotation = Quaternion.Slerp(snapStartRot, finalD20Rotation, tRot);
            }

            yield return null;
        }

        // Гарантированная фиксация в конце
        transform.position = startPosition;
        diceMesh.rotation = finalD20Rotation;

        // Эффект удара
        if (impactParticles) impactParticles.Play();
        if (audioSource && impactSound) audioSource.PlayOneShot(impactSound);

        yield return StartCoroutine(PulseImpact(1.2f, 0.15f));

        isRolling = false;
    }

    private void CheckXZBoundariesAndBounce()
    {
        Vector3 pos = transform.position;
        bool bounced = false;

        if (pos.x < minBounds.x) { pos.x = minBounds.x; currentVelocity.x *= -1f; bounced = true; }
        if (pos.x > maxBounds.x) { pos.x = maxBounds.x; currentVelocity.x *= -1f; bounced = true; }
        if (pos.z < minBounds.y) { pos.z = minBounds.y; currentVelocity.z *= -1f; bounced = true; }
        if (pos.z > maxBounds.y) { pos.z = maxBounds.y; currentVelocity.z *= -1f; bounced = true; }

        if (bounced)
        {
            transform.position = pos;
            if (audioSource && bounceSound) audioSource.PlayOneShot(bounceSound);
        }
    }

    private IEnumerator PulseImpact(float scaleMult, float duration)
    {
        Vector3 originalScale = diceMesh.localScale;
        Vector3 targetScale = originalScale * scaleMult;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration / 2f);
            diceMesh.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration / 2f);
            diceMesh.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        diceMesh.localScale = originalScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        float centerX = (minBounds.x + maxBounds.x) / 2f;
        float centerZ = (minBounds.y + maxBounds.y) / 2f;
        float posY = transform != null ? transform.position.y : 0f;
        Vector3 center = new Vector3(centerX, posY, centerZ);

        float width = Mathf.Abs(maxBounds.x - minBounds.x);
        float depth = Mathf.Abs(maxBounds.y - minBounds.y);
        Vector3 size = new Vector3(width, 0.05f, depth);

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.15f);
        Gizmos.DrawCube(center, size);
    }
}