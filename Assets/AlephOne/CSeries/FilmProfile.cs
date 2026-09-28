// Port of Aleph One: Source_Files/CSeries/FilmProfile.h, FilmProfile.cpp
//
// Map loading reads film_profile.m1_object_unused, and the redundant-data code reads
// adjacent_polygons_always_intersect, long_distance_physics and m1_platform_flood.
namespace AlephOne
{
    // Film profiles tell Aleph One exactly how to behave when playing back films
    public class FilmProfile
    {
        // some LP bug fix
        public bool keyframe_fix;

        // ghs changed this so that suiciding in tag made you it
        public bool damage_aggressor_last_in_tag;

        // some LP bug fix
        public bool swipe_nearby_items_fix;

        // ghs added this to stop monsters from spawning in net games
        public bool initial_monster_fix;

        // LP changed arctangent and distance2d to handle long distances
        public bool long_distance_physics;

        // LP added to support animated items
        public bool animate_items;

        // astrange changed M2's fast PIN to a new version which
        // doesn't shortcircuit and returns a different result when
        // floor/ceiling are reversed
        public bool inexplicable_pin_change;

        // LP increased the dynamic limits, which persisted through 1.0
        public bool increased_dynamic_limits_1_0;

        // 1.1 reverted number of paths to preserve original AI behavior
        public bool increased_dynamic_limits_1_1;

        // Infinity has an improved line_is_obstructed
        public bool line_is_obstructed_fix;

        // Aleph One implements pass_media_boundary one way
        public bool a1_smg;

        // Marathon Infinity implements pass_media_boundary a different way
        public bool infinity_smg;

        // Marathon 2 and Infinity give fusion damage a vertical
        // component when delta_vitality is greater than 100
        public bool use_vertical_kick_threshold;

        // Marathon Infinity fixes tag suicides in a different way
        public bool infinity_tag_fix;

        // Marathon Infinity always adds adjacent polygons to the
        // intersecting indexes (unmerged maps only)
        public bool adjacent_polygons_always_intersect;

        // Aleph One moved object initialization to improve Lua access
        public bool early_object_initialization;

        // Aleph One 1.1 fixes
        public bool fix_sliding_on_platforms;
        public bool prevent_dead_projectile_owners;
        public bool validate_random_ranged_attack;
        public bool allow_short_kamikaze;
        public bool ketchup_fix;
        public bool lua_increments_rng;
        public bool destroy_players_ball_fix;
        public bool calculate_terminal_lines_correctly;
        public bool key_frame_zero_shrapnel_fix; // for M1 lookers and simulacra
        public bool count_dead_dropped_items_correctly;

        // Aleph One 1.2 fixes
        public bool m1_low_gravity_projectiles;
        public bool m1_buggy_repair_goal;
        public bool find_action_key_target_has_side_effects;

        // Aleph One 1.3 fixes
        public bool m1_object_unused; // location.z and flags are unused in Marathon
        public bool m1_platform_flood; // checks more than just adjacent polygons
        public bool m1_teleport_without_delay; // Marathon terminals teleport immediately

        // Aleph One 1.4 fixes
        public bool better_terminal_word_wrap; // fixes rare infinity films
        public bool lua_monster_killed_trigger_fix;

        // Aleph One 1.7 fixes
        public bool chip_insertion_ignores_tag_state; // always toggle when using a chip
        public bool page_up_past_full_width_term_pict;
        public bool fix_destroy_scenery_random_frame;
        public bool m1_reload_sound; // play the reload sound on the key frame
        public bool m1_landscape_effects; // projectiles detonate on M1 landscapes
        public bool m1_bce_pickup; // you can pick up another BCE if you already have one

        // Aleph One 1.11 fixes
        public bool network_items; // early creation of players on new games to fix network items spawn
        public bool hotkey_fix; // hotkeys no longer work when player is dead or carrying the ball
        public bool finally_respawn; // the player can respawn after 15 seconds regardless of being stationary
        public bool overhead_map_terminal; // allow to enter terminals with the overhead map active


        public FilmProfile Clone() { return (FilmProfile) MemberwiseClone(); }
    }

