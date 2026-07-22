using System.Collections;
using UnityEngine;
using DG.Tweening;

public class DiceController : MonoBehaviour
{
    [Header("Mesh")]
    [SerializeField] private Transform diceMesh;
    [SerializeField] private Transform[] faceTransforms = new Transform[20];

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Bounds")]
    [SerializeField] private Vector2 minBounds = new Vector2(-4f, -4f);
    [SerializeField] private Vector2 maxBounds = new Vector2(4f, 4f);

    [Header("Roll Settings")]
    [SerializeField] private float rollDuration = 2.0f;
    [SerializeField] private float initialMoveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 1500f;
    [SerializeField] private float returnDuration = 0.5f;
    [SerializeField] private float snapDuration = 0.15f;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.8f);

    [Header("Visual (optional)")]
    [SerializeField] private DiceVisualController visualController;

    public bool IsRolling => isRolling;

    private Vector3 startPosition;
    private Vector3 currentVelocity;
    private bool isRolling;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        startPosition = transform.position;
    }

    public void RollDice(int resultValue)
    {
        if (isRolling) return;

        transform.DOKill();
        transform.localScale = Vector3.one;

        if (visualController != null)
            visualController.ResetScale();

        int index = Mathf.Clamp(resultValue - 1, 0, 19);
        StartCoroutine(AnimateRoll(index));
    }

    public void SnapToFace(int resultValue)
    {
        if (isRolling) return;

        int index = Mathf.Clamp(resultValue - 1, 0, 19);
        Quaternion finalRotation = CalculateFinalRotation(index);

        transform.position = startPosition;
        diceMesh.rotation = finalRotation;

        if (visualController != null)
            visualController.DisableBlur();
    }

    private Quaternion CalculateFinalRotation(int targetIndex)
    {
        Transform targetFace = faceTransforms[targetIndex];
        Vector3 toCameraDir = -targetCamera.transform.forward;
        Vector3 cameraUpDir = targetCamera.transform.up;
        Quaternion desiredWorldOrientation = Quaternion.LookRotation(cameraUpDir, toCameraDir);
        return desiredWorldOrientation * Quaternion.Inverse(targetFace.localRotation);
    }

    private IEnumerator AnimateRoll(int targetIndex)
    {
        isRolling = true;

        if (visualController != null)
            visualController.EnableBlur();

        Quaternion finalRotation = CalculateFinalRotation(targetIndex);
        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir2D.x, 0f, randomDir2D.y) * initialMoveSpeed;

        float elapsed = 0f;
        float returnStartTime = rollDuration - returnDuration;
        float snapStartTime = rollDuration - snapDuration;
        bool isReturning = false;
        bool isSnapping = false;
        Vector3 returnStartPos = Vector3.zero;
        Quaternion snapStartRot = Quaternion.identity;

        while (elapsed < rollDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            UpdateMovement(elapsed, returnStartTime, ref isReturning, ref returnStartPos);
            UpdateRotation(elapsed, snapStartTime, ref isSnapping, ref snapStartRot, finalRotation);

            yield return null;
        }

        FinishRoll(finalRotation);
        isRolling = false;
    }

    private void UpdateMovement(float elapsed, float returnStartTime, ref bool isReturning, ref Vector3 returnStartPos)
    {
        if (elapsed < returnStartTime)
        {
            transform.position += currentVelocity * Time.deltaTime;
            BounceOffBounds();
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 1.5f);
        }
        else
        {
            if (!isReturning)
            {
                isReturning = true;
                returnStartPos = transform.position;
            }

            float t = Mathf.Clamp01((elapsed - returnStartTime) / returnDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(returnStartPos, startPosition, ease);
        }
    }

    private void UpdateRotation(float elapsed, float snapStartTime, ref bool isSnapping, ref Quaternion snapStartRot, Quaternion finalRotation)
    {
        if (elapsed < snapStartTime)
        {
            diceMesh.Rotate(new Vector3(1.2f, 1.5f, 0.8f) * rotationSpeed * Time.deltaTime, Space.Self);
            if (visualController != null) visualController.SetBlur(1f);
        }
        else
        {
            if (!isSnapping)
            {
                isSnapping = true;
                snapStartRot = diceMesh.rotation;
                if (visualController != null) visualController.DisableBlur();
            }

            float t = Mathf.Clamp01((elapsed - snapStartTime) / snapDuration);
            diceMesh.rotation = Quaternion.Slerp(snapStartRot, finalRotation, t);
            if (visualController != null) visualController.SetBlur(Mathf.Lerp(1f, 0f, t));
        }
    }

    private void FinishRoll(Quaternion finalRotation)
    {
        transform.position = startPosition;
        diceMesh.rotation = finalRotation;

        if (visualController != null)
        {
            visualController.DisableBlur();
            visualController.PlayImpact();
        }
    }

    private void BounceOffBounds()
    {
        Vector3 pos = transform.position;
        bool bounced = false;

        if (pos.x < minBounds.x) { pos.x = minBounds.x; currentVelocity.x *= -1f; bounced = true; }
        if (pos.x > maxBounds.x) { pos.x = maxBounds.x; currentVelocity.x *= -1f; bounced = true; }
        if (pos.z < minBounds.y) { pos.z = minBounds.y; currentVelocity.z *= -1f; bounced = true; }
        if (pos.z > maxBounds.y) { pos.z = maxBounds.y; currentVelocity.z *= -1f; bounced = true; }

        if (bounced) transform.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        float centerX = (minBounds.x + maxBounds.x) / 2f;
        float centerZ = (minBounds.y + maxBounds.y) / 2f;
        float posY = transform != null ? transform.position.y : 0f;
        Vector3 center = new Vector3(centerX, posY, centerZ);
        Vector3 size = new Vector3(Mathf.Abs(maxBounds.x - minBounds.x), 0.05f, Mathf.Abs(maxBounds.y - minBounds.y));

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.15f);
        Gizmos.DrawCube(center, size);
    }
}
