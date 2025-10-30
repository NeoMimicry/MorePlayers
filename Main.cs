using MelonLoader;
using HarmonyLib;
using System.Reflection;

namespace MorePlayers
{
    public static class BuildInfo
    {
        public const string Name = "MorePlayers";
        public const string Description = "Remove player limit in Mimesys";
        public const string Author = "YourName";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class MorePlayersMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("MorePlayers Mod Loaded!");
            MelonLogger.Msg("Applying Harmony patches...");
            
            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            MelonLogger.Msg("Harmony patches applied successfully!");
        }
    }

    // НАЙДЕНО: private int _maximumClients в классе FishySteamworks.Server.ServerSocket
    // НАЙДЕНО: internal void SetMaximumClients(int value) - setter с ограничением Math.Min(value, 32766)
    // НАЙДЕНО: internal int GetMaximumClients() - getter
    
    // Патч 1: Для GetMaximumClients() - всегда возвращаем большой лимит
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "GetMaximumClients")]
    public class GetMaximumClients_Patch
    {
        static bool Prefix(ref int __result)
        {
            __result = 999;
            MelonLogger.Msg("[MorePlayers] GetMaximumClients() вызван, возвращаем 999");
            return false; // Не выполняем оригинальный метод
        }
    }

    // Патч 2: Для SetMaximumClients(int value) - игнорируем попытки установить лимит < 999
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")]
    public class SetMaximumClients_Patch
    {
        static bool Prefix(FishySteamworks.Server.ServerSocket __instance, ref int value)
        {
            if (value < 999)
            {
                MelonLogger.Msg("[MorePlayers] SetMaximumClients(" + value + ") вызван, устанавливаем 999 вместо " + value);
                // Напрямую устанавливаем поле через Traverse
                Traverse.Create(__instance).Field("_maximumClients").SetValue(999);
                return false; // Не выполняем оригинальный метод
            }
            return true; // Если значение >= 999, пропускаем оригинальный метод
        }
    }

    // Патч 3: Для приватного поля _maximumClients через Traverse (если свойства не работают)
    /*
    [HarmonyPatch]
    public class MaximumClients_Field_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ИмяКласса), MethodType.Constructor)]
        static void Constructor_Postfix(object __instance)
        {
            // Изменяем приватное поле после создания объекта
            Traverse.Create(__instance).Field("_maximumClients").SetValue(999);
            MelonLogger.Msg("[MorePlayers] Приватное поле _maximumClients изменено на 999");
        }
    }
    */
}