    public enum FilmProfileType
    {
        FILM_PROFILE_ALEPH_ONE_1_0,
        FILM_PROFILE_MARATHON_2,
        FILM_PROFILE_MARATHON_INFINITY,
        FILM_PROFILE_ALEPH_ONE_1_1,
        FILM_PROFILE_ALEPH_ONE_1_2,
        FILM_PROFILE_ALEPH_ONE_1_3,
        FILM_PROFILE_ALEPH_ONE_1_4,
        FILM_PROFILE_ALEPH_ONE_1_7,
        FILM_PROFILE_DEFAULT,
        //!\\ if you add a film profile, make sure to connect it in interface.cpp!
    }

    // FilmProfile.cpp
    public static class FilmProfileGlobals
    {
        private static readonly FilmProfile alephone1_11 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = true,
            m1_buggy_repair_goal = true,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = true,
            m1_platform_flood = true,
            m1_teleport_without_delay = true,
            better_terminal_word_wrap = true,
            lua_monster_killed_trigger_fix = true,
            chip_insertion_ignores_tag_state = true,
            page_up_past_full_width_term_pict = true,
            fix_destroy_scenery_random_frame = true,
            m1_reload_sound = true,
            m1_landscape_effects = true,
            m1_bce_pickup = true,
            network_items = true,
            hotkey_fix = true,
            finally_respawn = true,
            overhead_map_terminal = true,
        };

