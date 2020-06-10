﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_CounterAbsorbSkill : SL_CounterSkill
    {
        public List<AbsorbType> Absorbs;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var skill = item as CounterAbsorbSkill;
            
            if (this.Absorbs != null)
            {
                var list = new List<CounterAbsorb>();
                foreach (var absorbHolder in this.Absorbs)
                {
                    var absorb = new CounterAbsorb()
                    {
                        Condition = new BooleanCondition(),
                        Types = absorbHolder.DamageTypes
                    };

                    absorbHolder.Condition.ApplyToComponent(absorb.Condition);

                    list.Add(absorb);
                }
                skill.Absorbs = list.ToArray();
            }
        }

        public override void SerializeItem(Item item, SL_Item holder)
        {
            base.SerializeItem(item, holder);

            var skill = item as CounterAbsorbSkill;
            var template = holder as SL_CounterAbsorbSkill;

            template.Absorbs = new List<AbsorbType>();
            foreach (var absorb in skill.Absorbs)
            {
                template.Absorbs.Add(new AbsorbType()
                {
                    Condition = absorb.Condition != null ? (SL_BooleanCondition)SL_EffectCondition.ParseCondition(absorb.Condition) : null,
                    DamageTypes = absorb.Types
                });
            }
        }

        [SL_Serialized]
        public class AbsorbType
        {
            public SL_BooleanCondition Condition;
            public List<DamageType.Types> DamageTypes;
        }
    }
}
