using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    internal class EditorConveyorBelt : MonoBehaviour
    {
        void Start()
        {
            BeltManager beltManager = base.gameObject.GetComponent<BeltManager>();

            Direction beltDir = Direction.Null;
            switch (beltManager.transform.rotation.eulerAngles.y)
            {
                case 0: beltDir = Direction.North; break;
                case 90: beltDir = Direction.East; break;
                case 180: beltDir = Direction.South; break;
                case 270: beltDir = Direction.West; break;
                default: break;//Invalid direction treatment should be here
            }
            beltManager.GetComponent<BeltManager>().SetDirection(beltDir);
        }
    }
}
