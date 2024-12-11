using BaldiLevelEditor.UI;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.UI;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Rewired.Controller;

namespace BaldiLevelEditor
{

    public class EditorTrackCursor : MonoBehaviour
    {
        Vector3 initLocal;
        void Start()
        {
            initLocal = transform.localPosition;
        }

        void Update()
        {
            if (Singleton<PlusLevelEditor>.Instance.cursor == null) return;
            if (Singleton<PlusLevelEditor>.Instance.selectedTool != null)
            {
                transform.position = Singleton<PlusLevelEditor>.Instance.cursor.cursorTransform.position;
                return;
            }
            transform.localPosition = initLocal;
            Destroy(this);
        }
    }

    public class ToolIconManager : MonoBehaviour
    {
        private bool _active;

        public bool active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
                gameObject.tag = _active ? "Button" : "Untagged";
            }
        }
    }

    public class CategoryManager : MonoBehaviour
    {
        public List<ToolIconManager> toAnimate = new List<ToolIconManager>();
        public List<ToolIconManager> allTools = new List<ToolIconManager>();
        public ToolIconManager pageButton;
        public ToolIconManager pageButtonNoOtherButtons;
        public ToolIconManager pageButtonBack;
        public ToolIconManager pageButtonFirst;
        public int page;

        public IEnumerator AnimateIcon(ToolIconManager manager, Vector3 start, Vector3 end, float time, bool activeState)
        {
            manager.active = false;
            Transform transform = manager.transform.parent;
            if (transform == this.transform.parent)
            {
                transform = manager.transform;
            }
            float currentTime = 0f;
            while (currentTime < 1f)
            {
                currentTime += (Time.deltaTime / time);
                Vector3 lerped = Vector3.Lerp(start, end, currentTime);
                transform.localPosition = new Vector3(Mathf.Round(lerped.x), Mathf.Round(lerped.y), Mathf.Round(lerped.z));
                yield return null;
            }
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/UIClunk"));
            manager.active = activeState;
            yield break;
        }

        public void Initialize()
        {
            allTools.Do(x => x.active = false);
            toAnimate.Clear();
            for (int i = page * 7; i < Mathf.Min((page + 1) * 7, allTools.Count); i++)
            {
                toAnimate.Add(allTools[i]);
            }
        }

        bool doingSwitchAnimation = false;

        public void SwitchPage()
        {
            doingSwitchAnimation = true;
            StartCoroutine(CollapseInAndOut());
        }

        IEnumerator CollapseInAndOut()
        {
            float speed = 0.5f;
            ExpandViaPage(false, speed);
            yield return new WaitForSeconds((0.20f + (toAnimate.Count * 0.05f)) * speed);
            Initialize();
            ExpandViaPage(true, speed);
            yield return new WaitForSeconds((0.20f + (toAnimate.Count * 0.05f)) * speed);
            doingSwitchAnimation = false;
            yield break;
        }

        public bool isOut = false;
        public void OnClick()
        {
            if (doingSwitchAnimation) return;
            isOut = !isOut;
            StopAllCoroutines();
            ExpandViaPage(isOut);
        }

        public void ExpandViaPage(bool expand, float speed = 1f)
        {
            int i;
            Vector3 toMoveTo;
            for (i = 0; i < toAnimate.Count; i++)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 1), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(toAnimate[i], toAnimate[i].transform.parent.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
            if (pageButton != null)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 3), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(pageButton, pageButton.transform.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
            if (pageButtonNoOtherButtons != null)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 1), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(pageButtonNoOtherButtons, pageButtonNoOtherButtons.transform.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
            if (pageButtonBack != null)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 2), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(pageButtonBack, pageButtonBack.transform.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
            if (pageButtonFirst != null)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 1), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(pageButtonFirst, pageButtonFirst.transform.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
        }
    }

    public struct ToolCategory
    {
        public string name;
        public Sprite sprite;
        public List<EditorTool> tools;

        public ToolCategory(string name, Sprite sprite, params EditorTool[] tools)
        {
            this.name = name;
            this.sprite = sprite;
            this.tools = tools.ToList();
        }
    }

    public partial class PlusLevelEditor : Singleton<PlusLevelEditor>
    {

        public CustomImageAnimator gearAnimator;

        /*public List<EditorTool> tools = new List<EditorTool>()
        {
            new FloorTool("hall"),
            new FloorTool("class"),
            new FloorTool("faculty"),
            new FloorTool("office"),
            new DoorTool(),
            new WindowTool(),
            new MergeTool(),
            new DeleteTool()
        };*/

        public List<ToolCategory> toolCats = new List<ToolCategory>()
        {
            new ToolCategory("halls", GetUISprite("cat_floors"),
            new FloorTool("hall"),
            //new FloorTool("hall_carpet"),
            //new FloorTool("hall_tiles"),
            new FloorTool("hall_saloonwall"),
            //new FloorTool("hall_saloonwall_carpet"),
            //new FloorTool("hall_saloonwall_tiles"),
            new FloorTool("class"),
            new FloorTool("faculty"),
            new FloorTool("office"),
            new FloorTool("closet"),
            new FloorTool("closet_carpet"),
            new FloorTool("reflex"),
            new FloorTool("shop"),
            new FloorTool("library"),
            new FloorTool("cafeteria"),
            new FloorTool("outside"),
            //new FloorTool("outside_camphub"),
            new FloorTool("nullplaceholder")),
            new ToolCategory("doors", GetUISprite("cat_doors"),
            new DoorTool("standard"),
            new SwingingDoorTool("swing"),
            new SwingingDoorTool("swingsilent"),
            new SwingingDoorTool("coin"),
            new SwingingDoorTool("oneway"),
            new SwingingDoorTool("autodoor"),
            new WindowTool(),
            new WallTool(true),
            new WallTool(false)),

            new ToolCategory("objects", GetUISprite("cat_decor"),
            new RotateAndPlacePrefab("waterfountain"),
            new RotateAndPlacePrefab("dietbsodamachine"),
            new RotateAndPlacePrefab("bsodamachine"),
            new RotateAndPlacePrefab("zestymachine"),
            new RotateAndPlacePrefab("crazymachine_bsoda"),
            new RotateAndPlacePrefab("crazymachine_zesty"),
            new ObjectTool("payphone"),
            new ObjectTool("tapeplayer"),
            new RotateAndPlacePrefab("locker"),
            new RotateAndPlacePrefab("bluelocker"),
            new RotateAndPlacePrefab("greenlocker"),
            new PrebuiltStructureTool("bulklocker", new EditorPrebuiltStucture(
                new PrefabLocation("locker", new UnityVector3(-4f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(-2f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(0f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(2f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(4f,0f,4f)))),
            new PrebuiltStructureTool("bulklockerwithblue", new EditorPrebuiltStucture(
                new PrefabLocation("locker", new UnityVector3(-4f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(-2f,0f,4f)),
                new PrefabLocation("bluelocker", new UnityVector3(0f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(2f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(4f,0f,4f)))),
            new PrebuiltStructureTool("chairdesk", new EditorPrebuiltStucture(
                new PrefabLocation("chair", new UnityVector3(0f,0f,-2f)),
                new PrefabLocation("desk", new UnityVector3(0f,0f,0f)))),
            new RotateAndPlacePrefab("desk"),
            new RotateAndPlacePrefab("chair"),
            new RotateAndPlacePrefab("bigdesk"),
            new ObjectTool("roundtable"),
            new PrebuiltStructureTool("roundtable1", new EditorPrebuiltStucture(
                new PrefabLocation("roundtable", new UnityVector3(0f,0f,0f)),
                new PrefabLocation("chair", new UnityVector3(0f,0f,-5f)),
                new PrefabLocation("chair", new UnityVector3(-5f,0f,0f), Quaternion.Euler(0f, 90f, 0f).ToData()),
                new PrefabLocation("chair", new UnityVector3(0f,0f,5f), Quaternion.Euler(0f, 180f, 0f).ToData()),
                new PrefabLocation("chair", new UnityVector3(5f,0f,0f), Quaternion.Euler(0f, 270f, 0f).ToData())
                )),
            new PrebuiltStructureTool("roundtable2", new EditorPrebuiltStucture(
                new PrefabLocation("roundtable", new UnityVector3(0f,0f,0f)),
                new PrefabLocation("chair", new UnityVector3(3.5355f,0f,-3.535f), Quaternion.Euler(0f, 315f, 0f).ToData()),
                new PrefabLocation("chair", new UnityVector3(-3.5355f,0f,-3.535f), Quaternion.Euler(0f, 45f, 0f).ToData()),
                new PrefabLocation("chair", new UnityVector3(-3.5355f,0f,3.535f), Quaternion.Euler(0f, 135f, 0f).ToData()),
                new PrefabLocation("chair", new UnityVector3(3.5355f,0f,3.535f), Quaternion.Euler(0f, 225f, 0f).ToData())
                )),
            new RotateAndPlacePrefab("cafeteriatable"),
            new ObjectTool("nanapeelplaced"),
            new RotateAndPlacePrefab("picnictable"),
            new ObjectTool("rock"),
            new ObjectTool("plant"),
            new PrebuiltStructureTool("plantagainstwall", new EditorPrebuiltStucture(
                new PrefabLocation("plant", new UnityVector3(0f,0f,4f))
                )),
            new ObjectTool("decor_pencilnotes"),
            new ObjectTool("decor_papers"),
            new ObjectTool("decor_globe"),
            new ObjectTool("decor_notebooks"),
            new ObjectTool("decor_banana"),
            new ObjectTool("decor_lunch"),
            new ObjectTool("decor_zoneflag"),
            new RotateAndPlacePrefab("cabinettall"),
            new RotateAndPlacePrefab("cabinetshort"),
            new RotateAndPlacePrefab("computer"),
            new RotateAndPlacePrefab("computer_off"),
            new ObjectTool("ceilingfan"),
            new RotateAndPlacePrefab("bookshelf"),
            new RotateAndPlacePrefab("bookshelf_hole"),
            new RotateAndPlacePrefab("rounddesk"),
            new RotateAndPlacePrefab("pricetag"),
            /*new PrebuiltStructureTool("pricetagoncounter", new EditorPrebuiltStucture(
                new PrefabLocation("counter", new UnityVector3(0f,-3.75f,0f)),
                new PrefabLocation("pricetag", new UnityVector3(0f,0f,0f)))),*/
            new RotateAndPlacePrefab("pricetagmap"),
            new RotateAndPlacePrefab("pricetagout"),
            new RotateAndPlacePrefab("pricetagrestocking"),
            new RotateAndPlacePrefab("cashregister"),
            new ObjectTool("johnnysign"),
            new RotateAndPlacePrefab("counter"),
            new RotateAndPlacePrefab("examination"),
            new ObjectTool("merrygoround"),
            new RotateAndPlacePrefab("bus"),
            new RotateAndPlacePrefab("busbaldi"),
            new ObjectTool("tree"),
            new ObjectTool("appletree"),
            new ObjectTool("bananatree"),
            new ObjectTool("pinetree"),
            new ObjectTool("pinetreebully"),
            new ObjectTool("campfire"),
            //new ObjectTool("minigame1trigger"),
            new ObjectTool("picnicbasket"),
            //new ObjectTool("platter"),
            new RotateAndPlacePrefab("tent"),
            new RotateAndPlacePrefab("dirtcircle"),
            new RotateAndPlacePrefab("prizestatic"),
            //new ObjectTool("minigame2trigger"),
            new RotateAndPlacePrefab("hopscotch"),
            new RotateAndPlacePrefab("hoop"),
            new ObjectTool("balloon_blue"),
            new ObjectTool("balloon_green"),
            new ObjectTool("balloon_orange"),
            new ObjectTool("balloon_purple"),
            new ObjectTool("fluorescentlight"),
            new ObjectTool("hanginglight"),
            new ObjectTool("decor_exitsign")),
            new ToolCategory("activities", GetUISprite("cat_activities"),
            new ActivityTool("notebook"),
            /*new PrebuiltStructureTool("notebookondesk", new EditorPrebuiltStucture(
                new PrefabLocation("bigdesk", new UnityVector3(0f,0f,0f)),
                new PrefabLocation("notebook", new UnityVector3(0f,5f,0f)))),*/
            new ActivityTool("mathmachine"),
            new ActivityTool("mathmachine_corner")),
            new ToolCategory("characters", GetUISprite("cat_npcs"),
            new NpcTool("baldi"),
            new NpcTool("principal"),
            new NpcTool("sweep"),
            new NpcTool("playtime"),
            new NpcTool("bully"),
            new NpcTool("crafters"),
            new NpcTool("prize"),
            new NpcTool("cloudy"),
            new NpcTool("chalkface"),
            new NpcTool("beans"),
            new NpcTool("pomp"),
            new NpcTool("test"),
            new NpcTool("reflex"),
            new RotateAndPlacePrefab("johnny")
            //new NpcTool("fastbaldi"),
            /*new NpcTool("principal_allknowing")*/),
            new ToolCategory("items", GetUISprite("cat_item"),
            new ItemTool("quarter"),
            new ItemTool("dietbsoda"),
            new ItemTool("bsoda"),
            new ItemTool("zesty"),
            new ItemTool("banana"),
            new ItemTool("scissors"),
            new ItemTool("boots"),
            new ItemTool("nosquee"),
            new ItemTool("keys"),
            new ItemTool("tape"),
            new ItemTool("clock"),
            new ItemTool("swinglock"),
            new ItemTool("whistle"),
            new ItemTool("dirtychalk"),
            new ItemTool("nametag"),
            new ItemTool("teleporter"),
            new ItemTool("portalposter"),
            new ItemTool("grapple"),
            new ItemTool("apple"),
            new ItemTool("buspass"),
            new ItemTool("points25"),
            new ItemTool("points50"),
            new ItemTool("points100")),
            new ToolCategory("connectables", GetUISprite("cat_connectables"),
            new DisabledEditorTool("Button_button"),
            new DisabledEditorTool("Tile_lockdowndoor"),
            new RotateAndPlacePrefab("conveyorbelt"),
            //new RotateAndPlacePrefab("shrinkmachine"),

            new ObjectTool("randomevent_fog"),
            new ObjectTool("randomevent_flood"),
            new ObjectTool("randomevent_gravitychaos"),
            new ObjectTool("randomevent_brokenruler")//,

            //new ObjectTool("challenge_speedy"),
            //new ObjectTool("challenge_stealthy"),
            //new ObjectTool("challenge_grapple")//,
            /*new ObjectTool("randomevent_party")*/),

            new ToolCategory("posters", GetUISprite("cat_poster"),
            new RotateAndPlacePrefab("poster_pri_baldi"),
            new RotateAndPlacePrefab("poster_pri_principal"),
            new RotateAndPlacePrefab("poster_pri_sweep"),
            new RotateAndPlacePrefab("poster_pri_playtime"),
            new RotateAndPlacePrefab("poster_pri_bully"),
            new RotateAndPlacePrefab("poster_pri_crafters"),
            new RotateAndPlacePrefab("poster_pri_prize"),
            new RotateAndPlacePrefab("poster_pri_cloud"),
            new RotateAndPlacePrefab("poster_pri_chalk"),
            new RotateAndPlacePrefab("poster_pri_beans"),
            new RotateAndPlacePrefab("poster_pri_pomp"),
            new RotateAndPlacePrefab("poster_pri_test"),
            new RotateAndPlacePrefab("poster_pri_reflex"),
            new RotateAndPlacePrefab("poster_hint_boots"),
            new RotateAndPlacePrefab("poster_hint_nosquee"),
            new RotateAndPlacePrefab("poster_hint_phone"),
            new RotateAndPlacePrefab("poster_hint_read"),
            new RotateAndPlacePrefab("poster_hint_rules"),
            new RotateAndPlacePrefab("poster_hint_scissors"),
            new RotateAndPlacePrefab("poster_hint_ytps"),

            new RotateAndPlacePrefab("poster_numbers"),
            new RotateAndPlacePrefab("poster_baldiburied"),
            
            new RotateAndPlacePrefab("poster_beakid"),
            new RotateAndPlacePrefab("poster_candy"),
            new RotateAndPlacePrefab("poster_what"),

            new RotateAndPlacePrefab("poster_txt_inspiration"),
            new RotateAndPlacePrefab("poster_txt_recycle"),

            new RotateAndPlacePrefab("poster_mailcomic1"),
            new RotateAndPlacePrefab("poster_mailcomic2"),
            new RotateAndPlacePrefab("poster_mailcomic3"),
            new RotateAndPlacePrefab("poster_mailcomic4"),

            new RotateAndPlacePrefab("poster_baldisays1"),
            new RotateAndPlacePrefab("poster_baldisays2"),
            new RotateAndPlacePrefab("poster_baldisays3"),
            new RotateAndPlacePrefab("poster_baldisays4"),
            new RotateAndPlacePrefab("poster_baldisays5"),
            new RotateAndPlacePrefab("poster_baldisays6"),
            new RotateAndPlacePrefab("poster_baldisays7"),
            new RotateAndPlacePrefab("poster_baldisays8"),
            new RotateAndPlacePrefab("poster_baldisays9"),
            new RotateAndPlacePrefab("poster_baldisays_saveandquit"),

            new RotateAndPlacePrefab("poster_blt_all"),
            new RotateAndPlacePrefab("poster_blt_blah"),
            new RotateAndPlacePrefab("poster_blt_budget"),
            new RotateAndPlacePrefab("poster_blt_computer"),
            new RotateAndPlacePrefab("poster_blt_fired"),
            new RotateAndPlacePrefab("poster_blt_hawaii"),
            new RotateAndPlacePrefab("poster_blt_heykid"),
            new RotateAndPlacePrefab("poster_blt_meeting"),
            new RotateAndPlacePrefab("poster_blt_review"),

            new RotateAndPlacePrefab("poster_chk_apple"),
            new RotateAndPlacePrefab("poster_chk_baldisays"),
            new RotateAndPlacePrefab("poster_chk_chalk"),
            new RotateAndPlacePrefab("poster_chk_cheese"),
            new RotateAndPlacePrefab("poster_chk_g"),
            new RotateAndPlacePrefab("poster_chk_hangman"),
            new RotateAndPlacePrefab("poster_chk_mathh"),
            new RotateAndPlacePrefab("poster_chk_possible"),
            new RotateAndPlacePrefab("poster_chk_treehint"),
            new RotateAndPlacePrefab("poster_chk_world"),
            new RotateAndPlacePrefab("poster_chk_wow"),
            new RotateAndPlacePrefab("poster_chk_baldisboard"),

            new RotateAndPlacePrefab("poster_storeneon1"),
            new RotateAndPlacePrefab("poster_storeneon2"),
            new RotateAndPlacePrefab("poster_storeneon3"),
            new RotateAndPlacePrefab("poster_storeneon4"),
            new RotateAndPlacePrefab("poster_storeneon5"),

            new RotateAndPlacePrefab("poster_kick1"),
            new RotateAndPlacePrefab("poster_kick2"),
            new RotateAndPlacePrefab("poster_kick3"),
            new RotateAndPlacePrefab("poster_kick4"),
            new RotateAndPlacePrefab("poster_kick5"),
            new RotateAndPlacePrefab("poster_kick6"),
            new RotateAndPlacePrefab("poster_kick7"),
            new RotateAndPlacePrefab("poster_kick8")),
            new ToolCategory("utilities", GetUISprite("cat_utilities"),
            new ElevatorTool(true),
            new ElevatorTool(false),
            new DisabledEditorTool("Connect"),
            new MergeTool(),
            new DeleteTool()),
        };


        // UI
        public Transform UIWires;

        private static Sprite GetUISprite(string name)
        {
            return BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/" + name);
        }

        public void CreateDirectoryIfNoExist()
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "CustomLevels")))
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "CustomLevels"));
            }
        }

        IEnumerator WaitForAudMan()
        {
            yield return null;
            while (audMan.AnyAudioIsPlaying)
            {
                yield return null;
            }
            SceneManager.LoadScene("MainMenu");
        }

        public static T GetResourceFromName<T>(string name) where T : UnityEngine.Object
        {
            // Does a simple linear search based on the resource name
            foreach (T asset in Resources.FindObjectsOfTypeAll<T>())
            {
                // Stops the loop and returns the asset given
                if (asset.name == name)
                {
                    return asset;
                }
            }

            // Returns null if the resource with that name does not exist.
            return null;
        }

        /*public GameObject tooltipBase;
        public GameObject tooltip;
        public EditorTooltipController tooltipController;*/

        float originalScale = 1f;
        public void SpawnUI()
        {
            //Debug.Log(Mathf.Floor(canvas.scaleFactor));
            //Debug.Log(canvas.scaleFactor);
            /*if (Mathf.Floor(canvas.scaleFactor) != canvas.scaleFactor)
            {
                audMan.ReflectionSetVariable("disableSubtitles", false);
                audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/IncompatibleResolution"));
                StartCoroutine(WaitForAudMan());
                return;
            }*/

            /*tooltipBase = new GameObject();
            tooltipBase.transform.SetParent(canvas.transform);
            tooltipBase.name = "TooltipBase";
            tooltipBase.transform.localPosition = new Vector3(-240, 180, 0);
            tooltip = new GameObject();
            tooltip.name = "Tooltip";
            tooltip.layer = LayerMask.NameToLayer("UI");
            tooltip.AddComponent<RectTransform>();
            tooltip.transform.SetParent(tooltipBase.transform);
            Image tooltipBG = UIHelpers.CreateImage(Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "TooltipBG").First(), canvas.transform, Vector3.zero, false);
            tooltipBG.name = "BG";
            tooltipBG.transform.SetParent(tooltip.transform);
            GameObject tooltipText = new GameObject();
            tooltipText.name = "Tmp";
            tooltipText.transform.SetParent(tooltip.transform);
            TextMeshProUGUI tooltipTextText = tooltipText.AddComponent<TextMeshProUGUI>();
            tooltipTextText.fontSize = 12;
            tooltipTextText.font = GetResourceFromName<TMP_FontAsset>("COMIC_Small_Pro");*/



            Image anchor = UIHelpers.CreateImage(GetUISprite("Wires"), canvas.transform, Vector3.zero, false);
            originalScale = canvas.scaleFactor;
            anchor.rectTransform.pivot = new Vector2(0f, 1f);
            anchor.rectTransform.anchorMin = new Vector2(0f, 1f);
            anchor.rectTransform.anchorMax = new Vector2(0f, 1f);
            anchor.rectTransform.sizeDelta = new Vector2(anchor.rectTransform.sizeDelta.x, canvas.renderingDisplaySize.y / canvas.scaleFactor);
            anchor.rectTransform.anchoredPosition = Vector3.zero;
            //anchor.rectTransform.offsetMax = new Vector2(379f, 0f);
            //anchor.rectTransform.offsetMin = new Vector2(320f, -360f);
            UIWires = anchor.transform;
            UIWires.name = "Wires";
            for (int i = 0; i < toolCats.Count; i++)
            {
                CreateSlot(UIWires, -(20f + (40f * i)), toolCats[i]);
            }
            Sprite sprite = GetUISprite("CogsMoveScreen0");
            Image gears = UIHelpers.CreateImage(sprite, canvas.transform, Vector3.zero, false);
            gears.rectTransform.anchorMin = new Vector2(1f, 0f);
            gears.rectTransform.anchorMax = new Vector2(1f, 0f);
            gears.rectTransform.pivot = new Vector2(1f, 0f);
            gears.rectTransform.anchoredPosition = new Vector2(6f,-30f);
            gearAnimator = gears.gameObject.AddComponent<CustomImageAnimator>();
            gearAnimator.animations.Add("spin", new CustomAnimation<Sprite>(new Sprite[]
            {
                GetUISprite("CogsMoveScreen0"),
                GetUISprite("CogsMoveScreen1"),
                GetUISprite("CogsMoveScreen2"),
                GetUISprite("CogsMoveScreen3")
            },0.2f));
            gearAnimator.affectedObject = gears;

            /*StandardMenuButton testButton = UIHelpers.CreateImage(GetUISprite("UITestButton"), canvas.transform, Vector3.zero, false).gameObject.ConvertToButton<StandardMenuButton>();
            testButton.OnPress.AddListener(() =>
            {
                SwitchToMenu(new UIMenuBuilder()
                    .AddClipboard()
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Down)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddButton(GetUISprite("PlayButton"), (StandardMenuButton b) =>
                    {
                        Debug.Log(b.name);
                    },
                    NextDirection.Down)
                    .AddLabel(100f,100f,"I LOVE BURGER!", NextDirection.Down)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Left)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Down)
                    .AddTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Wall"), 0.25f, NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Up)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .Build());
            });*/

            //gearAnimator.SetDefaultAnimation("spin", 1f);
            //TextMeshProUGUI text = UIHelpers.CreateText<TextMeshProUGUI>(BaldiFonts.ComicSans12, "EVERYTHING SEEN HERE IS SUBJECT TO CHANGE!", canvas.transform,new Vector3(-165f, 150f, 0f), false);
            CreateGearButton(GetUISprite("SaveButton"), GetUISprite("SaveButtonHover"), new Vector2(45f, 65f), () =>
            {
                if (level.areas.Count == 0)
                {
                    return;
                }
                CreateDirectoryIfNoExist();
                SaveLevelAsEditor(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.bld")); // placeholder path
            }, true, () =>
            {
                if (level.areas.Count == 0)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    return;
                }
            });
            CreateGearButton(GetUISprite("LoadButton"), GetUISprite("LoadButtonHover"), new Vector2(100f, 65f), () =>
            {
                CreateDirectoryIfNoExist();
                SaveLevelAsEditor(Path.Combine(Application.persistentDataPath, "CustomLevels", "level_previous.bld")); // placeholder path
                LoadLevelFromFile(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.bld")); // placeholder path
            }, false,
            () =>
            {
                if (tempLevel == null)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    throw new Exception("Level Loading failed!");
                }
                LoadLevel(tempLevel);
                tempLevel = null;
            });
            CreateGearButton(GetUISprite("PlayButton"), GetUISprite("PlayButtonHover"), new Vector2(45f, 10f), () =>
            {
                CompileLevelAsPlayable(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.cbld"));
            }, false);
            CreateGearButton(GetUISprite("ExitButton"), GetUISprite("ExitButtonHover"), new Vector2(100f, 10f), () =>
            {
                /*CreateGearButton(GetUISprite("ConfirmButton"), GetUISprite("ConfirmButtonHover"), new Vector2(100f, 57f), () =>
                {
                    GameObject.Find("CursorOrigin(Clone)").GetComponent<CursorController>().Hide(true);
                    SceneManager.LoadScene("MainMenu");
                    Destroy(GameObject.Find("Main Camera(Clone)"));
                }, false);*/
                GameObject.Find("CursorOrigin(Clone)").GetComponent<CursorController>().Hide(true);
                SceneManager.LoadScene("MainMenu");
                Destroy(GameObject.Find("Main Camera(Clone)"));
            }, false);
            /*CreateGearButton(GetUISprite("SavePlayButton"), GetUISprite("SavePlayButtonHover"), new Vector2(-10f, 10f), () =>
            {
                if (level.areas.Count == 0)
                {
                    return;
                }
                CreateDirectoryIfNoExist();
                SaveLevelAsEditor(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.bld")); // placeholder path
                CompileLevelAsPlayable(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.cbld"));
            }, true, () =>
            {
                if (level.areas.Count == 0)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    return;
                }
            });*/

            //TooltipTextYay(new Vector2(100f, 50f));

            InitializeMenuBackground();
            UpdateCursor();
            UpdateCursor();
        }

        // do this so if saving/loading on a slow machine the game doesn't look like its crashing
        IEnumerator RunThreadAndSpinGear(Action toPerform, Action? toPerformPostThread)
        {
            gearAnimator.SetDefaultAnimation("spin", 1f);
            Thread myThread = new Thread(new ThreadStart(toPerform));
            myThread.Start();
            updateDelay = float.MaxValue;
            while (myThread.IsAlive)
            {
                yield return null;
            }
            updateDelay = 0.1f;
            gearAnimator.SetDefaultAnimation("", 0f);
            CursorController.Instance.DisableClick(false);
            if (toPerformPostThread != null)
            {
                toPerformPostThread();
            }
            yield break;
        }

        public TextMeshProUGUI editorTooltipText;

        void TooltipTextYay(Vector3 position)
        {
            editorTooltipText = UIHelpers.CreateText<TextMeshProUGUI>(BaldiFonts.ComicSans12, "", gearAnimator.transform, position, false);
        }

        void UpdateTooltipTextYay(string text)
        {
            editorTooltipText.text = text;
        }

        void CreateGearButton(Sprite sprite, Sprite highlightSprite, Vector2 position, Action toDo, bool thread, Action? postThread = null)
        {
            Image but = UIHelpers.CreateImage(sprite, gearAnimator.transform, Vector3.zero, false);
            but.rectTransform.anchorMin = new Vector2(0f, 0f);
            but.rectTransform.anchorMax = new Vector2(0f, 0f);
            but.rectTransform.pivot = new Vector2(0f, 0f);
            but.rectTransform.anchoredPosition = position - new Vector2(-6f, -30f);
            StandardMenuButton stanMen = but.gameObject.ConvertToButton<StandardMenuButton>();
            stanMen.highlightedSprite = highlightSprite;
            stanMen.unhighlightedSprite = sprite;
            stanMen.swapOnHigh = true;
            if (thread)
            {
                stanMen.OnPress.AddListener(() =>
                {
                    if (updateDelay > 0f) return;
                    CursorController.Instance.DisableClick(true);
                    StartCoroutine(RunThreadAndSpinGear(toDo, postThread));
                });

                stanMen.OnHighlight.AddListener(() =>
                {
                    UpdateTooltipTextYay("EPOOPE");
                });

                /*stanMen.OnHighlight.AddListener(() =>
                {
                    if (sprite == GetUISprite("SaveButton"))
                    {
                        tooltipController.ActualUpdateTooltip("Save the Level");
                    }
                    else if (sprite == GetUISprite("LoadButton"))
                    {
                        tooltipController.ActualUpdateTooltip("Load the Level");
                    }
                    else if (sprite == GetUISprite("PlayButton"))
                    {
                        tooltipController.ActualUpdateTooltip("Play the Level");
                    }
                    else if (sprite == GetUISprite("ExitButton"))
                    {
                        tooltipController.ActualUpdateTooltip("Exit the Level Editor");
                    }
                });
                stanMen.OnRelease.AddListener(() =>
                {
                    tooltipController.CloseTooltip();
                });*/
            }
            else
            {
                stanMen.OnPress.AddListener(() =>
                {
                    if (updateDelay > 0f) return;
                    toDo();
                    if (postThread != null)
                    {
                        postThread();
                    }
                });
            }
        }

        public void CreateSlot(Transform parent, float y, ToolCategory cat)
        {
            GameObject empty = new GameObject();
            empty.transform.SetParent(parent, false);
            RectTransform transform = empty.AddComponent<RectTransform>();
            transform.pivot = new Vector2(0.5f,1f);
            transform.anchorMin = new Vector2(0.5f, 1f);
            transform.anchorMax = new Vector2(0.5f, 1f);
            transform.anchoredPosition = new Vector2(-0.5f, y);
            empty.name = cat.name;
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotCategory"), empty.transform, Vector2.zero, false);
            slot.transform.SetParent(empty.transform, false);
            slot.name = "Category Slot";
            Image icon = UIHelpers.CreateImage(cat.sprite, slot.transform, new Vector2(0f, 0f), false);
            slot.transform.SetParent(slot.transform, false);
            icon.name = "Icon";
            CategoryManager catMan = slot.gameObject.AddComponent<CategoryManager>();
            StandardMenuButton button = icon.gameObject.ConvertToButton<StandardMenuButton>(true);
            for (int i = 0; i < cat.tools.Count; i++)
            {
                catMan.allTools.Add(CreateToolButton(empty.transform, cat.tools[i]));
            }
            if (catMan.allTools.Count > 14)
            {
                CreateInitialPageButton(empty.transform, catMan);
                CreatePreviousPageButton(empty.transform, catMan);
                CreateNextPageButton(empty.transform, catMan);
            }
            if (catMan.allTools.Count > 7 && catMan.allTools.Count <= 14)
            {
                CreateNextPageButtonNoOtherButtons(empty.transform, catMan);
            }
            /*if (catMan.allTools.Count > 7)
            {
                CreatePreviousPageButton(empty.transform, catMan);
            }*/
            catMan.Initialize();
            slot.transform.SetAsLastSibling();
            button.OnPress.AddListener(() =>
            {
                catMan.OnClick();
            });
        }

        public Dictionary<string, string> toolDictionary = new Dictionary<string, string>();

        public ToolIconManager CreateToolButton(Transform parent, EditorTool tool)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotStandard"), parent, new Vector2(0f,0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Slot (" + tool.GetType().Name + ")";
            Sprite targetSprite = null;
            try
            {
                targetSprite = tool.editorSprite;
            }
            catch (Exception E)
            {
                UnityEngine.Debug.LogException(E);
            }
            if (targetSprite == null)
            {
                targetSprite = BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/icon_unknown");
            }
            Image icon = UIHelpers.CreateImage(targetSprite, slot.transform, new Vector2(0f, 0f), false);
            slot.transform.SetParent(slot.transform, false);
            icon.name = "Icon";
            StandardMenuButton button = icon.gameObject.ConvertToButton<StandardMenuButton>(true);
            if (tool is DisabledEditorTool)
            {
                icon.color = new Color(0.5f, 0.5f, 0.5f);
            }
            button.OnPress.AddListener(() =>
            {
                if (button.GetComponent<EditorTrackCursor>()) return;
                if (tool is DisabledEditorTool)
                {
                    Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    return;
                }
                if (selectedTool == null)
                {
                    SelectTool(tool);
                    button.gameObject.AddComponent<EditorTrackCursor>();
                }
            });
            return icon.gameObject.AddComponent<ToolIconManager>();
        }

        public ToolIconManager CreateNextPageButton(Transform parent, CategoryManager category)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotScroll"), parent, new Vector2(0f, 0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Category Next page Button";
            slot.transform.SetParent(slot.transform, false);
            StandardMenuButton button = slot.gameObject.ConvertToButton<StandardMenuButton>(true);
            category.pageButton = slot.gameObject.AddComponent<ToolIconManager>();
            button.OnPress.AddListener(() =>
            {
                category.page = (category.page + 1) % (1 + Mathf.FloorToInt(category.allTools.Count / 7));
                category.SwitchPage();
            });
            return slot.gameObject.GetComponent<ToolIconManager>();
        }

        public ToolIconManager CreateNextPageButtonNoOtherButtons(Transform parent, CategoryManager category)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotScroll"), parent, new Vector2(0f, 0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Category Next page Button";
            slot.transform.SetParent(slot.transform, false);
            StandardMenuButton button = slot.gameObject.ConvertToButton<StandardMenuButton>(true);
            category.pageButtonNoOtherButtons = slot.gameObject.AddComponent<ToolIconManager>();
            button.OnPress.AddListener(() =>
            {
                category.page = (category.page + 1) % (1 + Mathf.FloorToInt(category.allTools.Count / 7));
                category.SwitchPage();
            });
            return slot.gameObject.GetComponent<ToolIconManager>();
        }

        public ToolIconManager CreatePreviousPageButton(Transform parent, CategoryManager category)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotScrollBack"), parent, new Vector2(0f, 0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Category Previous page Button";
            slot.transform.SetParent(slot.transform, false);
            StandardMenuButton button = slot.gameObject.ConvertToButton<StandardMenuButton>(true);
            category.pageButtonBack = slot.gameObject.AddComponent<ToolIconManager>();
            button.OnPress.AddListener(() =>
            {
                if (category.page > 0)
                    category.page = (category.page - 1) % (1 - Mathf.FloorToInt(category.allTools.Count / 7));
                else
                    category.page = Mathf.FloorToInt(category.allTools.Count / 7) - 1;
                category.SwitchPage();
            });
            return slot.gameObject.GetComponent<ToolIconManager>();
        }

        public ToolIconManager CreateInitialPageButton(Transform parent, CategoryManager category)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotScrollFirst"), parent, new Vector2(0f, 0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Category Initial page Button";
            slot.transform.SetParent(slot.transform, false);
            StandardMenuButton button = slot.gameObject.ConvertToButton<StandardMenuButton>(true);
            category.pageButtonFirst = slot.gameObject.AddComponent<ToolIconManager>();
            button.OnPress.AddListener(() =>
            {
                category.page = 0;
                category.SwitchPage();
            });
            return slot.gameObject.GetComponent<ToolIconManager>();
        }

        void UpdateCursor()
        {
            StartCoroutine(UpdateCursorDelay());
        }

        private IEnumerator UpdateCursorDelay()
        {
            yield return null;
            cursor.transform.SetAsLastSibling();
            yield break;
        }
    }
}
