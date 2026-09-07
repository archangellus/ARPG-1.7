using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Drop-in QuestGiver subclass that reads quest dialogue clips from a
    /// QuestAudioDatabase. The original QuestGiver.cs and Quest.cs stay untouched.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/Quest/Quest Giver Audio")]
    public class QuestGiverAudio : QuestGiver
    {
        [Header("Quest Audio")]
        [Tooltip(
            "Database containing start/completion dialogue AudioClips for the Quests offered by this giver."
        )]
        public QuestAudioDatabase audioDatabase;

        /// <summary>
        /// Handles quests that are ready to be completed by returning to this giver.
        /// This is the original QuestGiver behavior plus completion dialogue audio.
        /// </summary>
        protected override void CompleteReturningQuests()
        {
            foreach (var quest in quests)
            {
                if (
                    m_manager.TryGetQuest(quest, out var instance)
                    && !instance.completed
                    && instance.returningToGiver
                )
                {
                    var clip = audioDatabase != null
                        ? audioDatabase.GetCompleteDialogueAudio(quest)
                        : null;

                    GUIWindowsManager
                        .instance.GetDialogue()
                        .ShowAndFillWithAudio(
                            questGiverName,
                            quest.completeDialogue,
                            () => instance.NextState(),
                            clip
                        );
                }
            }
        }

        /// <summary>
        /// Shows the current quest's start dialogue with its optional audio clip.
        /// </summary>
        protected override void ShowQuestWindow()
        {
            var current = CurrentQuest(out var instance);

            if (current && (instance == null || !instance.returningToGiver))
            {
                var clip = audioDatabase != null
                    ? audioDatabase.GetStartDialogueAudio(current)
                    : null;

                GUIWindowsManager
                    .instance.GetDialogue()
                    .ShowAndFillWithAudio(
                        questGiverName,
                        current.startDialogue,
                        () => GUIWindowsManager.instance.quest.SetQuest(current),
                        clip
                    );
            }
        }
    }
}
