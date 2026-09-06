using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/Collectible/Collectible Animation")]
    public class CollectibleAnimation : MonoBehaviour
    {
        [Header("Hop Settings")]
        [Tooltip("The height, in units, this Collectible hops when its spawn animation plays.")]
        public float hopHeight = 1f;

        [Tooltip("The duration, in seconds, of the spawn hop.")]
        public float hopDuration = 0.6f;

        [Header("Rotation Settings")]
        [Tooltip(
            "Total degrees pitched (tumbled forward around the local X axis) while hopping. Set to 0 to disable rotation."
        )]
        public float spinAngle = 0f;

        public UnityEvent onComplete;

        /// <summary>
        /// Plays the spawn hop (and optional spin) animation from the current position and rotation.
        /// </summary>
        public virtual void Play() => StartCoroutine(AnimateRoutine());

        protected virtual IEnumerator AnimateRoutine()
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var elapsedTime = 0f;

            while (elapsedTime < hopDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / hopDuration);
                transform.position = startPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * hopHeight;

                if (spinAngle != 0f)
                    transform.rotation = startRotation * Quaternion.Euler(spinAngle * t, 0f, 0f);

                yield return null;
            }

            transform.position = startPosition;

            if (spinAngle != 0f)
                transform.rotation = startRotation * Quaternion.Euler(spinAngle, 0f, 0f);

            onComplete.Invoke();
        }
    }
}
