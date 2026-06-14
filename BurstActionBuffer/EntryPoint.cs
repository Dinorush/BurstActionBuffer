using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace BurstActionBuffer
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.1.1")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "BurstActionBuffer";

        public override void Load()
        {
            Configuration.Init();
            new Harmony(MODNAME).PatchAll();

            AssetAPI.OnStartupAssetsLoaded += () =>
            {
                ClassInjector.RegisterTypeInIl2Cpp<BufferHandler>();
                var go = new GameObject(MODNAME);
                GameObject.DontDestroyOnLoad(go);
                go.AddComponent<BufferHandler>();
            };

            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}