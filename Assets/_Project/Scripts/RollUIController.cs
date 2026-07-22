using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class RollUIController : MonoBehaviour
{
    [Header("UI Groups")]
    [SerializeField] private CanvasGroup rollPromptGroup;
    [SerializeField] private CanvasGroup rollButtonGroup;
    [SerializeField] private TextMeshProUGUI difficultyClassText;

    [Header("Intro Positions")]
    [SerializeField] private RectTransform rollPromptRect;

    [Header("Config")]
    [SerializeField] private RollUIConfig config;

    [Header("Sub Controllers")]
    [SerializeField] private ModifierController modifierController;
    [SerializeField] private DiceVisualController diceVisual;

    private void Awake()
    {
        HideAll();
    }

    private void Start()
    {
        StartCoroutine(IntroSequence());
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    private void HideAll()
    {
        SetAlpha(rollPromptGroup, 0f);
        SetAlpha(rollButtonGroup, 0f);
        if (modifierController != null) modifierController.HideAll();
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(config.introStartDelay);

        if (modifierController != null)
        {
            modifierController.SetIntroPosition(config.modifierSlideOffset);
            yield return modifierController.AnimateIntroSlide(this);
        }

        yield return new WaitForSeconds(config.introBetweenDelay);

        yield return FadeInGroup(rollButtonGroup, config.rollButtonFadeDuration);

        yield return new WaitForSeconds(config.introBetweenDelay);

        yield return SlideInPrompt();

        yield return new WaitForSeconds(config.introBetweenDelay);

        if (diceVisual != null)
        {
            yield return diceVisual.PlayIntroPulse(this);
            yield return diceVisual.ShowShine(this);
        }
    }

    public void PrepareForRoll()
    {
        StopAllCoroutines();
        DOTween.Kill(this);

        if (diceVisual != null) diceVisual.HideShine();

        if (difficultyClassText != null)
            difficultyClassText.alpha = 1f;

        SetInteractive(rollPromptGroup, false);
        SetInteractive(rollButtonGroup, false);
        if (rollButtonGroup != null) rollButtonGroup.alpha = 0f;
    }

    private IEnumerator FadeInGroup(CanvasGroup group, float duration)
    {
        if (group == null) yield break;

        group.alpha = 0f;
        yield return group.DOFade(1f, duration).SetTarget(this).WaitForCompletion();
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private IEnumerator SlideInPrompt()
    {
        if (rollPromptGroup == null || rollPromptRect == null) yield break;

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

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private static void SetInteractive(CanvasGroup group, bool interactive)
    {
        if (group == null) return;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }
}
