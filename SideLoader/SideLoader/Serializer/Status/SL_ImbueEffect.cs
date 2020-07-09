﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_ImbueEffect
    {
        /// <summary> [NOT SERIALIZED] The name of the SLPack this custom item template comes from (or is using).
        /// If defining from C#, you can set this to the name of the pack you want to load assets from.</summary>
        [XmlIgnore]
        public string SLPackName;
        /// <summary> [NOT SERIALIZED] The name of the folder this custom item is using for textures (MyPack/Items/[SubfolderName]/Textures/).</summary>
        [XmlIgnore]
        public string SubfolderName;

        /// <summary>This is the Preset ID of the Status Effect you want to base from.</summary>
        public int TargetStatusID;
        /// <summary>The new Preset ID for your Status Effect</summary>
        public int NewStatusID;

        public string Name;
        public string Description;

        public EffectBehaviours EffectBehaviour = EffectBehaviours.OverrideEffects;
        public List<SL_EffectTransform> Effects;

        public void ApplyTemplate()
        {
            var preset = (ImbueEffectPreset)ResourcesPrefabManager.Instance.GetEffectPreset(this.NewStatusID);
            if (!preset)
            {
                SL.Log($"Could not find an Imbue Effect with the ID {NewStatusID}! Make sure you've called CustomStatusEffects.CreateCustomImbue first!", 1);
                return;
            }

            CustomStatusEffects.SetImbueLocalization(preset, Name, Description);

            // check for custom icon
            if (!string.IsNullOrEmpty(SLPackName) && !string.IsNullOrEmpty(SubfolderName) && SL.Packs[SLPackName] is SLPack pack)
            {
                var path = $@"{pack.GetSubfolderPath(SLPack.SubFolders.StatusEffects)}\{SubfolderName}\icon.png";

                if (File.Exists(path))
                {
                    var tex = CustomTextures.LoadTexture(path, false, false);
                    var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.NONE);

                    preset.ImbueStatusIcon = sprite;
                }
            }

            SL_EffectTransform.ApplyTransformList(preset.transform, Effects, EffectBehaviour);
        }

        public static SL_ImbueEffect ParseImbueEffect(ImbueEffectPreset imbue)
        {
            var template = new SL_ImbueEffect
            {
                TargetStatusID = imbue.PresetID,
                Name = imbue.Name,
                Description = imbue.Description
            };

            //CustomStatusEffects.GetImbueLocalization(imbue, out template.Name, out template.Description);

            template.Effects = new List<SL_EffectTransform>();
            foreach (Transform child in imbue.transform)
            {
                var effectsChild = SL_EffectTransform.ParseTransform(child);

                if (effectsChild.ChildEffects.Count > 0 || effectsChild.Effects.Count > 0 || effectsChild.EffectConditions.Count > 0)
                {
                    template.Effects.Add(effectsChild);
                }
            }

            return template;
        }
    }
}
