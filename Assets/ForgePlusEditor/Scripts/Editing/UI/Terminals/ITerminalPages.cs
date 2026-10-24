using AlephOne;
using System;
using System.Collections.Generic;

namespace ForgePlus.UI
{
    // The pages a terminal panel (TerminalPanel) shows, and moves between: Terminals mode's preview of every page of a
    // terminal (TerminalsViewModel), or the pages a player reads in Play mode (PlayTerminalPages). Its properties are
    // bound to, so they notify as the page changes.
    public interface ITerminalPages
    {
        event Action OnPageChanged;

        terminal_text_t Terminal { get; }

        TerminalLayout.Page Page { get; }

        string PageCaption { get; }

        bool CanGoBack { get; }

        bool CanGoNext { get; }

        // Whether a checkpoint's map is the level itself, framed in the view behind the page (as the editor's camera can
        // be), rather than left out
        bool ShowsCheckpointMaps { get; }

        void Back();

        void Next();

        // The lines the page shows of a group whose text scrolls
        List<TerminalLayout.Line> GetPageLines();
    }
}
