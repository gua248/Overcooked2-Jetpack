using System;
using HarmonyLib;
using UnityEngine;
using OC2Jetpack.Extension;
using Team17.Online.Multiplayer;
using Team17.Online.Multiplayer.Messaging;
using BitStream;
using System.Collections.Generic;
using System.Linq;
using InControl;

namespace OC2Jetpack
{
    public static class MailboxPatch
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method("Mailbox:OnMessageReceived"), new HarmonyMethod(typeof(MailboxPatch).GetMethod("Prefix")), null);
        }
        public static bool Prefix(MessageType type, Serialisable message)
        {
            if (type != JetpackPlayerControl.jetpackMessageType)
                return true;
            JetpackMessage jetpackMessage = (JetpackMessage)message;
            JetpackPlayerControl.OnMessageReceived(jetpackMessage);
            return false;
        }
    }

    public static class ClientMessengerPatch
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method("ClientMessenger:OnClientStarted"), null, new HarmonyMethod(typeof(ClientMessengerPatch).GetMethod("Postfix")));
        }
        public static void Postfix(Client client)
        {
            JetpackPlayerControl.localClient = client;
        }
    }

    public static class ServerMessengerPatch
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method("ServerMessenger:OnServerStarted"), null, new HarmonyMethod(typeof(ServerMessengerPatch).GetMethod("PostfixStarted")));
            harmony.Patch(AccessTools.Method("ServerMessenger:OnServerStopped"), null, new HarmonyMethod(typeof(ServerMessengerPatch).GetMethod("PostfixStopped")));
        }
        public static void PostfixStarted(Server server)
        {
            JetpackPlayerControl.localServer = server;
        }
        public static void PostfixStopped()
        {
            JetpackPlayerControl.localServer = null;
        }
    }

    public static class Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FrontendCoopTabOptions), "OnOnlinePublicClicked")]
        [HarmonyPatch(typeof(FrontendVersusTabOptions), "OnOnlinePublicClicked")]
        public static bool OnOnlinePublicClickedPatch()
        {
            JetpackKeyboardRebind.jetpackKey = new Key[] { Key.None, Key.None, Key.None };
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPlayerRespawnBehaviour), "PauseMovement")]
        public static void ClientPlayerRespawnBehaviourPauseMovementPatch(ClientPlayerRespawnBehaviour __instance)
        {
            JetpackPlayerControl jetpackPlayerControl = __instance.gameObject.GetComponent<JetpackPlayerControl>();
            if (jetpackPlayerControl == null) return;
            if (jetpackPlayerControl.jetpackButton == null) return;
            jetpackPlayerControl.clientJetpackPlayerControl.cruising = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientChefSynchroniser), "ControlsMovingPlayer")]
        public static bool ClientChefSynchroniserControlsMovingPlayerPrefix(ref bool __result, ClientChefSynchroniser __instance)
        {
            JetpackPlayerControl jetpackPlayerControl = __instance.gameObject.GetComponent<JetpackPlayerControl>();
            if (jetpackPlayerControl == null) return true;
            if (jetpackPlayerControl.jetpackButton == null) return true;
            if (!jetpackPlayerControl.clientJetpackPlayerControl.jetting) return true;
            __result = true;
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ClientChefSynchroniser), "RunCorrection")]
        public static IEnumerable<CodeInstruction> ClientChefSynchroniserRunCorrectionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            codes[402].operand = 6.25f;
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "ApplyGravityForce")]
        public static bool ClientPlayerControlsImpl_DefaultApplyGravityForcePatch(ClientPlayerControlsImpl_Default __instance)
        {
            return JetpackPlayerControl.ApplyLiftForce(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimationDecisions), "OnFall")]
        public static bool PlayerAnimationDecisionsOnFallPatch(PlayerAnimationDecisions __instance)
        {
            ClientJetpackPlayerControl clientJetpackPlayerControl = __instance.gameObject.GetComponent<ClientJetpackPlayerControl>();
            if (clientJetpackPlayerControl != null)
                if (clientJetpackPlayerControl.jetting)
                    return false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Message), "Deserialise")]
        public static bool MessageDeserialisePatch(BitStreamReader reader, Message __instance, ref bool __result)
        {
            __instance.Type = (MessageType)reader.ReadByteAhead(8);
            if (__instance.Type == JetpackPlayerControl.jetpackMessageType)
            {
                __instance.Type = (MessageType)reader.ReadByte(8);
                __instance.Payload = new JetpackMessage(false, false, 0, false);
                __instance.Payload.Deserialise(reader);
                __result = true;
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkMessageTracker), "TrackSentGlobalEvent")]
        public static bool NetworkMessageTrackerTrackSentGlobalEventPatch(MessageType type)
        {
            return type != JetpackPlayerControl.jetpackMessageType;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkMessageTracker), "TrackReceivedGlobalEvent")]
        public static bool NetworkMessageTrackerTrackReceivedGlobalEventPatch(MessageType type)
        {
            return type != JetpackPlayerControl.jetpackMessageType;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ControlSchemeToggle), "OnEnable")]
        public static void ControlSchemeToggleOnEnablePatch(ControlSchemeToggle __instance)
        {
            JetpackKeyboardRebind.AddAllRebindUI();
            __instance.ShowCurrentControlScheme();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Localization), "Get", new Type[] { typeof(string), typeof(LocToken[])} )]
        public static void LocalizationGetPatch(ref string __result, string tag)
        {
            if (Localization.GetLanguage() == SupportedLanguages.Chinese && tag.Equals("Text.ControlsMenu.RemapBody") && !__result.EndsWith("["))
                __result += "[";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KeyboardRebindButtonElement), "RefreshBindingText")]
        public static bool KeyboardRebindButtonElementRefreshBindingTextPatch(KeyboardRebindButtonElement __instance)
        {
            if (__instance.get_m_ButtonID() == JetpackKeyboardRebind.jetpackButtonID)
            {
                PadSide side = __instance.get_m_Side();
                int id = side == PadSide.Both ? 0 : (side == PadSide.Left ? 1 : 2);
                JetpackKeyboardRebind.RefreshBindingText(__instance, id);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KeyboardRebindButtonElement), "HasAnyBindings")]
        public static bool KeyboardRebindButtonElementHasAnyBindingsPatch(KeyboardRebindButtonElement __instance, ref bool __result)
        {
            if (__instance.get_m_ButtonID() == JetpackKeyboardRebind.jetpackButtonID)
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientKitchenLoader), "StartEntities")]
        public static void ClientKitchenLoaderStartEntitiesPatch()
        {
            JetpackPlayerControl.AttachControl();
        }
    }
}
