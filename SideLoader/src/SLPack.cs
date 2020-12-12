﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using SideLoader.Helpers;

namespace SideLoader
{
    /// <summary>
    /// Handles internal management of SL Packs (folders which SideLoader will load and apply).
    /// </summary>
    public class SLPack
    {
        // used internally to manage assetbundles
        internal static Dictionary<string, AssetBundle> s_allLoadedAssetBundles = new Dictionary<string, AssetBundle>();

        /// <summary>The FolderName of this SLPack</summary>
        public string Name { get; private set; }

        /// <summary>
        /// Used internally to track where this SL Pack was loaded from.
        /// True = folder is `Outward\Mods\SideLoader\{Name}`. 
        /// False = folder is `Outward\BepInEx\plugins\{Name}\SideLoader\`.
        /// </summary>
        public bool InMainSLFolder = false;

        /// <summary>
        /// Returns the folder path for this SL Pack (relative to Outward directory).
        /// </summary>
        public string FolderPath => InMainSLFolder ?
            $@"{SL.SL_FOLDER}\{Name}" :
            $@"{SL.PLUGINS_FOLDER}\{Name}\SideLoader";

        /// <summary>AssetBundles loaded from the `AssetBundles\` folder. Dictionary Key is the file name.</summary>
        public Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
        /// <summary>Texture2Ds loaded from the PNGs in the `Texture2D\` folder (not from the `Items\...` folders). Dictionary Key is the file name (without ".png")</summary>
        public Dictionary<string, Texture2D> Texture2D = new Dictionary<string, Texture2D>();
        /// <summary>AudioClips loaded from the WAV files in the `AudioClip\` folder. Dictionary Key is the file name (without ".wav")</summary>
        public Dictionary<string, AudioClip> AudioClips = new Dictionary<string, AudioClip>();
        
        /// <summary>SL_Characters loaded from the `Characters\` folder. Dictionary Key is the SL_Character.UID value.</summary>
        public Dictionary<string, SL_Character> CharacterTemplates = new Dictionary<string, SL_Character>();

        /// <summary>
        /// The supported sub-folders in an SL Pack. 
        /// </summary>
        public enum SubFolders
        {
            AudioClip,
            AssetBundles,
            Characters,
            Enchantments,
            Items,
            Recipes,
            StatusEffects,
            Texture2D,
        }

        /// <summary>
        /// Returns the full (relative to the Outward folder) path for the specified subfolder, for this SLPack. Eg, "Mods/SideLoader/SLPACKNAME/SubFolder"
        /// </summary>
        /// <param name="subFolder">The SubFolder you want the path for</param>
        public string GetSubfolderPath(SubFolders subFolder)
        {
            return $@"{this.FolderPath}\{subFolder}";
        }

        /// <summary>
        /// Safely tries to load an SLPack with the provided name, either in the Mods\SideLoader\ folder or the BepInEx\plugins\ folder.
        /// </summary>
        /// <param name="name">The name of the SLPack folder.</param>
        /// <param name="inMainFolder">Is it in the Mods\SideLoader\ directory? (If not, it should be in BepInEx\plugins\)</param>
        /// <param name="hotReload">Is this a hot reload?</param>
        public static void TryLoadPack(string name, bool inMainFolder, bool hotReload)
        {
            try
            {
                if (SL.Packs.ContainsKey(name))
                {
                    SL.LogError($"ERROR: An SLPack already exists with the name '{name}'! Please use a unique name.");
                    return;
                }

                var pack = LoadFromFolder(name, inMainFolder, hotReload);
                SL.Packs.Add(pack.Name, pack);
            }
            catch (Exception e)
            {
                SL.LogError("Error loading SLPack from folder: " + name + "\r\nMessage: " + e.Message + "\r\nStackTrace: " + e.StackTrace);
            }
        }

