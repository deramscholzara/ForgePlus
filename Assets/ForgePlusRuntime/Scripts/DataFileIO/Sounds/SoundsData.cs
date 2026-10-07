namespace ForgePlus.DataFileIO
{
    public class SoundsData : FileDataBase<SoundsFile>
    {
        public SoundsFile SoundsFile
        {
            get
            {
                LoadData();

                return file;
            }
        }

        // The file if it has been loaded (without loading it)
        public SoundsFile LoadedFile
        {
            get
            {
                return file;
            }
        }
    }
}
