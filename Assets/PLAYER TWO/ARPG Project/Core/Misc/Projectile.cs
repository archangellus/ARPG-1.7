using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/Misc/Projectile")]
    public class Projectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The maximum distance this Projectile can reach.")]
        public float maxDistance = 15f;

        [Tooltip("The speed at which this Projectile moves.")]
        public float speed = 15f;

        [Tooltip(
            "If true, the Projectile will be destroyed upon impact with"
                + "solid objects (e.g., walls, terrain, etc.)."
        )]
        public bool destroyOnImpact = true;

        [Header("Ground Settings")]
        [Tooltip("If true, the Projectile will adjust its position relative to the ground.")]
        public bool adjustToGround = true;

        [Tooltip(
            "The speed at which the Projectile adjusts its position downwards when it exceeds the minimum ground distance."
        )]
        public float downwardAdjustmentSpeed = 10f;

        [Tooltip("Ground layer mask for the Projectile to adjust its position.")]
        public LayerMask groundLayer = ~0;

        [Header("Homing Settings")]
        [Tooltip("If true, the Projectile will seek and steer towards nearby targets.")]
        public bool homingEnabled = false;

        [Tooltip("The radius around the Projectile in which it will search for a target to home in on.")]
        public float homingRadius = 5f;

        [Tooltip("The speed at which the Projectile moves towards its homing target once one is found.")]
        public float homingSpeed = 20f;

        [Tooltip("The layer mask used to determine which colliders block line of sight when searching for a homing target.")]
        public LayerMask obstacleLayer = ~0;

        protected List<DamageLayer> m_damageLayers;
        protected bool m_critical;
        protected List<string> m_targets;
        protected EntityEffect[] m_targetEffects;
        protected float m_targetEffectChance = 1f;

        protected Vector3 m_origin;

        protected Entity m_entity;
        protected Entity m_otherEntity;
        protected Destructible m_destructible;

        protected Collider m_collider;
        protected Rigidbody m_rigidbody;

        protected float m_minimumGroundDistance = k_defaultGroundDistance;

        protected Transform m_homingTarget;
        protected Entity m_homingEntity;
        protected float m_lastHomingRefreshTime;

        protected static readonly Collider[] s_homingBuffer = new Collider[16];

        protected const float k_defaultGroundDistance = 1f;
        protected const float k_homingRefreshRate = 1f / 15;

        /// <summary>
        /// Sets the damage data for this Projectile.
        /// </summary>
        /// <param name="entity">The Entity casting this Projectile.</param>
        /// <param name="layers">The per-type damage layers for this Projectile.</param>
        /// <param name="critical">If true, the Projectile damage will be considered critical.</param>
        /// <param name="targets">The list of targets' tags for this Projectile to interact with.</param>
        public virtual void SetDamage(
            Entity entity,
            List<DamageLayer> layers,
            bool critical,
            List<string> targets
        )
        {
            m_entity = entity;
            m_damageLayers = layers;
            m_critical = critical;
            m_targets = new List<string>(targets);

            CaptureGroundDistance();
        }

        /// <summary>
        /// Sets the effects and their application chance to apply to entities hit by this Projectile.
        /// </summary>
        /// <param name="effects">The effect assets to apply on hit.</param>
        /// <param name="chance">Probability (0 to 1) of applying all effects.</param>
        public virtual void SetEffect(EntityEffect[] effects, float chance)
        {
            m_targetEffects = effects;
            m_targetEffectChance = chance;
        }

        protected virtual void Start()
        {
            InitializeCollider();
            InitializeRigidbody();
        }

        protected virtual void Update()
        {
            HandleHoming();
            HandleMovement();
            HandleDistanceCulling();
            HandleGroundDistance();
        }

        protected virtual void OnEnable() => m_origin = transform.position;

        protected virtual void OnTriggerEnter(Collider other)
        {
            HandleEntityAttack(other);
            HandleDestructibleAttack(other);
            HandleImpact(other);
        }

        protected virtual void InitializeCollider()
        {
            m_collider = GetComponent<Collider>();
            m_collider.isTrigger = true;
        }

        protected virtual void InitializeRigidbody()
        {
            if (!TryGetComponent(out m_rigidbody))
                m_rigidbody = gameObject.AddComponent<Rigidbody>();

            m_rigidbody.isKinematic = true;
        }

        protected virtual void HandleEntityAttack(Collider other)
        {
            if (!other.InTagList(m_targets) || !other.TryGetComponent(out m_otherEntity))
                return;

            gameObject.SetActive(false);
            m_otherEntity.Damage(
                m_entity,
                new EntityDamageInfo(
                    m_damageLayers,
                    m_critical,
                    m_targetEffects,
                    m_targetEffectChance
                )
            );
            Destroy(gameObject);
        }

        protected virtual void HandleDestructibleAttack(Collider other)
        {
            if (
                !m_entity.IsPlayer()
                || !other.IsDestructible()
                || !other.TryGetComponent(out m_destructible)
            )
                return;

            var totalDamage = 0;

            foreach (var layer in m_damageLayers)
                totalDamage += layer.amount;

            m_destructible.Damage(totalDamage);
            Destroy(gameObject);
        }

        protected virtual void HandleImpact(Collider other)
        {
            if (!other.isTrigger && other.gameObject != m_entity.gameObject && destroyOnImpact)
                Destroy(gameObject);
        }

        protected virtual void HandleMovement()
        {
            if (homingEnabled && m_homingTarget)
                HandleHomingMovement();
            else
                transform.position += speed * Time.deltaTime * transform.forward;
        }

        protected virtual void HandleHomingMovement()
        {
            var direction = m_homingTarget.position - transform.position;

            if (direction == Vector3.zero)
                return;

            direction.Normalize();
            transform.position += homingSpeed * Time.deltaTime * direction;
            transform.forward = direction;
        }

        protected virtual void HandleHoming()
        {
            if (!homingEnabled)
                return;

            if (m_homingTarget && m_homingTarget.gameObject.activeInHierarchy && !m_homingEntity.isDead)
                return;

            if (Time.time - m_lastHomingRefreshTime < k_homingRefreshRate)
                return;

            m_lastHomingRefreshTime = Time.time;
            FindHomingTarget();
        }

        protected virtual void FindHomingTarget()
        {
            m_homingTarget = null;
            m_homingEntity = null;

            var overlaps = Physics.OverlapSphereNonAlloc(
                transform.position,
                homingRadius,
                s_homingBuffer,
                ~0,
                QueryTriggerInteraction.Collide
            );

            var closestDistance = 0f;

            for (int i = 0; i < overlaps; i++)
            {
                var candidate = s_homingBuffer[i];

                if (
                    !candidate.InTagList(m_targets)
                    || !candidate.TryGetComponent(out Entity entity)
                    || entity.isDead
                    || !IsInFront(candidate)
                    || !HasLineOfSight(candidate)
                )
                    continue;

                var distance = Vector3.Distance(transform.position, candidate.transform.position);

                if (!m_homingTarget || distance < closestDistance)
                {
                    m_homingTarget = candidate.transform;
                    m_homingEntity = entity;
                    closestDistance = distance;
                }
            }
        }

        protected virtual bool IsInFront(Collider candidate) =>
            Vector3.Dot(transform.forward, candidate.transform.position - transform.position) > 0;

        protected virtual bool HasLineOfSight(Collider candidate)
        {
            var origin = transform.position;
            var offset = candidate.transform.position - origin;
            var distance = offset.magnitude;

            if (distance == 0)
                return true;

            if (
                Physics.Raycast(
                    origin,
                    offset / distance,
                    out var hit,
                    distance,
                    obstacleLayer,
                    QueryTriggerInteraction.Ignore
                )
            )
                return hit.collider == candidate;

            return true;
        }

        protected virtual void HandleDistanceCulling()
        {
            if (Vector3.Distance(m_origin, transform.position) >= maxDistance)
                Destroy(gameObject);
        }

        protected virtual void HandleGroundDistance()
        {
            if (!adjustToGround)
                return;

            if (
                Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    out var hit,
                    Mathf.Infinity,
                    groundLayer,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                var position = transform.position;
                var targetY = hit.point.y + m_minimumGroundDistance;
                position.y = Mathf.MoveTowards(
                    position.y,
                    targetY,
                    downwardAdjustmentSpeed * Time.deltaTime
                );
                transform.position = position;
            }
            else
                transform.position -= downwardAdjustmentSpeed * Time.deltaTime * Vector3.up;
        }

        protected virtual void CaptureGroundDistance()
        {
            if (
                Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    out var hit,
                    Mathf.Infinity,
                    groundLayer,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                m_minimumGroundDistance = hit.distance;
            }
        }
    }
}
