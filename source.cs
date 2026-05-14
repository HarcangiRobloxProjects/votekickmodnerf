using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using System;
using InnerNet;
using Hazel;
using System.Collections.Generic;
using System.Linq;

namespace VotekickMod
{
    [BepInPlugin("com.votekick.mod", "Votekick Mod", "1.0.0")]
    public class VotekickPlugin : BasePlugin
    {
        public static ManualLogSource Logger;
        public static bool showGui = false;
        public static int currentTab = 0;
        public static Vector2 scrollPosition = Vector2.zero;
        
        public static bool ol1Active = false;
        public static bool ol2Active = false;
        public static int selectedOverloadMethod = 1;
        private static Dictionary<byte, int> rpcSequences = new Dictionary<byte, int>();

        public static ConfigEntry<bool> AnticheatEnabled;
        public static ConfigEntry<KeyCode> MenuKeybind;
        
        public const byte VotekickHandshakeId = 255;

        public override void Load()
        {
            Logger = Log;
            AnticheatEnabled = Config.Bind("Anticheat", "Enabled", true, "Block illegal RPCs :D");
            MenuKeybind = Config.Bind("General", "MenuKeybind", KeyCode.F2, "Menu Key :D");

            var harmony = new Harmony("com.votekick.mod");
            harmony.PatchAll();
            
            AddComponent<VotekickMenu>();
            AddComponent<AnticheatCore>();
            AddComponent<HandshakeSender>();
        }

        public static int NextSetRpcSeqId(byte rpcId)
        {
            if (!rpcSequences.ContainsKey(rpcId)) rpcSequences[rpcId] = 0;
            return rpcSequences[rpcId]++;
        }
    }

    public class VotekickMenu : MonoBehaviour
    {
        public VotekickMenu(IntPtr ptr) : base(ptr) { }
        private Rect windowRect = new Rect(30, 30, 340, 450);

        private void Update() 
        { 
            if (Input.GetKeyDown(VotekickPlugin.MenuKeybind.Value)) 
                VotekickPlugin.showGui = !VotekickPlugin.showGui;

            if (VotekickPlugin.ol1Active) SafeOverload1LoopStep();
            if (VotekickPlugin.ol2Active) SafeOverload2LoopStep();
        }

        private void OnGUI()
        {
            if (!VotekickPlugin.showGui) return;
            float freq = Time.time * 2f;
            GUI.color = new Color(Mathf.Sin(freq) * 0.5f + 0.5f, Mathf.Sin(freq + 2f) * 0.5f + 0.5f, Mathf.Sin(freq + 4f) * 0.5f + 0.5f);
            windowRect = GUI.Window(0, windowRect, (Action<int>)DrawWindow, "VotekickMod Private Nerfed :D");
        }

        private void DrawWindow(int windowID)
        {
            GUI.color = Color.white; 
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Schizo :D")) VotekickPlugin.currentTab = 0;
            if (GUILayout.Button("Overload :D")) VotekickPlugin.currentTab = 1;
            if (GUILayout.Button("Toggles :D")) VotekickPlugin.currentTab = 2;
            if (GUILayout.Button("AC :D")) VotekickPlugin.currentTab = 3;
            GUILayout.EndHorizontal();

            try {
                VotekickPlugin.scrollPosition = GUILayout.BeginScrollView(VotekickPlugin.scrollPosition);
                switch (VotekickPlugin.currentTab)
                {
                    case 0: DrawSchizoTab(); break;
                    case 1: DrawOverloadTab(); break;
                    case 2: DrawTogglesTab(); break;
                    case 3: DrawAnticheatTab(); break;
                }
            } finally {
                GUILayout.EndScrollView();
            }
            GUI.DragWindow();
        }

