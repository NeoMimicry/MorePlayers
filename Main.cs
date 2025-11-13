using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
using MimicAPI.GameAPI;

[assembly: MelonInfo(
    typeof(MorePlayers.MorePlayersMod),
    "MorePlayers",
    "1.5.0",
    "github.com/rxflex"
)]
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

    [HarmonyPatch]
    public class GetMaximumClients_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var method = serverSocketType?.GetMethod(
                    "GetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return method;
            }
            catch
            {
                return null;
            }
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
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var method = serverSocketType?.GetMethod(
                    "SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return method;
            }
            catch
            {
                return null;
            }
        }

        static bool Prefix(ref int value)
        {
            if (value < MorePlayersMod.MAX_PLAYERS)
            {
                value = MorePlayersMod.MAX_PLAYERS;
            }
            return true;
        }
    }

    [HarmonyPatch]
    public class ServerSocket_Constructor_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var ctor = serverSocketType
                    ?.GetConstructors(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                    .FirstOrDefault();

                return ctor;
            }
            catch
            {
                return null;
            }
        }

        static void Postfix(object __instance)
        {
            try
            {
                var type = __instance.GetType();
                var setMethod = type.GetMethod(
                    "SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                setMethod?.Invoke(__instance, new object[] { MorePlayersMod.MAX_PLAYERS });
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public class ServerSocket_AllMethods_Transpiler
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");

                if (serverSocketType == null)
                    return new MethodBase[0];

                var methods = serverSocketType
                    .GetMethods(
                        BindingFlags.Instance
                            | BindingFlags.NonPublic
                            | BindingFlags.Public
                            | BindingFlags.Static
                            | BindingFlags.DeclaredOnly
                    )
                    .Where(m => !m.IsAbstract && !m.IsConstructor && !m.IsGenericMethod)
                    .ToList();

                return methods;
            }
            catch
            {
                return new MethodBase[0];
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var serverSocketType = assembly?.GetType("FishySteamworks.Server.ServerSocket");
                var maxClientsField = serverSocketType?.GetField(
                    "_maximumClients",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (maxClientsField == null)
                    return codes;

                for (int i = 0; i < codes.Count; i++)
                {
                    if (
                        codes[i].opcode == OpCodes.Ldfld
                        && codes[i].operand is System.Reflection.FieldInfo field
                        && field.Name == "_maximumClients"
                    )
                    {
                        codes.InsertRange(
                            i + 1,
                            new[]
                            {
                                new CodeInstruction(OpCodes.Pop),
                                new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS),
                            }
                        );
                        i += 2;
                    }
                }
            }
            catch { }

            return codes;
        }
    }

    [HarmonyPatch]
    public class GetMemberCount_Smart_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var methods = new List<MethodBase>();

                var ivroomType = assembly?.GetType("IVroom");
                var method = ivroomType?.GetMethod(
                    "GetMemberCount",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (method != null)
                {
                    methods.Add(method);
                }

                return methods;
            }
            catch
            {
                return new MethodBase[0];
            }
        }

        static bool Prefix(ref int __result, object __instance)
        {
            try
            {
                var vPlayerDict = ReflectionHelper.GetFieldValue<Dictionary<int, object>>(
                    __instance,
                    "_vPlayerDict"
                );
                int actualCount = vPlayerDict?.Count ?? 0;

                var stackTrace = new System.Diagnostics.StackTrace();
                bool isFromEnterCheck = false;
                bool isFromSessionCount = false;

                for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method != null)
                    {
                        string methodName = method.Name;
                        if (
                            methodName.Contains("EnterWaitingRoom")
                            || methodName.Contains("EnterMaintenenceRoom")
                            || methodName.Contains("CanEnter")
                        )
                        {
                            isFromEnterCheck = true;
                            break;
                        }
                        if (
                            methodName.Contains("GetSessionCount")
                            || methodName.Contains("GetRoomMemberCount")
                        )
                        {
                            isFromSessionCount = true;
                            break;
                        }
                    }
                }

                if (isFromEnterCheck)
                {
                    __result = 0;
                    return false;
                }
                else if (isFromSessionCount)
                {
                    __result = actualCount;
                    return false;
                }

                __result = actualCount;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    public class AllRooms_CanEnterChannel_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

                var methods = new List<MethodBase>();

                var vWaitingRoomType = assembly?.GetType("VWaitingRoom");
                var waitingMethod = vWaitingRoomType?.GetMethod(
                    "CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (waitingMethod != null)
                {
                    methods.Add(waitingMethod);
                }

                var maintenanceRoomType = assembly?.GetType("MaintenanceRoom");
                var maintenanceMethod = maintenanceRoomType?.GetMethod(
                    "CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (maintenanceMethod != null)
                {
                    methods.Add(maintenanceMethod);
                }

                var ivroomType = assembly?.GetType("IVroom");
                var ivroomMethod = ivroomType?.GetMethod(
                    "CanEnterChannel",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (ivroomMethod != null)
                {
                    methods.Add(ivroomMethod);
                }

                return methods;
            }
            catch
            {
                return new MethodBase[0];
            }
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a =>
                        a.GetName().Name.Contains("FishySteamworks")
                        || a.GetName().Name == "Assembly-CSharp"
                    );
                var msgErrorCodeType = assembly
                    ?.GetTypes()
                    .FirstOrDefault(t => t.Name == "MsgErrorCode");

                if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    return true;

                var vPlayerDict = ReflectionHelper.GetFieldValue<Dictionary<int, object>>(
                    __instance,
                    "_vPlayerDict"
                );

                if (vPlayerDict != null)
                {
                    foreach (var player in vPlayerDict.Values)
                    {
                        var uidProp = player
                            .GetType()
                            .GetProperty("UID", BindingFlags.Public | BindingFlags.Instance);
                        if (uidProp != null)
                        {
                            var uid = uidProp.GetValue(player);
                            if (uid != null && uid.Equals(playerUID))
                            {
                                __result = Enum.Parse(msgErrorCodeType, "DuplicatePlayer");
                                return false;
                            }
                        }
                    }

                    if (vPlayerDict.Count >= MorePlayersMod.MAX_PLAYERS)
                    {
                        __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                        return false;
                    }
                }

                __result = Enum.Parse(msgErrorCodeType, "Success");
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
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod(
                    "EnterWaitingRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return method;
            }
            catch
            {
                return null;
            }
        }

        static void Prefix(object __instance, object context)
        {
            try
            {
                var vrooms = ReflectionHelper.GetFieldValue<Dictionary<long, object>>(
                    __instance,
                    "_vrooms"
                );

                if (vrooms != null)
                {
                    foreach (var room in vrooms.Values)
                    {
                        if (room.GetType().Name == "VWaitingRoom")
                        {
                            ReflectionHelper.SetFieldValue(
                                room,
                                "_maxPlayers",
                                MorePlayersMod.MAX_PLAYERS
                            );
                            break;
                        }
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public class VRoomManager_EnterMaintenenceRoom_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var vroomManagerType = assembly?.GetType("VRoomManager");
                var method = vroomManagerType?.GetMethod(
                    "EnterMaintenenceRoom",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return method;
            }
            catch
            {
                return null;
            }
        }

        static void Prefix(object __instance, object context)
        {
            try
            {
                var vrooms = ReflectionHelper.GetFieldValue<Dictionary<long, object>>(
                    __instance,
                    "_vrooms"
                );

                if (vrooms != null)
                {
                    foreach (var room in vrooms.Values)
                    {
                        if (room.GetType().Name == "MaintenanceRoom")
                        {
                            ReflectionHelper.SetFieldValue(
                                room,
                                "_maxPlayers",
                                MorePlayersMod.MAX_PLAYERS
                            );
                            break;
                        }
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public class GameSessionInfo_AddPlayerSteamID_Patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                var assembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                var type = assembly?.GetType("GameSessionInfo");
                var method = type?.GetMethod(
                    "AddPlayerSteamID",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                return method;
            }
            catch
            {
                return null;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MorePlayersMod.MAX_PLAYERS);
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
                var steamMatchmakingType = Type.GetType(
                    "Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net"
                );
                var eLobbyTypeType = Type.GetType(
                    "Steamworks.ELobbyType, com.rlabrecque.steamworks.net"
                );
                var playerPrefsType = Type.GetType(
                    "UnityEngine.PlayerPrefs, UnityEngine.CoreModule"
                );

                if (
                    steamMatchmakingType == null
                    || eLobbyTypeType == null
                    || playerPrefsType == null
                )
                {
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod(
                    "CreateLobby",
                    BindingFlags.Public | BindingFlags.Static
                );
                var setIntMethod = playerPrefsType.GetMethod(
                    "SetInt",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (createLobbyMethod == null || setIntMethod == null)
                {
                    return true;
                }

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(
                    null,
                    new object[] { friendsOnly, MorePlayersMod.MAX_PLAYERS }
                );
                setIntMethod.Invoke(
                    null,
                    new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 }
                );

                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
