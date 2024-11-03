using MTM101BaldAPI;
using System.Collections;
using UnityEngine;

namespace PlusLevelLoader.Custom
{
    public class EditorSpeedyChallengeManager : MonoBehaviour //: BaseGameManager
    {
        public IEnumerator Initialize(EnvironmentController ec)
        {
            yield return new WaitForSecondsEnviromentTimescale(ec, 0.1f);
            InitializeVoid(ec);
        }

        public void InitializeVoid(EnvironmentController ec)
        {
            //base.Initialize();
            gameObject.SetActive(true);
            Singleton<CoreGameManager>.Instance.GetPlayer(0).plm.walkSpeed = 70f;
            Singleton<CoreGameManager>.Instance.GetPlayer(0).plm.runSpeed = 100f;
            ec.map.CompleteMap();
            //Singleton<BaseGameManager>.Instance.notebookAngerVal = 0f;
        }

        /*protected override void ExitedSpawn()
        {
            base.ExitedSpawn();
            ec.GetBaldi().GetAngry(42f);
        }

        public override void LoadNextLevel()
        {
            Singleton<CoreGameManager>.Instance.Quit();
        }*/

        /*public override void LoadNextLevel()
        {
            AudioListener.pause = true;
            Time.timeScale = 0f;
            Singleton<CoreGameManager>.Instance.disablePause = true;
            winScreen.gameObject.SetActive(value: true);
        }*/
    }
}