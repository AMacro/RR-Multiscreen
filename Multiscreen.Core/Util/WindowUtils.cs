using TMPro;
using UI.Common;
using UnityEngine;

namespace Multiscreen.Util
{
    public static class WindowUtils
    {
        public static void SetDisplay(this Component targetWindow, bool secondary)
        {
            if (targetWindow == null)
            {
                return;
            }

            Logger.LogVerbose($"SetDisplay({targetWindow?.name}, {secondary})\r\n\tCurrent Transform: {targetWindow?.transform?.name}\r\n\tCurrent Transform Parent: {targetWindow?.transform?.parent?.name}");

            GameObject newParent = null;
            GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK + "1");
            GameObject modalParent = GameObject.Find(Multiscreen.MODALS);

            if (undockParent != null)
                Logger.LogDebug($"SetDisplay: Found undockParent");
            if (modalParent != null)
                Logger.LogDebug($"SetDisplay: Found modalParent");

            if (secondary == true && undockParent != null)
            {
                newParent = undockParent;
                targetWindow.transform.SetLossyScale(new Vector3(Multiscreen.settings.secondDisplayScale, Multiscreen.settings.secondDisplayScale, Multiscreen.settings.secondDisplayScale));
            }
            else
            {
                newParent = modalParent;
                targetWindow.transform.SetLossyScale(modalParent.transform.lossyScale);
            }

            if (newParent != null)
            {
                Logger.LogDebug($"SetDisplay({targetWindow?.name}, {secondary}) New parent: {newParent.name}");
                targetWindow.transform.SetParent(newParent.transform);
            }
        }

        public static void ToggleDisplay(this Component targetWindow) 
        {
            Logger.LogDebug($"ToggleDisplay({targetWindow?.name}) Current parent: \"{targetWindow.transform.parent.name}\"");

            int display = targetWindow.GetDisplayForWindow();

            if (display == 0)
            {
                targetWindow.SetDisplay(true);
            }
            else
            {
                targetWindow.SetDisplay(false);
            }

            /*

            if (targetWindow.transform.parent.name == Multiscreen.UNDOCK)
            {
                targetWindow.SetDisplay(false);
            }
            else if (targetWindow.transform.parent.name == Multiscreen.MODALS)
            {
                targetWindow.SetDisplay(true);
            }
            */

            Window win = targetWindow.GetComponentInChildren<Window>();
            win.ClampToParentBounds();
        }

        public static void UpdateScale(float scale)
        {
            GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK+"1");

            if (undockParent == null)
                return;

            for (int i = 0; i < undockParent.transform.childCount; i++)
            {
                Window window = undockParent.transform.GetChild(i).GetComponent<Window>();
                if (window != null && window.IsShown)
                {
                    window.transform.SetLossyScale(new Vector3(scale, scale, scale));
                }
            }
        }

        public static void SetLossyScale(this Transform targetTransform, Vector3 lossyScale)
        {
            targetTransform.localScale = new Vector3(targetTransform.localScale.x * (lossyScale.x / targetTransform.lossyScale.x),
                                                     targetTransform.localScale.y * (lossyScale.y / targetTransform.lossyScale.y),
                                                     targetTransform.localScale.z * (lossyScale.z / targetTransform.lossyScale.z));
        }

        public static int GetDisplayForWindow(this Component window)
        {
            int display = 0;

            string canvasName = window.transform.parent.name;
            Logger.LogDebug($"GetDisplayForWindow({window.name}): canvas: {canvasName}");

            if ( canvasName == Multiscreen.MODALS)
                return 0;
            
            if(canvasName.StartsWith(Multiscreen.UNDOCK) && int.TryParse(canvasName.Substring(Multiscreen.UNDOCK.Length), out  display))
            {
                Logger.LogDebug($"GetDisplayForWindow({window.name}): display: {display}");
                return display;
            }
            else
            {
                Logger.LogDebug($"GetDisplayForWindow({window.name}): StartsWith:{canvasName.StartsWith(Multiscreen.UNDOCK)} TryParse: {int.TryParse(canvasName.Substring(Multiscreen.UNDOCK.Length), out display)}");
            }

            return 0;
        }
    }
}
