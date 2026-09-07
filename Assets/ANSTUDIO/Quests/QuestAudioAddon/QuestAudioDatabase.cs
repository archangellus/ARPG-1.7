using System;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Stores optional start/completion dialogue audio for existing Quest assets
    /// without modifying the original Quest.cs package file.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New Quest Audio Database",
        menuName = "PLAYER TWO/ARPG Project/Quest/Quest Audio Database"
    )]
    public class QuestAudioDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The existing Quest asset this audio belongs to.")]
            public Quest quest;

            [Tooltip("Audio played when this Quest's start dialogue is shown.")]
            public AudioClip startDialogueAudio;

            [Tooltip("Audio played when this Quest's completion dialogue is shown.")]
            public AudioClip completeDialogueAudio;
        }

        [Tooltip("Audio assignments for Quest assets.")]
        public Entry[] entries;

        /// <summary>
        /// Returns the audio entry associated with a Quest, if one exists.
        /// </summary>
        public virtual bool TryGetEntry(Quest quest, out Entry entry)
        {
            if (quest != null && entries != null)
            {
                foreach (var candidate in entries)
                {
                    if (candidate != null && candidate.quest == quest)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public virtual AudioClip GetStartDialogueAudio(Quest quest)
        {
            return TryGetEntry(quest, out var entry)
                ? entry.startDialogueAudio
                : null;
        }

        public virtual AudioClip GetCompleteDialogueAudio(Quest quest)
        {
            return TryGetEntry(quest, out var entry)
                ? entry.completeDialogueAudio
                : null;
        }
    }
}
