using GeonBit.UI;
using GeonBit.UI.Entities;
using GeonBit.UI.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GVS.Screens.Instances
{
    public class ConnectScreen : GameScreen
    {
        private Panel mainPanel;
        private TextInput portInput;
        private TextInput ipInput;
        private Button connectButton;

        public ConnectScreen() : base("Connect Screen")
        {
        }

        public override void Load()
        {
            if (mainPanel != null)
                return;

            mainPanel = new Panel(new Vector2(400, 300), PanelSkin.Fancy);

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

            connectButton = new Button("Connect", ButtonSkin.Fancy);
            connectButton.OnClick += OnConnectClicked;
            mainPanel.AddChild(connectButton);

            mainPanel.SetHeightBasedOnChildren();
        }

        public void OnConnectClicked(Entity e)
        {
            if (GeonBit.UI.Utils.MessageBox.IsMsgBoxOpened)
                return; 

            string ip = ipInput.Value.Trim();
            string port = portInput.Value.Trim();

            MessageBox.DefaultMsgBoxSize = new Vector2(300, 300);
            if (string.IsNullOrWhiteSpace(ip))
            {
                var handle = MessageBox.ShowMsgBox("Error", "Please input an IP address to connect to.", "Ok");
                var button = handle.Panel.Children[3].Children[0] as Button;
                button.FillColor = Color.Red;
                return;
            }
            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.ShowMsgBox("Error", "Please input a port number to connect on.", "Ok");
                return;
            }

            int portNum = int.Parse(port); // Should always work because there is a validator on the input.
            Debug.Log($"Starting connect: {ip}, {portNum}");
        }

        public override void UponShow()
        {
            UserInterface.Active.AddEntity(mainPanel);
        }

        public override void UponHide()
        {
            UserInterface.Active.RemoveEntity(mainPanel);
        }

        public override void Update()
        {
            if (Input.KeyDown(Keys.V))
                Debug.Log($"{portInput.GetRelativeOffset()}");
        }
    }
}
