using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
using MimicAPI.GameAPI;

[assembly: MelonInfo(
    typeof(MorePlayers.MorePlayersMod),
    "MorePlayers",
    "1.3.0",
    "github.com/Rxflex"
)]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MorePlayers
{
    public class MorePlayersMod : MelonMod
    {
        public const int MAX_PLAYERS = 999;

        public override void OnInitializeMelon()
        {
            var harmonyInstance = new HarmonyLib.Harmony("com.moreplayers.mod");

            PatchServerSocket(harmonyInstance);
            PatchIVroom(harmonyInstance);
            PatchVRoomManager(harmonyInstance);
            PatchGameSessionInfo(harmonyInstance);
            PatchSteamInviteDispatcher(harmonyInstance);
        }

        private void PatchServerSocket(HarmonyLib.Harmony harmony)
        {
            try
            {
                var getMaxMethod = ServerNetworkAPI.GetServerSocketMethod("GetMaximumClients");
                var setMaxMethod = ServerNetworkAPI.GetServerSocketMethod("SetMaximumClients");

                if (getMaxMethod != null)
                {
                    var prefix = typeof(MorePlayersMod).GetMethod(
                        nameof(GetMaximumClients_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(getMaxMethod, prefix: new HarmonyMethod(prefix));
                }

                if (setMaxMethod != null)
                {
                    var prefix = typeof(MorePlayersMod).GetMethod(
                        nameof(SetMaximumClients_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(setMaxMethod, prefix: new HarmonyMethod(prefix));
                }

                var serverSocket = ServerNetworkAPI.GetServerSocket();
                if (serverSocket != null)
                {
                    var serverSocketType = serverSocket.GetType();
                    var ctors = serverSocketType.GetConstructors(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    if (ctors.Length > 0)
                    {
                        var postfix = typeof(MorePlayersMod).GetMethod(
                            nameof(ServerSocket_Constructor_Postfix),
                            BindingFlags.Static | BindingFlags.NonPublic
                        );
                        harmony.Patch(ctors[0], postfix: new HarmonyMethod(postfix));
                    }
                }
            }
            catch { }
        }

        private void PatchIVroom(HarmonyLib.Harmony harmony)
        {
            try
            {
                var canEnterMethod = ServerNetworkAPI.GetIVroomMethod("CanEnterChannel");
                var getMemberCountMethod = ServerNetworkAPI.GetIVroomMethod("GetMemberCount");

                if (canEnterMethod != null)
                {
                    var transpiler = typeof(MorePlayersMod).GetMethod(
                        nameof(IVroom_CanEnterChannel_Transpiler),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(canEnterMethod, transpiler: new HarmonyMethod(transpiler));
                }

                if (getMemberCountMethod != null)
                {
                    var prefix = typeof(MorePlayersMod).GetMethod(
                        nameof(IVroom_GetMemberCount_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(getMemberCountMethod, prefix: new HarmonyMethod(prefix));
                }
            }
            catch { }
        }

        private void PatchVRoomManager(HarmonyLib.Harmony harmony)
        {
            try
            {
                var transpiler = typeof(MorePlayersMod).GetMethod(
                    nameof(VRoomManager_Transpiler),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                var methods = new[] { "EnterMaintenenceRoom", "EnterWaitingRoom" };
                foreach (var methodName in methods)
                {
                    var method = ServerNetworkAPI.GetVRoomManagerMethod(methodName);
                    if (method != null)
                    {
                        harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
                    }
                }

                var pendStartMethods = new[]
                {
                    "PendStartGame",
                    "PendStartSession",
                    "OnFinishGame",
                };
                var pendTranspiler = typeof(MorePlayersMod).GetMethod(
                    nameof(VRoomManager_PendStart_Transpiler),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                foreach (var methodName in pendStartMethods)
                {
                    var method = ServerNetworkAPI.GetVRoomManagerMethod(methodName);
                    if (method != null)
                    {
                        harmony.Patch(method, transpiler: new HarmonyMethod(pendTranspiler));
                    }
                }
            }
            catch { }
        }

        private void PatchGameSessionInfo(HarmonyLib.Harmony harmony)
        {
            try
            {
                var method = ServerNetworkAPI.GetServerMethod(
                    "GameSessionInfo",
                    "AddPlayerSteamID"
                );

                if (method != null)
                {
                    var transpiler = typeof(MorePlayersMod).GetMethod(
                        nameof(IVroom_CanEnterChannel_Transpiler),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
                }
            }
            catch { }
        }

        private void PatchSteamInviteDispatcher(HarmonyLib.Harmony harmony)
        {
            try
            {
                var method = ServerNetworkAPI.GetSteamInviteMethod("CreateLobby");

                if (method != null)
                {
                    var prefix = typeof(MorePlayersMod).GetMethod(
                        nameof(CreateLobby_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                }
            }
            catch { }
        }

        private static bool GetMaximumClients_Prefix(ref int __result)
        {
            __result = MAX_PLAYERS;
            return false;
        }

        private static bool SetMaximumClients_Prefix(ref int value)
        {
            if (value < MAX_PLAYERS)
                value = MAX_PLAYERS;
            return true;
        }

        private static void ServerSocket_Constructor_Postfix(object __instance)
        {
            try
            {
                var type = __instance.GetType();
                var setMethod = type.GetMethod(
                    "SetMaximumClients",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                setMethod?.Invoke(__instance, new object[] { MAX_PLAYERS });
            }
            catch { }
        }

        private static IEnumerable<CodeInstruction> IVroom_CanEnterChannel_Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MAX_PLAYERS);
            }

            return codes;
        }

        private static bool IVroom_GetMemberCount_Prefix(ref int __result)
        {
            __result = 0;
            return false;
        }

        private static IEnumerable<CodeInstruction> VRoomManager_Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MAX_PLAYERS);
            }

            return codes;
        }

        private static IEnumerable<CodeInstruction> VRoomManager_PendStart_Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_3)
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, MAX_PLAYERS);
            }

            return codes;
        }

        private static bool CreateLobby_Prefix(bool isOpenForRandomMatch)
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
                    return true;

                var createLobbyMethod = steamMatchmakingType.GetMethod(
                    "CreateLobby",
                    BindingFlags.Public | BindingFlags.Static
                );
                var setIntMethod = playerPrefsType.GetMethod(
                    "SetInt",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (createLobbyMethod == null || setIntMethod == null)
                    return true;

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MAX_PLAYERS });
                setIntMethod.Invoke(
                    null,
                    new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 }
                );

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"CreateLobby patch error: {ex.Message}");
                return true;
            }
        }
    }
}
