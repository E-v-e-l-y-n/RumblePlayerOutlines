using Il2CppPhoton.Pun;
using Il2CppRUMBLE.Players;
using MelonLoader;
using System;
using HarmonyLib;

namespace RumblePlayerOutlines.Patches
{
    [HarmonyPatch(typeof(PlayerController), "Initialize", new Type[] { typeof(Il2CppRUMBLE.Players.Player) })]
    public static class PlayerSpawnPatch
    {
        private static void Postfix(ref PlayerController __instance, ref Il2CppRUMBLE.Players.Player player)
        {
            MelonCoroutines.Start(Class1.instance.CreatePlayerOutline(__instance));
        }
    }
}
