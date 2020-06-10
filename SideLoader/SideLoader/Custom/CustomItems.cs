﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Localizer;
using System.Reflection;
using HarmonyLib;

namespace SideLoader
{
    public class CustomItems : MonoBehaviour
    {
        public static CustomItems Instance;

        /// <summary>Cached ORIGINAL Item Prefabs (not modified)</summary>
        private static readonly Dictionary<int, Item> OrigItemPrefabs = new Dictionary<int, Item>();

        /// <summary>Cached ResourcesPrefabManager.ITEM_PREFABS Dictionary</summary>
        public static Dictionary<string, Item> RPM_ITEM_PREFABS;

        /// <summary>Cached LocalizationManager.ItemLocalization</summary>
        public static Dictionary<int, ItemLocalization> ITEM_LOCALIZATION;

        /// <summary>Cached RecipeManager.m_recipes Dictionary</summary>
        public static Dictionary<string, Recipe> ALL_RECIPES;
        /// <summary>Cached RecipeManager.m_recipeUIDsPerUstensils Dictionary</summary>
        public static Dictionary<Recipe.CraftingType, List<UID>> RECIPES_PER_UTENSIL;

        internal void Awake()
        {
            Instance = this;

            // Cache useful dictionaries used by the game
            RPM_ITEM_PREFABS = At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") as Dictionary<string, Item>;
            ITEM_LOCALIZATION = At.GetValue(typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization") as Dictionary<int, ItemLocalization>;

            ALL_RECIPES = At.GetValue(typeof(RecipeManager), RecipeManager.Instance, "m_recipes") as Dictionary<string, Recipe>;
            RECIPES_PER_UTENSIL = At.GetValue(typeof(RecipeManager), RecipeManager.Instance, "m_recipeUIDsPerUstensils") as Dictionary<Recipe.CraftingType, List<UID>>;
        }

        // ====================== Harmony Patches ====================== //

        // fix for the recipe menu, which can break from some custom items when they are an ingredient.
        [HarmonyPatch(typeof(ItemListDisplay), "SortBySupport")]
        public class ItemListDisplay_SortBySupport
        {
            [HarmonyFinalizer]
            public static Exception Finalizer(ref int __result, Exception __exception)
            {
                if (__exception != null)
                {
                    __result = -1;
                }
                return null;
            }
        }

        // fix for ItemDetailsDisplay. Shouldn't really be needed anymore, but leaving it for now.
        [HarmonyPatch(typeof(ItemDetailsDisplay), "RefreshDetail")]
        public class ItemDetailsDisplay_RefreshDetail
        {
            [HarmonyFinalizer]
            public static Exception Finalizer()
            {
                return null;
            }
        }

        // ================================================================================ //
        /*                                  Public Helpers                                  */
        // ================================================================================ //

        /// <summary> Will return the true original prefab for this Item ID. </summary>
        public static Item GetOriginalItemPrefab(int ItemID)
        {
            if (OrigItemPrefabs.ContainsKey(ItemID))
            {
                return OrigItemPrefabs[ItemID];
            }
            else
            {
                return ResourcesPrefabManager.Instance.GetItemPrefab(ItemID);
            }
        }

        /// <summary>
        /// If defining a custom item after SL.OnPacksLoaded, just provide the template, it will automatically be applied.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>Your new custom item (or the original item, if modifying an existing one)</returns>
        public static Item CreateCustomItem(SL_Item template)
        {
            return CreateCustomItem(template.Target_ItemID, template.New_ItemID, template.Name, template);
        }

        /// <summary>
        /// Clones an item prefab and returns the clone to you. Caches the original prefab for other mods or other custom items to reference.
        /// </summary>
        /// <param name="cloneTargetID">The Item ID of the Item you want to clone from</param>
        /// <param name="newID">The new Item ID for your cloned item. Can be the same as the target, will overwrite.</param>
        /// <param name="name">Only used for the gameObject name, not the actual Item Name. This is the name thats used in Debug Menus.</param>
        /// <param name="template">[Optional] If you want to apply a template for this item manually, you can provide it here.</param>
        /// <returns>Your cloned Item prefab</returns>
        public static Item CreateCustomItem(int cloneTargetID, int newID, string name, SL_Item template = null)
        {
            Item original = GetOriginalItemPrefab(cloneTargetID);

            if (!original)
            {
                SL.Log("CustomItems::CreateCustomItem - Error! Could not find the clone target Item ID: " + cloneTargetID, 1);
                return null;
            }

            Item item; 

            // modifying an existing item
            if (newID == cloneTargetID)
            {
                // Modifying the original prefab for the first time. Cache it in case someone else wants the true original.
                if (!OrigItemPrefabs.ContainsKey(newID))
                {
                    var cached = Instantiate(original.gameObject).GetComponent<Item>();
                    cached.gameObject.SetActive(false);
                    DontDestroyOnLoad(cached.gameObject);
                    OrigItemPrefabs.Add(cached.ItemID, cached);
                }

                // apply to the original item prefab. this ensures direct prefab references to this item reflect the changes.
                item = original;
            }
            else // making a new item
            {
                item = Instantiate(original.gameObject).GetComponent<Item>();
                item.gameObject.SetActive(false);
                item.gameObject.name = newID + "_" + name;

                item.ItemID = newID;
                SetItemID(newID, item);

                // fix for name and description localization
                SetNameAndDescription(item, original.Name, original.Description);
            }

            // Do this so that any changes we make are not destroyed on scene changes.
            // This is needed whether this is a clone or a new item.
            DontDestroyOnLoad(item.gameObject);

            // fix for recipes (not sure if needed anymore?)
            if (!item.GetComponent<TagSource>())
            {
                var tags = item.gameObject.AddComponent<TagSource>();
                tags.RefreshTags();
            }

            if (template != null)
            {
                if (SL.PacksLoaded)
                {
                    template.ApplyTemplateToItem();
                }
                else
                {
                    if (template is SL_RecipeItem)
                    {
                        SL.INTERNAL_ApplyRecipeItems += template.ApplyTemplateToItem;
                    }
                    else
                    {
                        SL.INTERNAL_ApplyItems += template.ApplyTemplateToItem;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Fixes the ResourcesPrefabManager.ITEM_PREFABS dictionary for a custom Item ID. Will overwrite if the ID exists.
        /// This is called by CustomItems.CreateCustomItem
        /// </summary>
        /// <param name="_ID">The Item ID you want to set</param>
        /// <param name="item">The Item prefab</param>
        public static void SetItemID(int _ID, Item item)
        {
            //SL.Log("Setting a custom Item ID to the ResourcesPrefabManager dictionary. ID: " + ID + ", item name: " + item.Name);

            var id = _ID.ToString();
            if (RPM_ITEM_PREFABS.ContainsKey(id))
            {
                RPM_ITEM_PREFABS[id] = item;
            }
            else
            {
                RPM_ITEM_PREFABS.Add(id, item);
            }
        }

        /// <summary> Helper for setting an Item's name easily </summary>
        public static void SetName(Item item, string name)
        {
            SetNameAndDescription(item, name, item.Description);
        }

        /// <summary> Helper for setting an Item's description easily </summary>
        public static void SetDescription(Item item, string description)
        {
            SetNameAndDescription(item, item.Name, description);
        }

        /// <summary> Set both name and description. Used by SetName and SetDescription. </summary>
        public static void SetNameAndDescription(Item _item, string _name, string _description)
        {
            var name = _name ?? "";
            var desc = _description ?? "";

            At.SetValue(name, typeof(Item), _item, "m_name");
            At.SetValue(desc, typeof(Item), _item, "m_description");

            if (ITEM_LOCALIZATION.ContainsKey(_item.ItemID))
            {
                ITEM_LOCALIZATION[_item.ItemID].Name = name;
                ITEM_LOCALIZATION[_item.ItemID].Desc = desc;
            }
            else
            {
                ItemLocalization loc = new ItemLocalization(name, desc);
                ITEM_LOCALIZATION.Add(_item.ItemID, loc);
            }
        }

        // ================ TAGS ================ //

        /// <summary>
        /// Gets a tag from a string tag name. Note: This just calls CustomTags.GetTag(tagName, logging).
        /// </summary>
        public static Tag GetTag(string tagName, bool logging = true)
        {
            return CustomTags.GetTag(tagName, logging);
        }

        /// <summary>
        /// Creates a new custom tag. Note: This just calls CustomTags.CreateTag(tagName).
        /// </summary>
        public static void CreateTag(string tagName)
        {
            CustomTags.CreateTag(tagName);
        }

        /// <summary> Adds the range of tags to the Items' TagSource, and optionally destroys the existing tags.</summary>
        public static void SetItemTags(Item item, List<string> tags, bool destroyExisting)
        {
            TagSource tagsource;
            if (destroyExisting && item.GetComponent<TagSource>() is TagSource origTags)
            {
                DestroyImmediate(origTags);
                tagsource = item.gameObject.AddComponent<TagSource>();
            }
            else
            {
                tagsource = item.gameObject.GetComponent<TagSource>();
            }

            var list = new List<TagSourceSelector>();
            foreach (var tag in tags)
            {
                if (GetTag(tag) is Tag _tag && _tag != Tag.None)
                {
                    list.Add(new TagSourceSelector(_tag));
                }
            }

            At.SetValue(list, typeof(TagListSelectorComponent), tagsource as TagListSelectorComponent, "m_tagSelectors");

            tagsource.RefreshTags();

            At.SetValue(tagsource, typeof(Item), item, "m_tagSource");
        }

        [Obsolete("Use SL.DestroyChildren instead! (Moved)")]
        public static void DestroyChildren(Transform t, bool destroyContent = false)
        {
            SL.DestroyChildren(t, destroyContent);
        }
    }
}
