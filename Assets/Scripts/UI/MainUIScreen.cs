namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    public class MainUIScreen : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("[MainUIScreen] Awake called");
            UIScreen.Focus(GetComponent<UIScreen>());
        }
    }
}
