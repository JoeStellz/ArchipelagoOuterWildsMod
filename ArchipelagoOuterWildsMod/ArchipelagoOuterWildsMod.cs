using OWML.Common;
using OWML.Common.Menus;
using OWML.ModHelper;
using UnityEngine;
using System.Collections.Generic;

namespace ArchipelagoOuterWildsMod
{
    public class ArchipelagoOuterWildsMod : ModBehaviour
    {
        internal static ArchipelagoOuterWildsMod Instance;

        private System.Random rnd;
        private readonly int Seed = 1981184590;
        private bool Testing;

        private void Start()
        {
            ModHelper.Console.WriteLine($"Running {nameof(ArchipelagoOuterWildsMod)}!", MessageType.Success);

            Instance = this;

            // Subscribed events
            // ModHelper.Menus.MainMenu.OnInit += OnMainMenuInit;
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            // Patched methods
            // ModHelper.HarmonyHelper.AddPrefix<PopupMenu>("SetUpPopup", typeof(Patches), nameof(Patches.SetUpPopup));

            Testing = false;
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

            bool rndPlanets = true;

            if (!rndPlanets) return;

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
                Vector3 newPos;
                newPos = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                if (name == "BrittleHollow_Body" || name == "WhiteHole_Body") newPos.y = 0;
                newPos.Normalize();
                float newMag;
                bool done = true;
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

            offsets.Add("Satellite_Body", offsets["TimberHearth_Body"]);
            offsets.Add("Moon_Body", offsets["TimberHearth_Body"]);
            offsets.Add("VolcanicMoon_Body", offsets["BrittleHollow_Body"]);
            offsets.Add("OrbitalProbeCannon_Body", offsets["GiantsDeep_Body"]);

            foreach (var obj in FindObjectsOfType<OWRigidbody>())
            {
                var parentName = GetParent(obj);
                if (!offsets.ContainsKey(parentName)) continue;
                var offset = offsets[parentName];
                if (offset.magnitude > 0)
                {
                    obj.SetPosition(obj.GetPosition() + offset);
                    Physics.SyncTransforms();
                }
            }
        }

        private string GetParent(OWRigidbody body)
        {
            var parent = body.GetOrigParentBody();
            if (parent == null) return body.name;
            return GetParent(parent);
        }

        private void OnCompleteSolarSystemLoadTest()
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
    }
}
