namespace ForgePlus.DataFileIO
{
    public class PhysicsData : FileDataBase<PhysicsFile>
    {
        public PhysicsFile PhysicsFile
        {
            get
            {
                LoadData();

                return file;
            }
        }
    }
}
