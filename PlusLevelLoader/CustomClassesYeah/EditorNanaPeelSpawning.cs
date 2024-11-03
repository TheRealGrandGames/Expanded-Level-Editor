using System.Linq;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    internal class EditorNanaPeelSpawning : EnvironmentObject
    {
        private EnvironmentController ec;

        private ITM_NanaPeel bananaPrefab;

        void Start()
        {
            ec = base.transform.parent.parent.gameObject.GetComponent<RoomController>().ec;
            bananaPrefab = Resources.FindObjectsOfTypeAll<ITM_NanaPeel>().First(x => x.name == "NanaPeel");
            ITM_NanaPeel iTM_NanaPeel = Object.Instantiate(bananaPrefab);
            if (ec.ContainsCoordinates(base.transform.position) && ec.CellFromPosition(base.transform.position).room == ec.CellFromPosition(base.transform.position).room)
            {
                iTM_NanaPeel.Spawn(ec, base.transform.position, Vector3.zero, 0f);
            }
        }
    }
}
