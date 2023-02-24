namespace Fusion.Collywobbles.Futsal
{
    using TMPro;
    using UnityEngine;

    public class WorldUINickname : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private Vector3 _offset;

        private Transform _target;

        private PlayerEntity _player;

        public void SetPlayer(PlayerEntity player)
        {
            _player = player;
            _target = player.transform;
            _offset = new Vector3(0, 180.0f, 0);

            //var lobbyUser = _player.Controller.Ro
        }

        private void LateUpdate()
        {
            if (_target)
            {
                transform.position = Camera.main.WorldToScreenPoint(_target.position) + _offset;
                //transform.position = _target.position + _offset;
                //transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
