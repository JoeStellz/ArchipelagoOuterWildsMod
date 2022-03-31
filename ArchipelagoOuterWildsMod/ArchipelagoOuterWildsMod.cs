using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoOuterWildsMod
{
    public class ArchipelagoOuterWildsMod : ModBehaviour
    {
        private static ArchipelagoOuterWildsMod Instance;
        private System.Random RND;
        private static int Seed;// = 1981184590;
        private readonly bool Testing = false;
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
            if (Login != null)
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

            RandomizeEyeCoord(ref nomCoordInt._coordinateX);
            RandomizeEyeCoord(ref nomCoordInt._coordinateY);
            RandomizeEyeCoord(ref nomCoordInt._coordinateZ);

            DrawEyeCoordMeshes();
        }

        private void RandomizeEyeCoord(ref int[] coord)
        {
            coord = new int[RND.Next(4) + 3];

            for (var i = 0; i < coord.Length; i++) coord[i] = -1;

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

        private void DrawEyeCoordMeshes()
        {
            var orbCanHoloProj = FindObjectOfType<OrbitalCannonHologramProjector>();
            GameObject eyeCoords = (
                from hologram in orbCanHoloProj._holograms
                where hologram.name == "Hologram_EyeCoordinates"
                select hologram
            ).FirstOrDefault();

            var eyeCoordsMesh = new Mesh();
            eyeCoords.GetComponentInChildren<MeshFilter>().mesh = eyeCoordsMesh;
            var nomCoordInt = FindObjectOfType<NomaiCoordinateInterface>();
            int[] coordX = nomCoordInt._coordinateX, coordY = nomCoordInt._coordinateY, coordZ = nomCoordInt._coordinateZ;

            List<Vector3> verts = new List<Vector3>(), norms = new List<Vector3>();
            List<int> tris = new List<int>();

            var pos = Vector3.right * (1 - 1f / 4);
            DrawEyeCoordMesh(pos, coordX, ref verts, ref norms, ref tris);

            pos = Vector3.zero;
            DrawEyeCoordMesh(pos, coordY, ref verts, ref norms, ref tris);

            pos = Vector3.right * (1f / 4 - 1);
            DrawEyeCoordMesh(pos, coordZ, ref verts, ref norms, ref tris);

            eyeCoordsMesh.SetVertices(verts);
            eyeCoordsMesh.SetNormals(norms);
            eyeCoordsMesh.SetTriangles(tris, 0);
        }

        private void DrawEyeCoordMesh(Vector3 pos, int[] coord, ref List<Vector3> verts, ref List<Vector3> norms, ref List<int> tris)
        {
            for (var i = 0; i < coord.Length - 1; i++)
            {
                var curPosAngle = (5 - coord[i]) * Math.PI / 3;
                var curPos = new Vector3((float)Math.Cos(curPosAngle), 0, (float)Math.Sin(curPosAngle)) / 4 + pos;

                var nextPosAngle = (5 - coord[i + 1]) * Math.PI / 3;
                var nextPos = new Vector3((float)Math.Cos(nextPosAngle), 0, (float)Math.Sin(nextPosAngle)) / 4 + pos;

                var faceAngle = Math.Atan2(nextPos.z - curPos.z, nextPos.x - curPos.x) + Math.PI;
                var faceAngleCos = (float)Math.Cos(faceAngle);
                var faceAngleSin = (float)Math.Sin(faceAngle);
                var faceAngleVector = new Vector3(faceAngleCos, 0, faceAngleSin);

                var faceAngleFlip = faceAngle - Math.PI;
                var faceAngleFlipCos = (float)Math.Cos(faceAngleFlip);
                var faceAngleFlipSin = (float)Math.Sin(faceAngleFlip);
                var faceAngleFlipVector = new Vector3(faceAngleFlipCos, 0, faceAngleFlipSin);

                var vertAngle = faceAngle - Math.PI / 2;
                var vertAngleCos = (float)Math.Cos(vertAngle);
                var vertAngleSin = (float)Math.Sin(vertAngle);
                var vertAngleVector = new Vector3(vertAngleCos, 0, vertAngleSin);
                var vertAngleVectorInset = new Vector3(vertAngleCos, 0.5f, vertAngleSin);

                var vertAngleFlip = vertAngle + Math.PI;
                var vertAngleFlipCos = (float)Math.Cos(vertAngleFlip);
                var vertAngleFlipSin = (float)Math.Sin(vertAngleFlip);
                var vertAngleFlipVector = new Vector3(vertAngleFlipCos, 0, vertAngleFlipSin);
                var vertAngleFlipVectorInset = new Vector3(vertAngleFlipCos, 0.5f, vertAngleFlipSin);

                Vector3[] newVerts =
                {
                    vertAngleVector / 32 + curPos,
                    vertAngleFlipVector / 32 + curPos,
                    vertAngleFlipVectorInset / 32 + curPos,
                    vertAngleVectorInset / 32 + curPos,
                    vertAngleVector / 32 + nextPos,
                    vertAngleVectorInset / 32 + nextPos,
                    vertAngleFlipVectorInset / 32 + nextPos,
                    vertAngleFlipVector / 32 + nextPos
                };

                var triStart = verts.Count;

                // back cap

                if (i == 0)
                {
                    verts.Add(newVerts[0]);
                    verts.Add(newVerts[3]);
                    verts.Add(newVerts[2]);
                    verts.Add(newVerts[1]);

                    for (int j = 0; j < 4; j++) norms.Add(faceAngleVector);

                    tris.AddRange(MakePlane(triStart));
                    triStart = verts.Count;
                }

                // out

                verts.Add(newVerts[0]);
                verts.Add(newVerts[1]);
                verts.Add(newVerts[7]);
                verts.Add(newVerts[4]);

                for (int j = 0; j < 4; j++) norms.Add(Vector3.down);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // in

                verts.Add(newVerts[3]);
                verts.Add(newVerts[5]);
                verts.Add(newVerts[6]);
                verts.Add(newVerts[2]);

                for (int j = 0; j < 4; j++) norms.Add(Vector3.up);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // left

                verts.Add(newVerts[0]);
                verts.Add(newVerts[4]);
                verts.Add(newVerts[5]);
                verts.Add(newVerts[3]);

                for (int j = 0; j < 4; j++) norms.Add(vertAngleVector);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // right

                verts.Add(newVerts[1]);
                verts.Add(newVerts[2]);
                verts.Add(newVerts[6]);
                verts.Add(newVerts[7]);

                for (int j = 0; j < 4; j++) norms.Add(vertAngleFlipVector);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                if (i == 0) continue;

                // front cap

                if (i == coord.Length - 2)
                {
                    verts.Add(newVerts[4]);
                    verts.Add(newVerts[7]);
                    verts.Add(newVerts[6]);
                    verts.Add(newVerts[5]);

                    for (int j = 0; j < 4; j++) norms.Add(faceAngleFlipVector);

                    tris.AddRange(MakePlane(triStart));
                    triStart = verts.Count;
                }

                var prevPosAngle = (5 - coord[i - 1]) * Math.PI / 3;
                var prevPos = new Vector3((float)Math.Cos(prevPosAngle), 0, (float)Math.Sin(prevPosAngle)) / 4 + pos;

                var prevVertAngle = Math.Atan2(curPos.z - prevPos.z, curPos.x - prevPos.x) + Math.PI / 2;
                var prevVertAngleCos = (float)Math.Cos(prevVertAngle);
                var prevVertAngleSin = (float)Math.Sin(prevVertAngle);
                var prevVertAngleVector = new Vector3(prevVertAngleCos, 0, prevVertAngleSin);
                var prevVertAngleVectorInset = new Vector3(prevVertAngleCos, 0.5f, prevVertAngleSin);

                var prevVertAngleFlip = prevVertAngle + Math.PI;
                var prevVertAngleFlipCos = (float)Math.Cos(prevVertAngleFlip);
                var prevVertAngleFlipSin = (float)Math.Sin(prevVertAngleFlip);
                var prevVertAngleFlipVector = new Vector3(prevVertAngleFlipCos, 0, prevVertAngleFlipSin);
                var prevVertAngleFlipVectorInset = new Vector3(prevVertAngleFlipCos, 0.5f, prevVertAngleFlipSin);

                Vector3[] prevVerts =
                {
                    prevVertAngleVector / 32 + curPos,
                    prevVertAngleFlipVector / 32 + curPos,
                    prevVertAngleFlipVectorInset / 32 + curPos,
                    prevVertAngleVectorInset / 32 + curPos
                };

                // out cap

                verts.Add(prevVerts[0]);
                verts.Add(prevVerts[1]);
                verts.Add(newVerts[1]);
                verts.Add(newVerts[0]);

                for (int j = 0; j < 4; j++) norms.Add(Vector3.down);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // in cap

                verts.Add(prevVerts[2]);
                verts.Add(prevVerts[3]);
                verts.Add(newVerts[3]);
                verts.Add(newVerts[2]);

                for (int j = 0; j < 4; j++) norms.Add(Vector3.up);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // left cap

                verts.Add(prevVerts[3]);
                verts.Add(prevVerts[0]);
                verts.Add(newVerts[0]);
                verts.Add(newVerts[3]);

                var leftAngle = (prevVertAngle + vertAngle) / 2;
                var leftAngleCos = (float)Math.Cos(leftAngle);
                var leftAngleSin = (float)Math.Sin(leftAngle);
                var leftAngleVector = new Vector3(leftAngleCos, 0, leftAngleSin);

                for (int j = 0; j < 4; j++) norms.Add(leftAngleVector);

                tris.AddRange(MakePlane(triStart));
                triStart = verts.Count;

                // right cap

                verts.Add(prevVerts[1]);
                verts.Add(prevVerts[2]);
                verts.Add(newVerts[2]);
                verts.Add(newVerts[1]);

                var rightAngle = leftAngle + Math.PI;
                var rightAngleCos = (float)Math.Cos(rightAngle);
                var rightAngleSin = (float)Math.Sin(rightAngle);
                var rightAngleVector = new Vector3(rightAngleCos, 0, rightAngleSin);

                for (int j = 0; j < 4; j++) norms.Add(rightAngleVector);

                tris.AddRange(MakePlane(triStart));
            }

            /*
            for (var i = 1; i < coord.Length - 1; i++)
            {
                var prevPosAngle = (5 - coord[i - 1]) * Math.PI / 3;
                var prevPos = new Vector3((float)Math.Cos(prevPosAngle), 0, (float)Math.Sin(prevPosAngle)) / 4 + pos;
                var posAngle = (5 - coord[i]) * Math.PI / 3;
                var curPos = new Vector3((float)Math.Cos(posAngle), 0, (float)Math.Sin(posAngle)) / 4 + pos;
                var nextPosAngle = (5 - coord[i + 1]) * Math.PI / 3;
                var nextPos = new Vector3((float)Math.Cos(nextPosAngle), 0, (float)Math.Sin(nextPosAngle)) / 4 + pos;
                double prevAngle = Math.Atan2(curPos.z - prevPos.z, curPos.x - prevPos.x) + Math.PI / 2;
                double nextAngle = Math.Atan2(nextPos.z - curPos.z, nextPos.x - curPos.x) + Math.PI / 2;

                Vector3[] newVerts =
                {
                    new Vector3((float)Math.Cos(prevAngle), 0, (float)Math.Sin(prevAngle)) / 32 + prevPos,
                    new Vector3((float)Math.Cos(prevAngle), 0.5f, (float)Math.Sin(prevAngle)) / 32 + prevPos,
                    new Vector3((float)Math.Cos(prevAngle + Math.PI), 0, (float)Math.Sin(prevAngle + Math.PI)) / 32 + prevPos,
                    new Vector3((float)Math.Cos(prevAngle + Math.PI), 0.5f, (float)Math.Sin(prevAngle + Math.PI)) / 32 + prevPos,
                    new Vector3((float)Math.Cos(prevAngle), 0, (float)Math.Sin(prevAngle)) / 32 + curPos,
                    new Vector3((float)Math.Cos(prevAngle), 0.5f, (float)Math.Sin(prevAngle)) / 32 + curPos,
                    new Vector3((float)Math.Cos(prevAngle + Math.PI), 0, (float)Math.Sin(prevAngle + Math.PI)) / 32 + curPos,
                    new Vector3((float)Math.Cos(prevAngle + Math.PI), 0.5f, (float)Math.Sin(prevAngle + Math.PI)) / 32 + curPos,
                    new Vector3((float)Math.Cos(nextAngle), 0, (float)Math.Sin(nextAngle)) / 32 + curPos,
                    new Vector3((float)Math.Cos(nextAngle), 0.5f, (float)Math.Sin(nextAngle)) / 32 + curPos,
                    new Vector3((float)Math.Cos(nextAngle + Math.PI), 0, (float)Math.Sin(nextAngle + Math.PI)) / 32 + curPos,
                    new Vector3((float)Math.Cos(nextAngle + Math.PI), 0.5f, (float)Math.Sin(nextAngle + Math.PI)) / 32 + curPos,
                    new Vector3((float)Math.Cos(nextAngle), 0, (float)Math.Sin(nextAngle)) / 32 + nextPos,
                    new Vector3((float)Math.Cos(nextAngle), 0.5f, (float)Math.Sin(nextAngle)) / 32 + nextPos,
                    new Vector3((float)Math.Cos(nextAngle + Math.PI), 0, (float)Math.Sin(nextAngle + Math.PI)) / 32 + nextPos,
                    new Vector3((float)Math.Cos(nextAngle + Math.PI), 0.5f, (float)Math.Sin(nextAngle + Math.PI)) / 32 + nextPos
                };

                var triStart = verts.Count;
                verts.AddRange(newVerts);

                int[] newTris =
                {
                    triStart, triStart + 2, triStart + 1, triStart + 1, triStart + 2, triStart + 3,
                    triStart, triStart + 4, triStart + 2, triStart + 2, triStart + 4, triStart + 6,
                    triStart + 1, triStart + 3, triStart + 5, triStart + 3, triStart + 7, triStart + 5,
                    triStart, triStart + 1, triStart + 4, triStart + 1, triStart + 5, triStart + 4,
                    triStart + 2, triStart + 6, triStart + 3, triStart + 3, triStart + 6, triStart + 7,
                    triStart + 4, triStart + 6, triStart + 8, triStart + 4, triStart + 10, triStart + 6,
                    triStart + 5, triStart + 7, triStart + 11, triStart + 5, triStart + 9, triStart + 7,
                    triStart + 4, triStart + 8, triStart + 5, triStart + 5, triStart + 8, triStart + 9,
                    triStart + 6, triStart + 10, triStart + 7, triStart + 7, triStart + 10, triStart + 11,
                    triStart + 8, triStart + 12, triStart + 10, triStart + 10, triStart + 12, triStart + 14,
                    triStart + 9, triStart + 11, triStart + 13, triStart + 11, triStart + 15, triStart + 13,
                    triStart + 8, triStart + 9, triStart + 12, triStart + 9, triStart + 13, triStart + 12,
                    triStart + 10, triStart + 14, triStart + 11, triStart + 11, triStart + 14, triStart + 15,
                    triStart + 12, triStart + 13, triStart + 14, triStart + 13, triStart + 15, triStart +  14
                };

                tris.AddRange(newTris);
            }
            */
        }

        private int[] MakePlane(int triStart)
        {
            return new int[]
            {
                triStart, triStart + 1, triStart + 2,
                triStart, triStart + 2, triStart + 3
            };
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
            DrawEyeCoordMeshes();
        }
    }
}
