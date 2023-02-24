namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using Fusion;
    using UnityEngine.UI;

    public class ChatManager : NetworkBehaviour
    {
        public int maxMessages = 25;
        public GameObject chatPanel;
        public GameObject textObject;
        public bool _isFading;
        

        [SerializeField]
        private List<Message> messageList = new List<Message>();

        [SerializeField] private TMP_InputField _chatBoxInput;

        private CanvasGroup _canvasGroup;
        private Coroutine _chatFade;
        private string _username;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.0f;
            _isFading = false;
        }

        public void SetChatUsername(string username)
        {
            _username = username;
        }

        private void ResetAlpha(float alpha)
        {
            if (_chatFade != null)
                StopCoroutine(_chatFade);

            _canvasGroup.alpha = alpha;
            _isFading = false;
        }

        public string ShowChatBox()
        {
            string message = "";

            if (_chatFade != null)
                StopCoroutine(_chatFade);

            ResetAlpha(1.0f);

            Cursor.lockState = CursorLockMode.None;

            _chatBoxInput.Select();
            _chatBoxInput.ActivateInputField();

            if (_chatBoxInput.text != "")
            {
            //    RPC_SendMessage(_chatBoxInput.text);
                message = _chatBoxInput.text;
                _chatBoxInput.text = "";
            }

            return message;
        }

        public void FadeChatBox()
        {
            if (!_isFading)
            {
                _chatFade = StartCoroutine(DoFade());
                _isFading = true;
            }
        }

        public bool IsVisible()
        {
            return _canvasGroup.alpha > 0.0f;
        }


        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_ShowChatMessage(string text)
        {
            ResetAlpha(1.0f);

            // Remove oldest message when max messages is reached
            if (messageList.Count >= maxMessages)
            {
                Destroy(messageList[0].textObject.gameObject);
                messageList.Remove(messageList[0]);
            }

            // Create new message object with incoming message
            Message newMessage = new Message();

            newMessage.text = text;

            // Just return if no text actually entered
            if (text.Equals(""))
            {
                Debug.Log("No entered text to display!");
                return;
            }

            // Instantiate a new output message object
            GameObject newText = Instantiate(textObject, chatPanel.transform);

            // Get text mesh pro component and set the text to incoming message
            newMessage.textObject = newText.GetComponent<TextMeshProUGUI>();
            newMessage.textObject.SetText(newMessage.text);

            // Add message to message output list
            messageList.Add(newMessage);
        }

        private IEnumerator DoFade()
        {
            yield return new WaitForSeconds(2.0f);

            while (_canvasGroup.alpha > 0.0f)
            {
                _canvasGroup.alpha -= 0.01f;
                yield return null;
            }

            _isFading = false;
        }
    }

    [System.Serializable]
    public class Message
    {
        public string text;
        public TextMeshProUGUI textObject;
    }
}
