using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorPriceTag_Map : EnvironmentObject
    {
        PropagatedAudioManagerAnimator johnnyAudioManager;

        private ItemObject mapItem = Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Map").First();

        public override void LoadingFinished()
        {
            if (GameObject.Find("JohnnyBase(Clone)") != null)
                johnnyAudioManager = GameObject.Find("JohnnyBase(Clone)").GetComponent<PropagatedAudioManagerAnimator>();

            EnvironmentController ec = base.transform.parent.parent.gameObject.GetComponent<RoomController>().ec;
            Vector3 pos = base.transform.position + base.transform.forward * 1.5f;

            Pickup pickup = ec.CreateItem(base.transform.parent.parent.gameObject.GetComponent<RoomController>(), Singleton<CoreGameManager>.Instance.NoneItem, new Vector2(pos.x, pos.z)); //(Vector3.forward * 4.99f) + (Vector3.up * 5f)
            pickup.showDescription = true;
            pickup.OnItemPurchased += OnStoreItemPurchased;
            pickup.OnItemDenied += OnStoreItemDenied;
            pickup.AssignItem(mapItem);
            pickup.free = false;
            pickup.price = 150;
            base.gameObject.GetComponent<PriceTag>().SetText(pickup.price.ToString());
        }

        private SoundObject jonInfoMap = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_InfoMapShort").First();

        private SoundObject jonBuyMap1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_MapFilled").First();
        private SoundObject jonBuyMap2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_MapFilled2").First();

        void OnStoreItemPurchased(Pickup pickup, int player)
        {
            base.gameObject.GetComponent<PriceTag>().SetText("SOLD");
            pickup.free = true;

            if (johnnyAudioManager != null)
                johnnyAudioManager.QueueRandomAudio(new SoundObject[] { jonBuyMap1, jonBuyMap2 });

            //currentSceneObject = CustomLevelLoader.sceneObject;

            Singleton<BaseGameManager>.Instance.CompleteMapOnReady();

            foreach (EditorStoreRoomFunction editorFunction in GameObject.FindObjectsOfType<EditorStoreRoomFunction>())
            {
                editorFunction.purchasedItem = true;
            }
        }

        private SoundObject jonDeny1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_TooMuch1").First();
        private SoundObject jonDeny2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_TooMuch2").First();
        private SoundObject jonDeny3 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_TooMuch3").First();

        void OnStoreItemDenied(Pickup pickup, int player)
        {
            if (johnnyAudioManager != null)
                johnnyAudioManager.QueueRandomAudio(new SoundObject[] { jonDeny1, jonDeny2, jonDeny3 });
        }
    }
}