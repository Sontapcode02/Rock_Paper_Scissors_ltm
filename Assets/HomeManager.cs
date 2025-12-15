using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class HomeManager : MonoBehaviour
{
    [Header("--- MAIN UI ---")]
    public GameObject mainPanel;
    public TMP_InputField nameInput;

    [Header("--- ROOM UI (POPUP) ---")]
    public GameObject joinRoomPanel;
    public TMP_InputField roomIdInput;

    [Header("--- STATUS UI ---")]
    public GameObject loadingPanel;
    public TextMeshProUGUI statusText;

    [Header("--- SOUND UI ---")]
    public Button btnSound;
    public Image imgSoundIcon;
    public Sprite spriteSoundOn;
    public Sprite spriteSoundOff;

    private string playerName;
    private volatile bool isWaitingForRoom = false;
    private struct PendingMessage
    {
        public string cmd;
        public string content;
    }
    private Queue<PendingMessage> messageQueue = new Queue<PendingMessage>();
    private object queueLock = new object();

    void Start()
    {
        mainPanel.SetActive(true);
        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(false);

        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.OnMessageReceived += HandleServerMessage;
        }
        if (btnSound)
        {
            btnSound.onClick.AddListener(OnClick_ToggleSound);
            UpdateSoundIcon();
        }
    }

    void OnDestroy()
    {
        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.OnMessageReceived -= HandleServerMessage;
        }
    }

    void HandleServerMessage(string cmd, string content)
    {
        lock (queueLock)
        {
            messageQueue.Enqueue(new PendingMessage { cmd = cmd, content = content });
        }
    }

    void Update()
    {
        int processedCount = 0;
        while (processedCount < 5)
        {
            PendingMessage msg = new PendingMessage();
            bool hasMsg = false;

            lock (queueLock)
            {
                if (messageQueue.Count > 0)
                {
                    msg = messageQueue.Dequeue();
                    hasMsg = true;
                }
            }

            if (hasMsg)
            {
                ProcessMessageOnMainThread(msg.cmd, msg.content);
                processedCount++;
            }
            else
            {
                break;
            }
        }
    }

    void ProcessMessageOnMainThread(string cmd, string content)
    {
        if (cmd == "START")
        {
            isWaitingForRoom = false;
            if (ClientSocket.Instance != null)
                ClientSocket.Instance.isGameStarted = true;
            SceneManager.LoadScene("game");
        }
        else if (cmd == "ROOM_ID")
        {
            isWaitingForRoom = false;
            statusText.text = $"ID: <color=yellow>{content}</color>\nWaiting...";
        }
        else if (cmd == "ERROR")
        {
            Debug.LogError("Server Error: " + content);
            statusText.text = $"<color=red>{content}</color>"; 
            isWaitingForRoom = false;
            joinRoomPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }
        else if (cmd == "WAIT")
        {
            if (!statusText.text.Contains("ID:"))
            {
                statusText.text = content;
            }
        }
    }

    bool CheckName()
    {
        playerName = nameInput.text;
        if (string.IsNullOrEmpty(playerName))
        {
            return false;
        }
        PlayerPrefs.SetString("PlayerName", playerName);
        return true;
    }

    void OnClick_ToggleSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
            UpdateSoundIcon();
            AudioManager.Instance.PlayClick();
        }
    }

    void UpdateSoundIcon()
    {
        if (AudioManager.Instance != null && imgSoundIcon != null)
        {
            bool isMuted = AudioManager.Instance.IsMuted;
            imgSoundIcon.sprite = isMuted ? spriteSoundOff : spriteSoundOn;
        }
    }

    public void OnClick_StartRandom()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();
        if (!CheckName()) return;

        mainPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = "Searching...";

        if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();

        ClientSocket.Instance.OnConnected += () => {
            string msg = "LOGIN|" + playerName + "|RANDOM";
            ClientSocket.Instance.SendData(msg);
        };
        ClientSocket.Instance.ConnectToServer();
    }

    public void OnClick_OpenJoinPanel()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();
        if (!CheckName()) return;
        mainPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }

    public void OnClick_CreateRoom()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();

        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = "Creating...";

        if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();

        ClientSocket.Instance.OnConnected += () => {
            string msg = "LOGIN|" + playerName + "|CREATE";
            ClientSocket.Instance.SendData(msg);
        };
        ClientSocket.Instance.ConnectToServer();

        isWaitingForRoom = true;
        StopAllCoroutines();
        StartCoroutine(TimeoutCheck());
    }

    IEnumerator TimeoutCheck()
    {
        yield return new WaitForSeconds(5.0f);

        if (isWaitingForRoom)
        {
            if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();
            isWaitingForRoom = false;
            statusText.text = "Failed";
            yield return new WaitForSeconds(2.0f);
            joinRoomPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }
    }

    public void OnClick_JoinByCode()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();
        string code = roomIdInput.text;
        if (string.IsNullOrEmpty(code)) return;

        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = $"Loading {code}...";

        if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();

        ClientSocket.Instance.OnConnected += () => {
            string msg = "LOGIN|" + playerName + "|" + code;
            ClientSocket.Instance.SendData(msg);
        };
        ClientSocket.Instance.ConnectToServer();

        isWaitingForRoom = true;
        StopAllCoroutines();
        StartCoroutine(TimeoutCheck());
    }

    public void OnClick_BackToHome()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClick();

        if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();

        isWaitingForRoom = false;
        StopAllCoroutines();

        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}