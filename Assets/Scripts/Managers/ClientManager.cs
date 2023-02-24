using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using Fusion.Collywobbles.Futsal;

namespace Fusion.Sample.DedicatedServer
{

    public class ClientManager : MonoBehaviour, INetworkRunnerCallbacks
    {

        [SerializeField] private NetworkRunner _runnerPrefab;

        private string _lobbyName;
        private NetworkRunner _instanceRunner;

        public delegate void SessionListUpdated(List<SessionInfo> currentSessionList);
        public static SessionListUpdated onSessionListUpdated;


        #region Unity Callbacks

        void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            Debug.Log("[ClientManager] Joining server lobby");
            State_JoinLobby();
        }

        #endregion


        #region Public Methods

        public void JoinMatch(string sessionName)
        {
            Debug.Log("[ClientManager] Joining match lobby");

            //_instanceRunner = GetRunner("Client");

            StartSimulation(_instanceRunner, GameMode.Client, sessionName);
        }

        public void LeaveSession()
        {
            Debug.Log("[ClientManager] Leaving match lobby");

            ShutdownClient();

            //RoomPlayer.RemovePlayer(_instanceRunner, player);

            //_instanceRunner = GetRunner("Client");
            //_instanceRunner.Shutdown();
        }

        async void ShutdownClient()
        {
            Debug.Log("[ClientManager] Shutting down client network runner");

            await _instanceRunner.Shutdown();

            Destroy(_instanceRunner.gameObject);
            _instanceRunner = null;

            Debug.Log("[ClientManager] Shutdown complete.");

            Debug.Log("[ClientManager] Rejoining server lobby...");

            State_JoinLobby();
        }

        #endregion


        #region Private Methods

        private NetworkRunner GetRunner(string name)
        {

            var runner = Instantiate(_runnerPrefab);
            runner.name = name;
            runner.ProvideInput = true;
            runner.AddCallbacks(this);

            return runner;
        }

        private Task<StartGameResult> StartSimulation(NetworkRunner runner, GameMode gameMode, string sessionName)
        {
            return runner.StartGame(new StartGameArgs()
            {
                GameMode = gameMode,
                SessionName = sessionName,
                SceneManager = runner.gameObject.AddComponent<LevelManager>(),
                DisableClientSessionCreation = true,
            });
        }

        async void State_JoinLobby()
        {
            if (_instanceRunner == null)
            {
                _instanceRunner = GetRunner("Client");

                DontDestroyOnLoad(_instanceRunner.gameObject);
            }

            var result = await JoinLobby(_instanceRunner);

            if (result.Ok == false)
            {
                Debug.LogWarning(result.ShutdownReason);
            }
            else
            {
                Debug.Log("Done");
            }
        }

        private Task<StartGameResult> JoinLobby(NetworkRunner runner)
        {
            return runner.JoinSessionLobby(string.IsNullOrEmpty(_lobbyName) ? SessionLobby.ClientServer : SessionLobby.Custom, _lobbyName);
        }




        #endregion


        #region NetworkRunner Callbacks

        // ------------ RUNNER CALLBACKS ------------------------------------------------------------------------------------

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            // Reload scene after shutdown

            if (Application.isPlaying)
            {
                SceneManager.LoadScene((byte)SceneDefs.LOBBY);
            }
        }

        public void OnDisconnectedFromServer(NetworkRunner runner)
        {
            runner.Shutdown();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            runner.Shutdown();
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

            Log.Debug($"Received: {sessionList.Count}");

            // Let all delegates know that session list has been updated
            onSessionListUpdated?.Invoke(sessionList);
        }

        // Other callbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        #endregion
    }
}
