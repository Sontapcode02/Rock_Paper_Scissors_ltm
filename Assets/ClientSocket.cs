using System;
using System.Collections.Concurrent; // Dùng hàng đợi an toàn
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ClientSocket : MonoBehaviour
{
    public static ClientSocket Instance;
    public bool isGameStarted = false;
    public Action OnConnected;
    [Header("Server Config")]
    public string serverIP = "127.0.0.1"; 
    public int serverPort = 65432;        

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    public event Action<string, string> OnMessageReceived;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
    }

    public void ConnectToServer()
    {
        Thread connectThread = new Thread(() =>
        {
            try
            {
                client = new TcpClient(serverIP, serverPort);
                stream = client.GetStream();
                isRunning = true;

                receiveThread = new Thread(ReceiveLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                Debug.Log("Server connected!");
                if (OnConnected != null)
                {
                    OnConnected();                  
                    OnConnected = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Lỗi kết nối: " + e.Message);
            }
        });
        connectThread.IsBackground = true;
        connectThread.Start();
    }

    public void SendData(string message)
    {
        // 1. Kiểm tra biến client có null không
        if (client == null)
        {
            Debug.LogError("Client chưa khởi tạo!");
            return;
        }

        // 2. Kiểm tra trạng thái kết nối
        if (!client.Connected)
        {
            Debug.LogError("Mất kết nối với Server rồi đại ca ơi!");
            return;
        }

        try
        {
            // 3. Kiểm tra luồng ghi
            if (stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n"); // Thêm \n để chắc chắn ngắt dòng nếu cần
                stream.Write(data, 0, data.Length);
                // Debug.Log("Đã gửi: " + message); // Bật lên nếu muốn soi log
            }
        }
        catch (System.IO.IOException e)
        {
            // Đây chính là cái lỗi "Aborted" đại ca đang gặp
            Debug.LogError("Lỗi đường truyền (Server tắt hoặc mạng rớt): " + e.Message);

            // Tự động ngắt kết nối để reset
            isRunning = false;
            client.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi lạ khi gửi: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        while (isRunning)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageQueue.Enqueue(response);
                    }
                }
            }
            catch (Exception)
            {
                isRunning = false;
            }
            Thread.Sleep(10);
        }
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessData(message);
        }
    }

    private void ProcessData(string data)
    {
        // 1. Cắt vụn dữ liệu theo dấu xuống dòng \n (đề phòng ReceiveLoop cắt sót)
        string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            // 2. Xử lý từng lệnh đơn lẻ
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;

            string[] parts = cleanLine.Split('|');
            if (parts.Length >= 2)
            {
                string cmd = parts[0];
                string content = parts[1];

                // Bắn tin ra cho HomeManager/GameController xử lý
                OnMessageReceived?.Invoke(cmd, content);
            }
        }
    }
    public void Disconnect()
    {
        isRunning = false; // Dừng vòng lặp nhận tin

        if (stream != null) stream.Close();
        if (client != null) client.Close();

        Debug.Log("Đã ngắt kết nối và hủy Socket!");

        // Quan trọng: Hủy luôn chính nó để về Home nó Reset lại từ đầu
        Destroy(gameObject);
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        if (client != null) client.Close();
    }

}