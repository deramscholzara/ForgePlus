#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using SFB;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading
    {
        // A new map file, where it's chosen to be saved, with a first level named in a dialog (a map file must have at least
        // one level). It's then loaded, with its level open. Cancelling either the location or the name makes nothing.
        public async void CreateNewMap()
        {
            var path = await ChooseNewMapPath();
            if (path == null)
            {
                return;
            }

            var levelName = await AskForLevelName(
                Strings.Get(Strings.Common, "Dialog.NewMap.Title"),
                Strings.Get(Strings.Common, "Dialog.NewMap.Message"));

            if (levelName == null)
            {
                return;
            }

            try
            {
                MapsFile.CreateNew(path, NewLevel(levelName));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Attempt to create a new map file at path \"{path}\" failed with exception: {exception}");
                return;
            }

            FileSettings.Instance.UpdateFilePath(DataFileTypes.Maps, filePath: path, loadFile: true);

            if (data != null)
            {
                OpenLevel(0);
            }
        }

        // A new level (NewLevel), named in a dialog, added to the end of the loaded map file (which is saved right away). Returns
        // its index, or -1 if the dialog was cancelled (or it couldn't be added).
        public async Task<int> AddNewLevel()
        {
            if (MapsFile == null)
            {
                return -1;
            }

            var levelName = await AskForLevelName(
                Strings.Get(Strings.Common, "Dialog.NewLevel.Title"),
                Strings.Get(Strings.Common, "Dialog.NewLevel.Message"));

            if (levelName == null || data == null)
            {
                return -1;
            }

            try
            {
                data.AddLevel(NewLevel(levelName));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Attempt to add a level to \"{FileSettings.Instance.GetFilePath(DataFileTypes.Maps)}\" failed with exception: {exception}");
                return -1;
            }

            // The directory is reread from the file, which doesn't have the open level's unsaved name (if it was renamed)
            UpdateOpenLevelDirectory();
            OnLevelNamesChanged?.Invoke();

            return MapsFile.Levels.Count - 1;
        }

        // The level name (Mac Roman, with room for its NUL), which can't be blank
        private static async Task<string> AskForLevelName(string title, string message)
        {
            return await DialogManager.Instance.DisplayQueuedTextDialog(
                title: title,
                message: message,
                initialText: string.Empty,
                maxLength: new static_data().level_name.MacRomanTextCapacity(),
                isValid: name => !string.IsNullOrWhiteSpace(name));
        }

        // Where to save a new map file (in the loaded one's folder, if there is one), or null if it's cancelled
        private static Task<string> ChooseNewMapPath()
        {
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            var type = DataFileTypes.Maps;
            var currentPath = FileSettings.Instance.GetFilePath(type);
            var initialDirectory = string.IsNullOrEmpty(currentPath) ?
                                   Environment.GetFolderPath(Environment.SpecialFolder.Desktop) :
                                   Path.GetDirectoryName(currentPath);

            UIBlocking.Instance.Block();

            StandaloneFileBrowser.SaveFilePanelAsync(
                title: Strings.Get(Strings.Menu, "FileBrowser.NewMap.Title"),
                directory: initialDirectory,
                defaultName: Strings.Get(Strings.Menu, "FileBrowser.NewMap.DefaultName"),
                type.FileExtension(),
                cb: savePath =>
                {
                    UIBlocking.Instance.Unblock();

                    if (string.IsNullOrWhiteSpace(savePath))
                    {
                        completion.TrySetResult(null);
                        return;
                    }

                    completion.TrySetResult(savePath.Replace(type.FileExtensionWithPeriod().ToLowerInvariant(), type.FileExtensionWithPeriod()));
                });

            return completion.Task;
        }

        // Forge's (and Weland's) starting lights: normal lights, from full intensity down to none in steps of 5%
        private const int NewLevelLightSteps = 20;

        // The starting room's half width (4 world units across)
        private const short NewLevelRoomHalfSize = (short) (2 * world.WORLD_ONE);

        // A level set up for single player: the single player entry point, the first environment and landscape, no
        // mission or environment flags, no random items or monsters, and the starting lights (as Forge and Weland start
        // one), with a starting room (a plain polygon, lit by the brightest light) and a player start in its middle. A
        // level needs some geometry, as Aleph One (like Marathon) can't load one without points. It's Marathon 2 data, so
        // the original games can read it.
        private static MapLevel NewLevel(string name)
        {
            var level = new MapLevel();
            level.static_world.level_name.SetMacRomanText(name);
            level.static_world.entry_point_flags = map._single_player_entry_point;
            level.loaded_wad.data_version = editor.MARATHON_TWO_DATA_VERSION;

            var lightDefaults = lightsource.get_defaults_for_light_type(lightsource._normal_light);
            for (var step = 0; step <= NewLevelLightSteps; step++)
            {
                var light = new light_data { static_data = lightDefaults.Clone() };
                var intensity = cstypes.FIXED_ONE * (NewLevelLightSteps - step) / NewLevelLightSteps;
                light.static_data.primary_active.intensity = intensity;
                light.static_data.secondary_active.intensity = intensity;
                light.static_data.becoming_active.intensity = intensity;
                map.MARK_SLOT_AS_USED(ref light.flags);

                level.LightList.Add(light);
            }

            AddStartingRoom(level);

            return level;
        }

        // A square of lines around the origin, filled as a loop of drawn lines is (PolygonClosing), with the player start
        private static void AddStartingRoom(MapLevel level)
        {
            var corners = new[]
            {
                GeometryConstruction.AddEndpoint(level, new world_point2d((short) -NewLevelRoomHalfSize, (short) -NewLevelRoomHalfSize)),
                GeometryConstruction.AddEndpoint(level, new world_point2d(NewLevelRoomHalfSize, (short) -NewLevelRoomHalfSize)),
                GeometryConstruction.AddEndpoint(level, new world_point2d(NewLevelRoomHalfSize, NewLevelRoomHalfSize)),
                GeometryConstruction.AddEndpoint(level, new world_point2d((short) -NewLevelRoomHalfSize, NewLevelRoomHalfSize)),
            };

            for (var i = 0; i < corners.Length; i++)
            {
                GeometryConstruction.AddLine(level, corners[i], corners[(i + 1) % corners.Length], cstypes.NONE, cstypes.NONE);
            }

            var loop = PolygonClosing.FindEmptyLoops(level, level.BuildEndpointLines())[0];
            PolygonClosing.AddPolygon(level, loop, loop.Corners[0]);

            level.SavedObjectList.Add(new map_object
            {
                type = map._saved_player,
                polygon_index = 0,
                location = new world_point3d(0, 0, 0),
            });
        }
    }
}
#endif
