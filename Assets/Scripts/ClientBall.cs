namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;


    public class ClientBall : MonoBehaviour
    {
        private bool _isKnockedForward = false;
        private bool _reachedTarget = false;

        private Vector3 _targetDribblePos = new Vector3();

        public IBall.STATE _ballState { get; set; }

        public ThirdPersonPlayer BallOwner { get; set; }


        /************************************************************
         * Intialise the client version of the ball. Used to hide lag
         * when local player is in possession
         ************************************************************/
        public void InitialiseBall(ThirdPersonPlayer player, Transform dirController)
        {
            Debug.Log("Initialising client ball");

            // Postion ball somewhere off screen until activated
            transform.localPosition = new Vector3(0.0f, -10.0f, 0.0f);
            transform.localRotation = Quaternion.identity;

            transform.parent = dirController;
            transform.localPosition = new Vector3(0.0f, IBall.GROUND, IBall.MIN_DISTANCE);
            transform.localRotation = dirController.localRotation;

            _ballState = IBall.STATE.Open;
            BallOwner = player;
        }

        
        public void SetBallVisibility(bool isVisible)
        {
            if (isVisible)
            {
                gameObject.SetActive(true);
                _ballState = IBall.STATE.Possessed;
            }
            else
            {
                gameObject.SetActive(false);
                _ballState = IBall.STATE.Open;
            }
        }



        private void FixedUpdate()
        {
            // Allow player to dribble the ball when in possession
            if (_ballState == IBall.STATE.Possessed)
            {
                // Check for player carrying ball out of play
                if (transform.position.z < -11.22f || transform.position.z > 11.22f)
                {
                    Debug.Log("[ClientBall] Player crossed the sideline. It's a throw-in!");

                    BallOwner.BallOutOfPlay();
                }
                else if (transform.position.x < -21.2f || transform.position.x > 21.2f)
                {
                    if (transform.position.z < -1.6f || transform.position.z > 1.6f)
                    {
                        Debug.Log("[ClientBall] Player crossed the biline. It's a goal kick!");
                        BallOwner.BallOutOfPlay();
                    }
                    else
                    {
                        Debug.Log("Player crossed the goal line. Goal scored!!");
                        BallOwner.BallInGoal();
                    }
                }
                else if (BallOwner.IsMoving)
                {
                    if (!_isKnockedForward)
                    {
                        _targetDribblePos = new Vector3(0.0f, IBall.GROUND, IBall.MAX_DISTANCE);
                        _isKnockedForward = true;
                    }
                    else if (_reachedTarget)
                    {
                        _targetDribblePos = new Vector3(0.0f, IBall.GROUND, IBall.MIN_DISTANCE);
                    }

                    if (_isKnockedForward && !_reachedTarget)
                    {
                        float dist = Vector3.Distance(transform.localPosition, _targetDribblePos);

                        if (dist > 0.01f)
                        {
                            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetDribblePos, IBall.LERP_SPEED);
                        }
                        else
                        {
                            _reachedTarget = true;
                            transform.localPosition = _targetDribblePos;
                        }
                    }
                    else if (_isKnockedForward && _reachedTarget)
                    {
                        float dist = Vector3.Distance(transform.localPosition, _targetDribblePos);

                        if (dist > 0.01f)
                        {
                            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetDribblePos, IBall.LERP_SPEED2);
                        }
                        else
                        {
                            _isKnockedForward = false;
                            _reachedTarget = false;
                            transform.localPosition = _targetDribblePos;
                        }
                    }

                    transform.Rotate(15.0f, 0.0f, 0.0f, Space.Self);

                }
                else
                {
                    transform.localPosition = new Vector3(0.04f, IBall.GROUND, IBall.MIN_DISTANCE);

                }
            }
        }
    }
}
