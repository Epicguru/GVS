using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;

namespace GVS.Screens.Instances
{
    public class MainMenuScreen : GameScreen
    {
        private Panel mainPanel;

        private Button hostButton;
        private Button quitButton;
        private TextInput portInput;

        public MainMenuScreen() : base("Main Menu")
        {
        }

        public override void Load()
        {
            // Construct the UI, if it doesn't already exist.
            if (mainPanel != null)
                return;

            mainPanel = new Panel(Vector2.Zero);
            mainPanel.Size = new Vector2(200f, 400f);

            hostButton = new Button("Host", ButtonSkin.Fancy);
            hostButton.OnClick += OnPlayClicked;
            mainPanel.AddChild(hostButton);

            portInput = new TextInput(false);
            portInput.PlaceholderText = "Type port...";
            portInput.ToolTipText = "Port number to host on or connect to.";
            portInput.Validators.Add(new GeonBit.UI.Entities.TextValidators.TextValidatorNumbersOnly(false));
            portInput.TextParagraph.Text = "7777";
            mainPanel.AddChild(portInput);

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

        private void OnPlayClicked(Entity e)
        {
            if (!Manager.IsTransitioning)
                Manager.ChangeScreen<PlayScreen>();
        }

        private void OnExitClicked(Entity e)
        {
            Main.ForceExitGame();
        }
    }
}
