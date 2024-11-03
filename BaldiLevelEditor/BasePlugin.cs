using BaldiLevelEditor.Types;
using BaldiLevelEditor.UI;
using BaldiTexturePacks;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.UI;
using PlusLevelFormat;
using PlusLevelLoader.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BaldiLevelEditor
{
    [BepInDependency("mtm101.rulerp.baldiplus.texturepacks", BepInDependency.DependencyFlags.SoftDependency)]
    [ConditionalPatchMod("mtm101.rulerp.baldiplus.texturepacks")]
    [HarmonyPatch(typeof(TexturePack))]
    internal class TexturePackFixer
    {
        [HarmonyPatch("Apply")]
        [HarmonyPrefix]
        private static void RemoveNullValues(TexturePack __instance)
        {

            Dictionary<AudioClip, AudioClip> temp = new Dictionary<AudioClip, AudioClip>(__instance.clipsToReplace);
            __instance.clipsToReplace.Clear();
            foreach (KeyValuePair<AudioClip, AudioClip> item in temp)
            {
                if (!__instance.clipsToReplace.ContainsKey(item.Key) && item.Key != null && item.Value != null)
                {
                    __instance.clipsToReplace.Add(item.Key, item.Value);
                }
            }
            Dictionary<SoundObject, AudioClip> temp2 = new Dictionary<SoundObject, AudioClip>(TPPlugin.Instance.originalSoundClips);
            TPPlugin.Instance.originalSoundClips.Clear();
            foreach (KeyValuePair<SoundObject, AudioClip> item in temp2)
            {
                if (!TPPlugin.Instance.originalSoundClips.ContainsKey(item.Key) && item.Key != null && item.Value != null)
                {
                    TPPlugin.Instance.originalSoundClips.Add(item.Key, item.Value);
                }
            }
        }
    }

    public class EditorPrebuiltStucture
    {
        public List<PrefabLocation> prefabs = new List<PrefabLocation>();
        public Vector3 origin = Vector3.zero;

        public EditorPrebuiltStucture(params PrefabLocation[] _prefabs)
        {
            prefabs = _prefabs.ToList();
        }
    }

    [BepInPlugin("mtm101.rulerp.baldiplus.leveleditor", "Baldi's Basics Plus Level Editor", "0.1.0.0")]
    public class BaldiLevelEditorPlugin : BaseUnityPlugin
    {

        public static Dictionary<string, Type> doorTypes = new Dictionary<string, Type>();
        public static Dictionary<string, ITileVisual> tiledPrefabPrefabs = new Dictionary<string, ITileVisual>();

        public static bool isFucked { get; private set; }

        public static List<EditorObjectType> editorObjects = new List<EditorObjectType>();

        public static List<EditorObjectType> editorActivities = new List<EditorObjectType>();

        public static Dictionary<string, GameObject> characterObjects = new Dictionary<string, GameObject>();

        public static Dictionary<string, ItemObject> itemObjects = new Dictionary<string, ItemObject>();

        public AssetManager assetMan = new AssetManager();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static BaldiLevelEditorPlugin Instance;
        public static EditorObjectType pickupPrefab;
        GameCamera camera;
        Cubemap cubemap;
        Canvas canvasTemplate;
        CursorController cursorOrigin;
        internal Tile tilePrefab;
        EnvironmentController environmentControllerPrefab;

        public static CapsuleCollider playerColliderObject;

        public static Dictionary<string, Texture2D> lightmaps = new Dictionary<string, Texture2D>();
        public static Shader tileStandardShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard_AlphaClip");
        public static Shader tileMaskedShader => Instance.assetMan.Get<Shader>("Shader Graphs/MaskedStandard");
        public static Shader tilePosterShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandardWPoster_AlphaClip");
        public static Material spriteMaterial;
        public static ElevatorScreen elevatorScreen;
        public static CoreGameManager coreGamePrefab;
        public static EndlessGameManager endlessGameManager;
        public static MainGameManager mainGameManager;
        public static Texture2D yellowTexture => lightmaps["yellow"];
        public static Texture2D lightmapTexture => lightmaps["lighting"];
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static string[] editorThemes = new string[9];
        public static SoundObject[] fileEditorThemes = new SoundObject[1];

        Sprite platterSprite;

        public static GameObject StripAllScripts(GameObject reference, bool stripColliders = false)
        {
            bool active = reference.activeSelf;
            reference.SetActive(false);
            GameObject obj = GameObject.Instantiate(reference);
            obj.GetComponentsInChildren<MonoBehaviour>().Do(x => Destroy(x));
            if (stripColliders)
            {
                obj.GetComponentsInChildren<Collider>().Do(x => Destroy(x));
            }
            obj.name = "REFERENCE_" + obj.name;
            obj.ConvertToPrefab(false);
            reference.SetActive(active);
            return obj;
        }

        public static T CreateTileVisualFromObject<T, TC>(GameObject reference) where T : TileBasedEditorVisual<TC>
            where TC : TiledPrefab
        {
            GameObject newRef = StripAllScripts(reference, false);
            T comp = newRef.GetComponentInChildren<Collider>().gameObject.AddComponent<T>();
            return comp;
        }

        void OptMenPlaceholder(OptionsMenu __instance)
        {
            GameObject obj = CustomOptionsCore.CreateNewCategory(__instance, "EDITOR");
            StandardMenuButton but = CustomOptionsCore.CreateApplyButton(__instance, "WARP TO EDITOR!!", () =>
            {
                BaldiLevelEditorPlugin.Instance.StartCoroutine(BaldiLevelEditorPlugin.Instance.GoToGame());
            });
            but.transform.SetParent(obj.transform, false);
        }

        public IEnumerator GoToGame()
        {
            AsyncOperation waitForSceneLoad = SceneManager.LoadSceneAsync("Game");
            while (!waitForSceneLoad.isDone)
            {
                yield return null;
            }
            // this is slow AF but who actually cares
            for (int x = 0; x < lightmapTexture.width; x++)
            {
                for (int y = 0; y < lightmapTexture.height; y++)
                {
                    lightmapTexture.SetPixel(x, y, Color.white);
                }
            }
            lightmapTexture.Apply();
            GameCamera cam = GameObject.Instantiate<GameCamera>(camera);
            Shader.SetGlobalTexture("_Skybox", cubemap);
            Shader.SetGlobalColor("_SkyboxColor", Color.white);
            Shader.SetGlobalColor("_FogColor", Color.white);
            Shader.SetGlobalFloat("_FogStartDistance", 5f);
            Shader.SetGlobalFloat("_FogMaxDistance", 100f);
            Shader.SetGlobalFloat("_FogStrength", 0f);
            Canvas canvas = GameObject.Instantiate<Canvas>(canvasTemplate);
            canvas.name = "MainCanvas";
            CursorInitiator cursorInit = canvas.gameObject.AddComponent<CursorInitiator>();
            if ((float)Singleton<PlayerFileManager>.Instance.resolutionX / (float)Singleton<PlayerFileManager>.Instance.resolutionY >= 1.3333f)
            {
                canvas.scaleFactor = (float)Mathf.RoundToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 360f);
            }
            else
            {
                canvas.scaleFactor = (float)Mathf.FloorToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 480f);
            }
            cursorInit.screenSize = new Vector2(Screen.width / canvas.scaleFactor, Screen.height / canvas.scaleFactor);
            cursorInit.cursorPre = cursorOrigin;
            cursorInit.graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            canvas.gameObject.SetActive(true);
            canvas.worldCamera = cam.canvasCam;
            GameObject dummyObject = new GameObject();
            dummyObject.SetActive(false);
            PlusLevelEditor editor = dummyObject.AddComponent<PlusLevelEditor>();
            editor.ReflectionSetVariable("destroyOnLoad", true);
            dummyObject.SetActive(true);
            editor.gameObject.name = "Level Editor";
            editor.cursorBounds = cursorInit.screenSize;
            editor.gameObject.AddComponent<AudioManager>().audioDevice = editor.gameObject.AddComponent<AudioSource>();
            editor.audMan = editor.gameObject.GetComponent<AudioManager>();
            editor.audMan.ReflectionSetVariable("disableSubtitles", true);
            Singleton<PlusLevelEditor>.Instance.myCamera = cam;
            Singleton<PlusLevelEditor>.Instance.canvas = canvas;
            EnvironmentController ec = GameObject.Instantiate<EnvironmentController>(environmentControllerPrefab);
            ec.gameObject.SetActive(false);
            Singleton<PlusLevelEditor>.Instance.puppetEnvironmentController = ec;

            //CursorController cc = GameObject.Instantiate<CursorController>(cursorOrigin);
            //cc.transform.SetParent(canvas.transform, false);
        }

        public SoundObject editorE = new SoundObject();

        public void CreatePosterVisual(string editorName, PosterObject posterObject)
        {
            var basePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var renderer = basePlane.GetComponent<MeshRenderer>();
            //renderer.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "TileBase");
            renderer.material = new Material(tileStandardShader);

            basePlane.transform.localScale = Vector3.one * 10; // Gives the tile size
            basePlane.name = "PlaneTemplate";
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            GameObject gameObject = new GameObject();
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(10f, 10f, 0.1f);
            collider.center = new Vector3(0f, 5f, 4.85f);
            basePlane.transform.SetParent(gameObject.transform);
            basePlane.transform.localPosition = (Vector3.forward * 4.99f) + (Vector3.up * 5f);
            renderer.material.mainTexture = posterObject.baseTexture;
            //poster_beakid.name = "Poster_BeAKid";

            if (posterObject.textData.Length != 0)
            {
                TextTextureGenerator textureText = Instantiate(Resources.FindObjectsOfTypeAll<TextTextureGenerator>().First());
                //texturetext.LoadPosterData(posterObject);
                renderer.material.mainTexture = textureText.GenerateTextTexture(posterObject);
                /*EnvironmentController ec = GameObject.Find("Environment Controller(Clone)").GetComponent<EnvironmentController>();
                ec.BuildPoster(posterObject);*/

                /*GameObject textBillboard = new GameObject();
                textBillboard.transform.SetParent(gameObject.transform);
                var textBillboardText = textBillboard.AddComponent<TextMeshPro>();
                textBillboardText.text = posterObject.textData.ToString();
                textBillboard.name = "What Does The Poster Say?";
                textBillboard.layer = LayerMask.NameToLayer("Billboard");*/
            }

            gameObject.ConvertToPrefab(true);

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(editorName, gameObject, Vector3.zero));
        }

        public void CreatePriceTagVisual(string editorName, GameObject baseTag, string priceTagType)
        {

            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(editorName, baseTag, Vector3.up * 3.75f));

            GameObject baseTagFake = Instantiate(baseTag);

            GameObject itemObjectParent = new GameObject();
            itemObjectParent.transform.SetParent(baseTagFake.transform);
            GameObject itemActualObject = new GameObject();
            itemActualObject.transform.SetParent(itemObjectParent.transform);
            SpriteRenderer spriteRen = itemActualObject.AddComponent<SpriteRenderer>();
            spriteRen.size = new Vector2(5f, 5f);
            spriteRen.material = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "SpriteStandard_Billboard").First();

            Vector3 pos = baseTagFake.transform.position + baseTagFake.transform.forward * 1.5f;
            itemObjectParent.transform.position = pos;
            itemObjectParent.transform.position = new Vector3(itemObjectParent.transform.position.x, 5f, itemObjectParent.transform.position.z);

            itemActualObject.layer = LayerMask.NameToLayer("Billboard");

            itemObjectParent.name = "ItemPreview";
            itemActualObject.name = "ItemPreviewSprite";

            baseTagFake.AddComponent<PriceTag>();

            if (priceTagType == "map")
            {
                spriteRen.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "MapIcon_Large").First();
                baseTagFake.GetComponent<PriceTag>().SetText("MAP");
            }
            else if (priceTagType == "item")
            {
                spriteRen.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "NoItem").First();
                baseTagFake.GetComponent<PriceTag>().SetText("ITEM");
            }
            else if (priceTagType == "out")
            {
                baseTagFake.GetComponent<PriceTag>().SetText("OUT");
            }
            else if (priceTagType == "restocking")
            {
                //spriteRen.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "BackArrow_0").First();
                spriteRen.sprite = assetMan.Get<Sprite>("RestockingVisual");
                baseTagFake.GetComponent<PriceTag>().SetText("RESTOCK");
            }

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(editorName, baseTagFake, Vector3.up * 3.75f));
        }

        public void CreateEventVisual(string editorName, string eventType)
        {
            GameObject eventObjectThing = new GameObject();
            eventObjectThing.transform.localScale = new Vector3(15f, 15f, 15f);
            eventObjectThing.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.2f, 0.2f); //Was 2f, 2f, 2f
            //eventObjectThing.AddComponent<EditorRandomEvent>();

            GameObject eventActualObject = new GameObject();
            eventActualObject.transform.SetParent(eventObjectThing.transform);
            SpriteRenderer spriteRen = eventActualObject.AddComponent<SpriteRenderer>();
            //spriteRen.size = new Vector2(15f, 15f);
            spriteRen.material = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "SpriteStandard_Billboard").First();

            if (eventType == "fog")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_randomevent_fog");
            }
            else if (eventType == "flood")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_randomevent_flood");
            }
            else if (eventType == "gravityChaos")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_randomevent_gravitychaos");
            }
            else if (eventType == "brokenRuler")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_randomevent_brokenruler");
            }
            else if (eventType == "party")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_randomevent_party");
            }

            eventObjectThing.ConvertToPrefab(true);

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(editorName, eventObjectThing, Vector3.up * 5f));
        }

        public void CreateChallengeVisual(string editorName, string eventType)
        {
            GameObject eventObjectThing = new GameObject();
            eventObjectThing.transform.localScale = new Vector3(15f, 15f, 15f);
            eventObjectThing.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.2f, 0.2f); //Was 2f, 2f, 2f
            //eventObjectThing.AddComponent<EditorRandomEvent>();

            GameObject eventActualObject = new GameObject();
            eventActualObject.transform.SetParent(eventObjectThing.transform);
            SpriteRenderer spriteRen = eventActualObject.AddComponent<SpriteRenderer>();
            //spriteRen.size = new Vector2(15f, 15f);
            spriteRen.material = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "SpriteStandard_Billboard").First();

            if (eventType == "speedy")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_challenge_speedy");
            }
            else if (eventType == "stealthy")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_challenge_stealthy");
            }
            else if (eventType == "grapple")
            {
                spriteRen.sprite = assetMan.Get<Sprite>("UI/Object_challenge_grapple");
            }

            eventObjectThing.ConvertToPrefab(true);

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(editorName, eventObjectThing, Vector3.up * 5f));
        }

        IEnumerator AssetsLoadedActual()
        {
            if ((new Version(MTM101BaldiDevAPI.VersionNumber)) < (new Version("4.0.0.0")))
            {
                MTM101BaldiDevAPI.CauseCrash(this.Info, new Exception("Invalid API version, please use 4.0 or greater!"));
            }
            yield return 5;
            yield return "Defining Variables...";
            assetMan.Add<Sprite>("clipboard", Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "OptionsClipboard").First());
            spriteMaterial = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "SpriteStandard_Billboard").First();
            camera = Resources.FindObjectsOfTypeAll<GameCamera>().First();
            cubemap = Resources.FindObjectsOfTypeAll<Cubemap>().Where(x => x.name == "Cubemap_DayStandard").First();
            cursorOrigin = Resources.FindObjectsOfTypeAll<CursorController>().Where(x => !x.name.Contains("Clone")).First();
            environmentControllerPrefab = Resources.FindObjectsOfTypeAll<EnvironmentController>().First();
            tilePrefab = Resources.FindObjectsOfTypeAll<Tile>().First();
            Canvas endingError = GameObject.Instantiate<Canvas>(Resources.FindObjectsOfTypeAll<Canvas>().Where(x => x.name == "EndingError").First());
            endingError.gameObject.SetActive(false);
            for (int i = 0; i < endingError.transform.childCount; i++)
            {
                GameObject.Destroy(endingError.transform.GetChild(i).gameObject);
            }
            endingError.name = "Canvas Template";
            canvasTemplate = endingError;
            //GameObject.Destroy(canvasTemplate.GetComponent<GlobalCamCanvasAssigner>());
            canvasTemplate.gameObject.ConvertToPrefab(false);
            canvasTemplate.planeDistance = 100f;
            canvasTemplate.sortingOrder = 10;
            editorThemes[0] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorA.mid"), "editorA");
            editorThemes[1] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorB.mid"), "editorB");
            editorThemes[2] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorC.mid"), "editorC");
            editorThemes[3] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorD.mid"), "editorD");
            editorThemes[4] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorE.mid"), "editorE");
            editorThemes[5] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorF.mid"), "editorF");
            editorThemes[6] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorG.mid"), "editorG");
            editorThemes[7] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorH.mid"), "editorH");
            editorThemes[8] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorI.mid"), "editorI");
            //editorThemes[8] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorI.mid"), "editorI");

            //NEW; Adds a new Sprite lol
            assetMan.Add<Sprite>("RestockingVisual", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "RestockingVisual.png"), 50f));

            /*AudioClip editorEAudio = AssetLoader.AudioClipFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorE.ogg"), AudioType.OGGVORBIS);
            editorE.soundClip = editorEAudio;
            editorE.soundKey = "editorE";
            editorE.soundType = SoundType.Music;
            editorE.subDuration = 0f;
            fileEditorThemes[0] = editorE;*/

            lightmaps.Add("lighting", Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.name == "LightMap").First());

            pickupPrefab = EditorObjectType.CreateFromGameObject<ItemPrefab, ItemLocation>("item", Resources.FindObjectsOfTypeAll<Pickup>().Where(x => x.transform.parent == null).First().gameObject, Vector3.up * 5f);
            pickupPrefab.prefab.gameObject.AddComponent<EditorHasNoCollidersInBaseGame>();


            assetMan.AddFromResources<Texture2D>();
            assetMan.AddFromResources<Mesh>();
            assetMan.AddFromResources<Shader>();
            assetMan.AddFromResources<SoundObject>();
            elevatorScreen = Resources.FindObjectsOfTypeAll<ElevatorScreen>().Where(x => x.gameObject.transform.parent == null).First();
            coreGamePrefab = Resources.FindObjectsOfTypeAll<CoreGameManager>().First();
            endlessGameManager = Resources.FindObjectsOfTypeAll<EndlessGameManager>().First();
            Activity[] activites = Resources.FindObjectsOfTypeAll<Activity>();
            assetMan.Add("Mixer_Sounds", Resources.FindObjectsOfTypeAll<UnityEngine.Audio.AudioMixer>().Where(x => x.name == "Sounds").First());

            yield return "Creating Editor Prefabs...";
            // prefabs
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("desk", objects.Where(x => x.name == "Table_Test").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bigdesk", objects.Where(x => x.name == "BigDesk").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cabinettall", objects.Where(x => x.name == "FilingCabinet_Tall").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cabinetshort", objects.Where(x => x.name == "FilingCabinet_Short").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("chair", objects.Where(x => x.name == "Chair_Test").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("computer", objects.Where(x => x.name == "MyComputer").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("computer_off", objects.Where(x => x.name == "MyComputer_Off").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("roundtable", objects.Where(x => x.name == "RoundTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("locker", objects.Where(x => x.name == "Locker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bluelocker", objects.Where(x => x.name == "BlueLocker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("greenlocker", objects.Where(x => x.name == "StorageLocker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bookshelf", objects.Where(x => x.name == "Bookshelf_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bookshelf_hole", objects.Where(x => x.name == "Bookshelf_Hole_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("rounddesk", objects.Where(x => x.name == "RoundDesk").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cafeteriatable", objects.Where(x => x.name == "CafeteriaTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("dietbsodamachine", objects.Where(x => x.name == "DietSodaMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bsodamachine", objects.Where(x => x.name == "SodaMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("zestymachine", objects.Where(x => x.name == "ZestyMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_bsoda", objects.Where(x => x.name == "CrazyVendingMachineBSODA").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_zesty", objects.Where(x => x.name == "CrazyVendingMachineZesty").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("waterfountain", objects.Where(x => x.name == "WaterFountain").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("counter", objects.Where(x => x.name == "Counter").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("examination", objects.Where(x => x.name == "ExaminationTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("ceilingfan", objects.Where(x => x.name == "CeilingFan").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("merrygoround", objects.Where(x => x.name == "MerryGoRound_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tree", objects.Where(x => x.name == "TreeCG").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("pinetree", objects.Where(x => x.name == "PineTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("pinetreebully", objects.Where(x => x.name == "PineTree_Bully").Where(x => x.transform.parent != null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("prizestatic", objects.Where(x => x.name == "FirstPrize_SpriteBase").Where(x => x.transform.parent != null).First(), Vector3.up * 5f));

            /*GameObject prizeObject = GameObject.Instantiate(objects.Where(x => x.name == "FirstPrize_SpriteBase").Where(x => x.transform.parent != null).First());
            GameObject prizeObjectArrow = GameObject.Instantiate(objects.Where(x => x.name == "ConveyorBelt").First());
            prizeObject.ConvertToPrefab(true);
            prizeObjectArrow.transform.SetParent(prizeObject.transform, true);
            prizeObjectArrow.transform.localPosition += new Vector3(0f, 4.99f, 0f);
            prizeObjectArrow.GetComponent<MeshRenderer>().material.mainTexture = assetMan.Get<Texture2D>("Arrow");
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("prizestatic", prizeObject, Vector3.up * 5f));*/

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("appletree", objects.Where(x => x.name == "AppleTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bananatree", objects.Where(x => x.name == "BananaTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hoop", objects.Where(x => x.name == "HoopBase").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("payphone", objects.Where(x => x.name == "PayPhone").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tapeplayer", objects.Where(x => x.name == "TapePlayer").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("plant", objects.Where(x => x.name == "Plant").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_pencilnotes", objects.Where(x => x.name == "Decor_PencilNotes").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_papers", objects.Where(x => x.name == "Decor_Papers").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_globe", objects.Where(x => x.name == "Decor_Globe").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_notebooks", objects.Where(x => x.name == "Decor_Notebooks").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_lunch", objects.Where(x => x.name == "Decor_Lunch").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_banana", objects.Where(x => x.name == "Decor_Banana").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_zoneflag", objects.Where(x => x.name == "Decor_ZoningFlag").Where(x => x.transform.parent == null).First(), Vector3.zero));

            CreateChallengeVisual("challenge_speedy", "speedy");
            CreateChallengeVisual("challenge_stealthy", "stealthy");
            CreateChallengeVisual("challenge_grapple", "grapple");

            CreateEventVisual("randomevent_fog", "fog");
            CreateEventVisual("randomevent_flood", "flood");
            CreateEventVisual("randomevent_gravitychaos", "gravityChaos");
            CreateEventVisual("randomevent_brokenruler", "brokenRuler");
            //CreateEventVisual("randomevent_party", "party");

            CreatePriceTagVisual("pricetag", objects.Where(x => x.name == "PriceTag_4").Where(x => x.transform.parent.name == "RoomBase").First(), "item"); //Was isMapTag = false
            CreatePriceTagVisual("pricetagmap", objects.Where(x => x.name == "PriceTag_5").Where(x => x.transform.parent.name == "RoomBase").First(), "map"); //Was isMapTag = true
            CreatePriceTagVisual("pricetagout", objects.Where(x => x.name == "PriceTag_6").Where(x => x.transform.parent.name == "RoomBase").First(), "out");
            CreatePriceTagVisual("pricetagrestocking", objects.Where(x => x.name == "PriceTag_7").Where(x => x.transform.parent.name == "RoomBase").First(), "restocking");

            /*priceTagEditor = Instantiate(objects.Where(x => x.name == "PriceTag").Where(x => x.transform.parent.name == "RoomBase").First());
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("pricetag", priceTagEditor, Vector3.up * 3.75f));
            priceTagEditor.GetComponent<PriceTag>().SetText("ITEM");*/

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("picnictable", objects.Where(x => x.name == "PicnicTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("picnicbasket", objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First(), Vector3.up * 3.5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("minigame1trigger", objects.Where(x => x.name == "Minigame1Trigger").Where(x => x.transform.parent.name == "ObjectBase").First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("minigame2trigger", objects.Where(x => x.name == "Minigame2Trigger").Where(x => x.transform.parent.name == "ObjectBase").First(), Vector3.up * 5f));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("johnny", objects.Where(x => x.name == "JohnnyBase").Where(x => x.transform.parent.name == "RoomBase").First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cashregister", objects.Where(x => x.name == "CashRegister").Where(x => x.transform.parent.name == "RoomBase").First(), Vector3.up * 3.75f));

            /*priceMapEditor = Instantiate(objects.Where(x => x.name == "PriceTag").Where(x => x.transform.parent.name == "RoomBase").First());
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("pricetagmap", priceMapEditor, Vector3.up * 3.75f));
            priceMapEditor.GetComponent<PriceTag>().SetText("MAP");*/

            GameObject dirtCircle = GameObject.Instantiate(objects.Where(x => x.name == "DirtCircle").Where(x => x.transform.parent == null).First());
            GameObject dirtCircleBase = new GameObject();
            dirtCircleBase.AddComponent<BoxCollider>().size = new Vector3(15f, 0.1f, 15f);
            dirtCircle.transform.SetParent(dirtCircleBase.transform);
            //dirtCircle.transform.localRotation = new Quaternion(-90f, 0f, 0f, 0f);
            dirtCircleBase.ConvertToPrefab(true);

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("dirtcircle", dirtCircleBase, Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tent", objects.Where(x => x.name == "Tent_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("rock", objects.Where(x => x.name == "Rock").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("johnnysign", objects.Where(x => x.name == "JohnnySign").Where(x => x.transform.parent == null).First(), Vector3.up * 10f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("campfire", objects.Where(x => x.name == "CampFire").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("nanapeelplaced", objects.Where(x => x.name == "NanaPeel").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("balloon_blue", objects.Where(x => x.name == "Balloon_Blue").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("balloon_green", objects.Where(x => x.name == "Balloon_Green").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("balloon_orange", objects.Where(x => x.name == "Balloon_Orange").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("balloon_purple", objects.Where(x => x.name == "Balloon_Purple").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));

            //TiledEditorConnectable conveyorVisual = CreateTileVisualFromObject<TiledEditorConnectable, TiledPrefab>(objects.Where(x => x.name == "BeltManager").First());
            //conveyorVisual.positionOffset = Vector3.up * 21f;
            //conveyorVisual.directionAddition = 4f;
            //GameObject conveyorBelt = Instantiate(objects.Where(x => x.name == "ConveyorBelt").First());
            //conveyorBelt.transform.SetParent(conveyorVisual.transform);

            //Trying to fix the editor visual looking weird.
            GameObject beltObject = GameObject.Instantiate(objects.Where(x => x.name == "ConveyorBelt").First());
            GameObject beltObjectArrow = GameObject.Instantiate(objects.Where(x => x.name == "ConveyorBelt").First());
            GameObject beltBase = new GameObject();
            beltBase.ConvertToPrefab(true);
            beltObject.transform.SetParent(beltBase.transform, true);
            beltObjectArrow.transform.SetParent(beltBase.transform, true);
            beltObjectArrow.transform.localPosition += Vector3.up * 0.01f;
            beltObjectArrow.GetComponent<MeshRenderer>().material.mainTexture = assetMan.Get<Texture2D>("Arrow");
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("conveyorbelt", beltBase, Vector3.zero));
            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("conveyorbelt", objects.Where(x => x.name == "ConveyorBelt").First(), Vector3.zero));
            //beltObject.transform.rotation = new Quaternion(beltObject.transform.rotation.x + 90f, beltObject.transform.rotation.y, beltObject.transform.rotation.z, beltObject.transform.rotation.w);
            //beltObject.AddComponent<EditorConveyorBeltVisuals>();

            CreatePosterVisual("poster_pri_baldi", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BaldiPoster"));
            CreatePosterVisual("poster_pri_principal", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PrincipalPoster"));
            CreatePosterVisual("poster_pri_sweep", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "SweepPoster"));
            CreatePosterVisual("poster_pri_playtime", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PlaytimePoster"));
            CreatePosterVisual("poster_pri_bully", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BullyPoster"));
            CreatePosterVisual("poster_pri_crafters", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CraftersPoster"));
            CreatePosterVisual("poster_pri_prize", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PrizePoster"));
            CreatePosterVisual("poster_pri_cloud", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CumuloPoster"));
            CreatePosterVisual("poster_pri_chalk", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "ChalkPoster"));
            CreatePosterVisual("poster_pri_beans", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BeansPoster"));
            CreatePosterVisual("poster_pri_pomp", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PompPoster"));
            CreatePosterVisual("poster_pri_test", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TheTestPoster"));
            CreatePosterVisual("poster_pri_reflex", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "ReflexPoster"));

            CreatePosterVisual("poster_hint_boots", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Boots"));
            CreatePosterVisual("poster_hint_nosquee", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_NoSquee"));
            CreatePosterVisual("poster_hint_phone", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Phone"));
            CreatePosterVisual("poster_hint_read", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Read"));
            CreatePosterVisual("poster_hint_rules", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Rules"));
            CreatePosterVisual("poster_hint_scissors", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Scissors"));
            CreatePosterVisual("poster_hint_ytps", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_YTPs"));

            CreatePosterVisual("poster_numbers", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_Numbers"));
            CreatePosterVisual("poster_baldiburied", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiBuried"));

            CreatePosterVisual("poster_beakid", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_BeAKid"));
            CreatePosterVisual("poster_candy", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_Candy"));
            CreatePosterVisual("poster_what", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_What"));

            CreatePosterVisual("poster_txt_inspiration", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TXT_Inspiration"));
            CreatePosterVisual("poster_txt_recycle", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TXT_Recycle"));

            CreatePosterVisual("poster_mailcomic1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_0"));
            CreatePosterVisual("poster_mailcomic2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_1"));
            CreatePosterVisual("poster_mailcomic3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_2"));
            CreatePosterVisual("poster_mailcomic4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_3"));

            CreatePosterVisual("poster_baldisays1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_1"));
            CreatePosterVisual("poster_baldisays2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_2"));
            CreatePosterVisual("poster_baldisays3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_3"));
            CreatePosterVisual("poster_baldisays4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_4"));
            CreatePosterVisual("poster_baldisays5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_5"));
            CreatePosterVisual("poster_baldisays6", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_6"));
            CreatePosterVisual("poster_baldisays7", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_7"));
            CreatePosterVisual("poster_baldisays8", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_8"));
            CreatePosterVisual("poster_baldisays9", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_9"));
            CreatePosterVisual("poster_baldisays_saveandquit", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_SaveQuit"));

            CreatePosterVisual("poster_blt_all", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_All"));
            CreatePosterVisual("poster_blt_blah", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Blah"));
            CreatePosterVisual("poster_blt_budget", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Budget"));
            CreatePosterVisual("poster_blt_computer", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Computer"));
            CreatePosterVisual("poster_blt_fired", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Fired"));
            CreatePosterVisual("poster_blt_hawaii", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Hawaii"));
            CreatePosterVisual("poster_blt_heykid", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_HeyKid"));
            CreatePosterVisual("poster_blt_meeting", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Meeting"));
            CreatePosterVisual("poster_blt_review", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Review"));

            CreatePosterVisual("poster_chk_apple", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Apple"));
            CreatePosterVisual("poster_chk_baldisays", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_BaldiSays"));
            CreatePosterVisual("poster_chk_chalk", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Chalk"));
            CreatePosterVisual("poster_chk_cheese", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Cheese"));
            CreatePosterVisual("poster_chk_g", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_G"));
            CreatePosterVisual("poster_chk_hangman", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Hangman"));
            CreatePosterVisual("poster_chk_mathh", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Mathh"));
            CreatePosterVisual("poster_chk_possible", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Possible"));
            CreatePosterVisual("poster_chk_treehint", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_TreeHint"));
            CreatePosterVisual("poster_chk_world", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_World"));
            CreatePosterVisual("poster_chk_wow", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Wow"));
            CreatePosterVisual("poster_chk_baldisboard", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_baldisboard"));

            CreatePosterVisual("poster_storeneon1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_0"));
            CreatePosterVisual("poster_storeneon2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_1"));
            CreatePosterVisual("poster_storeneon3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_2"));
            CreatePosterVisual("poster_storeneon4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_3"));
            CreatePosterVisual("poster_storeneon5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_4"));

            CreatePosterVisual("poster_kick1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick1"));
            CreatePosterVisual("poster_kick2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick2"));
            CreatePosterVisual("poster_kick3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick3"));
            CreatePosterVisual("poster_kick4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick4"));
            CreatePosterVisual("poster_kick5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick5"));
            CreatePosterVisual("poster_kick6", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick6"));
            CreatePosterVisual("poster_kick7", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick7"));
            CreatePosterVisual("poster_kick8", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick8"));


            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bus", objects.Where(x => x.name == "Bus").Where(x => x.transform.parent == null).First(), Vector3.zero));
            GameObject baseBus = Instantiate(objects.Where(x => x.name == "Bus").Where(x => x.transform.parent.name == "BusObjects").Where(x => x.transform.parent.parent.name == "FieldTripEntranceRoomFunction").First());
            GameObject fakeBus = new GameObject();
            fakeBus.ConvertToPrefab(true);
            baseBus.SetActive(true);
            baseBus.transform.SetParent(fakeBus.transform);
            baseBus.gameObject.SetActive(true);
            baseBus.AddComponent<BoxCollider>().size = new Vector3(7f, 7f, 7f);
            fakeBus.name = "BaseBus";
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bus", fakeBus, Vector3.zero));

            GameObject baseBusBaldi = Instantiate(objects.Where(x => x.name == "Bus").Where(x => x.transform.parent.name == "BusObjects").Where(x => x.transform.parent.parent.name == "FieldTripEntranceRoomFunction").First());
            GameObject fakeBusBaldi = new GameObject();
            fakeBusBaldi.ConvertToPrefab(true);
            baseBusBaldi.SetActive(true);
            baseBusBaldi.transform.SetParent(fakeBusBaldi.transform);
            baseBusBaldi.gameObject.SetActive(true);
            baseBusBaldi.AddComponent<BoxCollider>().size = new Vector3(7f, 7f, 7f);
            fakeBusBaldi.name = "BaseBus (BALD)";
            baseBusBaldi.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetMainTexture(Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "BaldisBus_Occupied"));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("busbaldi", fakeBusBaldi, Vector3.zero));

            /*GameObject platter = Instantiate(objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First());
            GameObject platterBase = new GameObject();
            platterBase.ConvertToPrefab(true);
            platter.SetActive(true);
            platter.transform.SetParent(platterBase.transform);
            platter.gameObject.SetActive(true);
            platter.AddComponent<BoxCollider>().size = new Vector3(3f, 3f, 3f);
            platterBase.name = "Perry the Platter(pus)";
            platterSprite = assetMan.Get<Sprite>("Platter");
            platter.GetComponent<SpriteRenderer>().sprite = platterSprite;
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("platter", platterBase, Vector3.up * 3.5f));*/

            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("prizestatic", objects.Where(x => x.transform.parent.name == "FirstPrize_SpriteBase").Where(x => x.transform.parent != null).Where(x => x.transform.parent.parent.name == "ObjectBase").First(), Vector3.zero));

            //GameObject bus = GameObject.Instantiate(objects.Where(x => x.name == "Bus").Where(x => x.transform.parent.name == "BusObjects").Where(x => x.transform.parent.parent.name == "FieldTripEntranceRoomFunction").First());
            //bus.ConvertToPrefab(true);

            /*GameObject bus = GameObject.CreatePrimitive(PrimitiveType.Plane);
            bus.ConvertToPrefab(true);
            bus.GetComponent<MeshRenderer>().material.mainTexture = Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.name == "BaldisBus_Empty").First();

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bus", bus, Vector3.zero));*/

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_exitsign", objects.Where(x => x.name == "Decor_ExitSign").Where(x => x.transform.parent == null).First(), Vector3.up * 10f));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("fluorescentlight", objects.Where(x => x.name == "FluorescentLight").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hanginglight", objects.Where(x => x.name == "HangingLight").Where(x => x.transform.parent == null).First(), Vector3.zero));

            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hopscotch", );

            //objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First().transform.GetChild(0);
            // ugly hopscotch hack
            GameObject hopActual = GameObject.Instantiate(objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First().transform.GetChild(0).gameObject);
            GameObject hopBase = new GameObject();
            hopBase.ConvertToPrefab(false);
            hopActual.transform.SetParent(hopBase.transform, true);
            hopBase.SetActive(false);
            hopActual.gameObject.SetActive(true);
            Destroy(hopActual.gameObject.GetComponent<Collider>());
            hopBase.transform.name = "EditorHopscotchBase";
            BoxCollider box = hopBase.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(20f, 0.1f, 20f);
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hopscotch", hopBase, Vector3.zero, true));


            // activities
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("notebook", Resources.FindObjectsOfTypeAll<Notebook>().First().gameObject, Vector3.up * 5f));
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("mathmachine", activites.Where(x => (x.name == "MathMachine" && (x.transform.parent == null))).First().gameObject, Vector3.zero));
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("mathmachine_corner", activites.Where(x => (x.name == "MathMachine_Corner" && (x.transform.parent == null))).First().gameObject, Vector3.zero));
            // ugly corner math machine hack
            EditorObjectType cornerMathMachine = editorActivities.Last();
            GameObject baseObject = GameObject.Instantiate(cornerMathMachine.prefab.gameObject);
            baseObject.GetComponents<MonoBehaviour>().Do(x => Destroy(x));
            baseObject.GetComponentsInChildren<Collider>().Do(x => Destroy(x));
            baseObject.transform.SetParent(cornerMathMachine.prefab.transform, false);
            baseObject.transform.localPosition = Vector3.zero;
            cornerMathMachine.prefab.gameObject.transform.eulerAngles = Vector3.zero;
            baseObject.transform.eulerAngles = new Vector3(0f, 45f, 0f);
            Destroy(cornerMathMachine.prefab.gameObject.GetComponent<MeshRenderer>());
            baseObject.SetActive(true);
            baseObject.name = "CornerRenderer";

            // characters
            yield return "Creating NPC Prefabs...";
            characterObjects.Add("baldi", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Baldi).value.gameObject, true));
            characterObjects.Add("principal", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Principal).value.gameObject, true));
            characterObjects.Add("sweep", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Sweep).value.gameObject, true));
            characterObjects.Add("playtime", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Playtime).value.gameObject, true));
            GameObject chalklesReference = StripAllScripts(NPCMetaStorage.Instance.Get(Character.Principal).value.gameObject, true);
            chalklesReference.GetComponentInChildren<SpriteRenderer>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "ChalkFace").First();
            characterObjects.Add("chalkface", chalklesReference);
            characterObjects.Add("bully", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Bully).value.gameObject, true));
            characterObjects.Add("beans", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Beans).value.gameObject, true));
            characterObjects.Add("prize", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Prize).value.gameObject, true));
            characterObjects.Add("crafters", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Crafters).value.gameObject, true));
            characterObjects.Add("pomp", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Pomp).value.gameObject, true));
            characterObjects.Add("test", StripAllScripts(NPCMetaStorage.Instance.Get(Character.LookAt).value.gameObject, true));
            characterObjects.Add("cloudy", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Cumulo).value.gameObject, true));
            characterObjects.Add("reflex", StripAllScripts(NPCMetaStorage.Instance.Get(Character.DrReflex).value.gameObject, true));
            //characterObjects.Add("fastbaldi", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Baldi).value.gameObject, true));
            //characterObjects.Add("principal_allknowing", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Principal).value.gameObject, true));

            // items
            yield return "Setting Up Items...";
            itemObjects.Add("quarter", ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value);
            itemObjects.Add("keys", ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).value);
            itemObjects.Add("zesty", ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value);
            itemObjects.Add("whistle", ItemMetaStorage.Instance.FindByEnum(Items.PrincipalWhistle).value);
            itemObjects.Add("teleporter", ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value);
            itemObjects.Add("dietbsoda", ItemMetaStorage.Instance.FindByEnum(Items.DietBsoda).value);
            itemObjects.Add("bsoda", ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value);
            itemObjects.Add("boots", ItemMetaStorage.Instance.FindByEnum(Items.Boots).value);
            itemObjects.Add("clock", ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value);
            itemObjects.Add("dirtychalk", ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value);
            itemObjects.Add("grapple", ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).value);
            itemObjects.Add("nosquee", ItemMetaStorage.Instance.FindByEnum(Items.Wd40).value);
            itemObjects.Add("nametag", ItemMetaStorage.Instance.FindByEnum(Items.Nametag).value);
            itemObjects.Add("tape", ItemMetaStorage.Instance.FindByEnum(Items.Tape).value);
            itemObjects.Add("scissors", ItemMetaStorage.Instance.FindByEnum(Items.Scissors).value);
            itemObjects.Add("apple", ItemMetaStorage.Instance.FindByEnum(Items.Apple).value);
            itemObjects.Add("swinglock", ItemMetaStorage.Instance.FindByEnum(Items.DoorLock).value);
            itemObjects.Add("portalposter", ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value);
            itemObjects.Add("banana", ItemMetaStorage.Instance.FindByEnum(Items.NanaPeel).value);
            itemObjects.Add("buspass", ItemMetaStorage.Instance.FindByEnum(Items.BusPass).value);
            itemObjects.Add("points25", ItemMetaStorage.Instance.GetPointsObject(25, true));
            itemObjects.Add("points50", ItemMetaStorage.Instance.GetPointsObject(50, true));
            itemObjects.Add("points100", ItemMetaStorage.Instance.GetPointsObject(100, true));

            yield return "Setting Up Tiled Editor Prefabs...";
            // tile based objects
            TiledEditorConnectable lockdownVisual = CreateTileVisualFromObject<TiledEditorConnectable, TiledPrefab>(objects.Where(x => x.name == "LockdownDoor").First());
            lockdownVisual.positionOffset = Vector3.up * 21f;
            lockdownVisual.directionAddition = 4f;
            tiledPrefabPrefabs.Add("lockdowndoor", lockdownVisual);

            playerColliderObject = StripAllScripts(Resources.FindObjectsOfTypeAll<PlayerManager>().First().gameObject).GetComponent<CapsuleCollider>();
            playerColliderObject.name = "Player Collider Reference";

            yield return "Setting Misc Prefabs...";
            MainGameManager toCopy = Resources.FindObjectsOfTypeAll<MainGameManager>().First();
            GameObject newObject = new GameObject();
            newObject.SetActive(false);
            mainGameManager = newObject.AddComponent<EditorLevelManager>();
            GameObject ambienceChild = GameObject.Instantiate(toCopy.transform.Find("Ambience").gameObject, mainGameManager.transform);
            mainGameManager.ReflectionSetVariable("ambience", ambienceChild.GetComponent<Ambience>());
            mainGameManager.spawnNpcsOnInit = false;
            mainGameManager.spawnImmediately = false;
            mainGameManager.ReflectionSetVariable("allNotebooksNotification", BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/BAL_CommunityNotebooks"));
            mainGameManager.ReflectionSetVariable("happyBaldiPre", Resources.FindObjectsOfTypeAll<HappyBaldi>().First());
            mainGameManager.ReflectionSetVariable("destroyOnLoad", true);
            mainGameManager.gameObject.name = "CustomEditorGameManager";
            mainGameManager.gameObject.ConvertToPrefab(true);
            SetUpUIPrefabs();
            yield break;
        }

        void SetUpUIPrefabs()
        {
            UIImageComponent imagecomponent = UIComponent.CreateBase<UIImageComponent>();
            imagecomponent.gameObject.AddComponent<Image>();
            assetMan.Add<UIComponent>("image", imagecomponent);
            UIButtonComponent buttoncomponent = UIComponent.CreateBase<UIButtonComponent>();
            buttoncomponent.gameObject.AddComponent<Image>();
            buttoncomponent.button = buttoncomponent.gameObject.ConvertToButton<StandardMenuButton>();
            assetMan.Add<UIComponent>("button", buttoncomponent);
            UITextureComponent texturecomponent = UIComponent.CreateBase<UITextureComponent>();
            texturecomponent.gameObject.AddComponent<RawImage>();
            assetMan.Add<UIComponent>("texture", texturecomponent);
            UILabelComponent labelcomponent = UIComponent.CreateBase<UILabelComponent>();
            labelcomponent.gameObject.AddComponent<TextMeshProUGUI>();
            assetMan.Add<UIComponent>("label", labelcomponent);
        }


        void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                assetMan.Add<Sprite>(prefix + Path.GetFileNameWithoutExtension(paths[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(paths[i]), pixelsPerUnit));
            }
        }

        void AddAudioFolderToAssetMan(params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                SoundObject obj = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(paths[i]), Path.GetFileNameWithoutExtension(paths[i]), SoundType.Effect, Color.white);
                obj.subtitle = false;
                assetMan.Add<SoundObject>("Audio/" + Path.GetFileNameWithoutExtension(paths[i]), obj);
            }
        }

        void AddSolidColorLightmap(string name, Color color)
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            tex.SetPixels(0, 0, 256, 256, colors);
            tex.Apply();
            lightmaps.Add(name, tex);
        }

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.leveleditor");
            //CustomOptionsCore.OnMenuInitialize += OptMenPlaceholder;
            Instance = this;
            AddSolidColorLightmap("white", Color.white);
            AddSolidColorLightmap("yellow", Color.yellow);
            AddSolidColorLightmap("red", Color.red);
            AddSolidColorLightmap("green", Color.green);
            AddSolidColorLightmap("blue", Color.blue);
            assetMan.Add<Texture2D>("Selector", AssetLoader.TextureFromMod(this, "Selector.png"));
            assetMan.Add<Texture2D>("Grid", AssetLoader.TextureFromMod(this, "Grid.png"));
            assetMan.Add<Texture2D>("Cross", AssetLoader.TextureFromMod(this, "Cross.png"));
            assetMan.Add<Texture2D>("CrossMask", AssetLoader.TextureFromMod(this, "CrossMask.png"));
            assetMan.Add<Texture2D>("Border", AssetLoader.TextureFromMod(this, "Border.png"));
            assetMan.Add<Texture2D>("BorderMask", AssetLoader.TextureFromMod(this, "BorderMask.png"));
            assetMan.Add<Texture2D>("Arrow", AssetLoader.TextureFromMod(this, "Arrow.png"));
            assetMan.Add<Texture2D>("ArrowSmall", AssetLoader.TextureFromMod(this, "ArrowSmall.png"));
            assetMan.Add<Texture2D>("Circle", AssetLoader.TextureFromMod(this, "Circle.png"));
            assetMan.Add<Texture2D>("ArrowSmall", AssetLoader.TextureFromMod(this, "ArrowSmall.png"));
            assetMan.Add<Texture2D>("SwingDoorSilent", AssetLoader.TextureFromMod(this, "SwingDoorSilent.png"));
            assetMan.Add<Sprite>("EditorButton", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButton.png"), 1f));
            assetMan.Add<Sprite>("EditorButtonGlow", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButton_Glow.png"), 1f));
            assetMan.Add<Sprite>("EditorButtonFail", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButtonFail.png"), 1f));
            assetMan.Add<Sprite>("LinkSprite", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "LinkSprite.png"), 40f));
            doorTypes.Add("standard", typeof(DoorEditorVisual));
            doorTypes.Add("swing", typeof(SwingEditorVisual));
            doorTypes.Add("autodoor", typeof(AutoDoorEditorVisual));
            doorTypes.Add("swingsilent", typeof(SilentSwingEditorVisual));
            doorTypes.Add("coin", typeof(CoinSwingEditorVisual));
            doorTypes.Add("oneway", typeof(OneWaySwingEditorVisual));
            AddSpriteFolderToAssetMan("UI/", 40, AssetLoader.GetModPath(this), "UI");
            AddAudioFolderToAssetMan(AssetLoader.GetModPath(this), "Audio");
            assetMan.Get<SoundObject>("Audio/BAL_CommunityNotebooks").soundType = SoundType.Voice;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").soundType = SoundType.Voice;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").subtitle = true;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").soundKey = "Please change your resolution in the options menu!";
            assetMan.Get<Sprite>("UI/DitherPattern").texture.wrapMode = TextureWrapMode.Repeat;
            LoadingEvents.RegisterOnAssetsLoaded(Info, AssetsLoadedActual(), false);
            harmony.PatchAllConditionals();
        }
    }
}
