using GameData;
using Gear;
using HarmonyLib;

namespace BurstActionBuffer
{
    [HarmonyPatch]
    internal static class BurstPatches
    {
        private static ArchetypeDataBlock? _cachedArch;

        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Update))]
        [HarmonyPrefix]
        private static void Pre_Update(PLOC_Stand __instance, out bool __state)
        {
            __state = false;
            if (_cachedArch == null) return;

            if (__instance.m_owner.Locomotion.HasNonForwardInput())
            {
                __state = true;
                _cachedArch.FireMode = eWeaponFireMode.Semi;
            }
        }

        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Update))]
        [HarmonyPostfix]
        private static void Post_Update(bool __state)
        {
            if (__state)
                _cachedArch!.FireMode = eWeaponFireMode.Burst;
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStartFiring))]
        [HarmonyPostfix]
        private static void Post_StartBurstFire(BWA_Burst __instance)
        {
            _cachedArch = __instance.m_archetypeData;
            BufferHandler.Instance.OnBurstStart(__instance);
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStopFiring))]
        [HarmonyPostfix]
        private static void Post_EndBurstFire()
        {
            _cachedArch = null;
            BufferHandler.Instance.OnBurstEnd();
        }
    }
}
