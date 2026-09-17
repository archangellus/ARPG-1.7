using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [System.Serializable]
    public class CharacterSerializer
    {
        public int characterId;

        public string name;
        public string scene;
        public string waypoint;

        public UnitySerializer.Vector3 position;
        public UnitySerializer.Vector3 rotation;

        public bool usingTownPortal;
        public string townPortalReturnScene;
        public UnitySerializer.Vector3 townPortalReturnPosition;
        public UnitySerializer.Vector3 townPortalReturnRotation;
        public UnitySerializer.Vector3 townPortalPosition;
        public UnitySerializer.Vector3 townPortalRotation;

        public StatsSerializer stats;
        public EquipmentsSerializer equipments;
        public InventorySerializer inventory;
        public SkillsSerializer skills;
        public QuestsSerializer quests;
        public ScenesSerializer scenes;

        public CharacterSerializer(CharacterInstance character)
        {
            characterId = GameDatabase.instance.GetElementId<Character>(character.data);
            name = character.name;
            position = new UnitySerializer.Vector3(character.currentPosition);
            rotation = new UnitySerializer.Vector3(character.currentRotation.eulerAngles);
            scene = character.currentScene;
            waypoint = character.currentWaypoint;
            usingTownPortal = character.usingTownPortal;
            townPortalReturnScene = character.townPortalReturnScene;
            townPortalReturnPosition = new UnitySerializer.Vector3(
                character.townPortalReturnPosition
            );
            townPortalReturnRotation = new UnitySerializer.Vector3(
                character.townPortalReturnRotation.eulerAngles
            );
            townPortalPosition = new UnitySerializer.Vector3(character.townPortalPosition);
            townPortalRotation = new UnitySerializer.Vector3(
                character.townPortalRotation.eulerAngles
            );
            stats = new StatsSerializer(character.stats);
            equipments = new EquipmentsSerializer(character.equipments);
            inventory = new InventorySerializer(character.inventory);
            skills = new SkillsSerializer(character.skills);
            quests = new QuestsSerializer(character.quests);
            scenes = new ScenesSerializer(character.scenes);
        }

        public virtual void ToJson() => JsonUtility.ToJson(this);

        public static CharacterSerializer FromJson(string json) =>
            JsonUtility.FromJson<CharacterSerializer>(json);
    }
}
