using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.Palette;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The floor and ceiling heights in use (and those added this session), each list with a button for adding one. A
    // swatch's height can be typed in, which for a used height asks whether to move its faces or add a new swatch.
    public class HeightPalettePanel : PalettePanel
    {
        private const string ChangeAllOption = "All";
        private const string AddNewOption = "New";

        private VisualElement floorSwatches;
        private VisualElement ceilingSwatches;

        private Dictionary<short, int> floorUsageCounts = new Dictionary<short, int>();
        private Dictionary<short, int> ceilingUsageCounts = new Dictionary<short, int>();

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/HeightPalette";
            }
        }

        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchHeight";
            }
        }

        protected override IEnumerable<VisualElement> SwatchContainers
        {
            get
            {
                yield return floorSwatches;
                yield return ceilingSwatches;
            }
        }

        protected override void OnLoaded()
        {
            floorSwatches = Root.Q("floor-swatches");
            ceilingSwatches = Root.Q("ceiling-swatches");

            Root.Find<Button>("add-floor").clicked += () => Add(LevelEntity_Polygon.DataSources.Floor);
            Root.Find<Button>("add-ceiling").clicked += () => Add(LevelEntity_Polygon.DataSources.Ceiling);

            base.OnLoaded();
        }

        // Counted once for all the swatches
        protected override void OnRebuilding()
        {
            floorUsageCounts = HeightSwatches.UsageCounts(LevelEntity_Polygon.DataSources.Floor);
            ceilingUsageCounts = HeightSwatches.UsageCounts(LevelEntity_Polygon.DataSources.Ceiling);
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.IsHeight;
        }

        protected override VisualElement ContainerFor(PaletteManager.Swatch swatch)
        {
            return swatch.HeightDataSource == LevelEntity_Polygon.DataSources.Ceiling ? ceilingSwatches : floorSwatches;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            Strings.Localize(instance);

            var usageCounts = swatch.HeightDataSource == LevelEntity_Polygon.DataSources.Ceiling ? ceilingUsageCounts : floorUsageCounts;
            usageCounts.TryGetValue(swatch.Height, out var usageCount);

            instance.Q("in-use").style.display = usageCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var field = instance.Q<WorldDistanceField>("value");
            field.SetValueWithoutNotify(swatch.Height);
            field.KeepsEventsFromParents();
            field.RegisterValueChangedCallback(changeEvent => ChangeHeight(swatch, usageCount, changeEvent.newValue, field));
        }

        // An unused height just changes. A used one can move its faces (unless that would turn any inside out), or be
        // added as a new swatch.
        private async void ChangeHeight(PaletteManager.Swatch swatch, int usageCount, int value, WorldDistanceField field)
        {
            var dataSource = swatch.HeightDataSource;
            var oldHeight = swatch.Height;
            var newHeight = (short) Mathf.Clamp(value, short.MinValue, short.MaxValue);

            if (newHeight == oldHeight)
            {
                field.SetValueWithoutNotify(oldHeight);
                return;
            }

            if (usageCount == 0)
            {
                HeightSwatches.Replace(dataSource, oldHeight, newHeight);
                PaletteManager.Instance.ClickHeight(dataSource, newHeight);
                return;
            }

            var polygons = HeightSwatches.PolygonsAt(dataSource, oldHeight);
            var blockedCount = polygons.Count(polygon => !HeightsEditing.IsAllowed(polygon, dataSource, newHeight));
            var faces = dataSource == LevelEntity_Polygon.DataSources.Ceiling ? "Ceilings" : "Floors";

            var message = blockedCount == 0 ?
                          Strings.Get(Strings.Heights, $"HeightPalette.Change{faces}.Message", polygons.Count, oldHeight, newHeight) :
                          Strings.Get(Strings.Heights, $"HeightPalette.Change{faces}.Blocked", blockedCount, oldHeight, newHeight);

            var options = new List<string> { AddNewOption };
            var optionLabels = new List<string> { Strings.Get(Strings.Heights, "HeightPalette.Change.New") };

            if (blockedCount == 0)
            {
                options.Insert(0, ChangeAllOption);
                optionLabels.Insert(0, Strings.Get(Strings.Heights, "HeightPalette.Change.All"));
            }

            var result = await DialogManager.Instance.DisplayQueuedDialog(Strings.Get(Strings.Heights, $"HeightPalette.Change{faces}.Title"),
                                                                          message,
                                                                          options,
                                                                          optionLabels,
                                                                          checkboxLabel: null);

            switch (result.Option)
            {
                case ChangeAllOption:
                    // Once its faces have moved, the swatch moves with them
                    HeightsEditing.SetHeights(polygons, dataSource, newHeight);
                    HeightSwatches.Replace(dataSource, oldHeight, newHeight);
                    PaletteManager.Instance.ClickHeight(dataSource, newHeight);
                    break;
                case AddNewOption:
                    HeightSwatches.Add(dataSource, newHeight);
                    PaletteManager.Instance.ClickHeight(dataSource, newHeight);
                    break;
                default:
                    field.SetValueWithoutNotify(oldHeight);
                    break;
            }
        }

        // A world unit above the list's selected (or highest) height, and selected
        private static void Add(LevelEntity_Polygon.DataSources dataSource)
        {
            var heights = HeightSwatches.Heights(dataSource);

            var hasSelected = PaletteManager.Instance.TryGetSelectedHeight(out var selectedDataSource, out var selectedHeight) &&
                              selectedDataSource == dataSource;

            var start = hasSelected ? selectedHeight : heights.Count > 0 ? heights.Max() : -world.WORLD_ONE;
            var height = NextUnusedHeight(heights, start, world.WORLD_ONE) ?? NextUnusedHeight(heights, start, -world.WORLD_ONE);

            if (height.HasValue)
            {
                HeightSwatches.Add(dataSource, height.Value);
                PaletteManager.Instance.ClickHeight(dataSource, height.Value);
            }
        }

        // The first height past start (by step) that isn't listed, or null if none is left in range
        private static short? NextUnusedHeight(IReadOnlyList<short> heights, int start, int step)
        {
            for (var height = start + step; height >= short.MinValue && height <= short.MaxValue; height += step)
            {
                if (!heights.Contains((short) height))
                {
                    return (short) height;
                }
            }

            return null;
        }
    }
}
