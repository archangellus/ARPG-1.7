using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Adds audio-capable dialogue calls without changing the original GUIDialogue.cs.
    /// </summary>
    public static class GUIDialogueAudioExtensions
    {
        /// <summary>
        /// Shows the normal dialogue and optionally plays a voice/audio clip.
        /// </summary>
        public static void ShowAndFillWithAudio(
            this GUIDialogue dialogue,
            string npcName,
            string dialogueText,
            UnityAction onFinish,
            AudioClip dialogueAudio
        )
        {
            if (dialogue == null)
                return;

            // Keep all original GUIDialogue behavior.
            dialogue.ShowAndFill(npcName, dialogueText, onFinish);

            // Match the behavior from the pasted audio-enabled GUIDialogue.
            if (dialogueAudio != null && GameAudio.instance != null)
                GameAudio.instance.PlayUiEffect(dialogueAudio);
        }
    }
}
