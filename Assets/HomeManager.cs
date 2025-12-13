using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // <--- 1. CẦN THÊM CÁI NÀY ĐỂ DÙNG QUEUE

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

    private string playerName;

    // --- 2. SỬA ĐỔI: DÙNG QUEUE THAY VÌ BIẾN ĐƠN ---
    // Định nghĩa gói tin để lưu trữ
    private struct PendingMessage
    {
        public string cmd;
        public string content;
    }
    // Hàng đợi chứa các tin nhắn chờ xử lý
    private Queue<PendingMessage> messageQueue = new Queue<PendingMessage>();
    private object queueLock = new object(); // Khóa để tránh lỗi luồng
    // -----------------------------------------------

    void Start()
    {
        mainPanel.SetActive(true);
        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(false);

        if (PlayerPrefs.HasKey("PlayerName"))
            nameInput.text = PlayerPrefs.GetString("PlayerName");

        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.OnMessageReceived += HandleServerMessage;
        }
    }

    void OnDestroy()
    {
        if (ClientSocket.Instance != null)
        {
            ClientSocket.Instance.OnMessageReceived -= HandleServerMessage;
        }
    }

    // --- 3. NHẬN TIN: ĐẨY VÀO HÀNG ĐỢI ---
    void HandleServerMessage(string cmd, string content)
    {
        lock (queueLock) // Khóa lại cho an toàn luồng
        {
            messageQueue.Enqueue(new PendingMessage { cmd = cmd, content = content });
        }
    }

    // --- 4. UPDATE: LẤY TỪ HÀNG ĐỢI RA XỬ LÝ DẦN ---
    void Update()
    {
        // Xử lý tối đa 5 tin mỗi khung hình để tránh treo nếu tin đến quá nhiều
        int processedCount = 0;

        while (processedCount < 5)
        {
            PendingMessage msg = new PendingMessage();
            bool hasMsg = false;

            lock (queueLock)
            {
                if (messageQueue.Count > 0)
                {
                    msg = messageQueue.Dequeue(); // Lấy tin cũ nhất ra
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
                break; // Hết tin thì nghỉ
            }
        }
    }

    // --- HÀM XỬ LÝ LOGIC (GIỮ NGUYÊN) ---
    void ProcessMessageOnMainThread(string cmd, string content)
    {
        if (cmd == "START")
        {
            Debug.Log("Đủ người rồi! Chuyển cảnh thôi!");
            if (ClientSocket.Instance != null)
                ClientSocket.Instance.isGameStarted = true;
            SceneManager.LoadScene("game");
        }
        else if (cmd == "ROOM_ID")
        {
            // 
            statusText.text = $"ID PHÒNG: <color=yellow>{content}</color>\nWaiting...";
            Debug.Log(">>> ĐÃ NHẬN MÃ PHÒNG: " + content);
        }
        else if (cmd == "WAIT")
        {
            // Chỉ hiện Waiting nếu chưa có mã phòng (hoặc người thứ 2)
            if (!statusText.text.Contains("ID PHÒNG"))
            {
                statusText.text = content;
            }
        }
    }

    bool KiemTraTen()
    {
        playerName = nameInput.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Chưa nhập tên đại ca ơi!");
            return false;
        }
        PlayerPrefs.SetString("PlayerName", playerName);
        return true;
    }

    // ==========================================================
    // CÁC NÚT BẤM (GIỮ NGUYÊN)
    // ==========================================================

    public void OnClick_StartRandom()
    {
        if (!KiemTraTen()) return;
        mainPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = "Waiting...";
        ClientSocket.Instance.OnConnected += () => {
            ClientSocket.Instance.SendData("LOGIN|" + playerName);
        };
        ClientSocket.Instance.ConnectToServer();
    }

    public void OnClick_OpenJoinPanel()
    {
        if (!KiemTraTen()) return;
        mainPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }

    public void OnClick_CreateRoom()
    {
        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = "Creating room...";

        ClientSocket.Instance.OnConnected += () => {
            // Logic cũ: Gửi LOGIN -> Server tự biết là người đầu tiên -> Gửi ROOM_ID về
            ClientSocket.Instance.SendData("LOGIN|" + playerName);
        };
        ClientSocket.Instance.ConnectToServer();
    }

    public void OnClick_JoinByCode()
    {
        string code = roomIdInput.text;
        if (string.IsNullOrEmpty(code)) return;

        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(true);
        statusText.text = $"Loading {code}...";

        ClientSocket.Instance.OnConnected += () => {
            ClientSocket.Instance.SendData("LOGIN|" + playerName);
        };
        ClientSocket.Instance.ConnectToServer();
    }

    public void OnClick_BackToHome()
    {
        joinRoomPanel.SetActive(false);
        loadingPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}