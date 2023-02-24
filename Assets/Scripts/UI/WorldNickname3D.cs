namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using TMPro;
    using UnityEngine;

    public class WorldNickname3D : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _playerName;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private float standardDistance = 10.0f;

        private Transform _target;
        private PlayerEntity _player;
        private Vector3 startScale;

        public string nickname;



        private void Start()
        {
            startScale = transform.localScale;
        }

        private void Update()
        {
            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
            Vector3 newScale = startScale * (dist / standardDistance);
            transform.localScale = newScale;
        }


        public void SetPlayer(PlayerEntity player)
        {
            _target = player.transform;
            _player = player;
            //_player.OnRoomUserChanged += SetNicknameText;
            //_offset = new Vector3(0, 180.0f, 0);  

            _playerName.SetText(player.Nickname.Value);

            //nickname = "Hello";
        }

        public void SetName(PlayerEntity player)
        {
            _playerName.SetText(player.Nickname.Value);
        }

        /*
        public void SetNicknameText(PlayerEntity player)
        {
            if (player == null)
            {
                Debug.Log("[Nickname] PlayerEntity is null!");
                return;
            }

            Debug.Log($"[WorldNickname3D] Received playerEntity nickname = {player.Nickname.Value}");
            Debug.Log($"[WorldNickname3D] Current name = {_playerName.text} - setting playerName.text = {_player.Nickname.Value}");

            _playerName.SetText(_player.Nickname.Value);
            nickname = _player.Nickname.Value;

            Debug.Log($"[WorldNickname3D] Done. New player nickname = {_playerName.text}");
        }

        */

        private void LateUpdate()
        {
            if (_player)
            {
                transform.position = _target.position + _offset;
                transform.rotation = Camera.main.transform.rotation;
            }
            else
            {
                Debug.Log("This player disconnected, removing name tag...");
                Destroy(gameObject);
            }
        }

        private void WaitAndDestroy()
        {
            //yield return new WaitForSeconds(1);

            Debug.Log("This player disconnected, removing name tag...");
            Destroy(gameObject);          
        }
    }
}