        private void DrawSchizoTab()
        {
            GUILayout.Label("<b>Schizos :D</b>");
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.AmOwner || p.Data == null) continue;
                GUILayout.BeginVertical("box");
                GUILayout.Label("Target " + p.Data.PlayerName + " :D");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reactor :D")) FakeReactorTarget(p);
                if (GUILayout.Button("Doors :D")) DoorHallucination(p);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void DrawOverloadTab()
        {
            GUILayout.Label("<b>Network Flood :D</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(VotekickPlugin.selectedOverloadMethod == 1, "SetRpc :D")) VotekickPlugin.selectedOverloadMethod = 1;
            if (GUILayout.Toggle(VotekickPlugin.selectedOverloadMethod == 2, "Physics :D")) VotekickPlugin.selectedOverloadMethod = 2;
            GUILayout.EndHorizontal();
            VotekickPlugin.ol1Active = GUILayout.Toggle(VotekickPlugin.ol1Active, "OVERLOAD Host :D", "button", GUILayout.Height(30));
            VotekickPlugin.ol2Active = GUILayout.Toggle(VotekickPlugin.ol2Active, "OVERLOAD All :D", "button", GUILayout.Height(30));
        }

        private void DrawTogglesTab()
        {
            GUILayout.Label("<b>Toggles Hacks :D</b>");
            ImmortalityLogic.Enabled = GUILayout.Toggle(ImmortalityLogic.Enabled, "God Mode Vent :D");
            GUILayout.Space(10);
            GUILayout.Label("Menu Key: " + VotekickPlugin.MenuKeybind.Value.ToString() + " :D");
        }

        private void DrawAnticheatTab()
        {
            GUILayout.Label("<b>Host Protection :D</b>");
            VotekickPlugin.AnticheatEnabled.Value = GUILayout.Toggle(VotekickPlugin.AnticheatEnabled.Value, "Block Hack RPCs :D");
            GUILayout.Label("<color=yellow>Blocking only. No auto-kick.</color>");
        }

        private void SafeOverload1LoopStep()
        {
            AmongUsClient client = AmongUsClient.Instance;
            if (client?.connection == null || client.ClientId == client.HostId) { VotekickPlugin.ol1Active = false; return; }
            try {
                PlayerControl local = PlayerControl.LocalPlayer;
                for (int i = 0; i < 50; i++) {
                    bool isSetRpc = VotekickPlugin.selectedOverloadMethod == 1;
                    byte rpcId = isSetRpc ? (new byte[] { 39, 40, 42, 43 })[i % 4] : (byte)20;
                    uint netId = isSetRpc ? local.NetId : local.MyPhysics.NetId;
                    MessageWriter w = client.StartRpcImmediately(netId, rpcId, SendOption.None, client.HostId);
                    if (w != null) {
                        if (isSetRpc) { w.Write(""); w.Write(VotekickPlugin.NextSetRpcSeqId(rpcId)); }
                        else { w.WritePacked(0); }
                        client.FinishRpcImmediately(w);
                    }
                }
            } catch { VotekickPlugin.ol1Active = false; }
        }

        private void SafeOverload2LoopStep()
        {
            AmongUsClient client = AmongUsClient.Instance;
            if (client?.connection == null) { VotekickPlugin.ol2Active = false; return; }
            try {
                PlayerControl local = PlayerControl.LocalPlayer;
                for (int i = 0; i < 50; i++) {
                    bool isSetRpc = VotekickPlugin.selectedOverloadMethod == 1;
                    byte rpcId = isSetRpc ? (new byte[] { 39, 40, 42, 43 })[i % 4] : (byte)20;
                    uint netId = isSetRpc ? local.NetId : local.MyPhysics.NetId;
                    MessageWriter w = client.StartRpcImmediately(netId, rpcId, SendOption.None, -1);
                    if (w != null) {
                        if (isSetRpc) { w.Write(""); w.Write(VotekickPlugin.NextSetRpcSeqId(rpcId)); }
                        else { w.WritePacked(0); }
                        client.FinishRpcImmediately(w);
                    }
                }
            } catch { VotekickPlugin.ol2Active = false; }
        }

        public static void FakeReactorTarget(PlayerControl target) {
            if (target == null || ShipStatus.Instance == null) return;
            MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, 35, SendOption.Reliable, (int)target.Data.ClientId);
            w.Write((byte)SystemTypes.Reactor); w.WritePacked(PlayerControl.LocalPlayer.NetId); w.Write((byte)128);
            AmongUsClient.Instance.FinishRpcImmediately(w);
        }

