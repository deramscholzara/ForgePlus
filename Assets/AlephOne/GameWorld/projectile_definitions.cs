// Port of Aleph One: Source_Files/GameWorld/projectile_definitions.h
using Unity.Scripting.LifecycleManagement;
using static AlephOne.cstypes;
using static AlephOne.effects;
using static AlephOne.map;
using static AlephOne.media;
using static AlephOne.projectiles;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- structures */

    public class projectile_definition
    {
        public short collection, shape; /* collection can be NONE (invisible) */
        public short detonation_effect, media_detonation_effect;
        public short contrail_effect, ticks_between_contrails, maximum_contrails; /* maximum of NONE is infinite */
        public short media_projectile_promotion;

        public short radius; /* can be zero and will still hit */
        public short area_of_effect; /* one target if ==0 */
        public damage_definition damage = new damage_definition();

        public uint flags;

        public short speed;
        public short maximum_range;

        public int sound_pitch;
        public short flyby_sound, rebound_sound;

        public projectile_definition Clone()
        {
            var copy = (projectile_definition) MemberwiseClone();
            copy.damage = damage.Clone();
            return copy;
        }
    }

    [NoAutoStaticsCleanup]
    public static class projectile_definitions
    {
        /* ---------- constants */

        /* projectile flags */
        public const int _guided = 0x0001;
        public const int _stop_when_animation_loops = 0x0002;
        public const int _persistent = 0x0004; /* does stops doing damage and stops moving against a target, but doesn't vanish */
        public const int _alien_projectile = 0x0008; /* does less damage and moves slower on lower levels */
        public const int _affected_by_gravity = 0x0010;
        public const int _no_horizontal_error = 0x0020;
        public const int _no_vertical_error = 0x0040;
        public const int _can_toggle_control_panels = 0x0080;
        public const int _positive_vertical_error = 0x0100;
        public const int _melee_projectile = 0x0200; /* can use a monster’s custom melee detonation */
        public const int _persistent_and_virulent = 0x0400; /* keeps moving and doing damage after a successful hit */
        public const int _usually_pass_transparent_side = 0x0800;
        public const int _sometimes_pass_transparent_side = 0x1000;
        public const int _doubly_affected_by_gravity = 0x2000;
        public const int _rebounds_from_floor = 0x4000; /* unless v.z<kvzMIN */
        public const int _penetrates_media = 0x8000; /* huh uh huh ... i said penetrate */
        public const int _becomes_item_on_detonation = 0x10000; /* item type in .permutation field of projectile */
        public const int _bleeding_projectile = 0x20000; /* can use a monster’s custom bleeding detonation */
        public const int _horizontal_wander = 0x40000; /* random horizontal error perpendicular to direction of movement */
        public const int _vertical_wander = 0x80000; /* random vertical movement perpendicular to direction of movement */
        public const int _affected_by_half_gravity = 0x100000;
        public const int _penetrates_media_boundary = 0x200000; // Can enter/exit liquids
        public const int _passes_through_objects = 0x400000; // and does no damage as it passes

        /* ---------- projectile definitions */

        public static readonly projectile_definition[] original_projectile_definitions = new projectile_definition[NUMBER_OF_PROJECTILE_TYPES]
        {
            new projectile_definition /* player’s rocket */
            {
                collection = _collection_rocket, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_rocket_explosion, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_rocket_contrail, ticks_between_contrails = 1, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = WORLD_ONE+WORLD_ONE_HALF, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = 0, @base = 250, random = 50}, /* damage */

                flags = _can_toggle_control_panels|_guided, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_rocket_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* player’s grenade */
            {
                collection = _collection_rocket, shape = 3, /* collection number, shape number */
                detonation_effect = _effect_grenade_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_grenade_contrail, ticks_between_contrails = 1, maximum_contrails = 8, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = WORLD_THREE_FOURTHS, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = 0, @base = 80, random = 20}, /* damage */

                flags = _affected_by_gravity|_can_toggle_control_panels, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_grenade_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* player’s pistol bullet */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_bullet_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_projectile, flags = 0, @base = 20, random = 8}, /* damage */

                flags = _bleeding_projectile|_usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* player’s rifle bullet */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_bullet_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_projectile, flags = 0, @base = 9, random = 6}, /* damage */

                flags = _bleeding_projectile|_usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* player’s shotgun bullet */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_bullet_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_shotgun_projectile, flags = 0, @base = 20, random = 4}, /* damage */

                flags = _bleeding_projectile|_can_toggle_control_panels|_usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* electrical melee staff */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_fighter_melee_detonation, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_electrical_staff, flags = _alien_damage, @base = 20, random = 5}, /* damage */

                flags = _sometimes_pass_transparent_side|_alien_projectile|_melee_projectile|_penetrates_media, /* flags */

                speed = WORLD_ONE_HALF, /* speed */
                maximum_range = WORLD_ONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* electrical melee staff projectile */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 2), shape = 9, /* collection number, shape number */
                detonation_effect = _effect_fighter_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_electrical_staff, flags = _alien_damage, @base = 30, random = 5}, /* damage */

                flags = _sometimes_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_fighter_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* player’s flame thrower burst */
            {
                collection = _collection_rocket, shape = 6, /* collection number, shape number */
                detonation_effect = NONE, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/3, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_flame, flags = 0, @base = 8, random = 4}, /* damage */

                flags = _sometimes_pass_transparent_side|_stop_when_animation_loops|_persistent, /* flags */

                speed = WORLD_ONE/3, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_compiler_bolt_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 0), shape = 4, /* collection number, shape number */
                detonation_effect = _effect_compiler_bolt_minor_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_compiler_bolt, flags = _alien_damage, @base = 40, random = 10}, /* damage */

                flags = _sometimes_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_compiler_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_compiler_bolt_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 1), shape = 4, /* collection number, shape number */
                detonation_effect = _effect_compiler_bolt_major_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_compiler_bolt_major_contrail, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_compiler_bolt, flags = _alien_damage, @base = 40, random = 10}, /* damage */

                flags = _sometimes_pass_transparent_side|_alien_projectile|_guided, /* flags */

                speed = WORLD_ONE/12, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _higher_frequency, /* sound pitch */
                flyby_sound = _snd_compiler_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* alien weapon */
            {
                collection = _collection_rocket, shape = 22, /* collection number, shape number */
                detonation_effect = _effect_alien_weapon_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/10, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_alien_projectile, flags = _alien_damage, @base = 20, random = 8}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile|_can_toggle_control_panels, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _snd_enforcer_projectile_flyby, flyby_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_fusion_minor */
            {
                collection = _collection_rocket, shape = 11, /* collection number, shape number */
                detonation_effect = _effect_minor_fusion_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = _projectile_minor_fusion_dispersal, /* media projectile promotion */

                radius = WORLD_ONE/20, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_fusion_bolt, flags = 0, @base = 30, random = 10}, /* damage */

                flags = _usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_fusion_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_fusion_major */
            {
                collection = _collection_rocket, shape = 12, /* collection number, shape number */
                detonation_effect = _effect_major_fusion_detonation, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_major_fusion_contrail, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = _projectile_major_fusion_dispersal, /* media projectile promotion */

                radius = WORLD_ONE/10, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_fusion_bolt, flags = 0, @base = 80, random = 20}, /* damage */

                flags = _sometimes_pass_transparent_side|_can_toggle_control_panels, /* flags */

                speed = WORLD_ONE/3, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _higher_frequency, /* sound pitch */
                flyby_sound = _snd_fusion_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_hunter */
            {
                collection = (short) BUILD_COLLECTION(_collection_hunter, 0), shape = 5, /* collection number, shape number */
                detonation_effect = _effect_hunter_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_hunter_bolt, flags = 0, @base = 15, random = 5}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_hunter_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_fist */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_fist_detonation, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/4, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_fist, flags = 0, @base = 50, random = 10}, /* damage (will be scaled by player’s velocity) */

                flags = _usually_pass_transparent_side|_can_toggle_control_panels|_melee_projectile|_penetrates_media, /* flags */

                speed = (3*WORLD_ONE)/4, /* speed */
                maximum_range = (3*WORLD_ONE)/4, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_armageddon_sphere */
            {
                collection = 0
            },

            new projectile_definition /* _projectile_armageddon_electricity */
            {
                collection = 0
            },

            new projectile_definition /* _projectile_juggernaut_rocket */
            {
                collection = _collection_rocket, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_rocket_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_rocket_contrail, ticks_between_contrails = 1, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = WORLD_ONE+WORLD_ONE_HALF, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 250, random = 50}, /* damage */

                flags = _guided|_can_toggle_control_panels, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_trooper_bullet */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_bullet_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_projectile, flags = _alien_damage, @base = 15, random = 4}, /* damage */

                flags = _bleeding_projectile|_usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_trooper_grenade */
            {
                collection = _collection_trooper, shape = 5, /* collection number, shape number */
                detonation_effect = _effect_grenade_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_grenade_contrail, ticks_between_contrails = 1, maximum_contrails = 8, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = WORLD_THREE_FOURTHS, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 40, random = 20}, /* damage */

                flags = _affected_by_gravity|_can_toggle_control_panels, /* flags */

                speed = WORLD_ONE/5, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_minor_defender */
            {
                collection = (short) BUILD_COLLECTION(_collection_defender, 0), shape = 4, /* collection number, shape number */
                detonation_effect = _effect_minor_defender_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/4, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_defender, flags = 0, @base = 30, random = 8}, /* damage */

                flags = _usually_pass_transparent_side, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_defender_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_major_defender */
            {
                collection = (short) BUILD_COLLECTION(_collection_defender, 1), shape = 4, /* collection number, shape number */
                detonation_effect = _effect_major_defender_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/4, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_defender, flags = 0, @base = 30, random = 8}, /* damage */

                flags = _usually_pass_transparent_side|_guided, /* flags */

                speed = WORLD_ONE/6, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _higher_frequency, /* sound pitch */
                flyby_sound = _snd_defender_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_juggernaut_missile */
            {
                collection = _collection_juggernaut, shape = 4, /* collection number, shape number */
                detonation_effect = _effect_grenade_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_juggernaut_missile_contrail, ticks_between_contrails = 2, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = WORLD_THREE_FOURTHS, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 40, random = 20}, /* damage */

                flags = _affected_by_half_gravity|_can_toggle_control_panels|_guided|_positive_vertical_error, /* flags */

                speed = WORLD_ONE/5, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_minor_energy_drain */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = NONE, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_energy_drain, flags = 0, @base = 4, random = 0}, /* damage (will be scaled by player’s velocity) */

                flags = _melee_projectile|_penetrates_media, /* flags */

                speed = (3*WORLD_ONE)/4, /* speed */
                maximum_range = (3*WORLD_ONE)/4, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_major_energy_drain */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = NONE, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_energy_drain, flags = 0, @base = 8, random = 0}, /* damage (will be scaled by player’s velocity) */

                flags = _melee_projectile|_penetrates_media, /* flags */

                speed = (3*WORLD_ONE)/4, /* speed */
                maximum_range = (3*WORLD_ONE)/4, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_oxygen_drain */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = NONE, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_oxygen_drain, flags = 0, @base = 4, random = 0}, /* damage (will be scaled by player’s velocity) */

                flags = _melee_projectile|_penetrates_media, /* flags */

                speed = (3*WORLD_ONE)/4, /* speed */
                maximum_range = (3*WORLD_ONE)/4, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_hummer_slow */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 0), shape = 5, /* collection number, shape number */
                detonation_effect = _effect_minor_hummer_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_hummer_bolt, flags = 0, @base = 15, random = 5}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_hummer_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_hummer_fast */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 1), shape = 5, /* collection number, shape number */
                detonation_effect = _effect_major_hummer_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_hummer_bolt, flags = 0, @base = 15, random = 5}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/6, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _higher_frequency, /* sound pitch */
                flyby_sound = _snd_hummer_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_hummer_durandal */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 1), shape = 5, /* collection number, shape number */
                detonation_effect = _effect_durandal_hummer_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_hummer_bolt, flags = 0, @base = 15, random = 5}, /* damage */

                flags = _guided|_usually_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _lower_frequency, /* sound pitch */
                flyby_sound = _snd_hummer_projectile_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_minor_cyborg_ball */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 0), shape = 6, /* collection number, shape number */
                detonation_effect = _effect_grenade_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_grenade_contrail, ticks_between_contrails = 1, maximum_contrails = 8, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = WORLD_ONE, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = 0, @base = 20, random = 10}, /* damage */

                flags = _can_toggle_control_panels|_sometimes_pass_transparent_side|_alien_projectile|_rebounds_from_floor|_doubly_affected_by_gravity, /* flags */

                speed = WORLD_ONE/10, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_cyborg_projectile_flyby, rebound_sound = _snd_cyborg_projectile_bounce, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_major_cyborg_ball */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 1), shape = 6, /* collection number, shape number */
                detonation_effect = _effect_grenade_explosion, media_detonation_effect = _medium_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = _effect_grenade_contrail, ticks_between_contrails = 1, maximum_contrails = 8, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/8, /* radius */
                area_of_effect = WORLD_ONE, /* area-of-effect */
                damage = new damage_definition {type = _damage_explosion, flags = 0, @base = 40, random = 10}, /* damage */

                flags = _guided|_can_toggle_control_panels|_sometimes_pass_transparent_side|_alien_projectile|_rebounds_from_floor|_doubly_affected_by_gravity, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _lower_frequency, /* sound pitch */
                flyby_sound = _snd_cyborg_projectile_flyby, rebound_sound = _snd_cyborg_projectile_bounce, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_ball */
            {
                collection = (short) BUILD_COLLECTION(_collection_player, 0), shape = 0, /* collection number, shape number */
                detonation_effect = NONE, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 1, maximum_contrails = 8, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/4, /* radius */
                area_of_effect = NONE, /* area-of-effect */
                damage = new damage_definition {type = NONE, flags = 0, @base = 40, random = 10}, /* damage */

                flags = _persistent_and_virulent|_penetrates_media|_becomes_item_on_detonation|_can_toggle_control_panels|_rebounds_from_floor|_doubly_affected_by_gravity|_penetrates_media, /* flags */

                speed = 0, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = _snd_ball_bounce, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_minor_fusion_dispersal */
            {
                collection = _collection_rocket, shape = 11, /* collection number, shape number */
                detonation_effect = _effect_minor_fusion_dispersal, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/20, /* radius */
                area_of_effect = WORLD_ONE, /* area-of-effect */
                damage = new damage_definition {type = _damage_fusion_bolt, flags = 0, @base = 30, random = 10}, /* damage */

                flags = 0, /* flags */

                speed = WORLD_ONE/4, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_fusion_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_major_fusion_dispersal */
            {
                collection = _collection_rocket, shape = 12, /* collection number, shape number */
                detonation_effect = _effect_major_fusion_dispersal, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/10, /* radius */
                area_of_effect = 2*WORLD_ONE, /* area-of-effect */
                damage = new damage_definition {type = _damage_fusion_bolt, flags = 0, @base = 80, random = 20}, /* damage */

                flags = 0, /* flags */

                speed = WORLD_ONE/3, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _higher_frequency, /* sound pitch */
                flyby_sound = _snd_fusion_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_overloaded_fusion_dispersal */
            {
                collection = _collection_rocket, shape = 12, /* collection number, shape number */
                detonation_effect = _effect_overloaded_fusion_dispersal, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = WORLD_ONE/10, /* radius */
                area_of_effect = 4*WORLD_ONE, /* area-of-effect */
                damage = new damage_definition {type = _damage_fusion_bolt, flags = 0, @base = 500, random = 0}, /* damage */

                flags = 0, /* flags */

                speed = WORLD_ONE/3, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _lower_frequency, /* sound pitch */
                flyby_sound = _snd_fusion_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_yeti */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_yeti_melee_detonation, media_detonation_effect = NONE, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_yeti_claws, flags = _alien_damage, @base = 20, random = 5}, /* damage */

                flags = _sometimes_pass_transparent_side|_alien_projectile|_melee_projectile|_penetrates_media, /* flags */

                speed = WORLD_ONE_HALF, /* speed */
                maximum_range = WORLD_ONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_sewage_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 0), shape = 10, /* collection number, shape number */
                detonation_effect = _effect_sewage_yeti_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_yeti_projectile, flags = 0, @base = 15, random = 5}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile|_affected_by_half_gravity, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_yeti_projectile_sewage_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            new projectile_definition /* _projectile_lava_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 2), shape = 6, /* collection number, shape number */
                detonation_effect = _effect_lava_yeti_projectile_detonation, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = NONE, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_flame, flags = 0, @base = 30, random = 10}, /* damage */

                flags = _usually_pass_transparent_side|_alien_projectile, /* flags */

                speed = WORLD_ONE/8, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = _snd_yeti_projectile_lava_flyby, rebound_sound = NONE, /* flyby sound, rebound sound */
            },

            // LP addition: SMG bullet is a clone of the rifle one, except for entering/exiting liquids
            new projectile_definition /* player’s smg bullet */
            {
                collection = NONE, shape = 0, /* collection number, shape number */
                detonation_effect = _effect_bullet_ricochet, media_detonation_effect = _small_media_detonation_effect, /* detonation effect, media_detonation_effect */
                contrail_effect = NONE, ticks_between_contrails = 0, maximum_contrails = 0, /* contrail effect, ticks between contrails, maximum contrails */
                media_projectile_promotion = NONE, /* media projectile promotion */

                radius = 0, /* radius */
                area_of_effect = 0, /* area-of-effect */
                damage = new damage_definition {type = _damage_projectile, flags = 0, @base = 9, random = 6}, /* damage */

                flags = _bleeding_projectile|_usually_pass_transparent_side|_penetrates_media_boundary, /* flags */

                speed = WORLD_ONE, /* speed */
                maximum_range = NONE, /* maximum range */

                sound_pitch = _normal_frequency, /* sound pitch */
                flyby_sound = NONE, rebound_sound = NONE, /* flyby sound, rebound sound */
            },
        };
    }
}
