
using UI.Builder;
using UnityEngine;
using UI.Menu;
using UI.Builder;
using UI.Common;
using Markroader;

namespace Multiscreen.Custom_Window;
public class MultiscreenSplash : BuilderMenuBase
{
    public override void BuildPanelContent(UIPanelBuilder builder)
    {
        UIPanelBuilder builder2 = new UIPanelBuilder();

        string text = TMPMarkupRenderer.Render(Parser.Parse("<align=\"left\">\r\n## Multiscreen Mod\r\n\r\n### Usage\r\nEnsure your second screen is connected and working prior to starting Railroader.\r\n\r\nThe Map and Company windows will show up on the second screen when first opened, all other windows open on the main screen by default.\r\n\r\nTo swap windows between screens: hold ALT and click the title bar of the window.\r\n\r\nWindows can not be dragged between screens.\r\n\r\nIf you drag a window off-screen and release, you won't be able to drag it back, instead, hide/close the window and re-open it.\r\n\r\n\r\n### Support & Suggestions\r\nSupport and sugestions are welcome, please comment on the mod page on NexusMods.\r\n\r\n### Happy Railroading!\r\n</align>"));
        builder2.AddTextArea(text, delegate (string s)
        {
        }).FlexibleHeight(10000f);

        builder2.Spacer(16f);
        builder2.HStack(delegate (UIPanelBuilder builder)
        {
            builder.Spacer().FlexibleWidth(1f);
            builder.AddButton("Back", delegate
            {
                this.NavigationController().Pop();
            });
            builder.Spacer(22f);
            builder.Spacer().FlexibleWidth(1f);
        }, 4f);
        builder2.Spacer(8f);
    }
}

