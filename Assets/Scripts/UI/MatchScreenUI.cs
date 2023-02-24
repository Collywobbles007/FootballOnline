namespace Fusion.Collywobbles.Futsal
{
    using Fusion.Sample.DedicatedServer;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using UnityEngine;

    public class MatchScreenUI : MonoBehaviour, IDisabledUI
    {
        [SerializeField] private Transform _matchListHolder;
        [SerializeField] private MatchListItemUI _matchEntryPrefab;
        [SerializeField] private UIScreen _roomScreen;


        private List<MatchListItemUI> _matchList;

        public void ClearSessions()
        {
            Debug.Log($"[MatchScreenUI] Clearing {_matchList.Count} old sessions...");

            // Clear all sessions

            foreach (var match in _matchList)
            {
                if (!match.IsDestroyed())
                {
                    Debug.Log($"[MatchScreenUI] Destroying {match.name}");
                    match.transform.SetParent(null);
                    Destroy(match.gameObject);
                }
                else
                {
                    Debug.Log($"[MatchScreenUI] {match.name} is already destroyed");
                }
            }

            Debug.Log("[MatchScreenUI] Clearing list...");
            _matchList.Clear();
        }

        public void JoinSelectedMatch(string sessionName, string matchType, int maxPlayers, int pitchId)
        {
            Debug.Log("[MatchScreenUI] Clicked session " + sessionName);

            // Set lobby screen title
            ServerInfo.LobbyName = matchType;
            ServerInfo.PitchId = pitchId; // 0 = Futsal (3 & 5-a-side), 1 = 5-a-side Grass, 2 = 7-a-side Grass, 3 = 11-a-side Grass
            ServerInfo.MaxUsers = maxPlayers;

            //UIScreen.Focus()
            //_matchList.Clear();
            ClearSessions();

            // Enter the match lobby screen
            ClientManager clientManager = transform.GetComponentInParent<ClientManager>();
            clientManager.JoinMatch(sessionName);

            UIScreen.Focus(_roomScreen);
        }

        #region DisabledUI Interface methods

        // Called when Main Canvas Awake method executes
        // Allows an 'Awake' to occur on a disabled component
        public void Setup()
        {
            // Assign delagate function to receive lobby session update callbacks
            ClientManager.onSessionListUpdated += UpdateSessionList;

            _matchList = new List<MatchListItemUI>();
        }

        public void OnDestruction()
        {

        }

        #endregion

        // Delegate called when session list updated in GameLauncher
        private void UpdateSessionList(List<SessionInfo> currentSession)
        {
            string sessionNameFull;
            string sessionName;
            string sessionSize;
            int playerCount;
            int maxPlayers;
            int pitchId = 0;

            if (currentSession != null && currentSession.Count > 0)
            {
                Debug.Log($"[MatchScreenUI] Received {currentSession.Count} match sessions");

                ClearSessions();

                Debug.Log("[MatchScreenUI] Creating new list...");

                // Create new list of matches available
                foreach (var session in currentSession.ToArray())
                {
                    /** Session Properties not used
                    string sessionProps = "";
                    foreach (var item in session.Properties)
                    {
                        sessionProps += $"{item.Key}={item.Value.PropertyValue}, ";

                        Debug.Log($"[MatchScreenUI] Session: {session.Name} ({sessionProps})");
                       
                    }
                    */

                    sessionNameFull = session.Name;
                    sessionName = session.Name[..session.Name.IndexOf('_')]; // Expects '_' at end of session name
                    playerCount = session.PlayerCount - 1;
                    maxPlayers = session.MaxPlayers - 1;

                    switch (maxPlayers)
                    {
                        case 4:
                            sessionSize = "3-a-side";
                            pitchId = 0;
                                
                            break;

                        case 8:
                            sessionSize = "5-a-side";
                            pitchId = sessionName.Equals("Futsal") ? 0 : 1;
                            break;

                        case 12:
                            sessionSize = "7-a-side";
                            pitchId = 2;
                            break;

                        case 20:
                            sessionSize = "11-a-side";
                            pitchId = 3;
                            break;

                        default:
                            Debug.LogError($"[MatchScreenUI] Unhandled match size for {maxPlayers} players!");
                            sessionSize = "Unknown";
                            break;
                    }

                    Debug.Log($"[MatchScreenUI] Session Name: {sessionNameFull}");
                    Debug.Log($"[MatchScreenUI] Match Type: {sessionName}");
                    Debug.Log($"[MatchScreenUI] Match Size: {sessionSize}");
                    Debug.Log($"[MatchScreenUI] Players: {playerCount} / {maxPlayers}");

                    // Instantiate the match in the list (add 2 for goalkeepers)
                    MatchListItemUI match = Instantiate(_matchEntryPrefab, _matchListHolder);
                    match.SetMatch(sessionNameFull, sessionName, sessionSize, playerCount, maxPlayers, pitchId);

                    // Assign the button click handler with these parameters
                    match._joinButton.onClick.AddListener(delegate { JoinSelectedMatch(sessionNameFull, sessionName, maxPlayers, pitchId); });

                    _matchList.Add(match);
                }
            }
        }
    }
}
