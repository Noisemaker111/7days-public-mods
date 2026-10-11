using HarmonyLib;

namespace JonLootSkulls
{
    [HarmonyPatch(typeof(XUiC_LootWindow), "OnClose")]
    public static class LootClosed
    {
        public static void Prefix(XUiC_LootWindow __instance, out TEFeatureStorage __state) { __state = __instance.te; }
        public static void Postfix(TEFeatureStorage __state) { Runtime.Instance?.ObserveLoot(__state); }
    }
    // Native storage read is also the client's chunk/network replication boundary.
    [HarmonyPatch(typeof(TEFeatureStorage), "Read")]
    public static class LootReplicated
    {
        public static void Postfix(TEFeatureStorage __instance) { Runtime.Instance?.ObserveLoot(__instance); }
    }
    [HarmonyPatch(typeof(TEFeatureStorage), "UpdateTick")]
    public static class LootClockChanged
    {
        public struct Before { public ulong Time; public bool Touched; }
        public static void Prefix(TEFeatureStorage __instance, out Before __state)
        { var grid = __instance.ItemGrid; __state = new Before { Time = grid == null ? 0 : grid.WorldTimeTouched, Touched = grid != null && grid.Touched }; }
        public static void Postfix(TEFeatureStorage __instance, Before __state)
        {
            // Native nearby-player deferral extends the real respawn clock.
            var grid = __instance.ItemGrid;
            if (grid != null && (__state.Time != grid.WorldTimeTouched || __state.Touched != grid.Touched)) Runtime.Instance?.ObserveLoot(__instance);
        }
    }
    [HarmonyPatch(typeof(TEFeatureStorage), "OnUnlockedServer")]
    public static class SharedLootClosed
    {
        public static void Prefix(TEFeatureStorage __instance) { Runtime.Instance?.ObserveLoot(__instance); }
    }
    [HarmonyPatch(typeof(PrefabInstance), "ResetBlocksAndRebuild")]
    public static class QuestPoiReset
    {
        public static void Prefix(PrefabInstance __instance) { Runtime.Instance?.ClearPoi(__instance.id); }
    }
}
