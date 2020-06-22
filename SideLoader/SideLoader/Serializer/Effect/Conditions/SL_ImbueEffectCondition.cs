﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_ImbueEffectCondition : SL_EffectCondition
    {
        public int ImbuePresetID;
        public bool AnyImbue;
        public Weapon.WeaponSlot WeaponToCheck;

        public override void ApplyToComponent<T>(T component)
        {
            var preset = ResourcesPrefabManager.Instance.GetEffectPreset(this.ImbuePresetID) as ImbueEffectPreset;

            if (!preset && !this.AnyImbue)
            {
                SL.Log("SL_ImbueEffectCondition: Could not get an Imbue Preset with the ID '" + this.ImbuePresetID + "'!", 0);
                return;
            }

            var comp = component as ImbueEffectCondition;

            comp.ImbueEffectPreset = preset;
            comp.AnyImbue = this.AnyImbue;
            comp.WeaponToCheck = this.WeaponToCheck;
        }

        public override void SerializeEffect<T>(EffectCondition component, T template)
        {
            var comp = component as ImbueEffectCondition;
            var holder = template as SL_ImbueEffectCondition;

            holder.AnyImbue = comp.AnyImbue;
            holder.WeaponToCheck = comp.WeaponToCheck;
            holder.ImbuePresetID = comp.ImbueEffectPreset?.PresetID ?? -1;
        }
    }
}