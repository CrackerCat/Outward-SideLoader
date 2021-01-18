﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SideLoader.Helpers;
using UnityEngine;

namespace SideLoader
{
    public class SL_Skill : SL_Item
    {
        internal static Dictionary<int, Skill> s_customSkills = new Dictionary<int, Skill>();

        public float? Cooldown;
        public float? StaminaCost;
        public float? ManaCost;
        public float? DurabilityCost;
        public float? DurabilityCostPercent;

        public bool? VFXOnStart;
        public bool? StopStartVFXOnEnd;
        public SL_PlayVFX.VFXPrefabs? StartVFX;

        public SkillItemReq[] RequiredItems;

        public float? HealthCost;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var skill = item as Skill;

            if (this.Cooldown != null)
                skill.Cooldown = (float)this.Cooldown;

            if (this.ManaCost != null)
                skill.ManaCost = (float)this.ManaCost;

            if (this.StaminaCost != null)
                skill.StaminaCost = (float)this.StaminaCost;

            if (this.DurabilityCost != null)
                skill.DurabilityCost = (float)this.DurabilityCost;

            if (this.DurabilityCostPercent != null)
                skill.DurabilityCostPercent = (float)this.DurabilityCostPercent;

            if (this.VFXOnStart != null)
                skill.VFXOnStart = (bool)this.VFXOnStart;

            if (this.StopStartVFXOnEnd != null)
                skill.StopVFX = (bool)this.StopStartVFXOnEnd;

            if (this.HealthCost != null)
                skill.HealthCost = (float)this.HealthCost;

            if (this.StartVFX != null)
            {
                if (this.StartVFX == SL_PlayVFX.VFXPrefabs.NONE)
                    skill.StartVFX = null;
                else
                {
                    var prefab = SL_PlayVFX.GetVfxSystem((SL_PlayVFX.VFXPrefabs)this.StartVFX);
                    var copy = GameObject.Instantiate(prefab);
                    GameObject.DontDestroyOnLoad(copy);
                    copy.SetActive(false);
                    skill.StartVFX = copy.GetComponent<VFXSystem>();
                }
            }

            if (this.RequiredItems != null)
            {
                var list = new List<Skill.ItemRequired>();
                foreach (var req in this.RequiredItems)
                {
                    if (ResourcesPrefabManager.Instance.GetItemPrefab(req.ItemID) is Item reqItem)
                    {
                        list.Add(new Skill.ItemRequired()
                        {
                            Item = reqItem,
                            Consume = req.Consume,
                            Quantity = req.Quantity
                        });
                    }
                }
                skill.RequiredItems = list.ToArray();
            }

            var activationConditions = new List<Skill.ActivationCondition>();

            if (skill.transform.childCount > 0)
            {
                foreach (Transform child in skill.transform)
                {
                    if (child.name.Contains("Activation"))
                    {
                        foreach (var condition in child.GetComponentsInChildren<EffectCondition>())
                        {
                            var skillCondition = new Skill.ActivationCondition
                            {
                                Condition = condition
                            };

                            // Todo: do this properly. There's no easy way to get the right loc.
                            string msgLoc;
                            if (condition is WindAltarActivatedCondition)
                                msgLoc = "Notification_Skill_WindAltarRequired";
                            else
                                msgLoc = "Notification_Skill_RequirementsNotMet";

                            At.SetField(skillCondition, "m_messageLocKey", msgLoc);

                            activationConditions.Add(skillCondition);
                        }
                    }
                }
            }

            At.SetField(skill, "m_additionalConditions", activationConditions.ToArray());

            // Add to internal dictionary of custom skills (for F3 menu fix)
            if (s_customSkills.ContainsKey(skill.ItemID))
                s_customSkills[skill.ItemID] = skill;
            else
                s_customSkills.Add(skill.ItemID, skill);
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var skill = item as Skill;

            Cooldown = skill.Cooldown;
            StaminaCost = skill.StaminaCost;
            ManaCost = skill.ManaCost;
            DurabilityCost = skill.DurabilityCost;
            DurabilityCostPercent = skill.DurabilityCostPercent;

            VFXOnStart = skill.VFXOnStart;
            StopStartVFXOnEnd = skill.StopVFX;

            if (skill.StartVFX)
                StartVFX = SL_PlayVFX.GetVFXSystemEnum(skill.StartVFX);

            if (skill.RequiredItems != null)
            {
                var list = new List<SkillItemReq>();

                foreach (Skill.ItemRequired itemReq in skill.RequiredItems)
                {
                    if (itemReq.Item != null)
                    {
                        list.Add(new SkillItemReq
                        {
                            ItemID = itemReq.Item.ItemID,
                            Consume = itemReq.Consume,
                            Quantity = itemReq.Quantity
                        });
                    }
                }

                RequiredItems = list.ToArray();
            }
        }

        [SL_Serialized]
        public class SkillItemReq
        {
            public int ItemID;
            public int Quantity;
            public bool Consume;
        }
    }
}
