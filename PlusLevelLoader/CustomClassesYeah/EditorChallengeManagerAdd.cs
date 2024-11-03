using MTM101BaldAPI.Reflection;
using MTM101BaldAPI;
using System.Linq;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    internal class EditorChallengeManagerAdd : EnvironmentObject
    {
        public enum ChallengeTypeThing
        {
            Speedy,
            Stealthy,
            Grapple
        }

        public ChallengeTypeThing challengeToUse;

        public override void LoadingFinished()
        {
            base.LoadingFinished();
            if (challengeToUse == ChallengeTypeThing.Speedy)
            {
                StartCoroutine(base.gameObject.AddComponent<EditorSpeedyChallengeManager>().Initialize(ec));
            }
        }
    }
}
