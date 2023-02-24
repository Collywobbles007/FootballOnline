namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using UnityEngine;

    public enum Foot {
        Left,
        Right
    }

    public class SimplePlayerAnimator : MonoBehaviour
    {
        public Foot CurrentFoot;
        public bool StandingOnBall = false;

        private ThirdPersonPlayer playerController;
        private Animator footballerAnimator;
        private int _idleWithBallLayer;
        private float _currentWeight;


        /**************************
         * Initialise the animator
         **************************/
        public Animator InitAnimator()
        {
            footballerAnimator = GetComponent<Animator>();
            playerController = transform.parent.GetComponent<ThirdPersonPlayer>();

            _idleWithBallLayer = footballerAnimator.GetLayerIndex("IdleWithBall");

            return footballerAnimator;
        }

        /************************
         * End running animation
         ************************/
        public void StopRunning(bool hasBall)
        {
            footballerAnimator.SetBool("IsRunning", false);

            if (hasBall)
            {
                // Enable idle with ball animation layer when player stops with ball
                var weightVal = Mathf.SmoothDamp(footballerAnimator.GetLayerWeight(_idleWithBallLayer), 1.0f, ref _currentWeight, 0.1f);
                footballerAnimator.SetLayerWeight(_idleWithBallLayer, weightVal);
            }
            else
            {
                footballerAnimator.SetLayerWeight(_idleWithBallLayer, 0.0f);
            }
        }

        /****************************
         * Trigger running animation
         ****************************/
        public void StartRunning(bool hasBall)
        {
            if (footballerAnimator.GetBool("IsRunning")) return;

            // Start running animation
            footballerAnimator.SetBool("IsRunning", true);

            // If player has ball, temporarily disable the idle with ball layer whilst running
            if (hasBall)
            {
                footballerAnimator.SetLayerWeight(_idleWithBallLayer, 0.0f);
            }
        }

        /********************************
         * Trigger a kick ball animation
         ********************************/
        public void TriggerKickBall(float delay, bool isHigh)
        {
            StartCoroutine(DelayKickBall(delay, isHigh));
        }

        /****************************
         * Trigger a shoot animation
         ****************************/
        public void TriggerTakeShot(float delay)
        {
            StartCoroutine(DelayKickBall(delay, true));
        }

        public void TriggerPlayerTackled()
        {
            footballerAnimator.SetBool("IsTackled", true);
            footballerAnimator.SetBool("HasBall", false);
        }

        public void TriggerTacklingAnimation()
        {
            footballerAnimator.SetBool("MakeTackle", true);
            footballerAnimator.SetBool("HasBall", true);
        }


        #region Animation Events

        public void AnimEvent_KickBall()
        {
            playerController.KickBall(false);
        }

        public void AnimEvent_KickBallEnd()
        {
            footballerAnimator.SetBool("PassBall", false);
            footballerAnimator.SetBool("PassBallHigh", false);
            footballerAnimator.SetBool("HasBall", false);

            playerController.KickBallEnd();
        }

        public void AnimEvent_RunPass()
        {
            playerController.KickBall(true);
        }

        public void AnimEvent_RunPassEnd()
        {
            footballerAnimator.SetBool("PassBall", false);
            footballerAnimator.SetBool("PassBallHigh", false);
            footballerAnimator.SetBool("HasBall", false);

            playerController.KickBallEnd();
        }

        public void AnimEvent_SwitchFoot(int foot)
        {
            // foot: 1 = right foot, 0 = left foot
            CurrentFoot = foot == 1 ? Foot.Right : Foot.Left;
        }


        public void AnimEvent_StandOnBall(int toggle)
        {
            StandingOnBall = toggle == 1;
        }

        public void AnimEvent_EndTackle()
        {
            footballerAnimator.SetBool("MakeTackle", false);
        }

        public void AnimEvent_EndStumbleBack()
        {
            footballerAnimator.SetBool("IsTackled", false);

            playerController.StumbleEnd();
        }

        #endregion

        #region Private Methods

        /***********************************************
         * Delay start of pass animation on client
         * so that it lines up with server ball movement
         ***********************************************/
        private IEnumerator DelayKickBall(float delay, bool isHigh)
        {
            yield return new WaitForSeconds(delay);

            // Disable idle with ball layer
            footballerAnimator.SetLayerWeight(_idleWithBallLayer, 0.0f);

            // Trigger the kick animation
            if (isHigh)
            {
                footballerAnimator.SetBool("PassBallHigh", true);
                SoundManager.PlaySound(SoundManager.Effect.KickHard, 1.0f);
            }
            else
            {
                footballerAnimator.SetBool("PassBall", true);
                SoundManager.PlaySound(SoundManager.Effect.KickSoft, 1.0f);
            }
        }

        #endregion

    }
}
