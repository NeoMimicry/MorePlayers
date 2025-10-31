using MelonLoader;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MorePlayers
{
    public static class BuildInfo
    {
        public const string Name = "MorePlayers";
        public const string Description = "Remove player limit in Mimesis";
        public const string Author = "github.com/Rxflex";
        public const string Company = null;
        public const string Version = "1.0.4";
        public const string DownloadLink = null;
    }

    public class MorePlayersMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("MorePlayers Mod v1.0.4 - Initializing...");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("Author: github.com/Rxflex");
            MelonLogger.Msg("Target: FishySteamworks.Server.ServerSocket");
            MelonLogger.Msg("Goal: Remove 4-player limit, set to 999");
            MelonLogger.Msg("");
            
            try
            {
                var harmony = new HarmonyLib.Harmony("com.moreplayers.mod");
                
                MelonLogger.Msg("Applying Harmony patches...");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                
                MelonLogger.Msg("=================================================");
                MelonLogger.Msg("SUCCESS: All Harmony patches applied!");
                MelonLogger.Msg("=================================================");
                MelonLogger.Msg("Active patches:");
                MelonLogger.Msg("  [1] GetMaximumClients() - Prefix");
                MelonLogger.Msg("  [2] SetMaximumClients() - Prefix");
                MelonLogger.Msg("  [3] Constructor - Postfix");
                MelonLogger.Msg("  [4] Transpiler - IL Code Modification");
                MelonLogger.Msg("  [5] EnterWaitingRoom - Transpiler (VRoomManager)");
                MelonLogger.Msg("  [6] GetMemberCount() - Return 0");
                MelonLogger.Msg("  [7] CanEnterChannel() - All rooms (NEW!)");
                MelonLogger.Msg("  [8] EnterMaintenenceRoom - Set _maxPlayers");
                MelonLogger.Msg("  [9] Steam Lobby Creation - 999 slots");
                MelonLogger.Msg("=================================================");
                MelonLogger.Msg("Waiting for ServerSocket creation...");
                MelonLogger.Msg("");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("=================================================");
                MelonLogger.Error("FAILED to apply Harmony patches!");
                MelonLogger.Error("Exception: " + ex.Message);
                MelonLogger.Error("Stack: " + ex.StackTrace);
                MelonLogger.Error("=================================================");
                MelonLogger.Error("Please report this error with full log!");
            }
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
            MelonLogger.Msg("[PATCH 1] GetMaximumClients() called -> returning 999");
            return false;
        }
    }

    // Патч 2: Для SetMaximumClients(int value) - игнорируем попытки установить лимит < 999
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")]
    public class SetMaximumClients_Patch
    {
        static bool Prefix(FishySteamworks.Server.ServerSocket __instance, ref int value)
        {
            int originalValue = value;
            if (value < 999)
            {
                MelonLogger.Msg("[PATCH 2] SetMaximumClients(" + originalValue + ") called");
                MelonLogger.Msg("[PATCH 2] Overriding: " + originalValue + " -> 999");
                Traverse.Create(__instance).Field("_maximumClients").SetValue(999);
                
                // Verify field was set
                var currentValue = Traverse.Create(__instance).Field("_maximumClients").GetValue<int>();
                MelonLogger.Msg("[PATCH 2] Verification: _maximumClients = " + currentValue);
                return false;
            }
            else
            {
                MelonLogger.Msg("[PATCH 2] SetMaximumClients(" + value + ") called - value already >= 999, allowing");
                return true;
            }
        }
    }

    // Патч 3: Устанавливаем _maximumClients в конструкторе ServerSocket
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), MethodType.Constructor)]
    public class ServerSocket_Constructor_Patch
    {
        static void Postfix(FishySteamworks.Server.ServerSocket __instance)
        {
            MelonLogger.Msg("");
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("[PATCH 3] ServerSocket CONSTRUCTOR called!");
            MelonLogger.Msg("=================================================");
            
            var traverse = Traverse.Create(__instance);
            var originalValue = traverse.Field("_maximumClients").GetValue<int>();
            MelonLogger.Msg("[PATCH 3] Original _maximumClients value: " + originalValue);
            
            traverse.Field("_maximumClients").SetValue(999);
            
            var newValue = traverse.Field("_maximumClients").GetValue<int>();
            MelonLogger.Msg("[PATCH 3] New _maximumClients value: " + newValue);
            
            if (newValue == 999)
            {
                MelonLogger.Msg("[PATCH 3] SUCCESS: _maximumClients set to 999");
            }
            else
            {
                MelonLogger.Error("[PATCH 3] FAILED: _maximumClients is " + newValue + " instead of 999!");
            }
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("");
        }
    }

    // Патч 4: Перехватываем все обращения к полю _maximumClients через Transpiler
    // Этот патч изменяет IL-код для замены значения поля на лету
    [HarmonyPatch]
    public class MaximumClients_Transpiler_Patch
    {
        // Находим все методы в ServerSocket, которые читают _maximumClients
        static IEnumerable<MethodBase> TargetMethods()
        {
            var type = typeof(FishySteamworks.Server.ServerSocket);
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => !m.IsAbstract && m.DeclaringType == type);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var fieldInfo = AccessTools.Field(typeof(FishySteamworks.Server.ServerSocket), "_maximumClients");
            
            if (fieldInfo == null)
            {
                MelonLogger.Warning("[PATCH 4] TRANSPILER: Cannot find field _maximumClients");
                return codes;
            }

            int patchCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(fieldInfo))
                {
                    codes.InsertRange(i + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Pop),
                        new CodeInstruction(OpCodes.Ldc_I4, 999)
                    });
                    patchCount++;
                    i += 2;
                }
            }

            if (patchCount > 0)
            {
                MelonLogger.Msg("[PATCH 4] TRANSPILER: Patched method '" + original.Name + "' - replaced " + patchCount + " field reads with constant 999");
            }

            return codes;
        }
    }

    // Патч 5: VRoomManager.EnterWaitingRoom - устанавливаем _maxPlayers (NEW - из рабочего мода!)
    [HarmonyPatch]
    public class EnterWaitingRoom_SetMaxPlayers_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var vroomManagerType = assembly.GetTypes().FirstOrDefault(t => t.Name == "VRoomManager");
            
            if (vroomManagerType != null)
            {
                var method = vroomManagerType.GetMethod("EnterWaitingRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    MelonLogger.Msg("[PATCH 5] Target found: VRoomManager.EnterWaitingRoom() - will set _maxPlayers");
                    return new[] { method };
                }
            }
            
            return new MethodBase[0];
        }

        static void Prefix(object __instance)
        {
            const int NEW_LIMIT = 999;
            try
            {
                var vroomsField = __instance.GetType().GetField("_vrooms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (vroomsField == null) return;

                var vrooms = vroomsField.GetValue(__instance);
                if (vrooms == null) return;

                var vroomsDict = vrooms as System.Collections.IDictionary;
                if (vroomsDict == null) return;

                foreach (var v in vroomsDict.Values)
                {
                    if (v.GetType().Name == "VWaitingRoom")
                    {
                        var maxField = v.GetType().GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (maxField != null)
                        {
                            maxField.SetValue(v, NEW_LIMIT);
                            MelonLogger.Msg("[PATCH 5] VWaitingRoom._maxPlayers set to " + NEW_LIMIT);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PATCH 5] Exception: " + ex.Message);
            }
        }
    }

    // Патч 5 OLD: EnterWaitingRoom (VRoomManager) - заменяем проверку >= 4 на >= 999 (TRANSPILER - не работает)
    /*
    // НАЙДЕНО: vwaitingRoom.GetMemberCount() >= 4
    [HarmonyPatch]
    public class EnterWaitingRoom_Transpiler_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var methods = new List<MethodBase>();
            
            // Ищем класс VRoomManager
            var roomManagerType = assembly.GetTypes().FirstOrDefault(t => t.Name == "VRoomManager");
            
            if (roomManagerType != null)
            {
                var method = roomManagerType.GetMethod("EnterWaitingRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    methods.Add(method);
                    MelonLogger.Msg("[PATCH 5] Target found: VRoomManager.EnterWaitingRoom");
                    MelonLogger.Msg("[PATCH 5] Will replace 'if (count >= 4)' check with 'if (count >= 999)'");
                }
                else
                {
                    MelonLogger.Error("[PATCH 5] VRoomManager found but EnterWaitingRoom method missing!");
                }
            }
            else
            {
                MelonLogger.Error("[PATCH 5] VRoomManager class not found in Assembly-CSharp!");
            }
            
            return methods;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            int patchCount = 0;

            // Ищем константу 4 и заменяем на 999
            for (int i = 0; i < codes.Count; i++)
            {
                // Проверяем загрузку константы 4 (ldc.i4.4 или ldc.i4.s 4)
                if (codes[i].opcode == OpCodes.Ldc_I4_4 || 
                    (codes[i].opcode == OpCodes.Ldc_I4_S && codes[i].operand is sbyte && (sbyte)codes[i].operand == 4) ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int && (int)codes[i].operand == 4))
                {
                    // Проверяем, что следующая инструкция - сравнение (bge, bge.s, clt, cgt и т.д.)
                    if (i + 1 < codes.Count && IsComparisonInstruction(codes[i + 1].opcode))
                    {
                        MelonLogger.Msg("[PATCH 5] Found player limit check: constant 4 at IL_" + i);
                        codes[i] = new CodeInstruction(OpCodes.Ldc_I4, 999);
                        patchCount++;
                        MelonLogger.Msg("[PATCH 5] Replaced constant 4 -> 999");
                    }
                }
            }

            if (patchCount > 0)
            {
                MelonLogger.Msg("=================================================");
                MelonLogger.Msg("[PATCH 5] SUCCESS: EnterWaitingRoom patched!");
                MelonLogger.Msg("[PATCH 5] Replaced " + patchCount + " player limit check(s): 4 -> 999");
                MelonLogger.Msg("=================================================");
            }
            else
            {
                MelonLogger.Warning("[PATCH 5] No constant '4' found in EnterWaitingRoom - pattern might have changed!");
            }

            return codes;
        }

        static bool IsComparisonInstruction(OpCode opcode)
        {
            return opcode == OpCodes.Bge || opcode == OpCodes.Bge_S || opcode == OpCodes.Bge_Un || opcode == OpCodes.Bge_Un_S ||
                   opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_S || opcode == OpCodes.Bgt_Un || opcode == OpCodes.Bgt_Un_S ||
                   opcode == OpCodes.Ble || opcode == OpCodes.Ble_S || opcode == OpCodes.Ble_Un || opcode == OpCodes.Ble_Un_S ||
                   opcode == OpCodes.Blt || opcode == OpCodes.Blt_S || opcode == OpCodes.Blt_Un || opcode == OpCodes.Blt_Un_S ||
                   opcode == OpCodes.Beq || opcode == OpCodes.Beq_S ||
                   opcode == OpCodes.Clt || opcode == OpCodes.Clt_Un ||
                   opcode == OpCodes.Cgt || opcode == OpCodes.Cgt_Un ||
                   opcode == OpCodes.Ceq;
        }
    }
    */

    // Патч 6 OLD: ОТКЛЮЧЕН - был слишком агрессивным и крашил игру
    // Проблема: патчил все константы 4 в 297 методах, включая неправильные
    // TODO: Сделать более избирательным или патчить конкретные методы
    /*
    [HarmonyPatch]
    public class GlobalConstantScanner_Transpiler_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var methods = new List<MethodBase>();
            
            MelonLogger.Msg("[PATCH 6] Scanning for classes with 'VWorld', 'VRoom', 'Player' in name...");
            
            // Ищем все классы с VWorld, VRoom, Player в названии
            var targetTypes = assembly.GetTypes().Where(t => 
                t.Name.Contains("VWorld") || 
                t.Name.Contains("VRoom") || 
                t.Name.Contains("VPlayer") ||
                t.Name.Contains("PlayerCount") ||
                t.Name.Contains("SessionInfo") ||
                t.Name.Contains("GameSession")
            ).ToList();
            
            MelonLogger.Msg("[PATCH 6] Found " + targetTypes.Count + " candidate classes");
            
            foreach (var type in targetTypes)
            {
                try
                {
                    var typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                        .Where(m => !m.IsAbstract && m.DeclaringType == type);
                    
                    methods.AddRange(typeMethods);
                }
                catch { }
            }
            
            MelonLogger.Msg("[PATCH 6] Will scan " + methods.Count + " methods for constant '4' patterns");
            
            return methods;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            int patchCount = 0;
            
            for (int i = 0; i < codes.Count; i++)
            {
                // Ищем любую загрузку константы 4
                if (codes[i].opcode == OpCodes.Ldc_I4_4 || 
                    (codes[i].opcode == OpCodes.Ldc_I4_S && codes[i].operand is sbyte && (sbyte)codes[i].operand == 4) ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int && (int)codes[i].operand == 4))
                {
                    // Проверяем контекст: следующая инструкция должна быть сравнением или условным переходом
                    bool isPlayerCheck = false;
                    
                    if (i + 1 < codes.Count)
                    {
                        var nextOp = codes[i + 1].opcode;
                        
                        // Если после константы идёт сравнение или условный переход - это вероятно проверка лимита
                        if (IsComparisonOrBranch(nextOp))
                        {
                            isPlayerCheck = true;
                        }
                    }
                    
                    if (isPlayerCheck)
                    {
                        string className = (original.DeclaringType != null) ? original.DeclaringType.Name : "Unknown";
                        string methodName = original.Name;
                        
                        MelonLogger.Msg("[PATCH 6] Found constant 4 in " + className + "." + methodName + " at IL_" + i);
                        codes[i] = new CodeInstruction(OpCodes.Ldc_I4, 999);
                        patchCount++;
                    }
                }
            }
            
            if (patchCount > 0)
            {
                string className = (original.DeclaringType != null) ? original.DeclaringType.Name : "Unknown";
                string methodName = original.Name;
                MelonLogger.Msg("[PATCH 6] Patched " + className + "." + methodName + " - replaced " + patchCount + " constant(s)");
            }
            
            return codes;
        }

        static bool IsComparisonOrBranch(OpCode opcode)
        {
            // Сравнения
            if (opcode == OpCodes.Ceq || opcode == OpCodes.Cgt || opcode == OpCodes.Cgt_Un || 
                opcode == OpCodes.Clt || opcode == OpCodes.Clt_Un)
                return true;
            
            // Условные переходы
            if (opcode == OpCodes.Beq || opcode == OpCodes.Beq_S ||
                opcode == OpCodes.Bge || opcode == OpCodes.Bge_S || opcode == OpCodes.Bge_Un || opcode == OpCodes.Bge_Un_S ||
                opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_S || opcode == OpCodes.Bgt_Un || opcode == OpCodes.Bgt_Un_S ||
                opcode == OpCodes.Ble || opcode == OpCodes.Ble_S || opcode == OpCodes.Ble_Un || opcode == OpCodes.Ble_Un_S ||
                opcode == OpCodes.Blt || opcode == OpCodes.Blt_S || opcode == OpCodes.Blt_Un || opcode == OpCodes.Blt_Un_S ||
                opcode == OpCodes.Bne_Un || opcode == OpCodes.Bne_Un_S)
                return true;
            
            return false;
        }
    }
    */

    // Патч 7: CanEnterChannel() - ОСНОВНАЯ проверка входа (из рабочего мода!)
    // Это метод который реально решает может ли игрок войти в комнату
    [HarmonyPatch]
    public class CanEnterChannel_AllRooms_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            List<MethodBase> methods = new List<MethodBase>();
            
            // Патчим VWaitingRoom
            var vwaitingRoomType = assembly.GetTypes().FirstOrDefault(t => t.Name == "VWaitingRoom");
            if (vwaitingRoomType != null)
            {
                var method = vwaitingRoomType.GetMethod("CanEnterChannel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    methods.Add(method);
                    MelonLogger.Msg("[PATCH 7] Target found: VWaitingRoom.CanEnterChannel()");
                }
            }
            
            // Патчим MaintenanceRoom
            var maintenanceRoomType = assembly.GetTypes().FirstOrDefault(t => t.Name == "MaintenanceRoom");
            if (maintenanceRoomType != null)
            {
                var method = maintenanceRoomType.GetMethod("CanEnterChannel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    methods.Add(method);
                    MelonLogger.Msg("[PATCH 7] Target found: MaintenanceRoom.CanEnterChannel()");
                }
            }
            
            MelonLogger.Msg("[PATCH 7] Found " + methods.Count + " CanEnterChannel methods to patch");
            return methods;
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            const int NEW_LIMIT = 999;
            try
            {
                var getCountMethod = __instance.GetType().GetMethod("GetMemberCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (getCountMethod == null)
                {
                    return true;
                }
                
                int count = (int)getCountMethod.Invoke(__instance, null);
                string roomName = __instance.GetType().Name;

                // Попытка получить nickname игрока
                string playerInfo = "UID:" + playerUID;
                try
                {
                    // Пробуем найти Steam CSteamID и получить nickname
                    var steamworksAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name.Contains("Steamworks"));
                    
                    if (steamworksAssembly != null)
                    {
                        var steamFriendsType = steamworksAssembly.GetTypes().FirstOrDefault(t => t.Name == "SteamFriends");
                        var csteamIDType = steamworksAssembly.GetTypes().FirstOrDefault(t => t.Name == "CSteamID");
                        
                        if (steamFriendsType != null && csteamIDType != null)
                        {
                            // Создаём CSteamID из long
                            var steamIDConstructor = csteamIDType.GetConstructor(new[] { typeof(ulong) });
                            if (steamIDConstructor != null)
                            {
                                var steamID = steamIDConstructor.Invoke(new object[] { (ulong)playerUID });
                                
                                // Получаем nickname
                                var getPersonaNameMethod = steamFriendsType.GetMethod("GetFriendPersonaName", BindingFlags.Public | BindingFlags.Static);
                                if (getPersonaNameMethod != null)
                                {
                                    string nickname = (string)getPersonaNameMethod.Invoke(null, new[] { steamID });
                                    if (!string.IsNullOrEmpty(nickname))
                                    {
                                        playerInfo = "'" + nickname + "' (UID:" + playerUID + ")";
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Если не получилось - просто покажем UID
                }

                MelonLogger.Msg("[PATCH 7] CanEnterChannel called on " + roomName + ": " + count + "/" + NEW_LIMIT + " players - Player: " + playerInfo);

                if (count >= NEW_LIMIT)
                {
                    // Возвращаем MsgErrorCode.PlayerCountExceeded
                    var msgErrorCodeType = typeof(FishySteamworks.Server.ServerSocket).Assembly.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");
                    if (msgErrorCodeType != null && msgErrorCodeType.IsEnum)
                    {
                        __result = System.Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                    }
                    MelonLogger.Warning("[PATCH 7] ❌ Room " + roomName + " FULL (" + count + "/" + NEW_LIMIT + ") - Player " + playerInfo + " BLOCKED");
                }
                else
                {
                    // Возвращаем MsgErrorCode.Success
                    var msgErrorCodeType = typeof(FishySteamworks.Server.ServerSocket).Assembly.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");
                    if (msgErrorCodeType != null && msgErrorCodeType.IsEnum)
                    {
                        __result = System.Enum.Parse(msgErrorCodeType, "Success");
                    }
                    MelonLogger.Msg("[PATCH 7] ✅ Player " + playerInfo + " can enter " + roomName + " (" + count + "/" + NEW_LIMIT + ") - SUCCESS");
                }

                return false; // Skip original
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PATCH 7] Exception in CanEnterChannel patch: " + ex.Message);
                return true;
            }
        }
    }

    // Патч 8: EnterMaintenenceRoom - устанавливаем _maxPlayers
    [HarmonyPatch]
    public class EnterMaintenenceRoom_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var vroomManagerType = assembly.GetTypes().FirstOrDefault(t => t.Name == "VRoomManager");
            
            if (vroomManagerType != null)
            {
                var method = vroomManagerType.GetMethod("EnterMaintenenceRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    MelonLogger.Msg("[PATCH 8] Target found: VRoomManager.EnterMaintenenceRoom()");
                    return new[] { method };
                }
            }
            
            return new MethodBase[0];
        }

        static void Prefix(object __instance)
        {
            const int NEW_LIMIT = 999;
            try
            {
                var vroomsField = __instance.GetType().GetField("_vrooms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (vroomsField == null) return;

                var vrooms = vroomsField.GetValue(__instance);
                if (vrooms == null) return;

                // Iterate через dictionary
                var vroomsDict = vrooms as System.Collections.IDictionary;
                if (vroomsDict == null) return;

                foreach (var v in vroomsDict.Values)
                {
                    if (v.GetType().Name == "MaintenanceRoom")
                    {
                        var maxField = v.GetType().GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (maxField != null)
                        {
                            maxField.SetValue(v, NEW_LIMIT);
                            MelonLogger.Msg("[PATCH 8] MaintenanceRoom._maxPlayers set to " + NEW_LIMIT);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PATCH 8] Exception: " + ex.Message);
            }
        }
    }

    // Патч 9: Steam Lobby Creation - заменяем константу 4 на 999
    // Код: SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
    [HarmonyPatch]
    public class SteamLobbyCreation_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var steamInviteType = assembly.GetTypes().FirstOrDefault(t => t.Name == "SteamInviteDispatcher");
            
            if (steamInviteType != null)
            {
                var method = steamInviteType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (method != null)
                {
                    MelonLogger.Msg("[PATCH 9] Target found: SteamInviteDispatcher.CreateLobby()");
                    MelonLogger.Msg("[PATCH 9] Will replace constant 4 with 999 in IL code");
                    return new[] { method };
                }
            }
            
            MelonLogger.Warning("[PATCH 9] SteamInviteDispatcher.CreateLobby not found");
            return new MethodBase[0];
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int patchCount = 0;

            // Ищем константу 4 и заменяем на 999
            for (int i = 0; i < codes.Count; i++)
            {
                // ldc.i4.4 или ldc.i4.s 4 или ldc.i4 4
                if (codes[i].opcode == OpCodes.Ldc_I4_4 || 
                    (codes[i].opcode == OpCodes.Ldc_I4_S && codes[i].operand is sbyte && (sbyte)codes[i].operand == 4) ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int && (int)codes[i].operand == 4))
                {
                    MelonLogger.Msg("[PATCH 9] Found constant 4 at IL_" + i + " - replacing with 999");
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4, 999);
                    patchCount++;
                }
            }

            if (patchCount > 0)
            {
                MelonLogger.Msg("[PATCH 9] SUCCESS: Replaced " + patchCount + " constant(s) in CreateLobby");
                MelonLogger.Msg("[PATCH 9] Steam lobby will be created with 999 slots!");
            }
            else
            {
                MelonLogger.Warning("[PATCH 9] No constant 4 found - CreateLobby might have different structure");
            }

            return codes;
        }
    }

    // Патч 6 (старый): Более безопасный подход - патчим только GetMemberCount в VWaitingRoom
    // ОСТАВЛЯЕМ для совместимости, но теперь PATCH 7 важнее
    [HarmonyPatch]
    public class GetMemberCount_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = typeof(FishySteamworks.Server.ServerSocket).Assembly;
            var vwaitingRoomType = assembly.GetTypes().FirstOrDefault(t => t.Name == "VWaitingRoom");
            
            if (vwaitingRoomType != null)
            {
                var method = vwaitingRoomType.GetMethod("GetMemberCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    MelonLogger.Msg("[PATCH 6] Target found: VWaitingRoom.GetMemberCount()");
                    MelonLogger.Msg("[PATCH 6] Will return max(actualCount, 0) to bypass >= 4 check");
                    return new[] { method };
                }
                else
                {
                    MelonLogger.Warning("[PATCH 6] VWaitingRoom found but GetMemberCount() missing");
                }
            }
            else
            {
                MelonLogger.Warning("[PATCH 6] VWaitingRoom class not found");
            }
            
            return new MethodBase[0];
        }

        static bool Prefix(ref int __result, object __instance)
        {
            // Получаем реальное количество
            int actualCount = 0;
            try
            {
                var field = __instance.GetType().GetField("_members", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var members = field.GetValue(__instance);
                    if (members != null)
                    {
                        var countProp = members.GetType().GetProperty("Count");
                        if (countProp != null)
                        {
                            actualCount = (int)countProp.GetValue(members, null);
                        }
                    }
                }
            }
            catch { }
            
            // Всегда возвращаем 0 чтобы обойти >= 4 проверку
            __result = 0;
            MelonLogger.Msg("[PATCH 6] GetMemberCount() called - actual: " + actualCount + ", returning: 0 (to bypass >= 4 check)");
            return false;
        }
    }
}