using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Entity))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/Entity/Entity Auto Respawn")]
    public class EntityAutoRespawn : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [Tooltip("Time in seconds before the entity respawns after dying.")]
        public float respawnDelay = 5f;

        [Space(10)]
        public UnityEvent onRespawn;

        protected Entity m_entity;
        protected Coroutine m_respawnRoutine;
        protected WaitForSeconds m_respawnDelay;

        protected virtual void Start()
        {
            m_entity = GetComponent<Entity>();
            m_entity.onDie.AddListener(OnEntityDie);
            m_respawnDelay = new(respawnDelay);
        }

        protected virtual void OnEntityDie()
        {
            if (m_respawnRoutine != null)
                StopCoroutine(m_respawnRoutine);

            if (!gameObject.activeSelf)
            {
                CompleteRespawn();
                return;
            }

            m_respawnRoutine = StartCoroutine(RespawnRoutine());
        }

        protected virtual IEnumerator RespawnRoutine()
        {
            yield return m_respawnDelay;
            CompleteRespawn();
        }

        protected virtual void CompleteRespawn()
        {
            m_respawnRoutine = null;
            m_entity.Revive();
            m_entity.Teleport(m_entity.initialPosition, Quaternion.identity);
            onRespawn?.Invoke();
        }

        protected virtual void OnDisable()
        {
            if (m_respawnRoutine == null)
                return;

            CompleteRespawn();
        }
    }
}
