namespace Fusion.Collywobbles.Futsal
{
    using Fusion;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using UnityEngine;

    public class Pitch : NetworkBehaviour
    {
        //[SerializeField] private NetworkObject _playerPrefab;

        private readonly Dictionary<PlayerRef, PlayerEntity> _playersOnPitch = new Dictionary<PlayerRef, PlayerEntity>();

        public static Pitch Current { get; private set; }

        public Transform[] spawnpoints;

        //PlayerEntity.OnPlayerSpawned += DespawnPlayer;

        private void Awake()
        {
            Current = this;

            GameManager.SetPitch(this);

        }

        public override void Spawned()
        {
            Debug.Log("[Pitch] Spawning pitch...");

            base.Spawned();

            //PlayerEntity.OnPlayerDespawned += DespawnPlayer2;
        }

        public void SpawnPlayer(NetworkRunner runner, RoomPlayer roomPlayer)
        {
            Debug.Log($"[Pitch] Spawning player: {roomPlayer.Username} (playerRef = {roomPlayer.PlayerRef})");

            int index = RoomPlayer.Players.IndexOf(roomPlayer);
            Transform point = spawnpoints[index];

            // Reference to the chosen player model - always 0 for now until new models are added...
            int prefabId = roomPlayer.PlayerModelId;

            // Load the prefab for this model id
            PlayerEntity prefab = ResourceManager.Instance.playerDefinitions[prefabId].prefab;

            // Spawn the player using the prefab
            PlayerEntity character = runner.Spawn(prefab, point.position, point.rotation, inputAuthority: roomPlayer.PlayerRef);

            Debug.Log($"[Pitch] Player {roomPlayer.Username} spawned!");
            Debug.Log($"[Pitch] Setting Room User...");

            character.SetNickname(roomPlayer.Username.Value);
            character.transform.name = roomPlayer.Username.Value;

            roomPlayer.Player = character.Controller;

            // Save player in list
            _playersOnPitch[roomPlayer.PlayerRef] = character;
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_playersOnPitch.TryGetValue(player, out var playerEntity))
            {
                if (!playerEntity.IsDestroyed())
                    runner.Despawn(playerEntity.GetComponent<NetworkObject>());
            }
        }
    }
}
