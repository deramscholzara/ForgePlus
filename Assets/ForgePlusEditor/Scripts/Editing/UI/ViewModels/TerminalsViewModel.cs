using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.computer_interface;

namespace ForgePlus.UI
{
    // The open level's terminals as they're stored (each a list of groups), and the page of one being previewed
    // (a page is a group, or a screenful of a group's text), whose group is the one edited. An edit that breaks the
    // terminal (TerminalRules), or a permutation that refers to what doesn't exist, is ignored.
    [NoAutoStaticsCleanup]
    public class TerminalsViewModel : BindableObject, IDisposable
    {
        // The properties that describe the terminal, group and page, which all change with the page
        private static readonly string[] PageProperties =
        {
            nameof(TerminalIndex),
            nameof(HasTerminals),
            nameof(LinesPerPage),
            nameof(TextIsEncoded),
            nameof(GroupIndex),
            nameof(CanRemoveGroup),
            nameof(GroupType),
            nameof(UsesPermutation),
            nameof(Permutation),
            nameof(PermutationCaption),
            nameof(DrawObjectOnRight),
            nameof(IsDrawObjectOnRightEditable),
            nameof(CenterObject),
            nameof(IsCenterObjectEditable),
            nameof(IsMarathon1),
            nameof(HasText),
            nameof(Text),
            nameof(PageCaption),
            nameof(CanGoBack),
            nameof(CanGoNext),
        };

        // The group types' directives, by type
        private static readonly List<string> DirectiveNames = GetDirectiveNames();

        public event Action OnTerminalsChanged;

        // After the terminal changes, or its groups do (one's added, removed or moved)
        public event Action OnTerminalChanged;

        public event Action OnPageChanged;

        // After a terminal's data is edited
        public event Action OnTerminalEdited;

        // A style chosen from the styles panel, for the group's text
        public event Action<TerminalSource.StyleKind, short> OnStyleChosen;

        private readonly List<terminal_text_t> terminals = new List<terminal_text_t>();

        private List<TerminalLayout.Page> pages = new List<TerminalLayout.Page>();
        private int terminalIndex = -1;
        private int pageIndex = 0;

        // The group's text as last typed, kept while its group is edited (read back from the group, it loses codes that
        // change nothing, and half-typed ones)
        private string editedText;
        private terminal_text_t editedTextTerminal;
        private int editedTextGroupIndex = -1;

        public TerminalsViewModel()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
        }

        public int TerminalCount
        {
            get
            {
                return terminals.Count;
            }
        }

        public terminal_text_t Terminal
        {
            get
            {
                return terminalIndex >= 0 ? terminals[terminalIndex] : null;
            }
        }

        public TerminalLayout.Page Page
        {
            get
            {
                return pageIndex < pages.Count ? pages[pageIndex] : null;
            }
        }

        [CreateProperty]
        public int TerminalIndex
        {
            get
            {
                return terminalIndex;
            }
            set
            {
                if (terminalIndex != value && value >= 0 && value < terminals.Count)
                {
                    terminalIndex = value;
                    pages = TerminalLayout.GetPages(terminals[terminalIndex]);

                    // On its first page before its groups' toggles are made, so they start on the right one
                    pageIndex = 0;
                    OnTerminalChanged?.Invoke();
                    ShowPage(0);
                }
            }
        }

        [CreateProperty]
        public bool HasTerminals
        {
            get
            {
                return terminals.Count > 0;
            }
        }

        [CreateProperty]
        public string LinesPerPage
        {
            get
            {
                return Terminal != null ? Terminal.lines_per_page.ToString() : "-";
            }
        }

        // Saved lightly scrambled, as Bungie's tools saved it (Marathon and Aleph One read it either way)
        [CreateProperty]
        public bool TextIsEncoded
        {
            get
            {
                return Terminal != null && Terminal.encode_when_saved;
            }
            set
            {
                if (Terminal != null && Terminal.encode_when_saved != value)
                {
                    Terminal.encode_when_saved = value;
                    Notify(nameof(TextIsEncoded));
                    LevelHistory.MarkEdited();
                }
            }
        }

        public int GroupCount
        {
            get
            {
                return Terminal != null ? Terminal.groupings.Count : 0;
            }
        }

        // Choosing a group previews its first page
        [CreateProperty]
        public int GroupIndex
        {
            get
            {
                return Page != null ? Page.GroupIndex : -1;
            }
            set
            {
                if (value == GroupIndex)
                {
                    return;
                }

                var firstPage = pages.FindIndex(page => page.GroupIndex == value);
                if (firstPage >= 0)
                {
                    ShowPage(firstPage);
                }
            }
        }

