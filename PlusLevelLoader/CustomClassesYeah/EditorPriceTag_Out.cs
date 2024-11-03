using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorPriceTag_Out : EnvironmentObject
    {
        public override void LoadingFinished()
        {
            base.gameObject.GetComponent<PriceTag>().SetText("OUT");
        }
    }
}