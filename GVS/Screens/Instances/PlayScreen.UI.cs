using GeonBit.UI;
using GeonBit.UI.Entities;
using GVS.Networking.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.Screens.Instances
{
    public partial class PlayScreen
    {
        private Panel playerList;

        private Panel roundAndTurnPanel;
        private RichParagraph roundAndTurnParagraph;

        private Texture2D playerIcon;
        
        private void LoadUIData()
        {
            if(playerIcon == null)
            {
                playerIcon = Main.ContentManager.Load<Texture2D>("Textures/PlayerIcon");
            }
        }

        private void CreateUI()
        {
            if (playerList != null)
                return;

            LoadingScreenText = "Generate UI...";

            CreatePlayerList();
            CreateRoundAndTurnPanel();
        }

        private void CreatePlayerList()
        {
            playerList = new Panel(new Vector2(220, 200), PanelSkin.Simple, Anchor.TopLeft);
            playerList.FillColor = new Color(0, 0, 0, 0.3f);

            playerList.AdjustHeightAutomatically = true;
        }

        private void AddPlayerItem(Player p)
        {
            Panel playerItem = new Panel(new Vector2(-1, 42), anchor: Anchor.Auto);
            playerItem.AttachedData = p.ID; // Attach the ID of the player, to know who it is.
            playerItem.AddChild(new Image(playerIcon, new Vector2(32, 32), ImageDrawMode.Stretch, Anchor.CenterLeft));
            playerItem.AddChild(new Label(p.Name)
            {
                Anchor = Anchor.CenterLeft,
                Offset = new Vector2(40f, 0f)
            });
            playerList.AddChild(playerItem);
        }

        private void RemovePlayerItem(Player p)
        {
            var item = GetPlayerItem(p);
            if (item == null)
                return;

            playerList.RemoveChild(item);
        }

        private Panel GetPlayerItem(Player p)
        {
            if (p == null)
                return null;

            foreach (var item in playerList.Children)
            {
                if ((uint)item.AttachedData == p.ID)
                    return item as Panel;
            }

            return null;
        }

        private void CreateRoundAndTurnPanel()
        {
            roundAndTurnPanel = new Panel();
            roundAndTurnPanel.Anchor = Anchor.TopCenter;
            roundAndTurnPanel.Size = new Vector2(240, 60);
            roundAndTurnPanel.Skin = PanelSkin.Default;

            roundAndTurnParagraph = new RichParagraph("{{BOLD}}Round 0{{REGULAR}}\nBlyatman's turn");
            roundAndTurnParagraph.Anchor = Anchor.AutoCenter;
            roundAndTurnParagraph.AlignToCenter = true;
            roundAndTurnPanel.AdjustHeightAutomatically = true;
            roundAndTurnPanel.AddChild(roundAndTurnParagraph);
            roundAndTurnPanel.SetHeightBasedOnChildren();
        }

        private void ShowUI()
        {
            UserInterface.Active.AddEntity(playerList);
            UserInterface.Active.AddEntity(roundAndTurnPanel);
        }

        private void HideUI()
        {
            UserInterface.Active.RemoveEntity(playerList);
            UserInterface.Active.RemoveEntity(roundAndTurnPanel);
        }
    }
}
