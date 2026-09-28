// Port of Aleph One: Source_Files/GameWorld/weapons.h, weapons.cpp (weapon types and the packing of
// weapon definitions)
//
// Not ported: the player weapon state (weapon_data, trigger_data, shell_casing_data,
// player_weapon_data), weapon display, everything that runs weapons in a game, and MML parsing of
// weapons.
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.map_constructors;
using static AlephOne.Packing;
using static AlephOne.weapon_definitions;

namespace AlephOne
{
    public static class weapons
    {
        /* enums for player.c */
        /* Weapons */
        public const short _weapon_fist = 0;
        public const short _weapon_pistol = 1;
        public const short _weapon_plasma_pistol = 2;
        public const short _weapon_assault_rifle = 3;
        public const short _weapon_missile_launcher = 4;
        public const short _weapon_flamethrower = 5;
        public const short _weapon_alien_shotgun = 6;
        public const short _weapon_shotgun = 7;
        public const short _weapon_ball = 8; // or something
        // LP addition:
        public const short _weapon_smg = 9;
        public const short MAXIMUM_NUMBER_OF_WEAPONS = 10;

        public const short _weapon_doublefisted_pistols = MAXIMUM_NUMBER_OF_WEAPONS; /* This is a pseudo-weapon */
        public const short _weapon_doublefisted_shotguns = 11;
        public const short PLAYER_TORSO_SHAPE_COUNT = 12;

        // enum
        public const short _shape_weapon_idle = 0;
        public const short _shape_weapon_charging = 1;
        public const short _shape_weapon_firing = 2;
        public const short PLAYER_TORSO_WEAPON_ACTION_COUNT = 3; // ZZZ: added this one

        // enum
        public const short _primary_weapon = 0;
        public const short _secondary_weapon = 1;
        public const short NUMBER_OF_TRIGGERS = 2;

        /* weapon display positioning modes */
        public const short _position_low = 0; /* position==0 is invisible, position==FIXED_ONE is sticking out from left/bottom */
        public const short _position_center = 1; /* position==0 is off left/bottom, position==FIXED_ONE is off top/right */
        public const short _position_high = 2; /* position==0 is invisible, position==FIXED_ONE is sticking out from right/top
            (mirrored, whether you like it or not) */

        // SB: This needs to be accessed in lua_script.cpp

        public const short MAXIMUM_SHELL_CASINGS = 4;

        // For external access:
        public const int SIZEOF_weapon_definition = 134;

        public const int SIZEOF_player_weapon_data = 472;

        private static void StreamToTrigDefData(StreamPointer S, trigger_definition Object)
        {
            StreamToValue(S, out Object.rounds_per_magazine);
            StreamToValue(S, out Object.ammunition_type);
            StreamToValue(S, out Object.ticks_per_round);
            StreamToValue(S, out Object.recovery_ticks);
            StreamToValue(S, out Object.charging_ticks);
            StreamToValue(S, out Object.recoil_magnitude);
            StreamToValue(S, out Object.firing_sound);
            StreamToValue(S, out Object.click_sound);
            StreamToValue(S, out Object.charging_sound);
            StreamToValue(S, out Object.shell_casing_sound);
            StreamToValue(S, out Object.reloading_sound);
            StreamToValue(S, out Object.charged_sound);
            StreamToValue(S, out Object.projectile_type);
            StreamToValue(S, out Object.theta_error);
            StreamToValue(S, out Object.dx);
            StreamToValue(S, out Object.dz);
            StreamToValue(S, out Object.shell_casing_type);
            StreamToValue(S, out Object.burst_count);
            Object.sound_activation_range = 0;
        }

        private static void TrigDefDataToStream(StreamPointer S, trigger_definition Object)
        {
            ValueToStream(S, Object.rounds_per_magazine);
            ValueToStream(S, Object.ammunition_type);
            ValueToStream(S, Object.ticks_per_round);
            ValueToStream(S, Object.recovery_ticks);
            ValueToStream(S, Object.charging_ticks);
            ValueToStream(S, Object.recoil_magnitude);
            ValueToStream(S, Object.firing_sound);
            ValueToStream(S, Object.click_sound);
            ValueToStream(S, Object.charging_sound);
            ValueToStream(S, Object.shell_casing_sound);
            ValueToStream(S, Object.reloading_sound);
            ValueToStream(S, Object.charged_sound);
            ValueToStream(S, Object.projectile_type);
            ValueToStream(S, Object.theta_error);
            ValueToStream(S, Object.dx);
            ValueToStream(S, Object.dz);
            ValueToStream(S, Object.shell_casing_type);
            ValueToStream(S, Object.burst_count);
        }

        public static void unpack_weapon_definition(StreamPointer S, IList<weapon_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                weapon_definition ObjPtr = Objects[k];

                StreamToValue(S, out ObjPtr.item_type);
                StreamToValue(S, out ObjPtr.powerup_type);
                StreamToValue(S, out ObjPtr.weapon_class);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.firing_light_intensity);
                StreamToValue(S, out ObjPtr.firing_intensity_decay_ticks);

