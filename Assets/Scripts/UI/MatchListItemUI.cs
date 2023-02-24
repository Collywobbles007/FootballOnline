namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;

    public class MatchListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _matchTypeText;
        [SerializeField] private TextMeshProUGUI _matchSizeText;
        [SerializeField] private TextMeshProUGUI _matchQueueText;
        [SerializeField] private TextMeshProUGUI _maxPlayersText;
        [SerializeField] public Button _joinButton;


        public void SetMatch(string sessionName, string type, string size, int queue, int maxPlayers, int pitchId)
        {
            int maxPlayersWithGK = maxPlayers + 2;
            int currentQueueWithGK = queue + 2;

            _matchTypeText.text = type;
            _matchSizeText.text = size;
            _matchQueueText.text = currentQueueWithGK.ToString();
            _maxPlayersText.text = maxPlayersWithGK.ToString();
        }
    }
}
