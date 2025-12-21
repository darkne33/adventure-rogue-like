using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomPackages.Package.Extensions.UIExtension
{
    public static class UIExtensions
    {
        public static bool IsPointerOverUIElement()
        {
            for(int index = 0;  index < GetEventSystemRaycastResults().Count; index ++)
            {
                RaycastResult curRaysastResult = GetEventSystemRaycastResults()[index];

                if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
                    return true;
            }

            return false;
        }
        
        private static List<RaycastResult> GetEventSystemRaycastResults()
        {   
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position =  Input.mousePosition;

            List<RaycastResult> raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll( eventData, raysastResults );

            return raysastResults;
        }
    }
}