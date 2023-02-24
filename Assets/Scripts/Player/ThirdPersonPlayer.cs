namespace Fusion.Collywobbles.Futsal
{
    using System;
    using UnityEngine;
    using Fusion;
    using Fusion.KCC;
    using System.Collections;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using TMPro;

    /// <summary>
    /// Advanced player implementation with third person view.
    /// </summary>
    [OrderBefore(typeof(KCC))]
    [OrderAfter(typeof(AdvancedPlayer))]
    public sealed class ThirdPersonPlayer : AdvancedPlayer
    {
        public float SHOT_LAUNCH_ANGLE = 30.0f;
        public float BALL_MASS = 1.04f;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [Header("Display Controls")]
        [SerializeField]
        [Range(10, 100)]
        private int _linePoints = 25;

        [SerializeField]
        private float _timeBetweenPoints = 0.1f;


        // PRIVATE MEMBERS
        [Header("Movement Controls")]
        [SerializeField]
        [Tooltip("Visual should always face player move direction.")]
        private bool _faceMoveDirection;
        [SerializeField]
        [Tooltip("Visual always facing forward direction if the player holds right mouse button, ignoring Face Move Direction.")]
        private bool _mouseHoldRotationPriority;
        [SerializeField]
        [Tooltip("Events which trigger look rotation update of KCC.")]
        private ELookRotationUpdateSource _lookRotationUpdateSource = ELookRotationUpdateSource.Jump | ELookRotationUpdateSource.Movement | ELookRotationUpdateSource.MouseHold;

        [Networked]
        [Accuracy(0.00001f)]
        private Vector2 _pendingLookRotationDelta { get; set; }
        [Networked]
        [Accuracy(0.00001f)]
        private float _facingMoveRotation { get; set; }

        private Vector2 _renderLookRotationDelta;
        private Interpolator<float> _facingMoveRotationInterpolator;


        /* My added private vars */
        private const float BALL_GROUND = 0.125f;

        // Server controls the ball and who owns it
        // Networked so all players have read access
        [Networked(OnChanged = nameof(OnPossessionChanged))]
        public NetworkBool _hasBall { get; set; }

        [Networked]
        public NetworkBool _outOfPlay { get; set; }

        [Networked]
        public Vector3 _spacePassTarget { get; set; }

        [Networked]
        public NetworkBool _tacklingEnabled { get; set; }

        public bool _shootEnabled = false;

        private bool _takeShot = false;

        [Networked]
        public NetworkBool _isTackling { get; set; }

        //[Networked(OnChanged = nameof(OnSpacePassChanged))]
        //public NetworkBool _isSpacePass { get; set; }

        public bool IsMoving { get; set; }

        public bool _handleFreeLookRotation = false;
        private bool _isKicking = false;
        private bool _isAnimating = false;

        bool runningForward = false;
        bool runningRight = false;
        bool runningLeft = false;
        bool runningBack = false;

        private Animator footballerAnimator;

        [SerializeField] private SimplePlayerAnimator animationHandler;
        [SerializeField] private GameObject _localBallPrefab;
        [SerializeField] private Transform _directionController;

        private GameObject _localBallObject;
        private GameObject _serverBallObject;

        private ClientBall _clientBall;
        private ServerBall _serverBall;
        private Collider _ballCollider;

        private float oldForwardVal;
        private float oldHorizontalVal;
        private float currentValFwd;
        private float currentValHoriz;
        //private float currentWeight;

        private float _cameraZoom;

        //private int _idleWithBallLayer;

        private Quaternion _targetRot = Quaternion.identity;

        float _startValue, _endValue;
        float _startValue2, _endValue2;
        float _timeElapsed;
        public bool _isLerping = false;
        Quaternion _fixedCameraDir;
        float _fixedLaunchAngle, _fixedCameraYaw;

        float _kickingPower = 0.0f;

        private bool _isChatting = false;
        private bool _quitMenu = false;
        private Coroutine _chatFadeRoutine;
        private string _nickname;

        private Vector3 _ballStartPos, _ballEndPos;

        private bool _hasPreload = false;
        private bool _isSpacePass = false;
        private bool _isShooting = false;

        private ThirdPersonPlayer _currentTarget;

        private Vector3 _shotTarget = Vector3.zero;

        float _preloadAngle;
        float _preloadYaw;
        float _preloadPowerBar;
        Quaternion _preloadCameraRotation;

        public float _playerTackleAbility = 2.0f;

        // **************
        // Player UI Vars
        // **************

        // Kick direction arrows
        private GameObject _lowKickArrows;
        private GameObject _highKickArrows;

        // Power bar indicator
        private GameObject _powerBar;
        private Image _powerBarMask;
        private float _maxPowerBarValue = 10.0f;
        private float _currentPowerBarValue = 0.0f;
        private bool _powerBarIsActive = false;

        // Preload indicator
        private GameObject _preloadSymbol;

        // Tackle indicator
        private GameObject _tackleIcon;

        // Shoot indicator
        private GameObject _shootIcon;

        // General player stuff
        private GameObject _playDirection;
        private GameObject _targetPlayerParticles;

        // Chatbox
        private ChatManager _chatManager;

        // Loading screen
        private GameObject _loadingScreen;


        private List<GameObject> redTeamPlayers;
        private NetworkId myNetworkId;

        // Server use only vars
        //private bool _isTackling = false;
        float _oppDistance;
        ThirdPersonPlayer _playerInPossession;

        public IBall.STATE _serverBallState;

        // Debug Vars
        private LayerMask _collisionMask; // Used for debug line renderer


        // PUBLIC METHODS

        #region Public Methods

        public Vector3 GetVelocity()
        {
            return KCC.Data.RealVelocity;
        }

        public void BallOutOfPlay()
        {
            Debug.Log("Client ball has been carried out of play!");

            // Hide client ball
            if (Object.HasInputAuthority)
            {
                _clientBall.SetBallVisibility(false);
                _serverBall.SetVisibility(true);

                // Let server know this player has carried ball out of play
                footballerAnimator.SetBool("HasBall", false);

                RPC_BallOutOfPlay(false);
            }
        }

        public void BallInGoal()
        {
            Debug.Log("Client ball has crossed the goal line!");

            // Hide client ball
            if (Object.HasInputAuthority)
            {
                _localBallObject.SetActive(false);
                _serverBall.SetVisibility(true);

                // Let server know this player has carried ball out of play
                footballerAnimator.SetBool("HasBall", false);

                RPC_BallOutOfPlay(true);
            }
        }

        #endregion


        // PARENT CLASS PROTECTED METHODS

        #region Protected Methods


        protected override void OnSpawned()
        {
            Debug.Log("Player id = " + GetComponent<NetworkObject>().Id);

            IEnumerable playerRef = Runner.ActivePlayers;

            foreach (PlayerRef myRef in playerRef)
            {
                Debug.Log("myRef = " + myRef);
            }

            /** Code to change shirt colour
            // Set shirt colour
            Material shirtMaterial = Resources.Load<Material>("PlayerShirts/BlueShirt");
            Transform child1 = transform.GetChild(0);
            Transform grandChild1 = child1.GetChild(3);

            SkinnedMeshRenderer meshRenderer = grandChild1.GetComponent<SkinnedMeshRenderer>();

            Material oldMaterial = meshRenderer.material;

            // Set the new material on the GameObject
            meshRenderer.material = shirtMaterial;
            */

            _facingMoveRotationInterpolator = GetInterpolator<float>(nameof(_facingMoveRotation));

            // Get reference to chatbox manager
            var chatBox = GameObject.FindGameObjectWithTag("Chatbox");
            _chatManager = chatBox.GetComponent<ChatManager>();

            // Spawn and intitialise a client local ball (used to hide lag)
            if (Object.HasInputAuthority)
            {
                _lineRenderer = GameObject.FindGameObjectWithTag("DebugLine").GetComponent<LineRenderer>();

                if (_lineRenderer == null)
                    Debug.LogError("Could not locate line renderer");
                else
                    Debug.Log("Found line renderer");

                // TURN ON FOR DEBUGGING!!
                _lineRenderer.gameObject.SetActive(false);

                // Initialise UI stuff
                _lowKickArrows = GameObject.FindGameObjectWithTag("LowArrows");
                _highKickArrows = GameObject.FindGameObjectWithTag("HighArrows");

                _preloadSymbol = GameObject.FindGameObjectWithTag("Preload");

                _tackleIcon = GameObject.FindGameObjectWithTag("Tackle");
                _shootIcon = GameObject.FindGameObjectWithTag("Shoot");


                _powerBar = GameObject.FindGameObjectWithTag("PowerBar");
                _powerBarMask = _powerBar.transform.GetChild(0).GetComponent<Image>();

                _loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");


                // Set initial visibility of all icons
                _lowKickArrows.SetActive(true);
                _highKickArrows.SetActive(false);
                _preloadSymbol.SetActive(false);
                _powerBar.SetActive(false);
                _tackleIcon.SetActive(false);
                _shootIcon.SetActive(false);

                _loadingScreen.SetActive(true);

                _nickname = GetComponent<PlayerEntity>().Nickname.Value;

                // Initialise player icons/effects
                _playDirection = GameObject.FindGameObjectWithTag("PlayDirection");
                _targetPlayerParticles = GameObject.FindGameObjectWithTag("PlayerParticles");

                _playDirection.SetActive(true);
                _targetPlayerParticles.SetActive(false);

                // Get reference to the server ball
                _serverBallObject = GameObject.FindGameObjectWithTag("Ball");
                _serverBall = _serverBallObject.GetComponent<ServerBall>();

                // Instantiate a local ball game object 
                _localBallObject = Instantiate(_localBallPrefab);

                // Get reference to client ball script
                _clientBall = _localBallObject.GetComponent<ClientBall>();
                _clientBall.InitialiseBall(this, _directionController);

                // Local ball object is not active at start
                _localBallObject.SetActive(false);

                // Disable server ball collision detection for clients
                // Client to client ball collisions are disabled at physics level
                // Ball collisions are entirely server authoritative
                _serverBallObject.GetComponent<Collider>().enabled = false;
            }


            // Get reference to the animator
            footballerAnimator = animationHandler.InitAnimator();

            if (footballerAnimator == null)
            {
                Debug.LogError("ERROR: Footballer animator not found!");
            }

            oldForwardVal = 0.0f;
            oldHorizontalVal = 0.0f;
            currentValFwd = 0.0f;
            currentValHoriz = 0.0f;

            KCC.OnCollisionEnter += MyCollision;

            

            // Start without the ball
            if (Object.HasStateAuthority)
            {
                _serverBallObject = GameObject.FindGameObjectWithTag("Ball");
                _serverBall = _serverBallObject.GetComponent<ServerBall>();
                _hasBall = false;
            }

            IsMoving = false;
            _cameraZoom = CameraHandle.localPosition.z;

            // Set player as member or red or blue team
            gameObject.tag = "RedTeam";
            myNetworkId = transform.GetComponent<NetworkObject>().Id;

            RPC_UpdateTeamList();           

            if (Object.HasInputAuthority)
            {
                StartCoroutine(DoLoadingScreen());

                // Used by line renderer for debug only!!
                int layer = _serverBallObject.layer; // Set this to serverball layer

                for (int z = 0; z < 32; z++)
                {
                    if (!Physics.GetIgnoreLayerCollision(layer, z))
                    {
                        _collisionMask |= 1 << z;
                    }
                }
                /////////////////////////
            }
        }


        // 1.
        protected override void ProcessEarlyFixedInput()
        {
            // Here we process input and set properties related to movement / look.
            // For following lines, we should use Input.FixedInput only. This property holds input for fixed updates.

            // Clamp input look rotation delta. Instead of applying immediately to KCC, we store it locally as pending and defer application to a point where conditions for application are met.
            // This allows us to rotate with camera around character standing still.
            Vector2 lookRotation = KCC.FixedData.GetLookRotation(true, true);
            _pendingLookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, _pendingLookRotationDelta + Input.FixedInput.LookRotationDelta, MinCameraAngle, MaxCameraAngle);


            bool updateLookRotation = default;
            //Quaternion facingRotation = default; // default is invalid (not set)
            //Quaternion jumpRotation = default; // default is invalid (not set)

            // Checking look rotation update conditions
            if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold) == true) { updateLookRotation |= Input.FixedInput.RMB == false; }
            if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseMovement) == true) { updateLookRotation |= Input.FixedInput.LookRotationDelta.IsZero() == false; }


            if (updateLookRotation == true)
            {
                // Some conditions are met, we can apply pending look rotation delta to KCC
                if (_pendingLookRotationDelta.IsZero() == false)
                {
                    KCC.AddLookRotation(_pendingLookRotationDelta);
                    _pendingLookRotationDelta = default;

                    // Clear camera pivot rotation now pending look rotation has been processed
                    Vector2 pitchRotation = KCC.FixedData.GetLookRotation(true, false);
                    Vector2 clampedCameraRotation = KCCUtility.GetClampedLookRotation(pitchRotation, MinCameraAngle, MaxCameraAngle);
                    CameraPivot.rotation = KCC.FixedData.TransformRotation * Quaternion.Euler(clampedCameraRotation);
                }
            }

            if (updateLookRotation == true || _faceMoveDirection == false)
            {
                // Setting base facing and jump rotation
                //facingRotation = KCC.FixedData.TransformRotation;
                //jumpRotation = KCC.FixedData.TransformRotation;
            }

            Vector3 inputDirection = default;
            //bool faceOnMouseHold = _faceMoveDirection;

            Vector3 moveDirection = Input.FixedInput.MoveDirection.X0Y();

            if (_isAnimating)
            {
                moveDirection = Vector3.zero;
            }
            else if (Input.FixedInput.TAB && !_hasBall && !_hasPreload)
            {
                // Use TAB to run towards the ball
                Vector3 ballDirection = (_serverBall.transform.position - transform.position).normalized;

                if (ballDirection.magnitude > 0.1f)
                {
                    inputDirection = (_serverBall.transform.position - transform.position).normalized;
                }
            }
            else if (moveDirection.IsZero() == false)
            {
                // Calculating world space input direction for KCC, update facing and jump rotation based on configuration.
                inputDirection = KCC.FixedData.TransformRotation * moveDirection;
            }

            /** Dash not used for now. May be a useful reference later
             * 
            if (KCC.HasModifier(DashProcessor) == true)
            {
                // Dash processor detected, we want the visual to face the dash direction.
                // Also force disable facing in look direction on mouse hold.

                hasInputDirection = true;
                inputDirection = DashProcessor.Direction.OnlyXZ().normalized;
                facingRotation = Quaternion.LookRotation(inputDirection);
                faceOnMouseHold = false;
            }
            */

            //if (hasInputDirection)
            if (!moveDirection.IsZero())
            {
                _targetRot = Quaternion.LookRotation(moveDirection);
            }
            else
            {
                _targetRot = Quaternion.identity;

                // Looks better if ball instantly moves to position when stopped
                _directionController.localRotation = _targetRot;
            }

            _directionController.localRotation = Quaternion.Lerp(_directionController.localRotation, _targetRot, Time.fixedDeltaTime * 5.0f);

            if (Quaternion.Angle(_directionController.localRotation, _targetRot) < 2.0f)
            {
                _directionController.localRotation = _targetRot;
            }

            /** Not used
            if (hasInputDirection == true)
            {
                Quaternion inputRotation = Quaternion.LookRotation(inputDirection);

                // We are moving in certain direction, we'll use it also for jump.
                jumpRotation = inputRotation;

                // Facing move direction enabled and right mouse button rotation lock disabled? Treat input rotation as facing as well.
                if (faceOnMouseHold == true && (_mouseHoldRotationPriority == false || Input.FixedInput.RMB == false))
                {
                    facingRotation = inputRotation;
                }
                
            }
            */

            KCC.SetInputDirection(inputDirection);

            /** Jump not used for now. May be a useful reference later
             * 
            if (Input.WasActivated(EGameplayInputAction.Jump) == true)
            {
                // Is jump rotation invalid (not set)? Get it from other source.
                if (jumpRotation.IsZero() == true)
                {
                    // Is facing rotation valid? Use it.
                    if (facingRotation.IsZero() == false)
                    {
                        jumpRotation = facingRotation;
                    }
                    else
                    {
                        // Otherwise just jump forward.
                        jumpRotation = KCC.FixedData.TransformRotation;
                    }
                }

                // Is facing rotation invalid (not set)? Set it to the same rotation as jump.
                if (facingRotation.IsZero() == true)
                {
                    facingRotation = jumpRotation;
                }

                KCC.Jump(jumpRotation * JumpImpulse);
            }
            */

            /** Movement related actions
            // Notice we are checking KCC.FixedData because we are in fixed update code path (render update uses KCC.RenderData)
            if (KCC.FixedData.IsGrounded == true)
            {
                // Sprint is updated only when grounded
                KCC.SetSprint(Input.FixedInput.Sprint);
            }

            if (Input.WasActivated(EGameplayInputAction.Dash) == true)
            {
                // Dash is movement related action, should be processed before KCC ticks.
                // We only care about registering processor to the KCC, responsibility for cleanup is on dash processor.
                KCC.AddModifier(DashProcessor);
            }

            // Another movement related actions here (crouch, ...)

            */

            if (Input.WasActivated(EGameplayInputAction.TAB) == true)
            {
                if (_hasPreload)
                {
                    // Clear preload 
                    _hasPreload = false;
                    _takeShot = false;
                    _preloadSymbol.SetActive(false);

                    // Reset the powerbar vars
                    _powerBarMask.fillAmount = 0.0f;
                    _currentPowerBarValue = 0.0f;
                }     
            }


            /** Not used
            // Is facing rotation set? Apply to the visual and store it.
            if (facingRotation.IsZero() == false)
            {
                //Visual.Root.rotation = facingRotation;
                Visual.transform.rotation = facingRotation;

                if (_faceMoveDirection == true)
                {
                    KCCUtility.GetLookRotationAngles(facingRotation, out float facingPitch, out float facingYaw);
                    _facingMoveRotation = facingYaw;
                }
            }
            */

        }

        protected override void OnFixedUpdate()
        {
            // Regular fixed update for Player/AdvancedPlayer class.
            // Executed after all player KCC updates and before HitboxManager.

            // Setting camera pivot location
            // In this case we have to apply pending look rotation delta (cached locally) on top of current KCC look rotation.

            Vector2 pitchRotation = KCC.FixedData.GetLookRotation(true, false);
            Vector2 clampedCameraRotation = KCCUtility.GetClampedLookRotation(pitchRotation + _pendingLookRotationDelta, MinCameraAngle, MaxCameraAngle);
           
            if (_handleFreeLookRotation && _pendingLookRotationDelta.y == 0.0f)
            {
                _handleFreeLookRotation = false;
            }
            else
                CameraPivot.rotation = KCC.FixedData.TransformRotation * Quaternion.Euler(clampedCameraRotation);

            if (_faceMoveDirection == true && Object.IsProxy == true)
            {
                // Facing rotation for visual is already set on input and state authority, here we update proxies based on [Networked] property.
                Visual.transform.rotation = Quaternion.Euler(0.0f, _facingMoveRotation, 0.0f);
            }

            // Handle tackling
            if (Object.HasStateAuthority)
            {
                if (!_hasBall)
                {
                    // Check if an opp is in tackle range if we don't have the ball
                    _playerInPossession = _serverBall.BallOwner;
                    _oppDistance = 100.0f;

                    // Get distance to player with ball if they are dribbling
                    if (_playerInPossession != null && _playerInPossession._hasBall)
                    {
                        _oppDistance = Vector3.Distance(transform.position, _playerInPossession.transform.position);
                    }

                    // Set maximum distance for tackling icon to appear
                    // Not actual tackle distance!!
                    if (_oppDistance < 6.0f)
                        _tacklingEnabled = true;
                    else
                        _tacklingEnabled = false;
                }
            }
        }


        // 3.
        protected override void ProcessLateFixedInput()
        {
            float forwardVal, horizontalVal;
            float lerpSpeed;
            //Vector2 moveDir = Input.FixedInput.MoveDirection;

            Vector3 inputDirection = KCC.transform.InverseTransformDirection(KCC.Data.InputDirection);
            Vector2 moveDir = new Vector2(inputDirection.x, inputDirection.z);

            moveDir.Normalize();

            Vector3 lookVector = new Vector3(moveDir.x, 0.0f, moveDir.y);

            if (_isKicking)
                return;

            // Executed after HitboxManager. Process other non-movement actions like shooting.

            if (Input.WasActivated(EGameplayInputAction.LMB) == true)
            {
                // Left mouse button action
            }


            if (Input.WasDeactivated(EGameplayInputAction.RMB) == true)
            {
                _handleFreeLookRotation = true;
            }

            // Quick chat messages
            if (Input.WasDeactivated(EGameplayInputAction.F1)) RPC_SendMessage(_nickname + ": Good tackle!");
            if (Input.WasDeactivated(EGameplayInputAction.F2)) RPC_SendMessage(_nickname + ": Good pass!");
            if (Input.WasDeactivated(EGameplayInputAction.F3)) RPC_SendMessage(_nickname + ": Good goal!");
            if (Input.WasDeactivated(EGameplayInputAction.F4)) RPC_SendMessage(_nickname + ": Good game!");
            if (Input.WasDeactivated(EGameplayInputAction.F5)) RPC_SendMessage(_nickname + ": Pass, I'm open!");
            if (Input.WasDeactivated(EGameplayInputAction.F6)) RPC_SendMessage(_nickname + ": Help defence!");
            if (Input.WasDeactivated(EGameplayInputAction.F7)) RPC_SendMessage(_nickname + ": Thanks!");
            if (Input.WasDeactivated(EGameplayInputAction.F8)) RPC_SendMessage(_nickname + ": Sorry!");
            if (Input.WasDeactivated(EGameplayInputAction.F9)) RPC_SendMessage(_nickname + ": Unlucky!");
            if (Input.WasDeactivated(EGameplayInputAction.F10)) RPC_SendMessage(_nickname + ": BRB!");

            // Player input
            // For following lines, we should use Input.FixedInput only. This property holds input for fixed updates.
            if (Object.IsProxy == false) // Player has state and input authority
            {
                if (Runner.IsForward)
                {
                    if (moveDir.IsZero())
                    {
                        animationHandler.StopRunning(_hasBall);
                    }
                    else
                    {
                        animationHandler.StartRunning(_hasBall);
                    }

                    forwardVal = Mathf.SmoothDamp(oldForwardVal, moveDir.y, ref currentValFwd, 0.05f);
                    horizontalVal = Mathf.SmoothDamp(oldHorizontalVal, moveDir.x, ref currentValHoriz, 0.05f);

                    forwardVal = Mathf.Clamp(forwardVal, -1, 1);
                    horizontalVal = Mathf.Clamp(horizontalVal, -1, 1);

                    footballerAnimator.SetFloat("Forward", forwardVal);
                    footballerAnimator.SetFloat("Horizontal", horizontalVal);

                    oldForwardVal = forwardVal;
                    oldHorizontalVal = horizontalVal;

                    // Establish running direction for easier animation triggers
                    runningForward = forwardVal > 0.1f && lookVector.z != 0;
                    runningRight = horizontalVal > 0.1f && lookVector.x != 0;
                    runningLeft = horizontalVal < -0.1f && lookVector.x != 0;
                    runningBack = forwardVal < -0.1f && lookVector.z != 0;

                    footballerAnimator.SetBool("RunningFwd", runningForward);
                    footballerAnimator.SetBool("RunningBack", runningBack);
                    footballerAnimator.SetBool("RunningRight", runningRight);
                    footballerAnimator.SetBool("RunningLeft", runningLeft);

                    if (_hasBall)
                    {
                        if (Input.FixedInput.MoveDirection.IsZero())
                        {
                            IsMoving = false;
                        }
                        else
                        {
                            IsMoving = true;
                        }
                    }
                }
            }
        }

        // 4.
        protected override void ProcessRenderInput()
        {
            // Here we process input and set properties related to movement / look.
            // For following lines, we should use Input.RenderInput and Input.CachedInput only. These properties hold input for render updates.
            // Input.RenderInput holds input for current render frame.
            // Input.CachedInput holds combined input for all render frames from last fixed update. This property will be used to set input for next fixed update (polled by Fusion).

            // Get look rotation from last fixed update (not last render!)
            Vector2 lookRotation = KCC.FixedData.GetLookRotation(true, true);

            // For correct look rotation, we have to apply deltas from all render frames since last fixed update => stored in Input.CachedInput
            // Additionally we have to apply pending look rotation delta maintained in fixed update, resulting in pending look rotation delta dedicated to render update.
            _renderLookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, _pendingLookRotationDelta + Input.CachedInput.LookRotationDelta, MinCameraAngle, MaxCameraAngle);

            bool updateLookRotation = default;
            Quaternion facingRotation = default;
            Quaternion jumpRotation = default;

            // Checking look rotation update conditions. These check are done agains Input.CachedInput, because any render input accumulated since last fixed update will trigger look rotation update in next fixed udpate.
            //if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Jump) == true) { updateLookRotation |= Input.CachedInput.Jump == true; }
            //if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Movement) == true) { updateLookRotation |= Input.CachedInput.MoveDirection.IsZero() == false; }
            if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold) == true) { updateLookRotation |= Input.CachedInput.RMB == false; }
            if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseMovement) == true) { updateLookRotation |= Input.CachedInput.LookRotationDelta.IsZero() == false; }

            //if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Movement) == true) { updateLookRotation |= Input.FixedInput.RMB; }

            if (updateLookRotation == true)
            {
                // Some conditions are met, we can apply pending render look rotation delta to KCC
                if (_renderLookRotationDelta.IsZero() == false)
                {
                    KCC.SetLookRotation(lookRotation + _renderLookRotationDelta);

                    // Reset camera pivot rotation now pending look rotation has been processed
                    Vector2 pitchRotation = KCC.RenderData.GetLookRotation(true, false);
                    Vector2 clampedCameraRotation = KCCUtility.GetClampedLookRotation(pitchRotation, MinCameraAngle, MaxCameraAngle);
                    CameraPivot.rotation = KCC.RenderData.TransformRotation * Quaternion.Euler(clampedCameraRotation);
                }
            }

            if (updateLookRotation == true || _faceMoveDirection == false)
            {
                // Setting base facing and jump rotation
                facingRotation = KCC.RenderData.TransformRotation;
                jumpRotation = KCC.RenderData.TransformRotation;
            }

            Vector3 inputDirection = default;
            bool hasInputDirection = default;
            bool faceOnMouseHold = _faceMoveDirection;

            // Do we have move direction for this render frame? Use it.
            Vector3 moveDirection = Input.RenderInput.MoveDirection.X0Y();
            if (moveDirection.IsZero() == false)
            {
                hasInputDirection = true;
                inputDirection = KCC.RenderData.TransformRotation * moveDirection;
            }

            KCC.SetInputDirection(inputDirection);

            // There is no move direction for current render input. Do we have cached move direction (accumulated in frames since last fixed update)? Then use it. It will be used next fixed update after Fusion polls new input.
            Vector3 cachedMoveDirection = Input.CachedInput.MoveDirection.X0Y();
            if (hasInputDirection == false && cachedMoveDirection.IsZero() == false)
            {
                hasInputDirection = true;
                inputDirection = KCC.RenderData.TransformRotation * cachedMoveDirection;
            }


            /** Dash not used for now
            if (KCC.HasModifier(DashProcessor) == true)
            {
                // Dash processor detected, we want the visual to face the dash direction.
                // Also force disable facing in look direction on mouse hold.

                hasInputDirection = true;
                inputDirection = DashProcessor.Direction.OnlyXZ().normalized;
                facingRotation = Quaternion.LookRotation(inputDirection);
                faceOnMouseHold = false;
            }
            */

            // Do we have any input direction (from this frame or all frames since last fixed update)? Use it.
            if (hasInputDirection == true)
            {
                Quaternion inputRotation = Quaternion.LookRotation(inputDirection);

                // We are moving in certain direction, we'll use it also for jump.
                jumpRotation = inputRotation;

                // Facing move direction enabled and right mouse button rotation lock disabled? Treat input rotation as facing as well.
                if (faceOnMouseHold == true && (_mouseHoldRotationPriority == false || Input.CachedInput.RMB == false))
                {
                    facingRotation = inputRotation;
                }
            }

            // Check for user entering chatbox mode by pressing the Enter key
            // ESC key quits chat mode
            if (Object.HasInputAuthority)
            {
                if (Input.RenderInput.Enter) 
                { 
                    _isChatting = true;
                    string message = _chatManager.ShowChatBox();

                    if (!message.Equals(""))
                        RPC_SendMessage(_nickname + ": " + message);
                }
                else if (Cursor.lockState == CursorLockMode.Locked && _chatManager.IsVisible())
                {
                    _isChatting = false;
                    _chatManager.FadeChatBox();
                }
                else if (Cursor.lockState == CursorLockMode.None && !_quitMenu && !_isChatting)
                {
                    InterfaceManager.Instance.OpenQuitMatchMenu();
                    _quitMenu = true;
                }
                else if (_quitMenu && Cursor.lockState == CursorLockMode.Locked)
                {
                    _quitMenu = false;
                }
            }

            // Player has received the ball and already has a preload so 
            // perform it now
            if (_hasPreload && _hasBall)
            {
                if (_takeShot)
                {
                    RPC_TakeShot(_shotTarget, Input.FixedInput.MoveDirection.IsZero());
                    _takeShot = false;
                }
                else if (_isSpacePass)
                {
                    RPC_SpacePass(_spacePassTarget, _preloadAngle, Input.FixedInput.MoveDirection.IsZero());
                }
                else if (inputDirection.IsZero())
                {
                    // Kick whilst idle
                    RPC_PassWhilstIdle(_preloadPowerBar, _preloadCameraRotation, _preloadAngle, _preloadYaw);
                }
                else
                {
                    // Kick whilst running
                    RPC_PassWhilstRunning(_preloadPowerBar, _preloadCameraRotation, _preloadAngle, _preloadYaw);
                }

                // Clear preload 
                _hasPreload = false;
                _preloadSymbol.SetActive(false);

                // Reset the powerbar vars
                _powerBarMask.fillAmount = 0.0f;
                _currentPowerBarValue = 0.0f;
            }
            else if (_powerBarIsActive && !_hasPreload && !_isTackling && !Input.CachedInput.LMB)
            {
                // LMB has been released so perform the kick based on powerbar input
                HandleKicking(inputDirection.IsZero());
            }
            else if (Input.CachedInput.LMB)
            {
                // No current preloads or kicks, so check for tackling or kicking input
                if (_tacklingEnabled)
                {
                    RPC_AttemptToTackle();
                }
                else
                {
                    // Start powerbar for kicking when LMB is pressed and player has the ball
                    if (!_powerBarIsActive && !_hasPreload && !_isTackling)
                    {
                        // Make powerbar icon visible to player
                        _powerBar.SetActive(true);
                        _powerBarIsActive = true;

                        // Start powerbar increase using a coroutine
                        StartCoroutine(UpdatePowerBar());
                    }
                }
            }

            // Check for space pass input
            if (Input.CachedInput.Space && !_isKicking && !_hasPreload)
            {
                float pitch = CameraPivot.eulerAngles.x < 40.0f ? CameraPivot.eulerAngles.x : 0.0f;
                _preloadAngle = 0.0f;

                if (pitch > 0)
                {
                    _preloadAngle = (40 - CameraPivot.eulerAngles.x) + 20;
                }

                _spacePassTarget = _currentTarget.transform.position;// + _currentTarget.GetVelocity();
                _isSpacePass = true;

                if (_hasBall)
                    RPC_SpacePass(_spacePassTarget, _preloadAngle, Input.FixedInput.MoveDirection.IsZero());
                else
                {
                    _hasPreload = true;
                    _preloadSymbol.SetActive(true);
                }
            }

            /** Jump not used for now
             * 
            // Jump is extrapolated for render as well.
            // Checking Input.CachedInput here. Jump accumulated from render inputs since last fixed update will trigger similar code next fixed update.
            // We have to keep the visual to face the direction if there is a jump pending execution in fixed update.
            if (Input.CachedInput.Jump == true)
            {
                // Is jump rotation invalid (not set)? Get it from other source.
                if (jumpRotation.IsZero() == true)
                {
                    // Is facing rotation valid? Use it.
                    if (facingRotation.IsZero() == false)
                    {
                        jumpRotation = facingRotation;
                    }
                    else
                    {
                        // Otherwise just jump forward.
                        jumpRotation = KCC.RenderData.TransformRotation;
                    }
                }

                // Is facing rotation invalid (not set)? Set it to the same rotation as jump.
                if (facingRotation.IsZero() == true)
                {
                    facingRotation = jumpRotation;
                }

                
                if (Input.WasActivated(EGameplayInputAction.Jump) == true)
                {
                    KCC.Jump(jumpRotation * JumpImpulse);
                }
                
            }
            */

            float mouseWheel = UnityEngine.Input.mouseScrollDelta.y * 0.2f;

            if (mouseWheel != 0)
            {
                float newCameraY = CameraHandle.localPosition.y + (mouseWheel / 2);
                float newCameraZ = CameraHandle.localPosition.z + mouseWheel;

                newCameraY = Mathf.Clamp(newCameraY, -6, -1);
                newCameraZ = Mathf.Clamp(newCameraZ, -17, -7);

                CameraHandle.localPosition = new Vector3(CameraHandle.localPosition.x, newCameraY, newCameraZ);
            }


            /** Sprint not used for now
            // Notice we are checking KCC.RenderData because we are in render update code path (fixed update uses KCC.FixedData)
            if (KCC.RenderData.IsGrounded == true)
            {
                // Sprint is updated only when grounded
                KCC.SetSprint(Input.CachedInput.Sprint);
            }
            */

            // Is facing rotation set? Apply to the visual.
            if (facingRotation.IsZero() == false)
            {
                //Visual.Root.rotation = facingRotation;
                Visual.transform.rotation = facingRotation;
            }

            // At his point, KCC haven't been updated yet (except look rotation, which propagates immediately) so camera have to be synced later.
            // Because this is advanced implementation, base class triggers manual KCC update immediately after this method.
            // This allows us to synchronize camera in OnEarlyRender(). To keep consistency with fixed update, camera related properties are updated in regular render update - OnRender().
        }


        protected override void OnRender()
        {
            // Regular render update

            // For render we care only about input authority.
            // This can be extended to state authority if needed (inner code won't be executed on host for other players, having camera pivots to be set only from fixed update, causing jitter if spectating that player)
            if (Object.HasInputAuthority == true)
            {
                Vector2 pitchRotation = KCC.FixedData.GetLookRotation(true, false);
                Vector2 clampedCameraRotation = KCCUtility.GetClampedLookRotation(pitchRotation + _renderLookRotationDelta, MinCameraAngle, MaxCameraAngle);

                if (_handleFreeLookRotation && clampedCameraRotation.y == 0.0f)
                {
                    _handleFreeLookRotation = false;
                }
                else if (!_handleFreeLookRotation)
                {
                    CameraPivot.rotation = KCC.RenderData.TransformRotation * Quaternion.Euler(clampedCameraRotation);
                }

                _shootEnabled = false;

                // Check if we need to display the shooting target icon
                Vector3 rayOrigin = new Vector3(0.5f, 0.58f, 0.0f);
                float rayLength = 500.0f;

                Ray shootingRay = Camera.main.ViewportPointToRay(rayOrigin);
                LayerMask myMask = LayerMask.GetMask("Goalmouth");

                if (Physics.Raycast(shootingRay, out RaycastHit rayTarget, rayLength, myMask))
                {
                    if (rayTarget.collider.CompareTag("BlueGoal"))
                    {
                        _shootEnabled = true;
                        _shotTarget = rayTarget.point;
                    }
                }

                // Determine which UI icons need to be shown
                if (_hasBall)
                {
                    if (_shootEnabled && !_shootIcon.activeSelf)
                    {
                        _shootIcon.SetActive(true);

                        // Disable arrows whilst shooting
                        _lowKickArrows.SetActive(false);
                        _highKickArrows.SetActive(false);
                    }
                    else if (!_shootEnabled)
                    {
                        // Turn off shoot icon if still on
                        if (_shootIcon.activeSelf)
                        {
                            _shootIcon.SetActive(false);
                        }
                    }

                    // Turn off tackle icon when in possession
                    if (_tackleIcon.activeSelf)
                    {
                        _tackleIcon.SetActive(false);
                    }
                }
                else
                {
                    if (_shootEnabled && !_shootIcon.activeSelf)
                    {
                        // Disable shoot icon if no longer in possession
                        _shootIcon.SetActive(true);

                        // Disable arrows whilst shooting
                        _lowKickArrows.SetActive(false);
                        _highKickArrows.SetActive(false);
                    }
                    else if (!_shootEnabled)
                    {
                        // Turn off shoot icon if still on
                        if (_shootIcon.activeSelf)
                        {
                            _shootIcon.SetActive(false);
                        }
                    }

                    if (_tacklingEnabled && !_tackleIcon.activeSelf)
                    {
                        // Disable arrows and enable tackle icon if tackle flag set
                        _tackleIcon.SetActive(true);

                        // Disable arrows whilst tackling
                        _lowKickArrows.SetActive(false);
                        _highKickArrows.SetActive(false);
                    }
                    else if (!_tacklingEnabled)
                    {
                        // Disable tackle icon
                        _tackleIcon.SetActive(false);


                    }
                }

                // If not shooting or tackling, show the arrows
                if (!_shootEnabled && !_tacklingEnabled)
                {
                    // Display low or high kick icon depending on camera angle
                    if (CameraPivot.eulerAngles.x < 40.0f)
                    {
                        _lowKickArrows.SetActive(false);
                        _highKickArrows.SetActive(true);
                    }
                    else
                    {
                        _lowKickArrows.SetActive(true);
                        _highKickArrows.SetActive(false);
                    }
                }

                //_playDirection.transform.position = new Vector3(KCC.transform.position.x, 0.52f, KCC.transform.position.z);
            }

            if (_faceMoveDirection == true && Object.HasInputAuthority == false)
            {
                // Facing rotation for visual is already set on input authority, here we update proxies and state authority based on [Networked] property.

                float interpolatedFacingMoveRotation = _facingMoveRotation;

                if (_facingMoveRotationInterpolator.TryGetValues(out float fromFacingMoveRotation, out float toFacingMoveRotation, out float alpha) == true)
                {
                    // Interpolation which correctly handles circular range (-180 => 180)
                    interpolatedFacingMoveRotation = KCCMathUtility.InterpolateRange(fromFacingMoveRotation, toFacingMoveRotation, -180.0f, 180.0f, alpha);
                }

                //Visual.Root.rotation = Quaternion.Euler(0.0f, interpolatedFacingMoveRotation, 0.0f);
                Visual.transform.rotation = Quaternion.Euler(0.0f, interpolatedFacingMoveRotation, 0.0f);
            }

            if (Object.HasInputAuthority)
            {
                HighlightTeammate();
            }

            if (Object.HasInputAuthority && Runner.IsForward)
                _playDirection.transform.position = new Vector3(KCC.transform.position.x, 0.52f, KCC.transform.position.z);
        }

        #endregion


        // PRIVATE METHODS

        #region Private Methods

        /****************************************
         * Handle player collisions on the server
         ****************************************/
        private void MyCollision(KCC arg1, KCCCollision arg2)
        {
            // Only server can detect collisions
            if (!Object.HasStateAuthority)
                return;

            //Debug.Log($"Collision with {arg2.Collider.name} detected!!");

            if (_hasBall || _isKicking || _serverBall.OutOfPlay)
                return;

            // Server ball collision detection
            if (arg2.Collider.tag == "Ball")
            {
                _ballCollider = arg2.Collider;

                var x = this.GetComponent<NetworkObject>().Id;
                Debug.Log($"[TPP: Server] Player {x} has the ball");

                _serverBall.CollectBall(this, _directionController);

                _hasBall = true;
                footballerAnimator.SetBool("HasBall", true);

                // Tell this client to start using a local ball
                RPC_CollectBallClient();
            }
        }



        private IEnumerator TackleDelay()
        {
            yield return new WaitForSeconds(0.5f);

            _isTackling = false;
        }

        private IEnumerator DoLoadingScreen()
        {
            _isAnimating = true; // Hack to disable input!
            yield return new WaitForSeconds(2.0f);

            _isAnimating = false;

            if (_loadingScreen != null)
                _loadingScreen.SetActive(false);

        }

        private void HandleKicking(bool isIdle)
        {
            float tmp = _currentPowerBarValue * 1.6f;

            _currentPowerBarValue = (float)System.Math.Round(tmp, 2);

            float yaw = CameraHandle.eulerAngles.y;
            float pitch = CameraPivot.eulerAngles.x < 40.0f ? CameraPivot.eulerAngles.x : 0.0f;
            float launchAngle = 0.0f;

            if (pitch > 0)
            {
                launchAngle = (40 - CameraPivot.eulerAngles.x) + 20;
            }

            // Get camera rotation including any free look rotation
            _preloadCameraRotation = Quaternion.Euler(new Vector3(pitch, yaw, 0.0f));

            _preloadAngle = (float)Math.Round(launchAngle, 2);
            _preloadYaw = (float)Math.Round(yaw, 2);

            // Low kick whilst running
            float extra = 0.0f;
            float angle = Mathf.Abs(_renderLookRotationDelta.y);

            if (!isIdle)
            {
                // Add extra power if kicking in forward direction. Extra power is a proportion of forward facing angle
                if (angle < 90.0f && Input.RenderInput.MoveDirection.y >= 0)
                    extra = ((16 - _currentPowerBarValue) / 2) * ((90 - angle) / 90);
            }

            _preloadPowerBar = (float)Math.Round(extra + _currentPowerBarValue, 2);

            // If in possession, kick immediately
            if (_hasBall)
            {
                if (_shootEnabled)
                {
                    RPC_TakeShot(_shotTarget, isIdle);
                }
                else
                {
                    // Call RPCs to kick the ball with given power in the forward direction of the camera
                    if (isIdle)
                    {
                        // Kick whilst idle
                        RPC_PassWhilstIdle(_preloadPowerBar, _preloadCameraRotation, _preloadAngle, _preloadYaw);
                    }
                    else
                    {
                        // Kick whilst running
                        RPC_PassWhilstRunning(_preloadPowerBar, _preloadCameraRotation, _preloadAngle, _preloadYaw);
                    }
                }
            }
            else
            {
                // Set pre-load flag - action will be performed when player receives the ball
                _hasPreload = true;

                if (_shootEnabled)
                {
                    _takeShot = true;
                }

                _preloadSymbol.SetActive(true);
            }

            // Stop the powerbar coroutine as it's done it's job
            StopCoroutine(UpdatePowerBar());

            // Reset the powerbar vars
            _powerBarIsActive = false;
            _powerBarMask.fillAmount = 0.0f;
            _currentPowerBarValue = 0.0f;
            _powerBar.SetActive(false);
        }

        /********************
         * Perform a LMB pass
         ********************/
        private void LMBPass()
        {
            // Set kick vector based on camera forward direction
            Quaternion yawQuat = Quaternion.Euler(new Vector3(0.0f, _fixedCameraYaw, 0.0f));

            Vector3 camForward = yawQuat * Vector3.forward;
            Vector3 direction = Quaternion.AngleAxis(_fixedLaunchAngle, yawQuat * Vector3.right) * camForward;

            direction = new Vector3(direction.x, -direction.y, direction.z);

            // Finalise the kick vector
            Vector3 kickVector = direction * _kickingPower;

            // Apply kick vector to client or server ball
            if (Object.HasStateAuthority)
            {
                _serverBall.KickBall(kickVector);

            }
            else if (Object.HasInputAuthority)
            {
                _localBallObject.SetActive(false);
                _serverBall.SetVisibility(true);

                //DrawProjection(_serverBall.transform, kickVector, _collisionMask);
            }    
        }

        /**********************
         * Perform a space pass
         **********************/
        private void SpacePass()
        {
            if (Object.HasInputAuthority)
            {
                _localBallObject.SetActive(false);
                _serverBall.SetVisibility(true);
            }
            else if (Object.HasStateAuthority)
            {
                Vector3 spaceTarg = GetSpaceTarget();

                _serverBall.SpacePass(spaceTarg, _fixedLaunchAngle);
            }
        }

        private void TakeShot()
        {
            if (Object.HasInputAuthority)
            {
                _localBallObject.SetActive(false);
                _serverBall.SetVisibility(true);
            }
            else if (Object.HasStateAuthority)
            {
                _serverBall.TakeShot(_shotTarget, _fixedLaunchAngle);
            }
        }

        private void HighlightTeammate()
        {
            float shortestAngle = 1000.0f;

            foreach (GameObject teammate in redTeamPlayers)
            {
                // Ignore a player from the team if they have disconnected
                if (teammate.IsDestroyed())
                {
                    // Can't remove here as in the middle of processing this list!
                    // TODO: Find a way to remove the player from the list if they disconnect
                    //redTeamPlayers.Remove(teammate);
                    continue;
                }

                // Ignore self
                if (teammate.GetComponent<NetworkObject>().Id == myNetworkId)
                    continue;

                Vector3 camRight = CameraPivot.right;
                Vector3 camForward = Vector3.ProjectOnPlane(CameraPivot.forward, Vector3.up).normalized;


                if (teammate.GetComponentInChildren<Renderer>().isVisible)
                {
                    Vector3 playerPos = teammate.transform.position - transform.position;
                    float angle = Mathf.Abs(Vector3.Angle(playerPos, camForward));

                    if (angle < shortestAngle)
                    {
                        shortestAngle = angle;
                        _currentTarget = teammate.GetComponent<ThirdPersonPlayer>();

                        _targetPlayerParticles.SetActive(true);
                        _targetPlayerParticles.transform.position = teammate.transform.position + Vector3.up * 0.02f;
                    }
                }
            }

            if (shortestAngle == 1000.0f)
            {
                _targetPlayerParticles.SetActive(false);
                _targetPlayerParticles.transform.parent = null;
            }


            //Debug.Log("Angle to most central player in view is: " + shortestAngle);
        }

        private Vector3 GetSpaceTarget()
        {
            float shortestAngle = 1000.0f;

            foreach (GameObject teammate in redTeamPlayers)
            {
                // Ignore a player from the team if they have disconnected
                if (teammate.IsDestroyed())
                {
                    // Can't remove here as in the middle of processing this list!
                    // TODO: Find a way to remove the player from the list if they disconnect
                    //redTeamPlayers.Remove(teammate);
                    continue;
                }

                // Ignore self
                if (teammate.GetComponent<NetworkObject>().Id == myNetworkId)
                    continue;

                Vector3 camRight = CameraPivot.right;
                Vector3 camForward = Vector3.ProjectOnPlane(CameraPivot.forward, Vector3.up).normalized;

                    Vector3 playerPos = teammate.transform.position - transform.position;
                    float angle = Mathf.Abs(Vector3.Angle(playerPos, camForward));

                    if (angle < shortestAngle)
                    {
                        shortestAngle = angle;
                        _currentTarget = teammate.GetComponent<ThirdPersonPlayer>();
                    }
            }

            return _currentTarget.transform.position + _currentTarget.GetVelocity();
        }

        /***************************************
         * Powerbar controller for kicking power
         ***************************************/
        private IEnumerator UpdatePowerBar()
        {
            while (_powerBarIsActive)
            {
                _currentPowerBarValue += 1;
                _currentPowerBarValue = Mathf.Clamp(_currentPowerBarValue, 0, _maxPowerBarValue);

                _powerBarMask.fillAmount = _currentPowerBarValue / _maxPowerBarValue;

                yield return new WaitForSeconds(0.04f);
            }

            yield return null;
        }


        private bool HasLookRotationUpdateSource(ELookRotationUpdateSource source)
        {
            return (_lookRotationUpdateSource & source) == source;
        }

        private static void OnPossessionChanged(Changed<ThirdPersonPlayer> changed)
        {
            // Add any extra functionality here when ball changes possession
        }

        private static void OnSpacePassChanged(Changed<ThirdPersonPlayer> changed)
        {
            //Debug.Log("isSpace was changed to " + changed.Behaviour._isSpacePass + " by " + changed);
        }


        #endregion

        // RPCs

        #region RPC Handler Public Methods

        /******************************************************************
         * Called when server detects a player colliding with the ball and
         * replaces the server ball with a local ball. This hides any lag
         * when moving with the ball in posession.
         ******************************************************************/
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.InputAuthority)]
        public void RPC_CollectBallClient()
        {
            // Player taking posession will start seeing a local ball rather
            // than the server ball to hide any lag.
            if (_serverBallObject.activeSelf)
            {
                // Hide server ball
                _serverBall.SetVisibility(false);

                // Enable a local ball instead
                _localBallObject.SetActive(true);  
            }

            // Set local ball dribble position
            _clientBall.SetBallVisibility(true);

            footballerAnimator.SetBool("HasBall", true);
        }


        /************************************
         * Play a low pass from idle position
         ************************************/
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_PassWhilstIdle(float power, Quaternion pitchAndYaw, float launchAngle, float yaw)
        {
            _isKicking = true;
            _isAnimating = true; // Freeze movement whilst in a static animation
            _kickingPower = power;
            _fixedCameraDir = pitchAndYaw;
            _fixedLaunchAngle = launchAngle;
            _fixedCameraYaw = yaw;

            bool isHigh = _fixedLaunchAngle > 0.0f;

            if (Object.HasStateAuthority)
            {
                _hasBall = false;

                animationHandler.TriggerKickBall(0.0f, isHigh);
            }
            else if (Object.HasInputAuthority)
            {
                // Trigger the kick animation on client after short delay
                // Ensures the animations line up
                animationHandler.TriggerKickBall(0.1f, isHigh);
            }
        }


        /*******************************
         * Play a low pass while running
         *******************************/
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_PassWhilstRunning(float power, Quaternion pitchAndYaw, float launchAngle, float yaw)
        {
            _isKicking = true;
            _kickingPower = power;
            _fixedCameraDir = pitchAndYaw;
            _fixedLaunchAngle = launchAngle;
            _fixedCameraYaw = yaw;

            bool isHigh = _fixedLaunchAngle > 0.0f;

            if (Object.HasStateAuthority)
            {
                _hasBall = false;

                animationHandler.TriggerKickBall(0.0f, isHigh);
            }
            else if (Object.HasInputAuthority)
            {
                animationHandler.TriggerKickBall(0.2f, isHigh);
            }
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_TakeShot(Vector3 target, bool isIdle)
        {
            _isShooting = true;
            _isKicking = true;
            _shotTarget = target;

            float shotDistance = Vector3.Distance(transform.position, target);

            // Try to adjust launch angle based on distance from goal & target point of goal
            // Shot power is basically tied to launch angle - the lower the angle the more powerful the shot hence
            // the need to adjust for distance.
            // Minimum/maximum shot angle is currently set to 15 & 40 degrees respectively
            // Could also introduce a per player shooting power component to this formula
            float angle = (target.y * 15) + (shotDistance / 5);

            // Clamp the shot launch angle range to avoid silly values
            _fixedLaunchAngle = Mathf.Clamp(angle, 15, 40);

            // Trigger the shooting animation on server
            if (Object.HasStateAuthority)
            {
                _hasBall = false;

                animationHandler.TriggerTakeShot(0.0f);
            }
            else if (Object.HasInputAuthority)
            {
                float delay = 0.2f;

                if (isIdle)
                {
                    delay = 0.1f;
                    _isAnimating = true;
                }

                animationHandler.TriggerTakeShot(delay);
            }
        }

        /*******************************
         * Play a space pass
         *******************************/
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_SpacePass(Vector3 target, float launchAngle, bool isIdle)
        {
            _isSpacePass = true;
            _isKicking = true;
            _spacePassTarget = target;
            _fixedLaunchAngle = launchAngle;

            bool isHigh = _fixedLaunchAngle > 10.0f;

            if (Object.HasStateAuthority)
            {
                _hasBall = false;

                animationHandler.TriggerKickBall(0.0f, isHigh);
            }
            else if (Object.HasInputAuthority)
            {
                float delay = 0.2f;

                if (isIdle)
                {
                    delay = 0.1f;
                    _isAnimating = true;
                }

                animationHandler.TriggerKickBall(delay, isHigh);
            }
        }


        /****************************
         * Ball has gone out of play
         ****************************/
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
        public void RPC_BallOutOfPlay(NetworkBool ballInGoal)
        {
            _outOfPlay = true;

            if (ballInGoal)
                _serverBall.SetOutOfPlay(KCC.transform.forward * 5.0f, true);
            else
                _serverBall.SetOutOfPlay(KCC.transform.forward * 5.0f, false);

            _hasBall = false;
        }

        /***************************************************
         * Attempt tackle on server. If successfull trigger
         * player tackled animation on clients.
         ***************************************************/
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
        public void RPC_AttemptToTackle()
        {
            if (_isTackling)
                return;

            Debug.Log("Attempting tackle...");

            _isTackling = true;

            if (_oppDistance < _playerTackleAbility)
            {
                Debug.Log("Tackle succeeded!!");

                // Take possession with this player
                RPC_TakePossession();

                //_isTackling = false;
                _tacklingEnabled = false;

                // Dispossess player in possession
                _playerInPossession.RPC_LostPossession();
            }
            else
            {
                Debug.Log("Tackle failed!");

                RPC_FailedTackle();           
            }

            // Cause a delay before player can tackle again
            StartCoroutine(TackleDelay());
        }

        /************************************
         * Player has been tackled by an opp
         ************************************/
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_FailedTackle()
        {
            _isAnimating = true;
            animationHandler.TriggerPlayerTackled();
        }

        /************************************
         * Player has been tackled by an opp
         ************************************/
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_LostPossession()
        {
            animationHandler.TriggerPlayerTackled();

            if (Object.HasInputAuthority)
            {
                // Dispossess current player with the ball
                _localBallObject.SetActive(false);
                _serverBall.SetVisibility(true);

                _isAnimating = true;
            }
            else if (Object.HasStateAuthority)
                _hasBall = false;
        }

        /******************************
         * Take possession of the ball
         ******************************/
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_TakePossession()
        {
            Debug.Log("Taking possession...");

            animationHandler.TriggerTacklingAnimation();

            _hasBall = true;

            // Take possession of ball
            if (Object.HasStateAuthority)
            {
                _serverBall.CollectBall(this, _directionController);
            }
            else if (Object.HasInputAuthority)
            {
                _serverBall.SetVisibility(false);
                _clientBall.SetBallVisibility(true);
            }
        }


        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPC_UpdateTeamList()
        {
            GameObject[] redTeam = GameObject.FindGameObjectsWithTag("RedTeam");
            redTeamPlayers = new List<GameObject>(redTeam);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_PlaySound(SoundManager.Effect soundEffect, float volume)
        {
            SoundManager.PlaySound(soundEffect, volume);
        }
        
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
        public void RPC_SendMessage(string message)
        {
            _chatManager.RPC_ShowChatMessage(message);
        }
        
        #endregion

        // ANIMATOR CALLBACKS

        #region SimplePlayerAnimator Callback Public Methods

        // These methods are called when SimplePlayerAnimator receives an 
        // animation event and needs an action

        /****************************
         * Start of kicking animation
         ****************************/
        public void KickBall(bool isRunning)
        {
            //Debug.Log("Kicking ball - shoot enabled flag = " + _isShooting);

            // Call appropriate shoot/pass routine
            if (_isSpacePass)
            {
                SpacePass();

                _isSpacePass = false;
            }
            else if (_isShooting)
            {
                TakeShot();

                _isShooting = false;
            }
            else
                LMBPass();
        }

        /**************************
         * End of kicking animation
         **************************/
        public void KickBallEnd()
        {
            _isKicking = false;
            _isAnimating = false;
            _kickingPower = 0.0f;

            Physics.IgnoreLayerCollision(7, 3, false);
        }

        public void StumbleEnd()
        {
            _isAnimating = false;
        }

        #endregion

        // DATA STRUCTURES

        [Flags]
        private enum ELookRotationUpdateSource
        {
            Jump = 1 << 0, // Look rotation is updated on jump
            Movement = 1 << 1, // Look rotation is updated on character movement
            MouseHold = 1 << 2, // Look rotation is updated while holding right mouse button
            MouseMovement = 1 << 3, // Look rotation is updated on mouse move
            Dash = 1 << 4, // Look rotation is updated on dash
        }

        /// <summary>
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// DEBUG ONLY!
        /// </summary>



        public Vector3 DrawProjection(Transform releasePosition, Vector3 releaseVelocity, LayerMask collisionMask)
        {
            //Debug.Log("[LineRenderer] releasePosition = " + releasePosition.position);
            //Debug.Log("[LineRenderer] releaseVelocity = " + releaseVelocity);

            /********************************************************
             * Add this to ServerBall before calling DrawProjection
             ********************************************************/

            int layer = _serverBallObject.layer; // Set this to serverball layer
            
            for (int z = 0; z < 32; z++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, z))
                {
                    collisionMask |= 1 << z;
                }
            }
            
            //////////////////////////////////////////////////////////


            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = Mathf.CeilToInt(_linePoints / _timeBetweenPoints) + 1;

            Vector3 startPosition = releasePosition.position; // Server ball start position
            Vector3 startVelocity = releaseVelocity / BALL_MASS; // Server ball start velocity / mass of server ball

            int i = 0;

            _lineRenderer.SetPosition(i, startPosition);

            for (float time = 0; time < _linePoints; time += _timeBetweenPoints)
            {
                i++;

                Vector3 point = startPosition + time * startVelocity;

                // d = vt + 1/2 at^2
                point.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2.0f * time * time);

                _lineRenderer.SetPosition(i, point);

                // Stop the line renderer once it hits something
                Vector3 lastPosition = _lineRenderer.GetPosition(i - 1);

                
                if (Physics.Raycast(lastPosition, (point - lastPosition).normalized, out RaycastHit hit, (point - lastPosition).magnitude, collisionMask))
                {
                    //Debug.Log("[LineRenderer] collision with " + hit.transform.gameObject.name);

                    //_lineRenderer.SetPosition(i, hit.point);
                    //_lineRenderer.positionCount = i + 1;
                    //return hit.point;
                }
                
            }

            return _lineRenderer.transform.position;
        }
    }

}
