using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    /// <summary>
    /// Сброс интерфейса в начальное состояние перед броском
    /// </summary>
    public void ResetUI()
    {
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
        yield return StartCoroutine(FadeCanvasGroup(rollPromptGroup, 1f, 0f, 0.2f));

        // 2. Включаем плашку модификатора снизу
        yield return StartCoroutine(FadeCanvasGroup(modifierCardGroup, 0f, 1f, 0.4f));

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
            yield return new WaitForSeconds(1.5f); // Пауза-эмуляция, если контроллер куба не привязан
        }

        yield return new WaitForSeconds(0.3f);

        // 4. Анимация полета бонуса (+1) от карточки к кубику
        if (modifierValue != 0 && modifierFlyText != null && flyTextStartPoint != null && flyTextEndPoint != null)
        {
            modifierFlyText.text = (modifierValue > 0 ? "+" : "") + modifierValue.ToString();
            modifierFlyText.gameObject.SetActive(true);

            if (audioSource && modifierFlySound) audioSource.PlayOneShot(modifierFlySound);

            float flyDuration = 0.6f;
            float elapsed = 0f;

            RectTransform flyRect = modifierFlyText.rectTransform;

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;
                // Плавное перемещение с эффектом ускорения/замедления
                float easeT = Mathf.SmoothStep(0f, 1f, t);

                flyRect.position = Vector3.Lerp(flyTextStartPoint.position, flyTextEndPoint.position, easeT);

                // Прозрачность: появляемся и растворяемся в конце
                float alpha = Mathf.Sin(t * Mathf.PI);
                modifierFlyText.alpha = alpha;

                yield return null;
            }

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

        yield return StartCoroutine(FadeCanvasGroup(resultBannerGroup, 0f, 1f, 0.3f));

        yield return new WaitForSeconds(0.4f);

        // 7. Появление кнопки "Continue"
        yield return StartCoroutine(FadeCanvasGroup(continueButtonGroup, 0f, 1f, 0.4f));
    }

    // --- Вспомогательные функции плавной прозрачности ---

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha = startAlpha;
        group.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = endAlpha;
        group.interactable = endAlpha > 0.5f;
        group.blocksRaycasts = endAlpha > 0.5f;
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}