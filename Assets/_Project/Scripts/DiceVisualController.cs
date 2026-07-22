using System.Collections;
using UnityEngine;
using DG.Tweening;

public class DiceVisualController : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer diceRenderer;

    [Header("Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blurMaterial;

    [Header("Blur")]
    [SerializeField] private string shaderBlurProperty = "_BlurAmount";

    [Header("Particles")]
    [SerializeField] private ParticleSystem impactParticles;

    [Header("Intro Pulse")]
    [SerializeField] private float punchStartDelay = 0.4f;
    [SerializeField] private float punchScale1 = 1.15f;
    [SerializeField] private float punchDuration1 = 0.4f;
    [SerializeField] private float punchMidScale = 1.05f;
    [SerializeField] private float punchDelay = 0.2f;
    [SerializeField] private float punchScale2 = 1.08f;
    [SerializeField] private float punchDuration2 = 0.3f;

    [Header("Roll Pulse")]
    [SerializeField] private float rollPunchScale = 1.15f;
    [SerializeField] private float rollPunchDuration = 0.15f;

    [Header("Shine")]
    [SerializeField] private Renderer shineRenderer;
    [SerializeField] private float shineDuration = 0.7f;
    [SerializeField] private float shineFrom = 3f;
    [SerializeField] private float shineTo = -1f;

    private MaterialPropertyBlock mpb;
    private Material materialInstance;

    private void Start()
    {
        mpb = new MaterialPropertyBlock();

        if (diceRenderer == null) return;

        materialInstance = diceRenderer.material;
        if (normalMaterial == null) normalMaterial = materialInstance;

        SetBlur(0f);
    }

    private void OnDisable()
    {
        SetBlur(0f);
    }

    // --- Blur ---

    public void SetBlur(float amount)
    {
        if (materialInstance != null && materialInstance.HasProperty(shaderBlurProperty))
            materialInstance.SetFloat(shaderBlurProperty, amount);
    }

    public void EnableBlur()
    {
        if (diceRenderer != null && blurMaterial != null)
        {
            diceRenderer.material = blurMaterial;
            materialInstance = diceRenderer.material;
        }
        SetBlur(1f);
    }

    public void DisableBlur()
    {
        if (diceRenderer != null && normalMaterial != null)
        {
            diceRenderer.material = normalMaterial;
            materialInstance = diceRenderer.material;
        }
        SetBlur(0f);
    }

    // --- Particles ---

    public void PlayImpact()
    {
        if (impactParticles != null)
            impactParticles.Play();
    }

    // --- Intro Pulse ---

    public Coroutine PlayIntroPulse(MonoBehaviour host)
    {
        return host.StartCoroutine(IntroPulseSequence());
    }

    private IEnumerator IntroPulseSequence()
    {
        yield return new WaitForSeconds(punchStartDelay);

        Vector3 original = transform.localScale;
        Vector3 mid = original * punchMidScale;

        Sequence p1 = DOTween.Sequence().SetTarget(this);
        p1.Append(transform.DOScale(original * punchScale1, punchDuration1 / 2f).SetEase(Ease.OutBack));
        p1.Append(transform.DOScale(mid, punchDuration1 / 2f).SetEase(Ease.OutBack));
        yield return p1.WaitForCompletion();

        yield return new WaitForSeconds(punchDelay);

        Sequence p2 = DOTween.Sequence().SetTarget(this);
        p2.Append(transform.DOScale(original * punchScale2, punchDuration2 / 2f).SetEase(Ease.OutBack));
        p2.Append(transform.DOScale(original, punchDuration2 / 2f).SetEase(Ease.OutBack));
        yield return p2.WaitForCompletion();
    }

    // --- Roll Pulse ---

    public Coroutine PlayRollPulse(MonoBehaviour host)
    {
        return host.StartCoroutine(RollPulseSequence());
    }

    private IEnumerator RollPulseSequence()
    {
        Vector3 original = transform.localScale;
        Sequence seq = DOTween.Sequence().SetTarget(this);
        seq.Append(transform.DOScale(original * rollPunchScale, rollPunchDuration).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(original, rollPunchDuration).SetEase(Ease.OutBack));
        yield return seq.WaitForCompletion();
    }

    // --- Shine ---

    public Coroutine ShowShine(MonoBehaviour host)
    {
        return host.StartCoroutine(ShineSequence());
    }

    private IEnumerator ShineSequence()
    {
        if (shineRenderer == null) yield break;

        shineRenderer.gameObject.SetActive(true);
        mpb.SetFloat("_ShinePosition", shineFrom);
        shineRenderer.SetPropertyBlock(mpb);

        float val = shineFrom;
        yield return DOTween.To(
            () => val,
            x =>
            {
                val = x;
                mpb.SetFloat("_ShinePosition", val);
                shineRenderer.SetPropertyBlock(mpb);
            },
            shineTo,
            shineDuration
        )
        .SetEase(Ease.Linear)
        .SetTarget(this);
    }

    public void HideShine()
    {
        if (shineRenderer != null)
            shineRenderer.gameObject.SetActive(false);
    }

    // --- Scale Reset ---

    public void ResetScale()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
    }
}
