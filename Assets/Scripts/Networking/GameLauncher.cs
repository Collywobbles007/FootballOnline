namespace Fusion.Collywobbles.Futsal
{
    using System.Collections.Generic;
    using UnityEngine;
    using System.Threading.Tasks;
    using Fusion.Sockets;
    using System;
    using UnityEngine.SceneManagement;

    
    public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        //[SerializeField] private GameObject _matchListItemPrefab;
        //[SerializeField] private GameObject _matchListHodler;

        public static ConnectionStatus ConnectionStatus = ConnectionStatus.Disconnected;

        public delegate void SessionListUpdated(List<SessionInfo> currentSessionList);
        public static SessionListUpdated onSessionListUpdated;

        private string _sessionProps;
        private int _playerCount;
        private int _maxPlayers;
        //private string _sessionName;
        private string _lobbyName;

        private LevelManager _levelManager;
        private NetworkRunner _runner;

        //private State _currentState;
        //private List<SessionInfo> _currentSessionList;

        

        void Awake()
        {
            Application.targetFrameRate = 60;

            // Set a delegate function - not currently used here (see MatchScreenUI)
            //onSessionListUpdated += State_LobbyJoined;
        }

        void Start()
        {
            _levelManager = gameObject.GetComponent<LevelManager>();

            DontDestroyOnLoad(gameObject);         
        }


        // Start client session for selected match
        public void JoinMatch(string sessionName)
        {

            Debug.Log($"Joining match {sessionName}...");

            SetConnectionStatus(ConnectionStatus.Connecting);

            StartClientSession(sessionName);
        }

        // Join main lobby to view sessions (match servers) available
        public void JoinLobby()
        {
            State_JoinLobby();
        }

        
        // Leave main lobby
        public void LeaveLobby()
        {
            if (_runner != null)
                _runner.Shutdown();
            else
                SetConnectionStatus(ConnectionStatus.Disconnected);
        }
        

        // Join the selected match session (match server)
        async void StartClientSession(string sessionName)
        {
            // Get client Network Runner
            _runner = GetRunner("Client");

            //_runner.SetActiveScene(1);

            Debug.Log("[GameLauncher] Starting client session...");

            // Start client session
            var result = await StartSimulation(_runner, GameMode.Client, sessionName);

            if (result.Ok == false)
            {
                Debug.LogWarning(result.ShutdownReason);
            }
            else
            {
                Debug.Log("Done");
            }
        }

        async void State_JoinLobby()
        {
            _runner = GetRunner("Client");

            //_currentState = State.LobbyJoined;
            SetConnectionStatus(ConnectionStatus.LobbyJoined);

            var result = await JoinLobby(_runner);

            if (result.Ok == false)
            {
                Debug.LogWarning(result.ShutdownReason);

                //_currentState = State.SelectMode;
            }
            else
            {
                Debug.Log("Done");
            }
        }

        // Delegate called when lobby session list is updated
        // Not currently used
        void State_LobbyJoined(List<SessionInfo> currentSession)
        {
            if (currentSession != null && currentSession.Count > 0)
            {
                foreach (var session in currentSession.ToArray())
                {
                    //var props = "";
                    foreach (var item in session.Properties)
                    {
                        _sessionProps += $"{item.Key}={item.Value.PropertyValue}, ";

                        Debug.Log($"[GameLauncher] Session: {session.Name} ({_sessionProps})");
;                    }

                    //_sessionName = session.Name;
                    //_playerCount = session.PlayerCount - 1;
                    //_maxPlayers = session.MaxPlayers - 1;

                    Debug.Log($"[GameLauncher] Player Count: {_playerCount}, Max Players {_maxPlayers}");

                    //_population.text = _playerCount.ToString();
                    //_maxPopulation.text = _maxPlayers.ToString();
                }
            }
        }

        
        private NetworkRunner GetRunner(string name)
        {

            var runner = Instantiate(_runnerPrefab);
            runner.name = name;
            runner.ProvideInput = true;
            runner.AddCallbacks(this);

            return runner;
        }

        private void SetConnectionStatus(ConnectionStatus status)
        {
            Debug.Log($"Setting connection status to {status}");

            ConnectionStatus = status;

            if (!Application.isPlaying)
                return;

            if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Failed)
            {
                //SceneManager.LoadScene((int)SceneDefs.LOBBY);
                //UIScreen.BackToInitial();
            }
        }

        /*
        private NetworkRunner GetRunner(string name)
        {
            GameObject go = new GameObject("Session");

            NetworkRunner runner = go.AddComponent<NetworkRunner>();

            runner.name = name;
            runner.ProvideInput = true;
            runner.AddCallbacks(this);
            runner.AddComponent<ServerEventsInfo>();

            return runner;
        }
        */

        public Task<StartGameResult> StartSimulation(NetworkRunner runner, GameMode gameMode, string sessionName)
        {
            return runner.StartGame(new StartGameArgs()
            {
                SessionName = sessionName,
                GameMode = gameMode,
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                //SceneManager = _levelManager,
                //Scene = SceneManager.GetActiveScene().buildIndex,
                Scene = SceneManager.GetSceneByName("GAME").buildIndex,
                DisableClientSessionCreation = true,
            }) ;
        }

        public Task<StartGameResult> JoinLobby(NetworkRunner runner)
        {
            return runner.JoinSessionLobby(string.IsNullOrEmpty(_lobbyName) ? SessionLobby.ClientServer : SessionLobby.Custom, _lobbyName);
        }

        // ------------ RUNNER CALLBACKS ------------------------------------------------------------------------------------

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {

            //_currentSessionList = null;
            //_currentState = State.SelectMode;

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
            Debug.Log($"Connect failed {reason}");
            LeaveLobby();
            SetConnectionStatus(ConnectionStatus.Failed);
            //(string status, string message) = ConnectFailedReasonToHuman(reason);
            //_disconnectUI.ShowMessage(status, message);
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

            Log.Debug($"Received: {sessionList.Count}");

            //_currentSessionList = sessionList;

            // Let all delegates know that session list has been updated
            onSessionListUpdated?.Invoke(sessionList);
        }

        // Other callbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnConnectedToServer(NetworkRunner runner) 
        {
            Debug.Log("Connected to server");
            SetConnectionStatus(ConnectionStatus.Connected);
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) 
        {
            if (runner.CurrentScene > 0)
            {
                Debug.LogWarning($"Refused connection requested by {request.RemoteAddress}");
                request.Refuse();
            }
            else
                request.Accept();
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    }
}
