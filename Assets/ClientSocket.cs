using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ClientSocket : MonoBehaviour
{
    public static ClientSocket Instance;
    public bool isGameStarted = false;
    public Action OnConnected;

    [Header("Server Config")]
    public string serverIP = "147.185.221.24";
    public int serverPort = 28224;

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

    public void ConnectToServer()
    {
        if (client != null && client.Connected)
        {
            OnConnected?.Invoke();
            OnConnected = null;
            return;
        }

        Thread connectThread = new Thread(() =>
        {
            try
            {
                client = new TcpClient();
                client.Connect(serverIP, serverPort);
                stream = client.GetStream();
                isRunning = true;

                receiveThread = new Thread(ReceiveLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                Debug.Log("Connected");

                if (OnConnected != null)
                {
                    OnConnected();
                    OnConnected = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Connect failed: " + e.Message);
            }
        });
        connectThread.IsBackground = true;
        connectThread.Start();
    }

    public void SendData(string message)
    {
        if (client == null || !client.Connected)
        {
            Debug.LogError("Not connected");
            return;
        }

        try
        {
            if (stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
            Disconnect();
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        while (isRunning)
        {
            try
            {
                if (client == null || !client.Connected || stream == null)
                {
                    isRunning = false;
                    break;
                }

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
        string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;

            string[] parts = cleanLine.Split('|');
            if (parts.Length >= 2)
            {
                string cmd = parts[0];
                string content = parts[1];
                OnMessageReceived?.Invoke(cmd, content);
            }
        }
    }

    public void Disconnect()
    {
        isRunning = false;

        if (stream != null) stream.Close();
        if (client != null) client.Close();

        client = null;

        Debug.Log("Disconnected");
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}