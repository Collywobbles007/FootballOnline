namespace Fusion.Collywobbles.Futsal
{
    using Fusion.Collywobbles.Futsal;
    using Fusion.Sample.DedicatedServer;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class InterfaceManager : MonoBehaviour
    {
        [SerializeField] private ProfileSetupUI profileSetup;

        public ClientManager _clientManager;
        public UIScreen mainMenu;
        public UIScreen quitMenu;

        public static InterfaceManager Instance => Singleton<InterfaceManager>.Instance;

        private void Start()
        {
            profileSetup.AssertProfileSetup();
        }


        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public void OpenQuitMatchMenu()
        {
            if (UIScreen.activeScreen != quitMenu)
            {
                UIScreen.Focus(quitMenu);
            }
        }

        public void QuitMatch()
        {
            //Debug.Log("[InterfaceManager] Telling Client Manager to leave the session...");
            _clientManager.LeaveSession();

            UIScreen.Focus(mainMenu);
        }
    }
}
