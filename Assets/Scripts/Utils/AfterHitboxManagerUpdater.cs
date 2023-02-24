namespace Fusion.Collywobbles.Futsal
{
    using System;
    using Fusion;

    /// <summary>
    /// Helper component to invoke fixed and render update methods after HitboxManager.
    /// </summary>
    [OrderAfter(typeof(HitboxManager))]
    public sealed class AfterHitboxManagerUpdater : SimulationBehaviour
    {
        // PRIVATE MEMBERS

        private Action _fixedUpdate;
        private Action _render;

        // PUBLIC METHODS

        public void SetDelegates(Action fixedUpdateDelegate, Action renderDelegate)
        {
            _fixedUpdate = fixedUpdateDelegate;
            _render = renderDelegate;
        }

        // SimulationBehaviour INTERFACE

        public override void FixedUpdateNetwork()
        {
            if (_fixedUpdate != null)
            {
                _fixedUpdate();
            }
        }

        public override void Render()
        {
            if (_render != null)
            {
                _render();
            }
        }
    }
}
