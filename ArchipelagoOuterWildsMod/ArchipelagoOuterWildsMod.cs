using OWML.Common;
using OWML.Common.Menus;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ArchipelagoOuterWildsMod
{
    public class ArchipelagoOuterWildsMod : ModBehaviour
    {
        private static ArchipelagoOuterWildsMod Instance;

        private System.Random rnd;
        private readonly int Seed = 1981184590;
        private bool Testing;
        private TitleAnimationController TitleAnimCon;
        private TitleScreenManager TitleScreenMan;
        private ProfileMenuManager ProfMenuMan;

        private void Awake()
        {
            Instance = this;

            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            Testing = false;
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"Running {nameof(ArchipelagoOuterWildsMod)}!", MessageType.Success);

            // Subscribed events
            ModHelper.Menus.MainMenu.OnInit += OnMainMenuInit;

            // Patched methods
            ModHelper.HarmonyHelper.AddPrefix<TitleScreenManager>("ShowStartupPopupsAndShowMenu", typeof(ArchipelagoOuterWildsMod), nameof(BeforeShowStartupPopupsAndShowMenu));
        }

        private void OnMainMenuInit()
        {
            TitleAnimCon = FindObjectOfType<TitleAnimationController>();
            TitleScreenMan = FindObjectOfType<TitleScreenManager>();
            ProfMenuMan = FindObjectOfType<ProfileMenuManager>();

            TitleAnimCon.OnTitleMenuAnimationComplete += OnTitleMenuAnimationComplete;

            TitleScreenMan._resumeGameObject = ModHelper.Menus.MainMenu.ResumeExpeditionButton.Copy().Button.gameObject;
            TitleScreenMan._resumeGameAction.EnableConfirm(false);
            ModHelper.Menus.MainMenu.ResumeExpeditionButton.Title = "GO";

            ModHelper.Menus.MainMenu.NewExpeditionButton.Hide();

            TitleScreenMan._profileMenuObject = ModHelper.Menus.MainMenu.SwitchProfileButton.Copy().Button.gameObject;
            TitleScreenMan._profileMenuButton = TitleScreenMan._profileMenuObject.GetComponent<Selectable>();
            ModHelper.Menus.MainMenu.SwitchProfileButton.Hide();
            TitleScreenMan._profileMenuButton.enabled = false;
        }

        private static void BeforeShowStartupPopupsAndShowMenu()
        {
            Instance.TitleScreenMan._popupsToShow = StartupPopups.None;
        }

        private void OnTitleMenuAnimationComplete()
        {
            TitleAnimCon.OnTitleMenuAnimationComplete -= OnTitleMenuAnimationComplete;
            var oldPop = TitleScreenMan._okCancelPopup;
            var pop = Instantiate(oldPop);
            pop.transform.parent = oldPop.transform.parent;
            pop.transform.position = oldPop.transform.position;
            pop.transform.localScale = oldPop.transform.localScale;
            pop.ResetPopup();
            var message = "You are testing Joe's mod.";
            var okPrompt = new ScreenPrompt("Agree");
            pop.SetUpPopup(message, InputLibrary.menuConfirm, null, okPrompt, null, true, false);
            pop.OnPopupConfirm += OnStarterPopupConfirm;
            pop.EnableMenu(true);
        }

        private void OnStarterPopupConfirm()
        {
            Locator.GetMenuInputModule().SelectOnNextUpdate(TitleScreenMan._resumeGameButton);
            return;
            var oldPop = ProfMenuMan._createProfileConfirmPopup as PopupInputMenu;
            var pop = Instantiate(oldPop);
            pop.transform.parent = oldPop.transform.parent;
            pop.transform.position = oldPop.transform.position;
            pop.transform.localScale = oldPop.transform.localScale;
            pop.ResetPopup();
            var okPrompt = new ScreenPrompt("Join");
            var cancelPrompt = new ScreenPrompt("Quit");
            pop.SetUpPopup(null, null, null, okPrompt, cancelPrompt);
            pop.OnPopupConfirm += () => OnServerPopupClose(pop.GetInputText());
            pop.OnPopupCancel += () => Application.Quit();
            pop.EnableMenu(true);
            ModHelper.Console.WriteLine($"Game objects: {FindObjectsOfType<GameObject>().Length}", MessageType.Info);
        }

        private void OnServerPopupClose(string inputText)
        {
            if (inputText == "")
            {
                OnStarterPopupConfirm();
                return;
            }
            Locator.GetMenuInputModule().SelectOnNextUpdate(TitleScreenMan._resumeGameButton);
        }

        private void OnCompleteSceneLoad(OWScene origScene, OWScene loadScene)
        {
            if (loadScene == OWScene.SolarSystem)
            {
                if (Testing) OnCompleteSolarSystemLoadTest();
                else OnCompleteSolarSystemLoad();
            }
            else return;
        }

        private void OnCompleteSolarSystemLoad()
        {
            rnd = new System.Random(Seed);

            var rndEye = false;
            if (rndEye) RandomizeEye();

            var rndPlanets = true;
            if (rndPlanets) RandomizePlanets();
        }

        private void RandomizeEye()
        {
            var nomCoordInt = FindObjectOfType<NomaiCoordinateInterface>();
            if (nomCoordInt == null) return;
            ModHelper.Console.WriteLine("Found Nomai coordinate interface!", MessageType.Success);

            ModHelper.Console.WriteLine($"Coordinate X", MessageType.Info);
            for (int i = 0; i < nomCoordInt._coordinateX.Length; i++)
            {
                int newCoord = 0;
                bool done = false;
                while (!done)
                {
                    newCoord = rnd.Next(6);
                    done = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (nomCoordInt._coordinateX[j] == newCoord) { done = false; break; }
                    }
                }
                nomCoordInt._coordinateX[i] = newCoord;
                ModHelper.Console.WriteLine($"{newCoord}", MessageType.Message);
            }

            ModHelper.Console.WriteLine($"Coordinate Y", MessageType.Info);
            for (int i = 0; i < nomCoordInt._coordinateY.Length; i++)
            {
                int newCoord = 0;
                bool done = false;
                while (!done)
                {
                    newCoord = rnd.Next(6);
                    done = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (nomCoordInt._coordinateY[j] == newCoord) { done = false; break; }
                    }
                }
                nomCoordInt._coordinateY[i] = newCoord;
                ModHelper.Console.WriteLine($"{newCoord}", MessageType.Message);
            }

            ModHelper.Console.WriteLine($"Coordinate Z", MessageType.Info);
            for (int i = 0; i < nomCoordInt._coordinateZ.Length; i++)
            {
                int newCoord = 0;
                bool done = false;
                while (!done)
                {
                    newCoord = rnd.Next(6);
                    done = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (nomCoordInt._coordinateZ[j] == newCoord) { done = false; break; }
                    }
                }
                nomCoordInt._coordinateZ[i] = newCoord;
                ModHelper.Console.WriteLine($"{newCoord}", MessageType.Message);
            }
        }

        private void RandomizePlanets()
        {
            var bodNames = new List<string>()
            {
                "BrittleHollow_Body",
                "DarkBramble_Body",
                "SunStation_Body",
                "FocalBody",
                "WhiteHole_Body",
                "TimberHearth_Body",
                "GiantsDeep_Body"
            };

            var origPositions = new Dictionary<string, Vector3>();
            var newMags = new List<float>();
            var offsets = new Dictionary<string, Vector3>();

            foreach (var obj in FindObjectsOfType<AstroObject>())
            {
                var name = obj.name;
                if (!bodNames.Contains(name)) continue;

                var pos = obj.transform.position;
                origPositions.Add(name, pos);

                var newPos = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                if (name == "BrittleHollow_Body" || name == "WhiteHole_Body") newPos.y = 0;
                newPos.Normalize();

                float newMag;
                var done = true;
                do
                {
                    newMag = (float)rnd.NextDouble() * 21000f + 5000f;
                    foreach (var val in newMags)
                    {
                        done = Mathf.Abs(newMag - val) > 2000f;
                        if (!done) break;
                    }
                } while (!done);

                newMags.Add(newMag);
                newPos *= newMag;
                var offset = newPos - pos;
                offsets.Add(name, offset);
            }

            offsets.Add("OrbitalProbeCannon_Body", offsets["GiantsDeep_Body"]);
            offsets.Add("CannonMuzzle_Body", offsets["GiantsDeep_Body"]);
            offsets.Add("Moon_Body", offsets["TimberHearth_Body"]);
            offsets.Add("Satellite_Body", offsets["TimberHearth_Body"]);
            offsets.Add("VolcanicMoon_Body", offsets["BrittleHollow_Body"]);
            offsets.Add("CannonBarrel_Body", offsets["GiantsDeep_Body"]);

            foreach (var body in FindObjectsOfType<OWRigidbody>())
            {
                var parentName = GetParent(body);
                if (!offsets.ContainsKey(parentName)) continue;
                var offset = offsets[parentName];
                if (offset.magnitude > 0)
                {
                    body.SetPosition(body.GetPosition() + offset);
                }
            }

            Physics.SyncTransforms();
        }

        private string GetParent(OWRigidbody body)
        {
            var parent = body.GetOrigParentBody();
            if (parent == null) return body.name;
            return GetParent(parent);
        }

        private void OnCompleteSolarSystemLoadTest()
        {
        }
    }
}
