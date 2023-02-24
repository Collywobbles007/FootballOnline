namespace Fusion.Collywobbles.Futsal
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem; // Extra! Installed from package manager
    using Fusion;
    using Fusion.KCC;
    using UnityEngine.EventSystems;


    public class PlayerInput : NetworkBehaviour, IBeforeUpdate, IBeforeTick
    {
        // PUBLIC MEMBERS

        /// <summary>
        /// Holds input for fixed update.
        /// </summary>
        public GameplayInput FixedInput { get { CheckFixedAccess(false); return _fixedInput; } }

        /// <summary>
        /// Holds input for current frame render update.
        /// </summary>
        public GameplayInput RenderInput { get { CheckRenderAccess(false); return _renderInput; } }

        /// <summary>
        /// Holds combined inputs from all render frames since last fixed update. Used when Fusion input poll is triggered.
        /// </summary>
        public GameplayInput CachedInput { get { CheckRenderAccess(false); return _cachedInput; } }

        // PRIVATE MEMBERS

        [SerializeField]
        [Tooltip("Mouse delta multiplier.")]
        private Vector2 _standaloneLookSensitivity = Vector2.one;

        [SerializeField]
        [Range(0.0f, 0.1f)]
        [Tooltip("Look rotation delta for a render frame is calculated as average from all frames within responsivity time.")]
        private float _lookResponsivity = 0.025f;

        // We need to store last known input to compare current input against (to track actions activation/deactivation). It is also used if an input for current frame is lost.
        // This is not needed on proxies, only input authority is registered to nameof(PlayerInput) interest group.
        [Networked(nameof(PlayerInput))]
        private GameplayInput _lastKnownInput { get; set; }

        private GameplayInput _fixedInput;
        private GameplayInput _renderInput;
        private GameplayInput _cachedInput;
        private GameplayInput _baseFixedInput;
        private GameplayInput _baseRenderInput;

        private Vector2 _cachedMoveDirection;
        private float _cachedMoveDirectionSize;
        private FrameRecord[] _frameRecords = new FrameRecord[32];

        private bool _resetCachedInput;

        // PRIVATE STRUCTS
        private struct FrameRecord
        {
            public float DeltaTime;
            public Vector2 LookRotationDelta;

            public FrameRecord(float deltaTime, Vector2 lookRotationDelta)
            {
                DeltaTime = deltaTime;
                LookRotationDelta = lookRotationDelta;
            }
        }

        // PUBLIC METHODS

        #region Input Activation Checks

        /// <summary>
        /// Check if an action is active in current input. FixedUpdateNetwork/Render input is resolved automatically.
        /// </summary>
        public bool HasActive(EGameplayInputAction action)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.IsActive(_fixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.IsActive(_renderInput);
            }
        }

        /// <summary>
        /// Check if an action was activated in current input.
        /// In FixedUpdateNetwork this method compares current fixed input agains previous fixed input.
        /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FixedUpdateNetwork).
        /// </summary>
        public bool WasActivated(EGameplayInputAction action)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasActivated(_fixedInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasActivated(_renderInput, _baseRenderInput);
            }
        }

        /// <summary>
        /// Check if an action was activated in custom input.
        /// In FixedUpdateNetwork this method compares custom input agains previous fixed input.
        /// In Render this method compares custom input against previous render input OR current fixed input (first Render call after FixedUpdateNetwork).
        /// </summary>
        public bool WasActivated(EGameplayInputAction action, GameplayInput customInput)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasActivated(customInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasActivated(customInput, _baseRenderInput);
            }
        }

        /// <summary>
        /// Check if an action was deactivated in current input.
        /// In FUN this method compares current fixed input agains previous fixed input.
        /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
        /// </summary>
        public bool WasDeactivated(EGameplayInputAction action)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasDeactivated(_fixedInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasDeactivated(_renderInput, _baseRenderInput);
            }
        }

        /// <summary>
        /// Check if an action was deactivated in custom input.
        /// In FUN this method compares custom input agains previous fixed input.
        /// In Render this method compares custom input against previous render input OR current fixed input (first Render call after FUN).
        /// </summary>
        public bool WasDeactivated(EGameplayInputAction action, GameplayInput customInput)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasDeactivated(customInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasDeactivated(customInput, _baseRenderInput);
            }
        }


        #endregion

        #region Set Inputs

        /// <summary>
        /// Updates fixed input. Use after manipulating with fixed input outside.
        /// </summary>
        /// <param name="fixedInput">Input used in fixed update.</param>
        /// <param name="updateBaseInputs">Updates base fixed input and base render input.</param>
        public void SetFixedInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            CheckFixedAccess(true);

            _fixedInput = fixedInput;

            if (updateBaseInputs == true)
            {
                _baseFixedInput = fixedInput;
                _baseRenderInput = fixedInput;
            }
        }

        /// <summary>
        /// Updates render input. Use after manipulating with render input outside.
        /// </summary>
        /// <param name="renderInput">Input used in render update.</param>
        /// <param name="updateBaseInput">Updates base render input.</param>
        public void SetRenderInput(GameplayInput renderInput, bool updateBaseInput)
        {
            CheckRenderAccess(false);

            _renderInput = renderInput;

            if (updateBaseInput == true)
            {
                _baseRenderInput = renderInput;
            }
        }

        /// <summary>
        /// Updates last known input. Use after manipulating with fixed input outside.
        /// </summary>
        /// <param name="fixedInput">Input used as last known input.</param>
        /// <param name="updateBaseInputs">Updates base fixed input and base render input.</param>
        public void SetLastKnownInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            CheckFixedAccess(true);

            _lastKnownInput = fixedInput;

            if (updateBaseInputs == true)
            {
                _baseFixedInput = fixedInput;
                _baseRenderInput = fixedInput;
            }
        }

        #endregion


        #region NetworkBehaviour Overrides

        /// <summary>
        /// Initialise input polling when player spawns in.
        /// </summary>
        public override void Spawned()
        {
            // Reset to default state.
            _fixedInput = default;
            _renderInput = default;
            _cachedInput = default;
            _lastKnownInput = default;
            _baseFixedInput = default;
            _baseRenderInput = default;

            if (Object.HasStateAuthority == true)
            {
                // Only state and input authority works with input and access _lastFixedInput
                Object.SetInterestGroup(Object.InputAuthority, nameof(PlayerInput), true);
            }

            if (Object.HasInputAuthority == true)
            {
                // Register local player input polling.
                NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();

                networkEvents.OnInput.RemoveListener(OnInput);
                networkEvents.OnInput.AddListener(OnInput);

                // Hide cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Deregister input polling when player despawns from game.
        /// </summary>
        /// <param name="runner">Newtwork Runner</param>
        /// <param name="hasState"></param>
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _frameRecords.Clear();

            if (runner == null)
                return;

            NetworkEvents networkEvents = runner.GetComponent<NetworkEvents>();

            if (networkEvents != null)
            {
                // Unregister local player input polling.
                networkEvents.OnInput.RemoveListener(OnInput);
            }
        }

        #endregion


        #region INTERFACE implementations IBeforeUpdate, IBeforeTick

        // IBeforeUpdate INTERFACE implementation

        /// <summary>
        /// 1. Collect input from devices, can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed
        /// </summary>
        public void BeforeUpdate()
        {
            if (Object == null || Object.HasInputAuthority == false)
                return;

            // Store last render input as a base to current render input
            _baseRenderInput = _renderInput;

            // Reset input for current frame to default
            _renderInput = default;

            // Cached input was polled and explicit reset requested
            if (_resetCachedInput == true)
            {
                _resetCachedInput = false;

                _cachedInput = default;
                _cachedMoveDirection = default;
                _cachedMoveDirectionSize = default;
            }

            Keyboard keyboard = Keyboard.current;

            // Cursor lock processing. Toggle on/off with enter key
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame == true)
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            // Input is tracked only if the cursor is locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                ProcessTextEntry();
                return;
            }

            ProcessStandaloneInput();
        }

        private void ProcessTextEntry()
        {
            // Process keyboard input
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                _renderInput.Enter = keyboard.enterKey.isPressed; // Enter key = enable/submit chat messages
            }
        }

        // IBeforeTick INTERFACE implementation

        /// <summary>
        /// 3. Read input from Fusion. On input authority the FixedInput will match CachedInput.
        /// </summary>
        public void BeforeTick()
        {
            // Store last known fixed input. This will be compared agaisnt new fixed input
            _baseFixedInput = _lastKnownInput;

            // Set fixed input to last known fixed input as a fallback
            _fixedInput = _lastKnownInput;

            // Clear all properties which should not propagate from last known input in case of missing input
            _fixedInput.LookRotationDelta = default;

            if (Object.InputAuthority != PlayerRef.None)
            {
                // If this fails, fallback (last known) input will be used as current
                if (Runner.TryGetInputForPlayer(Object.InputAuthority, out GameplayInput input) == true)
                {
                    // New input received, we can store it
                    _fixedInput = input;

                    // Update last known input. Will be used next tick as base and fallback
                    _lastKnownInput = input;
                }
            }

            // The current fixed input will be used as a base to first Render after FixedUpdateNetwork()
            _baseRenderInput = _fixedInput;
        }

        #endregion


        /// <summary>
        /// 2. Push cached input and reset properties, can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation (or there is a performance spike).
        /// </summary>
        private void OnInput(NetworkRunner runner, NetworkInput networkInput)
        {
            GameplayInput gameplayInput = _cachedInput;

            // Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
            // This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).

            _resetCachedInput = true;

            // Now we reset all properties which should not propagate into next OnInput() call (for example LookRotationDelta - this must be applied only once and reset immediately).
            // If there's a spike, OnInput() and FixedUpdateNetwork() will be called multiple times in a row without BeforeUpdate() in between, so we don't reset move direction to preserve movement.
            // Instead, move direction and other sensitive properties are reset in next BeforeUpdate() - driven by _resetCachedInput.

            _cachedInput.LookRotationDelta = default;

            // Input consumed by OnInput() call will be read in FixedUpdateNetwork() and immediately propagated to KCC.
            // Here we should reset render properties so they are not applied twice (fixed + render update).

            _renderInput.LookRotationDelta = default;

            networkInput.Set(gameplayInput);
        }

        private void ProcessStandaloneInput()
        {
            // Always use KeyControl.isPressed, Input.GetMouseButton() and Input.GetKey()
            // Never use KeyControl.wasPressedThisFrame, Input.GetMouseButtonDown() or Input.GetKeyDown() otherwise the action might be lost

            Vector2 moveDirection = Vector2.zero;
            Vector2 lookRotationDelta = Vector2.zero;

            // Process mouse input
            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                lookRotationDelta = ProcessLookRotationDelta(new Vector2(-mouseDelta.y, mouseDelta.x), _standaloneLookSensitivity);

                _renderInput.LMB = mouse.leftButton.isPressed;
                _renderInput.RMB = mouse.rightButton.isPressed;
                _renderInput.MMB = mouse.middleButton.isPressed;
            }

            // Process keyboard input
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                // WASD keyboard control
                if (keyboard.wKey.isPressed == true) { moveDirection += Vector2.up; }
                if (keyboard.sKey.isPressed == true) { moveDirection += Vector2.down; }
                if (keyboard.aKey.isPressed == true) { moveDirection += Vector2.left; }
                if (keyboard.dKey.isPressed == true) { moveDirection += Vector2.right; }

                if (moveDirection.IsZero() == false)
                {
                    moveDirection.Normalize();
                }

                _renderInput.Space = keyboard.spaceKey.isPressed; // Space pass
                _renderInput.Sprint = keyboard.leftShiftKey.isPressed; // Player sprint
                _renderInput.TAB = keyboard.tabKey.isPressed; // TAB used to collect ball or cancel preload
                _renderInput.Enter = keyboard.enterKey.isPressed; // Enter key = enable/submit chat messages

                // Quick short messages for chat
                _renderInput.F1 = keyboard.f1Key.isPressed; // F1 = Good tackle
                _renderInput.F2 = keyboard.f2Key.isPressed; // F2 = Good pass
                _renderInput.F3 = keyboard.f3Key.isPressed; // F3 = Good goal
                _renderInput.F4 = keyboard.f4Key.isPressed; // F4 = Good game
                _renderInput.F5 = keyboard.f5Key.isPressed; // F5 = Pass, I'm open
                _renderInput.F6 = keyboard.f6Key.isPressed; // F6 = Help defence
                _renderInput.F7 = keyboard.f7Key.isPressed; // F7 = Thanks
                _renderInput.F8 = keyboard.f8Key.isPressed; // F8 = Sorry
                _renderInput.F9 = keyboard.f9Key.isPressed; // F9 = Unlucky
                _renderInput.F10 = keyboard.f10Key.isPressed; // F10 = BRB
            }

            _renderInput.MoveDirection = moveDirection;
            _renderInput.LookRotationDelta = lookRotationDelta;

            // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update

            float deltaTime = Time.deltaTime;

            // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
            // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
            // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
            // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
            // This way the next fixed update will correspond more accurately to what happened in render frames.
            _cachedMoveDirection += moveDirection * deltaTime;
            _cachedMoveDirectionSize += deltaTime;

            // Cached input will eventually be sent as network input via OnInput
            _cachedInput.Actions = new NetworkButtons(_cachedInput.Actions.Bits | _renderInput.Actions.Bits);
            _cachedInput.MoveDirection = _cachedMoveDirection / _cachedMoveDirectionSize;
            _cachedInput.LookRotationDelta += _renderInput.LookRotationDelta;
        }

        private Vector2 ProcessLookRotationDelta(Vector2 lookRotationDelta, Vector2 lookRotationSensitivity)
        {
            lookRotationDelta *= lookRotationSensitivity;

            // If the look rotation responsivity is enabled, calculate average delta instead
            if (_lookResponsivity > 0.0f)
            {
                // Kill any rotation in opposite direction for instant direction flip.
                CleanLookRotationDeltaHistory(lookRotationDelta);

                FrameRecord frameRecord = new FrameRecord(Time.unscaledDeltaTime, lookRotationDelta);

                // Shift history with frame records
                Array.Copy(_frameRecords, 0, _frameRecords, 1, _frameRecords.Length - 1);

                // Store current frame to history
                _frameRecords[0] = frameRecord;

                float accumulatedDeltaTime = default;
                Vector2 accumulatedLookRotationDelta = default;

                // Iterate over all frame records
                for (int i = 0; i < _frameRecords.Length; ++i)
                {
                    frameRecord = _frameRecords[i];

                    // Accumualte delta time and look rotation delta until we pass responsivity threshold.
                    accumulatedDeltaTime += frameRecord.DeltaTime;
                    accumulatedLookRotationDelta += frameRecord.LookRotationDelta;

                    if (accumulatedDeltaTime > _lookResponsivity)
                    {
                        // To have exact responsivity time window length, we have to remove delta overshoot from last accumulation.

                        float overshootDeltaTime = accumulatedDeltaTime - _lookResponsivity;

                        accumulatedDeltaTime -= overshootDeltaTime;
                        accumulatedLookRotationDelta -= overshootDeltaTime * frameRecord.LookRotationDelta;

                        break;
                    }
                }

                // Normalize acucmulated look rotation delta and calculate size for current frame
                lookRotationDelta = (accumulatedLookRotationDelta / accumulatedDeltaTime) * Time.unscaledDeltaTime;
            }

            return lookRotationDelta;
        }

        private void CleanLookRotationDeltaHistory(Vector2 lookRotationDelta)
        {
            int count = _frameRecords.Length;

            // Iterate over all records and clear rotation with opposite direction, giving instant responsivity when direction flips.
            // Each axis is processed separately.

            if (lookRotationDelta.x < 0.0f) { for (int i = 0; i < count; ++i) { if (_frameRecords[i].LookRotationDelta.x > 0.0f) { _frameRecords[i].LookRotationDelta.x = 0.0f; } } }
            if (lookRotationDelta.x > 0.0f) { for (int i = 0; i < count; ++i) { if (_frameRecords[i].LookRotationDelta.x < 0.0f) { _frameRecords[i].LookRotationDelta.x = 0.0f; } } }
            if (lookRotationDelta.y < 0.0f) { for (int i = 0; i < count; ++i) { if (_frameRecords[i].LookRotationDelta.y > 0.0f) { _frameRecords[i].LookRotationDelta.y = 0.0f; } } }
            if (lookRotationDelta.y > 0.0f) { for (int i = 0; i < count; ++i) { if (_frameRecords[i].LookRotationDelta.y < 0.0f) { _frameRecords[i].LookRotationDelta.y = 0.0f; } } }
        }


        #region Error checks for development

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void CheckFixedAccess(bool checkStage)
        {
            if (checkStage == true && Runner.Stage == default)
            {
                throw new InvalidOperationException("This call should be executed from FixedUpdateNetwork!");
            }

            if (Runner.Stage != default && Object.IsProxy == true)
            {
                throw new InvalidOperationException("Fixed input is available only on State & Input authority!");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void CheckRenderAccess(bool checkStage)
        {
            if (checkStage == true && Runner.Stage != default)
            {
                throw new InvalidOperationException("This call should be executed outside of FixedUpdateNetwork!");
            }

            if (Runner.Stage == default && Object.HasInputAuthority == false)
            {
                throw new InvalidOperationException("Render and cached inputs are available only on Input authority!");
            }
        }

        #endregion
    }
}