        public static void DoorHallucination(PlayerControl target) {
            if (target == null || ShipStatus.Instance == null) return;
            foreach (var door in ShipStatus.Instance.AllDoors) {
                MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, 27, SendOption.Reliable, (int)target.Data.ClientId);
                w.Write((byte)door.Room);
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
        }
    }

    public class HandshakeSender : MonoBehaviour {
        public HandshakeSender(IntPtr ptr) : base(ptr) { }
        private float timer = 5f;
        private void Update() {
            timer -= Time.deltaTime;
            if (timer <= 0f && PlayerControl.LocalPlayer != null) {
                MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, VotekickPlugin.VotekickHandshakeId, SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(w);
                timer = 10f;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class RpcPatch {
        public static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader) {
            if (callId == VotekickPlugin.VotekickHandshakeId) {
                if (__instance.cosmetics?.nameText != null && !__instance.cosmetics.nameText.text.Contains("VotekickMod")) {
                    __instance.cosmetics.nameText.text += "\n<color=#800080><size=70%>VotekickMod Private or Nerfed User :D</size></color>";
                }
                return false;
            }

            if (VotekickPlugin.AnticheatEnabled.Value && AmongUsClient.Instance.AmHost)
            {
                bool block = false;
                AnticheatCore.ValidateRpc(__instance, callId, reader, ref block);
                if (block) return false;
            }
            return true;
        }
    }

    public static class ImmortalityLogic {
        public static bool Enabled = false;
    }

    [HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
    public static class VentPatch { 
        public static bool Prefix(int ventId) => ventId == 50 || !ImmortalityLogic.Enabled; 
    }

    public class AnticheatCore : MonoBehaviour {
        public AnticheatCore(IntPtr ptr) : base(ptr) { }
        
        public static void ValidateRpc(PlayerControl player, byte callId, MessageReader reader, ref bool blockRpc)
        {
            if (player == null || player.AmOwner || player.NetId == PlayerControl.LocalPlayer?.NetId) return;

            switch (callId)
            {
                case 2: case 6: ValidateName(player, reader, ref blockRpc); break;
                case 11: ValidateScanner(player, reader, ref blockRpc); break;
                case 14: ValidateTask(player, reader, ref blockRpc); break;
                case 19: ValidateSnap(player, reader, ref blockRpc); break;
                case 23: case 24: ValidateVent(player, reader, ref blockRpc); break;
                case 35: ValidateSystemUpdate(player, reader, ref blockRpc); break;
            }
            
            if (blockRpc && DestroyableSingleton<HudManager>.Instance?.Notifier != null)
                DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("<color=red>Blocked Hack from " + player.Data.PlayerName + "</color>");
        }

        private static void ValidateName(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            string name = reader.ReadString();
            if (name.Length > 12 || name.Contains("<")) blockRpc = true;
        }

        private static void ValidateScanner(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            bool scan = reader.ReadBoolean();
            if (scan && (ShipStatus.Instance == null || RoleManager.IsImpostorRole(player.Data.RoleType))) blockRpc = true;
        }

        private static void ValidateTask(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            uint tIdx = reader.ReadPackedUInt32();
            if (ShipStatus.Instance == null || (tIdx + 1 > player.Data.Tasks.Count)) blockRpc = true;
        }

        private static void ValidateVent(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            if (ShipStatus.Instance == null || (!player.Data.IsDead && !player.Data.Role.CanVent)) blockRpc = true;
        }

        private static void ValidateSnap(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            if (LobbyBehaviour.Instance != null) blockRpc = true;
        }

        private static void ValidateSystemUpdate(PlayerControl player, MessageReader reader, ref bool blockRpc) {
            if (ShipStatus.Instance == null) return;
            SystemTypes sys = (SystemTypes)reader.ReadByte();
            if (!ShipStatus.Instance.Systems.ContainsKey(sys)) { blockRpc = true; return; }
            if (sys == SystemTypes.Reactor) {
                byte op = reader.ReadByte();
                if (op == 16 || op == 128) blockRpc = true;
            }
        }
    }
}
