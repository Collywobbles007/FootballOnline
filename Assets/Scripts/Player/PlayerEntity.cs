namespace Fusion.Collywobbles.Futsal
{
    using Fusion;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class PlayerEntity : NetworkBehaviour
    {
        public static event Action<PlayerEntity> OnPlayerSpawned;
        public static event Action<PlayerEntity> OnPlayerDespawned;
        public static event Action<PlayerEntity> OnNicknameUpdated;


        public SimplePlayerAnimator Animator { get; private set; }
        public ThirdPersonPlayer Controller { get; private set; }
        public PlayerInput Input { get; private set; }
        public GameUI Hud { get; private set; }
        public NetworkRigidbody RigidBody { get; private set; }


        [Networked] public NetworkString<_32> Nickname { get; set; }

        public static readonly List<PlayerEntity> Players = new List<PlayerEntity>();

        private bool _despawned;

        private void Awake()
        {
            Animator = GetComponentInChildren<SimplePlayerAnimator>();
            Controller = GetComponent<ThirdPersonPlayer>();
            Input = GetComponent<PlayerInput>();
            //RigidBody = GetComponent<NetworkRigidbody>();
            RigidBody = GetComponent<KCC.KCC>().GetComponent<NetworkRigidbody>();
        }



        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasInputAuthority)
            {
                //Hud = Instantiate(ResourceManager.Instance.hudPrefab);
                //Hud.Init(this);

            }

            Players.Add(this);
            OnPlayerSpawned?.Invoke(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            Players.Remove(this);

            _despawned = true;
            //OnPlayerDespawned?.Invoke(this);

            // If all players have left the match, request match server shutdown
            if (Players.Count == 0)
                runner.Shutdown();
        }

        public void SetNickname(string nickname)
        {
            Nickname = nickname;

            Debug.Log($"[PlayerEntity] Setting nickname to {Nickname.Value}");

            OnNicknameUpdated?.Invoke(this);
        }

        private void OnDestroy()
        {
            Players.Remove(this);

            if (!_despawned)
            {
                //OnPlayerDespawned?.Invoke(this);
            }
        }
    }

}
