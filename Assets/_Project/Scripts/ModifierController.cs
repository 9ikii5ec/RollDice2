using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ModifierController : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private CanvasGroup cardGroup;
    [SerializeField] private RectTransform cardRect;

    [Header("Fly Text")]
    [SerializeField] private TextMeshProUGUI flyText;
    [SerializeField] private RectTransform flyStartPoint;
    [SerializeField] private RectTransform flyEndPoint;

    [Header("Config")]
    [SerializeField] private RollUIConfig config;

    public void ShowCard()
    {
        if (cardGroup == null) return;
        cardGroup.alpha = 1f;
        cardGroup.interactable = true;
        cardGroup.blocksRaycasts = true;
    }

    public Coroutine AnimateCardFadeIn(MonoBehaviour host)
    {
        return host.StartCoroutine(FadeCardSequence());
    }

    private IEnumerator FadeCardSequence()
    {
        if (cardGroup == null) yield break;

        if (cardGroup.alpha >= 1f) yield break;

        cardGroup.alpha = 0f;
        yield return cardGroup.DOFade(1f, config.modifierFadeInDuration).SetTarget(this).WaitForCompletion();
        cardGroup.interactable = true;
        cardGroup.blocksRaycasts = true;
    }

    public void FadeCardToHalf()
    {
        if (cardGroup != null)
            cardGroup.DOFade(0.5f, 0.2f).SetTarget(this);
    }

    public void HideCard()
    {
        if (cardGroup != null)
            cardGroup.DOFade(0f, 0.2f).SetTarget(this);
    }

    public Coroutine AnimateFlyText(MonoBehaviour host, int modifierValue)
    {
        return host.StartCoroutine(FlyTextSequence(modifierValue));
    }

    private IEnumerator FlyTextSequence(int modifierValue)
    {
        if (flyText == null || flyStartPoint == null || flyEndPoint == null) yield break;
        if (modifierValue == 0) yield break;

        flyText.text = (modifierValue > 0 ? "+" : "") + modifierValue.ToString();
        flyText.transform.position = flyStartPoint.position;
        flyText.alpha = 1f;
        flyText.transform.localScale = Vector3.one;

        Sequence flySeq = DOTween.Sequence().SetTarget(this);
        flySeq.Append(flyText.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
        flySeq.Join(flyText.transform.DOMove(flyEndPoint.position, config.flyTextMoveDuration).SetEase(Ease.InOutQuad));
        yield return flySeq.WaitForCompletion();

        flyText.alpha = 0f;
    }

    public void HideAll()
    {
        DOTween.Kill(this);
        if (cardGroup != null) cardGroup.alpha = 0f;
        if (flyText != null) flyText.alpha = 0f;
    }

    public void SetIntroPosition(float slideOffset)
    {
        if (cardGroup == null || cardRect == null) return;
        Vector2 pos = cardRect.anchoredPosition;
        cardRect.anchoredPosition = new Vector2(pos.x, pos.y - slideOffset);
    }

    public Coroutine AnimateIntroSlide(MonoBehaviour host)
    {
        return host.StartCoroutine(IntroSlideSequence());
    }

    private IEnumerator IntroSlideSequence()
    {
        if (cardGroup == null || cardRect == null) yield break;

        cardGroup.alpha = 0f;
        Vector2 finalPos = cardRect.anchoredPosition;

        Sequence seq = DOTween.Sequence().SetTarget(this);
        seq.Append(cardRect.DOAnchorPosY(finalPos.y, config.modifierSlideDuration).SetEase(Ease.OutCubic));
        seq.Join(cardGroup.DOFade(1f, config.modifierFadeDuration));
        yield return seq.WaitForCompletion();

        cardGroup.interactable = true;
        cardGroup.blocksRaycasts = true;
    }
}
