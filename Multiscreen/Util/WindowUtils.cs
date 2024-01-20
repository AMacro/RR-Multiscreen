using RuntimeUnityEditor.Core.Utils;
using UI.Common;
using UnityEngine;

namespace Multiscreen.Utils
{
    public static class WindowUtils
    {
        public static void SetDisplay(this Component targetWindow, bool secondary)
        {
            if (targetWindow == null)
            {
                return;
            }

            Multiscreen.Log($"SetDisplay({targetWindow?.name}, {secondary})\r\n\tCurrent Transform: {targetWindow?.transform?.name}\r\n\tCurrent Transform Parent: {targetWindow?.transform?.parent?.name}");

            GameObject newParent = null;
            GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);
            GameObject modalParent = GameObject.Find(Multiscreen.MODALS);

            if (undockParent != null)
                Multiscreen.Log($"SetDisplay: Found undockParent");
            if (modalParent != null)
                Multiscreen.Log($"SetDisplay: Found modalParent");

            if (secondary == true && undockParent != null)
            {
                newParent = undockParent;
                targetWindow.transform.SetLossyScale(new Vector3(Multiscreen.Settings.secondDisplayScale, Multiscreen.Settings.secondDisplayScale, Multiscreen.Settings.secondDisplayScale));
            }
            else
            {
                newParent = modalParent;
                //targetWindow.transform.SetLossyScale(new Vector3(1, 1, 1));
            }

            if (newParent != null)
            {
                Multiscreen.Log($"SetDisplay({targetWindow?.name}, {secondary}) New parent: {newParent.name}");
                targetWindow.transform.SetParent(newParent.transform);
                Window win = targetWindow.GetComponentInChildren<Window>();
                win.ShowWindow();


            }
             

        }

        public static void ToggleDisplay(this Component targetWindow) 
        {
            Multiscreen.Log($"ToggleDisplay({targetWindow?.name}) Current parent: \"{targetWindow.transform.parent.name}\"");

            if (targetWindow.transform.parent.name == Multiscreen.UNDOCK)
            {
                targetWindow.SetDisplay(false);
            }
            else if (targetWindow.transform.parent.name == Multiscreen.MODALS)
            {
                targetWindow.SetDisplay(true);
            }
        }

        public static void UpdateScale()
        {
            GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);

            if (undockParent == null)
                return;

            for (int i = 0; i < undockParent.transform.childCount; i++)
            {
                Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
                if (window != null && window.IsShown)
                {
                    window.transform.SetLossyScale(new Vector3(Multiscreen.Settings.secondDisplayScale, Multiscreen.Settings.secondDisplayScale, Multiscreen.Settings.secondDisplayScale));
                }
            }
        }
    }
}
