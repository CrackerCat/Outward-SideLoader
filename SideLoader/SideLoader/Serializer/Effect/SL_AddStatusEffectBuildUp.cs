﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SideLoader
{
    public class SL_AddStatusEffectBuildUp : SL_Effect
    {
        public string StatusEffect = "";
        public float Buildup;

        public override void ApplyToComponent<T>(T component)
        {
            var status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.StatusEffect);

            if (!status)
            {
                SL.Log("Error getting status effect on AddStatusEffectBuildupHolder. Could not find " + this.StatusEffect);
                return;
            }

            (component as AddStatusEffectBuildUp).Status = status;
            (component as AddStatusEffectBuildUp).BuildUpValue = this.Buildup;
        }

        public override void SerializeEffect<T>(T effect, SL_Effect holder)
        {
            var addStatusEffectBuildUp = effect as AddStatusEffectBuildUp;
            var addStatusEffectBuildupHolder = holder as SL_AddStatusEffectBuildUp;

            if (addStatusEffectBuildUp.Status != null)
            {
                addStatusEffectBuildupHolder.StatusEffect = addStatusEffectBuildUp.Status.IdentifierName;
                addStatusEffectBuildupHolder.Buildup = addStatusEffectBuildUp.BuildUpValue;
            }
        }
    }
}
