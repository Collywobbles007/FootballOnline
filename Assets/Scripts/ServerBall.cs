namespace Fusion.Collywobbles.Futsal
{
    using Fusion;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.InputSystem.XR;

    public class ServerBall : NetworkBehaviour
    {
        // Public read-only vars
        public Vector3 BallTargetVector => _ballTargetVector;
        public IBall.TARGET_TYPE BallTargetType => _ballTarget;
        public float TimeToTarget => _timeToTarget;
        public IBall.STATE BallState => _ballState;
        public ThirdPersonPlayer BallOwner => _ballOwner;
        public bool OutOfPlay => _outOfPlay;

        // Private vars
        private ThirdPersonPlayer _ballOwner;

        private Rigidbody _ballRB;
        private Collider _ballCollider;
        private Renderer _ballRenderer;   

        private bool _isKnockedForward = false;
        private bool _reachedTarget = false;
        private bool _outOfPlay = false;
        private bool _returnBall = false;

        private float _lastShotDistance;

        private Vector3 _targetDribblePos = new Vector3();

        private Vector3 _ballTargetVector;
        private IBall.TARGET_TYPE _ballTarget;
        private float _timeToTarget;

        private NetworkId _ownerId;

        private IBall.STATE _ballState = IBall.STATE.Open;

        private KeeperBlueTeam _blueGoalkeeper;
        private KeeperRedTeam _redGoalkeeper;


        public override void Spawned()
        {
            base.Spawned();

            _ballRB = GetComponent<Rigidbody>();
            _ownerId = new NetworkId();
            _ballRenderer = transform.GetChild(0).GetChild(0).GetComponent<Renderer>();

            InitialiseBall();
        }



        /************************************************************
         * Intialise the server ball. Used to hide lag
         * when local player is in possession
         ************************************************************/
        public void InitialiseBall()
        {
            _ballRB = GetComponent<Rigidbody>();
            _ballCollider = GetComponent<Collider>();

            // Position ball on centre circle
            transform.localPosition = new Vector3(0.0f, 2.0f, 0.0f);
            transform.localRotation = Quaternion.identity;

            _ownerId = new NetworkId();
            _ballState = IBall.STATE.Open;

            //Physics.IgnoreLayerCollision(8, 3, true);
        }

        /*************************************
         * Interface method: KickBall
         * Params: Vector3 - Kicking direction
         * 
         * Player kicks the ball
         *************************************/
        public void KickBall(Vector3 direction)
        {
            Debug.Log("Ball launch velocity = " + direction);

            transform.parent = null;

            SetBallCollisionTarget(direction);

            // Ball returns to being a rigid body once kicked
            _ballCollider.enabled = true;
            _ballRB.isKinematic = false;
            _ballRB.velocity = direction;

            // Clear possession flag when kicking the ball
            _ballState = IBall.STATE.Open;
            _isKnockedForward = false;

            
        }

        /***************************************************
         * Interface Method: CollectBall
         * Param: SimplePlayer - Player collecting the ball
         * Param: Transform - Direction player is facing
         * 
         * Player collects the ball and now has possession
         ***************************************************/
        public void CollectBall(ThirdPersonPlayer player, Transform dirController)
        {
            // Used to determine which direction player is actually facing
            SetBallOwner(player);
            _ballState = IBall.STATE.Possessed;



            // Ball becomes kinematic when in possession
            _ballCollider.enabled = false;
            _ballRB.isKinematic = true;
            _ballRB.velocity = Vector3.zero;

            // Set initial ball position for dribbling
            transform.parent = dirController;
            transform.localPosition = new Vector3(0.0f, IBall.GROUND, IBall.MIN_DISTANCE);
            transform.localRotation = dirController.localRotation;

            Physics.IgnoreLayerCollision(7, 3, true);
        }

        public void KeeperCatchBall(Transform parent)
        {
            _ballState = IBall.STATE.Keeper;

            // Ball becomes kinematic when in possession
            _ballCollider.enabled = false;
            _ballRB.isKinematic = true;
            _ballRB.velocity = Vector3.zero;

            transform.parent = parent;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3(0.0f, 0.16f, 0.16f);
        }


        public void SetBallState(IBall.STATE state)
        {
            _ballState = state;
        }

        public void SetVisibility(bool isVisible)
        {
            _ballRenderer.enabled = isVisible;
        }

        public void ClearTarget()
        {
            _ballTargetVector = Vector2.zero;
        }

        // Called as each goalkeeper spawns
        public void AddGoalkeeper(Transform keeper)
        {
            if (keeper.CompareTag("BlueGoalkeeper")) 
            {
                _blueGoalkeeper = keeper.GetComponent<KeeperBlueTeam>();
            }
            else if (keeper.CompareTag("RedGoalkeeper"))
            {
                _redGoalkeeper = keeper.GetComponent<KeeperRedTeam>();
            }
        }

        private void SetBallOwner(ThirdPersonPlayer player)
        {
            NetworkId playerId = player.GetComponent<NetworkObject>().Id;

            // Check if possession has changed
            if (_ownerId != playerId)
            {
                Debug.Log($"Ball has new owner {playerId}.");

                // Tell current owner to hide local ball
                if (_ballOwner != null) // Need this as no one has possession at start
                {
                    Debug.Log($"Telling previous owner {_ownerId} to hide local ball");
                    //_ballOwner.RPC_HideLocalBall();
                }
                else
                {
                    Debug.LogError("[ServerBall] ERROR: Can't find previous owner player object!");
                }

                // Set new owner of ball
                _ballOwner = player;
                _ownerId = playerId;
            }
            else
            {
                Debug.Log($"New ball owner {playerId} is the same as previous owner {_ownerId}.");
            }
        }


        private void FixedUpdate()
        {
            if (_outOfPlay)
            {
                Physics.IgnoreLayerCollision(7, 3, true);

                if (_returnBall)
                {
                    Debug.Log("Ball went out of play. Resetting position to centre circle");

                    ResetBall();

                    _outOfPlay = false;
                    Physics.IgnoreLayerCollision(7, 3, false);
                }
            }            
            else if (_ballState == IBall.STATE.Possessed)
            {
                // Remove possession if player carries ball out of play
                if (_ballOwner.IsMoving)
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
            else if (_ballOwner != null)
            {
                _ballOwner._hasBall = false;
            }
        }

        public void ResetBall()
        {
            // Let all players know ball is back in play
            _ballOwner._outOfPlay = false;

            transform.parent = null;

            _ballCollider.enabled = true;
            _ballRB.isKinematic = false;
            _ballRB.velocity = Vector3.zero;
            _ballState = IBall.STATE.Open;

            // Position ball on centre circle
            transform.position = new Vector3(0.0f, 1.0f, 0.0f);

            _returnBall = false;
        }

        public void SetOutOfPlay(Vector3 dir, bool isGoal)
        {
            if (isGoal)
            {
                _ballState = IBall.STATE.Goal;
                SoundManager.PlaySound(SoundManager.Effect.LongWhistle, 1.0f);
            }
            else
            {
                SoundManager.PlaySound(SoundManager.Effect.ShortWhistle, 1.0f);
            }

            StartCoroutine(ReturnBallDelay());

            transform.parent = null;

            _ballCollider.enabled = true;
            _ballRB.isKinematic = false;
            _ballRB.velocity = dir;
            
        }

        public IEnumerator ReturnBallDelay()
        {
            _outOfPlay = true;

            yield return new WaitForSeconds(3.0f);

            _returnBall = true;
            _ballState = IBall.STATE.Open;
        }

        /*************************************************************
         * Kicks the ball towards the player target with a given angle
         *************************************************************/
        public void SpacePass(Vector3 target, float launchAngle)
        {
            // Ball returns to being a rigid body once kicked
            _ballCollider.enabled = true;
            _ballRB.isKinematic = false;
            //_ballRB.velocity = direction;

            // Clear possession flag when kicking the ball
            _ballState = IBall.STATE.Open;
            _isKnockedForward = false;

            transform.parent = null;

            if (launchAngle < 10.0f)
                launchAngle = 2.0f;

            Vector3 ballTarget = target;

            Vector3 projectileXZPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
            //Vector3 targetXZPos = new Vector3(ballTarget.x, 0.0f, ballTarget.z);
            Vector3 targetXZPos = new Vector3(ballTarget.x, ballTarget.y, ballTarget.z);

            // rotate the object to face the target
            transform.LookAt(targetXZPos);

            // shorthands for the formula
            float R = Vector3.Distance(projectileXZPos, targetXZPos);
            float G = Physics.gravity.y;
            float tanAlpha = Mathf.Tan(launchAngle * Mathf.Deg2Rad);
            float H = ballTarget.y - transform.position.y;

            // calculate the local space components of the velocity 
            // required to land the projectile on the target object 
            float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));
            //Vz *= power;
            //Vz = Mathf.Clamp(Vz, -14, 14);
            float Vy = tanAlpha * Vz;

            // Adjustments for ground pass
            if (launchAngle < 5.0f)
            {
                Vz = R * 1.3f;
                Vz = Mathf.Clamp(Vz, 8, 16);

                Debug.Log("R = " + R + ", Vz = " + Vz);
            }

            // create the velocity vector in local space and get it in global space
            Vector3 localVelocity = new Vector3(0f, Vy, Vz);
            Vector3 globalVelocity = transform.TransformDirection(localVelocity);

            // launch the object by setting its initial velocity and flipping its state
            _ballRB.velocity = globalVelocity;

            //Debug.Log("localVelocity = " + localVelocity);
            //Debug.Log("globalVelocity = " + globalVelocity);
            //Debug.Log("Space pass launch angle = " + launchAngle);
            
        }

        public void TakeShot(Vector3 target, float shotAngle)
        {
            // Ball returns to being a rigid body once kicked
            _ballCollider.enabled = true;
            _ballRB.isKinematic = false;
            //_ballRB.velocity = direction;

            // Clear possession flag when kicking the ball
            _ballState = IBall.STATE.Open;
            _isKnockedForward = false;

            transform.parent = null;

            Vector3 projectileXZPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 targetXZPos = new Vector3(target.x, 0.0f, target.z);

            // rotate the object to face the target
            transform.LookAt(targetXZPos);

            // shorthands for the formula
            float R = Vector3.Distance(projectileXZPos, targetXZPos);
            float G = Physics.gravity.y;
            float tanAlpha = Mathf.Tan(shotAngle * Mathf.Deg2Rad);
            float H = (target.y + 0.7f) - transform.position.y;

            // calculate the local space components of the velocity 
            // required to land the projectile on the target object 
            float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));
            float Vy = tanAlpha * Vz;

            // create the velocity vector in local space and get it in global space
            Vector3 localVelocity = new Vector3(0f, Vy, Vz);
            Vector3 globalVelocity = transform.TransformDirection(localVelocity);

            // launch the object by setting its initial velocity and flipping its state
            _ballRB.velocity = globalVelocity;

            float cosAlpha = Mathf.Cos(shotAngle * Mathf.Deg2Rad);
            float mytime = R / (globalVelocity.magnitude * cosAlpha);

            // Blue keeper is at x = -21.0f, red keeper is at x = 21.0f
            if (target.x < 0)
            {
                //Debug.Log("[ServerBall] Telling blue keeper to prepare to save...");
                //Debug.Log("[ServerBall] Intial velocity = " + globalVelocity.magnitude);
                //Debug.Log("[ServerBall] Distance = " + R);
                //Debug.Log("[ServerBall] Estimated time = " + mytime);

                _blueGoalkeeper.PrepareToSave(target, mytime, R);               
            }

            _lastShotDistance = R;
        }

        Transform RandomiseShotTarget(Transform TargetObjectTF)
        {
            float TargetRadius = 1.0f;

            Transform targetTF = TargetObjectTF.GetComponent<Transform>(); // shorthand

            // To acquire our new target from a point around the projectile object:
            // - we start with a vector in the XZ-Plane (ground), let's pick right (1, 0, 0).
            //    (or pick left, forward, back, or any perpendicular vector to the rotation axis, which is up)
            // - We'll use a quaternion to rotate our vector. To create a rotation quaternion, we'll be using
            //    the AngleAxis() function, which takes a rotation angle and a rotation amount in degrees as parameters.
            Vector3 rotationAxis = Vector3.up;  // as our object is on the XZ-Plane, we'll use up vector as the rotation axis.
            float randomAngle = Random.Range(0.0f, 360.0f);
            Vector3 randomVectorOnGroundPlane = Quaternion.AngleAxis(randomAngle, rotationAxis) * Vector3.right;

            // Add a random offset to the height of the target location:
            // - If the RandomizeHeightOffset flag is turned on, pick a random number between 0.2f and 1.0f to make sure
            //    we're somewhat above or below the ground. If the flag is off, just pick 1.0f. Finally, scale this number
            //    with the TargetHeightOffsetFromGround.
            // - We want to randomly determine if the target is above or below ground. 
            //    Randomly assign the multiplier -1.0f or 1.0f
            // - Create an offset vector from the random height and add the offset vector to the random point on the plane
            //float heightOffset = (RandomizeHeightOffset ? Random.Range(0.2f, 1.0f) : 1.0f) * TargetHeightOffsetFromGround;
            float aboveOrBelowGround = (Random.Range(0.0f, 1.0f) > 0.5f ? 1.0f : -1.0f);
            //Vector3 heightOffsetVector = new Vector3(0, heightOffset, 0) * aboveOrBelowGround;
            Vector3 randomPoint = randomVectorOnGroundPlane * TargetRadius;// + heightOffsetVector;

            //  - finally, we'll set the target object's position and update our state. 
            TargetObjectTF.SetPositionAndRotation(randomPoint, targetTF.rotation);

            return TargetObjectTF;

            //bTargetReady = true;
        }

        private void SetBallCollisionTarget(Vector3 direction)
        {
            int linePoints = 25;
            float timeBetweenPoints = 0.1f;
            LayerMask collisionMask = new LayerMask();

            // Set layer mask for collision detection
            int layer = gameObject.layer;

            for (int z = 0; z < 32; z++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, z))
                {
                    collisionMask |= 1 << z;
                }
            }

            Vector3 startPosition = transform.position; // Server ball start position
            Vector3 lastPosition = transform.position;
            Vector3 startVelocity = direction / 1.04f; // Server ball start velocity / mass of server ball

            int i = 0;

            for (float time = 0; time < linePoints; time += timeBetweenPoints)
            {
                i++;

                Vector3 point = startPosition + time * startVelocity;

                // d = vt + 1/2 at^2
                point.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2.0f * time * time);

                // Stop the calculation once it hits something
                if (Physics.Raycast(lastPosition, (point - lastPosition).normalized, out RaycastHit hit, (point - lastPosition).magnitude, collisionMask))
                {
                    if (hit.transform.CompareTag("BlueGoal"))
                    {
                        Debug.Log("[ServerBall] Ball will enter the blue goal at " + hit.point);
                        _ballTargetVector = hit.point;
                        _ballTarget = IBall.TARGET_TYPE.BlueGoal;
                        //RPC_SetBallTarget(_ballTargetVector, _ballTarget);
                    }
                    else if (hit.transform.CompareTag("RedGoal"))
                    {
                        Debug.Log("[ServerBall] Ball will enter the red goal!");
                        _ballTargetVector = hit.point;
                        _ballTarget = IBall.TARGET_TYPE.RedGoal;
                        //RPC_SetBallTarget(_ballTargetVector, _ballTarget);
                    }
                    else if (hit.transform.CompareTag("BlueGoalkeeper"))
                    {
                        Debug.Log("[ServerBall] Blue keeper will catch the ball midriff!");
                        _ballTargetVector = hit.point;
                        _ballTarget = IBall.TARGET_TYPE.BlueKeeper;
                        //RPC_SetBallTarget(_ballTargetVector, _ballTarget);
                    }
                    else if (hit.transform.CompareTag("RedGoalkeeper"))
                    {
                        Debug.Log("[ServerBall] Red keeper will catch the ball midriff!");
                        _ballTargetVector = hit.point;
                        _ballTarget = IBall.TARGET_TYPE.RedKeeper;
                        //RPC_SetBallTarget(_ballTargetVector, _ballTarget);
                    }
                    else
                    {
                        Debug.Log("[ServerBall] Hit unknown object " + hit.transform.tag);
                    }

                    _timeToTarget = time;

                    return;
                }

                lastPosition = point;
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_SetBallTarget(Vector3 position, IBall.TARGET_TYPE target)
        {
            _ballTargetVector = position;
            _ballTarget = target;
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_ClearBallTarget()
        {
            _ballTarget = IBall.TARGET_TYPE.None;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority)
                return;

            if (_outOfPlay)
            {
                Debug.Log("Ball already out of play...returning");
                return;
            }

            //Debug.Log("OnTriggerEnter: collider was name = " + other.name + ", tag = " + other.tag);


            if (other.CompareTag("BallBoundary"))
            {
                Debug.Log("[ServerBall] Ball out of play");

                _outOfPlay = true;

                // Set network vars
                _ballOwner._hasBall = false;
                _ballOwner._outOfPlay = true;

                StartCoroutine(ReturnBallDelay());
                SoundManager.PlaySound(SoundManager.Effect.ShortWhistle, 1.0f);
            }
            else if (other.CompareTag("BlueGoalkeeper"))
            {
                Debug.Log($"[ServerBall] I hit the blue goalkeeper with speed {_ballRB.velocity.x} from distance {_lastShotDistance}");

                
                if (_lastShotDistance > 16.0f)
                {
                    //_ballState = IBall.STATE.Keeper;

                    // Ball becomes kinematic when in possession
                    //_ballCollider.enabled = false;
                    //_ballRB.isKinematic = true;
                    //_ballRB.velocity = Vector3.zero;

                    // Set initial ball position for dribbling
                    //transform.parent = other.transform;

                    //Debug.Log("[ServerBall] other.gameObject.name = " + other.gameObject.name);

                    //other.transform.root.GetComponent<KeeperController>().RPC_CatchBall();
                }
                else
                {
                    float newZ = _ballRB.velocity.z < 0 ? Random.Range(-4, -8) : Random.Range(4, 8);

                    _ballRB.velocity = new Vector3(Random.Range(3, 6), _ballRB.velocity.y, newZ);
                }
                
            }

        }

        public void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("RedGoal") && transform.position.x > 21.2f && _ballState != IBall.STATE.Goal)
            {
                Debug.Log("Ball in red goal. Goal scored by blue team");

                _ballState = IBall.STATE.Goal;

                StartCoroutine(ReturnBallDelay());
                SoundManager.PlaySound(SoundManager.Effect.LongWhistle, 1.0f);
            }
            else if (other.CompareTag("BlueGoal") && transform.position.x < -21.2f && _ballState != IBall.STATE.Goal)
            {
                Debug.Log("Ball in blue goal. Goal scored by red team");

                _ballState = IBall.STATE.Goal;

                StartCoroutine(ReturnBallDelay());
                SoundManager.PlaySound(SoundManager.Effect.LongWhistle, 1.0f);
            }
        }


        public void OnCollisionEnter(Collision collision)
        {
            if (_ballOwner == null)
                return;

            //Debug.Log("[CollisionEnter] Ball hit " + collision.gameObject);

            if (collision.gameObject.CompareTag("Net"))
            {
                _ballOwner.RPC_PlaySound(SoundManager.Effect.Net, 0.5f);
            }
            else if (collision.gameObject.CompareTag("Pitch") && _ballRB.velocity.y > 0)
            {
                _ballOwner.RPC_PlaySound(SoundManager.Effect.Bounce, 0.15f);
            }
            else if (collision.gameObject.CompareTag("BlueGoalkeeper"))
            {
                Debug.Log("Keeper made a save!");
            }
        }
    }
}
