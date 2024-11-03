using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorRandomEvent : MonoBehaviour
    {
        EnvironmentController ec;

        RandomEvent theRandomEvent;
        float eventTime; //Was a float

        public enum RandomEventTypeThing
        {
            Fog,
            GravityChaos,
            Flood,
            BrokenRuler,
            Party,

            MysteryRoom,
            Lockdown
        }

        public RandomEventTypeThing randomEventToUse;

        void Start()
        {
            StartCoroutine(AddARandomEvent());
        }

        IEnumerator AddARandomEvent()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(90, 180));
            //yield return new WaitForSeconds(0); //For testing

            ec = base.transform.parent.parent.gameObject.GetComponent<RoomController>().ec;

            if (randomEventToUse == RandomEventTypeThing.Fog)
            {
                eventTime = UnityEngine.Random.Range(30f, 60f);
                theRandomEvent = Instantiate(Resources.FindObjectsOfTypeAll<FogEvent>().Where(x => x.name == "Event_Fog").First()); //Was using Instantiate().
            }
            else if (randomEventToUse == RandomEventTypeThing.Flood)
            {
                eventTime = UnityEngine.Random.Range(60f, 90f);
                theRandomEvent = Instantiate(Resources.FindObjectsOfTypeAll<FloodEvent>().Where(x => x.name == "Event_Flood").First()); //Was using Instantiate().
            }
            else if (randomEventToUse == RandomEventTypeThing.GravityChaos)
            {
                eventTime = UnityEngine.Random.Range(60f, 90f);
                theRandomEvent = Instantiate(Resources.FindObjectsOfTypeAll<GravityEvent>().Where(x => x.name == "Event_GravityChaos").First()); //Was using Instantiate().
            }
            else if (randomEventToUse == RandomEventTypeThing.BrokenRuler)
            {
                eventTime = UnityEngine.Random.Range(40f, 80f);
                theRandomEvent = Instantiate(Resources.FindObjectsOfTypeAll<RulerEvent>().Where(x => x.name == "Event_BrokenRuler").First()); //Was using Instantiate().
            }
            /*else if (randomEventToUse == RandomEventTypeThing.Party)
            {
                eventTime = UnityEngine.Random.Range(50f, 80f);
                theRandomEvent = Instantiate(Resources.FindObjectsOfTypeAll<PartyEvent>().Where(x => x.name == "Event_Party").First()); //Was using Instantiate().
                FieldInfo balloons = AccessTools.Field(typeof(PartyEvent), "balloon");
                balloons.SetValue(AccessTools.Field(typeof(PartyEvent), "balloon"), Resources.FindObjectsOfTypeAll<Balloon>());
            }*/

            //AddRandomEvent(theRandomEvent, UnityEngine.Random.Range(30f, 60f));

            theRandomEvent.Initialize(ec, new System.Random());

            theRandomEvent.PremadeSetup();
            theRandomEvent.SetEventTime(new System.Random());
            ec.AddEvent(theRandomEvent, eventTime);
            ec.StartEventTimers();

            //RandomizeEvents(int numberOfEvents, float initialGap, float minGap, float maxGap, System.Random cRng)
            ec.RandomizeEvents(ec.EventsCount, 0f, 180f, 300f, new System.Random()); //initialGap was 120f
        }
    }
}
