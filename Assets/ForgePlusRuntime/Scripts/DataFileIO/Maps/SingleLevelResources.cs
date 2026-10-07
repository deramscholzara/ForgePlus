using AlephOne;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The resources a level uses from its map's resource fork, as Aleph One finds them, for saving it as level 0 of a map
    // of its own: its chapter screen, terminal pictures, level script (and its texts), end screens, and non-level types.
    public static class SingleLevelResources
    {
        private const short LevelScriptId = 128;
        private const int DefaultEndScreenIndex = 99;
        private const int DefaultEndScreenCount = 1;

        // Empty if it uses none, or null if the map's can't be changed (so it's saved as it is)
        public static byte[] ResourceForkFor(MapsFile mapsFile, MapLevel level, int levelIndex)
        {
            var forks = mapsFile.Forks;
            var resources = forks.ResourceFork;
            if (resources == null || !forks.CanEditResources)
            {
                return null;
            }

            var newIds = new Dictionary<(uint Type, short Id), short>();
            var replacements = new Dictionary<(uint Type, short Id), byte[]>();

            void Keep(uint type, int id, int newId)
            {
                if (id >= short.MinValue && id <= short.MaxValue && newId >= short.MinValue && newId <= short.MaxValue &&
                    resources.Has(type, (short) id) && !forks.IsResourceRemoved(type, (short) id))
                {
                    newIds[(type, (short) id)] = (short) newId;
                }
            }

            void KeepPicture(int id, int newId)
            {
                foreach (var offset in MapsFile.ChapterScreenDepthOffsets)
                {
                    Keep(MacResourceFork.PictType, id + offset, newId + offset);
                    Keep(MacResourceFork.ClutType, id + offset, newId + offset);
                }
            }

            // computer_interface.cpp
            foreach (var terminal in level.map_terminal_text)
            {
                foreach (var group in terminal.groupings)
                {
                    if (group.type == computer_interface._logon_group ||
                        group.type == computer_interface._logoff_group ||
                        group.type == computer_interface._pict_group)
                    {
                        KeepPicture(group.permutation, group.permutation);
                    }
                }
            }

            // XML_LevelScript.cpp
            var endScreenIndex = DefaultEndScreenIndex;
            var endScreenCount = DefaultEndScreenCount;
            var levelScript = forks.IsResourceRemoved(MacResourceFork.TextType, LevelScriptId) ? null : resources.Get(MacResourceFork.TextType, LevelScriptId);

            if (levelScript != null)
            {
                var filteredScript = LevelScriptFor(levelScript, levelIndex, out var texts, out endScreenIndex, out endScreenCount);
                if (filteredScript != null)
                {
                    Keep(MacResourceFork.TextType, LevelScriptId, LevelScriptId);
                    replacements[(MacResourceFork.TextType, LevelScriptId)] = filteredScript;

                    foreach (var text in texts)
                    {
                        Keep(MacResourceFork.TextType, text, text);
                    }
                }
                else
                {
                    Debug.LogWarning("The level script (TEXT 128) couldn't be read, so it's saved with the level as it is, with every text resource.");

                    foreach (var resource in resources.All.Where(resource => resource.Type == MacResourceFork.TextType))
                    {
                        Keep(MacResourceFork.TextType, resource.Id, resource.Id);
                    }
                }
            }

            // Finishing the only level ends the game
            for (var i = 0; i < endScreenCount; i++)
            {
                var id = MapsFile.ChapterScreenBase + endScreenIndex + i;
                KeepPicture(id, id);
                Keep(MacResourceFork.SoundType, id, id);
            }

            var isChapterScreenIdTaken = MapsFile.ChapterScreenDepthOffsets.Any(offset =>
                newIds.Any(kept => kept.Key.Type == MacResourceFork.PictType &&
                                   kept.Value == MapsFile.ChapterScreenBase + offset &&
                                   kept.Key.Id != MapsFile.ChapterScreenBase + levelIndex + offset));

            if (isChapterScreenIdTaken)
            {
                Debug.LogWarning($"Level {levelIndex}'s chapter screen isn't saved with it: its ID as level 0's ({MapsFile.ChapterScreenBase}) is a picture its terminals or the end screens use.");
            }
            else
            {
                KeepPicture(MapsFile.ChapterScreenBase + levelIndex, MapsFile.ChapterScreenBase);
                Keep(MacResourceFork.SoundType, MapsFile.ChapterScreenBase + levelIndex, MapsFile.ChapterScreenBase);
            }

            // Other types (such as 'vers') aren't a level's
            foreach (var resource in resources.All)
            {
                if (resource.Type != MacResourceFork.PictType &&
                    resource.Type != MacResourceFork.ClutType &&
                    resource.Type != MacResourceFork.SoundType &&
                    resource.Type != MacResourceFork.TextType)
                {
                    Keep(resource.Type, resource.Id, resource.Id);
                }
            }

            if (newIds.Count == 0)
            {
                return new byte[0];
            }

            return resources.Rebuild((type, id) => newIds.TryGetValue((type, id), out var newId) ? newId : (short?) null, replacements);
        }

        // Keeps the level's own commands (as level 0's) and those not for a particular level. Null if it can't be read.
        private static byte[] LevelScriptFor(byte[] script, int levelIndex, out HashSet<short> texts, out int endScreenIndex, out int endScreenCount)
        {
            texts = new HashSet<short>();
            endScreenIndex = DefaultEndScreenIndex;
            endScreenCount = DefaultEndScreenCount;

            try
            {
                var endsWithNul = script.Length > 0 && script[script.Length - 1] == 0;
                var document = XDocument.Parse(Encoding.UTF8.GetString(script).TrimEnd('\0'), LoadOptions.PreserveWhitespace);

                var root = document.Root;
                if (root == null || root.Name.LocalName != "marathon_levels")
                {
                    return null;
                }

                var isChanged = false;
                foreach (var levelElement in root.Elements("level").ToList())
                {
                    if (!int.TryParse((string) levelElement.Attribute("index"), out var index) || index != levelIndex)
                    {
                        levelElement.Remove();
                        isChanged = true;
                    }
                    else if (index != 0)
                    {
                        levelElement.SetAttributeValue("index", 0);
                        isChanged = true;
                    }
                }

                foreach (var command in root.Elements().SelectMany(element => element.Elements()))
                {
                    if ((command.Name.LocalName == "mml" || command.Name.LocalName == "lua") &&
                        short.TryParse((string) command.Attribute("resource"), out var resource))
                    {
                        texts.Add(resource);
                    }
                }

                foreach (var endScreens in root.Elements("end_screens"))
                {
                    if (int.TryParse((string) endScreens.Attribute("index"), out var index))
                    {
                        endScreenIndex = index;
                    }

                    if (int.TryParse((string) endScreens.Attribute("count"), out var count))
                    {
                        endScreenCount = count;
                    }
                }

                if (!isChanged)
                {
                    return script;
                }

                var text = (document.Declaration != null ? document.Declaration.ToString() : string.Empty) +
                           document.ToString(SaveOptions.DisableFormatting);
                var bytes = Encoding.UTF8.GetBytes(text);

                return endsWithNul ? bytes.Concat(new byte[] { 0 }).ToArray() : bytes;
            }
            catch (Exception exception) when (exception is XmlException || exception is ArgumentException)
            {
                return null;
            }
        }
    }
}
