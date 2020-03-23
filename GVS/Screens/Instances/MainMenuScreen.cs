using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;

namespace GVS.Screens.Instances
{
    public class MainMenuScreen : GameScreen
    {
        private Panel mainPanel;

        private Button hostButton;
        private Button connectButton;
        private Button quitButton;

        public MainMenuScreen() : base("Main Menu")
        {
        }

        public override void Load()
        {
            // Construct the UI, if it doesn't already exist.
            if (mainPanel != null)
                return;

            mainPanel = new Panel(Vector2.Zero);
            mainPanel.Size = new Vector2(300f, 500f);

            hostButton = new Button("Host", ButtonSkin.Fancy);
            hostButton.OnClick += OnHostClicked;
            mainPanel.AddChild(hostButton);

            connectButton = new Button("Connect", ButtonSkin.Fancy);
            connectButton.OnClick += OnConnectClicked;
            mainPanel.AddChild(connectButton);

            quitButton = new Button("Exit");
            quitButton.Anchor = Anchor.BottomCenter;
            quitButton.OnClick += OnExitClicked;
            mainPanel.AddChild(quitButton);
        }

        public override void UponShow()
        {
            UserInterface.Active.AddEntity(mainPanel);
        }

        public override void UponHide()
        {
            UserInterface.Active.RemoveEntity(mainPanel);
        }

        private void OnHostClicked(Entity e)
        {
            if (!Manager.IsTransitioning)
            {
                Manager.GetScreen<PlayScreen>().HostMode = true;
                Manager.ChangeScreen<PlayScreen>();
            }
        }

        private void OnConnectClicked(Entity e)
        {
            if (!Manager.IsTransitioning)
                Manager.ChangeScreen<ConnectScreen>();
        }

        private void OnExitClicked(Entity e)
        {
            Main.ForceExitGame();
        }
    }
}
