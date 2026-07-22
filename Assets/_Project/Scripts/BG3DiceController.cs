using System.Collections;
using UnityEngine;

public class BG3DiceParentController : MonoBehaviour
{
    [Header("Иерархия")]
    public Transform diceMesh;
    public Renderer diceRenderer;
    public Transform[] faceTransforms = new Transform[20];

    [Header("Камера и Эффекты")]
    public Camera targetCamera;
    public ParticleSystem impactParticles;
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip bounceSound;
    public AudioClip impactSound;

    [Header("Настройки Шейдера / Смазывания (Blur)")]
    [Tooltip("Имя свойства размытия в вашем шейдере (если используется Shader Graph)")]
    public string shaderBlurProperty = "_BlurAmount";
    [Tooltip("Максимальная сила размытия при быстром вращении")]
    public float maxBlurAmount = 1.0f;

    [Header("Опционально: Альтернативный Материал для вращения")]
    [Tooltip("Если хотите использовать отдельный смазанный материал/текстуру во время вращения")]
    public Material normalMaterial;
    public Material blurMaterial;

    [Header("Границы перемещения (Плоскость XZ)")]
    public Vector2 minBounds = new Vector2(-4f, -4f);
    public Vector2 maxBounds = new Vector2(4f, 4f);

    [Header("Настройки Броска")]
    public float rollDuration = 2.0f;
    public float initialMoveSpeed = 10f;
    public float rotationSpeed = 1500f;

    [Tooltip("Время возврата кубика в центр экрана (в секундах)")]
    public float returnDuration = 0.5f;
    [Tooltip("Время фиксации грани (чем меньше, тем резче защелкивается число)")]
    public float snapDuration = 0.15f;

    [Header("Визуализация Границ (Gizmos)")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0f, 1f, 0.4f, 0.8f);

    private Vector3 startPosition;
    private Vector3 currentVelocity;
    private bool isRolling = false;
    private Material targetMaterialInstance;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        startPosition = transform.position;

        if (diceRenderer == null && diceMesh != null)
        {
            diceRenderer = diceMesh.GetComponent<Renderer>();
        }

        if (diceRenderer != null)
        {
            targetMaterialInstance = diceRenderer.material;
            if (normalMaterial == null) normalMaterial = targetMaterialInstance;
        }

        // ВАЖНО: При запуске гарантированно отключаем блюр
        SetShaderBlur(0f);
    }

    private void OnDisable()
    {
        // Безопасный сброс размытия, если объект выключился во время броска
        SetShaderBlur(0f);
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

        if (diceRenderer && blurMaterial)
        {
            diceRenderer.material = blurMaterial;
            targetMaterialInstance = diceRenderer.material;
        }

        SetShaderBlur(maxBlurAmount);

        // =========================================================================
        // ТОЧНЫЙ РАСЧЕТ ДЛЯ ВАШЕГО ПРЕФАБА (где ось Y пустышек смотрит из грани)
        // =========================================================================
        Transform targetFace = faceTransforms[targetIndex];

        // 1. Вектор от куба к камере (куда должна смотреть грань)
        Vector3 toCameraDir = -targetCamera.transform.forward;

        // 2. Вектор "верха" экрана камеры
        Vector3 cameraUpDir = targetCamera.transform.up;

        // 3. Создаем базовую ориентацию: Y смотрит на камеру, Z смотрит вверх
        Quaternion desiredWorldOrientation = Quaternion.LookRotation(cameraUpDir, toCameraDir);

        // 4. Поворачиваем меш кубика с учетом локального поворота целевой грани
        Quaternion finalD20Rotation = desiredWorldOrientation * Quaternion.Inverse(targetFace.localRotation);
        // =========================================================================

        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir2D.x, 0f, randomDir2D.y) * initialMoveSpeed;

        float elapsed = 0f;
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

            // --- 1. ПЕРЕМЕЩЕНИЕ ---
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
                float easeMove = 1f - Mathf.Pow(1f - tMove, 3f);
                transform.position = Vector3.Lerp(returnStartPos, startPosition, easeMove);
            }

            // --- 2. ВРАЩЕНИЕ И СМАЗЫВАНИЕ ---
            if (elapsed < snapStartTime)
            {
                // Быстрое вращение — держим блюр активным
                diceMesh.Rotate(new Vector3(1.2f, 1.5f, 0.8f) * rotationSpeed * dt, Space.Self);
                SetShaderBlur(maxBlurAmount);
            }
            else
            {
                // Фаза защелкивания (Snap) — плавно тушим блюр до 0
                if (!isSnapping)
                {
                    isSnapping = true;
                    snapStartRot = diceMesh.rotation;

                    if (diceRenderer && normalMaterial && blurMaterial)
                    {
                        diceRenderer.material = normalMaterial;
                        targetMaterialInstance = diceRenderer.material;
                    }
                }

                float tRot = Mathf.Clamp01((elapsed - snapStartTime) / snapDuration);
                diceMesh.rotation = Quaternion.Slerp(snapStartRot, finalD20Rotation, tRot);

                // Убираем блюр к моменту остановки
                SetShaderBlur(Mathf.Lerp(maxBlurAmount, 0f, tRot));
            }

            yield return null;
        }

        // Гарантированный сброс в 0 после завершения корутины
        transform.position = startPosition;
        diceMesh.rotation = finalD20Rotation;

        if (diceRenderer && normalMaterial)
        {
            diceRenderer.material = normalMaterial;
            targetMaterialInstance = diceRenderer.material;
        }

        // ВАЖНО: Жесткий сброс размытия в ноль
        SetShaderBlur(0f);

        if (impactParticles) impactParticles.Play();
        if (audioSource && impactSound) audioSource.PlayOneShot(impactSound);

        yield return StartCoroutine(PulseImpact(1.2f, 0.15f));

        isRolling = false;
    }

    private void SetShaderBlur(float amount)
    {
        if (targetMaterialInstance != null && targetMaterialInstance.HasProperty(shaderBlurProperty))
        {
            targetMaterialInstance.SetFloat(shaderBlurProperty, amount);
        }
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