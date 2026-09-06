using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Entity))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/Entity/Entity Portal Opener")]
    public class EntityPortalOpener : MonoBehaviour
    {
        [Header("Portal Settings")]
        [Tooltip("The Town Portal prefab to instantiate when the portal opens.")]
        public GameObject portalPrefab;

        [Tooltip("The duration, in seconds, the Entity must channel before the portal opens.")]
        public float channelDuration = 3f;

        [Header("Safety Check Settings")]
        [Tooltip("If true, blocks opening a Town Portal when there are enemies nearby.")]
        public bool checkNearbyEnemies = true;

        [Tooltip(
            "The radius around the Entity checked for nearby enemies before opening a portal."
        )]
        public float safeRadius = 10f;

        [Tooltip("The layers scanned when checking for nearby enemies.")]
        public LayerMask enemyLayers = ~0;

        [Tooltip(
            "If true, blocks opening a Town Portal too close to another already-open Town Portal."
        )]
        public bool checkNearbyPortals = true;

        [Tooltip(
            "The minimum distance the Entity must be from another Town Portal to be allowed to open one."
        )]
        public float minDistanceFromPortals = 10f;

        [Tooltip(
            "If true, blocks opening a Town Portal too close to the Level's Town Portal Exit."
        )]
        public bool checkNearbyLevelExit = true;

        [Tooltip(
            "The minimum distance the Entity must be from the Level's Town Portal Exit to be allowed to open one."
        )]
        public float minDistanceFromLevelExit = 20f;

        [Tooltip("If true, blocks opening a Town Portal too close to a Waypoint.")]
        public bool checkNearbyWaypoints = true;

        [Tooltip(
            "The minimum distance the Entity must be from a Waypoint to be allowed to open a Town Portal."
        )]
        public float minDistanceFromWaypoints = 10f;

        [Header("Placement Settings")]
        [Tooltip("The minimum distance from the Entity where the portal can be instantiated.")]
        public float minPortalDistance = 2f;

        [Tooltip("The maximum distance from the Entity where the portal can be instantiated.")]
        public float maxPortalDistance = 5f;

        [Header("Events")]
        public UnityEvent onOpenStarted;
        public UnityEvent onOpenCompleted;
        public UnityEvent onOpenCancelled;
        public UnityEvent onEnemiesNearby;
        public UnityEvent onTooCloseToAnotherPortal;
        public UnityEvent onTooCloseToLevelExit;
        public UnityEvent onTooCloseToWaypoint;
        public UnityEvent onNoValidPositionFound;

        protected Entity m_entity;
        protected Coroutine m_openRoutine;
        protected GameObject m_activePortal;
        protected Collider[] m_scanBuffer = new Collider[32];

        protected const int k_placementAttempts = 4;

        /// <summary>
        /// Returns true if this Entity is currently channeling a Town Portal.
        /// </summary>
        public bool isOpening => m_openRoutine != null;

        protected virtual void Awake() => m_entity = GetComponent<Entity>();

        protected virtual void Start() => RestoreActivePortal();

        /// <summary>
        /// Re-instantiates a Town Portal this character already left open in the scene that
        /// just loaded (e.g. after saving and reloading).
        /// </summary>
        protected virtual void RestoreActivePortal()
        {
            if (!portalPrefab || m_activePortal || !Level.instance)
                return;

            var character = Level.instance.currentCharacter;

            if (
                string.IsNullOrEmpty(character.townPortalReturnScene)
                || character.townPortalReturnScene != Level.instance.currentScene.name
            )
                return;

            m_activePortal = Instantiate(
                portalPrefab,
                character.townPortalPosition,
                character.townPortalRotation
            );

            if (m_activePortal.TryGetComponent(out TownPortal townPortal))
                townPortal.PrewarmParticles();
        }

        /// <summary>
        /// Checks the surroundings and, if safe, channels before opening a Town Portal.
        /// </summary>
        /// <returns>
        /// Returns true if the channel has started, or false if it was rejected by a safety
        /// check. This does not indicate whether the channel will successfully complete.
        /// </returns>
        public virtual bool Open()
        {
            if (isOpening || !portalPrefab)
                return false;

            if (checkNearbyEnemies && !IsSafe())
            {
                onEnemiesNearby.Invoke();
                return false;
            }

            if (checkNearbyPortals && IsNearAnotherPortal())
            {
                onTooCloseToAnotherPortal.Invoke();
                return false;
            }

            if (checkNearbyLevelExit && IsNearLevelExit())
            {
                onTooCloseToLevelExit.Invoke();
                return false;
            }

            if (checkNearbyWaypoints && IsNearWaypoint())
            {
                onTooCloseToWaypoint.Invoke();
                return false;
            }

            m_openRoutine = StartCoroutine(OpenRoutine());
            return true;
        }

        /// <summary>
        /// Cancels an in-progress Town Portal channel.
        /// </summary>
        public virtual void Cancel()
        {
            if (!isOpening)
                return;

            StopCoroutine(m_openRoutine);
            m_openRoutine = null;
            onOpenCancelled.Invoke();
        }

        protected virtual bool IsSafe()
        {
            var count = Physics.OverlapSphereNonAlloc(
                transform.position,
                safeRadius,
                m_scanBuffer,
                enemyLayers
            );

            for (int i = 0; i < count; i++)
                if (m_scanBuffer[i].IsEnemy())
                    return false;

            return true;
        }

        protected virtual bool IsNearAnotherPortal()
        {
            var exit = Level.instance.townPortalExit;

            foreach (var portal in TownPortal.all)
            {
                if (portal == exit)
                    continue;

                if (
                    Vector3.Distance(transform.position, portal.transform.position)
                    < minDistanceFromPortals
                )
                    return true;
            }

            return false;
        }

        protected virtual bool IsNearLevelExit()
        {
            var exit = Level.instance.townPortalExit;

            return exit
                && Vector3.Distance(transform.position, exit.transform.position)
                    < minDistanceFromLevelExit;
        }

        protected virtual bool IsNearWaypoint()
        {
            if (!LevelWaypoints.instance)
                return false;

            foreach (var waypoint in LevelWaypoints.instance.waypoints)
                if (
                    waypoint
                    && Vector3.Distance(transform.position, waypoint.transform.position)
                        < minDistanceFromWaypoints
                )
                    return true;

            return false;
        }

        protected virtual IEnumerator OpenRoutine()
        {
            onOpenStarted.Invoke();

            m_entity.StandStill();

            var elapsed = 0f;

            while (elapsed < channelDuration)
            {
                yield return null;

                if (m_entity.isWalking)
                {
                    m_openRoutine = null;
                    onOpenCancelled.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
            }

            if (!TryFindPortalPosition(out var position, out var rotation))
            {
                m_openRoutine = null;
                onNoValidPositionFound.Invoke();
                yield break;
            }

            if (m_activePortal)
                Destroy(m_activePortal);

            m_activePortal = Instantiate(portalPrefab, position, rotation);

            if (m_activePortal.TryGetComponent(out TownPortal townPortal))
            {
                m_activePortal.transform.position += Vector3.up * townPortal.GetGroundOffset();
                GameAudio.instance.PlayEffect(townPortal.openClip);

                var spacePoint = townPortal.GetSpacePoint();
                var character = Level.instance.currentCharacter;
                character.townPortalReturnScene = Level.instance.currentScene.name;
                character.townPortalReturnPosition = spacePoint.position;
                character.townPortalReturnRotation = spacePoint.rotation;
                character.townPortalPosition = m_activePortal.transform.position;
                character.townPortalRotation = m_activePortal.transform.rotation;
            }

            m_openRoutine = null;
            onOpenCompleted.Invoke();
        }

        protected virtual bool TryFindPortalPosition(out Vector3 position, out Quaternion rotation)
        {
            for (int i = 0; i < k_placementAttempts; i++)
            {
                var random = Random.insideUnitCircle.normalized;
                var direction = new Vector3(random.x, 0, random.y);
                var candidate = transform.position + direction * minPortalDistance;

                if (
                    NavMesh.SamplePosition(
                        candidate,
                        out var hit,
                        maxPortalDistance - minPortalDistance,
                        NavMesh.AllAreas
                    )
                )
                {
                    position = hit.position;
                    rotation = Quaternion.LookRotation(-direction, Vector3.up);
                    return true;
                }
            }

            position = default;
            rotation = default;
            return false;
        }
    }
}
