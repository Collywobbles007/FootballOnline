namespace Fusion.Collywobbles.Futsal
{
    using Fusion;
    using Fusion.KCC;
    using System.Numerics;
    using UnityEngine;
    using Quaternion = UnityEngine.Quaternion;

    /// <summary>
    /// Base class for advanced variant of controlling player.
    /// Provides API for correct execution order.
    /// </summary>
    [OrderBefore(typeof(KCC))]
    [OrderAfter(typeof(Player))]
    public abstract class AdvancedPlayer : Player
    {
        // PRIVATE MEMBERS

        private BeforeHitboxManagerUpdater _beforeHitboxManagerUpdater;
        private AfterHitboxManagerUpdater _afterHitboxManagerUpdater;

        // AdvancedPlayer INTERFACE

        protected abstract void ProcessEarlyFixedInput();
        protected abstract void ProcessLateFixedInput();
        protected abstract void ProcessRenderInput();

        protected virtual void OnSpawned() { }
        protected virtual void OnDespawned() { }
        protected virtual void OnBeforeUpdate() { }
        protected virtual void OnEarlyFixedUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnLateFixedUpdate() { }
        protected virtual void OnEarlyRender() { }
        protected virtual void OnRender() { }
        protected virtual void OnLateRender() { }

        // NetworkBehaviour INTERFACE

        public override sealed void Spawned()
        {
            base.Spawned();

            // All advanced players have BeforeHitboxManagerUpdater and AfterHitboxManagerUpdater component.

            // BeforeHitboxManagerUpdater provides callbacks which are executed before HitboxManager => we use this to process "movement" input - set move direction, jump, look rotation, ...

            _beforeHitboxManagerUpdater = AddBehaviour<BeforeHitboxManagerUpdater>();
            _beforeHitboxManagerUpdater.SetDelegates(EarlyFixedUpdate, EarlyRender);
            Runner.AddSimulationBehaviour(_beforeHitboxManagerUpdater, Object);

            // AfterHitboxManagerUpdater provides callbacks which are executed after HitboxManager => we use this to process "non-movement" input - shooting, actions, ...

            _afterHitboxManagerUpdater = AddBehaviour<AfterHitboxManagerUpdater>();
            _afterHitboxManagerUpdater.SetDelegates(LateFixedUpdate, LateRender);
            Runner.AddSimulationBehaviour(_afterHitboxManagerUpdater, Object);

            // KCC is updated manually to preserve correct execution order.
            KCC.SetManualUpdate(true);

            OnSpawned();
        }

        public override sealed void Despawned(NetworkRunner runner, bool hasState)
        {
            OnDespawned();

            if (runner != null)
            {
                runner.RemoveSimulationBehavior(_beforeHitboxManagerUpdater);
                runner.RemoveSimulationBehavior(_afterHitboxManagerUpdater);
            }

            _beforeHitboxManagerUpdater = null;
            _afterHitboxManagerUpdater = null;

            base.Despawned(runner, hasState);
        }

        /// <summary>
        /// 2. Regular fixed update for Player/AdvancedPlayer
        /// </summary>
        public override sealed void FixedUpdateNetwork()
        {
            // At this point, KCC and Transform is already updated (only input and state authority).

            // This call includes setting of AoI from base class (using correct position compared to SimplePlayer).
            base.FixedUpdateNetwork();

            if (Culling.IsCulled == true)
                return;

            if (Runner.Stage == SimulationStages.Forward && Object.IsProxy == true)
            {
                // Interpolate proxies early before HitboxManager updates.
                KCC.Interpolate();
            }

            // At this point all players (including proxies) have set their positions and rotations, we can run some post-processing (setting camera pivots, synchronizing other owned objects, ...).

            OnFixedUpdate();
        }

        /// <summary>
        /// 5. Regular render update for Player/AdvancedPlayer
        /// </summary>
        public override sealed void Render()
        {
            base.Render();

            if (Culling.IsCulled == true)
                return;

            // At this point all players have set their positions and rotations, we can run some post-processing (setting camera pivots, synchronizing other owned objects, ...).

            OnRender();
        }

        // PRIVATE METHODS

        /// <summary>
        /// 1. Update PlayerInput instance and process input for fixed update. Only input and state authority will make changes, proxies stay unaffected.
        /// </summary>
        private void EarlyFixedUpdate()
        {
            if (Culling.IsCulled == true)
                return;

            // At this point, PlayerInput.BeforeUpdate() and PlayerInput.OnInput() were already called so we have the input ready.
            // PlayerInput.FixedUpdateNetwork() was called as well so Input.FixedInput is set and can be processed by derived classes.

            if (Object.IsProxy == false)
            {
                // This method expects derived classes to make movement / look related calls to KCC.
                // Only state and input authority can process input.
                ProcessEarlyFixedInput();
            }

            // All movement related properties set, we can trigger manual KCC update.
            KCC.ManualFixedUpdate();

            // This method can be used to post-process KCC update (Transform is already updated as well).
            // This is executed before any of Player/AdvancedPlayer and HitboxManager FixedUpdateNetwork().
            OnEarlyFixedUpdate();
        }

        /// <summary>
        /// 3. Executed after all Player/AdvancedPlayer and HitboxManager FixedUpdateNetwork() calls, process rest of player input (shooting, other non-movement related actions).
        /// </summary>
        private void LateFixedUpdate()
        {
            if (Culling.IsCulled == true)
                return;

            if (Object.IsProxy == false)
            {
                // Only state and input authority can process input.
                ProcessLateFixedInput();
            }

            // This method can be used to react on player actions. At this point player input has been processed completely.
            OnLateFixedUpdate();
        }

        /// <summary>
        /// 4. Process input for render update. Only input and state authority will make changes, proxies are already interpolated.
        /// </summary>
        private void EarlyRender()
        {
            if (Culling.IsCulled == true)
                return;

            if (Object.HasInputAuthority == true)
            {
                // This method expects derived classes to make movement / look related calls to KCC.
                ProcessRenderInput();
            }

            // All movement related properties set, we can trigger manual KCC update.
            KCC.ManualRenderUpdate();

            // This method can be used to post-process KCC update (Transform is already updated as well).
            // This is executed before any of Player/AdvancedPlayer Render().
            OnEarlyRender();
        }

        /// <summary>
        /// 6. Executed after all Player/AdvancedPlayer Render() calls
        /// </summary>
        private void LateRender()
        {
            if (Culling.IsCulled == true)
                return;

            // Here comes "late" render input processing of all other non-movement actions.
            // This gives you extra responsivity at the cost of maintaining extrapolation and reconcilliation.
            // Currently there are no specific actions extrapolated for render.

            // Setting base camera transform based on handle

            if (Camera != null && CameraHandle != null && !DisableCameraFollow)
            {
                Camera.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
            }

            // This method can be used to override final state of the object for render. At this point player input has been processed completely.
            OnLateRender();
        }
    }
}
