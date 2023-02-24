namespace Fusion.Collywobbles.Futsal
{
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;

    public class NicknameUI : MonoBehaviour
    {
        //public WorldUINickname nicknamePrefab;
        public WorldNickname3D nicknamePrefab3D;

        private readonly Dictionary<PlayerEntity, WorldNickname3D> _playerNicknames = new Dictionary<PlayerEntity, WorldNickname3D>();

        private void Awake()
        {
            // Add any player names that spawned before we did
            EnsureAllTexts();

            // Subscribe to new players arriving
            PlayerEntity.OnPlayerSpawned += SpawnNicknameText;
            PlayerEntity.OnPlayerDespawned += DespawnNicknameText;
            PlayerEntity.OnNicknameUpdated += SpawnNicknameText;
        }

        private void OnDestroy()
        {
            PlayerEntity.OnPlayerSpawned -= SpawnNicknameText;
            PlayerEntity.OnPlayerDespawned -= DespawnNicknameText;
            PlayerEntity.OnNicknameUpdated -= SpawnNicknameText;
        }

        private void EnsureAllTexts()
        {
            // we need to make sure that any karts that spawned before the callback was subscribed, are registered
            var players = PlayerEntity.Players;

            foreach (var player in players)
            {
                if (!_playerNicknames.ContainsKey(player))
                    SpawnNicknameText(player);
            }
        }

        private void SpawnNicknameText(PlayerEntity player)
        {
            if (nicknamePrefab3D == null) return;

            if (!_playerNicknames.ContainsKey(player))
            {
                var obj = Instantiate(nicknamePrefab3D, transform);
                _playerNicknames.Add(player, obj);
                obj.SetPlayer(player);
            }
            else
            {
                _playerNicknames[player].SetName(player);
            }

            
        }

        private void DespawnNicknameText(PlayerEntity player)
        {
            if (!_playerNicknames.ContainsKey(player))
                return;

            var text = _playerNicknames[player];
            Destroy(text.gameObject);

            _playerNicknames.Remove(player);
        }

    }
}
