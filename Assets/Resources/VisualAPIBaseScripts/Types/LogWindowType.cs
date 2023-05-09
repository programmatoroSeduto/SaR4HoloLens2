using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.VisualItems.Types
{
    public class LogWindowBaseType : MonoBehaviour
    {
        public virtual void EVENT_LogContent(string txt)
        {

        }
    }

    public class LogWindowTitleContentType : LogWindowBaseType
    {
        public virtual void EVENT_LogTitle(string txt)
        {

        }
    }
}
