using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BG3DiceParentController : MonoBehaviour
{
    [Header("Объекты")]
    [SerializeField] private Transform diceMesh;
    [SerializeField] private Renderer diceRenderer;
    [SerializeField] private Transform[] faceTransforms = new Transform[20];

    [Header("Камера и эффекты")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spinSound;
    [SerializeField] private AudioClip impactSound;

    [Header("Настройки размытия (Blur)")]
    [Tooltip("Имя свойства шейдера для размытия (обычно задаётся в Shader Graph)")]
    [SerializeField] private string shaderBlurProperty = "_BlurAmount";
    [Tooltip("Максимальное значение размытия при вращении")]
    [SerializeField] private float maxBlurAmount = 1.0f;

    [Header("Материалы: переключение между нормальным и размытием")]
    [Tooltip("При завершении кубик переключается между нормальным/размытием на этот материал")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blurMaterial;

    [Header("Границы перемещения (границы XZ)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-4f, -4f);
    [SerializeField] private Vector2 maxBounds = new Vector2(4f, 4f);

    [Header("Настройки броска")]
    [SerializeField] private float rollDuration = 2.0f;
    [SerializeField] private float initialMoveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 1500f;
    public bool IsRolling => isRolling;

    [Tooltip("Длительность возврата к начальной позиции (в секундах)")]
    [SerializeField] private float returnDuration = 0.5f;
    [Tooltip("Длительность привязки (для точности, чтобы кубик не продолжал крутиться)")]
    [SerializeField] private float snapDuration = 0.15f;

    [Header("Визуализация границ (Gizmos)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.8f);

    [Header("Ссылки")]
    [SerializeField] private BG3RollUIController uiController;

    private Vector3 startPosition;
    private Vector3 currentVelocity;
    private bool isRolling = false;
    private Material targetMaterialInstance;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        startPosition = transform.position;

        if (diceRenderer == null && diceMesh != null)
            diceRenderer = diceMesh.GetComponent<Renderer>();

        if (diceRenderer != null)
        {
            targetMaterialInstance = diceRenderer.material;
            if (normalMaterial == null) normalMaterial = targetMaterialInstance;
        }

        SetShaderBlur(0f);
    }

    private void OnDisable()
    {
        SetShaderBlur(0f);
    }

    public void RollDiceWrapper()
    {
        int randomFace = Random.Range(1, 21);
        Debug.Log("Выпало число: " + randomFace);

        if (uiController != null)
            uiController.StartRollSequence(randomFace, 1, 10);
        else
            RollDice(randomFace);
    }

    public void RollDice(int resultValue)
    {
        if (isRolling) return;
        int index = Mathf.Clamp(resultValue - 1, 0, 19);
        StartCoroutine(AnimateBG3Roll(index));
    }

    public void SnapToFace(int resultValue)
    {
        if (isRolling) return;
        int index = Mathf.Clamp(resultValue - 1, 0, 19);

        Transform targetFace = faceTransforms[index];
        Vector3 toCameraDir = -targetCamera.transform.forward;
        Vector3 cameraUpDir = targetCamera.transform.up;
        Quaternion desiredWorldOrientation = Quaternion.LookRotation(cameraUpDir, toCameraDir);
        Quaternion finalRotation = desiredWorldOrientation * Quaternion.Inverse(targetFace.localRotation);

        transform.position = startPosition;
        diceMesh.rotation = finalRotation;

        if (diceRenderer && normalMaterial)
        {
            diceRenderer.material = normalMaterial;
            targetMaterialInstance = diceRenderer.material;
        }

        SetShaderBlur(0f);
    }

    private IEnumerator AnimateBG3Roll(int targetIndex)
    {
        isRolling = true;

        if (diceRenderer && blurMaterial)
        {
            diceRenderer.material = blurMaterial;
            targetMaterialInstance = diceRenderer.material;
        }

        SetShaderBlur(maxBlurAmount);

        Transform targetFace = faceTransforms[targetIndex];
        Vector3 toCameraDir = -targetCamera.transform.forward;
        Vector3 cameraUpDir = targetCamera.transform.up;
        Quaternion desiredWorldOrientation = Quaternion.LookRotation(cameraUpDir, toCameraDir);
        Quaternion finalD20Rotation = desiredWorldOrientation * Quaternion.Inverse(targetFace.localRotation);

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

            if (elapsed < snapStartTime)
            {
                diceMesh.Rotate(new Vector3(1.2f, 1.5f, 0.8f) * rotationSpeed * dt, Space.Self);
                SetShaderBlur(maxBlurAmount);
            }
            else
            {
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
                SetShaderBlur(Mathf.Lerp(maxBlurAmount, 0f, tRot));
            }

            yield return null;
        }

        transform.position = startPosition;
        diceMesh.rotation = finalD20Rotation;

        if (diceRenderer && normalMaterial)
        {
            diceRenderer.material = normalMaterial;
            targetMaterialInstance = diceRenderer.material;
        }

        SetShaderBlur(0f);

        if (impactParticles) impactParticles.Play();

        yield return StartCoroutine(PulseImpact(1.2f, 0.15f));

        isRolling = false;
    }

    private void SetShaderBlur(float amount)
    {
        if (targetMaterialInstance != null && targetMaterialInstance.HasProperty(shaderBlurProperty))
            targetMaterialInstance.SetFloat(shaderBlurProperty, amount);
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
            transform.position = pos;
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

    public void Reset()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
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