        /// <summary>
        /// Loads all the assets from the specified SLPack name. Not for calling directly, just place your pack in the SideLoader folder and use SL.Packs["Folder"]
        /// </summary>
        /// <param name="name">The name of the SideLoader pack (ie. the name of the folder inside Mods/SideLoader/)</param>
        /// <param name="inMainSLFolder">Is the SLPack in Mods\SideLoader? If not, it should be Mods\ModName\SideLoader\ structure.</param>
        /// <param name="hotReload">Is this a hot reload?</param>
        private static SLPack LoadFromFolder(string name, bool inMainSLFolder, bool hotReload)
        {
            var pack = new SLPack()
            {
                Name = name,
                InMainSLFolder = inMainSLFolder
            };

            SL.Log("Reading SLPack " + pack.Name);

            // order is somewhat important.
            pack.LoadAssetBundles();
            pack.LoadAudioClips();
            pack.LoadTexture2D();

            pack.LoadCustomStatuses();

            pack.LoadCustomItems();

            pack.LoadRecipes();

            if (!hotReload)
            {
                pack.LoadCharacters();
                pack.LoadEnchantments();
            }

            return pack;
        }

        private void LoadAssetBundles()
        {
            var dir = GetSubfolderPath(SubFolders.AssetBundles);
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (var bundlePath in Directory.GetFiles(GetSubfolderPath(SubFolders.AssetBundles))
                                                .Where(x => !x.EndsWith(".meta") 
                                                         && !x.EndsWith(".manifest")))
            {
                try
                {
                    if (s_allLoadedAssetBundles.ContainsKey(bundlePath))
                    {
                        if (s_allLoadedAssetBundles[bundlePath])
                            s_allLoadedAssetBundles[bundlePath].Unload(true);

                        s_allLoadedAssetBundles.Remove(bundlePath);
                    }

                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle is AssetBundle)
                    {
                        string name = Path.GetFileName(bundlePath);
                        AssetBundles.Add(name, bundle);
                        s_allLoadedAssetBundles.Add(bundlePath, bundle);
                        SL.Log("Loaded assetbundle " + name);
                    }
                    else
                        throw new Exception($"Unknown error (Bundle '{Path.GetFileName(bundlePath)}' was null)");
                }
                catch (Exception e)
                {
                    SL.LogError("Error loading asset bundle! Message: " + e.Message + "\r\nStack: " + e.StackTrace);
                }
            }
        }

        private void LoadAudioClips()
        {
            var dir = GetSubfolderPath(SubFolders.AudioClip);
            if (!Directory.Exists(dir))
                return;

            foreach (var clipPath in Directory.GetFiles(dir, "*.wav"))
            {
                SLPlugin.Instance.StartCoroutine(CustomAudio.LoadClip(clipPath, this));
            }
        }

        // Note: Does NOT load Pngs from the CustomItems/*/Textures/ folders
        // That is done on CustomItem.ApplyTemplateToItem, those textures are not stored in the dictionary.
        private void LoadTexture2D()
        {
            if (!Directory.Exists(GetSubfolderPath(SubFolders.Texture2D)))
                return;

            foreach (var texPath in Directory.GetFiles(GetSubfolderPath(SubFolders.Texture2D)))
            {
                var texture = CustomTextures.LoadTexture(texPath, false, false);
                var name = Path.GetFileNameWithoutExtension(texPath);

                // add to the Texture2D dict for this pack
                Texture2D.Add(name, texture);

                // add to the global Tex replacements dict
                if (CustomTextures.Textures.ContainsKey(name))
                {
                    SL.Log("CustomTextures: A Texture already exists in the global list called " + name + "! Overwriting with this one...");
                    CustomTextures.Textures[name] = texture;
                }
                else
                    CustomTextures.Textures.Add(name, texture);
            }
        }

        private void LoadCustomStatuses()
        {
            var dir = GetSubfolderPath(SubFolders.StatusEffects);
            if (!Directory.Exists(dir))
                return;

            // Key: Filepath, Value: Subfolder name (if any)
            var dict = new Dictionary<string, string>();

            // get basic template xmls
            foreach (var path in Directory.GetFiles(dir, "*.xml"))
                dict.Add(path, "");

            // get subfolder-per-status
            foreach (var folder in Directory.GetDirectories(dir))
            {
                // get the xml inside this folder
                foreach (string path in Directory.GetFiles(folder, "*.xml"))
                    dict.Add(path, Path.GetFileName(folder));
            }

            // apply templates
            foreach (var entry in dict)
            {
                var template = Serializer.LoadFromXml(entry.Key);

                if (template is SL_StatusEffect statusTemplate)
                {
                    CustomStatusEffects.CreateCustomStatus(statusTemplate);
                    statusTemplate.SLPackName = Name;
                    statusTemplate.SubfolderName = entry.Value;
                }
                else if (template is SL_ImbueEffect imbueTemplate)
                {
                    CustomStatusEffects.CreateCustomImbue(imbueTemplate);
                    imbueTemplate.SLPackName = Name;
                    imbueTemplate.SubfolderName = entry.Value;
                }
                else
                {
                    SL.LogError("Unrecognized status effect template: " + entry.Key);
                }
            }
        }

