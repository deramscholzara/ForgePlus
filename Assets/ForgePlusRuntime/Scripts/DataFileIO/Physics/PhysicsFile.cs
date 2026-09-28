using System.IO;

namespace ForgePlus.DataFileIO
{
    public class PhysicsFile : IFileLoadable
    {
        public string Path { get; private set; }

        public LoadedPhysicsModel Model { get; private set; }

        public void Load(string fileName)
        {
            var model = LoadedPhysicsModel.FromPhysicsFile(fileName);

            // Aleph One silently falls back to its built-in physics when a physics file can't be read,
            // but a file the user picked should be reported as unreadable
            if (model.Source == PhysicsModelSource.EngineDefaults)
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon physics file.");
            }

            Path = fileName;
            Model = model;
        }
    }
}
