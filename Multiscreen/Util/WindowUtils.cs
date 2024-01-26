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
            GameObject undockParent = GameObject.Find(Multiscreen.UNDOCK);
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
                //targetWindow.transform.SetLossyScale(new Vector3(1, 1, 1));
            }

            if (newParent != null)
            {
                Logger.LogDebug($"SetDisplay({targetWindow?.name}, {secondary}) New parent: {newParent.name}");
                targetWindow.transform.SetParent(newParent.transform);
                //Window win = targetWindow.GetComponentInChildren<Window>();
                //win.ShowWindow();


            }
             

        }

        public static void ToggleDisplay(this Component targetWindow) 
        {
            Logger.LogDebug($"ToggleDisplay({targetWindow?.name}) Current parent: \"{targetWindow.transform.parent.name}\"");

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
                    window.transform.SetLossyScale(new Vector3(Multiscreen.settings.secondDisplayScale, Multiscreen.settings.secondDisplayScale, Multiscreen.settings.secondDisplayScale));
                }
            }
        }

        public static void SetLossyScale(this Transform targetTransform, Vector3 lossyScale)
        {
            targetTransform.localScale = new Vector3(targetTransform.localScale.x * (lossyScale.x / targetTransform.lossyScale.x),
                                                     targetTransform.localScale.y * (lossyScale.y / targetTransform.lossyScale.y),
                                                     targetTransform.localScale.z * (lossyScale.z / targetTransform.lossyScale.z));
        }
    }
}
