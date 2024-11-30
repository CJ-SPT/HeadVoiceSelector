#if !UNITY_EDITOR
using System.Reflection;
using EFT.UI;
using HeadVoiceSelector.Core.UI;
using SPT.Reflection.Patching;

namespace HeadVoiceSelector.Patches
{
    internal class OverallScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(OverallScreen).GetMethod(nameof(OverallScreen.Show));

        [PatchPostfix]
        public static void PatchPostfix(OverallScreen __instance)
        {
            _ = NewVoiceHeadDrawers.AddCustomizationDrawers(__instance);
        }
    }
}

#endif