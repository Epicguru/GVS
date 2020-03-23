using GeonBit.UI;
using GeonBit.UI.Entities;
using GeonBit.UI.Utils;
using GVS.Networking;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GVS.Screens.Instances
{
    public class ConnectScreen : GameScreen
    {
        private Panel mainPanel;
        private TextInput portInput;
        private TextInput ipInput;
        private TextInput passwordInput;
        private Button connectButton;

        private Panel connectingPanel;
        private Paragraph connectionStatus;
        private Button cancelConnectButton;

        public ConnectScreen() : base("Connect Screen")
        {
        }

        public override void Load()
        {
            if (mainPanel != null)
                return;

            connectingPanel = new Panel(new Vector2(300, 200), PanelSkin.Golden);
            connectionStatus = new Paragraph("Connecting...");
            connectionStatus.Anchor = Anchor.TopCenter;
            connectionStatus.AlignToCenter = true;
            connectionStatus.WrapWords = true;
            connectingPanel.AddChild(connectionStatus);

            cancelConnectButton = new Button("Cancel", ButtonSkin.Alternative);
            cancelConnectButton.FillColor = Color.LightPink;
            cancelConnectButton.Anchor = Anchor.BottomRight;
            cancelConnectButton.OnClick += OnCancelConnectClicked;
            connectingPanel.AddChild(cancelConnectButton);

            mainPanel = new Panel(new Vector2(400, 250), PanelSkin.Fancy);

            Button exitButton;
            mainPanel.AddChild(exitButton = new Button("X", ButtonSkin.Alternative, Anchor.TopRight, new Vector2(32, 32)));
            exitButton.Children[0].Offset = new Vector2(0f, -10f);
            exitButton.FillColor = Color.HotPink;
            exitButton.OnClick += OnExitClicked;

            mainPanel.AddChild(new Header("Join Multiplayer", Anchor.TopCenter){ AlignToCenter = true });
            mainPanel.AddChild(new HorizontalLine());

            Label l;
            mainPanel.AddChild(l = new Label("IP:"));
            l.MatchTextWidth();

            ipInput = new TextInput(false);
            ipInput.Anchor = Anchor.AutoInlineNoBreak;
            ipInput.PlaceholderText = "Type ip address...";
            ipInput.CharactersLimit = 32;
            mainPanel.AddChild(ipInput);
            ipInput.ExtendToParentBounds(true, false);

            mainPanel.AddChild(l = new Label("Port:"));
            l.MatchTextWidth();

            portInput = new TextInput(false);
            portInput.Anchor = Anchor.AutoInlineNoBreak;
            portInput.PlaceholderText = "Type port...";
            portInput.ToolTipText = "Port number to host on or connect to.";
            portInput.Validators.Add(new GeonBit.UI.Entities.TextValidators.TextValidatorNumbersOnly(false, 0));
            portInput.Value = "7777";
            portInput.CharactersLimit = 5;
            mainPanel.AddChild(portInput);
            portInput.ExtendToParentBounds(true, false);

            mainPanel.AddChild(l = new Label("Password"));
            l.MatchTextWidth();

            passwordInput = new TextInput(false);
            passwordInput.Anchor = Anchor.AutoInlineNoBreak;
            passwordInput.PlaceholderText = "No password.";
            passwordInput.ToolTipText = "Server password. Leave blank for no password.";
            passwordInput.Value = "";
            passwordInput.CharactersLimit = 32;
            mainPanel.AddChild(passwordInput);
            passwordInput.ExtendToParentBounds(true, false);

            connectButton = new Button("Connect", ButtonSkin.Fancy);
            connectButton.OnClick += OnConnectClicked;
            mainPanel.AddChild(connectButton);
        }

        public void OnConnectClicked(Entity e)
        {
            if (MessageBox.IsMsgBoxOpened)
                return; 

            string ip = ipInput.Value.Trim();
            string port = portInput.Value.Trim();
            string password = passwordInput.Value.Trim();

            MessageBox.DefaultMsgBoxSize = new Vector2(300, 300);
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.ShowMsgBox("Error", "Please input an IP address to connect to.", "Ok");
                return;
            }
            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.ShowMsgBox("Error", "Please input a port number to connect on.", "Ok");
                return;
            }

            int portNum = int.Parse(port); // Should always work because there is a validator on the input.

            Debug.Log($"Starting connect: {ip}, {portNum}");
            bool worked = Main.Client.Connect(ip, portNum, out string error, password);
            if (!worked)
            {
                MessageBox.ShowMsgBox("Failed to connect", $"Connecting failed:\n{error}");
                return;
            }

            ToggleConnectPanel(true);
        }

        public void OnCancelConnectClicked(Entity e)
        {
            Main.Client.Disconnect();
            ToggleConnectPanel(false);
        }

        public void OnExitClicked(Entity e)
        {
            if (!Manager.IsTransitioning)
                Manager.ChangeScreen<MainMenuScreen>();
        }

        private void ToggleConnectPanel(bool visible)
        {
            connectingPanel.Visible = visible;
            mainPanel.Visible = !visible;
        }

        private void SetConnectionStatus(string txt)
        {
            connectionStatus.Text = txt;
        }

        public override void UponShow()
        {
            Debug.Assert(Main.Client == null, "Expected client to be null!");
            Main.Client = new GameClient();
            Main.Client.OnStatusChange += ClientConnStatusChange;
            Main.Client.OnConnected += OnClientConnect;
            Main.Client.OnDisconnected += OnClientDisconnected;

            UserInterface.Active.AddEntity(mainPanel);
            UserInterface.Active.AddEntity(connectingPanel);
            ToggleConnectPanel(false);
        }

        public override void UponHide()
        {
            UserInterface.Active.RemoveEntity(mainPanel);
            UserInterface.Active.RemoveEntity(connectingPanel);

            if(Main.Client.ConnectionStatus == NetConnectionStatus.Disconnected)
            {
                Main.Client.Dispose();
                Main.Client = null;
            }
            else
            {
                Main.Client.OnStatusChange -= ClientConnStatusChange;
                Main.Client.OnConnected -= OnClientConnect;
                Main.Client.OnDisconnected -= OnClientDisconnected;
            }
        }

        private void ClientConnStatusChange(NetConnection conn, NetConnectionStatus status, NetIncomingMessage msg)
        {
            SetConnectionStatus($"Connecting: {status}");
        }

        private void OnClientConnect(NetIncomingMessage msg)
        {
            // Cool, connected. Move to game screen.
            var screen = Manager.GetScreen<PlayScreen>();
            screen.HostMode = false;

            screen.ConnectMessage = msg;

            Manager.ChangeScreen<PlayScreen>();
        }

        private void OnClientDisconnected(NetIncomingMessage msg)
        {
            string reason = msg.PeekString();
            ToggleConnectPanel(false);
            MessageBox.ShowMsgBox("Failed to connect", $"Connection was rejected:\n{reason}");
        }

        public override void Update()
        {
            if (Input.KeyDown(Keys.V))
                Debug.Log($"{portInput.GetRelativeOffset()}");

            Main.Client.Update();
        }
    }
}
