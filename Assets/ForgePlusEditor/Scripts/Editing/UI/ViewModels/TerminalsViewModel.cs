using AlephOne;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Properties;
using static AlephOne.computer_interface;

namespace ForgePlus.UI
{
    // The open level's terminals as they're stored (each a list of groups), and the page of one being previewed
    // (a page is a group, or a screenful of a group's text)
    public class TerminalsViewModel : BindableObject, IDisposable
    {
        // The properties that describe the terminal, group and page, which all change with the page
        private static readonly string[] PageProperties =
        {
            nameof(TerminalIndex),
            nameof(HasTerminals),
            nameof(LinesPerPage),
            nameof(GroupIndex),
            nameof(GroupType),
            nameof(UsesPermutation),
            nameof(Permutation),
            nameof(DrawObjectOnRight),
            nameof(CenterObject),
            nameof(IsMarathon1),
            nameof(HasText),
            nameof(Text),
            nameof(TextStyles),
            nameof(PageCaption),
            nameof(CanGoBack),
            nameof(CanGoNext),
        };

        public event Action OnTerminalsChanged;
        public event Action OnTerminalChanged;
        public event Action OnPageChanged;

        private readonly List<terminal_text_t> terminals = new List<terminal_text_t>();

        private List<TerminalLayout.Page> pages = new List<TerminalLayout.Page>();
        private int terminalIndex = -1;
        private int pageIndex = 0;

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

        [CreateProperty]
        public string GroupType
        {
            get
            {
                return Page != null ? TerminalLayout.GetDirectiveName(Page.Group.type) : "-";
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
        public string Permutation
        {
            get
            {
                return Page != null ? Page.Group.permutation.ToString() : "-";
            }
        }

        [CreateProperty]
        public bool DrawObjectOnRight
        {
            get
            {
                return HasFlag(_draw_object_on_right);
            }
        }

        [CreateProperty]
        public bool CenterObject
        {
            get
            {
                return HasFlag(_center_object);
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

        // The group's text as it's stored, with its own line breaks (not as its pages wrap it)
        [CreateProperty]
        public string Text
        {
            get
            {
                if (!HasText)
                {
                    return "-";
                }

                var text = TerminalText.Decode(Terminal.text, Page.Group.start_index, Page.Group.start_index + Page.Group.length, keepLineEnds: true);
                return string.IsNullOrWhiteSpace(text) ? "(none)" : text;
            }
        }

        // The style changes in the group's text: where each is (from the text's start), and the faces and color it
        // changes to. They're stored for the whole terminal, but each group starts plain, so only its own apply.
        [CreateProperty]
        public string TextStyles
        {
            get
            {
                if (!HasText)
                {
                    return "-";
                }

                var builder = new StringBuilder();
                var start = Page.Group.start_index;
                var end = Page.Group.start_index + Page.Group.length;

                foreach (var change in Terminal.font_changes)
                {
                    if (change.index < start || change.index >= end)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append($"At {change.index - start}: {DescribeFace(change.face)}, color {change.color}");
                }

                return builder.Length > 0 ? builder.ToString() : "None";
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

                var caption = $"Page {pageIndex + 1} of {pages.Count}  -  Group {Page.GroupIndex}";
                if (Page.PagesOfGroup > 1)
                {
                    caption += $", part {Page.PageOfGroup + 1} of {Page.PagesOfGroup}";
                }

                return caption;
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

        private bool HasFlag(short flag)
        {
            return Page != null && (Page.Group.flags & flag) != 0;
        }

        private static string DescribeFace(short face)
        {
            if (face == _plain_text)
            {
                return "plain";
            }

            var styles = new List<string>();
            if ((face & _bold_text) != 0) styles.Add("bold");
            if ((face & _italic_text) != 0) styles.Add("italic");
            if ((face & _underline_text) != 0) styles.Add("underline");

            return string.Join(" ", styles);
        }

        private void ShowPage(int index)
        {
            pageIndex = Math.Max(0, Math.Min(index, pages.Count - 1));

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
    }
}
