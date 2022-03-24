using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoOuterWildsMod
{
    public class ArchipelagoOuterWildsMod : ModBehaviour
    {
        private static ArchipelagoOuterWildsMod Instance;
        private System.Random RND;
        private static int Seed;// = 1981184590;
        private readonly bool Testing = true;
        private static readonly StandaloneProfileManager ProfMan = StandaloneProfileManager.SharedInstance;
        private TitleAnimationController TitleAnimCon;
        private TitleScreenManager TitleScreenMan;
        private ProfileMenuManager ProfMenuMan;
        private ArchipelagoSession Session;
        private string SlotName;
        private static LoginResult Login;
        private bool RNDEye, RNDPlanets, RNDMoons;

        private void Awake()
        {
            Instance = this;

            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"Running {nameof(ArchipelagoOuterWildsMod)}!", MessageType.Success);
            ModHelper.Console.WriteLine($"{ModHelper.Manifest.Version}", MessageType.Info);

            // Subscribed events
            ModHelper.Menus.MainMenu.OnInit += OnMainMenuInit;

            // Patched methods
            ModHelper.HarmonyHelper.AddPrefix<StandaloneProfileManager>("InitializeProfileData", typeof(ArchipelagoOuterWildsMod), nameof(BeforeInitializeProfileData));
            ModHelper.HarmonyHelper.AddPrefix<TitleScreenManager>("TryShowStartupPopupsAndShowMenu", typeof(ArchipelagoOuterWildsMod), nameof(BeforeTryShowStartupPopupsAndShowMenu));
        }

        private void OnMainMenuInit()
        {
            //ProfMan.OnProfileReadDone += OnProfileReadDone;

            TitleAnimCon = FindObjectOfType<TitleAnimationController>();
            TitleScreenMan = FindObjectOfType<TitleScreenManager>();
            ProfMenuMan = FindObjectOfType<ProfileMenuManager>();

            TitleAnimCon.OnTitleMenuAnimationComplete += OnTitleMenuAnimationComplete;

            TitleScreenMan._resumeGameObject = ModHelper.Menus.MainMenu.ResumeExpeditionButton.Copy().Button.gameObject;
            TitleScreenMan._resumeGameAction.EnableConfirm(false);
            ModHelper.Menus.MainMenu.ResumeExpeditionButton.Title = "GO";

            ModHelper.Menus.MainMenu.NewExpeditionButton.Hide();

            /*
            TitleScreenMan._profileMenuObject = ModHelper.Menus.MainMenu.SwitchProfileButton.Copy().Button.gameObject;
            TitleScreenMan._profileMenuButton = TitleScreenMan._profileMenuObject.GetComponent<Selectable>();
            ModHelper.Menus.MainMenu.SwitchProfileButton.Hide();
            TitleScreenMan._profileMenuButton.enabled = false;
            */
        }

        private static void BeforeInitializeProfileData()
        {
            if (Login != null)
            {
                if (!ProfMan.TryCreateProfile(Seed.ToString()) && !ProfMan.SwitchProfile(Seed.ToString()))
                {
                    Instance.ModHelper.Console.WriteLine("Failed to create/find session profile!", MessageType.Fatal);
                    Application.Quit();
                }

                return;
            }

            if (!ProfMan.TryCreateProfile("ArchipelagoTemp") && !ProfMan.SwitchProfile("ArchipelagoTemp"))
            {
                Instance.ModHelper.Console.WriteLine("Failed to create/find temporary profile!", MessageType.Fatal);
                Application.Quit();
            }
        }

        private static void BeforeTryShowStartupPopupsAndShowMenu()
        {
            Instance.TitleScreenMan._popupsToShow = StartupPopups.None;
        }

        private void OnTitleMenuAnimationComplete()
        {
            TitleAnimCon.OnTitleMenuAnimationComplete -= OnTitleMenuAnimationComplete;
            CreateStartupPopup();
        }
        
        private void CreateStartupPopup()
        {
            var oldPop = TitleScreenMan._okCancelPopup;
            var pop = Instantiate(oldPop);
            pop.transform.parent = oldPop.transform.parent;
            pop.transform.position = oldPop.transform.position;
            pop.transform.localScale = oldPop.transform.localScale;

            pop.ResetPopup();

            var message = "You are testing Joe's mod.";
            var okPrompt = new ScreenPrompt("Agree");
            pop.SetUpPopup(message, InputLibrary.menuConfirm, null, okPrompt, null, true, false);

            pop.OnPopupConfirm += OnStartupPopupConfirm;

            pop.EnableMenu(true);
        }

        private void OnStartupPopupConfirm()
        {
            if (Login == null)
            {
                OnPopupsComplete();
                return;
            }

            CreateHostPopup();
        }

        private void CreateHostPopup(string message = "")
        {
            message += "Enter Host";
            var pop = CreateInputPopup("Next", "Quit", message);

            pop.OnPopupConfirm += () => OnHostPopupConfirm(pop.GetInputText());
            pop.OnPopupCancel += Application.Quit;
        }

        private PopupInputMenu CreateInputPopup(string okPromptStr, string cancelPromptStr, string message)
        {
            var oldPop = ProfMenuMan._createProfileConfirmPopup as PopupInputMenu;
            var pop = Instantiate(oldPop);
            pop.transform.parent = oldPop.transform.parent;
            pop.transform.position = oldPop.transform.position;
            pop.transform.localScale = oldPop.transform.localScale;

            pop.ResetPopup();

            var okPrompt = new ScreenPrompt(okPromptStr);
            var cancelPrompt = new ScreenPrompt(cancelPromptStr);
            pop.SetUpPopup(null, null, null, okPrompt, cancelPrompt);

            var refText = TitleScreenMan._okCancelPopup._labelText;
            var text = Instantiate(refText);
            text.transform.parent = pop.transform;
            text.transform.position = refText.transform.position;
            text.transform.localScale = refText.transform.localScale;
            var oldText = pop._labelText;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            oldText.GetComponent<Graphic>().enabled = false;

            Destroy(pop._inputField.placeholder);

            pop.EnableMenu(true);

            return pop;
        }

        private void OnHostPopupConfirm(string host)
        {
            string hostname;
            int port;

            if (host.Length == 0)
            {
                CreateHostPopup("Host required\n");
                return;
            }
            else if (host.Contains(":"))
            {
                var split = host.Split(':');
                if (!int.TryParse(split[1], out port))
                {
                    CreateHostPopup("Invalid format\n");
                    return;
                }
                hostname = split[0];
            }
            else if (int.TryParse(host, out port)) hostname = "archipelago.gg";
            else
            {
                hostname = host;
                port = 38281;
            }

            Session = ArchipelagoSessionFactory.CreateSession(hostname, port);

            CreateNamePopup();
        }

        private void CreateNamePopup(string message = "")
        {
            message += "Enter Name";
            var pop = CreateInputPopup("Join", "Back", message);
            var cancelled = false;

            pop.OnPopupConfirm += () =>
            {
                SlotName = pop.GetInputText();
                OnNamePopupConfirm();
            };
            pop.OnPopupCancel += () =>
            {
                if (!cancelled)
                {
                    cancelled = true;
                    CreateHostPopup();
                }
            };
        }

        private void OnNamePopupConfirm()
        {
            if (SlotName.Length == 0)
            {
                CreateNamePopup("Name required\n");
                return;
            }

            Login = Session.TryConnectAndLogin("Outer Wilds", SlotName, new Version(2, 1, 0), ItemsHandlingFlags.AllItems);

            HandleLoginResult();
        }

        private void HandleLoginResult()
        {
            // if you just wanna mess around, remove the ! from the line below :)
            if (Login.Successful) HandleLoginFail(Login as LoginFailure);
            else HandleLoginSuccess(Login as LoginSuccessful);
        }

        private void HandleLoginFail(LoginFailure login)
        {
            var errCodes = new List<ConnectionRefusedError>(login.ErrorCodes);

            Action<string> act;

            if (errCodes.Contains(ConnectionRefusedError.InvalidItemsHandling)) act = Quit;
            else if (errCodes.Count == 0
                || errCodes.Contains(ConnectionRefusedError.IncompatibleVersion))
                act = CreateHostPopup;
            else if (errCodes.Contains(ConnectionRefusedError.InvalidSlot)
                || errCodes.Contains(ConnectionRefusedError.InvalidGame)
                || errCodes.Contains(ConnectionRefusedError.SlotAlreadyTaken))
                act = CreateNamePopup;
            else act = CreatePasswordPopup;

            var message = "";

            foreach (var err in login.Errors)
            {
                ModHelper.Console.WriteLine(err, MessageType.Error);
                message += err + "\n";
            }

            act(message);
        }

        private void Quit(string message = "You shouldn't see this.")
        {
            ModHelper.Console.WriteLine(message, MessageType.Fatal);
            Application.Quit();
        }

        private void CreatePasswordPopup(string message = "You shouldn't see this.")
        {
            message += "Enter Password";
            var pop = CreateInputPopup("Join", "Back", message);
            var cancelled = false;

            pop.OnPopupConfirm += () => OnPasswordPopupConfirm(pop.GetInputText());
            pop.OnPopupCancel += () =>
            {
                if (!cancelled)
                {
                    cancelled = true;
                    CreateNamePopup();
                }
            };
        }

        private void OnPasswordPopupConfirm(string password)
        {
            if (password.Length == 0)
            {
                CreatePasswordPopup("Password required");
                return;
            }

            Login = Session.TryConnectAndLogin("Outer Wilds", SlotName, new Version(2, 1, 0), ItemsHandlingFlags.AllItems, null, null, password);
            HandleLoginResult();
        }

        private void HandleLoginSuccess(LoginSuccessful login)
        {
            Seed = new System.Random().Next();
            RNDEye = true;
            RNDPlanets = true;
            RNDMoons = false;
            ProfMan.DeleteProfile("ArchipelagoTemp");

            LoadManager.ReloadScene();
        }

        private void OnPopupsComplete()
        {
            var button = TitleScreenMan._resumeGameButton;
            button.GetComponent<SelectableAudioPlayer>().SilenceNextSelectEvent();
            Locator.GetMenuInputModule().SelectOnNextUpdate(button);
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
            RND = new System.Random(Seed);
            if (RNDEye) RandomizeEyeCoords();
            if (RNDPlanets) RandomizePlanets();
            if (RNDMoons) RandomizeMoons();
        }

        private void RandomizeEyeCoords()
        {
            var nomCoordInt = FindObjectOfType<NomaiCoordinateInterface>();
            if (nomCoordInt == null) return;
            ModHelper.Console.WriteLine("Found Nomai coordinate interface!", MessageType.Success);

            ModHelper.Console.WriteLine("Original Coordinate X", MessageType.Info);
            foreach (var coord in nomCoordInt._coordinateX) ModHelper.Console.WriteLine(coord.ToString(), MessageType.Message);
            ModHelper.Console.WriteLine("Original Coordinate Y", MessageType.Info);
            foreach (var coord in nomCoordInt._coordinateY) ModHelper.Console.WriteLine(coord.ToString(), MessageType.Message);
            ModHelper.Console.WriteLine("Original Coordinate Z", MessageType.Info);
            foreach (var coord in nomCoordInt._coordinateZ) ModHelper.Console.WriteLine(coord.ToString(), MessageType.Message);

            ModHelper.Console.WriteLine($"New Coordinate X", MessageType.Info);
            RandomizeEyeCoord(ref nomCoordInt._coordinateX);
            ModHelper.Console.WriteLine($"New Coordinate Y", MessageType.Info);
            RandomizeEyeCoord(ref nomCoordInt._coordinateY);
            ModHelper.Console.WriteLine($"New Coordinate Z", MessageType.Info);
            RandomizeEyeCoord(ref nomCoordInt._coordinateZ);
        }

        private void RandomizeEyeCoord(ref int[] coord)
        {
            coord = new int[RND.Next(5) + 2];

            for (var i = 0; i < coord.Length; i++)
            {
                int newCoord;
                var done = true;

                do
                {
                    newCoord = RND.Next(6);
                    for (var j = 0; j < coord.Length; j++)
                    {
                        done = newCoord != coord[j];
                        if (!done) break;
                    }
                } while (!done);

                coord[i] = newCoord;
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

            //var origPos = new Dictionary<string, Vector3>();
            var mags = new List<float>();
            var offsets = new Dictionary<string, Vector3>();

            foreach (var obj in FindObjectsOfType<AstroObject>())
            {
                var name = obj.name;
                if (!bodNames.Contains(name)) continue;

                var origPos = obj.transform.position;
                var pos = new Vector3((float)RND.NextDouble(), (float)RND.NextDouble(), (float)RND.NextDouble());
                if (name == "BrittleHollow_Body" || name == "WhiteHole_Body") pos.y = 0;
                pos.Normalize();
                float mag;
                var done = true;

                do
                {
                    mag = (float)RND.NextDouble() * 21000f + 5000f;
                    foreach (var val in mags)
                    {
                        done = Mathf.Abs(mag - val) > 2000f;
                        if (!done) break;
                    }
                } while (!done);

                mags.Add(mag);
                pos *= mag;
                var offset = pos - origPos;
                offsets.Add(name, offset);

                var rot = new Vector3((float)RND.NextDouble(), (float)RND.NextDouble(), (float)RND.NextDouble()) * 360;
                obj.transform.rotation.eulerAngles.Set((float)RND.NextDouble(), (float)RND.NextDouble(), (float)RND.NextDouble());
                var angs = obj.transform.rotation.eulerAngles;
                ModHelper.Console.WriteLine($"{angs.x} {angs.y} {angs.z}", MessageType.Info);
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

        private void RandomizeMoons()
        {
            var bodNames = new List<string>()
            {
                "OrbitalProbeCannon_Body",
                "Moon_Body",
                "VolcanicMoon_Body"
            };
        }

        private void OnCompleteSolarSystemLoadTest()
        {
            var orbCanHoloProj = FindObjectOfType<OrbitalCannonHologramProjector>();
            GameObject eyeCoords = null;

            foreach (var obj in orbCanHoloProj._holograms)
            {
                if (obj.name == "Hologram_EyeCoordinates") eyeCoords = obj;
            }

            var eyeCoordsMesh = new Mesh();
            eyeCoords.GetComponentInChildren<MeshFilter>().mesh = eyeCoordsMesh;
            var nomCoordInt = FindObjectOfType<NomaiCoordinateInterface>();
            int[] coordX = nomCoordInt._coordinateX, coordY = nomCoordInt._coordinateY, coordZ = nomCoordInt._coordinateZ;
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            var pos = Vector3.right * (1 - 1f / 4);
            DrawEyeCoordMesh(pos, coordX, ref verts, ref tris);
            pos = Vector3.zero;
            DrawEyeCoordMesh(pos, coordY, ref verts, ref tris);
            pos = Vector3.right * (1f / 4 - 1);
            DrawEyeCoordMesh(pos, coordZ, ref verts, ref tris);
            eyeCoordsMesh.SetVertices(verts);
            eyeCoordsMesh.SetTriangles(tris, 0);
        }

        private void DrawEyeCoordMesh(Vector3 pos, int[] coord, ref List<Vector3> verts, ref List<int> tris)
        {
            for (var i = 0; i < coord.Length - 1; i++)
            {
                var posAngle = (5 - coord[i]) * Math.PI / 3;
                var curPos = new Vector3((float)Math.Cos(posAngle), 0, (float)Math.Sin(posAngle)) / 4 + pos;
                var nextPosAngle = (5 - coord[i + 1]) * Math.PI / 3;
                var nextPos = new Vector3((float)Math.Cos(nextPosAngle), 0, (float)Math.Sin(nextPosAngle)) / 4 + pos;
                double lookAngle;
                lookAngle = Math.Atan2(nextPos.z - curPos.z, nextPos.x - curPos.x) + Math.PI / 2;

                Vector3[] newVerts =
                {
                    new Vector3((float)Math.Cos(lookAngle), 0, (float)Math.Sin(lookAngle)) / 32 + curPos,
                    new Vector3((float)Math.Cos(lookAngle), 0, (float)Math.Sin(lookAngle)) / 32 + nextPos,
                    new Vector3((float)Math.Cos(lookAngle + Math.PI), 0, (float)Math.Sin(lookAngle + Math.PI)) / 32 + curPos,
                    new Vector3((float)Math.Cos(lookAngle + Math.PI), 0, (float)Math.Sin(lookAngle + Math.PI)) / 32 + nextPos
                };

                var triStart = verts.Count;
                verts.AddRange(newVerts);

                int[] newTris =
                {
                    triStart, triStart + 1, triStart + 2,
                    triStart + 1, triStart + 3, triStart + 2
                };

                tris.AddRange(newTris);
            }
        }
    }
}
