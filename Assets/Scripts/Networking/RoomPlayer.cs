namespace Fusion.Collywobbles.Futsal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fusion;
    using UnityEngine;

    public class RoomPlayer : NetworkBehaviour
    {
        public enum EGameState
        {
            Lobby,
            GameCutscene,
            GameReady
        }

        public static readonly List<RoomPlayer> Players = new List<RoomPlayer>();

        public static Action<RoomPlayer> PlayerJoined;
        public static Action<RoomPlayer> PlayerLeft;
        public static Action<RoomPlayer> PlayerChanged;

        public static RoomPlayer Local;

        [Networked(OnChanged = nameof(OnStateChanged))] public NetworkBool IsReady { get; set; }
        [Networked(OnChanged = nameof(OnStateChanged))] public NetworkString<_32> Username { get; set; }
        [Networked] public ThirdPersonPlayer Player { get; set; }
        [Networked] public EGameState GameState { get; set; }
        [Networked] public int PlayerModelId { get; set; }
        [Networked] public int PlayerRef { get; set; }

        public bool IsServer => Object != null && Object.IsValid && Object.HasStateAuthority;

        private static bool IsAllReady() => RoomPlayer.Players.Count > 0 && RoomPlayer.Players.All(player => player.IsReady);

        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasInputAuthority)
            {
                Local = this;

                PlayerChanged?.Invoke(this);
                RPC_SetPlayerStats(ClientInfo.Username, ClientInfo.PlayerModelId);
            }

            Players.Add(this);
            PlayerJoined?.Invoke(this);

            DontDestroyOnLoad(gameObject);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, InvokeResim = true)]
        private void RPC_SetPlayerStats(NetworkString<_32> username, int playerModelId)
        {
            Username = username;
            PlayerModelId = playerModelId;

            Debug.Log($"[RoomPlayer] {Username} joined the lobby");
        }

        // Call from LobbyUI
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
        public void RPC_ChangeReadyState(NetworkBool state)
        {
            Debug.Log($"Setting {Object.Name} ready state to {state}");
            IsReady = state;

        }

        /*
        private void OnDestroy()
        {
            Debug.Log("[RoomPlayer] OnDestroy called");
            PlayerLeft?.Invoke(this);
            Players.Remove(this);
        }
        */

        private void OnDisable()
        {
            Debug.Log("[RoomPlayer] OnDestroy called");

            // OnDestroy does not get called for pooled objects
            PlayerLeft?.Invoke(this);
            Players.Remove(this);
        }

        private static void OnStateChanged(Changed<RoomPlayer> changed) => PlayerChanged?.Invoke(changed.Behaviour);

        public static void RemovePlayer(NetworkRunner runner, PlayerRef p)
        {
            var roomPlayer = Players.FirstOrDefault(x => x.Object.InputAuthority == p);
            if (roomPlayer != null)
            {
                //if (roomPlayer.Kart != null)
                //    runner.Despawn(roomPlayer.Kart.Object);

                Debug.Log($"[RoomPlayer] Removing {roomPlayer.Username} from list");
                Players.Remove(roomPlayer);

                Debug.Log($"[RoomPlayer] Despawning and destroying RoomPlayer object");
                runner.Despawn(roomPlayer.Object);
            }
        }
    }
}
