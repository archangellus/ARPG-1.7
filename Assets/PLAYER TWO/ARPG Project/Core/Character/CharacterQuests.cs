using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    public class CharacterQuests
    {
        public List<QuestInstance> initialQuests = new();

        public QuestsManager m_quests;

        public QuestInstance[] currentQuests =>
            m_quests != null ? m_quests.list : initialQuests.ToArray();

        public QuestsManager manager => m_quests;

        public CharacterQuests() { }

        public CharacterQuests(QuestInstance[] initialQuests)
        {
            this.initialQuests = new List<QuestInstance>(initialQuests);
        }

        /// <summary>
        /// Initializes the Character's Quest Manager.
        /// </summary>
        public virtual void InitializeQuests()
        {
            if (m_quests != null)
                return;

            m_quests = new QuestsManager();

            if (initialQuests == null || initialQuests.Count == 0)
                return;

            var instances = initialQuests.Select(q => new QuestInstance(
                q.data,
                q.progress,
                q.state
            ));

            m_quests.SetQuests(instances.ToArray());
        }

        public static CharacterQuests CreateFromSerializer(QuestsSerializer serializer)
        {
            if (serializer?.quests == null)
                return new CharacterQuests();

            var quests = serializer.quests.Select(q =>
            {
                if (q == null)
                {
                    Debug.LogWarning(
                        "A quest won't be loaded from the save file because its data is missing."
                    );
                    return null;
                }

                var data = GameDatabase.instance.FindElementById<Quest>(q.questId);

                if (data == null)
                {
                    Debug.LogWarning(
                        $"Quest with id '{q.questId}' won't be loaded from the save file "
                            + "because it was not found in the game database."
                    );
                    return null;
                }

                return new QuestInstance(data, q.progress, q.state);
            });

            return new CharacterQuests(quests.Where(q => q != null).ToArray());
        }
    }
}
