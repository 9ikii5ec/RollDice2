using UnityEngine;

[System.Serializable]
public class RollUIConfig
{
    [Header("Задержки")]
    public float introStartDelay = 0.2f;
    public float introBetweenDelay = 0.05f;

    [Header("Modifier Card")]
    public float modifierSlideOffset = 0.3f;
    public float modifierSlideDuration = 0.35f;
    public float modifierFadeDuration = 0.3f;

    [Header("Roll Prompt")]
    public float rollPromptSlideOffset = 0.3f;
    public float rollPromptSlideDuration = 0.35f;
    public float rollPromptFadeDuration = 0.3f;

    [Header("Roll Button")]
    public float rollButtonFadeDuration = 0.3f;

    [Header("Roll Sequence")]
    public float modifierFadeInDuration = 0.4f;
    public float postRollDelay = 0.3f;

    [Header("Fly Text")]
    public float flyTextMoveDuration = 0.6f;

    [Header("Result Banner")]
    public float bannerFadeDuration = 0.3f;
    public float bannerPostDelay = 0.4f;

    [Header("Continue Button")]
    public float continueFadeDuration = 0.4f;
}
