namespace Fusion.Collywobbles.Futsal {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class KeeperAnimationHandler : MonoBehaviour
    {
        public float DiveTimeMid => _diveTimeMid;
        public float DiveTimeLow => _diveTimeLow;
        public float CatchMidriffTime => _catchMidriffTime;

        KeeperController keeper;
        Animator _keeperAnimator;

        private float _diveTimeMid;
        private float _diveTimeLow;
        private float _catchMidriffTime;

        private int _keeperIdle;
        private int _keeperIdleWithBall;

        private void Awake()
        {
            keeper = transform.GetParentComponent<KeeperController>();
            _keeperAnimator = GetComponent<Animator>();

            _diveTimeMid = 0.7f;
            _diveTimeLow = 0.8f;
            _catchMidriffTime = 0.7f;

            //UpdateAnimClipTimes();

            _keeperIdle = Animator.StringToHash("Keeper_Idle");
            _keeperIdleWithBall = Animator.StringToHash("IdleWithBall");
        }

        public bool IsIdle()
        {
            int state = _keeperAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            //Debug.Log("Current animation state hash = " + state);
            //Debug.Log("IDLE_STATE = " + IDLE_STATE);

            return state == _keeperIdle;
        }

        public bool IsIdleWithBall()
        {
            int state = _keeperAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            return state == _keeperIdleWithBall;
        }

        public void GoalScored()
        {
            _keeperAnimator.SetBool("IsGoal", true);
        }

        public void DiveLeftLow()
        {
            _keeperAnimator.SetBool("DiveLeftLow", true);
        }

        public void DiveRightLow()
        {
            _keeperAnimator.SetBool("DiveRightLow", true);
        }

        public void DiveLeft()
        {
            _keeperAnimator.SetBool("DiveLeft", true);
        }

        public void DiveRight()
        {
            _keeperAnimator.SetBool("DiveRight", true);
        }

        public void CatchMidriff()
        {
            _keeperAnimator.SetBool("CatchMidriff", true);
        }

        /** Not used
        public void UpdateAnimClipTimes()
        {
            AnimationClip[] clips = _keeperAnimator.runtimeAnimatorController.animationClips;

            foreach (AnimationClip clip in clips)
            {
                switch (clip.name)
                {
                    case "SaveLeftMid":
                        _diveLeftTime = clip.length;
                        break;

                    case "SaveRightMid":
                        _diveRightTime = clip.length;
                        break;

                    case "CatchMidriff":
                        _catchMidriffTime = clip.length;
                        break;

                    default:
                        Debug.Log("No time set for clip " + clip.name);
                        break;
                }
            }
        }
        */

        #region Callback events from keeper animator


        public void AnimEvent_CatchMidriffEnd()
        {
            _keeperAnimator.SetBool("CatchMidriff", false);

            keeper.EndCatchMidriff();
        }

        public void AnimEvent_EndDive()
        {
            Debug.Log("[Keeper] Ending dive animation");

            _keeperAnimator.SetBool("DiveRight", false);
            _keeperAnimator.SetBool("DiveLeft", false);
            _keeperAnimator.SetBool("DiveRightLow", false);
            _keeperAnimator.SetBool("DiveLeftLow", false);

            keeper.EndDive();
        }

        public void AnimEvent_EndGoal()
        {
            _keeperAnimator.SetBool("IsGoal", false);

            //keeper.EndGoalAnimation();
        }

        public void AnimEvent_RollPassRelease()
        {
            keeper.MakeRollPass();
        }

        #endregion
    }
}
