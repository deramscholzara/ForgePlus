using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Localization;
using System;
using System.Collections.Generic;
using static AlephOne.computer_interface;

namespace ForgePlus.UI
{
    // What keeps a level's terminals from being read as they're meant to be (TerminalRules), and what refers to what
    // doesn't exist: a group's level, polygon or goal object, and a control panel's terminal
    public static class TerminalErrors
    {
        public static IEnumerable<LevelError> Find(MapLevel level)
        {
            for (short terminalIndex = 0; terminalIndex < number_of_terminal_texts(level); terminalIndex++)
            {
                foreach (var error in FindInTerminal(get_indexed_terminal_data(level, terminalIndex), terminalIndex))
                {
                    yield return error;
                }
            }

            foreach (var error in FindInControlPanels(level))
            {
                yield return error;
            }
        }

        private static IEnumerable<LevelError> FindInTerminal(terminal_text_t terminal, short terminalIndex)
        {
            var groups = TerminalSource.GetGroups(terminal);
            var reading = TerminalRules.Read(groups);

            if (!reading.HasEntry)
            {
                yield return new LevelError(Get("Errors.Terminal.NoEntry", terminalIndex), () => EditGroups(terminal, edited =>
                {
                    edited.Insert(0, new TerminalSource.Group { Type = _unfinished_group, IsChanged = true });
                }));
            }
            else
            {
                if (reading.Unreachable.Count > 0)
                {
                    yield return new LevelError(Get("Errors.Terminal.Unreachable", terminalIndex, string.Join(", ", reading.Unreachable)));
                }

                foreach (var index in reading.SectionStartsReadInto)
                {
                    yield return new LevelError(Get("Errors.Terminal.SectionReadInto", terminalIndex, index, Name(groups[index].Type)), () => EditGroups(terminal, edited =>
                    {
                        edited.Insert(index, new TerminalSource.Group { Type = _end_group, IsChanged = true });
                    }));
                }

                foreach (var index in reading.UnusedSectionStartsReadInto)
                {
                    var type = groups[index].Type;
                    yield return new LevelError(Get("Errors.Terminal.UnusedSectionStart", terminalIndex, index, Name(type), reading.SectionStarts[type]), () => EditGroups(terminal, edited =>
                    {
                        edited.RemoveAt(index);
                    }));
                }

                foreach (var index in reading.StrandingGroups)
                {
                    yield return new LevelError(Get("Errors.Terminal.Stranding", terminalIndex, index, Name(groups[index].Type)));
                }
            }

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                if (TerminalRules.IsPermutationValid(group.Type, group.Permutation))
                {
                    continue;
                }

                switch (group.Type)
                {
                    case _interlevel_teleport_group:
                        yield return new LevelError(Get("Errors.Terminal.NoLevel", terminalIndex, index, group.Permutation, MapsLoading.Instance.MapsFile?.Levels.Count ?? 0));
                        break;
                    case _intralevel_teleport_group:
                        yield return new LevelError(Get("Errors.Terminal.NoPolygon", terminalIndex, index, group.Permutation));
                        break;
                    case _checkpoint_group:
                        yield return new LevelError(Get("Errors.Terminal.NoGoal", terminalIndex, index, group.Permutation));
                        break;
                }
            }
        }

        // devices.cpp: a terminal's control panel opens the terminal its permutation numbers
        private static IEnumerable<LevelError> FindInControlPanels(MapLevel level)
        {
            var terminalCount = number_of_terminal_texts(level);

            for (var sideIndex = 0; sideIndex < level.SideList.Count; sideIndex++)
            {
                var side = level.SideList[sideIndex];
                if (!map.SIDE_IS_CONTROL_PANEL(side) || devices.get_control_panel_definition(side.control_panel_type) == null ||
                    devices.get_panel_class(side.control_panel_type) != map._panel_is_computer_terminal)
                {
                    continue;
                }

                var terminalIndex = side.control_panel_permutation;
                if (terminalIndex < 0 || terminalIndex >= terminalCount)
                {
                    yield return new LevelError(Get("Errors.ControlPanel.NoTerminal", sideIndex, terminalIndex, terminalCount));
                }
            }
        }

        // A fix edits the terminal's groups as they are when it's applied
        private static void EditGroups(terminal_text_t terminal, Action<List<TerminalSource.Group>> edit)
        {
            var groups = TerminalSource.GetGroups(terminal);
            edit(groups);
            TerminalSource.SetGroups(terminal, groups);
        }

        private static string Name(short groupType)
        {
            return TerminalLayout.GetDirectiveName(groupType);
        }

        private static string Get(string key, params object[] args)
        {
            return Strings.Get(Strings.Common, key, args);
        }
    }
}
