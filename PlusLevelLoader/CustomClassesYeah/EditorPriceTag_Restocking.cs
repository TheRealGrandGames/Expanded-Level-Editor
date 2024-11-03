using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorPriceTag_Restocking : EnvironmentObject
    {
        PropagatedAudioManagerAnimator johnnyAudioManager;

        ItemObject[] itemObjects;

        public override void LoadingFinished()
        {
            if (GameObject.Find("JohnnyBase(Clone)") != null)
                johnnyAudioManager = GameObject.Find("JohnnyBase(Clone)").GetComponent<PropagatedAudioManagerAnimator>();

            itemObjects = new ItemObject[]
            {
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "AlarmClock").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Boots").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Bsoda").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "ChalkEraser").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "DetentionKey").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "GrapplingHook").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "NoSquee").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Quarter").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Zesty").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "SwingDoorLock").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Tape").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Nametag").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Safety Scissors").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Apple").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "Teleporter").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "PortalPoster").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "NanaPeel").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "PrincipalWhistle").First(),
                Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == "DietBsoda").First()
            };

            Restock();
        }

        private int notebooksPerReset = 3;

        private int notebooksAtLastReset;

        private bool previousItemPurchased = false;

        private void Update()
        {
            if (Singleton<BaseGameManager>.Instance.FoundNotebooks - notebooksAtLastReset >= notebooksPerReset)
            {
                notebooksAtLastReset = Singleton<BaseGameManager>.Instance.FoundNotebooks;

                if (previousItemPurchased)
                {
                    Restock();
                    previousItemPurchased = false;
                }
            }
        }

        void Restock()
        {
            EnvironmentController ec = base.transform.parent.parent.gameObject.GetComponent<RoomController>().ec;
            Vector3 pos = base.transform.position + base.transform.forward * 1.5f;

            Pickup pickup = ec.CreateItem(base.transform.parent.parent.gameObject.GetComponent<RoomController>(), Singleton<CoreGameManager>.Instance.NoneItem, new Vector2(pos.x, pos.z)); //(Vector3.forward * 4.99f) + (Vector3.up * 5f)
            pickup.showDescription = true;
            pickup.OnItemPurchased += OnStoreItemPurchased;
            pickup.OnItemDenied += OnStoreItemDenied;
            ItemObject itemObject;
            itemObject = itemObjects[UnityEngine.Random.Range(0, itemObjects.Length)];
            pickup.AssignItem(itemObject);
            pickup.free = false;
            pickup.price = itemObject.price / 2;
            int actualPrice = itemObject.price / 2;
            base.gameObject.GetComponent<PriceTag>().SetText(actualPrice.ToString());
        }

        private SoundObject jonBuy1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy1").First();
        private SoundObject jonBuy2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy2").First();
        private SoundObject jonBuy3 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy3").First();
        private SoundObject jonBuy4 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy4").First();
        private SoundObject jonBuy5 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy5").First();
        private SoundObject jonBuy6 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Buy6").First();

        private SoundObject jonInfoMap = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_InfoMapShort").First();

        private SoundObject jonBuyMap1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_MapFilled").First();
        private SoundObject jonBuyMap2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_MapFilled2").First();

        void OnStoreItemPurchased(Pickup pickup, int player)
        {
            previousItemPurchased = true;

            base.gameObject.GetComponent<PriceTag>().SetText("SOLD");
            pickup.free = true;

            if (johnnyAudioManager != null)
                johnnyAudioManager.QueueRandomAudio(new SoundObject[] { jonBuy1, jonBuy2, jonBuy3, jonBuy4, jonBuy5, jonBuy6 });


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