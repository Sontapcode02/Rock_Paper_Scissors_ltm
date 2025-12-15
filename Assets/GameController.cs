using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("--- UI SCORE & STATUS ---")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI scoreText;

    [Header("--- UI CHAT ---")]
    public GameObject chatPanel;
    public TMP_InputField chatInput;
    public TextMeshProUGUI chatHistory;
    public ScrollRect chatScroll;

    [Header("--- BATTLE AREA ---")]
    public Image playerHandImg;
    public Image enemyHandImg;
    public Animator playerAnim;
    public Animator enemyAnim;

    [Header("--- SPRITES ---")]
    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;

    [Header("--- BUTTONS ---")]
    public Button btnKeo;
    public Button btnBua;
    public Button btnBao;
    public Button btnQuit;
    public Button btnChat;
    public Button btnCloseChat;

    private Color originalColor;
    int countRound = 1;

    void Start()
    {
        if (chatPanel) chatPanel.SetActive(false);
        if (chatHistory) chatHistory.text = "";

        if (statusText) originalColor = statusText.color;
        if (scoreText) scoreText.text = "0 : 0";

        if (ClientSocket.Instance == null)
        {
            SceneManager.LoadScene("home");
            return;
        }
        ClientSocket.Instance.OnMessageReceived += ProcessMessage;

        if (ClientSocket.Instance.isGameStarted)
        {
            SetupGameStart();
        }

        if (btnKeo) btnKeo.onClick.AddListener(() => { SendMove("SCISSORS"); AudioManager.Instance.PlaySelect(); });
        if (btnBua) btnBua.onClick.AddListener(() => { SendMove("ROCK"); AudioManager.Instance.PlaySelect(); });
        if (btnBao) btnBao.onClick.AddListener(() => { SendMove("PAPER"); AudioManager.Instance.PlaySelect(); });
        if (btnQuit) btnQuit.onClick.AddListener(() => { Disconnect(); AudioManager.Instance.PlayClick(); });

        if (btnChat) btnChat.onClick.AddListener(() => { ToggleChat(); AudioManager.Instance.PlayClick(); });
        if (btnCloseChat) btnCloseChat.onClick.AddListener(() => { CloseChat(); AudioManager.Instance.PlayClick(); });

        if (chatInput) chatInput.onSubmit.AddListener(SendChatMessage);

        ResetHands();
    }

    void OnDestroy()
    {
        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.OnMessageReceived -= ProcessMessage;
        }
    }

    void ProcessMessage(string cmd, string content)
    {
        switch (cmd)
        {
            case "START":
                SetupGameStart();
                break;

            case "WAIT":
            case "INFO":
                if (statusText) statusText.text = content;
                break;

            case "RESULT":
                string result = ExtractValue(content, "Result:");
                string myMove = ExtractValue(content, "You:");
                string oppMove = ExtractValue(content, "Opponent:");
                string myScore = ExtractValue(content, "MyScore:");
                string enemyScore = ExtractValue(content, "EnemyScore:");

                if (scoreText) scoreText.text = $"{myScore} : {enemyScore}";
                if (statusText) statusText.text = result;

                StartCoroutine(ShowClashResult(myMove, oppMove));
                Invoke("ResetRound", 3.0f);
                break;

            case "GAMEOVER":
                CancelInvoke("ResetRound");
                StartCoroutine(HandleGameOver(content));
                break;

            case "CHAT":
                if (chatHistory)
                {
                    chatHistory.text += content + "\n";
                    AudioManager.Instance.PlayClick();
                    ScrollToBottom();
                    if (chatPanel && !chatPanel.activeSelf) chatPanel.SetActive(true);
                }
                break;
        }
    }

    void ToggleChat()
    {
        if (chatPanel)
        {
            bool isActive = chatPanel.activeSelf;
            chatPanel.SetActive(!isActive);
        }
    }

    void CloseChat()
    {
        if (chatPanel) chatPanel.SetActive(false);
    }

    public void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        ClientSocket.Instance.SendData("CHAT|" + message);

        if (chatInput)
        {
            chatInput.text = "";
            chatInput.ActivateInputField();
        }
    }

    void ScrollToBottom()
    {
        if (chatScroll)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }

    IEnumerator HandleGameOver(string result)
    {
        yield return new WaitForSeconds(2.0f);
        if (result == "WIN")
        {
            if (statusText)
            {
                statusText.text = "Win";
                statusText.color = Color.green;
                AudioManager.Instance.PlayWin();
            }
        }
        else
        {
            if (statusText)
            {
                statusText.text = "Lose";
                statusText.color = Color.red;
                AudioManager.Instance.PlayLose();
            }
        }
        yield return new WaitForSeconds(3.0f);

        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.Disconnect();
        }
        SceneManager.LoadScene("home");
    }

    void SetupGameStart()
    {
        if (statusText) { statusText.text = "Start"; statusText.color = originalColor; }
        ResetHands();
        EnableButtons(true);
    }

    void SendMove(string move)
    {
        ClientSocket.Instance.SendData("MOVE|" + move);
        if (statusText) statusText.text = "Waiting";
        ShowWaitingHands();
        EnableButtons(false);
    }

    void Disconnect()
    {
        if (ClientSocket.Instance) ClientSocket.Instance.Disconnect();
        SceneManager.LoadScene("home");
    }

    void ShowWaitingHands()
    {
        playerHandImg.gameObject.SetActive(true);
        enemyHandImg.gameObject.SetActive(true);
        playerHandImg.sprite = rockSprite;
        enemyHandImg.sprite = rockSprite;
        if (playerAnim) playerAnim.Play("hand_animation_static");
        if (enemyAnim) enemyAnim.Play("hand_animation_static");
    }

    IEnumerator ShowClashResult(string myMove, string oppMove)
    {
        if (playerAnim) playerAnim.Play("hand_clash");
        if (enemyAnim) enemyAnim.Play("hand_clash_enemy");
        yield return new WaitForSeconds(0.5f);
        if (playerAnim) playerAnim.enabled = false;
        if (enemyAnim) enemyAnim.enabled = false;
        playerHandImg.sprite = GetSprite(myMove);
        enemyHandImg.sprite = GetSprite(oppMove);
    }

    void ResetRound()
    {
        countRound++;
        if (statusText) statusText.text = "Round " + countRound;
        EnableButtons(true);
        playerHandImg.gameObject.SetActive(true);
        enemyHandImg.gameObject.SetActive(true);
        if (playerAnim) { playerAnim.enabled = true; playerAnim.Play("hand_animation_static"); }
        if (enemyAnim) { enemyAnim.enabled = true; enemyAnim.Play("hand_animation_static"); }
    }

    void ResetHands()
    {
        playerHandImg.gameObject.SetActive(false);
        enemyHandImg.gameObject.SetActive(false);
    }

    Sprite GetSprite(string move)
    {
        if (move == "SCISSORS") return scissorsSprite;
        if (move == "PAPER") return paperSprite;
        return rockSprite;
    }

    string ExtractValue(string fullText, string key)
    {
        int start = fullText.IndexOf(key);
        if (start == -1) return "0";
        start += key.Length;
        int end = fullText.IndexOf(' ', start);
        if (end == -1) end = fullText.Length;
        return fullText.Substring(start, end - start).Trim();
    }

    void EnableButtons(bool state)
    {
        if (btnKeo) btnKeo.interactable = state;
        if (btnBua) btnBua.interactable = state;
        if (btnBao) btnBao.interactable = state;
    }
}