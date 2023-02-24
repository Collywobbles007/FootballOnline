namespace Fusion.Collywobbles.Futsal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class KeeperController : NetworkBehaviour
    {
        public enum SAVETYPE
        {
            None,
            LowLeft,
            LowRight,
            CatchMidriff
        }

        [SerializeField] private Transform _rightHand;
        [SerializeField] protected bool _holdBall;

        // Server ball vars
        protected GameObject _serverBallObject;
        protected ServerBall _serverBall;

        protected bool _ballIncoming = false;
        protected Vector3 _ballDestination = Vector3.zero;
        //private float _ballSpeed;
        protected float _ballDistance;
        protected float _timeToArrival;
        protected float _shotDistance;

        protected KeeperAnimationHandler _animationHandler;

        private bool _isAnimating;

        public override void Spawned()
        {
            base.Spawned();

            // Get reference to the server ball
            _serverBallObject = GameObject.FindGameObjectWithTag("Ball");
            _serverBall = _serverBallObject.GetComponent<ServerBall>();

            _animationHandler = transform.GetChild(0).GetComponent<KeeperAnimationHandler>();

            _holdBall = false;
            _isAnimating = false;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (_animationHandler.IsIdle())
            {
                //transform.LookAt(_serverBallObject.transform);

                Vector3 lookPos = _serverBallObject.transform.position - transform.position;
                lookPos.y = 0;
                Quaternion lookRot = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.fixedDeltaTime);
            }
            else if (_animationHandler.IsIdleWithBall())
            {
                Quaternion lookRot = Quaternion.LookRotation(new Vector3(20.0f, 0.0f, 0.0f));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.fixedDeltaTime);
            }

            if (Object.HasStateAuthority)
            {
                if (_ballIncoming)
                {
                    _ballDistance = GetBallDistance();
                    _timeToArrival -= Time.fixedDeltaTime;

                    //Debug.Log("_timeToArrival = " + _timeToArrival);

                    CheckForSave();
                }
            }
        }

        public void PrepareToSave(Vector3 goalEntryPoint, float travelTime, float distance)
        {
            _ballIncoming = true;
            _ballDestination = goalEntryPoint;
            _timeToArrival = travelTime;
            _shotDistance = distance;
        }

        public void EndGoalAnimation()
        {

        }

        public void EndDive()
        {
            _isAnimating = false;
        }

        public void EndCatchMidriff()
        {
            _holdBall = true;

            if (Object.HasStateAuthority)
            {
                _serverBall.KeeperCatchBall(_rightHand);
            }
        }

        public void MakeRollPass()
        {
            _holdBall = false;

            if (Object.HasStateAuthority)
            {
                _serverBall.transform.rotation = Quaternion.identity;
                _serverBall.KickBall(new Vector3(10.0f, 0.0f, 0.0f));
            }
        }

        protected void CheckForSave()
        {
            float side = GetBallSide(new Vector2(_ballDestination.x, _ballDestination.z));
            float height = _ballDestination.y;

            //Debug.Log("[BlueKeeper] Time to dive!!");
            //Debug.Log("[BlueKeeper] side = " + side);
            //Debug.Log("[BlueKeeper] time until arrival = " + _timeToArrival);
            //Debug.Log("[BlueKeeper] animationTime = " + _animationHandler.DiveTimeMid);

            if (Mathf.Abs(side) <= 1.0f)
            {
                // Ball coming straight at keeper
                if (height < 2.0f && _timeToArrival <= _animationHandler.CatchMidriffTime)
                {
                    RPC_CatchMidriff();
                    _ballIncoming = false;
                    _isAnimating = true;
                }
            }
            else if (side < -0.1f)
            {
                // Ball is to the left
                if (height >= 1.1f && _timeToArrival <= _animationHandler.DiveTimeMid)
                {
                    RPC_DiveLeft();
                    _ballIncoming = false;
                    _isAnimating = true;

                }
                else if (height < 1.1f && _timeToArrival <= _animationHandler.DiveTimeLow)
                {
                    RPC_DiveLeftLow();
                    _ballIncoming = false;
                    _isAnimating = true;
                }
            }
            else
            {
                // Ball is to the right
                if (height >= 1.1f && _timeToArrival <= _animationHandler.DiveTimeMid)
                {
                    RPC_DiveRight();
                    _ballIncoming = false;
                    _isAnimating = true;

                }
                else if (height < 1.1f && _timeToArrival <= _animationHandler.DiveTimeLow)
                {
                    RPC_DiveRightLow();
                    _ballIncoming = false;
                    _isAnimating = true;
                }
            }
        }

        protected float GetBallSide(Vector2 targ)
        {
            Vector2 firstVector = new Vector2(transform.position.x, transform.position.z);
            Vector2 secondVector = targ;

            float side = Vector2.SignedAngle(firstVector, secondVector);
         
            return side;
        }

        protected float GetBallDistance()
        {
            return Vector3.Distance(_serverBallObject.transform.position, transform.position);
        }

        protected Vector3 GetVectorToBall()
        {
            return _serverBallObject.transform.position - transform.position;
        }


        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_DiveLeftLow()
        {
            _animationHandler.DiveLeftLow();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_DiveRightLow()
        {
            _animationHandler.DiveRightLow();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_DiveLeft()
        {
            _animationHandler.DiveLeft();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_DiveRight()
        {
            _animationHandler.DiveRight();
        }



        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_CatchMidriff()
        {
            _animationHandler.CatchMidriff();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_GoalScored()
        {
            //Debug.Log("[Keeper] I conceded a goal!");

            _animationHandler.GoalScored();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_CatchBall()
        {
            _holdBall = true;

            _serverBall.transform.parent = _rightHand;
            _serverBall.transform.localRotation = Quaternion.identity;
            _serverBall.transform.localPosition = new Vector3(0.0f, 0.18f, 0.18f);
        }
    }
}