        // A terminal keeps one group at least
        [CreateProperty]
        public bool CanRemoveGroup
        {
            get
            {
                return GroupCount > 1;
            }
        }

        // As terminal source writes it (#LOGON, #INFORMATION)
        [CreateProperty]
        public string GroupType
        {
            get
            {
                return Page != null ? TerminalLayout.GetDirectiveName(Page.Group.type) : "-";
            }
            set
            {
                var type = (short) DirectiveNames.IndexOf(value);
                if (type < 0)
                {
                    return;
                }

                if (TerminalRules.IsStranding(type))
                {
                    Notify(nameof(GroupType));
                    return;
                }

                EditGroup(group =>
                {
                    group.Type = type;
                    group.IsChanged = true;
                });
            }
        }

        // Every group type (all of which the original games have), and the group's own if it's none of them
        [CreateProperty]
        public List<string> GroupTypeChoices
        {
            get
            {
                var choices = new List<string>(DirectiveNames);

                if (Page != null && !choices.Contains(GroupType))
                {
                    choices.Add(GroupType);
                }

                return choices;
            }
        }

        // The types that strand the player, which are shown but can't be chosen
        [CreateProperty]
        public List<string> GroupTypeUnavailableChoices
        {
            get
            {
                var choices = new List<string>();
                for (short type = 0; type < DirectiveNames.Count; type++)
                {
                    if (TerminalRules.IsStranding(type))
                    {
                        choices.Add(DirectiveNames[type]);
                    }
                }

                return choices;
            }
        }

        [CreateProperty]
        public bool UsesPermutation
        {
            get
            {
                return Page != null && TerminalLayout.UsesPermutation(Page.Group.type);
            }
        }

        [CreateProperty]
        public int Permutation
        {
            get
            {
                return Page != null ? Page.Group.permutation : 0;
            }
            set
            {
                var permutation = (short) Math.Max(short.MinValue, Math.Min(short.MaxValue, value));
                if (Page == null || !TerminalRules.IsPermutationValid(Page.Group.type, permutation))
                {
                    Notify(nameof(Permutation));
                    return;
                }

                EditGroup(group => group.Permutation = permutation);
            }
        }

        // What the group's permutation is for its type
        [CreateProperty]
        public string PermutationCaption
        {
            get
            {
                return Page != null ? PermutationCaptions.ForTerminalGroup(Page.Group) : string.Empty;
            }
        }

        [CreateProperty]
        public bool DrawObjectOnRight
        {
            get
            {
                return HasFlag(_draw_object_on_right);
            }
            set
            {
                SetFlag(_draw_object_on_right, value);
            }
        }

        // Where a group draws an object beside its text: its picture or checkpoint map (calculate_bounds_for_object)
        [CreateProperty]
        public bool IsDrawObjectOnRightEditable
        {
            get
            {
                return DrawsObject;
            }
        }

        [CreateProperty]
        public bool CenterObject
        {
            get
            {
                return HasFlag(_center_object);
            }
            set
            {
                SetFlag(_center_object, value);
            }
        }

        [CreateProperty]
        public bool IsCenterObjectEditable
        {
            get
            {
                return DrawsObject;
            }
        }

        [CreateProperty]
        public bool IsMarathon1
        {
            get
            {
                return HasFlag(_group_is_marathon_1);
            }
        }

        [CreateProperty]
        public bool HasText
        {
            get
            {
                return Page != null && TerminalLayout.HasText(Page.Group.type);
            }
        }

        // The group's text as terminal source writes it: with its own line breaks (not as its pages wrap it), and its
        // style changes as codes in it
        [CreateProperty]
        public string Text
        {
            get
            {
                if (Page == null)
                {
                    return string.Empty;
                }

                if (IsEditedTextShown)
                {
                    return editedText;
                }

                return TerminalSource.GetSource(TerminalSource.GetGroups(Terminal)[GroupIndex]);
            }
            set
            {
                if (Page == null || value == Text)
                {
                    return;
                }

                // Kept as typed before the group's shown again (or as it was, if the text doesn't fit in the terminal)
                var previousText = IsEditedTextShown ? editedText : null;
                editedText = value ?? string.Empty;
                editedTextTerminal = Terminal;
                editedTextGroupIndex = GroupIndex;

                EditGroup(group => TerminalSource.SetSource(group, editedText), onFailure: () =>
                {
                    editedText = previousText;
                    editedTextTerminal = previousText != null ? Terminal : null;
                });
            }
        }

