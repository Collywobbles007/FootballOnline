namespace Fusion.Collywobbles.Futsal
{
    using System.Collections.Generic;
    using UnityEngine;
    using Fusion.Sockets;
    using System;
    using UnityEngine.SceneManagement;
    using UnityEngine.TextCore.Text;

    //using static UnityEditor.PlayerSettings;

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        LobbyJoined,
        Failed,
        Connected
    }

    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class ServerManager : SimulationBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunner _runner;

        private string _sessionProps;
        //private string _sessionName;
        private int _playerCount;
        private int _maxPlayers;

        // *********************************************************
        // Move spawning these to a pitch network script?
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _ballPrefab;
        [SerializeField] private NetworkObject _blueKeeperPrefab;
        [SerializeField] private NetworkObject _redKeeperPrefab;
        // *********************************************************

        [SerializeField] private NetworkObject _gameManagerPrefab;
        [SerializeField] private RoomPlayer _roomPlayerPrefab;
        //[SerializeField] private NetworkObject _chatManager;

        private NetworkObject _gameManager;

        private readonly Dictionary<PlayerRef, NetworkObject> _playerMap = new Dictionary<PlayerRef, NetworkObject>();
        //private readonly Dictionary<PlayerRef, NetworkObject> _playersInRoom = new Dictionary<PlayerRef, NetworkObject>();

        public static ConnectionStatus ConnectionStatus = ConnectionStatus.Disconnected;

        private void Awake()
        {
            _runner = GetComponent<NetworkRunner>();

            //PlayerEntity.OnPlayerDespawned += DespawnPlayer;
        }

        //public void DespawnPlayer(PlayerEntity playerEntity)
        //{
        //    _runner.Despawn(playerEntity.GetComponent<NetworkObject>());
        //}

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Log.Info($"Player {player} entered the match lobby!");

            // Spawn the game manager if not yet done
            if (_gameManager == null)
                _gameManager = runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);

            // Spawn a RoomPlayer here...
            var roomPlayer = runner.Spawn(_roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);

            //_playersInRoom[player] = roomPlayer.GetComponent<NetworkObject>();

            roomPlayer.GameState = RoomPlayer.EGameState.Lobby;
            roomPlayer.PlayerRef = player;
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.CurrentScene > (int)SceneDefs.LOBBY)
            {
                Log.Info($"Player {player} has left the match");
                Log.Info("Requesting despawn...");

                GameManager.CurrentPitch.DespawnPlayer(runner, player);

                Log.Info("Despawn complete");

            }
            else
            {
                Log.Info($"Player {player} left the match lobby!");

                RoomPlayer.RemovePlayer(runner, player);
            }
        }


        public void OnConnectedToServer(NetworkRunner runner) 
        {

        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (runner.CurrentScene > (int)SceneDefs.LOBBY)
            {
                ServerInfo.LobbyName = runner.SessionInfo.Name;

                Debug.Log($"[Server] Session Name = " + runner.SessionInfo.Name);
                Debug.Log($"[Server] Loaded scene {runner.CurrentScene}. Spawning ball and goalkeepers...");

                // Spawn ball and goalkeepers
                runner.Spawn(_ballPrefab, Vector3.zero, Quaternion.identity);

                runner.Spawn(_blueKeeperPrefab, Vector3.zero, Quaternion.identity);
                runner.Spawn(_redKeeperPrefab, Vector3.zero, Quaternion.identity);

                //runner.Spawn(_chatManager, Vector3.zero, Quaternion.identity);

                //var pos = UnityEngine.Random.insideUnitSphere * 3;
                //pos.y = 1;


                foreach (var player in RoomPlayer.Players)
                {
                    //player.GameState = RoomPL
                    GameManager.CurrentPitch.SpawnPlayer(Runner, player);
                }
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"OnShutdown {shutdownReason}");
            SetConnectionStatus(ConnectionStatus.Disconnected);

            (string status, string message) = ShutdownReasonToHuman(shutdownReason);

            Debug.Log($"Disconnect Status: {status}");
            Debug.Log($"Disconnect Message: {message}");

            RoomPlayer.Players.Clear();

            if (_runner)
            {
                //_runner.Shutdown();
                Destroy(_runner.gameObject);
            }

            // Reset the object pools
            //_pool.ClearPools();
            //_pool = null;

            _runner = null;

            // Quit application after the Server Shutdown
            Application.Quit(0);
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }


        private void SetConnectionStatus(ConnectionStatus status)
        {
            Debug.Log($"Setting connection status to {status}");

            ConnectionStatus = status;

            if (!Application.isPlaying)
                return;

            if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Failed)
            {
                SceneManager.LoadScene((int)SceneDefs.LOBBY);
                UIScreen.BackToInitial();
            }
        }

        private static (string, string) ShutdownReasonToHuman(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.Ok:
                    return (null, null);
                case ShutdownReason.Error:
                    return ("Error", "Shutdown was caused by some internal error");
                case ShutdownReason.IncompatibleConfiguration:
                    return ("Incompatible Config", "Mismatching type between client Server Mode and Shared Mode");
                case ShutdownReason.ServerInRoom:
                    return ("Room name in use", "There's a room with that name! Please try a different name or wait a while.");
                case ShutdownReason.DisconnectedByPluginLogic:
                    return ("Disconnected By Plugin Logic", "You were kicked, the room may have been closed");
                case ShutdownReason.GameClosed:
                    return ("Game Closed", "The session cannot be joined, the game is closed");
                case ShutdownReason.GameNotFound:
                    return ("Game Not Found", "This room does not exist");
                case ShutdownReason.MaxCcuReached:
                    return ("Max Players", "The Max CCU has been reached, please try again later");
                case ShutdownReason.InvalidRegion:
                    return ("Invalid Region", "The currently selected region is invalid");
                case ShutdownReason.GameIdAlreadyExists:
                    return ("ID already exists", "A room with this name has already been created");
                case ShutdownReason.GameIsFull:
                    return ("Game is full", "This lobby is full!");
                case ShutdownReason.InvalidAuthentication:
                    return ("Invalid Authentication", "The Authentication values are invalid");
                case ShutdownReason.CustomAuthenticationFailed:
                    return ("Authentication Failed", "Custom authentication has failed");
                case ShutdownReason.AuthenticationTicketExpired:
                    return ("Authentication Expired", "The authentication ticket has expired");
                case ShutdownReason.PhotonCloudTimeout:
                    return ("Cloud Timeout", "Connection with the Photon Cloud has timed out");
                default:
                    Debug.LogWarning($"Unknown ShutdownReason {reason}");
                    return ("Unknown Shutdown Reason", $"{(int)reason}");
            }
        }

        private static (string, string) ConnectFailedReasonToHuman(NetConnectFailedReason reason)
        {
            switch (reason)
            {
                case NetConnectFailedReason.Timeout:
                    return ("Timed Out", "");
                case NetConnectFailedReason.ServerRefused:
                    return ("Connection Refused", "The lobby may be currently in-game");
                case NetConnectFailedReason.ServerFull:
                    return ("Server Full", "");
                default:
                    Debug.LogWarning($"Unknown NetConnectFailedReason {reason}");
                    return ("Unknown Connection Failure", $"{(int)reason}");
            }
        }
    }
}