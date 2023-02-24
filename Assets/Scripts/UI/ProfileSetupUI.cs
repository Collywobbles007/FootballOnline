namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class ProfileSetupUI : MonoBehaviour
    {
        public TMP_InputField nicknameInput;
        public Button confirmButton;

        private void Start()
        {
            nicknameInput.onValueChanged.AddListener(x => ClientInfo.Username = x);
            nicknameInput.onValueChanged.AddListener(x =>
            {
                // disallows empty usernames to be input
                confirmButton.interactable = !string.IsNullOrEmpty(x);
            });

            nicknameInput.text = ClientInfo.Username;

            // Hard-coded for now. Allow player to choose player model in future!
            ClientInfo.PlayerModelId = 0;
        }

        public void AssertProfileSetup()
        {
            if (string.IsNullOrEmpty(ClientInfo.Username))
                UIScreen.Focus(GetComponent<UIScreen>());
        }
    }
}
