using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/Collectible/Collectible Item")]
    public class CollectibleItem : Collectible
    {
        [Header("Audio Settings")]
        [Tooltip("The Audio Clip that plays when the Collectible is dropped.")]
        public AudioClip dropRegularClip;

        [Tooltip("The Audio Clip that plays when the Collectible is dropped with an Item Armor.")]
        public AudioClip dropArmorClip;

        [Tooltip("The Audio Clip that plays when the Collectible is dropped with an Item Weapon.")]
        public AudioClip dropWeaponClip;

        [Tooltip("The Audio Clip that plays when collecting.")]
        public AudioClip collectRegularClip;

        [Tooltip("The Audio Clip that plays when collecting an Item Armor.")]
        public AudioClip collectArmorClip;

        [Tooltip("The Audio Clip that plays when collecting an Item Weapon.")]
        public AudioClip collectWeaponClip;

        [Header("Rarity Particle Settings")]
        [Tooltip(
            "A child Particle System tinted with the item's rarity color and toggled on or off based on "
                + "the rarity's Show Rarity Particle setting."
        )]
        public ParticleSystem rarityParticle;

        /// <summary>
        /// Returns the Item Instance of this Collectible.
        /// </summary>
        public ItemInstance item { get; protected set; }

        protected GameAudio m_audio => GameAudio.instance;

        protected bool m_showRarityParticle;

        /// <summary>
        /// Sets the Item Instance of this Collectible Item.
        /// </summary>
        /// <param name="item">The Item Instance you want to set to this Collectible.</param>
        public virtual void SetItem(ItemInstance item)
        {
            this.item = item;

            var position = transform.position + item.data.dropPosition;
            var rotation = Quaternion.Euler(item.data.dropRotation);

            Instantiate(this.item.data.prefab, position, rotation, transform);
            PlayDropClip();
            UpdateRarityParticle();
        }

        public override string GetName() => item.GetDisplayName();

        public override Color GetNameColor() => item.GetRarityColor(nameColor);

        protected override bool TryCollect(Inventory inventory)
        {
            if (inventory.TryAddOrStack(item))
            {
                PlayCollectClip();
                return true;
            }

            m_audio.PlayDeniedSound();
            return false;
        }

        /// <summary>
        /// Plays the drop Audio Clip that matches the item's type, using the item's rarity override
        /// when one is set instead of this Collectible's own clip.
        /// </summary>
        protected virtual void PlayDropClip()
        {
            if (Level.TimeSinceLevelStart < 0.1f)
                return;

            var rarity = item.GetRarity();

            if (item.IsArmor())
                m_audio.PlayEffect(
                    ResolveClip(dropArmorClip, rarity != null ? rarity.dropArmorClip : null)
                );
            else if (item.IsWeapon())
                m_audio.PlayEffect(
                    ResolveClip(dropWeaponClip, rarity != null ? rarity.dropWeaponClip : null)
                );
            else
                m_audio.PlayEffect(
                    ResolveClip(dropRegularClip, rarity != null ? rarity.dropRegularClip : null)
                );
        }

        /// <summary>
        /// Plays the collect Audio Clip that matches the item's type, using the item's rarity override
        /// when one is set instead of this Collectible's own clip.
        /// </summary>
        protected virtual void PlayCollectClip()
        {
            var rarity = item.GetRarity();

            if (item.IsArmor())
                m_audio.PlayEffect(
                    ResolveClip(collectArmorClip, rarity != null ? rarity.collectArmorClip : null)
                );
            else if (item.IsWeapon())
                m_audio.PlayEffect(
                    ResolveClip(collectWeaponClip, rarity != null ? rarity.collectWeaponClip : null)
                );
            else
                m_audio.PlayEffect(
                    ResolveClip(
                        collectRegularClip,
                        rarity != null ? rarity.collectRegularClip : null
                    )
                );
        }

        /// <summary>
        /// Returns <paramref name="overrideClip"/> when set, otherwise falls back to <paramref name="regular"/>.
        /// </summary>
        protected virtual AudioClip ResolveClip(AudioClip regular, AudioClip overrideClip) =>
            overrideClip ? overrideClip : regular;

        /// <summary>
        /// Reads the item's rarity, tints the rarity particle with its color, and caches whether it
        /// should be shown. The particle itself is only enabled once this Collectible becomes ready
        /// (see <see cref="OnReady"/>) so it never appears mid spawn animation.
        /// </summary>
        protected virtual void UpdateRarityParticle()
        {
            if (!rarityParticle)
                return;

            var rarity = item.GetRarity();
            m_showRarityParticle = rarity != null && rarity.showRarityParticle;

            if (m_showRarityParticle)
            {
                var main = rarityParticle.main;
                main.startColor = rarity.color;
            }

            rarityParticle.gameObject.SetActive(false);
        }

        protected override void OnReady()
        {
            base.OnReady();

            if (rarityParticle)
                rarityParticle.gameObject.SetActive(m_showRarityParticle);
        }
    }
}
