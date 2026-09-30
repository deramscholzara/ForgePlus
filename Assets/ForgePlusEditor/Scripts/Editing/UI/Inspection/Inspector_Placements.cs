using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // The level's placement of every type of item and monster, each in its own foldout. Types don't need objects in the
    // level to be placed (they can be added at random), so this belongs to the level rather than to its objects.
    public class Inspector_Placements : Inspector_Base
    {
        private readonly LevelEntity_Level level;
        private readonly List<Inspector_Placement> placements = new List<Inspector_Placement>();

        public Inspector_Placements(LevelEntity_Level level)
        {
            this.level = level;
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Placements";
            }
        }

        protected override object InspectedObject
        {
            get
            {
                return level;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var info = level.Level.object_placement_info;

            var itemsFoldout = Root.Find<Foldout>("items");
            for (short item = 0; item < AlephOne.items.NUMBER_OF_DEFINED_ITEMS; item++)
            {
                AddPlacement(itemsFoldout, AlephOneNames.ItemType(item), info[placement.item_placement_info + item]);
            }

            // Monster 0 (the marine) is never placed, as Aleph One clears its placement when loading
            var monstersFoldout = Root.Find<Foldout>("monsters");
            for (short monster = 1; monster < AlephOne.monsters.NUMBER_OF_MONSTER_TYPES; monster++)
            {
                AddPlacement(monstersFoldout, AlephOneNames.MonsterType(monster), info[placement.monster_placement_info + monster]);
            }
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            foreach (var placementInspector in placements)
            {
                placementInspector.Unload();
            }

            placements.Clear();
        }

        private void AddPlacement(Foldout group, string typeName, object_frequency_definition typePlacement)
        {
            var foldout = new Foldout { text = typeName, value = false };
            foldout.AddToClassList("fp-inspector-foldout");
            group.Add(foldout);

            var placementInspector = new Inspector_Placement(typePlacement);
            placementInspector.Load(foldout.contentContainer);
            placements.Add(placementInspector);
        }
    }
}
