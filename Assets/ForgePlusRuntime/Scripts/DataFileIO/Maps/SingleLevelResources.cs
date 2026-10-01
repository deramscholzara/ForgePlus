using AlephOne;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The resources a level uses from its map file's resource fork, as Aleph One finds them, for saving the level as a
    // map file of its own (where it's level 0):
    // - its chapter screen (interface.cpp, try_and_display_chapter_screen): the picture 1500 + its index (and that
    //   picture's 16-bit and 32-bit versions, 10000 and 20000 more), with the color table and sound of the same ID,
    //   renumbered for level 0;
    // - its terminals' pictures (computer_interface.cpp): each logon, logoff and picture group's permutation, at each bit
    //   depth, with its color table;
    // - the level script (XML_LevelScript.cpp, TEXT 128), with only what applies to it (its own level's commands, as
    //   level 0's, and those for every level), and the texts its MML and Lua commands load;
    // - the end screens (shown when the game ends, as finishing the only level does): the chapter screens from the
    //   level script's end_screens index (99), for its count (1);
    // - every resource of another type (such as 'vers', the file's version), which isn't a level's.
    // A level's embedded physics are in its own wad, so they're saved with it anyway.
    public static class SingleLevelResources
    {
        private static readonly uint PictType = FourCharacterCode("PICT");
        private static readonly uint ClutType = FourCharacterCode("clut");
        private static readonly uint SoundType = FourCharacterCode("snd ");
        private static readonly uint TextType = FourCharacterCode("TEXT");

        private const short ChapterScreenBase = 1500;
        private const short LevelScriptId = 128;
        private const int DefaultEndScreenIndex = 99;
        private const int DefaultEndScreenCount = 1;

        // A picture's ID at each bit depth (images.cpp: determine_pict_resource_id)
        private static readonly int[] DepthOffsets = { 0, 10000, 20000 };

        // The resource fork to save with the level (empty, if it uses none), or null if the map's can't be changed
        // (so it's saved as it is)
        public static byte[] ResourceForkFor(MapsFile mapsFile, MapLevel level, int levelIndex)
        {
            var forks = mapsFile.Forks;
            var resources = forks.ResourceFork;
            if (resources == null || !forks.CanEditResources)
            {
                return null;
            }

            // Each kept resource's new ID, by its type and ID
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
                foreach (var offset in DepthOffsets)
                {
                    Keep(PictType, id + offset, newId + offset);
                    Keep(ClutType, id + offset, newId + offset);
                }
            }

            // Its terminals' pictures
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

            // The level script, and the texts it loads
            var endScreenIndex = DefaultEndScreenIndex;
            var endScreenCount = DefaultEndScreenCount;
            var levelScript = forks.IsResourceRemoved(TextType, LevelScriptId) ? null : resources.Get(TextType, LevelScriptId);

            if (levelScript != null)
            {
                var levelsScript = LevelScriptFor(levelScript, levelIndex, out var texts, out endScreenIndex, out endScreenCount);
                if (levelsScript != null)
                {
                    Keep(TextType, LevelScriptId, LevelScriptId);
                    replacements[(TextType, LevelScriptId)] = levelsScript;

                    foreach (var text in texts)
                    {
                        Keep(TextType, text, text);
                    }
                }
                else
                {
                    // It couldn't be read, so it's kept as it is, with every text it might load
                    Debug.LogWarning("The level script (TEXT 128) couldn't be read, so it's saved with the level as it is, with every text resource.");

                    foreach (var resource in resources.All.Where(resource => resource.Type == TextType))
                    {
                        Keep(TextType, resource.Id, resource.Id);
                    }
                }
            }

            // The end screens
            for (var i = 0; i < endScreenCount; i++)
            {
                var id = ChapterScreenBase + endScreenIndex + i;
                KeepPicture(id, id);
                Keep(SoundType, id, id);
            }

            // Its chapter screen, as level 0's, unless one of the pictures kept above already has its ID
            var isChapterScreenIdTaken = DepthOffsets.Any(offset =>
                newIds.Any(kept => kept.Key.Type == PictType && kept.Value == ChapterScreenBase + offset && kept.Key.Id != ChapterScreenBase + levelIndex + offset));

            if (isChapterScreenIdTaken)
            {
                Debug.LogWarning($"Level {levelIndex}'s chapter screen isn't saved with it: its ID as level 0's ({ChapterScreenBase}) is a picture its terminals or the end screens use.");
            }
            else
            {
                KeepPicture(ChapterScreenBase + levelIndex, ChapterScreenBase);
                Keep(SoundType, ChapterScreenBase + levelIndex, ChapterScreenBase);
            }

            // Every resource of another type
            foreach (var resource in resources.All)
            {
                if (resource.Type != PictType && resource.Type != ClutType && resource.Type != SoundType && resource.Type != TextType)
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

        // The level script with only what applies to the level: its own level's commands (as level 0's), and the
        // commands for every level, the end and restoring (and the end screens' settings). Null if it can't be read.
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
            catch (Exception exception) when (exception is System.Xml.XmlException || exception is ArgumentException)
            {
                return null;
            }
        }

        private static uint FourCharacterCode(string code)
        {
            return (uint) ((code[0] << 24) | (code[1] << 16) | (code[2] << 8) | code[3]);
        }
    }
}
