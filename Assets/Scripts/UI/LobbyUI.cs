namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    using System.Linq;

    public class LobbyUI : MonoBehaviour, IDisabledUI
    {
        [SerializeField] private UIScreen _dummyScreen;
        [SerializeField] private CanvasFader fader;

        public GameObject playerUITextPrefab;
        public Transform parent;
        public Button readyUp;
        //public Button customizeButton;
        //public TextMeshProUGUI matchStyleText;
        public TextMeshProUGUI matchSizeText;
        public TextMeshProUGUI matchLengthText;
        //public TextMeshProUGUI matchTypeText;
        public TextMeshProUGUI lobbyNameText;
        public Image pitchIconImage;

        private static readonly Dictionary<RoomPlayer, LobbyItemUI> ListItems = new Dictionary<RoomPlayer, LobbyItemUI>();
        private static bool IsSubscribed;


        private void Awake()
        {
            GameManager.OnLobbyDetailsUpdated += UpdateDetails;

            RoomPlayer.PlayerChanged += (player) =>
            {
                //var isLeader = RoomPlayer.Local.IsLeader;
                //trackNameDropdown.interactable = isLeader;
                //gameTypeDropdown.interactable = isLeader;
                //customizeButton.interactable = !RoomPlayer.Local.IsReady;
            };
        }

        private void Start()
        {
            lobbyNameText.text = ServerInfo.LobbyName;
            
            switch (ServerInfo.MaxUsers)
            {
                case 4:
                    matchSizeText.text = "3-a-side";
                    matchLengthText.text = "10 Minutes";
                    break;
                case 8:
                    matchSizeText.text = "5-a-side";
                    matchLengthText.text = "20 Minutes";
                    break;
                case 12:
                    matchSizeText.text = "7-a-side";
                    matchLengthText.text = "20 Minutes";
                    break;
                case 20:
                    matchSizeText.text = "11-a-side";
                    matchLengthText.text = "30 Minutes";
                    break;
                default:
                    matchSizeText.text = "Undefined";
                    matchLengthText.text = "Undefined";
                    break;
            }
        }


        #region DisabledUI Interface methods

        // Called when Main Canvas Awake method executes
        // Allows an 'Awake' to occur on a disabled component
        public void Setup()
        {
            if (IsSubscribed) return;

            // Subscribe to RoomPlayer actions
            RoomPlayer.PlayerJoined += AddPlayer;
            RoomPlayer.PlayerLeft += RemovePlayer;

            RoomPlayer.PlayerChanged += EnsureAllPlayersReady;

            readyUp.onClick.AddListener(ReadyUpListener);

            IsSubscribed = true;
        }

        public void OnDestruction()
        {
            
        }

        #endregion

        private void UpdateDetails(GameManager manager)
        {

        }

        private void OnDestroy()
        {
            if (!IsSubscribed) return;

            RoomPlayer.PlayerJoined -= AddPlayer;
            RoomPlayer.PlayerLeft -= RemovePlayer;

            readyUp.onClick.RemoveListener(ReadyUpListener);

            IsSubscribed = false;
        }

        private void AddPlayer(RoomPlayer player)
        {
            if (ListItems.ContainsKey(player))
            {
                var toRemove = ListItems[player];
                Destroy(toRemove.gameObject);

                ListItems.Remove(player);
            }

            var obj = Instantiate(playerUITextPrefab, parent).GetComponent<LobbyItemUI>();
            obj.SetPlayer(player);

            ListItems.Add(player, obj);

            UpdateDetails(GameManager.Instance);
        }

        private void RemovePlayer(RoomPlayer player)
        {
            if (!ListItems.ContainsKey(player))
                return;

            var obj = ListItems[player];

            if (obj != null)
            {
                Destroy(obj.gameObject);
                ListItems.Remove(player);
            }
        }

        private void ReadyUpListener()
        {
            Debug.Log("[LobbyUI] Ready button pressed!");

            var local = RoomPlayer.Local;

            if (local && local.Object && local.Object.IsValid)
            {
                Debug.Log("[LobbyUI] Calling RPC to change ready state...");
                local.RPC_ChangeReadyState(!local.IsReady);
            }
        }

        private void EnsureAllPlayersReady(RoomPlayer lobbyPlayer)
        {
            Debug.Log("[LobbyUI] Checking if all players are ready...");

            if (GameManager.IsAllReady())
            {
                Debug.Log("[LobbyUI] All players are ready! Showing dummy screen...");

                UIScreen.Focus(_dummyScreen);
                PreLoadScreen();

                Debug.Log("[LobbyUI] Telling GameManager to start match...");
                GameManager.Instance.StartMatch();
                PostLoadScreen();
            }
        }

        private void PreLoadScreen()
        {
            fader.gameObject.SetActive(true);
            fader.FadeIn();
        }

        private void PostLoadScreen()
        {
            fader.FadeOut();
        }
    }
}
