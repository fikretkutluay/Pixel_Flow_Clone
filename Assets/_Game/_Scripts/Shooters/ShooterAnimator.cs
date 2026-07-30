using System;
using DG.Tweening;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Presentation for a shooter: the hop between rail and park, the queue shuffle,
    /// and the spin-out when it runs dry. Kept apart from <see cref="Shooter"/> so
    /// that class stays about state, not looks.
    ///
    /// Nothing here decides anything — the controllers still own position; this only
    /// animates the way there. While a shooter is on the rail, TrackController writes
    /// its position every frame, so no positional tween may run then. That is why the
    /// launch reads as a squash rather than a jump.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShooterAnimator : MonoBehaviour
    {
        [Header("Hop — rail to park")]
        [SerializeField] private float hopPower = 0.9f;
        [SerializeField] private float hopDuration = 0.38f;
        [SerializeField] private float landSquash = 0.22f;

        [Header("Launch — park or queue to rail")]
        [SerializeField] private float launchStretch = 0.3f;
        [SerializeField] private float launchDuration = 0.28f;

        [Header("Queue shuffle")]
        [SerializeField] private float slideDuration = 0.3f;

        [Header("Reveal — the \"?\" opening up")]
        [SerializeField] private float revealSquash = 0.22f;
        [SerializeField] private float revealDuration = 0.34f;

        [Header("Spent — spins up, then shrinks away")]
        [SerializeField] private float spinDuration = 0.42f;
        [SerializeField] private int spinTurns = 2;

        private Transform body;
        private Vector3 bodyBaseScale;
        private Vector3 rootBaseScale;
        private Sequence move;
        private Sequence flourish;

        private void Awake()
        {
            Shooter shooter = GetComponent<Shooter>();
            body = shooter != null && shooter.Body != null ? shooter.Body : transform;
            bodyBaseScale = body.localScale;
            rootBaseScale = transform.localScale;
        }

        // Pooled objects come back mid-animation, so everything resets on the way out.
        private void OnDisable()
        {
            move?.Kill();
            flourish?.Kill();
            move = flourish = null;
            body.localScale = bodyBaseScale;
            transform.localScale = rootBaseScale;
            body.DOKill();
            transform.DOKill();
        }

        /// <summary>Arcs into a park slot and squashes on landing.</summary>
        public void HopTo(Vector3 target)
        {
            move?.Kill();
            transform.DOKill();

            move = DOTween.Sequence();
            move.Append(transform.DOJump(target, hopPower, 1, hopDuration).SetEase(Ease.OutQuad));
            move.Join(body.DOScale(Squash(landSquash * 0.5f), hopDuration * 0.45f)
                          .SetEase(Ease.OutQuad));
            move.Append(body.DOScale(Squash(landSquash), hopDuration * 0.2f).SetEase(Ease.OutQuad));
            move.Append(body.DOScale(bodyBaseScale, hopDuration * 0.45f).SetEase(Ease.OutBack));
            move.OnComplete(() => move = null);
        }

        /// <summary>
        /// Sent back out to the rail. Stretches rather than moves: the rail claims
        /// this shooter's position on the very next frame.
        /// </summary>
        public void PunchLaunch()
        {
            flourish?.Kill();
            body.DOKill();
            body.localScale = bodyBaseScale;

            flourish = DOTween.Sequence();
            flourish.Append(body.DOScale(Squash(-launchStretch), launchDuration * 0.3f)
                                .SetEase(Ease.OutQuad));
            flourish.Append(body.DOScale(bodyBaseScale, launchDuration * 0.7f)
                                .SetEase(Ease.OutBack));
            flourish.OnComplete(() => flourish = null);
        }

        /// <summary>
        /// The mystery shooter opening up as it reaches the front of the queue.
        /// Squashes and springs back while <see cref="Shooter"/> lifts the "?" off
        /// the body — the two run together and read as one beat.
        /// </summary>
        public void PunchReveal()
        {
            flourish?.Kill();
            body.DOKill();
            body.localScale = bodyBaseScale;

            flourish = DOTween.Sequence();
            flourish.Append(body.DOScale(Squash(revealSquash), revealDuration * 0.35f)
                                .SetEase(Ease.OutQuad));
            flourish.Append(body.DOScale(bodyBaseScale, revealDuration * 0.65f)
                                .SetEase(Ease.OutBack));
            flourish.OnComplete(() => flourish = null);
        }

        /// <summary>Slides up a place in the queue. Staggered so a column ripples.</summary>
        public void SlideTo(Vector3 target, float delay = 0f)
        {
            move?.Kill();
            transform.DOKill();

            move = DOTween.Sequence();
            if (delay > 0f) move.AppendInterval(delay);
            move.Append(transform.DOMove(target, slideDuration).SetEase(Ease.OutBack));
            move.Join(body.DOScale(Squash(-0.12f), slideDuration * 0.35f).SetEase(Ease.OutQuad));
            move.Insert(delay + slideDuration * 0.35f,
                        body.DOScale(bodyBaseScale, slideDuration * 0.65f).SetEase(Ease.OutBack));
            move.OnComplete(() => move = null);
        }

        /// <summary>
        /// Out of ammo: spins on its own axis and, as it comes back upright, shrinks
        /// away. <paramref name="onComplete"/> is where the caller returns it to the
        /// pool — do not return it before this fires.
        /// </summary>
        public void PlaySpentExit(Action onComplete)
        {
            flourish?.Kill();
            move?.Kill();
            body.DOKill();
            transform.DOKill();

            flourish = DOTween.Sequence();
            flourish.Append(body.DOLocalRotate(new Vector3(0f, 0f, -360f * spinTurns), spinDuration,
                                               RotateMode.LocalAxisAdd)
                                .SetEase(Ease.InOutQuad));
            flourish.Join(body.DOScale(bodyBaseScale * 1.18f, spinDuration * 0.35f)
                              .SetEase(Ease.OutQuad));
            // Shrink starts late, so the vanish lands as the spin finishes upright.
            flourish.Insert(spinDuration * 0.55f,
                            transform.DOScale(Vector3.zero, spinDuration * 0.45f)
                                     .SetEase(Ease.InBack));
            flourish.OnComplete(() =>
            {
                flourish = null;
                onComplete?.Invoke();
            });
        }

        /// <summary>Positive squashes (wide and short), negative stretches.</summary>
        private Vector3 Squash(float amount)
        {
            return new Vector3(bodyBaseScale.x * (1f + amount),
                               bodyBaseScale.y * (1f - amount),
                               bodyBaseScale.z * (1f + amount));
        }
    }
}
