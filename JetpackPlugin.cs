using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OC2Jetpack
{
    [BepInPlugin("dev.gua.overcooked.jetpack", "Overcooked2 Jetpack Plugin", "1.0")]
    [BepInProcess("Overcooked2.exe")]
    public class JetpackPlugin : BaseUnityPlugin
    {
        public static JetpackPlugin pluginInstance;
        private static Harmony patcher;
        
        public void Awake()
        {
            pluginInstance = this;
            patcher = new Harmony("dev.gua.overcooked.jetpack");
            patcher.PatchAll(typeof(Patch));
            ClientMessengerPatch.Patch(patcher);
            ServerMessengerPatch.Patch(patcher);
            MailboxPatch.Patch(patcher);
            foreach (var patched in patcher.GetPatchedMethods())
                Log("Patched: " + patched.FullDescription());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
        }

        public static void Log(String msg) { pluginInstance.Logger.LogInfo(msg); }
    }
}
