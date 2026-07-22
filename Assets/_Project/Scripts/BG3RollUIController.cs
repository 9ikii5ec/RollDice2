using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Подключаем DOTween

public class BG3RollUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI difficultyClassText; // Текст сложности (например "10")
    [SerializeField] private CanvasGroup rollPromptGroup;       // Кнопка/подсказка "Click dice to roll"
    [SerializeField] private CanvasGroup modifierCardGroup;     // Карточка модификатора снизу (+1 Intelligence)
    [SerializeField] private TextMeshProUGUI modifierFlyText;  // Летающий текст бонуса ("+1")
    [SerializeField] private CanvasGroup resultBannerGroup;    // Плашка результата (SUCCESS / FAILURE)
    [SerializeField] private TextMeshProUGUI resultBannerText; // Текст на плашке
    [SerializeField] private CanvasGroup continueButtonGroup;   // Кнопка "Continue"
    [SerializeField] private Button continueButton;

    [Header("Animation Positions")]
    [SerializeField] private RectTransform flyTextStartPoint; // Начальная позиция летающего текста (у плашки модификатора)
    [SerializeField] private RectTransform flyTextEndPoint;   // Конечная позиция летающего текста (у кубика)

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip modifierFlySound;      // Звук вылета бонуса
    [SerializeField] private AudioClip successSound;          // Звук победы
    [SerializeField] private AudioClip failureSound;          // Звук поражения

    [Header("Particles (Optional)")]
    [SerializeField] private ParticleSystem successParticles;

    // Ссылка на контроллер броска кубика
    [SerializeField] private BG3DiceParentController diceController;

    private void Awake()
    {
        ResetUI();
    }

    private void OnDestroy()
    {
        // Убиваем все активные твины этого объекта при уничтожении, чтобы не было ошибок
        DOTween.Kill(this);
    }

    /// <summary>
    /// Сброс интерфейса в начальное состояние перед броском
    /// </summary>
    public void ResetUI()
    {
        // Останавливаем все запущенные твины UI
        DOTween.Kill(this);

        SetGroupAlpha(rollPromptGroup, 1f, true);
        SetGroupAlpha(modifierCardGroup, 0f, false);
        SetGroupAlpha(resultBannerGroup, 0f, false);
        SetGroupAlpha(continueButtonGroup, 0f, false);

        if (modifierFlyText != null)
            modifierFlyText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Вызывается при клике по кубику или старту броска
    /// </summary>
    public void StartRollSequence(int rawDiceResult, int modifierValue, int difficultyClass)
    {
        StartCoroutine(RollSequenceCoroutine(rawDiceResult, modifierValue, difficultyClass));
    }

    private IEnumerator RollSequenceCoroutine(int rawDiceResult, int modifierValue, int difficultyClass)
    {
        // 1. Скрываем подсказку "Click dice to roll"
        if (rollPromptGroup != null)
        {
            rollPromptGroup.interactable = false;
            rollPromptGroup.blocksRaycasts = false;
            yield return rollPromptGroup.DOFade(0f, 0.2f).SetTarget(this).WaitForCompletion();
            rollPromptGroup.gameObject.SetActive(false);
        }

        // 2. Включаем плашку модификатора снизу
        if (modifierCardGroup != null)
        {
            modifierCardGroup.gameObject.SetActive(true);
            yield return modifierCardGroup.DOFade(1f, 0.4f).SetTarget(this).WaitForCompletion();
            modifierCardGroup.interactable = true;
            modifierCardGroup.blocksRaycasts = true;
        }

        // 3. Запускаем бросок самого кубика D20
        if (diceController != null)
        {
            // Бросаем кубик на базовое значение (без учета модификатора)
            diceController.RollDice(rawDiceResult);

            // Ждем, пока кубик докрутится и остановится
            while (diceController.IsRolling)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f); // Эмуляция паузы, если кубик не привязан
        }

        yield return new WaitForSeconds(0.3f);

        // 4. Анимация полета бонуса (+1) от карточки к кубику через DOTween Sequence
        if (modifierValue != 0 && modifierFlyText != null && flyTextStartPoint != null && flyTextEndPoint != null)
        {
            modifierFlyText.text = (modifierValue > 0 ? "+" : "") + modifierValue.ToString();
            modifierFlyText.transform.position = flyTextStartPoint.position;
            modifierFlyText.alpha = 0f;
            modifierFlyText.gameObject.SetActive(true);

            if (audioSource && modifierFlySound) audioSource.PlayOneShot(modifierFlySound);

            // Создаем цепочку анимаций полёта
            Sequence flySeq = DOTween.Sequence().SetTarget(this);

            // Плавное перемещение к кубику с эффектом ускорения/замедления
            flySeq.Join(modifierFlyText.transform.DOMove(flyTextEndPoint.position, 0.6f).SetEase(Ease.InOutQuad));

            // Появление в начале и растворение в конце полета
            flySeq.Join(modifierFlyText.DOFade(1f, 0.2f));
            flySeq.Insert(0.4f, modifierFlyText.DOFade(0f, 0.2f));

            yield return flySeq.WaitForCompletion();

            modifierFlyText.gameObject.SetActive(false);
        }

        // 5. Вычисляем итоговый результат
        int totalResult = rawDiceResult + modifierValue;
        bool isSuccess = totalResult >= difficultyClass;

        // 6. Появление баннера SUCCESS / FAILURE
        if (resultBannerText != null)
        {
            resultBannerText.text = isSuccess ? "SUCCESS" : "FAILURE";
            resultBannerText.color = isSuccess ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);
        }

        if (isSuccess)
        {
            if (audioSource && successSound) audioSource.PlayOneShot(successSound);
            if (successParticles) successParticles.Play();
        }
        else
        {
            if (audioSource && failureSound) audioSource.PlayOneShot(failureSound);
        }

        if (resultBannerGroup != null)
        {
            resultBannerGroup.gameObject.SetActive(true);
            yield return resultBannerGroup.DOFade(1f, 0.3f).SetTarget(this).WaitForCompletion();
            resultBannerGroup.interactable = true;
            resultBannerGroup.blocksRaycasts = true;
        }

        yield return new WaitForSeconds(0.4f);

        // 7. Появление кнопки "Continue"
        if (continueButtonGroup != null)
        {
            continueButtonGroup.gameObject.SetActive(true);
            yield return continueButtonGroup.DOFade(1f, 0.4f).SetTarget(this).WaitForCompletion();
            continueButtonGroup.interactable = true;
            continueButtonGroup.blocksRaycasts = true;
        }
    }

    // --- Вспомогательный метод установки параметров CanvasGroup ---
    private void SetGroupAlpha(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
        group.gameObject.SetActive(alpha > 0f);
    }
}