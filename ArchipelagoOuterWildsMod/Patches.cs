using OWML.Common;

namespace ArchipelagoOuterWildsMod
{
    internal static class Patches
    {
        private static readonly ArchipelagoOuterWildsMod Instance = ArchipelagoOuterWildsMod.Instance;

        internal static void AddEntry(ShipLogEntry entry)
        {
            Instance.ModHelper.Console.WriteLine($"Ship log entry {entry._name} added!", MessageType.Success);
            foreach (var fact in entry._completionFacts)
            {
                if (entry._exploreFacts.Contains(fact)) continue;
                Instance.ModHelper.Console.WriteLine($"Completion fact: {fact._text}", MessageType.Info);
            }
            foreach (var fact in entry._exploreFacts)
            {
                if (entry._completionFacts.Contains(fact)) continue;
                Instance.ModHelper.Console.WriteLine($"Explore fact: {fact._text}", MessageType.Info);
            }
            foreach (var fact in entry._rumorFacts)
            {
                Instance.ModHelper.Console.WriteLine($"Rumor fact: {fact._text}", MessageType.Info);
            }
        }

        internal static void RevealFact(string id, bool saveGame = true, bool showNotification = true)
        {
            Instance.ModHelper.Console.WriteLine($"Revealed fact {id}!", MessageType.Success);
            if (!saveGame) Instance.ModHelper.Console.WriteLine("Game not saved", MessageType.Info);
            if (!showNotification) Instance.ModHelper.Console.WriteLine("Notification not shown", MessageType.Info);
        }

        internal static void SetPersistentCondition(string condition, bool state)
        {
            Instance.ModHelper.Console.WriteLine($"{condition} set to {state}", MessageType.Info);
        }
    }
}
