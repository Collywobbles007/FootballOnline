using UnityEngine;
using System.Threading.Tasks;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Fusion.Collywobbles.Futsal
{

    public class ServerLauncher : MonoBehaviour
    {

        /// <summary>
        /// Network Runner Prefab used to Spawn a new Runner used by the Server
        /// </summary>
        [SerializeField] private NetworkRunner _runnerPrefab;

        private NetworkRunner _runner;
        //private LevelManagerTest _levelManager;

        async void Start()
        {
            // Load Menu Scene if not Running in Headless Mode
            // This can be replaced with a check to UNITY_SERVER if running on Unity 2021.2+
            if (CommandLineUtils.IsHeadlessMode() == false)
            {

                SceneManager.LoadScene((int)SceneDefs.LOBBY, LoadSceneMode.Single);
                return;
            }


            // Continue with start the Dedicated Server
            Application.targetFrameRate = 30;

            // Session Name
            CommandLineUtils.TryGetArg("-session", out string sessionName);

            // Session Size
            CommandLineUtils.TryGetArg("-matchSize", out string sessionSize);
            if (ushort.TryParse(sessionSize, out var matchSize) == false)
            {
                matchSize = 4;
            }

            // Custom Region
            CommandLineUtils.TryGetArg("-region", out string customRegion);

            // Server Lobby
            CommandLineUtils.TryGetArg("-lobby", out string customLobby);

            // Server Port
            CommandLineUtils.TryGetArg("-port", out string customPort);
            if (ushort.TryParse(customPort, out var port) == false)
            {
                port = 27015;
            }

            // Server Properties
            var argsCustomProps = CommandLineUtils.GetArgumentList("-P");

            var customServerProperties = new Dictionary<string, SessionProperty>();
            var outputProps = string.Empty;

            foreach (var item in argsCustomProps)
            {
                var key = item.Item1;
                var value = item.Item2;

                outputProps += $"{key}={value}, ";

                customServerProperties.Add(key, value);
            }


            Log.Debug($"Starting Server: {nameof(sessionName)}={sessionName}, {nameof(matchSize)}={matchSize}, {nameof(customRegion)}={customRegion}, {nameof(customLobby)}={customLobby}, {nameof(port)}={port}, {nameof(customServerProperties)}={outputProps}");

            // Start the Server
            var result = await StartSimulation(sessionName, matchSize, customServerProperties, port, customLobby, customRegion);

            // Check if all went fine
            if (result.Ok)
            {
                Log.Debug($"Runner Start DONE");
            }
            else
            {
                // Quit the application if startup fails

                Log.Debug($"Error while starting Server: {result.ShutdownReason}");

                // it can be used any error code that can be read by an external application
                // using 0 means all went fine
                Application.Quit(1);
            }
        }

        public Task<StartGameResult> StartSimulation(string SessionName, int MatchSize, Dictionary<string, SessionProperty> customProps, ushort port, string customLobby, string customRegion)
        {
            var photonSettings = Photon.Realtime.PhotonAppSettings.Instance.AppSettings.GetCopy();

            if (string.IsNullOrEmpty(customRegion) == false)
            {
                photonSettings.FixedRegion = customRegion.ToLower();
            }
            
            var runner = Instantiate(_runnerPrefab);

            runner.ProvideInput = false;

            return runner.StartGame(new StartGameArgs()
            {
                SessionName = SessionName,
                GameMode = GameMode.Server,
                SceneManager = runner.gameObject.AddComponent<LevelManager>(),
                Scene = (int)SceneDefs.LOBBY,
                SessionProperties = customProps,
                Address = NetAddress.Any(port),
                CustomLobbyName = customLobby,
                CustomPhotonAppSettings = photonSettings,
                PlayerCount = MatchSize
            });
            
        }
    }
}