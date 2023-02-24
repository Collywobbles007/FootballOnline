namespace Fusion.Collywobbles.Futsal
{
    using System;
    using UnityEngine;
    using Fusion;

    public class NetworkCulling : NetworkBehaviour
    {
        // PUBLIC MEMBERS

        public bool IsCulled => _isCulled;

        public Action<bool> Updated;

        // PRIVATE MEMBERS

        [Networked]
        private NetworkBool _keepAlive { get; set; }

        private int _tickRate;
        private bool _isCulled;


        // NetworkBehaviour INTERFACE

        public override sealed void Spawned()
        {
            _tickRate = Runner.Config.Simulation.TickRate;
            _isCulled = false;
        }

        public override sealed void Despawned(NetworkRunner runner, bool hasState)
        {
            _isCulled = false;
        }

        public override sealed void FixedUpdateNetwork()
        {
            if (Runner == null || Runner.IsForward == false)
                return;

            // Trigger network synchronization once per second
            int simulationTick = Runner.Simulation.Tick;
            if (simulationTick % _tickRate == 0)
            {
                _keepAlive = !_keepAlive;
            }

            bool isCulled = false;

            if (Object.IsProxy == true && Object.LastReceiveTick > 0)
            {
                // The object is treated as culled if it hasn't received update for more than 2 second.
                int lastReceiveTickThreshold = simulationTick - _tickRate * 2;

                SimulationSnapshot serverState = Runner.Simulation.LatestServerState;
                if (serverState != null)
                {
                    // Clients have to check against latest server state tick received, otherwise it won't work with high ping
                    lastReceiveTickThreshold = serverState.Tick - _tickRate * 2;
                }

                if (Object.LastReceiveTick < lastReceiveTickThreshold)
                {
                    isCulled = true;
                }
            }

            if (_isCulled != isCulled)
            {
                _isCulled = isCulled;

                if (Updated != null)
                {
                    try
                    {
                        Updated(isCulled);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }
    }
}