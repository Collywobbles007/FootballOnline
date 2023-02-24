namespace Fusion.Collywobbles.Futsal
{
    using Fusion.Sample.DedicatedServer;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class InGameMenu : SimulationBehaviour
    {
        private bool _guiEnabled = false;
        public ClientManager _clientManager;

        public void EnableGUI()
        {
            _guiEnabled = true;
        }

        private void OnGUI()
        {

            if (Runner == null || !_guiEnabled)
            {
                return;
            }

            //Rect area = new Rect(10, 90, Screen.width - 20, Screen.height - 100);
            Rect area = new Rect(20, 10, Screen.width - 100, 50);

            GUILayout.BeginArea(area);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Quit", GUILayout.ExpandWidth(false), GUILayout.MinHeight(30), GUILayout.MinWidth(100)))
                {
                    //Runner.Shutdown();

                    //SceneManager.LoadScene((byte)SceneDefs.LOBBY);
                    _clientManager.LeaveSession();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}