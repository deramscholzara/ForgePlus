#if !NO_EDITING
namespace ForgePlus.History
{
    // A change that UndoHistory can take back and make again. Each is undone and redone in turn, so it can rely on the
    // data being as it left it.
    public interface IUndoableAction
    {
        // What changed (such as "Polygon 12"), for the Undo and Redo buttons' tooltips
        string Description { get; }

        // Whether it changes what's saved (the level and map file), rather than only the selection
        bool ChangesData { get; }

        void Undo();

        void Redo();
    }
}
#endif
