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
    }
}
