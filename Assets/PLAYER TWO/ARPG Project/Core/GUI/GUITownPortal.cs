using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Town Portal")]
    public class GUITownPortal : MonoBehaviour
    {
        [Header("Progress Bar Settings")]
        [Tooltip(
            "The Game Object that holds the progress bar UI, shown while the portal is opening."
        )]
        public GameObject progressGroup;

        [Tooltip("The Image used as the progress bar fill.")]
        public Image progressImage;

        [Header("Feedback Settings")]
        [Tooltip(
            "The Game Object that holds the feedback text UI, shown briefly when the portal fails to open."
        )]
        public GameObject feedbackGroup;

        [Tooltip("The TMP Text used to show why the portal failed to open.")]
        public TMP_Text feedbackText;

        [Tooltip("The message shown when there are enemies nearby.")]
        public string enemiesNearbyMessage = "Enemies nearby!";

        [Tooltip("The message shown when too close to another Town Portal.")]
        public string tooCloseMessage = "Too close to another Town Portal!";

        [Tooltip("The message shown when too close to the Level's Town Portal Exit.")]
        public string tooCloseToLevelExitMessage = "Too close to the Town!";

        [Tooltip("The message shown when too close to a Waypoint.")]
        public string tooCloseToWaypointMessage = "Too close to a Waypoint!";

        [Tooltip("How long the feedback text stays visible.")]
        public float feedbackDuration = 2f;

        protected Coroutine m_progressRoutine;
        protected Coroutine m_feedbackRoutine;

        protected EntityPortalOpener m_opener =>
            Level.instance.player.GetComponent<EntityPortalOpener>();

        protected virtual void InitializeCallbacks()
        {
            var opener = m_opener;

            if (!opener)
                return;

            opener.onOpenStarted.AddListener(OnOpenStarted);
            opener.onOpenCompleted.AddListener(OnOpenFinished);
            opener.onOpenCancelled.AddListener(OnOpenFinished);
            opener.onEnemiesNearby.AddListener(() => ShowFeedback(enemiesNearbyMessage));
            opener.onTooCloseToAnotherPortal.AddListener(() => ShowFeedback(tooCloseMessage));
            opener.onTooCloseToLevelExit.AddListener(
                () => ShowFeedback(tooCloseToLevelExitMessage)
            );
            opener.onTooCloseToWaypoint.AddListener(() => ShowFeedback(tooCloseToWaypointMessage));
        }

        protected virtual void OnOpenStarted()
        {
            if (progressGroup)
                progressGroup.SetActive(true);

            if (m_progressRoutine != null)
                StopCoroutine(m_progressRoutine);

            m_progressRoutine = StartCoroutine(ProgressRoutine(m_opener.channelDuration));
        }

        protected virtual void OnOpenFinished()
        {
            if (progressGroup)
                progressGroup.SetActive(false);
        }

        protected virtual IEnumerator ProgressRoutine(float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (progressImage)
                    progressImage.fillAmount = elapsed / duration;

                yield return null;
            }
        }

        protected virtual void ShowFeedback(string message)
        {
            if (feedbackText)
                feedbackText.text = message;

            if (feedbackGroup)
                feedbackGroup.SetActive(true);

            if (m_feedbackRoutine != null)
                StopCoroutine(m_feedbackRoutine);

            m_feedbackRoutine = StartCoroutine(HideFeedbackRoutine());
        }

        protected virtual IEnumerator HideFeedbackRoutine()
        {
            yield return new WaitForSeconds(feedbackDuration);

            if (feedbackGroup)
                feedbackGroup.SetActive(false);

            m_feedbackRoutine = null;
        }

        protected virtual void Start()
        {
            if (progressGroup)
                progressGroup.SetActive(false);

            if (feedbackGroup)
                feedbackGroup.SetActive(false);

            InitializeCallbacks();
        }
    }
}
