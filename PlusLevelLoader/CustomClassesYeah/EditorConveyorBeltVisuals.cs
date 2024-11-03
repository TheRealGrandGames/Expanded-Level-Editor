using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorConveyorBeltVisuals : MonoBehaviour
    {
        void Start()
        {
            base.transform.localRotation = new Quaternion(90f, base.transform.rotation.y, base.transform.rotation.z, base.transform.rotation.w);
        }
    }
}
