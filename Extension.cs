using BitStream;
using HarmonyLib;
using InControl;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OC2Jetpack.Extension
{
    public static class BitStreamReaderExtension
    {
        private static readonly FieldInfo fieldInfo_bufferLengthInBits = AccessTools.Field(typeof(BitStreamReader), "_bufferLengthInBits");
        private static readonly FieldInfo fieldInfo_cbitsInPartialByte = AccessTools.Field(typeof(BitStreamReader), "_cbitsInPartialByte");
        private static readonly FieldInfo fieldInfo_partialByte = AccessTools.Field(typeof(BitStreamReader), "_partialByte");
        private static readonly FieldInfo fieldInfo_byteArray = AccessTools.Field(typeof(BitStreamReader), "_byteArray");
        private static readonly FieldInfo fieldInfo_byteArrayIndex = AccessTools.Field(typeof(BitStreamReader), "_byteArrayIndex");
        
        public static byte ReadByteAhead(this BitStreamReader instance, int countOfBits)
        {
            if (instance.EndOfStream) return 0;
            if (countOfBits > 8 || countOfBits <= 0) return 0;
            if ((long)countOfBits > (long)(ulong)(uint)fieldInfo_bufferLengthInBits.GetValue(instance)) return 0;
            byte b;
            
            int cbitsInPartialByte = (int)fieldInfo_cbitsInPartialByte.GetValue(instance);
            byte partialByte = (byte)fieldInfo_partialByte.GetValue(instance);
            if (cbitsInPartialByte >= countOfBits)
            {
                int num = 8 - countOfBits;
                b = (byte)(partialByte >> num);
            }
            else
            {
                byte[] byteArray = (byte[])fieldInfo_byteArray.GetValue(instance);
                byte b2 = byteArray[(int)fieldInfo_byteArrayIndex.GetValue(instance)];
                int num2 = 8 - countOfBits;
                b = (byte)(partialByte >> num2);
                int num3 = num2 + cbitsInPartialByte;
                b |= (byte)(b2 >> num3);
            }
            return b;
        }
    }

    public static class ClientPlayerControlsImpl_DefaultExtension
    {
        private static readonly FieldInfo fieldInfo_m_impactTimer = AccessTools.Field(typeof(ClientPlayerControlsImpl_Default), "m_impactTimer");
        private static readonly FieldInfo fieldInfo_m_isFalling = AccessTools.Field(typeof(ClientPlayerControlsImpl_Default), "m_isFalling");
        public static float get_m_impactTimer(this ClientPlayerControlsImpl_Default instance)
        {
            return (float)fieldInfo_m_impactTimer.GetValue(instance);
        }
        public static bool get_m_isFalling(this ClientPlayerControlsImpl_Default instance)
        {
            return (bool)fieldInfo_m_isFalling.GetValue(instance);
        }
    }

    public static class KeyboardRebindExtension
    {
        private static readonly MethodInfo methodInfo_KeysToString = AccessTools.Method(typeof(KeyboardRebindElement), "KeysToString");
        private static readonly MethodInfo methodInfo_SetKeyBindingsText = AccessTools.Method(typeof(KeyboardRebindElement), "SetKeyBindingsText");
        private static readonly FieldInfo fieldInfo_m_ButtonID = AccessTools.Field(typeof(KeyboardRebindButtonElement), "m_ButtonID");
        private static readonly FieldInfo fieldInfo_m_Side = AccessTools.Field(typeof(KeyboardRebindButtonElement), "m_Side");
        private static readonly MethodInfo methodInfo_ShowRebindDialog = AccessTools.Method(typeof(KeyboardRebindController), "ShowRebindDialog");
        private static readonly MethodInfo methodInfo_HideRebindDialog = AccessTools.Method(typeof(KeyboardRebindController), "HideRebindDialog");
        private static readonly FieldInfo fieldInfo_keyCodes = AccessTools.Field(typeof(KeyInfo), "keyCodes");
        private static readonly FieldInfo fieldInfo_m_ControlSchemeIndex = AccessTools.Field(typeof(ControlSchemeToggle), "m_ControlSchemeIndex");
        private static readonly MethodInfo methodInfo_ShowControlScheme = AccessTools.Method(typeof(ControlSchemeToggle), "ShowControlScheme");

        public static void ShowCurrentControlScheme(this ControlSchemeToggle instance)
        {
            int index = (int)fieldInfo_m_ControlSchemeIndex.GetValue(instance);
            methodInfo_ShowControlScheme.Invoke(instance, new object[] { index });
        }

        public static string KeyToString(this KeyboardRebindElement instance, Key key)
        {
            return (string)methodInfo_KeysToString.Invoke(instance, new object[] { new List<Key> { key } });
        }

        public static void SetKeyBindingsText(this KeyboardRebindElement instance, string keyBindingsText)
        {
            methodInfo_SetKeyBindingsText.Invoke(instance, new object[] { keyBindingsText });
        }

        public static void set_m_ButtonID(this KeyboardRebindButtonElement instance, PlayerInputLookup.LogicalButtonID m_ButtonID)
        {
            fieldInfo_m_ButtonID.SetValue(instance, m_ButtonID);
        }

        public static PlayerInputLookup.LogicalButtonID get_m_ButtonID(this KeyboardRebindButtonElement instance)
        {
            return (PlayerInputLookup.LogicalButtonID)fieldInfo_m_ButtonID.GetValue(instance);
        }
        public static void set_m_Side(this KeyboardRebindButtonElement instance, PadSide m_Side)
        {
            fieldInfo_m_Side.SetValue(instance, m_Side);
        }

        public static PadSide get_m_Side(this KeyboardRebindButtonElement instance)
        {
            return (PadSide)fieldInfo_m_Side.GetValue(instance);
        }

        public static bool ShowRebindDialog(this KeyboardRebindController instance, KeyboardRebindElement keyboardRebindElement)
        {
            return (bool)methodInfo_ShowRebindDialog.Invoke(instance, new object[] { keyboardRebindElement });
        }

        public static void HideRebindDialog(this KeyboardRebindController instance)
        {
            methodInfo_HideRebindDialog.Invoke(instance, null);
        }

        public static KeyCode ToKeyCode(this Key key)
        {
            KeyCode[] keyCodes = (KeyCode[])fieldInfo_keyCodes.GetValue(KeyInfo.KeyList[(int)key]);
            return keyCodes[0];
        }
    }
}
