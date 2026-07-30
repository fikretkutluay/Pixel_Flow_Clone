using DG.Tweening;
using MobileCore;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// The little white ball a shooter spits at a cube, trailing its own colour.
    /// Purely cosmetic: the cube is already broken by the time this flies, the
    /// break animation is simply held back until it lands.
    /// </summary>
    [DisallowMultipleComponent]
    public class Tracer : MonoBehaviour
    {
        public const string PoolTag = "Tracer";

        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Renderer ball;

        [Tooltip("Seconds of flight per world unit — short shots stay snappy.")]
        [SerializeField] private float secondsPerUnit = 0.014f;
        [SerializeField] private float minDuration = 0.05f;
        [SerializeField] private float maxDuration = 0.16f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;
        private Tween flight;

        /// <summary>How long a shot between these two points will take.</summary>
        public float DurationFor(Vector3 from, Vector3 to)
        {
            return Mathf.Clamp(Vector3.Distance(from, to) * secondsPerUnit, minDuration, maxDuration);
        }

        public void Fire(Vector3 from, Vector3 to, Color trailColor)
        {
            transform.position = from;
            Tint(trailColor);

            if (trail != null)
            {
                trail.Clear();               // otherwise it streaks in from wherever it last died
                trail.emitting = true;
            }

            flight?.Kill();
            flight = transform.DOMove(to, DurationFor(from, to))
                              .SetEase(Ease.Linear)
                              .OnComplete(Despawn);
        }

        private void Tint(Color c)
        {
            if (trail != null)
            {
                trail.startColor = c;
                trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }

            // The ball itself stays white; only the trail carries the colour.
            if (ball == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            ball.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, Color.white);
            ball.SetPropertyBlock(mpb);
        }

        private void Despawn()
        {
            flight = null;
            if (trail != null) trail.emitting = false;
            ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
        }

        private void OnDisable()
        {
            flight?.Kill();
            flight = null;
            transform.DOKill();
            if (trail != null) trail.Clear();
        }
    }
}
