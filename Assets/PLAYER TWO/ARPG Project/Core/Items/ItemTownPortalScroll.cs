using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    [CreateAssetMenu(
        fileName = "New Town Portal Scroll",
        menuName = "PLAYER TWO/ARPG Project/Item/Town Portal Scroll"
    )]
    public class ItemTownPortalScroll : ItemConsumable
    {
        /// <summary>
        /// Starts opening a Town Portal through the Entity's Entity Portal Opener. The scroll is
        /// only actually removed from the inventory once the portal finishes opening; if the
        /// safety checks fail, the channel is cancelled, or no valid position is found, the
        /// scroll stays in the inventory and <paramref name="onConsumed"/> is never invoked.
        /// </summary>
        /// <param name="entity">The Entity attempting to open the Town Portal.</param>
        /// <param name="instance">The Item Instance being consumed.</param>
        /// <param name="onConsumed">Invoked once the Town Portal has actually opened.</param>
        public override void Consume(Entity entity, ItemInstance instance, System.Action onConsumed)
        {
            if (!CanConsume(entity) || !entity.TryGetComponent(out EntityPortalOpener opener))
                return;

            UnityAction onCompleted = null;
            UnityAction onCancelled = null;
            UnityAction onNoValidPosition = null;

            void StopListening()
            {
                opener.onOpenCompleted.RemoveListener(onCompleted);
                opener.onOpenCancelled.RemoveListener(onCancelled);
                opener.onNoValidPositionFound.RemoveListener(onNoValidPosition);
            }

            onCompleted = () =>
            {
                StopListening();
                base.Consume(entity, instance, onConsumed);
            };

            onCancelled = StopListening;
            onNoValidPosition = StopListening;

            opener.onOpenCompleted.AddListener(onCompleted);
            opener.onOpenCancelled.AddListener(onCancelled);
            opener.onNoValidPositionFound.AddListener(onNoValidPosition);

            if (!opener.Open())
                StopListening();
        }

        /// <summary>
        /// Returns true if the given Entity has an Entity Portal Opener that isn't already
        /// channeling a Town Portal.
        /// </summary>
        /// <param name="entity">The Entity attempting to consume the item.</param>
        public override bool CanConsume(Entity entity) =>
            entity.TryGetComponent(out EntityPortalOpener opener) && !opener.isOpening;

        protected virtual void Reset() => consumeImmediately = true;
    }
}
