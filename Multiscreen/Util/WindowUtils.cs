using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            GameObject undockParent = GameObject.Find("Canvas - Undock");
            GameObject modalParent = GameObject.Find("Canvas - Modals");

            if (undockParent != null)
                Multiscreen.Log($"SetDisplay: Found undockParent");
            if (modalParent != null)
                Multiscreen.Log($"SetDisplay: Found modalParent");

            if (secondary == true && undockParent != null)
            {
                newParent = undockParent;
            }
            else
            {
                newParent = modalParent;
            }

            if (newParent != null)
            {
                Multiscreen.Log($"SetDisplay({targetWindow?.name}, {secondary}) New parent: {newParent.name}");
                targetWindow.transform.SetParent(newParent.transform);
            }
             

        }

        public static void ToggleDisplay(this Component targetWindow) 
        {
            Multiscreen.Log($"ToggleDisplay({targetWindow?.name}) Current parent: \"{targetWindow.transform.parent.name}\"");

            if (targetWindow.transform.parent.name == "Canvas - Undock")
            {
                targetWindow.SetDisplay(false);
            }
            else if (targetWindow.transform.parent.name == "Canvas - Modals")
            {
                targetWindow.SetDisplay(true);
            }
        }
    }
}
