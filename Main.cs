using MelonLoader;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using MimicAPI.GameAPI;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "1.2.1", "github.com/Rxflex")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);
        }
    }

    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "GetMaximumClients")]
    public class GetMaximumClients_Patch
    {
        static bool Prefix(ref int __result)
        {
            __result = MorePlayersMod.MAX_PLAYERS;
            return false;
        }
    }

    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")]
    public class SetMaximumClients_Patch
    {
        static bool Prefix(FishySteamworks.Server.ServerSocket __instance, ref int value)
        {
            if (value < MorePlayersMod.MAX_PLAYERS)
            {
                ServerNetworkAPI.SetMaximumClients(__instance, MorePlayersMod.MAX_PLAYERS);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), MethodType.Constructor)]
    public class ServerSocket_Constructor_Patch
    {
        static void Postfix(FishySteamworks.Server.ServerSocket __instance)
        {
            ServerNetworkAPI.SetMaximumClients(__instance, MorePlayersMod.MAX_PLAYERS);
        }
    }

    [HarmonyPatch]
    public class MaximumClients_Transpiler_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return ServerNetworkAPI.GetAllServerSocketMethods();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var fieldInfo = ServerNetworkAPI.GetMaximumClientsField();

            if (fieldInfo == null)
                return codes;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(fieldInfo))
                {
                    codes.InsertRange(i + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Pop),
                        new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS)
                    });
                    i += 2;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public class EnterWaitingRoom_SetMaxPlayers_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetVRoomManagerMethod("EnterWaitingRoom");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static void Prefix(object __instance)
        {
            try
            {
                var waitingRoom = ServerNetworkAPI.GetWaitingRoom();
                if (waitingRoom != null)
                    RoomAPI.SetRoomMaxPlayers(waitingRoom, MorePlayersMod.MAX_PLAYERS);
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public class CanEnterChannel_AllRooms_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> methods = new List<MethodBase>();

            var waitingRoomMethod = ServerNetworkAPI.GetRoomMethod("VWaitingRoom", "CanEnterChannel");
            if (waitingRoomMethod != null)
                methods.Add(waitingRoomMethod);

            var maintenanceRoomMethod = ServerNetworkAPI.GetRoomMethod("MaintenanceRoom", "CanEnterChannel");
            if (maintenanceRoomMethod != null)
                methods.Add(maintenanceRoomMethod);

            return methods;
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            try
            {
                int count = ServerNetworkAPI.GetRoomMemberCount(__instance);
                var msgErrorCode = ServerNetworkAPI.GetMsgErrorCodeType();

                if (msgErrorCode != null && msgErrorCode.IsEnum)
                {
                    if (count >= MorePlayersMod.MAX_PLAYERS)
                        __result = System.Enum.Parse(msgErrorCode, "PlayerCountExceeded");
                    else
                        __result = System.Enum.Parse(msgErrorCode, "Success");
                }

                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    public class EnterMaintenenceRoom_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetVRoomManagerMethod("EnterMaintenenceRoom");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static void Prefix(object __instance)
        {
            try
            {
                var maintenanceRoom = ServerNetworkAPI.GetMaintenanceRoom();
                if (maintenanceRoom != null)
                    RoomAPI.SetRoomMaxPlayers(maintenanceRoom, MorePlayersMod.MAX_PLAYERS);
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public class SteamLobbyCreation_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetSteamInviteMethod("CreateLobby");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4 ||
                    (codes[i].opcode == OpCodes.Ldc_I4_S && codes[i].operand is sbyte && (sbyte)codes[i].operand == 4) ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int && (int)codes[i].operand == 4))
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public class GetMemberCount_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetRoomMethod("VWaitingRoom", "GetMemberCount");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static bool Prefix(ref int __result, object __instance)
        {
            __result = 0;
            return false;
        }
    }

    [HarmonyPatch]
    public class InGameMenu_SetPingImage_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetUIMethod("UIPrefab_InGameMenu", "SetPingImage");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null && __exception is System.ArgumentOutOfRangeException)
                return null;
            return __exception;
        }
    }

    [HarmonyPatch]
    public class InGameMenu_InitPlayerUI_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetUIMethod("UIPrefab_InGameMenu", "InitializePlayerUI");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null && __exception is System.ArgumentOutOfRangeException)
                return null;
            return __exception;
        }
    }

    [HarmonyPatch]
    public class SurvivalResult_PatchParameter_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = ServerNetworkAPI.GetUIMethod("UIPrefab_SurvivalResult", "PatchParameter");
            return method != null ? new[] { method } : new MethodBase[0];
        }

        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null && __exception is System.ArgumentOutOfRangeException)
                return null;
            return __exception;
        }
    }
}