using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using PlusLevelFormat;
using PlusLevelLoader.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace PlusLevelLoader
{

    [BepInPlugin("mtm101.rulerp.baldiplus.levelloader", "Baldi's Basics Plus Level Loader", "0.1.0.2")]
    public class PlusLevelLoaderPlugin : BaseUnityPlugin
    {
        public static PlusLevelLoaderPlugin Instance;
        public static Level level;

        public static Texture2D TextureFromAlias(string alias)
        {
            if (!Instance.textureAliases.ContainsKey(alias))
            {
                return Instance.assetMan.Get<Texture2D>("Placeholder_Wall"); //return placeholder
            }
            return Instance.textureAliases[alias];
        }

        public AssetManager assetMan = new AssetManager();

        public Dictionary<string, Texture2D> textureAliases = new Dictionary<string, Texture2D>();
        public Dictionary<string, RoomSettings> roomSettings = new Dictionary<string, RoomSettings>();
        public Dictionary<string, Door> doorPrefabs = new Dictionary<string, Door>();
        public Dictionary<string, WindowObject> windowObjects = new Dictionary<string, WindowObject>();
        public Dictionary<string, GameObject> prefabAliases = new Dictionary<string, GameObject>();
        public Dictionary<string, TileBasedObject> tileAliases = new Dictionary<string, TileBasedObject>();
        public Dictionary<string, Activity> activityAliases = new Dictionary<string, Activity>();
        public Dictionary<string, NPC> npcAliases = new Dictionary<string, NPC>();
        public Dictionary<string, ItemObject> itemObjects = new Dictionary<string, ItemObject>();
        public Dictionary<string, GameButtonBase> buttons = new Dictionary<string, GameButtonBase>(); //rest in pieces lever...

        /*void OptMenPlaceholder(OptionsMenu __instance)
        {
            GameObject obj = CustomOptionsCore.CreateNewCategory(__instance, "LOADER");
            StandardMenuButton but = CustomOptionsCore.CreateApplyButton(__instance, "LOAD THE MAP YEAH FUCK YOU!!", () =>
            {
                FileStream stream = File.OpenRead("C:\\Users\\User1\\OneDrive\\Desktop\\experimentalhellscape\\BALDI_Data\\StreamingAssets\\test.lvl");
                BinaryReader reader = new BinaryReader(stream);
                level = reader.ReadLevel();
                LevelAsset asset = CustomLevelLoader.LoadLevel(level);
                reader.Close();
                Resources.FindObjectsOfTypeAll<SceneObject>().Do(x =>
                {
                    x.levelAsset = asset;
                    x.MarkAsNeverUnload();
                    if (x.extraAsset == null) return;
                    x.extraAsset.minLightColor = Color.white;
                    x.extraAsset.npcsToSpawn = new List<NPC>();
                });
            });
            but.transform.SetParent(obj.transform, false);
        }*/

        public GameObject balloon_blue;
        public GameObject balloon_green;
        public GameObject balloon_orange;
        public GameObject balloon_purple;

        public GameObject nana_peel_placed;

        public GameObject price_tag;
        public GameObject price_map;
        public GameObject price_map_child;
        public GameObject price_out;
        public GameObject price_restocking;

        public GameObject challenge_speedy;
        public GameObject challenge_stealthy;
        public GameObject challenge_grapple;

        public GameObject randomevent_fog;
        public GameObject randomevent_flood;
        public GameObject randomevent_gravityChaos;
        public GameObject randomevent_brokenRuler;
        public GameObject randomevent_party;

        public GameObject beltManager;
        public GameObject beltVisual;

        public Direction beltDir;

        public RoomFunctionContainer shopContainer;

        public NPC fastbaldi;
        public NPC principal_allknowing;

        public void CreateEditorPoster(string editorName, PosterObject posterObject)
        {
            GameObject gameObject = new GameObject();
            //poster_beakid.name = "Poster_BeAKid";
            prefabAliases.Add(editorName, gameObject);
            var editorPoster = gameObject.AddComponent<EditorPoster>();
            editorPoster.posterObject = posterObject;
            gameObject.ConvertToPrefab(true);
        }

        IEnumerator OnAssetsLoaded()
        {
            yield return 4;
            yield return "Adding Texture Aliases...";
            assetMan.AddFromResources<Texture2D>();
            assetMan.AddFromResources<Material>();
            assetMan.AddFromResources<Door>();
            assetMan.AddFromResources<Cubemap>();
            assetMan.AddFromResources<GameButtonBase>();
            assetMan.Add<Elevator>("ElevatorPrefab", Resources.FindObjectsOfTypeAll<Elevator>().First());
            assetMan.Remove<Door>("Door_Swinging");
            assetMan.Add<Door>("Door_Swinging", Resources.FindObjectsOfTypeAll<Door>().Where(x => x.name == "Door_Swinging").Where(x => x.transform.parent == null).First());
            assetMan.AddFromResources<WindowObject>();
            assetMan.AddFromResources<StandardDoorMats>();
            textureAliases.Add("HallFloor", assetMan.Get<Texture2D>("TileFloor"));
            textureAliases.Add("Wall", assetMan.Get<Texture2D>("Wall"));
            textureAliases.Add("Ceiling", assetMan.Get<Texture2D>("CeilingNoLight"));
            textureAliases.Add("BlueCarpet", assetMan.Get<Texture2D>("Carpet"));
            textureAliases.Add("FacultyWall", assetMan.Get<Texture2D>("WallWithMolding"));
            textureAliases.Add("Actual", assetMan.Get<Texture2D>("ActualTileFloor"));
            textureAliases.Add("ElevatorCeiling", assetMan.Get<Texture2D>("ElCeiling"));
            textureAliases.Add("Grass", assetMan.Get<Texture2D>("Grass"));
            textureAliases.Add("Fence", assetMan.Get<Texture2D>("fence"));
            //textureAliases.Add("TreeLineWall", assetMan.Get<Texture2D>("Treeline_Wall"));
            textureAliases.Add("JohnnyWall", assetMan.Get<Texture2D>("JohnnyWall"));
            textureAliases.Add("None", assetMan.Get<Texture2D>("Transparent"));

            textureAliases.Add("SaloonWallEditor", assetMan.Get<Texture2D>("SaloonWall"));

            textureAliases.Add("PlaceholderFloorEditor", assetMan.Get<Texture2D>("Placeholder_Floor"));
            textureAliases.Add("PlaceholderWallEditor", assetMan.Get<Texture2D>("Placeholder_Wall_W"));
            textureAliases.Add("PlaceholderCeilingEditor", assetMan.Get<Texture2D>("Placeholder_Celing"));

            //PlusLevelEditor.level.Add("break", new TextureContainer("Actual", "FacultyWall", "Ceiling"));

            yield return "Setting Up Room Settings...";
            List<RoomFunctionContainer> roomFunctions = Resources.FindObjectsOfTypeAll<RoomFunctionContainer>().ToList();
            roomSettings.Add("hall", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            //roomSettings.Add("hall_carpet", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            //roomSettings.Add("hall_tiles", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));

            roomSettings.Add("hall_saloonwall", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            //roomSettings.Add("hall_saloonwall_carpet", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            //roomSettings.Add("hall_saloonwall_tiles", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));

            roomSettings.Add("class", new RoomSettings(RoomCategory.Class, RoomType.Room, Color.green, assetMan.Get<StandardDoorMats>("ClassDoorSet"), assetMan.Get<Material>("MapTile_Classroom")));
            roomSettings.Add("faculty", new RoomSettings(RoomCategory.Faculty, RoomType.Room, Color.red, assetMan.Get<StandardDoorMats>("FacultyDoorSet"), assetMan.Get<Material>("MapTile_Faculty")));
            roomSettings.Add("office", new RoomSettings(RoomCategory.Office, RoomType.Room, new Color(1f,1f,0f), assetMan.Get<StandardDoorMats>("PrincipalDoorSet"), assetMan.Get<Material>("MapTile_Office")));
            roomSettings.Add("closet", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(1f, 0.6214f, 0f), assetMan.Get<StandardDoorMats>("SuppliesDoorSet")));
            roomSettings.Add("closet_carpet", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(1f, 0.6214f, 0f), assetMan.Get<StandardDoorMats>("SuppliesDoorSet")));
            roomSettings.Add("reflex", new RoomSettings(RoomCategory.Null, RoomType.Room, new Color(1f, 1f, 1f), assetMan.Get<StandardDoorMats>("DoctorDoorSet")));
            roomSettings.Add("library", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("cafeteria", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("outside", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            //roomSettings.Add("outside_camphub", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("shop", new RoomSettings(RoomCategory.Store, RoomType.Room, new Color(1f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("nullplaceholder", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 0f, 0f), assetMan.Get<StandardDoorMats>("DefaultDoorSet")));
            roomSettings["faculty"].container = roomFunctions.Find(x => x.name == "FacultyRoomFunction");
            roomSettings["office"].container = roomFunctions.Find(x => x.name == "OfficeRoomFunction");
            roomSettings["class"].container = roomFunctions.Find(x => x.name == "ClassRoomFunction");
            roomSettings["library"].container = roomFunctions.Find(x => x.name == "LibraryRoomFunction");
            roomSettings["cafeteria"].container = roomFunctions.Find(x => x.name == "CafeteriaRoomFunction");
            roomSettings["outside"].container = roomFunctions.Find(x => x.name == "PlaygroundRoomFunction");
            //roomSettings["outside_camphub"].container = roomFunctions.Find(x => x.name == "CampHubRoomFunction"); //Was CampHubRoomFunction

            shopContainer = new GameObject().AddComponent<RoomFunctionContainer>();
            shopContainer.name = "EditorStoreRoomFunction";
            shopContainer.gameObject.AddComponent<EditorStoreRoomFunction>();

            AccessTools.Field(typeof(RoomFunctionContainer), "functions").SetValue(shopContainer.gameObject.GetComponent<RoomFunctionContainer>(), new List<RoomFunction>() { shopContainer.gameObject.GetComponent<EditorStoreRoomFunction>() });
            shopContainer.gameObject.ConvertToPrefab(true);

            roomSettings["shop"].container = shopContainer.GetComponent<RoomFunctionContainer>();

            yield return "Setting Up Prefabs...";
            windowObjects.Add("standard", assetMan.Get<WindowObject>("WoodWindow"));
            doorPrefabs.Add("standard", assetMan.Get<Door>("ClassDoor_Standard"));
            doorPrefabs.Add("swing", assetMan.Get<Door>("Door_Swinging"));
            doorPrefabs.Add("autodoor", assetMan.Get<Door>("Door_Auto"));
            doorPrefabs.Add("coin", assetMan.Get<Door>("Door_SwingingCoin"));
            doorPrefabs.Add("oneway", assetMan.Get<Door>("Door_SwingingOneWay")); //FilingCabinet_Tall
            doorPrefabs.Add("swingsilent", assetMan.Get<Door>("SilentDoor_Swinging"));
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            prefabAliases.Add("desk", objects.Where(x => x.name == "Table_Test").First());
            prefabAliases.Add("bigdesk", objects.Where(x => x.name == "BigDesk").First());
            prefabAliases.Add("cabinettall", objects.Where(x => x.name == "FilingCabinet_Tall").First());
            prefabAliases.Add("cabinetshort", objects.Where(x => x.name == "FilingCabinet_Short").First());
            prefabAliases.Add("chair", objects.Where(x => x.name == "Chair_Test").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("computer", objects.Where(x => x.name == "MyComputer").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("computer_off", objects.Where(x => x.name == "MyComputer_Off").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("roundtable", objects.Where(x => x.name == "RoundTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("locker", objects.Where(x => x.name == "Locker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bluelocker", objects.Where(x => x.name == "BlueLocker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("greenlocker", objects.Where(x => x.name == "StorageLocker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_pencilnotes", objects.Where(x => x.name == "Decor_PencilNotes").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_papers", objects.Where(x => x.name == "Decor_Papers").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_globe", objects.Where(x => x.name == "Decor_Globe").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_notebooks", objects.Where(x => x.name == "Decor_Notebooks").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_lunch", objects.Where(x => x.name == "Decor_Lunch").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bookshelf", objects.Where(x => x.name == "Bookshelf_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bookshelf_hole", objects.Where(x => x.name == "Bookshelf_Hole_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("rounddesk", objects.Where(x => x.name == "RoundDesk").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("cafeteriatable", objects.Where(x => x.name == "CafeteriaTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("dietbsodamachine", objects.Where(x => x.name == "DietSodaMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bsodamachine", objects.Where(x => x.name == "SodaMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("zestymachine", objects.Where(x => x.name == "ZestyMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("crazymachine_bsoda", objects.Where(x => x.name == "CrazyVendingMachineBSODA").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("crazymachine_zesty", objects.Where(x => x.name == "CrazyVendingMachineZesty").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("waterfountain", objects.Where(x => x.name == "WaterFountain").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("counter", objects.Where(x => x.name == "Counter").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("examination", objects.Where(x => x.name == "ExaminationTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("ceilingfan", objects.Where(x => x.name == "CeilingFan").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("merrygoround", objects.Where(x => x.name == "MerryGoRound_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tree", objects.Where(x => x.name == "TreeCG").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("pinetree", objects.Where(x => x.name == "PineTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("pinetreebully", objects.Where(x => x.name == "PineTree_Bully").Where(x => x.transform.parent != null).First());
            prefabAliases.Add("prizestatic", objects.Where(x => x.name == "FirstPrize_SpriteBase").Where(x => x.transform.parent != null).First());
            
            prefabAliases.Add("appletree", objects.Where(x => x.name == "AppleTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bananatree", objects.Where(x => x.name == "BananaTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("hoop", objects.Where(x => x.name == "HoopBase").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("payphone", objects.Where(x => x.name == "PayPhone").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tapeplayer", objects.Where(x => x.name == "TapePlayer").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("plant", objects.Where(x => x.name == "Plant").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_banana", objects.Where(x => x.name == "Decor_Banana").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_zoneflag", objects.Where(x => x.name == "Decor_ZoningFlag").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("hopscotch", objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First());

            randomevent_fog = new GameObject();
            randomevent_fog.ConvertToPrefab(true);
            prefabAliases.Add("randomevent_fog", randomevent_fog);
            randomevent_fog.name = "EventObject_Fog";
            randomevent_fog.AddComponent<EditorRandomEvent>().randomEventToUse = EditorRandomEvent.RandomEventTypeThing.Fog;

            randomevent_flood = new GameObject();
            randomevent_flood.ConvertToPrefab(true);
            prefabAliases.Add("randomevent_flood", randomevent_flood);
            randomevent_flood.name = "EventObject_Flood";
            randomevent_flood.AddComponent<EditorRandomEvent>().randomEventToUse = EditorRandomEvent.RandomEventTypeThing.Flood;

            randomevent_gravityChaos = new GameObject();
            randomevent_gravityChaos.ConvertToPrefab(true);
            prefabAliases.Add("randomevent_gravitychaos", randomevent_gravityChaos);
            randomevent_gravityChaos.name = "EventObject_GravityChaos";
            randomevent_gravityChaos.AddComponent<EditorRandomEvent>().randomEventToUse = EditorRandomEvent.RandomEventTypeThing.GravityChaos;

            randomevent_brokenRuler = new GameObject();
            randomevent_brokenRuler.ConvertToPrefab(true);
            prefabAliases.Add("randomevent_brokenruler", randomevent_brokenRuler);
            randomevent_brokenRuler.name = "EventObject_BrokenRuler";
            randomevent_brokenRuler.AddComponent<EditorRandomEvent>().randomEventToUse = EditorRandomEvent.RandomEventTypeThing.BrokenRuler;

            challenge_speedy = new GameObject();
            challenge_speedy.ConvertToPrefab(true);
            prefabAliases.Add("challenge_speedy", challenge_speedy);
            challenge_speedy.name = "ChallengeObject_Speedy";
            challenge_speedy.AddComponent<EditorChallengeManagerAdd>().challengeToUse = EditorChallengeManagerAdd.ChallengeTypeThing.Speedy;

            /*randomevent_party = new GameObject();
            randomevent_party.ConvertToPrefab(true);
            prefabAliases.Add("randomevent_party", randomevent_party);
            randomevent_party.name = "EventObject_Party";
            randomevent_party.AddComponent<EditorRandomEvent>().randomEventToUse = EditorRandomEvent.RandomEventTypeThing.Party;*/

            prefabAliases.Add("picnictable", objects.Where(x => x.name == "PicnicTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("picnicbasket", objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("minigame1trigger", objects.Where(x => x.name == "Minigame1Trigger").Where(x => x.transform.parent.name == "ObjectBase").First());
            prefabAliases.Add("minigame2trigger", objects.Where(x => x.name == "Minigame2Trigger").Where(x => x.transform.parent.name == "ObjectBase").First());

            price_tag = objects.Where(x => x.name == "PriceTag").Where(x => x.transform.parent.name == "RoomBase").First();
            prefabAliases.Add("pricetag", price_tag);
            price_tag.AddComponent<EditorPriceTag>();
            price_tag.name = "PriceTag";

            prefabAliases.Add("dirtcircle", objects.Where(x => x.name == "DirtCircle").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tent", objects.Where(x => x.name == "Tent_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("rock", objects.Where(x => x.name == "Rock").Where(x => x.transform.parent == null).First());

            prefabAliases.Add("johnny", objects.Where(x => x.name == "JohnnyBase").Where(x => x.transform.parent.name == "RoomBase").First());
            prefabAliases.Add("cashregister", objects.Where(x => x.name == "CashRegister").Where(x => x.transform.parent.name == "RoomBase").First());
            prefabAliases.Add("johnnysign", objects.Where(x => x.name == "JohnnySign").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("campfire", objects.Where(x => x.name == "CampFire").Where(x => x.transform.parent == null).First());

            //prefabAliases.Add("shrinkmachine", objects.Where(x => x.name == "ShrinkMachine").Where(x => x.transform.parent == null).First());

            price_map = objects.Where(x => x.name == "PriceTag_1").Where(x => x.transform.parent.name == "RoomBase").First();
            prefabAliases.Add("pricetagmap", price_map);
            price_map.AddComponent<EditorPriceTag_Map>();
            price_map.name = "PriceTag_Map";

            price_out = objects.Where(x => x.name == "PriceTag_2").Where(x => x.transform.parent.name == "RoomBase").First();
            prefabAliases.Add("pricetagout", price_out);
            price_out.AddComponent<EditorPriceTag_Out>();
            price_out.name = "PriceTag_Out";

            price_restocking = objects.Where(x => x.name == "PriceTag_3").Where(x => x.transform.parent.name == "RoomBase").First();
            prefabAliases.Add("pricetagrestocking", price_restocking);
            price_restocking.AddComponent<EditorPriceTag_Restocking>();
            price_restocking.name = "PriceTag_Restocking";

            nana_peel_placed = new GameObject();
            prefabAliases.Add("nanapeelplaced", nana_peel_placed);
            nana_peel_placed.AddComponent<EditorNanaPeelSpawning>();
            nana_peel_placed.ConvertToPrefab(true);

            prefabAliases.Add("decor_exitsign", objects.Where(x => x.name == "Decor_ExitSign").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("fluorescentlight", objects.Where(x => x.name == "FluorescentLight").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("hanginglight", objects.Where(x => x.name == "HangingLight").Where(x => x.transform.parent == null).First());

            //prefabAliases.Add("bus", objects.Where(x => x.name == "Bus").Where(x => x.transform.parent == null).First());
            //prefabAliases.Add("bus", objects.Where(x => x.name == "BusObjects").Where(x => x.transform.parent.name == "FieldTripEntranceRoomFunction").First());
            prefabAliases.Add("bus", objects.Where(x => x.name == "Bus").Where(x => x.transform.parent.name == "BusObjects").Where(x => x.transform.parent.parent.name == "FieldTripEntranceRoomFunction").First());

            GameObject baseBusBaldi = Instantiate(objects.Where(x => x.name == "Bus").Where(x => x.transform.parent.name == "BusObjects").Where(x => x.transform.parent.parent.name == "FieldTripEntranceRoomFunction").First());
            baseBusBaldi.ConvertToPrefab(true);
            baseBusBaldi.SetActive(true);
            baseBusBaldi.name = "Bus_Occupied";
            baseBusBaldi.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetMainTexture(Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "BaldisBus_Occupied"));
            prefabAliases.Add("busbaldi", baseBusBaldi);

            /*GameObject platter = Instantiate(objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First());
            platter.ConvertToPrefab(true);
            platter.SetActive(true);
            platter.name = "Platter";
            platter.GetComponent<SpriteRenderer>().sprite = assetMan.Get<Sprite>("Platter");
            prefabAliases.Add("platter", platter);*/

            //prefabAliases.Add("prizestatic", objects.Where(x => x.transform.parent.name == "FirstPrize_SpriteBase").Where(x => x.transform.parent != null).Where(x => x.transform.parent.parent.name == "ObjectBase").First());

            balloon_blue = objects.Where(x => x.name == "Balloon_Blue").Where(x => x.transform.parent == null).First();
            prefabAliases.Add("balloon_blue", balloon_blue);
            Destroy(balloon_blue.GetComponent<Balloon>());
            balloon_blue.AddComponent<EditorBalloon>();

            balloon_green = objects.Where(x => x.name == "Balloon_Green").Where(x => x.transform.parent == null).First();
            prefabAliases.Add("balloon_green", balloon_green);
            Destroy(balloon_green.GetComponent<Balloon>());
            balloon_green.AddComponent<EditorBalloon>();

            balloon_orange = objects.Where(x => x.name == "Balloon_Orange").Where(x => x.transform.parent == null).First();
            prefabAliases.Add("balloon_orange", balloon_orange);
            Destroy(balloon_orange.GetComponent<Balloon>());
            balloon_orange.AddComponent<EditorBalloon>();

            balloon_purple = objects.Where(x => x.name == "Balloon_Purple").Where(x => x.transform.parent == null).First();
            prefabAliases.Add("balloon_purple", balloon_purple);
            Destroy(balloon_purple.GetComponent<Balloon>());
            balloon_purple.AddComponent<EditorBalloon>();

            beltManager = objects.Where(x => x.name == "BeltManager").Where(x => x.transform.parent == null).First();
            beltVisual = objects.Where(x => x.name == "ConveyorBelt").Where(x => x.transform.parent == null).First();

            prefabAliases.Add("conveyorbelt", beltManager);

            beltVisual.transform.SetParent(beltManager.transform);
            beltManager.GetComponent<BeltManager>().AddBelt(beltVisual.GetComponent<MeshRenderer>());
            //beltManager.GetComponent<BeltManager>().Initialize();
            //beltManager.GetComponent<BeltManager>().SetDirection(Directions.DirFromVector3(beltManager.transform.forward, 45f));
            Vector3 size = new Vector3(6f, 10f, 6f);
            beltManager.GetComponent<BeltManager>().BoxCollider.size = size;
            beltManager.AddComponent<EditorConveyorBelt>();
            Traverse.Create(beltManager.GetComponentInChildren<AudioManager>()).Field("disableSubtitles").SetValue(true);
            //beltManager.GetComponentInChildren<AudioManager>().disableSubtitles = true;
            //Destroy(beltManager.GetComponentInChildren<VA_AudioSource>());
            //Destroy(beltManager.GetComponentInChildren<AudioManager>());
            //beltManager.GetComponentInChildren<AudioSource>().clip = Resources.FindObjectsOfTypeAll<AudioClip>().Where(x => x.name == "ConveyorBeltLoop").First();
            //beltManager.GetComponentInChildren<AudioSource>().loop = true;
            //beltManager.GetComponentInChildren<AudioSource>().Play();

            CreateEditorPoster("poster_pri_baldi", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BaldiPoster"));
            CreateEditorPoster("poster_pri_principal", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PrincipalPoster"));
            CreateEditorPoster("poster_pri_sweep", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "SweepPoster"));
            CreateEditorPoster("poster_pri_playtime", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PlaytimePoster"));
            CreateEditorPoster("poster_pri_bully", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BullyPoster"));
            CreateEditorPoster("poster_pri_crafters", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CraftersPoster"));
            CreateEditorPoster("poster_pri_prize", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PrizePoster"));
            CreateEditorPoster("poster_pri_cloud", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CumuloPoster"));
            CreateEditorPoster("poster_pri_chalk", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "ChalkPoster"));
            CreateEditorPoster("poster_pri_beans", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BeansPoster"));
            CreateEditorPoster("poster_pri_pomp", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PompPoster"));
            CreateEditorPoster("poster_pri_test", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TheTestPoster"));
            CreateEditorPoster("poster_pri_reflex", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "ReflexPoster"));

            CreateEditorPoster("poster_hint_boots", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Boots"));
            CreateEditorPoster("poster_hint_nosquee", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_NoSquee"));
            CreateEditorPoster("poster_hint_phone", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Phone"));
            CreateEditorPoster("poster_hint_read", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Read"));
            CreateEditorPoster("poster_hint_rules", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Rules"));
            CreateEditorPoster("poster_hint_scissors", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_Scissors"));
            CreateEditorPoster("poster_hint_ytps", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_YTPs"));

            CreateEditorPoster("poster_numbers", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_Numbers"));
            CreateEditorPoster("poster_baldiburied", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiBuried"));

            CreateEditorPoster("poster_beakid", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_BeAKid"));
            CreateEditorPoster("poster_candy", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_Candy"));
            CreateEditorPoster("poster_what", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "PHT_What"));

            CreateEditorPoster("poster_txt_inspiration", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TXT_Inspiration"));
            CreateEditorPoster("poster_txt_recycle", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "TXT_Recycle"));

            CreateEditorPoster("poster_mailcomic1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_0"));
            CreateEditorPoster("poster_mailcomic2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_1"));
            CreateEditorPoster("poster_mailcomic3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_2"));
            CreateEditorPoster("poster_mailcomic4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CMC_Mail_3"));

            CreateEditorPoster("poster_baldisays1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_1"));
            CreateEditorPoster("poster_baldisays2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_2"));
            CreateEditorPoster("poster_baldisays3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_3"));
            CreateEditorPoster("poster_baldisays4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_4"));
            CreateEditorPoster("poster_baldisays5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_5"));
            CreateEditorPoster("poster_baldisays6", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_6"));
            CreateEditorPoster("poster_baldisays7", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_7"));
            CreateEditorPoster("poster_baldisays8", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_8"));
            CreateEditorPoster("poster_baldisays9", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "CLS_BaldiSays_9"));
            CreateEditorPoster("poster_baldisays_saveandquit", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "HNT_SaveQuit"));

            CreateEditorPoster("poster_blt_all", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_All"));
            CreateEditorPoster("poster_blt_blah", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Blah"));
            CreateEditorPoster("poster_blt_budget", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Budget"));
            CreateEditorPoster("poster_blt_computer", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Computer"));
            CreateEditorPoster("poster_blt_fired", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Fired"));
            CreateEditorPoster("poster_blt_hawaii", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Hawaii"));
            CreateEditorPoster("poster_blt_heykid", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_HeyKid"));
            CreateEditorPoster("poster_blt_meeting", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Meeting"));
            CreateEditorPoster("poster_blt_review", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "BLT_Review"));

            CreateEditorPoster("poster_chk_apple", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Apple"));
            CreateEditorPoster("poster_chk_baldisays", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_BaldiSays"));
            CreateEditorPoster("poster_chk_chalk", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Chalk"));
            CreateEditorPoster("poster_chk_cheese", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Cheese"));
            CreateEditorPoster("poster_chk_g", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_G"));
            CreateEditorPoster("poster_chk_hangman", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Hangman"));
            CreateEditorPoster("poster_chk_mathh", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Mathh"));
            CreateEditorPoster("poster_chk_possible", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Possible"));
            CreateEditorPoster("poster_chk_treehint", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_TreeHint"));
            CreateEditorPoster("poster_chk_world", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_World"));
            CreateEditorPoster("poster_chk_wow", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_Wow"));
            CreateEditorPoster("poster_chk_baldisboard", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Chk_baldisboard"));

            CreateEditorPoster("poster_storeneon1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_0"));
            CreateEditorPoster("poster_storeneon2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_1"));
            CreateEditorPoster("poster_storeneon3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_2"));
            CreateEditorPoster("poster_storeneon4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_3"));
            CreateEditorPoster("poster_storeneon5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "StoreNeon_4"));

            CreateEditorPoster("poster_kick1", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick1"));
            CreateEditorPoster("poster_kick2", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick2"));
            CreateEditorPoster("poster_kick3", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick3"));
            CreateEditorPoster("poster_kick4", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick4"));
            CreateEditorPoster("poster_kick5", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick5"));
            CreateEditorPoster("poster_kick6", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick6"));
            CreateEditorPoster("poster_kick7", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick7"));
            CreateEditorPoster("poster_kick8", Resources.FindObjectsOfTypeAll<PosterObject>().First(x => x.name == "Kick8"));


            TileBasedObject[] tiledObjects = Resources.FindObjectsOfTypeAll<TileBasedObject>();

            tileAliases.Add("lockdowndoor", tiledObjects.Where(x => x.name == "LockdownDoor").Where(x => x.transform.parent == null).First());

            Activity[] activites = Resources.FindObjectsOfTypeAll<Activity>();
            activityAliases.Add("notebook", activites.Where(x => x.name == "NoActivity").First());
            activityAliases.Add("mathmachine", activites.Where(x => (x.name == "MathMachine" && (x.transform.parent == null))).First());
            activityAliases.Add("mathmachine_corner", activites.Where(x => (x.name == "MathMachine_Corner" && (x.transform.parent == null))).First());
            npcAliases.Add("baldi", MTM101BaldiDevAPI.npcMetadata.Get(Character.Baldi).value);
            npcAliases.Add("principal", MTM101BaldiDevAPI.npcMetadata.Get(Character.Principal).value);
            npcAliases.Add("sweep", MTM101BaldiDevAPI.npcMetadata.Get(Character.Sweep).value);
            npcAliases.Add("playtime", MTM101BaldiDevAPI.npcMetadata.Get(Character.Playtime).value);
            npcAliases.Add("chalkface", MTM101BaldiDevAPI.npcMetadata.Get(Character.Chalkles).value);
            npcAliases.Add("bully", MTM101BaldiDevAPI.npcMetadata.Get(Character.Bully).value);
            npcAliases.Add("beans", MTM101BaldiDevAPI.npcMetadata.Get(Character.Beans).value);
            npcAliases.Add("prize", MTM101BaldiDevAPI.npcMetadata.Get(Character.Prize).value);
            npcAliases.Add("crafters", MTM101BaldiDevAPI.npcMetadata.Get(Character.Crafters).value);
            npcAliases.Add("pomp", MTM101BaldiDevAPI.npcMetadata.Get(Character.Pomp).value);
            npcAliases.Add("test", MTM101BaldiDevAPI.npcMetadata.Get(Character.LookAt).value);
            npcAliases.Add("cloudy", MTM101BaldiDevAPI.npcMetadata.Get(Character.Cumulo).value);
            npcAliases.Add("reflex", MTM101BaldiDevAPI.npcMetadata.Get(Character.DrReflex).value);

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

            buttons.Add("button", assetMan.Get<GameButtonBase>("GameButton"));
            yield break;
        }

        void Awake()
        {
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded(), false);
            Instance = this;
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.levelloader");

            harmony.PatchAllConditionals();
        }
    }
}
