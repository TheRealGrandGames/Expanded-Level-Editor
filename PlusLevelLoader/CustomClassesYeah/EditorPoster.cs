using System.Linq;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorPoster : MonoBehaviour
    {
        private EnvironmentController ec;

        public PosterObject posterObject;

        private Direction direction;

        public bool inEditor;

        void Start()
        {
            Direction dir = Direction.Null;
            switch (base.transform.rotation.eulerAngles.y)
            {
                case 0: dir = Direction.North; break;
                case 90: dir = Direction.East; break;
                case 180: dir = Direction.South; break;
                case 270: dir = Direction.West; break;
                default: break;//Invalid direction treatment should be here
            }

            direction = dir;

            ec = base.transform.parent.parent.gameObject.GetComponent<RoomController>().ec;

            ec.BuildPoster(posterObject, ec.CellFromPosition(base.transform.position), direction);

            Destroy(base.gameObject);
        }
    }
}