        private void LoadCustomItems()
        {
            var itemsfolder = GetSubfolderPath(SubFolders.Items);

            if (Directory.Exists(itemsfolder))
            {
                // ******** Build the list of template xml paths ******** //

                // Key: full Template filepath, Value:SubFolder name (if any)
                var templates = new Dictionary<string, string>(); 

                // get basic xml templates in the Items folder
                foreach (var path in Directory.GetFiles(itemsfolder, "*.xml"))
                    templates.Add(path, "");

                // check for subfolders (items which are using custom texture pngs)
                foreach (var folder in Directory.GetDirectories(itemsfolder))
                {
                    if (Path.GetFileName(folder) == "TextureBundles")
                    {
                        // folder used to load bulk textures for items, continue for now
                        continue;
                    }

                    //SL.Log("Parsing CustomItem subfolder: " + Path.GetFileName(folder));

                    foreach (string path in Directory.GetFiles(folder, "*.xml"))
                        templates.Add(path, Path.GetFileName(folder));
                }

                // ******** Serialize and prepare each template (does not apply the template, but does clone the base prefab) ******** //

                foreach (var entry in templates)
                {
                    try
                    {
                        // load the ItemHolder template and set the pack/folder info
                        var holder = Serializer.LoadFromXml(entry.Key);

                        var list = new List<SL_Item>();

                        if (holder is SL_Item)
                            list.Add(holder as SL_Item);
                        else
                            list.AddRange((holder as SL_MultiItem).Items);

                        foreach (var itemHolder in list)
                        {
                            itemHolder.SubfolderName = entry.Value;
                            itemHolder.SLPackName = Name;

                            // Clone the target item. This also adds a callback for itemHolder.ApplyTemplateToItem
                            var item = CustomItems.CreateCustomItem(itemHolder);
                        }
                    }
                    catch (Exception e)
                    {
                        SL.Log("LoadFromFolder: Error creating custom item! " + e.GetType() + ", " + e.Message);
                        while (e != null)
                        {
                            SL.Log(e.ToString());
                            e = e.InnerException;
                        }
                    }
                }
            }
        }

        public void TryApplyItemTextureBundles()
        {
            var itemsFolder = this.GetSubfolderPath(SubFolders.Items);
            var bundlesFolder = $@"{itemsFolder}\TextureBundles";
            if (Directory.Exists(bundlesFolder))
            {
                foreach (var file in Directory.GetFiles(bundlesFolder))
                {
                    if (AssetBundle.LoadFromFile(file) is AssetBundle bundle)
                    {
                        CustomItemVisuals.ApplyTexturesFromAssetBundle(bundle);
                    }
                }
            }
        }

        private void LoadRecipes()
        {
            var path = GetSubfolderPath(SubFolders.Recipes);

            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var recipePath in Directory.GetFiles(path))
            {
                if (Serializer.LoadFromXml(recipePath) is SL_Recipe recipeHolder)
                {
                    SL.INTERNAL_ApplyRecipes += recipeHolder.ApplyRecipe;
                }
            }
        }

        private void LoadCharacters()
        {
            var path = GetSubfolderPath(SubFolders.Characters);

            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(path))
            {
                if (Serializer.LoadFromXml(filePath) is SL_Character template)
                {
                    SL.Log("Serialized SL_Character '" + template.Name + "'");

                    CharacterTemplates.Add(template.UID, template);

                    template.Prepare();
                }
            }
        }

        private void LoadEnchantments()
        {
            var dir = GetSubfolderPath(SubFolders.Enchantments);
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(GetSubfolderPath(SubFolders.Enchantments)))
            {
                try
                {
                    if (Serializer.LoadFromXml(filePath) is SL_EnchantmentRecipe template)
                    {
                        template.Apply();
                    }
                }
                catch
                {
                    SL.Log($"Exception loading Enchantment from {filePath}!");
                }
            }
        }
    }
}