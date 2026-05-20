using GameData;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurstActionBuffer
{
    public sealed class BufferHandler : MonoBehaviour
    {
        public static BufferHandler Instance { get; private set; } = null!;
        public BufferHandler(IntPtr ptr) : base(ptr) { }

        enum BufferAction
        {
            None,
            Reload,
            Swap,
            Push
        }

        private static InventorySlot _bufferSwapSlot;
        private static BufferAction _bufferAction;
        private static float _bufferTime;
        private PlayerAgent? _owner;
        private BWA_Burst? _bwa;

        private static readonly Dictionary<InputAction, InventorySlot> SwapActions = new() {
            { InputAction.SelectStandard, InventorySlot.GearStandard },
            { InputAction.SelectSpecial, InventorySlot.GearSpecial },
            { InputAction.SelectTool, InventorySlot.GearClass },
            { InputAction.SelectMelee, InventorySlot.GearMelee },
            { InputAction.SelectConsumable, InventorySlot.Consumable },
            { InputAction.SelectHackingTool, InventorySlot.HackingTool },
            { InputAction.SelectResourcePack, InventorySlot.ResourcePack },
        };

        private void Awake()
        {
            Instance = this;
            enabled = false;
        }

        public void OnBurstStart(BWA_Burst bwa)
        {
            if (bwa.m_owner == null) return;

            _bwa = bwa;
            _owner = bwa.m_owner;
            enabled = true;
        }

        public void OnBurstEnd()
        {
            AttemptBufferedAction();
            enabled = false;
            _bwa = null;
            _owner = null;
            _bufferAction = BufferAction.None;
            _bufferSwapSlot = InventorySlot.None;
            _bufferTime = 0;
        }

        private void Update()
        {
            if (_owner == null)
            {
                OnBurstEnd();
                return;
            }

            if (InputMapper.GetButtonDown.Invoke(InputAction.Reload, _owner.InputFilter))
            {
                _bufferAction = BufferAction.Reload;
                _bufferTime = Clock.Time;
            }

            foreach (var pair in SwapActions)
            {
                if (InputMapper.GetButtonDown.Invoke(pair.Key, _owner.InputFilter))
                {
                    _bufferSwapSlot = pair.Value;
                    _bufferAction = BufferAction.Swap;
                    _bufferTime = Clock.Time;
                }
            }

            if (InputMapper.GetButtonDown.Invoke(InputAction.Melee, _owner.InputFilter))
            {
                _bufferAction = BufferAction.Push;
                _bufferTime = Clock.Time;
            }
        }

        private void AttemptBufferedAction()
        {
            if (_owner == null || Clock.Time - _bufferTime > Configuration.BufferTime) return;

            var itemHolder = _owner.FPItemHolder;
            if (itemHolder.WieldedItem != _bwa!.m_weapon) return;

            var inventory = _owner.Inventory;
            switch (_bufferAction)
            {
                case BufferAction.Reload:
                    if (!itemHolder.ItemIsBusy && inventory.CanReloadCurrent())
                        inventory.TriggerReload();
                    break;
                case BufferAction.Swap:
                    if (inventory.WieldedSlot != InventorySlot.InLevelCarry && inventory.WieldedSlot != _bufferSwapSlot)
                        _owner.Sync.WantsToWieldSlot(_bufferSwapSlot);
                    break;
                case BufferAction.Push:
                    itemHolder.MeleeAttackShortcut();
                    break;
            }
        }
    }
}
