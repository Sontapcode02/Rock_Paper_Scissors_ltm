NETWORK PROGRAMMING PROJECT: ROCK PAPER SCISSORS
A TCP-based multiplayer game implementation demonstrating socket programming, multithreading, and client-server synchronization.
1. PROJECT OVERVIEW
•	Project Name: Rock Paper Scissors Online
•	Architecture: Client-Server Model
•	Communication: TCP/IP Sockets (Stream)
•	Data Format: String-based custom protocol (CMD|PAYLOAD)
2. REPOSITORY STRUCTURE
Plaintext
Root/
├── Server/
│   ├── Server.py          # Main server logic, room management, matchmaking
│   └── README.txt         # Server deployment instructions
│
├── Client (Unity)/
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── ClientSocket.cs    # TCP Connection & Threading handler
│   │   │   ├── HomeManager.cs     # UI Logic: Login, Matchmaking
│   │   │   ├── GameController.cs  # UI Logic: Gameplay, Results
│   │   │   └── AudioManager.cs    # Audio Singleton
│   │   └── Scenes/
│   │       ├── HomeScene.unity
│   │       └── GameScene.unity
│   └── ProjectSettings/
│
└── README.md
3. COMMUNICATION PROTOCOL
The application uses a custom text-based protocol. Messages are delimited by the newline character \n.
Client Requests
Command	Format	Description
LOGIN	`LOGIN	Name
MOVE	`MOVE	Selection`
CHAT	`CHAT	Message`
Server Responses
Command	Format	Description
ROOM_ID	`ROOM_ID	1234`
WAIT	`WAIT	Message`
START	`START	Start`
RESULT	`RESULT	Details...`
GAMEOVER	`GAMEOVER	Result`
ERROR	`ERROR	Message`
4. FEATURES
•	Matchmaking System:
o	Private Room: Generates a unique 4-digit code.
o	Public Match: Randomly pairs players in open public lobbies.
•	Synchronization:
o	Thread-safe room management using Python threading.Lock.
o	Real-time state updates for both clients.
•	Game Mechanics:
o	Standard Rock-Paper-Scissors logic.
o	Score tracking (First to 3 points wins).
o	Handling of opponent disconnection.
5. INSTALLATION
Prerequisites
•	Server: Python 3.8 or higher.
•	Client: Unity 2021.3 LTS or higher.
Server Deployment
1.	Open a terminal in the Server folder.
2.	Run the server:
Bash
python Server.py
3.	The server binds to 0.0.0.0:65432 by default.
Client Configuration
1.	Open ClientSocket.cs in the Unity Project.
2.	Modify the serverIP variable:
C#
public string serverIP = "127.0.0.1"; // Change to Server Public IP for WAN
3.	Build the project for Android or Windows.
6. TROUBLESHOOTING
•	Connection Failed: Check if the Firewall allows traffic on port 65432.
•	Room Not Found: Ensure the Room ID is correct and the room has not been closed.
•	UI Scaling: Ensure "Canvas Scaler" is set to "Scale with Screen Size" in Unity.
________________________________________
University Project - Network Programming Module

