using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    public abstract class ItemConsumable : Item
    {
        [Header("Consumable Settings")]
        [Tooltip(
            "If true, right-clicking this item in the inventory consumes it immediately. If "
                + "false, it's equipped to a consumable slot instead, to be consumed from there."
        )]
        public bool consumeImmediately;

        [Tooltip("The sound played when this Consumable is consumed.")]
        public AudioClip consumeSound;

        [Tooltip("The Particle System played when this Consumable is consumed.")]
        public ParticleSystem consumeParticles;

        [Tooltip("An offset for the Particle System played when this Consumable is consumed.")]
        public Vector3 particleOffset;

        [Header("Instruction Settings")]
        [Tooltip("The instruction to show when inspecting this Consumable.")]
        public string pcInstruction = "Press 'Right-Click' to equip";

        [Tooltip("The instruction to show when inspecting this Consumable on mobile.")]
        public string mobileInstruction = "Double Tap to equip";

        /// <summary>
        /// Returns true if this Consumable can currently be consumed by the given Entity. Lets
        /// callers cheaply skip a Consume attempt (and avoid allocating a callback for it) when
        /// it's already known to fail, e.g. while a Town Portal Scroll is already channeling.
        /// </summary>
        /// <param name="entity">The Entity attempting to consume the item.</param>
        public virtual bool CanConsume(Entity entity) => true;

        /// <summary>
        /// Consumes the item and applies its effects to the given entity.
        /// </summary>
        /// <param name="entity">The Entity you want to apply the effects to.</param>
        /// <param name="instance">The Item Instance being consumed.</param>
        /// <param name="onConsumed">
        /// Invoked once the item should actually be removed from wherever it's stored. Call it
        /// synchronously for an effect that always succeeds immediately, or defer it (or never
        /// call it) for an effect that completes asynchronously and can still fail, such as a
        /// Town Portal Scroll.
        /// </param>
        public virtual void Consume(Entity entity, ItemInstance instance, System.Action onConsumed)
        {
            if (consumeParticles)
            {
                var particlePosition = entity.position + particleOffset;
                Instantiate(
                        consumeParticles,
                        particlePosition,
                        Quaternion.identity,
                        entity.transform
                    )
                    .Play();
            }

            if (consumeSound && GameAudio.instance)
                GameAudio.instance.PlayEffect(consumeSound);

            onConsumed?.Invoke();
        }
    }
}