        // Where the preview is: its page of the terminal, and of its group where the group has more than one
        [CreateProperty]
        public string PageCaption
        {
            get
            {
                if (Page == null)
                {
                    return "-";
                }

                if (Page.PagesOfGroup > 1)
                {
                    return Strings.Get(Strings.Terminals, "Terminals.PageCaption.WithPart", pageIndex + 1, pages.Count, Page.GroupIndex, Page.PageOfGroup + 1, Page.PagesOfGroup);
                }

                return Strings.Get(Strings.Terminals, "Terminals.PageCaption", pageIndex + 1, pages.Count, Page.GroupIndex);
            }
        }

        [CreateProperty]
        public bool CanGoBack
        {
            get
            {
                return pageIndex > 0;
            }
        }

        [CreateProperty]
        public bool CanGoNext
        {
            get
            {
                return pageIndex + 1 < pages.Count;
            }
        }

        public void Back()
        {
            ShowPage(pageIndex - 1);
        }

        public void Next()
        {
            ShowPage(pageIndex + 1);
        }

        // An information group, which is then the one edited: after the group, or else at the nearest place it can be
        // read (such as before a section's #END)
        public void AddGroup()
        {
            if (Terminal == null)
            {
                return;
            }

            var groups = TerminalSource.GetGroups(Terminal);
            var blockers = TerminalRules.GetBlockers(groups);
            var newGroup = new TerminalSource.Group { Type = _information_group, IsChanged = true };

            foreach (var groupIndex in GetPlacesNear(GroupIndex + 1, groups.Count))
            {
                groups.Insert(groupIndex, newGroup);
                var isReadable = !TerminalRules.AddsBlockers(blockers, groups);
                groups.RemoveAt(groupIndex);

                if (isReadable)
                {
                    EditGroups(edited =>
                    {
                        edited.Insert(groupIndex, newGroup);
                        return groupIndex;
                    });
                    return;
                }
            }
        }

        // The places from one, then back toward the first, then on toward the last
        private static IEnumerable<int> GetPlacesNear(int place, int groupCount)
        {
            place = Math.Max(0, Math.Min(place, groupCount));

            for (var index = place; index >= 0; index--)
            {
                yield return index;
            }

            for (var index = place + 1; index <= groupCount; index++)
            {
                yield return index;
            }
        }

        // After terminals' data is changed elsewhere (as an error's fix changes it), on the same group where it can be
        public void ReloadTerminal()
        {
            if (Terminal != null)
            {
                ShowGroupAgain(Math.Max(0, Math.Min(GroupIndex, GroupCount - 1)), groupsChanged: true);
            }
        }

        public void RemoveGroup()
        {
            if (!CanRemoveGroup)
            {
                return;
            }

            var groupIndex = GroupIndex;
            EditGroups(groups =>
            {
                groups.RemoveAt(groupIndex);
                return Math.Min(groupIndex, groups.Count - 1);
            });
        }

        // The moved group is then the one edited
        public void MoveGroup(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex || fromIndex < 0 || fromIndex >= GroupCount || toIndex < 0 || toIndex >= GroupCount)
            {
                return;
            }

