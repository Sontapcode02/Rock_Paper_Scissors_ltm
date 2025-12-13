using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("--- UI SCORE & STATUS ---")]
    public TextMeshProUGUI statusText; // Hiện kết quả, thông báo
    public TextMeshProUGUI scoreText;  // Hiện tỷ số "3 : 2"

    [Header("--- UI CHAT ---")]
    public GameObject chatPanel;       // Cái khung chat (Box History)
    public TMP_InputField chatInput;   // Ô nhập chat
    public TextMeshProUGUI chatHistory;// Text chứa lịch sử chat
    public ScrollRect chatScroll;      // Thanh cuộn chat

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
    public Button btnChat;      // Nút mở chat
    public Button btnCloseChat; // Nút tắt chat (dấu X trong box)

    private Color originalColor;

    void Start()
    {
        // 1. Setup Chat mặc định
        if (chatPanel) chatPanel.SetActive(false); // Mới vào ẩn chat đi
        if (chatHistory) chatHistory.text = "";

        // 2. Setup Score & Status
        if (statusText) originalColor = statusText.color;
        if (scoreText) scoreText.text = "0 : 0";

        // 3. Check Socket
        if (ClientSocket.Instance == null)
        {
            SceneManager.LoadScene("HomeScene");
            return;
        }
        ClientSocket.Instance.OnMessageReceived += ProcessMessage;

        if (ClientSocket.Instance.isGameStarted)
        {
            SetupGameStart();
        }

        // 4. Gán sự kiện cho các nút
        if (btnKeo) btnKeo.onClick.AddListener(() => SendMove("SCISSORS"));
        if (btnBua) btnBua.onClick.AddListener(() => SendMove("ROCK"));
        if (btnBao) btnBao.onClick.AddListener(() => SendMove("PAPER"));
        if (btnQuit) btnQuit.onClick.AddListener(Disconnect);

        // --- SỰ KIỆN NÚT CHAT ---
        if (btnChat) btnChat.onClick.AddListener(ToggleChat);       // Nút Chat chính (Bật/Tắt)
        if (btnCloseChat) btnCloseChat.onClick.AddListener(CloseChat); // Nút X (Chỉ tắt)

        // Sự kiện Enter để gửi chat
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
                // Xử lý kết quả trận đấu & Tỷ số
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

            // --- XỬ LÝ TIN NHẮN CHAT TỪ SERVER GỬI VỀ ---
            case "CHAT":
                if (chatHistory)
                {
                    chatHistory.text += content + "\n";
                    ScrollToBottom();

                    // (Tùy chọn) Nếu muốn tin nhắn đến thì tự bật khung chat lên:
                    // if (chatPanel && !chatPanel.activeSelf) chatPanel.SetActive(true);
                }
                break;
        }
    }

    // ======================================================
    // LOGIC CHAT (Bật/Tắt/Gửi)
    // ======================================================
    void ToggleChat()
    {
        if (chatPanel)
        {
            bool isActive = chatPanel.activeSelf;
            chatPanel.SetActive(!isActive); // Đang bật thì tắt, đang tắt thì bật
        }
    }

    void CloseChat()
    {
        if (chatPanel) chatPanel.SetActive(false);
    }

    public void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Gửi lên Server (Server sẽ gửi lại cho cả 2 người)
        ClientSocket.Instance.SendData("CHAT|" + message);

        // Xóa ô nhập liệu và focus lại để gõ tiếp
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

    // ======================================================
    // LOGIC GAMEPLAY
    // ======================================================
    IEnumerator HandleGameOver(string result)
    {
        yield return new WaitForSeconds(2.0f);
        if (result == "WIN")
        {
            if (statusText) { statusText.text = "VICTORY!"; statusText.color = Color.green; }
        }
        else
        {
            if (statusText) { statusText.text = "DEFEAT!"; statusText.color = Color.red; }
        }
        yield return new WaitForSeconds(3.0f);
        Debug.Log("Back home!");
        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.Disconnect();
        }
        SceneManager.LoadScene("home");
    }

    void SetupGameStart()
    {
        if (statusText) { statusText.text = "START!"; statusText.color = originalColor; }
        ResetHands();
        EnableButtons(true);
    }

    void SendMove(string move)
    {
        ClientSocket.Instance.SendData("MOVE|" + move);
        if (statusText) statusText.text = move + ". WAIT...";
        ShowWaitingHands();
        EnableButtons(false);
    }

    void Disconnect()
    {
        if (ClientSocket.Instance) ClientSocket.Instance.isGameStarted = false;
        SceneManager.LoadScene("HomeScene");
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
        if (statusText) statusText.text = "NEW ROUND!";
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