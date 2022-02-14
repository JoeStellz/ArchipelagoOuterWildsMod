using OWML.Common;

namespace ArchipelagoOuterWildsMod
{
    internal static class Patches
    {
        private static readonly ArchipelagoOuterWildsMod Instance = ArchipelagoOuterWildsMod.Instance;

        internal static void SetUpPopup(string message, IInputCommands okCommand, IInputCommands cancelCommand, ScreenPrompt okPrompt, ScreenPrompt cancelPrompt, bool closeMenuOnOk = true, bool setCancelButtonActive = true)
        {
            Instance.ModHelper.Console.WriteLine($"Setting up popup!", MessageType.Success);
            Instance.ModHelper.Console.WriteLine($"Message: {message}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"OK command: {okCommand}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"Cancel command: {cancelCommand}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"OK prompt: {okPrompt}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"Cancel prompt: {cancelPrompt}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"Close menu on ok: {closeMenuOnOk}", MessageType.Info);
            Instance.ModHelper.Console.WriteLine($"Set cancel button active: {setCancelButtonActive}", MessageType.Info);
        }
    }
}
