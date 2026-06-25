namespace MatchFactory.Animation
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Bezier arc animation cho item bay từ board đến collection slot.
    /// Quadratic Bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
    /// </summary>
    public class ItemFlyAnimation : MonoBehaviour
    {
        [SerializeField] private float flyDuration = 0.35f;
        [SerializeField] private float arcHeight = 2.5f;
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// Bay item từ vị trí hiện tại đến destination theo đường cong Bezier.
        /// Scale down nhẹ khi đến nơi.
        /// </summary>
        public IEnumerator FlyTo(Transform item, Vector3 destination, System.Action onComplete = null)
        {
            Vector3 start = item.position;
            Vector3 mid = (start + destination) * 0.5f + Vector3.up * arcHeight;
            Vector3 startScale = item.localScale;
            Vector3 endScale = startScale * 0.65f;

            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float easedT = speedCurve.Evaluate(t);

                float u = 1f - easedT;
                item.position = u * u * start + 2f * u * easedT * mid + easedT * easedT * destination;
                item.localScale = Vector3.Lerp(startScale, endScale, easedT);

                // Slight random rotation during flight
                item.Rotate(Vector3.up * 180f * Time.deltaTime, Space.World);

                yield return null;
            }

            item.position = destination;
            item.localScale = endScale;
            onComplete?.Invoke();
        }
    }
}
