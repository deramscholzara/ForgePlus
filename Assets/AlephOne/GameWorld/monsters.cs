// Port of Aleph One: Source_Files/GameWorld/monsters.h, monsters.cpp (monster types and the packing of
// monster definitions)
//
// Not ported: monster_data and everything that runs monsters in a game (AI, pathing, damage, ...), and
// MML parsing of monster and damage-kick settings.
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.map_constructors;
using static AlephOne.monster_definitions;
using static AlephOne.Packing;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    public static class monsters
    {
        /* ---------- constants */

        public static readonly ushort FLAMING_DEAD_SHAPE = BUILD_DESCRIPTOR(_collection_rocket, 7);
        public static readonly ushort FLAMING_DYING_SHAPE = BUILD_DESCRIPTOR(_collection_rocket, 8);

        /* constants for activate_nearby_monsters */
        public const short _pass_one_zone_border = 0x0001;
        public const short _passed_zone_border = 0x0002;
        public const short _activate_invisible_monsters = 0x0004; // sound or teleport trigger
        public const short _activate_deaf_monsters = 0x0008; // i.e., trigger
        public const short _pass_solid_lines = 0x0010; // i.e., not a sound (trigger)
        public const short _use_activation_biases = 0x0020; // inactive monsters follow their editor instructions (trigger)
        public const short _activation_cannot_be_avoided = 0x0040; // cannot be suppressed because of recent activation (trigger)
        public const short _cannot_pass_superglue = 0x0080; // i.e., glue trigger
        public const short _activate_glue_monsters = 0x0100; // glue trigger

        /* activation biases are only used when the monster is activated by a trigger */
        /* activation biases (set in editor) */
        public const short _activate_on_player = 0;
        public const short _activate_on_nearest_hostile = 1;
        public const short _activate_on_goal = 2;
        public const short _activate_randomly = 3;

        /* ---------- monsters */

        /* player monsters are never active */

        /* monster types */
        public const short _monster_marine = 0;
        public const short _monster_tick_energy = 1;
        public const short _monster_tick_oxygen = 2;
        public const short _monster_tick_kamakazi = 3;
        public const short _monster_compiler_minor = 4;
        public const short _monster_compiler_major = 5;
        public const short _monster_compiler_minor_invisible = 6;
        public const short _monster_compiler_major_invisible = 7;
        public const short _monster_fighter_minor = 8;
        public const short _monster_fighter_major = 9;
        public const short _monster_fighter_minor_projectile = 10;
        public const short _monster_fighter_major_projectile = 11;
        public const short _civilian_crew = 12;
        public const short _civilian_science = 13;
        public const short _civilian_security = 14;
        public const short _civilian_assimilated = 15;
        public const short _monster_hummer_minor = 16; // slow hummer
        public const short _monster_hummer_major = 17; // fast hummer
        public const short _monster_hummer_big_minor = 18; // big hummer
        public const short _monster_hummer_big_major = 19; // angry hummer
        public const short _monster_hummer_possessed = 20; // hummer from durandal
        public const short _monster_cyborg_minor = 21;
        public const short _monster_cyborg_major = 22;
        public const short _monster_cyborg_flame_minor = 23;
        public const short _monster_cyborg_flame_major = 24;
        public const short _monster_enforcer_minor = 25;
        public const short _monster_enforcer_major = 26;
        public const short _monster_hunter_minor = 27;
        public const short _monster_hunter_major = 28;
        public const short _monster_trooper_minor = 29;
        public const short _monster_trooper_major = 30;
        public const short _monster_mother_of_all_cyborgs = 31;
        public const short _monster_mother_of_all_hunters = 32;
        public const short _monster_sewage_yeti = 33;
        public const short _monster_water_yeti = 34;
        public const short _monster_lava_yeti = 35;
        public const short _monster_defender_minor = 36;
        public const short _monster_defender_major = 37;
        public const short _monster_juggernaut_minor = 38;
        public const short _monster_juggernaut_major = 39;
        public const short _monster_tiny_fighter = 40;
        public const short _monster_tiny_bob = 41;
        public const short _monster_tiny_yeti = 42;
        // LP addition:
        public const short _civilian_fusion_crew = 43;
        public const short _civilian_fusion_science = 44;
        public const short _civilian_fusion_security = 45;
        public const short _civilian_fusion_assimilated = 46;
        public const short NUMBER_OF_MONSTER_TYPES = 47;

        /* monster actions */
        public const short _monster_is_stationary = 0;
        public const short _monster_is_waiting_to_attack_again = 1;
        public const short _monster_is_moving = 2;
        public const short _monster_is_attacking_close = 3; /* melee */
        public const short _monster_is_attacking_far = 4; /* ranged */
        public const short _monster_is_being_hit = 5;
        public const short _monster_is_dying_hard = 6;
        public const short _monster_is_dying_soft = 7;
        public const short _monster_is_dying_flaming = 8;
        public const short _monster_is_teleporting = 9; // transparent
        public const short _monster_is_teleporting_in = 10;
        public const short _monster_is_teleporting_out = 11;
        public const short NUMBER_OF_MONSTER_ACTIONS = 12;

        /* monster modes */
        public const short _monster_locked = 0;
        public const short _monster_losing_lock = 1;
        public const short _monster_lost_lock = 2;
        public const short _monster_unlocked = 3;
        public const short _monster_running = 4;
        public const short NUMBER_OF_MONSTER_MODES = 5;

        /* monster flags */
        public const short _monster_was_promoted = 0x1;
        public const short _monster_was_demoted = 0x2;
        public const short _monster_has_never_been_activated = 0x4;
        public const short _monster_is_blind = 0x8;
        public const short _monster_is_deaf = 0x10;
        public const short _monster_teleports_out_when_deactivated = 0x20;

        public const int SIZEOF_monster_data = 64;

        public const int SIZEOF_monster_definition = 156;

        private static void StreamToAttackDef(StreamPointer S, attack_definition Object)
        {
            StreamToValue(S, out Object.type);
            StreamToValue(S, out Object.repetitions);
            StreamToValue(S, out Object.error);
            StreamToValue(S, out Object.range);
            StreamToValue(S, out Object.attack_shape);

            StreamToValue(S, out Object.dx);
            StreamToValue(S, out Object.dy);
            StreamToValue(S, out Object.dz);
        }

        private static void AttackDefToStream(StreamPointer S, attack_definition Object)
        {
            ValueToStream(S, Object.type);
            ValueToStream(S, Object.repetitions);
            ValueToStream(S, Object.error);
            ValueToStream(S, Object.range);
            ValueToStream(S, Object.attack_shape);

            ValueToStream(S, Object.dx);
            ValueToStream(S, Object.dy);
            ValueToStream(S, Object.dz);
        }

        public static void unpack_monster_definition(StreamPointer S, IList<monster_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                monster_definition ObjPtr = Objects[k];

                StreamToValue(S, out ObjPtr.collection);

                StreamToValue(S, out ObjPtr.vitality);
                StreamToValue(S, out ObjPtr.immunities);
                StreamToValue(S, out ObjPtr.weaknesses);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr._class);
                StreamToValue(S, out ObjPtr.friends);
                StreamToValue(S, out ObjPtr.enemies);

                StreamToValue(S, out ObjPtr.sound_pitch);
                StreamToValue(S, out ObjPtr.activation_sound);
                StreamToValue(S, out ObjPtr.friendly_activation_sound);
                StreamToValue(S, out ObjPtr.clear_sound);
                StreamToValue(S, out ObjPtr.kill_sound);
                StreamToValue(S, out ObjPtr.apology_sound);
                StreamToValue(S, out ObjPtr.friendly_fire_sound);
                StreamToValue(S, out ObjPtr.flaming_sound);
                StreamToValue(S, out ObjPtr.random_sound);
                StreamToValue(S, out ObjPtr.random_sound_mask);

                StreamToValue(S, out ObjPtr.carrying_item_type);

                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.height);
                StreamToValue(S, out ObjPtr.preferred_hover_height);
                StreamToValue(S, out ObjPtr.minimum_ledge_delta);
                StreamToValue(S, out ObjPtr.maximum_ledge_delta);
                StreamToValue(S, out ObjPtr.external_velocity_scale);
                StreamToValue(S, out ObjPtr.impact_effect);
                StreamToValue(S, out ObjPtr.melee_impact_effect);
                StreamToValue(S, out ObjPtr.contrail_effect);

                StreamToValue(S, out ObjPtr.half_visual_arc);
                StreamToValue(S, out ObjPtr.half_vertical_visual_arc);
                StreamToValue(S, out ObjPtr.visual_range);
                StreamToValue(S, out ObjPtr.dark_visual_range);
                StreamToValue(S, out ObjPtr.intelligence);
                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.gravity);
                StreamToValue(S, out ObjPtr.terminal_velocity);
                StreamToValue(S, out ObjPtr.door_retry_mask);
                StreamToValue(S, out ObjPtr.shrapnel_radius);
                unpack_damage_definition(S, new[] { ObjPtr.shrapnel_damage }, 1);

                StreamToValue(S, out ObjPtr.hit_shapes);
                StreamToValue(S, out ObjPtr.hard_dying_shape);
                StreamToValue(S, out ObjPtr.soft_dying_shape);
                StreamToValue(S, out ObjPtr.hard_dead_shapes);
                StreamToValue(S, out ObjPtr.soft_dead_shapes);
                StreamToValue(S, out ObjPtr.stationary_shape);
                StreamToValue(S, out ObjPtr.moving_shape);
                StreamToValue(S, out ObjPtr.teleport_in_shape);
                StreamToValue(S, out ObjPtr.teleport_out_shape);

                StreamToValue(S, out ObjPtr.attack_frequency);
                StreamToAttackDef(S, ObjPtr.melee_attack);
                StreamToAttackDef(S, ObjPtr.ranged_attack);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_monster_definition));
        }

        public static void unpack_m1_monster_definition(StreamPointer S, IList<monster_definition> monster_definitions, int Count)
        {
            for (int k = 0; k < Count; k++)
            {
                monster_definition ObjPtr = monster_definitions[k];

                StreamToValue(S, out ObjPtr.collection);

                StreamToValue(S, out ObjPtr.vitality);
                StreamToValue(S, out ObjPtr.immunities);
                StreamToValue(S, out ObjPtr.weaknesses);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr._class);
                StreamToValue(S, out ObjPtr.friends);
                StreamToValue(S, out ObjPtr.enemies);

                ObjPtr.sound_pitch = FIXED_ONE;
                StreamToValue(S, out ObjPtr.activation_sound);
                S.Skip(2); // ignore conversation sound

                // Marathon doesn't have these
                ObjPtr.friendly_activation_sound = NONE;
                ObjPtr.clear_sound = NONE;
                ObjPtr.kill_sound = NONE;
                ObjPtr.apology_sound = NONE;
                ObjPtr.friendly_fire_sound = NONE;

                StreamToValue(S, out ObjPtr.flaming_sound);
                StreamToValue(S, out ObjPtr.random_sound);
                StreamToValue(S, out ObjPtr.random_sound_mask);

                StreamToValue(S, out ObjPtr.carrying_item_type);

                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.height);
                StreamToValue(S, out ObjPtr.preferred_hover_height);
                StreamToValue(S, out ObjPtr.minimum_ledge_delta);
                StreamToValue(S, out ObjPtr.maximum_ledge_delta);
                StreamToValue(S, out ObjPtr.external_velocity_scale);

                StreamToValue(S, out ObjPtr.impact_effect);
                StreamToValue(S, out ObjPtr.melee_impact_effect);
                ObjPtr.contrail_effect = NONE;

                StreamToValue(S, out ObjPtr.half_visual_arc);
                StreamToValue(S, out ObjPtr.half_vertical_visual_arc);
                StreamToValue(S, out ObjPtr.visual_range);
                StreamToValue(S, out ObjPtr.dark_visual_range);
                StreamToValue(S, out ObjPtr.intelligence);
                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.gravity);
                StreamToValue(S, out ObjPtr.terminal_velocity);
                StreamToValue(S, out ObjPtr.door_retry_mask);
                StreamToValue(S, out ObjPtr.shrapnel_radius);

                unpack_damage_definition(S, new[] { ObjPtr.shrapnel_damage }, 1);

                StreamToValue(S, out ObjPtr.hit_shapes);
                StreamToValue(S, out ObjPtr.hard_dying_shape);
                StreamToValue(S, out ObjPtr.soft_dying_shape);
                StreamToValue(S, out ObjPtr.hard_dead_shapes);
                StreamToValue(S, out ObjPtr.soft_dead_shapes);
                StreamToValue(S, out ObjPtr.stationary_shape);
                StreamToValue(S, out ObjPtr.moving_shape);

                ObjPtr.teleport_in_shape = ObjPtr.stationary_shape;
