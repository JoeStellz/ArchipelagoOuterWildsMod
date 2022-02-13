using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using System.Collections.Generic;

namespace ArchipelagoOuterWildsMod
{
    public class ArchipelagoOuterWildsMod : ModBehaviour
    {
        internal static ArchipelagoOuterWildsMod Instance;

        private System.Random rnd;

        private void Start()
        {
            Instance = this;

            // ModHelper.HarmonyHelper.AddPostfix<ShipLogManager>("AddEntry", typeof(Patches), "AddEntry");
            // ModHelper.HarmonyHelper.AddPostfix<ShipLogManager>("RevealFact", typeof(Patches), "RevealFact");
            // ModHelper.HarmonyHelper.AddPrefix<GameSave>("SetPersistentCondition", typeof(Patches), "SetPersistentCondition");

            ModHelper.Console.WriteLine($"Running {nameof(ArchipelagoOuterWildsMod)}!", MessageType.Success);

            var seed = new System.Random().Next();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                ModHelper.Console.WriteLine($"Loaded scene {loadScene}!", MessageType.Success);
                switch (loadScene)
                {
                    case OWScene.SolarSystem:
                        // seed = new System.Random().Next();
                        ModHelper.Console.WriteLine($"Using seed {seed}", MessageType.Info);
                        StartSolarSystem(seed);
                        // TestSolarSystem();
                        break;
                    default:
                        return;
                }
            };
        }

        private void StartSolarSystem(int seed)
        {
            rnd = new System.Random(seed);

            bool rndPlanets = true;

            var planetNames = new List<string>()
            {
                "BrittleHollow_Body",
                "DarkBramble_Body",
                "FocalBody",
                "TimberHearth_Body",
                "GiantsDeep_Body"
            };

            var otherNames = new List<string>()
            {
                "SunStation_Body",
                "WhiteHole_Body"
            };

            var origPositions = new Dictionary<string, Vector3>();
            var newPosMags = new List<float>();
            var offsets = new Dictionary<string, Vector3>();

            foreach (var obj in FindObjectsOfType<AstroObject>())
            {
                var name = obj.name;
                if (!planetNames.Contains(name) && !otherNames.Contains(name)) continue;
                var pos = obj.transform.position;
                origPositions.Add(name, pos);
                Vector3 newPos;
                if (rndPlanets)
                {
                    newPos = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                    if (name == "BrittleHollow_Body" || name == "WhiteHole_Body") newPos.y = 0;
                    newPos.Normalize();
                    // ModHelper.Console.WriteLine($"Normalized position vector generated for {name}!", MessageType.Success);
                    bool done;
                    // ModHelper.Console.WriteLine($"Now generating position magnitude for {name}", MessageType.Info);
                    do
                    {
                        var newMag = (float)rnd.NextDouble() * 21000f + 5000f;
                        done = true;
                        foreach (var val in newPosMags)
                        {
                            if (Mathf.Abs(newMag - val) < 2000f)
                            {
                                done = false;
                                // ModHelper.Console.WriteLine($"{name} position magnitude invalid, trying again", MessageType.Warning);
                                break;
                            }
                        }
                        if (!done) continue;
                        newPosMags.Add(newMag);
                        newPos *= newMag;
                    } while (!done);
                    // ModHelper.Console.WriteLine($"{name} position randomized!", MessageType.Success);
                }
                else newPos = pos;
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
                    // ModHelper.Console.WriteLine($"Adjusted {obj.name} position!", MessageType.Success);
                }
            }
        }

        private string GetParent(OWRigidbody body)
        {
            var parent = body.GetOrigParentBody();
            if (parent == null) return body.name;
            return GetParent(parent);
        }

        private void TestSolarSystem()
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
