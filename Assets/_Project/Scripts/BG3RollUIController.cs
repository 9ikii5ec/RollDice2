using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BG3RollUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI difficultyClassText;
    [SerializeField] private CanvasGroup rollPromptGroup;
    [SerializeField] private CanvasGroup rollButtonGroup;
    [SerializeField] private CanvasGroup modifierCardGroup;
    [SerializeField] private TextMeshProUGUI modifierFlyText;
    [SerializeField] private CanvasGroup resultBannerGroup;
    [SerializeField] private TextMeshProUGUI resultBannerText;
    [SerializeField] private CanvasGroup continueButtonGroup;
    [SerializeField] private Button continueButton;

    [Header("Animation Positions")]
    [SerializeField] private RectTransform flyTextStartPoint;
    [SerializeField] private RectTransform flyTextEndPoint;

    [Header("Config")]
    [SerializeField] private RollUIConfig config = new RollUIConfig();

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spinSound;
    [SerializeField] private AudioClip modifierAddSound;
    [SerializeField] private AudioClip successSound;

    [Header("Particles (Optional)")]
    [SerializeField] private ParticleSystem successParticles;

    [SerializeField] private BG3DiceParentController diceController;

    [Header("Dice Pulse")]
    [SerializeField] private Transform diceTransform;
    [SerializeField] private Renderer shineRenderer;

    private RectTransform modifierCardRect;
    private RectTransform rollPromptRect;

    private void Awake()
    {
        if (modifierCardGroup != null)
            modifierCardRect = modifierCardGroup.GetComponent<RectTransform>();
        if (rollPromptGroup != null)
            rollPromptRect = rollPromptGroup.GetComponent<RectTransform>();

        HideAll();
    }

    private void Start()
    {
        StartCoroutine(IntroAnimation());
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    private void HideAll()
    {
        SetAlpha(rollPromptGroup, 0f);
        SetAlpha(rollButtonGroup, 0f);
        SetAlpha(modifierCardGroup, 0f);
        SetAlpha(resultBannerGroup, 0f);
        SetAlpha(continueButtonGroup, 0f);
        if (modifierFlyText != null)
            modifierFlyText.alpha = 0f;
    }

    private IEnumerator IntroAnimation()
    {
        yield return new WaitForSeconds(config.introStartDelay);

        // 1. Modifier card: снизу вверх + fade
        if (modifierCardGroup != null && modifierCardRect != null)
        {
            modifierCardGroup.alpha = 0f;
            Vector2 finalPos = modifierCardRect.anchoredPosition;
            modifierCardRect.anchoredPosition = new Vector2(finalPos.x, finalPos.y - config.modifierSlideOffset);

            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(modifierCardRect.DOAnchorPosY(finalPos.y, config.modifierSlideDuration).SetEase(Ease.OutCubic));
            seq.Join(modifierCardGroup.DOFade(1f, config.modifierFadeDuration));
            yield return seq.WaitForCompletion();

            modifierCardGroup.interactable = true;
            modifierCardGroup.blocksRaycasts = true;
        }

        yield return new WaitForSeconds(config.introBetweenDelay);

        // 2. Roll button: fade
        if (rollButtonGroup != null)
        {
            rollButtonGroup.alpha = 0f;
            yield return rollButtonGroup.DOFade(1f, config.rollButtonFadeDuration).SetTarget(this).WaitForCompletion();
            rollButtonGroup.interactable = true;
            rollButtonGroup.blocksRaycasts = true;
        }

        yield return new WaitForSeconds(config.introBetweenDelay);

        // 3. Roll prompt: сверху вниз + fade
        if (rollPromptGroup != null && rollPromptRect != null)
        {
            rollPromptGroup.alpha = 0f;
            Vector2 finalPos = rollPromptRect.anchoredPosition;
            rollPromptRect.anchoredPosition = new Vector2(finalPos.x, finalPos.y + config.rollPromptSlideOffset);

            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(rollPromptRect.DOAnchorPosY(finalPos.y, config.rollPromptSlideDuration).SetEase(Ease.OutCubic));
            seq.Join(rollPromptGroup.DOFade(1f, config.rollPromptFadeDuration));
            yield return seq.WaitForCompletion();

            rollPromptGroup.interactable = true;
            rollPromptGroup.blocksRaycasts = true;
        }

        yield return new WaitForSeconds(config.introBetweenDelay);

        //// 4. Difficulty text: fade
        //if (difficultyClassText != null)
        //{
        //    difficultyClassText.alpha = 0f;
        //    yield return difficultyClassText.DOFade(1f, config.dcTextFadeDuration).SetTarget(this).WaitForCompletion();
        //}

        // 5. Пульс кубика ×2
        if (diceTransform != null)
        {
            yield return new WaitForSeconds(config.dicePunchStartDelay);

            Vector3 originalScale = diceTransform.localScale;
            Vector3 midScale = originalScale * config.dicePunchMidScale;

            Sequence pulse1 = DOTween.Sequence().SetTarget(this);
            pulse1.Append(diceTransform.DOScale(originalScale * config.dicePunchScale1, config.dicePunchDuration1 / 2f).SetEase(Ease.OutBack));
            pulse1.Append(diceTransform.DOScale(midScale, config.dicePunchDuration1 / 2f).SetEase(Ease.OutBack));
            yield return pulse1.WaitForCompletion();

            yield return new WaitForSeconds(config.dicePunchDelay);

            Sequence pulse2 = DOTween.Sequence().SetTarget(this);
            pulse2.Append(diceTransform.DOScale(originalScale * config.dicePunchScale2, config.dicePunchDuration2 / 2f).SetEase(Ease.OutBack));
            pulse2.Append(diceTransform.DOScale(originalScale, config.dicePunchDuration2 / 2f).SetEase(Ease.OutBack));
            yield return pulse2.WaitForCompletion();

            if (shineRenderer != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                shineRenderer.GetPropertyBlock(mpb);
                mpb.SetFloat("_ShinePosition", 3f);
                shineRenderer.SetPropertyBlock(mpb);
                shineRenderer.gameObject.SetActive(true);

                float val = 3f;
                yield return DOTween.To(
                    () => val,
                    x =>
                    {
                        val = x;
                        mpb.SetFloat("_ShinePosition", val);
                        shineRenderer.SetPropertyBlock(mpb);
                    },
                    -1f,
                    config.diceShineDuration
                )
                .SetEase(Ease.Linear)
                .SetTarget(this);
            }
        }
    }

    public void StartRollSequence(int rawDiceResult, int modifierValue, int difficultyClass)
    {
        StopAllCoroutines();
        if (shineRenderer != null)
            shineRenderer.gameObject.SetActive(false);
        StartCoroutine(RollSequenceCoroutine(rawDiceResult, modifierValue, difficultyClass));
    }

    private IEnumerator RollSequenceCoroutine(int rawDiceResult, int modifierValue, int difficultyClass)
    {
        DOTween.Kill(this);

        if (difficultyClassText != null)
            difficultyClassText.alpha = 1f;

        if (rollPromptGroup != null)
        {
            rollPromptGroup.interactable = false;
            rollPromptGroup.blocksRaycasts = false;
        }

        if (rollButtonGroup != null)
        {
            rollButtonGroup.interactable = false;
            rollButtonGroup.blocksRaycasts = false;
            rollButtonGroup.alpha = 0f;
        }

        // 2. Появление модификатора (без мигания если уже виден)
        if (modifierCardGroup != null)
        {
            if (modifierCardGroup.alpha < 1f)
            {
                modifierCardGroup.alpha = 0f;
                yield return modifierCardGroup.DOFade(1f, config.modifierFadeInDuration).SetTarget(this).WaitForCompletion();
            }
            modifierCardGroup.interactable = true;
            modifierCardGroup.blocksRaycasts = true;
        }

        // 3. Бросок кубика
        if (diceController != null)
        {
            if (audioSource != null && spinSound != null)
                audioSource.PlayOneShot(spinSound);
            diceController.RollDice(rawDiceResult);
            while (diceController.IsRolling)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(config.postRollDelay);

        // 4. Летающий текст модификатора
        if (modifierValue != 0 && modifierFlyText != null && flyTextStartPoint != null && flyTextEndPoint != null)
        {
            modifierFlyText.text = (modifierValue > 0 ? "+" : "") + modifierValue.ToString();
            modifierFlyText.transform.position = flyTextStartPoint.position;
            modifierFlyText.alpha = 1f;
            modifierFlyText.transform.localScale = Vector3.one;

            if (audioSource != null && modifierAddSound != null)
                audioSource.PlayOneShot(modifierAddSound);

            Sequence flySeq = DOTween.Sequence().SetTarget(this);
            flySeq.Append(modifierFlyText.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            flySeq.Join(modifierFlyText.transform.DOMove(flyTextEndPoint.position, config.flyTextMoveDuration).SetEase(Ease.InOutQuad));
            yield return flySeq.WaitForCompletion();

            modifierFlyText.alpha = 0f;
        }

        // 5. Кубик мгновенно поворачивается на итоговое число + пульс
        int totalResult = rawDiceResult + modifierValue;
        if (diceController != null)
            diceController.SnapToFace(totalResult);

        if (diceTransform != null)
        {
            Vector3 originalScale = diceTransform.localScale;
            Sequence rollPunch = DOTween.Sequence().SetTarget(this);
            rollPunch.Append(diceTransform.DOScale(originalScale * 1.15f, 0.15f).SetEase(Ease.OutBack));
            rollPunch.Append(diceTransform.DOScale(originalScale, 0.15f).SetEase(Ease.OutBack));
            yield return rollPunch.WaitForCompletion();
        }

        // 6. Модификатор наполовину скрывается
        if (modifierCardGroup != null)
            yield return modifierCardGroup.DOFade(0.5f, 0.2f).SetTarget(this).WaitForCompletion();

        bool isSuccess = totalResult >= difficultyClass;

        if (resultBannerText != null)
        {
            resultBannerText.text = isSuccess ? "SUCCESS" : "FAILURE";
            audioSource.PlayOneShot(successSound);
            resultBannerText.color = isSuccess ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);
        }

        if (resultBannerGroup != null)
        {
            resultBannerGroup.alpha = 0f;
            yield return resultBannerGroup.DOFade(1f, config.bannerFadeDuration).SetTarget(this).WaitForCompletion();
            resultBannerGroup.interactable = true;
            resultBannerGroup.blocksRaycasts = true;
        }

        // 7. Модификатор полностью скрывается
        if (modifierCardGroup != null)
            yield return modifierCardGroup.DOFade(0f, 0.2f).SetTarget(this).WaitForCompletion();

        yield return new WaitForSeconds(config.bannerPostDelay);

        // 6. Кнопка Continue
        if (continueButtonGroup != null)
        {
            continueButtonGroup.alpha = 0f;
            if (continueButton != null) continueButton.interactable = false;
            yield return continueButtonGroup.DOFade(1f, config.continueFadeDuration).SetTarget(this).WaitForCompletion();
            if (continueButton != null) continueButton.interactable = true;
        }
    }

    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