#pragma warning disable CS1717 // sic
                ObjPtr.teleport_out_shape = ObjPtr.teleport_out_shape;
#pragma warning restore CS1717

                StreamToValue(S, out ObjPtr.attack_frequency);
                StreamToAttackDef(S, ObjPtr.melee_attack);
                StreamToAttackDef(S, ObjPtr.ranged_attack);

                ObjPtr.flags |= _monster_weaknesses_cause_soft_death;
                ObjPtr.flags |= _monster_screams_when_crushed;
                ObjPtr.flags |= _monster_makes_sound_when_activated;
                ObjPtr.flags |= _monster_can_grenade_climb;
            }
        }

        public static void pack_monster_definition(StreamPointer S, IList<monster_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                monster_definition ObjPtr = Objects[k];

                ValueToStream(S, ObjPtr.collection);

                ValueToStream(S, ObjPtr.vitality);
                ValueToStream(S, ObjPtr.immunities);
                ValueToStream(S, ObjPtr.weaknesses);
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr._class);
                ValueToStream(S, ObjPtr.friends);
                ValueToStream(S, ObjPtr.enemies);

                ValueToStream(S, ObjPtr.sound_pitch);
                ValueToStream(S, ObjPtr.activation_sound);
                ValueToStream(S, ObjPtr.friendly_activation_sound);
                ValueToStream(S, ObjPtr.clear_sound);
                ValueToStream(S, ObjPtr.kill_sound);
                ValueToStream(S, ObjPtr.apology_sound);
                ValueToStream(S, ObjPtr.friendly_fire_sound);
                ValueToStream(S, ObjPtr.flaming_sound);
                ValueToStream(S, ObjPtr.random_sound);
                ValueToStream(S, ObjPtr.random_sound_mask);

                ValueToStream(S, ObjPtr.carrying_item_type);

                ValueToStream(S, ObjPtr.radius);
                ValueToStream(S, ObjPtr.height);
                ValueToStream(S, ObjPtr.preferred_hover_height);
                ValueToStream(S, ObjPtr.minimum_ledge_delta);
                ValueToStream(S, ObjPtr.maximum_ledge_delta);
                ValueToStream(S, ObjPtr.external_velocity_scale);
                ValueToStream(S, ObjPtr.impact_effect);
                ValueToStream(S, ObjPtr.melee_impact_effect);
                ValueToStream(S, ObjPtr.contrail_effect);

                ValueToStream(S, ObjPtr.half_visual_arc);
                ValueToStream(S, ObjPtr.half_vertical_visual_arc);
                ValueToStream(S, ObjPtr.visual_range);
                ValueToStream(S, ObjPtr.dark_visual_range);
                ValueToStream(S, ObjPtr.intelligence);
                ValueToStream(S, ObjPtr.speed);
                ValueToStream(S, ObjPtr.gravity);
                ValueToStream(S, ObjPtr.terminal_velocity);
                ValueToStream(S, ObjPtr.door_retry_mask);
                ValueToStream(S, ObjPtr.shrapnel_radius);
                pack_damage_definition(S, new[] { ObjPtr.shrapnel_damage }, 1);

                ValueToStream(S, ObjPtr.hit_shapes);
                ValueToStream(S, ObjPtr.hard_dying_shape);
                ValueToStream(S, ObjPtr.soft_dying_shape);
                ValueToStream(S, ObjPtr.hard_dead_shapes);
                ValueToStream(S, ObjPtr.soft_dead_shapes);
                ValueToStream(S, ObjPtr.stationary_shape);
                ValueToStream(S, ObjPtr.moving_shape);
                ValueToStream(S, ObjPtr.teleport_in_shape);
                ValueToStream(S, ObjPtr.teleport_out_shape);

                ValueToStream(S, ObjPtr.attack_frequency);
                AttackDefToStream(S, ObjPtr.melee_attack);
                AttackDefToStream(S, ObjPtr.ranged_attack);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_monster_definition));
        }

        public static void init_monster_definitions(monster_definition[] monster_definitions)
        {
            // memcpy(monster_definitions, original_monster_definitions, sizeof(monster_definitions));
            for (int k = 0; k < NUMBER_OF_MONSTER_TYPES; k++)
            {
                monster_definitions[k] = original_monster_definitions[k].Clone();
            }
        }

        private static monster_definition get_monster_definition(monster_definition[] monster_definitions, short type)
        {
            monster_definition definition = GetMemberWithBounds(monster_definitions, type, NUMBER_OF_MONSTER_TYPES);
            assert(definition != null);

            return definition;
        }

        //a non-inlined version for external use
        public static monster_definition get_monster_definition_external(monster_definition[] monster_definitions, short type)
        {
            return get_monster_definition(monster_definitions, type);
        }
    }
}