            EditGroups(groups =>
            {
                var group = groups[fromIndex];
                groups.RemoveAt(fromIndex);
                groups.Insert(toIndex, group);
                return toIndex;
            });
        }

        // For the group's text, where it's shown
        public void ChooseStyle(TerminalSource.StyleKind kind, short value)
        {
            if (HasText)
            {
                OnStyleChosen?.Invoke(kind, value);
            }
        }

        // The lines the page shows of a group whose text scrolls
        public List<TerminalLayout.Line> GetPageLines()
        {
            if (Page == null || !TerminalLayout.HasLines(Page.Group.type))
            {
                return new List<TerminalLayout.Line>();
            }

            return TerminalLayout.GetLines(Terminal, Page.Group, TerminalLayout.GetTextBounds(Page.Group), Page.FirstLine, Terminal.lines_per_page);
        }

        public void Dispose()
        {
            MapsLoading.Instance.OnLevelOpened -= OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed -= OnLevelClosed;
        }

        private bool DrawsObject
        {
            get
            {
                return Page != null && (Page.Group.type == _pict_group || Page.Group.type == _checkpoint_group);
            }
        }

        private bool HasFlag(short flag)
        {
            return Page != null && (Page.Group.flags & flag) != 0;
        }

        // A flag that changes where the group's text is (so how many lines it has)
        private void SetFlag(short flag, bool isSet)
        {
            if (HasFlag(flag) == isSet)
            {
                return;
            }

            EditGroup(group =>
            {
                group.Flags = (short) (isSet ? group.Flags | flag : group.Flags & ~flag);
                group.IsChanged = true;
            });
        }

        private bool IsEditedTextShown
        {
            get
            {
                return editedTextTerminal != null && editedTextTerminal == Terminal && editedTextGroupIndex == GroupIndex;
            }
        }

        private void EditGroup(Action<TerminalSource.Group> edit, Action onFailure = null)
        {
            if (Page == null)
            {
                return;
            }

            var groupIndex = GroupIndex;
            EditGroups(groups =>
            {
                edit(groups[groupIndex]);
                return groupIndex;
            }, groupsChanged: false, onFailure);
        }

        // Edits the terminal's groups, which are stored again (or, if they no longer fit in a terminal, left as they
        // were), and previews the group the edit returns: on the same page of it, if it was being previewed already
        private void EditGroups(Func<List<TerminalSource.Group>, int> edit, bool groupsChanged = true, Action onFailure = null)
        {
            var previousGroupIndex = GroupIndex;
            var previousPageOfGroup = Page != null ? Page.PageOfGroup : 0;

            var groups = TerminalSource.GetGroups(Terminal);
            var blockers = TerminalRules.GetBlockers(groups);
            var groupIndex = edit(groups);

            // An edit that breaks the terminal is ignored, as one that doesn't fit is (and what shows it shows what was)
            if (TerminalRules.AddsBlockers(blockers, groups) || !TerminalSource.SetGroups(Terminal, groups))
            {
                onFailure?.Invoke();
                ShowPage(pageIndex);
                return;
            }

            var isSameGroup = !groupsChanged && groupIndex == previousGroupIndex;
            ShowGroupAgain(groupIndex, groupsChanged, isSameGroup ? previousPageOfGroup : 0);
            OnTerminalEdited?.Invoke();
            LevelHistory.MarkEdited();
        }

        // Lays out the terminal's pages again, and previews the group (from a later page of it, where it has that many)
        private void ShowGroupAgain(int groupIndex, bool groupsChanged, int pageOfGroup = 0)
        {
            pages = TerminalLayout.GetPages(Terminal);

            var firstPage = Math.Max(0, pages.FindIndex(page => page.GroupIndex == groupIndex));
            pageIndex = firstPage;
            if (pageOfGroup > 0)
            {
                pageIndex = Math.Min(firstPage + pageOfGroup, firstPage + pages[firstPage].PagesOfGroup - 1);
            }

            if (groupsChanged)
            {
                // The groups are numbered again
                editedTextTerminal = null;
                OnTerminalChanged?.Invoke();
            }

            ShowPage(pageIndex);
        }

        private void ShowPage(int index)
        {
            pageIndex = Math.Max(0, Math.Min(index, pages.Count - 1));

            // The text as typed is kept for its group only
            if (!IsEditedTextShown)
            {
                editedTextTerminal = null;
                editedText = null;
            }

            foreach (var property in PageProperties)
            {
                Notify(property);
            }

            OnPageChanged?.Invoke();
        }

        private void OnLevelOpened(string levelName)
        {
            Load(LevelEntity_Level.Instance ? LevelEntity_Level.Instance.Level : null);
        }

        private void OnLevelClosed()
        {
            Load(null);
        }

        // The level's terminals, previewing the first one's first page (or none, without a level or terminals)
        private void Load(MapLevel level)
        {
            terminals.Clear();
            terminalIndex = -1;
            pages = new List<TerminalLayout.Page>();
            pageIndex = 0;

            if (level != null)
            {
                for (short i = 0; i < number_of_terminal_texts(level); i++)
                {
                    terminals.Add(get_indexed_terminal_data(level, i));
                }
            }

            OnTerminalsChanged?.Invoke();

            if (terminals.Count > 0)
            {
                TerminalIndex = 0;
            }
            else
            {
                OnTerminalChanged?.Invoke();
                ShowPage(0);
            }
        }

        private static List<string> GetDirectiveNames()
        {
            var names = new List<string>();
            for (short type = 0; type < NUMBER_OF_GROUP_TYPES; type++)
            {
                names.Add(TerminalLayout.GetDirectiveName(type));
            }

            return names;
        }
    }
}
