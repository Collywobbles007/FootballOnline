namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class LobbyItemUI : MonoBehaviour
    {
        public TextMeshProUGUI username;
        public Image ready;

        private RoomPlayer _player;

        public void SetPlayer(RoomPlayer player)
        {
            _player = player;
        }

        private void Update()
        {
            if (_player.Object != null && _player.Object.IsValid)
            {
                username.text = _player.Username.Value;
                ready.gameObject.SetActive(_player.IsReady);
            }
        }
    }
}
