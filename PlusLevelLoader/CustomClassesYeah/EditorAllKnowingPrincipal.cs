using System;
using System.Collections;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    internal class EditorAllKnowingPrincipal : Principal
    {
        private PlayerManager targetedPlayer;

        [SerializeField]
        private DetentionUi detentionUiPre;

        private DetentionUi detentionUi;

        [SerializeField]
        private AudioManager audMan;

        [SerializeField]
        private SoundObject[] audTimes = new SoundObject[0];

        [SerializeField]
        private SoundObject[] audScolds = new SoundObject[0];

        [SerializeField]
        private SoundObject audNoRunning;

        [SerializeField]
        private SoundObject audNoDrinking;

        [SerializeField]
        private SoundObject audNoEating;

        [SerializeField]
        private SoundObject audNoFaculty;

        [SerializeField]
        private SoundObject audNoStabbing;

        [SerializeField]
        private SoundObject audNoBullying;

        [SerializeField]
        private SoundObject audNoEscaping;

        [SerializeField]
        private SoundObject audNoLockers;

        [SerializeField]
        private SoundObject audNoAfterHours;

        [SerializeField]
        private SoundObject audDetention;

        [SerializeField]
        private SoundObject audWhistle;

        [SerializeField]
        private SoundObject audComing;

        private System.Random whistleRng = new System.Random();

        [SerializeField]
        private float detentionInit = 15f;

        [SerializeField]
        private float detentionInc = 5f;

        [SerializeField]
        private float whistleChance = 2f;

        [SerializeField]
        private float whistleSpeed = 200f;

        [SerializeField]
        private float knockPauseTime = 3f;

        private float defaultSpeed;

        private float[] timeInSight = new float[0];

        [SerializeField]
        [Range(0f, 127f)]
        private int detentionNoise = 95;

        private int detentionLevel;

        [SerializeField]
        private bool allKnowing;

        private StandardDoor lastKnockedDoor;

        public override void Initialize()
        {
            base.Initialize();
            Singleton<CoreGameManager>.Instance.GetPlayer(0).RuleBreak("AfterHours", 1000000f);
            Singleton<CoreGameManager>.Instance.GetPlayer(0).ec.map.AddArrow(base.transform, Color.gray);
            timeInSight = new float[players.Count];
            defaultSpeed = navigator.maxSpeed;
            behaviorStateMachine.ChangeState(new Principal_Wandering(this));
        }

        public void ObservePlayer(PlayerManager player)
        {
            if (!player.Disobeying || player.Tagged)
            {
                return;
            }
            timeInSight[player.playerNumber] += Time.deltaTime * base.TimeScale;
            if (timeInSight[player.playerNumber] >= player.GuiltySensitivity)
            {
                if (!allKnowing)
                {
                    behaviorStateMachine.ChangeState(new Principal_ChasingPlayer(this, player));
                }
                else
                {
                    behaviorStateMachine.ChangeState(new Principal_ChasingPlayer_AllKnowing(this, player));
                }
                targetedPlayer = player;
                Scold(player.ruleBreak);
            }
        }

        public void LoseTrackOfPlayer(PlayerManager player)
        {
            timeInSight[player.playerNumber] = 0f;
        }

        public void FacultyDoorHit(StandardDoor door, Cell otherSide)
        {
            if (lastKnockedDoor != door)
            {
                KnockOnDoor(door, otherSide);
            }
            else
            {
                door.OpenTimedWithKey(door.DefaultTime, makeNoise: false);
            }
            lastKnockedDoor = door;
        }

        public void KnockOnDoor(StandardDoor door, Cell otherSide)
        {
            door.Knock();
            NavigationState_DoNothing navigationState_DoNothing = new NavigationState_DoNothing(this, 0);
            navigationStateMachine.ChangeState(navigationState_DoNothing);
            navigationState_DoNothing.priority = -1;
            StopAllCoroutines();
            StartCoroutine(UnpauseAfterKnock(door, knockPauseTime, otherSide));
        }

        private IEnumerator UnpauseAfterKnock(StandardDoor door, float time, Cell otherSide)
        {
            while (time > 0f)
            {
                time -= Time.deltaTime * base.TimeScale;
                yield return null;
            }
            navigationStateMachine.ChangeState(new NavigationState_TargetPosition(this, -1, otherSide.FloorWorldPosition));
            if (Vector3.Distance(base.transform.position, door.CenteredPosition) <= 5f)
            {
                door.OpenTimedWithKey(door.DefaultTime, makeNoise: false);
            }
        }

        public void SendToDetention()
        {
            if (ec.offices.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, ec.offices.Count);
                targetedPlayer.Teleport(ec.RealRoomMid(ec.offices[index]));
                targetedPlayer.ClearGuilt();
                base.transform.position = targetedPlayer.transform.position + targetedPlayer.transform.forward * 10f;
                float time = detentionInit + detentionInc * (float)detentionLevel;
                if (detentionLevel >= audTimes.Length - 1)
                {
                    time = 99f;
                }
                ec.offices[index].functionObject.GetComponent<DetentionRoomFunction>().Activate(time, ec);
                audMan.QueueAudio(audTimes[detentionLevel]);
                audMan.QueueAudio(audDetention);
                audMan.QueueAudio(audScolds[UnityEngine.Random.Range(0, audScolds.Length)]);
                timeInSight[targetedPlayer.playerNumber] = 0f;
                if (detentionUi != null)
                {
                    UnityEngine.Object.Destroy(detentionUi.gameObject);
                }
                detentionUi = UnityEngine.Object.Instantiate(detentionUiPre);
                detentionUi.Initialize(Singleton<CoreGameManager>.Instance.GetCamera(targetedPlayer.playerNumber).canvasCam, time, ec);
                detentionLevel = Mathf.Min(detentionLevel + 1, audTimes.Length - 1);
                ec.GetBaldi()?.ClearSoundLocations();
                ec.MakeNoise(targetedPlayer.transform.position, detentionNoise);
                behaviorStateMachine.ChangeState(new Principal_Detention(this, 3f));
            }
        }

        public void Scold(string brokenRule)
        {
            audMan.FlushQueue(endCurrent: true);
            if (brokenRule != null)
            {
                switch (brokenRule)
                {
                    case "Running":
                        audMan.QueueAudio(audNoRunning);
                        break;
                    case "Faculty":
                        audMan.QueueAudio(audNoFaculty);
                        break;
                    case "Drinking":
                        audMan.QueueAudio(audNoDrinking);
                        break;
                    case "Escaping":
                        audMan.QueueAudio(audNoEscaping);
                        break;
                    case "Lockers":
                        audMan.QueueAudio(audNoLockers);
                        break;
                    case "AfterHours":
                        audMan.QueueAudio(audNoAfterHours);
                        break;
                    case "Bullying":
                        audMan.QueueAudio(audNoBullying);
                        break;
                }
            }
        }

        public void WhistleReact(Vector3 target)
        {
            behaviorStateMachine.ChangeState(new Principal_WhistleApproach(this, behaviorStateMachine.currentState, target));
            navigator.SetSpeed(whistleSpeed);
            audMan.FlushQueue(endCurrent: true);
            audMan.PlaySingle(audComing);
        }

        public void WhistleReached()
        {
            navigator.maxSpeed = defaultSpeed;
        }

        public void WhistleChance()
        {
            if (whistleRng.NextDouble() < (double)whistleChance && !audMan.QueuedAudioIsPlaying)
            {
                audMan.PlaySingle(audWhistle);
            }
        }
    }
}
