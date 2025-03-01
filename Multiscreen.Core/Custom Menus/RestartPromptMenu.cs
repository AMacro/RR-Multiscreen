//using UI.Builder;
//using UI.Menu;
//using UnityEngine;
//using System.Linq;
//using Multiscreen.Patches.Menus;
//using TMPro;
//using System.Collections.Generic;
//using UI.CompanyWindow;
//using Logger = Multiscreen.Util.Logger;

//namespace Multiscreen.CustomMenu;

//public class RestartPromptMenu : MonoBehaviour
//{
//    public GameObject contentPanel;
//    public UIBuilderAssets assets;

//    public int newGameDisplay;
//    public int oldGameDisplay;
//    public int newSecondDisplay;
//    public int oldSecondDisplay;

//    // Token: 0x06000AA5 RID: 2725 RVA: 0x00054B67 File Offset: 0x00052D67
//    private void Awake()
//    {
//        TextMeshProUGUI title = GetComponentInChildren<TextMeshProUGUI>();
//        title.text = "Restart Required";
  
//        GameObject.DestroyImmediate(GameObject.Find("Settings Menu(Clone)/Content/Tab View(Clone)"));

//        contentPanel = GameObject.Find("Settings Menu(Clone)/Content");

//        assets = this.transform.GetComponent<PreferencesMenu>().panelAssets;

//        //oldGameDisplay = newGameDisplay = Multiscreen.gameDisplay;
//        //oldSecondDisplay = newSecondDisplay = Multiscreen.secondDisplay;
//    } 

//    private void Start()
//    {
//        BuildPanelContent();
//    }

//    private void Update()
//    {
//        if(oldGameDisplay != newGameDisplay || oldSecondDisplay != newSecondDisplay)
//        {
//            Logger.LogDebug($"Update: {oldGameDisplay} {newGameDisplay} - {oldSecondDisplay} {newSecondDisplay}");

//            oldGameDisplay = newGameDisplay;
//            oldSecondDisplay = newSecondDisplay;

//            Logger.LogDebug($"Post Update: {oldGameDisplay} {newGameDisplay} - {oldSecondDisplay} {newSecondDisplay}");

//            GameObject.DestroyImmediate(GameObject.Find("Settings Menu(Clone)/Content/Tab View(Clone)"));
//            BuildPanelContent();
//        }
//    }

//    public void BuildPanelContent()
//    {
        
//        if (contentPanel != null)
//        {
//            UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
//            {
//                Logger.LogVerbose("PanelCreate");

//                UIPanel.Create(contentPanel.transform.GetComponent<RectTransform>(), assets, delegate (UIPanelBuilder builder)
//                {
//                    builder.AddSection("Game Display");

//                    List<DisplayInfo> displays = new();
//                    Screen.GetDisplayLayout(displays);

//                    List<int> values = displays.Select((DisplayInfo r, int i) => i).ToList();

//                    //DisplayInfo currentGame = Screen.mainWindowDisplayInfo;

//                    //int selected = displays.FindIndex((DisplayInfo dis) => dis.Equals(currentGame));

//                    builder.AddField("Display", builder.AddDropdownIntPicker(values, oldGameDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
//                    {
//                        Logger.LogDebug($"Selected Display: {i}");
//                        if(i == oldSecondDisplay)
//                        {
//                            newSecondDisplay = oldGameDisplay;
//                        }
//                        newGameDisplay = i;

//                    }));
//                    builder.AddField("Full Screen", builder.AddToggle(() => Screen.fullScreen, delegate (bool en)
//                    {
//                        //Screen.fullScreen = en;
//                        Logger.LogDebug($"Game Display Full Screen: {en}");
//                    })).Disable(true);

//                    builder.AddSection("Secondary Display");
//                    //selected = Multiscreen.secondDisplay;
//                    builder.AddField("Display", builder.AddDropdownIntPicker(values, oldSecondDisplay, (int i) => (i >= 0) ? $"Display: {i} ({displays[i].name})" : $"00", canWrite: true, delegate (int i)
//                    {
//                        Logger.LogDebug($"Secondary Display: {i}");
//                        if (i == oldGameDisplay)
//                        {
//                            newGameDisplay = oldSecondDisplay;
//                        }
//                        newSecondDisplay = i;
//                        //Screen.MoveMainWindowTo(displays[i], new Vector2Int(0, 0));
//                    }));
//                    builder.AddField("Full Screen", builder.AddToggle(() => Screen.fullScreen, delegate (bool en)
//                    {
//                        //Screen.fullScreen = en;
//                        Logger.LogDebug($"Secondary Display Full Screen: {en}");
//                    }));

//                    builder.AddExpandingVerticalSpacer();

//                });

//                builder.Spacer(16f);
//                builder.HStack(delegate (UIPanelBuilder builder)
//                {
//                    builder.AddButton("Back", delegate
//                    {
//                        MenuManagerPatch._MMinstance.navigationController.Pop();

//                    });
//                    builder.Spacer().FlexibleWidth(1f);
//                    builder.AddButton("Apply", delegate
//                    {
//                        List<DisplayInfo> displays = new();
//                        Screen.GetDisplayLayout(displays);

//                        if (Multiscreen.gameDisplay != newGameDisplay)
//                        {
//                            Multiscreen.gameDisplay = newGameDisplay;
//                            Multiscreen.settings.gameDisplay = newGameDisplay;
//                            Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));
//                        }
                        
//                        if(Multiscreen.secondDisplay != newSecondDisplay)
//                        {
//                            Multiscreen.secondDisplay = newSecondDisplay;
//                            Multiscreen.settings.secondDisplay = newSecondDisplay;

                            
                           
//                        }
//                        //Screen.MoveMainWindowTo(displays[Multiscreen.gameDisplay], new Vector2Int(0, 0));

//                    });
//                });
//            });
//        }
//    }
//}


