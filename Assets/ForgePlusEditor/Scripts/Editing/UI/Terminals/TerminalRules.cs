using AlephOne;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using System.Collections.Generic;
using System.Linq;
using static AlephOne.computer_interface;

namespace ForgePlus.UI
{
    // How the games read a terminal's groups (computer_interface.cpp), and what keeps them from reading it right. A
    // terminal is entered at its first #UNFINISHED, #SUCCESS or #FAILURE group (by the mission's state, the last two
    // falling back to #UNFINISHED: next_terminal_group), and read on until a group ends it or its groups run out.
    public static class TerminalRules
    {
        // interface.cpp: EPILOGUE_LEVEL_NUMBER, which ends the game (as Marathon Infinity's last terminals use it)
        private const short EpilogueLevel = 256;

        private static readonly short[] SectionStartTypes = { _unfinished_group, _success_group, _failure_group };

        public enum GroupKind
        {
            // Not shown
            SectionStart,
            // The logo, as the terminal is logged on and off
            Log,
            // A page of text or a picture (or static, or a camera's blank page)
            Body,
            // Done between pages (a sound played, a tag activated)
            Action,
            // Leaving the terminal (or the level)
            Ending,
            // Never made to work: the player is left in the terminal, which no key moves on from or leaves
            Stranding,
        }

        // What an edit mustn't add: a terminal that isn't read, groups that can't be reached, or a stranding group
        public enum Blocker
        {
            NoEntry,
            Unreachable,
            Stranding,
        }

        public class Reading
        {
            // Without an #UNFINISHED group (every state's fallback), the terminal isn't read at all
            public bool HasEntry;

            // The groups read (or used as a section's start)
            public readonly HashSet<int> Reached = new HashSet<int>();

            // The groups never read that would show or do something (an unread #END, such as one after a teleport, as
            // Bungie's terminals have them, does neither)
            public readonly List<int> Unreachable = new List<int>();

            // Section starts that a section is read on into (the game shows a blank page for them, then goes on), by
            // whether they're where their own section starts (the first of their type)
            public readonly List<int> SectionStartsReadInto = new List<int>();
            public readonly List<int> UnusedSectionStartsReadInto = new List<int>();

            public readonly List<int> StrandingGroups = new List<int>();

            // The first group of each section start type (or -1)
            public readonly Dictionary<short, int> SectionStarts = new Dictionary<short, int>();
        }

        public static GroupKind KindOf(short groupType)
        {
            switch (groupType)
            {
                case _unfinished_group:
                case _success_group:
                case _failure_group:
                    return GroupKind.SectionStart;
                case _logon_group:
                case _logoff_group:
                    return GroupKind.Log;
                case _sound_group:
                case _tag_group:
                    return GroupKind.Action;
                case _end_group:
                case _interlevel_teleport_group:
                case _intralevel_teleport_group:
                    return GroupKind.Ending;
                case _movie_group:
                case _track_group:
                    return GroupKind.Stranding;
                default:
                    return GroupKind.Body;
            }
        }

        // handle_reading_terminal_keys reads no keys for these, so a group can't be made one
        public static bool IsStranding(short groupType)
        {
            return KindOf(groupType) == GroupKind.Stranding;
        }

        public static Reading Read(IReadOnlyList<TerminalSource.Group> groups)
        {
            var reading = new Reading();
            var types = groups.Select(group => group.Type).ToList();
            var isMarathon1 = groups.Count > 0 && (groups[0].Flags & _group_is_marathon_1) != 0;

            foreach (var sectionStart in SectionStartTypes)
            {
                reading.SectionStarts[sectionStart] = types.IndexOf(sectionStart);
            }

            var unfinished = reading.SectionStarts[_unfinished_group];
            reading.HasEntry = unfinished >= 0 || isMarathon1;
            if (!reading.HasEntry)
            {
                return reading;
            }

            foreach (var sectionStart in SectionStartTypes)
            {
                var start = reading.SectionStarts[sectionStart];
                if (start < 0)
                {
                    start = unfinished;
                }

                if (start >= 0)
                {
                    reading.Reached.Add(start);
                    ReadFrom(types, start + 1, reading);
                }
                else
                {
                    // Marathon 1's terminals have no sections, and are read from their first group
                    ReadFrom(types, 0, reading);
                }
            }

            for (var index = 0; index < types.Count; index++)
            {
                if (!reading.Reached.Contains(index) && types[index] != _end_group)
                {
                    reading.Unreachable.Add(index);
                }
            }

            return reading;
        }

        // Compared before and after an edit, as the same group objects
        public static HashSet<(Blocker Kind, TerminalSource.Group Group)> GetBlockers(IReadOnlyList<TerminalSource.Group> groups)
        {
            var blockers = new HashSet<(Blocker Kind, TerminalSource.Group Group)>();
            var reading = Read(groups);

            if (!reading.HasEntry)
            {
                // Then every group is unreachable, which is the one problem
                blockers.Add((Blocker.NoEntry, null));
                return blockers;
            }

            foreach (var index in reading.Unreachable)
            {
                blockers.Add((Blocker.Unreachable, groups[index]));
            }

            foreach (var index in reading.StrandingGroups)
            {
                blockers.Add((Blocker.Stranding, groups[index]));
            }

            return blockers;
        }

        // Whether the edited groups have a problem the groups didn't have before (so an edit can't break a terminal,
        // but one already broken can still be edited, and fixed a step at a time)
        public static bool AddsBlockers(HashSet<(Blocker Kind, TerminalSource.Group Group)> blockersBefore, IReadOnlyList<TerminalSource.Group> editedGroups)
        {
            return GetBlockers(editedGroups).Any(blocker => !blockersBefore.Contains(blocker));
        }

        // Whether what the group's permutation refers to exists: a level of the map, a polygon of the level, or a goal
        // object of the level
        public static bool IsPermutationValid(short groupType, short permutation)
        {
            var level = LevelEntity_Level.Instance;

            switch (groupType)
            {
                case _interlevel_teleport_group:
                {
                    var mapsFile = MapsLoading.Instance.MapsFile;
                    return mapsFile == null || permutation == EpilogueLevel || (permutation >= 0 && permutation < mapsFile.Levels.Count);
                }
                case _intralevel_teleport_group:
                    return !level || (permutation >= 0 && permutation < level.Level.PolygonList.Count);
                case _checkpoint_group:
                    // find_checkpoint_location: where the goal object with its number is
                    return !level || level.Level.SavedObjectList.Any(saved => saved.type == map._saved_goal && saved.index == permutation);
                default:
                    return true;
            }
        }

        private static void ReadFrom(List<short> types, int start, Reading reading)
        {
            for (var index = start; index < types.Count; index++)
            {
                var isFirstRead = reading.Reached.Add(index);

                switch (KindOf(types[index]))
                {
                    case GroupKind.Ending:
                        return;
                    case GroupKind.Stranding:
                        if (isFirstRead)
                        {
                            reading.StrandingGroups.Add(index);
                        }
                        return;
                    case GroupKind.SectionStart:
                    {
                        // goto_terminal_group: "You shouldn't be coming to this group"
                        var list = reading.SectionStarts[types[index]] == index ? reading.SectionStartsReadInto : reading.UnusedSectionStartsReadInto;
                        if (!list.Contains(index))
                        {
                            list.Add(index);
                        }

                        break;
                    }
                }
            }
        }
    }
}