        private static readonly FilmProfile alephone1_7 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = true,
            m1_buggy_repair_goal = true,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = true,
            m1_platform_flood = true,
            m1_teleport_without_delay = true,
            better_terminal_word_wrap = true,
            lua_monster_killed_trigger_fix = true,
            chip_insertion_ignores_tag_state = true,
            page_up_past_full_width_term_pict = true,
            fix_destroy_scenery_random_frame = true,
            m1_reload_sound = true,
            m1_landscape_effects = true,
            m1_bce_pickup = true,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile alephone1_4 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = true,
            m1_buggy_repair_goal = true,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = true,
            m1_platform_flood = true,
            m1_teleport_without_delay = true,
            better_terminal_word_wrap = true,
            lua_monster_killed_trigger_fix = true,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile alephone1_3 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = true,
            m1_buggy_repair_goal = true,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = true,
            m1_platform_flood = true,
            m1_teleport_without_delay = true,
            better_terminal_word_wrap = false,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile alephone1_2 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = true,
            m1_buggy_repair_goal = true,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = false,
            m1_platform_flood = false,
            m1_teleport_without_delay = false,
            better_terminal_word_wrap = false,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile alephone1_1 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = true,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = true,
            fix_sliding_on_platforms = true,
            prevent_dead_projectile_owners = true,
            validate_random_ranged_attack = true,
            allow_short_kamikaze = true,
            ketchup_fix = true,
            lua_increments_rng = false,
            destroy_players_ball_fix = true,
            calculate_terminal_lines_correctly = true,
            key_frame_zero_shrapnel_fix = true,
            count_dead_dropped_items_correctly = true,
            m1_low_gravity_projectiles = false,
            m1_buggy_repair_goal = false,
            find_action_key_target_has_side_effects = true,
            m1_object_unused = false,
            m1_platform_flood = false,
            m1_teleport_without_delay = false,
            better_terminal_word_wrap = false,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile alephone1_0 = new FilmProfile
        {
            keyframe_fix = true,
            damage_aggressor_last_in_tag = true,
            swipe_nearby_items_fix = true,
            initial_monster_fix = true,
            long_distance_physics = true,
            animate_items = true,
            inexplicable_pin_change = true,
            increased_dynamic_limits_1_0 = true,
            increased_dynamic_limits_1_1 = false,
            line_is_obstructed_fix = false,
            a1_smg = true,
            infinity_smg = false,
            use_vertical_kick_threshold = false,
            infinity_tag_fix = false,
            adjacent_polygons_always_intersect = false,
            early_object_initialization = true,
            fix_sliding_on_platforms = false,
            prevent_dead_projectile_owners = false,
            validate_random_ranged_attack = false,
            allow_short_kamikaze = false,
            ketchup_fix = false,
            lua_increments_rng = true,
            destroy_players_ball_fix = false,
            calculate_terminal_lines_correctly = false,
            key_frame_zero_shrapnel_fix = false,
            count_dead_dropped_items_correctly = false,
            m1_low_gravity_projectiles = false,
            m1_buggy_repair_goal = false,
            find_action_key_target_has_side_effects = true,
            m1_object_unused = false,
            m1_platform_flood = false,
            m1_teleport_without_delay = false,
            better_terminal_word_wrap = false,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile marathon2 = new FilmProfile
        {
            keyframe_fix = false,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = false,
            initial_monster_fix = false,
            long_distance_physics = false,
            animate_items = false,
            inexplicable_pin_change = false,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = false,
            line_is_obstructed_fix = false,
            a1_smg = false,
            infinity_smg = false,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = false,
            adjacent_polygons_always_intersect = false,
            early_object_initialization = false,
            fix_sliding_on_platforms = false,
            prevent_dead_projectile_owners = false,
            validate_random_ranged_attack = false,
            allow_short_kamikaze = false,
            ketchup_fix = false,
            lua_increments_rng = false,
            destroy_players_ball_fix = false,
            calculate_terminal_lines_correctly = false,
            key_frame_zero_shrapnel_fix = false,
            count_dead_dropped_items_correctly = false,
            m1_low_gravity_projectiles = false,
            m1_buggy_repair_goal = false,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = false,
            m1_platform_flood = false,
            m1_teleport_without_delay = false,
            better_terminal_word_wrap = true,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        private static readonly FilmProfile marathon_infinity = new FilmProfile
        {
            keyframe_fix = false,
            damage_aggressor_last_in_tag = false,
            swipe_nearby_items_fix = false,
            initial_monster_fix = true,
            long_distance_physics = false,
            animate_items = false,
            inexplicable_pin_change = false,
            increased_dynamic_limits_1_0 = false,
            increased_dynamic_limits_1_1 = false,
            line_is_obstructed_fix = true,
            a1_smg = false,
            infinity_smg = true,
            use_vertical_kick_threshold = true,
            infinity_tag_fix = true,
            adjacent_polygons_always_intersect = true,
            early_object_initialization = false,
            fix_sliding_on_platforms = false,
            prevent_dead_projectile_owners = false,
            validate_random_ranged_attack = false,
            allow_short_kamikaze = false,
            ketchup_fix = false,
            lua_increments_rng = false,
            destroy_players_ball_fix = false,
            calculate_terminal_lines_correctly = false,
            key_frame_zero_shrapnel_fix = false,
            count_dead_dropped_items_correctly = false,
            m1_low_gravity_projectiles = false,
            m1_buggy_repair_goal = false,
            find_action_key_target_has_side_effects = false,
            m1_object_unused = false,
            m1_platform_flood = false,
            m1_teleport_without_delay = false,
            better_terminal_word_wrap = true,
            lua_monster_killed_trigger_fix = false,
            chip_insertion_ignores_tag_state = false,
            page_up_past_full_width_term_pict = false,
            fix_destroy_scenery_random_frame = false,
            m1_reload_sound = false,
            m1_landscape_effects = false,
            m1_bce_pickup = false,
            network_items = false,
            hotkey_fix = false,
            finally_respawn = false,
            overhead_map_terminal = false,
        };

        public static FilmProfile film_profile = alephone1_11.Clone();

        public static void load_film_profile(FilmProfileType type)
        {
            switch (type)
            {
                case FilmProfileType.FILM_PROFILE_DEFAULT:
                    film_profile = alephone1_11.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_MARATHON_2:
                    film_profile = marathon2.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_MARATHON_INFINITY:
                    film_profile = marathon_infinity.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_0:
                    film_profile = alephone1_0.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_1:
                    film_profile = alephone1_1.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_2:
                    film_profile = alephone1_2.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_3:
                    film_profile = alephone1_3.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_4:
                    film_profile = alephone1_4.Clone();
                    break;
                case FilmProfileType.FILM_PROFILE_ALEPH_ONE_1_7:
                    film_profile = alephone1_7.Clone();
                    break;
            }
        }
    }
}
