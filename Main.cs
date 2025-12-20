using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
using MimicAPI.GameAPI;

[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), "MorePlayers", "2.0.0", "github.com/NeoMimicry")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            var harmony = new HarmonyLib.Harmony("com.neomimicry.moreplayers");
            harmony.PatchAll(typeof(MorePlayersMod).Assembly);
        }
    }

    [HarmonyPatch]
    public class IVroom_CanEnterChannel_Transpiler
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();
            var method = ServerNetworkAPI.GetIVroomCanEnterChannel();
            if (method != null)
            {
                methods.Add(method);
            }
            return methods;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo field && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS));
                    break;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public class GetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
            var method = serverSocketType?.GetMethod("GetMaximumClients", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return method;
        }

        static bool Prefix(ref int __result)
        {
            __result = MorePlayersMod.MAX_PLAYERS;
            return false;
        }
    }

    [HarmonyPatch]
    public class SetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
            var method = serverSocketType?.GetMethod("SetMaximumClients", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return method;
        }

        static bool Prefix(ref int value)
        {
            if (value < MorePlayersMod.MAX_PLAYERS)
                value = MorePlayersMod.MAX_PLAYERS;
            return true;
        }
    }

    [HarmonyPatch]
    public class ServerSocket_Constructor_Patch
    {
        static MethodBase TargetMethod()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
            var ctor = serverSocketType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

            return ctor;
        }

        static void Postfix(object __instance)
        {
            ServerNetworkAPI.SetMaximumClients(__instance, MorePlayersMod.MAX_PLAYERS);
        }
    }

    [HarmonyPatch]
    public class AllRooms_CanEnterChannel_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var methods = new List<MethodBase>();

            var vWaitingRoomType = assembly?.GetType("VWaitingRoom");
            var waitingMethod = vWaitingRoomType?.GetMethod("CanEnterChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (waitingMethod != null)
            {
                methods.Add(waitingMethod);
            }

            var maintenanceRoomType = assembly?.GetType("MaintenanceRoom");
            var maintenanceMethod = maintenanceRoomType?.GetMethod("CanEnterChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (maintenanceMethod != null)
            {
                methods.Add(maintenanceMethod);
            }

            var ivroomType = assembly?.GetType("IVroom");
            var ivroomMethod = ivroomType?.GetMethod("CanEnterChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (ivroomMethod != null)
            {
                methods.Add(ivroomMethod);
            }

            return methods;
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            try
            {
                var msgErrorCodeType = ServerNetworkAPI.GetMsgErrorCodeType();
                if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    return true;

                if (ServerNetworkAPI.IsPlayerInRoom(__instance, playerUID))
                {
                    __result = ServerNetworkAPI.CreateErrorCodeEnum("DuplicatePlayer");
                    return false;
                }

                int playerCount = ServerNetworkAPI.GetRoomPlayerCount(__instance);
                if (playerCount >= MorePlayersMod.MAX_PLAYERS)
                {
                    __result = ServerNetworkAPI.CreateErrorCodeEnum("PlayerCountExceeded");
                    return false;
                }

                __result = ServerNetworkAPI.CreateErrorCodeEnum("Success");
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    public class VRoomManager_EnterWaitingRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var vroomManagerType = assembly?.GetType("VRoomManager");
            var method = vroomManagerType?.GetMethod("EnterWaitingRoom", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return method;
        }

        static void Prefix(object __instance)
        {
            var room = ServerNetworkAPI.GetWaitingRoom();
            ServerNetworkAPI.SetRoomMaxPlayers(room, MorePlayersMod.MAX_PLAYERS);
        }
    }

    [HarmonyPatch]
    public class VRoomManager_EnterMaintenenceRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            var assembly = ServerNetworkAPI.GetGameAssembly();
            var vroomManagerType = assembly?.GetType("VRoomManager");
            var method = vroomManagerType?.GetMethod("EnterMaintenenceRoom", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return method;
        }

        static void Prefix(object __instance)
        {
            var room = ServerNetworkAPI.GetMaintenanceRoom();
            ServerNetworkAPI.SetRoomMaxPlayers(room, MorePlayersMod.MAX_PLAYERS);
        }
    }

    [HarmonyPatch]
    public class GameSessionInfo_AddPlayerSteamID_Patch
    {
        static MethodBase TargetMethod()
        {
            var method = ServerNetworkAPI.GetGameSessionInfoAddPlayerSteamID();
            return method;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int patchCount = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
                    patchCount++;
                }
                else if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo field && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS));
                    patchCount++;
                    i++;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public class SteamLobbyCreation_Patch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            try
            {
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                    return true;

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt", BindingFlags.Public | BindingFlags.Static);

                if (createLobbyMethod == null || setIntMethod == null)
                    return true;

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });

                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
