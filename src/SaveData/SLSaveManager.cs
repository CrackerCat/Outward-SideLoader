﻿using System;
using System.IO;

namespace SideLoader.SaveData
{
    public class SLSaveManager
    {
        internal const string SAVEDATA_FOLDER = SL.SL_FOLDER + @"\_SAVEDATA";

        internal const string CHARACTERS_FOLDER = "Characters";
        internal const string ITEMSPAWNS_FOLDER = "ItemSpawns";
        internal const string CUSTOM_FOLDER = "Custom";

        public static string GetSaveFolderForWorldHost()
        {
            var player = CharacterManager.Instance.GetFirstLocalCharacter();
            var host = CharacterManager.Instance.GetWorldHostCharacter();

            if (!player || !host || player != host)
                return null;

            return GetSaveFolderForCharacter(player);
        }

        public static string GetSaveFolderForCharacter(Character character)
            => GetSaveFolderForCharacter(character.UID);

        public static string GetSaveFolderForCharacter(string UID)
        {
            var ret = $@"{SAVEDATA_FOLDER}\{UID}";

            // Create the base folder structure for this player character (does nothing if already exists)
            Directory.CreateDirectory(ret);
            Directory.CreateDirectory(ret + $@"\{CHARACTERS_FOLDER}");
            Directory.CreateDirectory(ret + $@"\{ITEMSPAWNS_FOLDER}");
            Directory.CreateDirectory(ret + $@"\{CUSTOM_FOLDER}");

            return ret;
        }

        // ~~~~~~~~~ Core internal ~~~~~~~~~

        internal static void OnSaveInstanceSave(SaveInstance __instance)
        {
            try
            {
                if (__instance.CharSave == null || string.IsNullOrEmpty(__instance.CharSave.CharacterUID))
                    return;

                var charUID = __instance.CharSave.CharacterUID;

                bool isHost = !PhotonNetwork.isNonMasterClientInRoom && !(bool)At.GetField(NetworkLevelLoader.Instance, "m_saveOnHostLost")
                                && CharacterManager.Instance?.GetWorldHostCharacter()?.UID == charUID;

                // Save internal stuff for the host
                if (isHost)
                {
                    SLCharacterSaveManager.SaveCharacters();
                    SLItemSpawnSaveManager.SaveItemSpawns();
                }

                // Save custom extensions from other mods
                PlayerSaveExtension.SaveAllExtensions(CharacterManager.Instance.GetCharacter(charUID), isHost);
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception on SaveInstance.Save!");
                SL.LogInnerException(ex);
            }
        }

        internal static void OnEnvironmentSaveLoaded(DictionaryExt<string, CharacterSaveInstanceHolder> charSaves)
        {
            var host = CharacterManager.Instance.GetWorldHostCharacter();
            if (!host || !host.IsPhotonPlayerLocal)
            {
                SL.WasLastSceneReset = false;
                return;
            }

            if (charSaves.TryGetValue(host.UID, out CharacterSaveInstanceHolder holder))
            {
                if (At.GetField(holder.CurrentSaveInstance, "m_loadedScene") is EnvironmentSave loadedScene)
                {
                    var area = (AreaManager.AreaEnum)AreaManager.Instance.GetAreaFromSceneName(loadedScene.AreaName).ID;
                    if (IsPermanent(area))
                        SLCharacterSaveManager.SceneResetWanted = false;
                    else
                    {
                        float age = (float)(loadedScene.GameTime - EnvironmentConditions.GameTime);
                        SLCharacterSaveManager.SceneResetWanted = AreaManager.Instance.IsAreaExpired(loadedScene.AreaName, age);
                    }
                }
                else
                    SLCharacterSaveManager.SceneResetWanted = true;
            }
            else
                SLCharacterSaveManager.SceneResetWanted = true;

            SL.WasLastSceneReset = SLCharacterSaveManager.SceneResetWanted;

            SL.Log("Set SceneResetWanted: " + SLCharacterSaveManager.SceneResetWanted);
        }

        internal static bool IsPermanent(AreaManager.AreaEnum area)
        {
            var perms = AreaManager.Instance.PermenantAreas;
            foreach (var a in perms)
            {
                if (area == a)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
