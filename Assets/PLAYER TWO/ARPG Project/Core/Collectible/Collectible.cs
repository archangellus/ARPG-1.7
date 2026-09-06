using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Collider))]
    public abstract class Collectible : Interactive
    {
        [Header("GUI Name Settings")]
        [Tooltip("The color of the text on the GUI Collectible.")]
        public Color nameColor = Color.white;

        [Header("Spawn Animation Settings")]
        [Tooltip(
            "If false, this Collectible skips its spawn animation and becomes immediately interactive. "
                + "Set to false automatically when restored from saved level data."
        )]
        public bool playSpawnAnimation = true;

        public UnityEvent onCollect;

        /// <summary>
        /// All Collectibles currently active in the scene. Populated on Start, cleared on OnDestroy.
        /// </summary>
        public static readonly List<Collectible> all = new();

        protected CollectibleAnimation m_animation;

        protected virtual void InitializeCanvas()
        {
            if (GUICollectibles.instance)
                GUICollectibles.instance.Add(this);
        }

        protected override void InitializeTag() => tag = GameTags.Collectible;

        /// <summary>
        /// Marks this Collectible as being gathered: disables interaction and hides the GUI label.
        /// Called by auto-gathering systems before moving the item toward the Entity.
        /// </summary>
        public virtual void StartGathering()
        {
            interactive = false;

            if (GUICollectibles.instance)
                GUICollectibles.instance.Hide(this);
        }

        /// <summary>
        /// Collects this Collectible.
        /// </summary>
        /// <param name="other">The object that is collecting this Collectible.</param>
        public virtual void Collect(object other)
        {
            onCollect.Invoke();

            if (GUICollectibles.instance)
                GUICollectibles.instance.Remove(this);

            Destroy(gameObject);
        }

        protected override void OnInteract(object other)
        {
            if (other is Entity && TryCollect((other as Entity).inventory.instance))
                Collect(other);
        }

        /// <summary>
        /// Returns the name of the Item on the Collectible.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Returns the color used to display this Collectible's name label.
        /// Defaults to <see cref="nameColor"/>; subclasses can override to drive color from data.
        /// </summary>
        public virtual Color GetNameColor() => nameColor;

        protected abstract bool TryCollect(Inventory inventory);

        protected override void Start()
        {
            base.Start();
            all.Add(this);
            TryGetComponent(out m_animation);

            if (playSpawnAnimation && m_animation)
            {
                interactive = false;
                m_animation.onComplete.AddListener(OnSpawnAnimationComplete);
                m_animation.Play();
            }
            else
            {
                OnReady();
            }
        }

        protected virtual void OnSpawnAnimationComplete()
        {
            interactive = true;
            OnReady();
        }

        /// <summary>
        /// Called once this Collectible becomes fully available: immediately on Start if it has no
        /// spawn animation to play, or once that animation completes otherwise. Shows the GUI name label.
        /// </summary>
        protected virtual void OnReady()
        {
            InitializeCanvas();
        }

        protected virtual void OnDestroy()
        {
            all.Remove(this);
        }
    }
}
