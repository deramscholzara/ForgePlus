using AlephOne;
using ForgePlus.Localization;
using ForgePlus.UI;
using System;
using System.Collections.Generic;
using Unity.Properties;
using static AlephOne.computer_interface;

namespace ForgePlus.Play
{
    // The pages a player reads at a terminal in Play mode (computer_interface.cpp): from its unfinished section (the
    // level's goals aren't tracked, so they're never finished or failed; a terminal without one starts at its success
    // section, or else its first group) to that section's end. A tag group activates its tagged lights and platforms as
    // it's reached; a teleport (within the level, or to another) ends the reading there, and then teleports the player;
    // sounds, music, movies and cameras are left out. Moving past the last page ends the reading.
    public class PlayTerminalPages : BindableObject, ITerminalPages
    {
        // Each step is a page, or (with no page) a tag to activate as it's reached
        private readonly List<(TerminalLayout.Page Page, short Tag)> steps = new List<(TerminalLayout.Page, short)>();
        private readonly Action<short> activateTag;
        private readonly Action<short> teleport;

        // Where the reading teleports the player once it ends (a polygon, or -1 - a level number, as Aleph One encodes
        // a teleport to another level), if it does
        private short? teleportDestination;

        private int stepIndex = -1;
        private bool isFinished;

        public PlayTerminalPages(terminal_text_t terminal, Action<short> activateTag, Action<short> teleport)
        {
            Terminal = terminal;
            this.activateTag = activateTag;
            this.teleport = teleport;

            BuildSteps();
        }

        public event Action OnPageChanged;

        // After moving past the last page, before any teleport
        public event Action OnFinished;

        // On to the first page (activating any tags before it, or ending at once if it has none)
        public void Start()
        {
            MoveTo(0);
        }

        public terminal_text_t Terminal { get; }

        public TerminalLayout.Page Page
        {
            get
            {
                return stepIndex >= 0 && stepIndex < steps.Count ? steps[stepIndex].Page : null;
            }
        }

        [CreateProperty]
        public string PageCaption
        {
            get
            {
                var page = Page;
                if (page == null)
                {
                    return "-";
                }

                return Strings.Get(Strings.Terminals, "Terminals.PageCaption", PageNumber(stepIndex), PageNumber(steps.Count - 1), page.GroupIndex);
            }
        }

        [CreateProperty]
        public bool CanGoBack
        {
            get
            {
                return FindPage(stepIndex - 1, forward: false) >= 0;
            }
        }

        // Past the last page too, which ends the reading
        [CreateProperty]
        public bool CanGoNext
        {
            get
            {
                return Page != null;
            }
        }

        // A checkpoint's map would need the level's own view, which the player is looking through
        public bool ShowsCheckpointMaps
        {
            get
            {
                return false;
            }
        }

        public void Back()
        {
            var previous = FindPage(stepIndex - 1, forward: false);
            if (previous >= 0)
            {
                stepIndex = previous;
                NotifyPageChanged();
            }
        }

        public void Next()
        {
            MoveTo(stepIndex + 1);
        }

        public List<TerminalLayout.Line> GetPageLines()
        {
            var page = Page;
            if (page == null || !TerminalLayout.HasLines(page.Group.type))
            {
                return new List<TerminalLayout.Line>();
            }

            return TerminalLayout.GetLines(Terminal, page.Group, TerminalLayout.GetTextBounds(page.Group), page.FirstLine, Terminal.lines_per_page);
        }

        private void BuildSteps()
        {
            var groups = Terminal.groupings;
            var start = FindGroup(_unfinished_group);
            if (start < 0)
            {
                start = FindGroup(_success_group);
            }

            var pagesByGroup = new Dictionary<short, List<TerminalLayout.Page>>();
            foreach (var page in TerminalLayout.GetPages(Terminal))
            {
                if (!pagesByGroup.TryGetValue(page.GroupIndex, out var groupPages))
                {
                    groupPages = new List<TerminalLayout.Page>();
                    pagesByGroup[page.GroupIndex] = groupPages;
                }

                groupPages.Add(page);
            }

            for (var groupIndex = (short) (start + 1); groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                switch (group.type)
                {
                    case _logon_group:
                    case _information_group:
                    case _pict_group:
                    case _checkpoint_group:
                    case _logoff_group:
                    case _static_group:
                        if (pagesByGroup.TryGetValue(groupIndex, out var groupPages))
                        {
                            foreach (var page in groupPages)
                            {
                                steps.Add((page, 0));
                            }
                        }

                        break;

                    case _tag_group:
                        steps.Add((null, group.permutation));
                        break;

                    case _intralevel_teleport_group:
                        teleportDestination = group.permutation;
                        return;

                    case _interlevel_teleport_group:
                        teleportDestination = (short) (-group.permutation - 1);
                        return;

                    case _unfinished_group:
                    case _success_group:
                    case _failure_group:
                    case _end_group:
                        return;
                }
            }
        }

        // The section's group, or -1 (so a terminal without sections is read from its first group)
        private short FindGroup(short type)
        {
            for (short groupIndex = 0; groupIndex < Terminal.groupings.Count; groupIndex++)
            {
                if (Terminal.groupings[groupIndex].type == type)
                {
                    return groupIndex;
                }
            }

            return -1;
        }

        // On to the next page, activating the tags on the way, or the end
        private void MoveTo(int index)
        {
            while (index < steps.Count && steps[index].Page == null)
            {
                activateTag?.Invoke(steps[index].Tag);
                index++;
            }

            stepIndex = index;
            NotifyPageChanged();

            if (stepIndex >= steps.Count && !isFinished)
            {
                isFinished = true;
                OnFinished?.Invoke();

                if (teleportDestination.HasValue)
                {
                    teleport?.Invoke(teleportDestination.Value);
                }
            }
        }

        // The nearest page from the step, in the direction given, or -1
        private int FindPage(int index, bool forward)
        {
            while (index >= 0 && index < steps.Count)
            {
                if (steps[index].Page != null)
                {
                    return index;
                }

                index += forward ? 1 : -1;
            }

            return -1;
        }

        // The page's number among the pages (from 1)
        private int PageNumber(int index)
        {
            var number = 0;
            for (var i = 0; i <= index && i < steps.Count; i++)
            {
                if (steps[i].Page != null)
                {
                    number++;
                }
            }

            return number;
        }

        private void NotifyPageChanged()
        {
            Notify(nameof(PageCaption));
            Notify(nameof(CanGoBack));
            Notify(nameof(CanGoNext));
            OnPageChanged?.Invoke();
        }
    }
}
