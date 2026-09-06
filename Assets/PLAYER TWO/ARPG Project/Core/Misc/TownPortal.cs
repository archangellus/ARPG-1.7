using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/Misc/Town Portal")]
    public class TownPortal : Interactive
    {
        /// <summary>
        /// All Town Portals currently active in the scene. Populated on Start, cleared on OnDestroy.
        /// </summary>
        public static readonly List<TownPortal> all = new();

        [Header("Town Portal Settings")]
        [Tooltip(
            "The transform representing where the player lands when arriving through this Town Portal. Defaults to this object's own transform."
        )]
        public Transform exitPoint;

        [Tooltip(
            "Extra vertical offset applied on top of this Town Portal's own collider bounds when it's spawned by an Entity Portal Opener at a ground-sampled position, so it doesn't clip into the ground."
        )]
        public float groundOffset;

        [Header("Audio Settings")]
        [Tooltip(
            "An Audio Clip that plays when this Town Portal is first opened. Not played when it's prewarmed instead (the exit portal being revealed, or one restored on scene load)."
        )]
        public AudioClip openClip;

        [Tooltip("An Audio Clip that plays whenever the Player travels through this Town Portal.")]
        public AudioClip travelClip;

        [Header("Visual Settings")]
        [Tooltip(
            "How many seconds to fast-forward this Town Portal's particle systems so they're already in a steady, looping state the moment it becomes visible, instead of restarting from empty after being out of view (e.g. disabled while inactive)."
        )]
        public float particlePrewarmDuration = 5f;

        protected bool m_isReturnPortal;
        protected Collider m_ownCollider;

        protected Level m_level => Level.instance;
        protected Entity m_player => m_level.player;

        /// <summary>
        /// This Town Portal's own Collider, cached lazily since this may run before
        /// Interactive's own Start() has cached one.
        /// </summary>
        protected virtual Collider OwnCollider
        {
            get
            {
                if (!m_ownCollider)
                    m_ownCollider = GetComponent<Collider>();

                return m_ownCollider;
            }
        }

        /// <summary>
        /// True if this instance returns the player to their recorded origin instead of the
        /// Level's configured town. Set only via <see cref="SetAsReturnPortal"/>.
        /// </summary>
        public bool isReturnPortal => m_isReturnPortal;

        /// <summary>
        /// Returns where an arriving player should land: exitPoint if assigned, otherwise a
        /// point clear of this portal's own collider bounds. Height is ground-corrected via
        /// raycast, and the rotation faces away from this portal's center.
        /// </summary>
        public virtual SpacePoint GetSpacePoint()
        {
            transform.GetPositionAndRotation(out var centerPosition, out var centerRotation);

            Vector3 rawPosition;

            if (exitPoint)
                rawPosition = exitPoint.position;
            else
            {
                var standoff =
                    (OwnCollider ? OwnCollider.bounds.extents.magnitude : 0f)
                    + m_player.controller.radius;
                rawPosition = centerPosition + centerRotation * Vector3.forward * standoff;
            }

            var faceDirection = rawPosition - centerPosition;
            faceDirection.y = 0f;

            var rotation =
                faceDirection.sqrMagnitude > 0f
                    ? Quaternion.LookRotation(faceDirection.normalized, Vector3.up)
                    : centerRotation;

            var groundPosition = Physics.Raycast(rawPosition, Vector3.down, out var hit)
                ? hit.point
                : rawPosition;

            var position = groundPosition + Vector3.up * m_player.controller.height * 0.5f;

            return new(position, rotation);
        }

        /// <summary>
        /// Returns how far above a ground point this Town Portal should sit to avoid clipping,
        /// based on its collider bounds plus <see cref="groundOffset"/>.
        /// </summary>
        public virtual float GetGroundOffset() =>
            (OwnCollider ? OwnCollider.bounds.extents.y : 0f) + groundOffset;

        /// <summary>
        /// Enables this Town Portal, prewarming its particles first so they're already
        /// steady by the time it becomes visible.
        /// </summary>
        public virtual void Activate()
        {
            PrewarmParticles();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Disables this Town Portal. Used on exit portals with no pending return trip.
        /// </summary>
        public virtual void Deactivate() => gameObject.SetActive(false);

        /// <summary>
        /// Marks this instance as a return portal. Called by Level.Initialize() on its
        /// Town Portal Exit.
        /// </summary>
        public virtual void SetAsReturnPortal() => m_isReturnPortal = true;

        /// <summary>
        /// Fast-forwards this Town Portal's particle systems to a steady, looping state,
        /// since they don't simulate while out of view. Only for a portal that already
        /// existed - a newly opened one should play its normal opening animation instead.
        /// </summary>
        public virtual void PrewarmParticles()
        {
            foreach (var particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Simulate(particlePrewarmDuration, withChildren: false, restart: true);
                particles.Play(withChildren: false);
            }
        }

        protected override void Start()
        {
            base.Start();
            all.Add(this);
        }

        protected virtual void OnDestroy() => all.Remove(this);

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
                Interact(other);
        }

        protected override void OnInteract(object other)
        {
            m_player.StandStill();
            GameAudio.instance.PlayEffect(travelClip);

            if (isReturnPortal)
                TravelToOrigin();
            else
                TravelToTown();
        }

        protected virtual void TravelToTown()
        {
            var sameScene =
                string.IsNullOrEmpty(m_level.townSceneName)
                || m_level.townSceneName == m_level.currentScene.name;

            if (sameScene)
            {
                if (!m_level.townPortalExit)
                    return;

                m_level.townPortalExit.Activate();
                var point = m_level.townPortalExit.GetSpacePoint();
                TeleportSameScene(point.position, point.rotation);
            }
            else
            {
                m_level.currentCharacter.usingTownPortal = true;
                GameScenes.instance.LoadScene(
                    m_level.townSceneName,
                    setAsCharacterScene: true,
                    playTransitionAudio: false
                );
            }
        }

        protected virtual void TravelToOrigin()
        {
            var character = m_level.currentCharacter;

            if (string.IsNullOrEmpty(character.townPortalReturnScene))
                return;

            var scene = character.townPortalReturnScene;
            var sameScene = scene == m_level.currentScene.name;

            if (sameScene)
                TeleportSameScene(
                    character.townPortalReturnPosition,
                    character.townPortalReturnRotation
                );
            else
            {
                GameScenes.instance.SetNextSceneCoordinates(
                    character.townPortalReturnPosition,
                    character.townPortalReturnRotation.eulerAngles
                );
                GameScenes.instance.LoadScene(
                    scene,
                    setAsCharacterScene: true,
                    playTransitionAudio: false
                );
            }
        }

        protected virtual void TeleportSameScene(Vector3 position, Quaternion rotation)
        {
            Fader.instance.FadeOut(() =>
            {
                m_level.player.Teleport(position, rotation);
                Fader.instance.FadeIn();
            });
        }
    }
}
