using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ResultController : MonoBehaviour
{
    [Header("Banner")]
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TextMeshProUGUI bannerText;

    [Header("Continue")]
    [SerializeField] private CanvasGroup continueGroup;
    [SerializeField] private Button continueButton;

    [Header("Config")]
    [SerializeField] private RollUIConfig config;

    public bool IsSuccess { get; private set; }

    public void ShowBanner(int totalResult, int difficultyClass)
    {
        IsSuccess = totalResult >= difficultyClass;

        if (bannerText != null)
        {
            bannerText.text = IsSuccess ? "SUCCESS" : "FAILURE";
            bannerText.color = IsSuccess ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);
        }
    }

    public Coroutine AnimateBanner(MonoBehaviour host)
    {
        return host.StartCoroutine(BannerSequence());
    }

    private IEnumerator BannerSequence()
    {
        if (bannerGroup == null) yield break;

        bannerGroup.alpha = 0f;
        yield return bannerGroup.DOFade(1f, config.bannerFadeDuration).SetTarget(this).WaitForCompletion();
        bannerGroup.interactable = true;
        bannerGroup.blocksRaycasts = true;
    }

    public Coroutine AnimateContinue(MonoBehaviour host)
    {
        return host.StartCoroutine(ContinueSequence());
    }

    private IEnumerator ContinueSequence()
    {
        if (continueGroup == null) yield break;

        continueGroup.alpha = 0f;
        if (continueButton != null) continueButton.interactable = false;

        yield return new WaitForSeconds(config.bannerPostDelay);

        yield return continueGroup.DOFade(1f, config.continueFadeDuration).SetTarget(this).WaitForCompletion();
        if (continueButton != null) continueButton.interactable = true;
    }

    public void HideAll()
    {
        DOTween.Kill(this);
        if (bannerGroup != null) bannerGroup.alpha = 0f;
        if (continueGroup != null) continueGroup.alpha = 0f;
    }
}
