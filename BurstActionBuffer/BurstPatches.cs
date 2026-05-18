using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gear;
using HarmonyLib;
using Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BurstActionBuffer
{
    [HarmonyPatch]
    internal static class BurstPatches
    {
        enum BufferAction
        {
            None,
            Reload,
            Swap,
            Push
        }
        private static Coroutine? _bufferRoutine;
        private static InventorySlot _bufferSwapSlot;
        private static BufferAction _bufferAction;
        private static float _bufferTime;

        private static readonly Dictionary<InputAction, InventorySlot> SwapActions = new() {
            { InputAction.SelectStandard, InventorySlot.GearStandard },
            { InputAction.SelectSpecial, InventorySlot.GearSpecial },
            { InputAction.SelectTool, InventorySlot.GearClass },
            { InputAction.SelectMelee, InventorySlot.GearMelee },
            { InputAction.SelectConsumable, InventorySlot.Consumable },
            { InputAction.SelectHackingTool, InventorySlot.HackingTool },
            { InputAction.SelectResourcePack, InventorySlot.ResourcePack },
        };

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStartFiring))]
        [HarmonyPostfix]
        private static void Post_StartBurstFire(BWA_Burst __instance)
        {
            _bufferRoutine = CoroutineManager.StartCoroutine(SwapBuffer(__instance.m_owner).WrapToIl2Cpp());
        }

        private static IEnumerator SwapBuffer(PlayerAgent? owner)
        {
            // Check for swap attempts until we can swap again
            while (owner != null)
            {
                if (InputMapper.GetButtonDown.Invoke(InputAction.Reload, owner.InputFilter))
                {
                    _bufferAction = BufferAction.Reload;
                    _bufferTime = Clock.Time;
                }

                foreach (var pair in SwapActions)
                {
                    if (InputMapper.GetButtonDown.Invoke(pair.Key, owner.InputFilter))
                    {
                        _bufferSwapSlot = pair.Value;
                        _bufferAction = BufferAction.Swap;
                        _bufferTime = Clock.Time;
                    }
                }

                if (InputMapper.GetButtonDown.Invoke(InputAction.Melee, owner.InputFilter))
                {
                    _bufferAction = BufferAction.Push;
                    _bufferTime = Clock.Time;
                }

                yield return null;
            }

            ClearBuffer();
        }

        [HarmonyPatch(typeof(BWA_Burst), nameof(BWA_Burst.OnStopFiring))]
        [HarmonyPostfix]
        private static void Post_EndBurstFire(BWA_Burst __instance)
        {
            TryBufferedAction(__instance);
            ClearBuffer();
        }

        private static void TryBufferedAction(BWA_Burst __instance)
        {
            if (Clock.Time - _bufferTime > Configuration.BufferTime) return;

            var owner = __instance.m_owner;
            if (owner == null) return;

            var itemHolder = owner.FPItemHolder;
            if (itemHolder.WieldedItem != __instance.m_weapon) return;

            var inventory = owner.Inventory;
            switch (_bufferAction)
            {
                case BufferAction.Reload:
                    if (!itemHolder.ItemIsBusy && inventory.CanReloadCurrent())
                        inventory.TriggerReload();
                    break;
                case BufferAction.Swap:
                    if (inventory.WieldedSlot != InventorySlot.InLevelCarry && inventory.WieldedSlot != _bufferSwapSlot)
                        owner.Sync.WantsToWieldSlot(_bufferSwapSlot);
                    break;
                case BufferAction.Push:
                    itemHolder.MeleeAttackShortcut();
                    break;
            }
        }

        private static void ClearBuffer()
        {
            _bufferAction = BufferAction.None;
            _bufferSwapSlot = InventorySlot.None;
            _bufferTime = 0;
            if (_bufferRoutine != null)
                CoroutineManager.StopCoroutine(_bufferRoutine);
            _bufferRoutine = null;
        }
    }
}
