using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorStoreRoomFunction : RoomFunction
    {
        public bool purchasedItem;

        public override void OnGenerationFinished()
        {
            base.OnGenerationFinished();
            if (GameObject.Find("JohnnyBase(Clone)") != null)
                johnnyAudioManager = GameObject.Find("JohnnyBase(Clone)").GetComponent<PropagatedAudioManagerAnimator>();

            if (johnnyAudioManager != null)
                johnnyAudioManager.soundQueue = new SoundObject[1];
        }

        PropagatedAudioManagerAnimator johnnyAudioManager;

        private SoundObject jonIntro1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Hi").First();
        private SoundObject jonIntro2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_Welcome").First();

        public override void OnPlayerEnter(PlayerManager player)
        {
            base.OnPlayerEnter(player);
            Singleton<CoreGameManager>.Instance.GetHud(player.playerNumber).PointsAnimator.ShowDisplay(val: true);

            if (johnnyAudioManager != null)
            {
                johnnyAudioManager.QueueAudio(jonIntro1);
                johnnyAudioManager.QueueAudio(jonIntro2);
            }

            /*foreach (PropagatedAudioManagerAnimator johnnyAudioManager in johnnyAudioManagers)
            {
                if (johnnyAudioManager != null)
                {
                    if (johnnyAudioManager.transform.parent.name == "JohnnyBase(Clone)")
                    {
                        Instantiate(johnnyAudioManager);
                        johnnyAudioManager.QueueAudio(jonIntro1);
                        johnnyAudioManager.QueueAudio(jonIntro2);
                    }
                }
            }*/
        }

        private SoundObject jonSatisfied1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_ThankYou").First();
        private SoundObject jonSatisfied2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_FaveCustomer").First();
        private SoundObject jonSatisfied3 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_SayHi").First();

        private SoundObject jonNotSatisfied1 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_SeeLater").First();
        private SoundObject jonNotSatisfied2 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_ThanksNot").First();
        private SoundObject jonNotSatisfied3 = Resources.FindObjectsOfTypeAll<SoundObject>().Where(x => x.name == "Jon_DidntWant").First();

        public override void OnPlayerExit(PlayerManager player)
        {
            base.OnPlayerExit(player);
            Singleton<CoreGameManager>.Instance.GetHud(player.playerNumber).PointsAnimator.ShowDisplay(val: false);
            if (johnnyAudioManager != null)
            {
                if (purchasedItem)
                {
                    johnnyAudioManager.QueueRandomAudio(new SoundObject[] { jonSatisfied1, jonSatisfied2, jonSatisfied3 });
                    purchasedItem = false;
                }
                else
                {
                    johnnyAudioManager.QueueRandomAudio(new SoundObject[] { jonNotSatisfied1, jonNotSatisfied2, jonNotSatisfied3 });
                }
            }
        }
    }
}