                StreamToValue(S, out ObjPtr.idle_height);
                StreamToValue(S, out ObjPtr.bob_amplitude);
                StreamToValue(S, out ObjPtr.kick_height);
                StreamToValue(S, out ObjPtr.reload_height);
                StreamToValue(S, out ObjPtr.idle_width);
                StreamToValue(S, out ObjPtr.horizontal_amplitude);

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.idle_shape);
                StreamToValue(S, out ObjPtr.firing_shape);
                StreamToValue(S, out ObjPtr.reloading_shape);
                StreamToValue(S, out ObjPtr.unused);
                StreamToValue(S, out ObjPtr.charging_shape);
                StreamToValue(S, out ObjPtr.charged_shape);

                StreamToValue(S, out ObjPtr.ready_ticks);
                StreamToValue(S, out ObjPtr.await_reload_ticks);
                StreamToValue(S, out ObjPtr.loading_ticks);
                StreamToValue(S, out ObjPtr.finish_loading_ticks);
                StreamToValue(S, out ObjPtr.powerup_ticks);

                for (int m = 0; m < NUMBER_OF_TRIGGERS; m++)
                    StreamToTrigDefData(S, ObjPtr.weapons_by_trigger[m]);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_weapon_definition));
        }

        public static void unpack_m1_weapon_definition(StreamPointer S, IList<weapon_definition> weapon_definitions, int Count)
        {
            for (int k = 0; k < Count; k++)
            {
                weapon_definition ObjPtr = weapon_definitions[k];

                StreamToValue(S, out ObjPtr.item_type);
                ObjPtr.powerup_type = NONE;
                StreamToValue(S, out ObjPtr.weapon_class);
                StreamToValue(S, out ObjPtr.flags);

                trigger_definition Trigger0 = ObjPtr.weapons_by_trigger[0];
                trigger_definition Trigger1 = ObjPtr.weapons_by_trigger[1];

                StreamToValue(S, out Trigger0.ammunition_type);
                StreamToValue(S, out Trigger0.rounds_per_magazine);
                StreamToValue(S, out Trigger1.ammunition_type);
                StreamToValue(S, out Trigger1.rounds_per_magazine);

                StreamToValue(S, out ObjPtr.firing_light_intensity);
                StreamToValue(S, out ObjPtr.firing_intensity_decay_ticks);

                StreamToValue(S, out ObjPtr.idle_height);
                StreamToValue(S, out ObjPtr.bob_amplitude);
                StreamToValue(S, out ObjPtr.kick_height);
                StreamToValue(S, out ObjPtr.reload_height);
                StreamToValue(S, out ObjPtr.idle_width);
                StreamToValue(S, out ObjPtr.horizontal_amplitude);

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.idle_shape);
                StreamToValue(S, out ObjPtr.firing_shape);
                StreamToValue(S, out ObjPtr.reloading_shape);
                StreamToValue(S, out ObjPtr.unused);
                StreamToValue(S, out ObjPtr.charging_shape);
                StreamToValue(S, out ObjPtr.charged_shape);

                StreamToValue(S, out Trigger0.ticks_per_round);
                StreamToValue(S, out Trigger1.ticks_per_round);

                StreamToValue(S, out ObjPtr.await_reload_ticks);
                StreamToValue(S, out ObjPtr.ready_ticks);
                ObjPtr.loading_ticks = 0;
                ObjPtr.finish_loading_ticks = 0;

                StreamToValue(S, out Trigger0.recovery_ticks);
                StreamToValue(S, out Trigger1.recovery_ticks);
                StreamToValue(S, out Trigger0.charging_ticks);
                StreamToValue(S, out Trigger1.charging_ticks);

                StreamToValue(S, out Trigger0.recoil_magnitude);
                StreamToValue(S, out Trigger1.recoil_magnitude);

                StreamToValue(S, out Trigger0.firing_sound);
                StreamToValue(S, out Trigger1.firing_sound);
                StreamToValue(S, out Trigger0.click_sound);
                StreamToValue(S, out Trigger1.click_sound);

                StreamToValue(S, out Trigger0.reloading_sound);
                Trigger1.reloading_sound = NONE;

                StreamToValue(S, out Trigger0.charging_sound);
                Trigger1.charging_sound = Trigger0.charging_sound;

                StreamToValue(S, out Trigger0.shell_casing_sound);
                StreamToValue(S, out Trigger1.shell_casing_sound);

                StreamToValue(S, out Trigger0.sound_activation_range);
                StreamToValue(S, out Trigger1.sound_activation_range);

                StreamToValue(S, out Trigger0.projectile_type);
                StreamToValue(S, out Trigger1.projectile_type);

                StreamToValue(S, out Trigger0.theta_error);
                StreamToValue(S, out Trigger1.theta_error);

                StreamToValue(S, out Trigger0.dx);
                StreamToValue(S, out Trigger0.dz);
                StreamToValue(S, out Trigger1.dx);
                StreamToValue(S, out Trigger1.dz);

                StreamToValue(S, out Trigger0.burst_count);
                StreamToValue(S, out Trigger1.burst_count);

                S.Skip(2); // instant reload tick

                Trigger0.charged_sound = NONE;
                Trigger1.charged_sound = NONE;
                Trigger0.shell_casing_type = NONE;
                Trigger1.shell_casing_type = NONE;

                if ((ObjPtr.flags & _weapon_disappears_after_use_m1) != 0)
                {
                    ObjPtr.flags |= _weapon_disappears_after_use;
                    ObjPtr.flags &= ~_weapon_disappears_after_use_m1;
                }

                if (ObjPtr.weapon_class == _twofisted_pistol_class)
                {
                    // Marathon's settings for trigger 1 are mostly empty
                    ObjPtr.flags |= _weapon_fires_out_of_phase;
                    short dx = Trigger1.dx;
                    short dz = Trigger1.dz;
                    // Trigger1 = Trigger0;
                    Trigger1 = ObjPtr.weapons_by_trigger[1] = Trigger0.Clone();
                    Trigger1.dx = dx;
                    Trigger1.dz = dz;
                }
                else if (ObjPtr.weapon_class == _dual_function_class)
                {
                    // triggers share ammo must have been
                    // hard-coded for dual function weapons in
                    // Marathon; also, Marathon 2 expects rounds
                    // per magazine and ammunition type to match
                    ObjPtr.flags |= _weapon_triggers_share_ammo;
                    Trigger1.rounds_per_magazine = Trigger0.rounds_per_magazine;
                    Trigger1.ammunition_type = Trigger0.ammunition_type;

                }

                // automatic weapons in Marathon flutter while firing
                if ((ObjPtr.flags & _weapon_is_automatic) != 0)
                {
                    ObjPtr.flags |= _weapon_flutters_while_firing;
                }

                // this makes the TOZT render correctly, but we don't
                // want it to flutter so apply after the above statement
                if (Trigger0.recovery_ticks == 0)
                {
                    ObjPtr.flags |= _weapon_is_automatic;
                }

                // SPNKR doesn't have a firing shape, just use idle
                if (ObjPtr.firing_shape == NONE)
                {
                    ObjPtr.firing_shape = ObjPtr.idle_shape;
                }

                if (k == _weapon_alien_shotgun)
                {
                    // is there a better way?
                    ObjPtr.flags |= _weapon_has_random_ammo_on_pickup;
                }

                ObjPtr.flags |= _weapon_is_marathon_1;
            }
        }

        public static void pack_weapon_definition(StreamPointer S, IList<weapon_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                weapon_definition ObjPtr = Objects[k];

                ValueToStream(S, ObjPtr.item_type);
                ValueToStream(S, ObjPtr.powerup_type);
                ValueToStream(S, ObjPtr.weapon_class);
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.firing_light_intensity);
                ValueToStream(S, ObjPtr.firing_intensity_decay_ticks);

                ValueToStream(S, ObjPtr.idle_height);
                ValueToStream(S, ObjPtr.bob_amplitude);
                ValueToStream(S, ObjPtr.kick_height);
                ValueToStream(S, ObjPtr.reload_height);
                ValueToStream(S, ObjPtr.idle_width);
                ValueToStream(S, ObjPtr.horizontal_amplitude);

                ValueToStream(S, ObjPtr.collection);
                ValueToStream(S, ObjPtr.idle_shape);
                ValueToStream(S, ObjPtr.firing_shape);
                ValueToStream(S, ObjPtr.reloading_shape);
                ValueToStream(S, ObjPtr.unused);
                ValueToStream(S, ObjPtr.charging_shape);
                ValueToStream(S, ObjPtr.charged_shape);

                ValueToStream(S, ObjPtr.ready_ticks);
                ValueToStream(S, ObjPtr.await_reload_ticks);
                ValueToStream(S, ObjPtr.loading_ticks);
                ValueToStream(S, ObjPtr.finish_loading_ticks);
                ValueToStream(S, ObjPtr.powerup_ticks);

                for (int m = 0; m < NUMBER_OF_TRIGGERS; m++)
                    TrigDefDataToStream(S, ObjPtr.weapons_by_trigger[m]);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_weapon_definition));
        }

        public static void init_weapon_definitions(weapon_definition[] weapon_definitions)
        {
            // memcpy(weapon_definitions, original_weapon_definitions, sizeof(weapon_definitions));
            for (int k = 0; k < NUMBER_OF_WEAPONS; k++)
            {
                weapon_definitions[k] = original_weapon_definitions[k].Clone();
            }
        }

        // LP additions: get weapon-definition size and number of weapon types
        public static int get_number_of_weapon_types() {return NUMBER_OF_WEAPONS;}
    }
}
