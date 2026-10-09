#if !NO_EDITING
namespace ForgePlus.History
{
    // What records UndoHistory's actions as edits are made
    public interface IUndoRecorder
    {
        // Undoing or redoing waits for it
        bool IsEditInProgress { get; }

        // Records anything edited since the last action, before undoing or redoing
        void RecordPendingEdit();
    }
}
#endif
