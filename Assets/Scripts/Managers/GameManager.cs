namespace Fusion.Collywobbles.Futsal
{
    using System;
    using System.Linq;
    using UnityEngine;

    public class GameManager : NetworkBehaviour
    {
        public static event Action<GameManager> OnLobbyDetailsUpdated;

        public static GameManager Instance { get; set; }

        public static Pitch CurrentPitch { get; private set; }

        public string PitchName => ResourceManager.Instance.pitches[PitchId].pitchName;
        public string MatchSizeName => ResourceManager.Instance.matchTypes[MatchTypeId].matchSizeName;
        public string MatchLengthName => ResourceManager.Instance.matchTypes[MatchTypeId].matchLengthUnit;
        public string MatchTypeName => ResourceManager.Instance.matchTypes[MatchTypeId].matchTypeName;

        [Networked(OnChanged = nameof(OnLobbyDetailsChangedCallback))] public NetworkString<_32> LobbyName { get; set; }
        [Networked(OnChanged = nameof(OnLobbyDetailsChangedCallback))] public int PitchId { get; set; }
        [Networked(OnChanged = nameof(OnLobbyDetailsChangedCallback))] public int MatchTypeId { get; set; }
        [Networked(OnChanged = nameof(OnLobbyDetailsChangedCallback))] public int MaxUsers { get; set; }

        public static bool IsAllReady() => RoomPlayer.Players.Count > 0 && RoomPlayer.Players.All(player => player.IsReady);

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void Spawned()
        {
            base.Spawned();

            Debug.Log("[GameManager] Server game manager has spawned into the lobby!");
            Debug.Log("[GameManager] Waiting for players to join the match...");

            if (Object.HasStateAuthority)
            {
                LobbyName = ServerInfo.LobbyName;
                PitchId = ServerInfo.PitchId;
                MatchTypeId = ServerInfo.GameMode;
                MaxUsers = ServerInfo.MaxUsers;
            }
        }

        public void StartMatch()
        {
            LevelManager.LoadMatch(2);
        }

        public static void SetPitch(Pitch pitch)
        {
            CurrentPitch = pitch;
        }


        private static void OnLobbyDetailsChangedCallback(Changed<GameManager> changed)
        {
            OnLobbyDetailsUpdated?.Invoke(changed.Behaviour);
        }
    }
}