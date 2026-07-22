using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RollManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private RollUIController uiController;
    [SerializeField] private DiceController diceController;
    [SerializeField] private ModifierController modifierController;
    [SerializeField] private ResultController resultController;
    [SerializeField] private DiceVisualController diceVisual;
    [SerializeField] private AudioController audioController;

    [Header("Config")]
    [SerializeField] private int modifierValue = 1;
    [SerializeField] private int difficultyClass = 10;

    private bool isRolling;

    public void StartRoll()
    {
        if (isRolling) return;
        int randomResult = Random.Range(1, 21);
        StartCoroutine(RollSequence(randomResult));
    }

    private IEnumerator RollSequence(int rawDiceResult)
    {
        isRolling = true;

        uiController.PrepareForRoll();

        if (modifierController != null)
            modifierController.ShowCard();

        if (audioController != null)
            audioController.PlayRoll();

        if (diceController != null)
        {
            diceController.RollDice(rawDiceResult);
            while (diceController.IsRolling)
                yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        if (modifierController != null)
            yield return modifierController.AnimateFlyText(this, modifierValue);

        int totalResult = rawDiceResult + modifierValue;

        if (diceController != null)
            diceController.SnapToFace(totalResult);

        if (diceVisual != null)
            yield return diceVisual.PlayRollPulse(this);

        if (modifierController != null)
            modifierController.FadeCardToHalf();

        if (audioController != null)
            audioController.PlaySuccess();

        if (resultController != null)
        {
            resultController.ShowBanner(totalResult, difficultyClass);
            yield return resultController.AnimateBanner(this);
        }

        if (modifierController != null)
            modifierController.HideCard();

        if (resultController != null)
            yield return resultController.AnimateContinue(this);

        isRolling = false;
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
