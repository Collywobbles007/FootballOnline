namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GameUI : MonoBehaviour
    {
        public PlayerEntity Player { get; private set; }

        private ThirdPersonPlayer playerController => Player.Controller;

        public void Init(PlayerEntity player)
        {
            Player = player;

            // Initialise game here...
        }

        private void Update()
        {
            // Update on-screen graphics here...
        }

        public void QuitGame()
        {
            InterfaceManager.Instance.QuitMatch();
        }
    }
}
