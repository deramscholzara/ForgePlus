// Port of Aleph One: Source_Files/GameWorld/monster_definitions.h
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.effects;
using static AlephOne.items;
using static AlephOne.map;
using static AlephOne.monsters;
using static AlephOne.projectiles;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- monster definition structures */

    public class attack_definition
    {
        public short type;
        public short repetitions;
        public short error; /* ±error is added to the firing angle */
        public short range; /* beyond which we cannot attack */
        public short attack_shape; /* attack occurs when keyframe is displayed */

        public short dx, dy, dz; /* +dy is right, +dx is out, +dz is up */

        public attack_definition Clone()
        {
            return (attack_definition) MemberwiseClone();
        }
    }

    public class monster_definition /* <128 bytes */
    {
        public short collection;

        public short vitality;
        public uint immunities, weaknesses;
        public uint flags;

        public int _class; /* our class */
        public int friends, enemies; /* bit fields of what classes we consider friendly and what types we don’t like */

        public int sound_pitch;
        public short activation_sound, friendly_activation_sound, clear_sound;
        public short kill_sound, apology_sound, friendly_fire_sound;
        public short flaming_sound; /* the scream we play when we go down in flames */
        public short random_sound, random_sound_mask; /* if moving and locked play this sound if we get time and our mask comes up */

        public short carrying_item_type; /* an item type we might drop if we don’t explode */

        public short radius, height;
        public short preferred_hover_height;
        public short minimum_ledge_delta, maximum_ledge_delta;
        public int external_velocity_scale;
        public short impact_effect, melee_impact_effect, contrail_effect;

        public short half_visual_arc, half_vertical_visual_arc;
        public short visual_range, dark_visual_range;
        public short intelligence;
        public short speed, gravity, terminal_velocity;
        public short door_retry_mask;
        public short shrapnel_radius; /* no shrapnel if NONE */
        public damage_definition shrapnel_damage = new damage_definition();

        public ushort hit_shapes;
        public ushort hard_dying_shape, soft_dying_shape; /* minus dead frame */
        public ushort hard_dead_shapes, soft_dead_shapes; /* NONE for vanishing */
        public ushort stationary_shape, moving_shape;
        public ushort teleport_in_shape, teleport_out_shape;

        /* which type of attack the monster actually uses is determined at attack time; typically
            melee attacks will occur twice as often as ranged attacks because the monster will be
            stopped (and stationary monsters attack twice as often as moving ones) */
        public short attack_frequency;
        public attack_definition melee_attack = new attack_definition();
        public attack_definition ranged_attack = new attack_definition();

        public monster_definition Clone()
        {
            var copy = (monster_definition) MemberwiseClone();
            copy.shrapnel_damage = shrapnel_damage.Clone();
            copy.melee_attack = melee_attack.Clone();
            copy.ranged_attack = ranged_attack.Clone();
            return copy;
        }
    }

    [NoAutoStaticsCleanup]
    public static class monster_definitions
    {
        /* ---------- macros */

        public static bool TYPE_IS_NEUTRAL(monster_definition[] monster_definitions, monster_definition definition, short type) { return (((definition.friends | definition.enemies) & monster_definitions[type]._class) == 0); }
        public static int TYPE_IS_ENEMY(monster_definition[] monster_definitions, monster_definition definition, short type) { return (definition.enemies & monster_definitions[type]._class); }
        public static int TYPE_IS_FRIEND(monster_definition[] monster_definitions, monster_definition definition, short type) { return (definition.friends & monster_definitions[type]._class); }

        /* ---------- constants */

        /* monster classes */
        public const int _class_player_bit = 0;
        public const int _class_human_civilian_bit = 1;
        public const int _class_madd_bit = 2;
        public const int _class_possessed_hummer_bit = 3;

        public const int _class_defender_bit = 4;

        public const int _class_fighter_bit = 5;
        public const int _class_trooper_bit = 6;
        public const int _class_hunter_bit = 7;
        public const int _class_enforcer_bit = 8;
        public const int _class_juggernaut_bit = 9;
        public const int _class_hummer_bit = 10;

        public const int _class_compiler_bit = 11;
        public const int _class_cyborg_bit = 12;
        public const int _class_assimilated_civilian_bit = 13;

        public const int _class_tick_bit = 14;
        public const int _class_yeti_bit = 15;

        public const int _class_player = 1<<_class_player_bit;
        public const int _class_human_civilian = 1<<_class_human_civilian_bit;
        public const int _class_madd = 1<<_class_madd_bit;
        public const int _class_possessed_hummer = 1<<_class_possessed_hummer_bit;
        public const int _class_human = _class_player|_class_human_civilian|_class_madd|_class_possessed_hummer;

        public const int _class_defender = 1<<_class_defender_bit;

        public const int _class_fighter = 1<<_class_fighter_bit;
        public const int _class_trooper = 1<<_class_trooper_bit;
        public const int _class_hunter = 1<<_class_hunter_bit;
        public const int _class_enforcer = 1<<_class_enforcer_bit;
        public const int _class_juggernaut = 1<<_class_juggernaut_bit;
        public const int _class_pfhor = _class_fighter|_class_trooper|_class_hunter|_class_enforcer|_class_juggernaut;

        public const int _class_compiler = 1<<_class_compiler_bit;
        public const int _class_cyborg = 1<<_class_cyborg_bit;
        public const int _class_assimilated_civilian = 1<<_class_assimilated_civilian_bit;
        public const int _class_hummer = 1<<_class_hummer_bit;
        public const int _class_client = _class_compiler|_class_assimilated_civilian|_class_cyborg|_class_hummer;

        public const int _class_tick = 1<<_class_tick_bit;
        public const int _class_yeti = 1<<_class_yeti_bit;
        public const int _class_native = _class_tick|_class_yeti;

        public const int _class_hostile_alien = _class_pfhor|_class_client;
        public const int _class_neutral_alien = _class_native;

        // old Marathon monster classes
        // enum
        public const int _class_player_m1 = 0x01;
        public const int _class_human_civilian_m1 = 0x02;
        public const int _class_madd_m1 = 0x04;
        public const int _class_fighter_m1 = 0x08;
        public const int _class_trooper_m1 = 0x10;
        public const int _class_hunter_m1 = 0x20;
        public const int _class_enforcer_m1 = 0x40;
        public const int _class_juggernaut_m1 = 0x80;
        // unused 0x100
        public const int _class_compiler_m1 = 0x200;
        public const int _class_hulk = 0x400;
        // unused 0x800
        public const int _class_looker = 0x1000;
        // unused 0x2000,
        public const int _class_wasp = 0x4000;
        public const int _class_assimilated_civilian_m1 = 0x8000;

        public const int _class_client_m1 = _class_compiler_m1|_class_hulk|_class_assimilated_civilian_m1;
        public const int _class_pfhor_m1 = _class_fighter_m1|_class_trooper_m1|_class_hunter_m1|_class_enforcer_m1|_class_juggernaut_m1;

        /* intelligence: maximum polygon switches before losing lock */
        public const short _intelligence_low = 2;
        public const short _intelligence_average = 3;
        public const short _intelligence_high = 8;

        /* door retry masks */
        public const short _slow_door_retry_mask = 63;
        public const short _normal_door_retry_mask = 31;
        public const short _fast_door_retry_mask = 15;
        public const short _vidmaster_door_retry_mask = 3;

        /* flags */
        public const uint _monster_is_omniscent = 0x1; /* ignores line-of-sight during find_closest_appropriate_target() */
        public const uint _monster_flys = 0x2;
        public const uint _monster_is_alien = 0x4; /* moves slower on slower levels, etc. */
        public const uint _monster_major = 0x8; /* type -1 is minor */
        public const uint _monster_minor = 0x10; /* type +1 is major */
        public const uint _monster_cannot_be_dropped = 0x20; /* low levels cannot skip this monster */
        public const uint _monster_floats = 0x40; /* exclusive from flys; forces the monster to take +∂h gradually */
        public const uint _monster_cannot_attack = 0x80; /* monster has no weapons and cannot attack (runs constantly to safety) */
        public const uint _monster_uses_sniper_ledges = 0x100; /* sit on ledges and hurl shit at the player (ranged attack monsters only) */
        public const uint _monster_is_invisible = 0x200; /* this monster uses _xfer_invisibility */
        public const uint _monster_is_subtly_invisible = 0x400; /* this monster uses _xfer_subtle_invisibility */
        public const uint _monster_is_kamakazi = 0x800; /* monster does shrapnel damage and will suicide if close enough to target */
        public const uint _monster_is_berserker = 0x1000; /* below 1/4 vitality this monster goes berserk */
        public const uint _monster_is_enlarged = 0x2000; /* monster is 1.25 times normal height */
        public const uint _monster_has_delayed_hard_death = 0x4000; /* always dies soft, then switches to hard */
        public const uint _monster_fires_symmetrically = 0x8000; /* fires at ±dy, simultaneously */
        public const uint _monster_has_nuclear_hard_death = 0x10000; /* player’s screen whites out and slowly recovers */
        public const uint _monster_cant_fire_backwards = 0x20000; /* monster can’t turn more than 135° to fire */
        public const uint _monster_can_die_in_flames = 0x40000; /* uses humanoid flaming body shape */
        public const uint _monster_waits_with_clear_shot = 0x80000; /* will sit and fire (slowly) if we have a clear shot */
        public const uint _monster_is_tiny = 0x100000; /* 0.25-size normal height */
        public const uint _monster_attacks_immediately = 0x200000; /* monster will try an attack immediately */
        public const uint _monster_is_not_afraid_of_water = 0x400000;
        public const uint _monster_is_not_afraid_of_sewage = 0x800000;
        public const uint _monster_is_not_afraid_of_lava = 0x1000000;
        public const uint _monster_is_not_afraid_of_goo = 0x2000000;
        public const uint _monster_can_teleport_under_media = 0x4000000;
        public const uint _monster_chooses_weapons_randomly = 0x8000000;
        /* monsters unable to open doors have door retry masks of NONE */
        /* monsters unable to switch levels have min,max ledge deltas of 0 */
        /* monsters unstopped by bullets have hit frames of NONE */

        // pseudo flags set when reading Marathon 1 physics
        public const uint _monster_weaknesses_cause_soft_death = 0x10000000;
        public const uint _monster_screams_when_crushed = 0x20000000;
        public const uint _monster_makes_sound_when_activated = 0x40000000; // instead of when locking on a target
        public const uint _monster_can_grenade_climb = 0x80000000; // only applies to player

        /* monster speeds (world_distance per tick); also used for projectiles */
        public const short _speed_slow = WORLD_ONE/120;
        public const short _speed_medium = WORLD_ONE/80;
        public const short _speed_almost_fast = WORLD_ONE/70;
        public const short _speed_fast = WORLD_ONE/40;
        public const short _speed_superfast1 = WORLD_ONE/30;
        public const short _speed_superfast2 = WORLD_ONE/28;
        public const short _speed_superfast3 = WORLD_ONE/26;
        public const short _speed_superfast4 = WORLD_ONE/24;
        public const short _speed_superfast5 = WORLD_ONE/22;
        public const short _speed_blinding = WORLD_ONE/20;
        public const short _speed_insane = WORLD_ONE/10;

        public const short NORMAL_MONSTER_GRAVITY = (WORLD_ONE/120);
        public const short NORMAL_MONSTER_TERMINAL_VELOCITY = (WORLD_ONE/14);

        /* ---------- monster definitions */

        public static readonly monster_definition[] original_monster_definitions = new monster_definition[NUMBER_OF_MONSTER_TYPES]
        {
            new monster_definition /* _monster_marine (can’t be used as a regular monster) */
            {
                collection = _collection_player, /* shape collection */
                vitality = 20, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_cannot_be_dropped|_monster_can_die_in_flames, /* flags */

                _class = _class_player,
                friends = _class_human, /* friends */
                enemies = -1, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = 0, maximum_ledge_delta = 0,
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_player_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = 0, half_vertical_visual_arc = 0, /* half visual arc, half vertical visual arc */
                visual_range = 0, dark_visual_range = 0, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_almost_fast, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 0, soft_dying_shape = 0, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 0, soft_dead_shapes = 0, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */
            },

            new monster_definition /* _monster_tick_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_tick, 0), /* shape collection */
                vitality = 0, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_flys|_monster_has_delayed_hard_death|_monster_cannot_attack, /* flags */

                _class = _class_tick, /* class */
                friends = -1, /* friends */
                enemies = 0, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = _snd_tick_chatter, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE_HALF, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = NONE, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 1, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },

            new monster_definition /* _monster_tick_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_tick, 0), /* shape collection */
                vitality = 0, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_flys|_monster_has_delayed_hard_death|_monster_cannot_attack, /* flags */

                _class = _class_tick, /* class */
                friends = -1, /* friends */
                enemies = 0, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = _snd_tick_chatter, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE_FOURTH, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = NONE, /* door retry mask */
                shrapnel_radius = 2*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 40, random = 20, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 4, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 1, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },

            new monster_definition /* _monster_tick_kamakazi */
            {
                collection = (short) BUILD_COLLECTION(_collection_tick, 0), /* shape collection */
                vitality = 0, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_flys|_monster_is_kamakazi|_monster_has_delayed_hard_death, /* flags */

                _class = _class_tick, /* class */
                friends = 0, /* friends */
                enemies = -1, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE_FOURTH, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = NONE, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 4, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_minor_energy_drain, /* melee attack type */
                    repetitions = 5000, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE_HALF, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },

            new monster_definition /* _monster_compiler_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 0), /* shape collection */
                vitality = 160, immunities = FLAG(_damage_flame)|FLAG(_damage_lava), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_floats|_monster_can_teleport_under_media, /* flags */

                _class = _class_compiler, /* class */
                friends = _class_compiler, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast2, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 3, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_compiler_bolt_minor, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = NUMBER_OF_ANGLES/200, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_compiler_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 1), /* shape collection */
                vitality = 200, immunities = FLAG(_damage_flame)|FLAG(_damage_lava), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_floats|_monster_can_teleport_under_media, /* flags */

                _class = _class_compiler, /* class */
                friends = _class_compiler, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast3, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 3, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_compiler_bolt_major, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_compiler_minor_invisible */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 0), /* shape collection */
                vitality = 160, immunities = FLAG(_damage_flame)|FLAG(_damage_lava), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_floats|_monster_is_invisible|_monster_can_teleport_under_media, /* flags */

                _class = _class_compiler, /* class */
                friends = _class_compiler, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 3, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_compiler_bolt_minor, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = NUMBER_OF_ANGLES/200, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_compiler_major_invisible */
            {
                collection = (short) BUILD_COLLECTION(_collection_compiler, 1), /* shape collection */
                vitality = 200, immunities = FLAG(_damage_flame)|FLAG(_damage_lava), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_floats|_monster_is_subtly_invisible|_monster_can_teleport_under_media, /* flags */

                _class = _class_compiler, /* class */
                friends = _class_compiler, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast5, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 3, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_compiler_bolt_major, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_fighter (minor) */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 0), /* shape collection */
                vitality = 40, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_can_die_in_flames, /* flags */

                _class = _class_fighter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_fighter_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 2*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast1, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = 1, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 7, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 12, teleport_out_shape = 12, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_staff, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 3, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_fighter (major) */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 1), /* shape collection */
                vitality = 80, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_is_berserker|_monster_can_die_in_flames, /* flags */

                _class = _class_fighter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_fighter_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast2, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = 1, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 7, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 12, teleport_out_shape = 12, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_staff, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 3, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_fighter (minor projectile) */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 2), /* shape collection */
                vitality = 80, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_uses_sniper_ledges|_monster_can_die_in_flames, /* flags */

                _class = _class_fighter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_fighter_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast3, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = 1, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 7, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 12, teleport_out_shape = 12, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_staff, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_FOURTH, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_staff_bolt, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_fighter (major projectile) */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 3), /* shape collection */
                vitality = 80, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_uses_sniper_ledges|_monster_is_berserker|_monster_can_die_in_flames, /* flags */

                _class = _class_fighter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_fighter_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 5*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = 1, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 7, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 12, teleport_out_shape = 12, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_staff, /* melee attack type */
                    repetitions = 1, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_FOURTH, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_staff_bolt, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_crew "bob" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian, 0), /* shape collection */
                vitality = 20, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_human_activation, friendly_activation_sound = _snd_kill_the_player, clear_sound = _snd_human_clear, kill_sound = _snd_human_trash_talk, apology_sound = _snd_human_apology, friendly_fire_sound = _snd_human_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_human_wail, /* dying flaming */
                random_sound = _snd_human_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_magnum_magazine, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_pistol_bullet, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_science "fred" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian, 1), /* shape collection */
                vitality = 25, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human|_class_assimilated_civilian, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_human_activation, friendly_activation_sound = _snd_kill_the_player, clear_sound = _snd_human_clear, kill_sound = _snd_human_trash_talk, apology_sound = _snd_human_apology, friendly_fire_sound = _snd_human_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_human_wail, /* dying flaming */
                random_sound = _snd_human_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_magnum_magazine, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_pistol_bullet, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 13*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_security "steve" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian, 2), /* shape collection */
                vitality = 30, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human|_class_assimilated_civilian, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_human_activation, friendly_activation_sound = _snd_kill_the_player, clear_sound = _snd_human_clear, kill_sound = _snd_human_trash_talk, apology_sound = _snd_human_apology, friendly_fire_sound = _snd_human_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_human_wail, /* dying flaming */
                random_sound = _snd_human_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_magnum, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_pistol_bullet, /* ranged attack type */
                    repetitions = 5, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 17*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_assimilated "evil bob" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian, 3), /* shape collection */
                vitality = 30, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_is_kamakazi|_monster_can_die_in_flames, /* flags */

                _class = _class_assimilated_civilian,
                friends = _class_pfhor, /* friends */
                enemies = _class_player|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = _snd_human_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_human_wail, /* dying flaming */
                random_sound = _snd_assimilated_human_chatter, random_sound_mask = 0xf, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_assimilated_civilian_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 15*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 80, random = 40, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage  */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 11, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 0, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 8, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },

            new monster_definition /* _monster_hummer_minor (small hummer) */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 0), /* shape collection */
                vitality = 40, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt)|FLAG(_damage_compiler_bolt)|FLAG(_damage_electrical_staff), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_flys|_monster_minor|_monster_has_delayed_hard_death, /* flags */

                _class = _class_hummer, /* class */
                friends = _class_pfhor|_class_client, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_hummer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = WORLD_ONE_FOURTH, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_hummer_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = _effect_rocket_contrail, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = HALF_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_hummer, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 3, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = 0, dy = 0, dz = 0, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hummer_major (big hummer) */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 1), /* shape collection */
                vitality = 60, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt)|FLAG(_damage_compiler_bolt)|FLAG(_damage_electrical_staff), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_flys|_monster_major|_monster_has_delayed_hard_death, /* flags */

                _class = _class_hummer, /* class */
                friends = _class_pfhor|_class_client, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_hummer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = WORLD_ONE_FOURTH, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_hummer_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = _effect_rocket_contrail, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = HALF_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_hummer, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = 5, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = 0, dy = 0, dz = 0, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hummer_big_minor (big hummer) */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 2), /* shape collection */
                vitality = 40, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt)|FLAG(_damage_compiler_bolt)|FLAG(_damage_electrical_staff), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_flys|_monster_minor|_monster_has_delayed_hard_death, /* flags */

                _class = _class_hummer, /* class */
                friends = _class_pfhor|_class_client, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = _snd_hummer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = WORLD_ONE_FOURTH, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_hummer_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = _effect_rocket_contrail, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = HALF_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_hummer, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 3, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = 0, dy = 0, dz = 0, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hummer_big_major (angry hummer) */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 3), /* shape collection */
                vitality = 60, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt)|FLAG(_damage_compiler_bolt)|FLAG(_damage_electrical_staff), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_flys|_monster_major|_monster_has_delayed_hard_death, /* flags */

                _class = _class_hummer, /* class */
                friends = _class_pfhor|_class_client, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = _snd_hummer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = WORLD_ONE_FOURTH, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_hummer_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = _effect_rocket_contrail, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = HALF_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_hummer, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = 5, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = 0, dy = 0, dz = 0, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hummer_possessed (hummer from durandal) */
            {
                collection = (short) BUILD_COLLECTION(_collection_hummer, 4), /* shape collection */
                vitality = 60, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt)|FLAG(_damage_compiler_bolt)|FLAG(_damage_electrical_staff), /* vitality, immunities, weaknesses */
                flags = _monster_flys|_monster_has_delayed_hard_death|_monster_attacks_immediately, /* flags */

                _class = _class_possessed_hummer, /* class */
                friends = _class_human, /* friends */
                enemies = _class_pfhor|_class_client|_class_native, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = _snd_hummer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE_HALF, /* radius, height */
                preferred_hover_height = WORLD_ONE_FOURTH, /* preferred hover height */
                minimum_ledge_delta = -5*WORLD_ONE, maximum_ledge_delta = 5*WORLD_ONE, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_hummer_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = _effect_rocket_contrail, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = HALF_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 15*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = _alien_damage, @base = 30, random = 10, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 2, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_durandal_hummer, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = 5, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 1, /* ranged attack shape */

                    dx = 0, dy = 0, dz = 0, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_cyborg_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 0), /* shape collection */
                vitality = 300, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_uses_sniper_ledges, /* flags */

                _class = _class_cyborg, /* class */
                friends = _class_cyborg, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/4, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_cyborg_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast5, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 0, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE,
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_cyborg_ball, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_cyborg_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 1), /* shape collection */
                vitality = 450, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_uses_sniper_ledges, /* flags */

                _class = _class_cyborg, /* class */
                friends = _class_cyborg, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/4, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_cyborg_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 0, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE,
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_cyborg_ball, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = 0, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_cyborg_flame_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 0), /* shape collection */
                vitality = 300, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_uses_sniper_ledges, /* flags */

                _class = _class_cyborg, /* class */
                friends = _class_cyborg, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/4, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_cyborg_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 0, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_flamethrower_burst, /* ranged attack type */
                    repetitions = 15, /* repetitions */
                    error = 0, /* error angle */
                    range = 2*WORLD_ONE, /* range */
                    attack_shape = 4, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_major_cyborg_ball, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_cyborg_flame_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 0), /* shape collection */
                vitality = 450, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_uses_sniper_ledges, /* flags */

                _class = _class_cyborg, /* class */
                friends = _class_cyborg, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/4, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_cyborg_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 0, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_flamethrower_burst, /* ranged attack type */
                    repetitions = 15, /* repetitions */
                    error = 0, /* error angle */
                    range = 2*WORLD_ONE, /* range */
                    attack_shape = 4, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_major_cyborg_ball, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_enforcer_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_enforcer, 0), /* shape collection */
                vitality = 120, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_uses_sniper_ledges|_monster_can_die_in_flames|_monster_waits_with_clear_shot, /* flags */

                _class = _class_enforcer, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_enforcer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = _i_alien_shotgun, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_enforcer_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 7, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_alien_weapon, /* ranged attack type */
                    repetitions = 8, /* repetitions */
                    error = 2, /* error angle */
                    range = 15*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_enforcer_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_enforcer, 1), /* shape collection */
                vitality = 160, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_uses_sniper_ledges|_monster_can_die_in_flames|_monster_waits_with_clear_shot, /* flags */

                _class = _class_enforcer, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = _snd_enforcer_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = _i_alien_shotgun, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_enforcer_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 7, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_alien_weapon, /* ranged attack type */
                    repetitions = 12, /* repetitions */
                    error = 5, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH/2, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hunter_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_hunter, 0), /* shape collection */
                vitality = 200, immunities = FLAG(_damage_flame), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_uses_sniper_ledges|_monster_minor, /* flags */

                _class = _class_hunter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE_HALF, /* external velocity scale */
                impact_effect = _effect_hunter_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 4*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast3, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = 2*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 30, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 9, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 10, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 1, teleport_out_shape = 1, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_hunter, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = 3, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = WORLD_ONE/8, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_hunter_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_hunter, 1), /* shape collection */
                vitality = 300, immunities = FLAG(_damage_flame), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_uses_sniper_ledges|_monster_major, /* flags */

                _class = _class_hunter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE_HALF, /* external velocity scale */
                impact_effect = _effect_hunter_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 4*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast5, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = 2*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 60, random = 30, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = 9, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 10, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 1, teleport_out_shape = 1, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_hunter, /* ranged attack type */
                    repetitions = 5, /* repetitions */
                    error = 3, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = WORLD_ONE/8, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_trooper_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_trooper, 0), /* shape collection */
                vitality = 110, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_minor|_monster_uses_sniper_ledges|_monster_is_berserker|_monster_can_die_in_flames, /* flags */

                _class = _class_trooper, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_trooper_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast3, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 0, soft_dead_shapes = 7, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 1, teleport_out_shape = 1, /* teleport in shape, teleport out shape */

                attack_frequency = 4*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_trooper_bullet, /* melee attack type */
                    repetitions = 3, /* repetitions */
                    error = 30, /* error */
                    range = 3*WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = 0, dy = -WORLD_ONE/10, dz = WORLD_ONE_HALF-WORLD_ONE_FOURTH/4, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_trooper_grenade, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 10, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 9, /* ranged attack shape */

                    dx = -WORLD_ONE/10, dy = WORLD_ONE/8, dz = WORLD_ONE_HALF-WORLD_ONE_FOURTH/8, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_trooper_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_trooper, 1), /* shape collection */
                vitality = 200, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_major|_monster_uses_sniper_ledges|_monster_is_berserker|_monster_can_die_in_flames, /* flags */

                _class = _class_trooper, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -4*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = (3*FIXED_ONE)/4, /* external velocity scale */
                impact_effect = _effect_trooper_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast3, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _fast_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 0, soft_dead_shapes = 7, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 1, teleport_out_shape = 1, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_trooper_bullet, /* melee attack type */
                    repetitions = 8, /* repetitions */
                    error = 10, /* error */
                    range = 3*WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = -WORLD_ONE/10, dy = WORLD_ONE/8, dz = WORLD_ONE_HALF-WORLD_ONE_FOURTH/8, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_trooper_grenade, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = 5, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 9, /* ranged attack shape */

                    dx = 0, dy = -WORLD_ONE/10, dz = WORLD_ONE_HALF-WORLD_ONE_FOURTH/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_mother_of_all_cyborgs */
            {
                collection = (short) BUILD_COLLECTION(_collection_cyborg, 0), /* shape collection */
                vitality = 1500, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_enlarged|_monster_is_alien|_monster_cannot_be_dropped|_monster_uses_sniper_ledges, /* flags */

                _class = _class_cyborg, /* class */
                friends = 0, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/3, height = WORLD_ONE + WORLD_ONE/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/4, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_cyborg_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast4, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = 3*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 140, random = 40, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 0, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 5, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = 0, teleport_out_shape = 0, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_flamethrower_burst, /* ranged attack type */
                    repetitions = 15, /* repetitions */
                    error = 0, /* error angle */
                    range = 2*WORLD_ONE, /* range */
                    attack_shape = 4, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_major_cyborg_ball, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/16, dy = 0, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_mother_of_all_hunters */
            {
                collection = (short) BUILD_COLLECTION(_collection_hunter, 2), /* shape collection */
                vitality = 1500, immunities = FLAG(_damage_flame), weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_is_enlarged|_monster_uses_sniper_ledges|_monster_cannot_be_dropped, /* flags */

                _class = _class_hunter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = _class_human|_class_native|_class_defender, /* enemies */

                sound_pitch = _lower_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming death sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE+WORLD_ONE/6, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE_HALF, /* external velocity scale */
                impact_effect = _effect_hunter_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 4*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast1, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = 4*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 140, random = 50, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 7, /* being hit */
                hard_dying_shape = 3, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 8, /* hard dead frames, soft dead frames */
                stationary_shape = 1, moving_shape = 0, /* stationary shape, moving shape (no permutations) */
                teleport_in_shape = 1, teleport_out_shape = 1, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_hunter, /* ranged attack type */
                    repetitions = 5, /* repetitions */
                    error = 3, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = WORLD_ONE/8, dz = WORLD_ONE_HALF+WORLD_ONE_FOURTH, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_sewage_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 0), /* shape collection */
                vitality = 100, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_not_afraid_of_sewage|_monster_is_alien|_monster_is_berserker, /* flags */

                _class = _class_yeti, /* class */
                friends = _class_yeti, /* friends */
                enemies = _class_pfhor|_class_human|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* dying flaming */
                random_sound = NONE, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE-1, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 3*FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_sewage_yeti_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_superfast5, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _slow_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 12, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_yeti, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */
                    attack_shape = 13, /* melee attack shape */

                    dx = 0, dy = 0, dz = 4*WORLD_ONE/5, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_sewage_yeti, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/3, dy = WORLD_ONE/6, dz = 4*WORLD_ONE/5, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_water_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 1), /* shape collection */
                vitality = 250, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_not_afraid_of_water|_monster_is_alien|_monster_is_berserker, /* flags */

                _class = _class_yeti, /* class */
                friends = _class_yeti, /* friends */
                enemies = _class_pfhor|_class_human|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* dying flaming */
                random_sound = NONE, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE-1, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 3*FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_water_yeti_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_superfast5, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _slow_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 12, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = TICKS_PER_SECOND/2, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_yeti, /* melee attack type */
                    repetitions = 1, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */
                    attack_shape = 13, /* melee attack shape */

                    dx = 0, dy = WORLD_ONE/6, dz = 4*WORLD_ONE/5, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE
                }
            },

            new monster_definition /* _monster_lava_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 2), /* shape collection */
                vitality = 200, immunities = FLAG(_damage_flame)|FLAG(_damage_alien_projectile)|FLAG(_damage_fusion_bolt)|FLAG(_damage_lava), weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_not_afraid_of_lava|_monster_is_alien|_monster_is_berserker, /* flags */

                _class = _class_yeti, /* class */
                friends = _class_yeti, /* friends */
                enemies = _class_pfhor|_class_human|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* dying flaming */
                random_sound = NONE, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/4, height = WORLD_ONE-1, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 3*FIXED_ONE/4, /* external velocity scale */
                impact_effect = _effect_lava_yeti_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _slow_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 12, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_yeti, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */
                    attack_shape = 13, /* melee attack shape */

                    dx = 0, dy = 0, dz = 4*WORLD_ONE/5, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_lava_yeti, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/3, dy = WORLD_ONE/6, dz = 4*WORLD_ONE/5, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_defender_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_defender, 0), /* shape collection */
                vitality = 160, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_flys|_monster_waits_with_clear_shot, /* flags */

                _class = _class_defender, /* class */
                friends = _class_defender, /* friends */
                enemies = _class_pfhor|_class_client|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = WORLD_ONE/4, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 3, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 6, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 8, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_minor_defender, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = NUMBER_OF_ANGLES/200, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/8, dy = -WORLD_ONE/4+WORLD_ONE/10, dz = WORLD_ONE_HALF, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_defender_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_defender, 1), /* shape collection */
                vitality = 240, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_flys|_monster_waits_with_clear_shot, /* flags */

                _class = _class_defender, /* class */
                friends = _class_defender, /* friends */
                enemies = _class_pfhor|_class_client|_class_native, /* enemies */

                sound_pitch = _higher_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = WORLD_ONE, /* radius, height */
                preferred_hover_height = WORLD_ONE/4, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = NONE, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 3, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 6, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = UNONE, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 8, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_major_defender, /* ranged attack type */
                    repetitions = 4, /* repetitions */
                    error = NUMBER_OF_ANGLES/100, /* error angle */
                    range = 20*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = WORLD_ONE/8, dy = -WORLD_ONE/4+WORLD_ONE/10, dz = WORLD_ONE_HALF, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_juggernaut_minor */
            {
                collection = (short) BUILD_COLLECTION(_collection_juggernaut, 0), /* shape collection */
                vitality = 2500, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_minor|_monster_is_alien|_monster_cant_fire_backwards|_monster_has_nuclear_hard_death|
                    _monster_has_delayed_hard_death|_monster_cannot_be_dropped|_monster_fires_symmetrically|
                    _monster_chooses_weapons_randomly|_monster_flys, /* flags */

                _class = _class_juggernaut, /* class */
                friends = _class_juggernaut, /* friends */
                enemies = _class_human|_class_client|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE, height = 2*WORLD_ONE, /* radius, height */
                preferred_hover_height = WORLD_ONE, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = _effect_juggernaut_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY/4, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY/4, /* gravity, terminal velocity */
                door_retry_mask = NONE, /* door retry mask */
                shrapnel_radius = 5*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 350, random = 50, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = UNONE, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 5, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 8, soft_dead_shapes = 8, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 7, teleport_out_shape = 7, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_alien_weapon, /* melee attack type */
                    repetitions = 10, /* repetitions */
                    error = 5, /* error */
                    range = 15*WORLD_ONE, /* range */
                    attack_shape = 1, /* melee attack shape */

                    dx = WORLD_ONE/4, dy = WORLD_ONE_HALF+WORLD_ONE/8, dz = WORLD_ONE-WORLD_ONE/4-WORLD_ONE/16, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_juggernaut_missile, /* ranged attack type */
                    repetitions = 0, /* repetitions */
                    error = 40, /* error angle */
                    range = 25*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = WORLD_ONE_HALF, dz = WORLD_ONE+WORLD_ONE_HALF, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_juggernaut_major */
            {
                collection = (short) BUILD_COLLECTION(_collection_juggernaut, 1), /* shape collection */
                vitality = 5000, immunities = 0, weaknesses = FLAG(_damage_fusion_bolt), /* vitality, immunities, weaknesses */
                flags = _monster_major|_monster_is_alien|_monster_cant_fire_backwards|_monster_has_nuclear_hard_death|
                    _monster_has_delayed_hard_death|_monster_cannot_be_dropped|_monster_fires_symmetrically|
                    _monster_chooses_weapons_randomly|_monster_flys, /* flags */

                _class = _class_juggernaut, /* class */
                friends = _class_juggernaut, /* friends */
                enemies = _class_human|_class_client|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* flaming dying sound */
                random_sound = NONE, random_sound_mask = 0, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE, height = 2*WORLD_ONE, /* radius, height */
                preferred_hover_height = WORLD_ONE, /* preferred hover height */
                minimum_ledge_delta = INT16_MIN, maximum_ledge_delta = INT16_MAX, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = 0, /* external velocity scale */
                impact_effect = _effect_juggernaut_spark, melee_impact_effect = _effect_metallic_clang, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY/4, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY/4, /* gravity, terminal velocity */
                door_retry_mask = NONE, /* door retry mask */
                shrapnel_radius = 5*WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 350, random = 50, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage */

                hit_shapes = UNONE, /* being hit */
                hard_dying_shape = 6, soft_dying_shape = 5, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 8, soft_dead_shapes = 8, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 7, teleport_out_shape = 7, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_alien_weapon, /* melee attack type */
                    repetitions = 20, /* repetitions */
                    error = 5, /* error */
                    range = 15*WORLD_ONE, /* range */
                    attack_shape = 1, /* melee attack shape */

                    dx = WORLD_ONE/4, dy = WORLD_ONE_HALF+WORLD_ONE/8, dz = WORLD_ONE-WORLD_ONE/4-WORLD_ONE/16, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_juggernaut_missile, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = 40, /* error angle */
                    range = 25*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = WORLD_ONE_HALF, dz = WORLD_ONE+WORLD_ONE_HALF, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_tiny_fighter */
            {
                collection = (short) BUILD_COLLECTION(_collection_fighter, 1), /* shape collection */
                vitality = 40, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_tiny|_monster_is_berserker|_monster_can_die_in_flames, /* flags */

                _class = _class_fighter, /* class */
                friends = _class_pfhor, /* friends */
                enemies = (_class_human&~_class_player)|_class_native|_class_defender, /* enemies */

                sound_pitch = FIXED_ONE+FIXED_ONE_HALF, /* sound pitch */
                activation_sound = _snd_fighter_activate, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_fighter_wail, /* dying flaming */
                random_sound = _snd_fighter_chatter, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/12, height = (4*WORLD_ONE)/12, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -8*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/6, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/2, /* external velocity scale */
                impact_effect = _effect_fighter_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = 3*WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_fast, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _normal_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 4, /* being hit */
                hard_dying_shape = 1, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 6, soft_dead_shapes = 5, /* hard dead frames, soft dead frames */
                stationary_shape = 7, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 12, teleport_out_shape = 12, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_staff, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */

                    attack_shape = 2, /* melee attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE/5, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },

            new monster_definition /* _monster_tiny_bob */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian, 0), /* shape collection */
                vitality = 10, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges|_monster_is_tiny, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = FIXED_ONE+FIXED_ONE_HALF, /* sound pitch */
                activation_sound = _snd_human_activation, friendly_activation_sound = _snd_kill_the_player, clear_sound = _snd_human_clear, kill_sound = _snd_human_trash_talk, apology_sound = _snd_human_apology, friendly_fire_sound = _snd_human_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_human_wail, /* dying flaming */
                random_sound = _snd_human_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/12, height = (4*WORLD_ONE)/12, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/6, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/2, /* external velocity scale */
                impact_effect = _effect_civilian_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_superfast2, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_pistol_bullet, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE/5, /* dx, dy, dz */
                }
            },

            new monster_definition /* _monster_tiny_yeti */
            {
                collection = (short) BUILD_COLLECTION(_collection_yeti, 2), /* shape collection */
                vitality = 100, immunities = FLAG(_damage_flame)|FLAG(_damage_alien_projectile)|FLAG(_damage_fusion_bolt)|FLAG(_damage_lava), weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_not_afraid_of_lava|_monster_is_berserker|_monster_is_tiny, /* flags */

                _class = _class_yeti, /* class */
                friends = _class_yeti, /* friends */
                enemies = (_class_human&~_class_player)|_class_pfhor, /* enemies */

                sound_pitch = FIXED_ONE+FIXED_ONE_HALF, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = NONE, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = NONE, /* dying flaming */
                random_sound = NONE, random_sound_mask = 15, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/12, height = (4*WORLD_ONE)/12, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -WORLD_ONE, maximum_ledge_delta = WORLD_ONE/6, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE/2, /* external velocity scale */
                impact_effect = _effect_lava_yeti_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_low, /* intelligence */
                speed = _speed_superfast2, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _slow_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 12, /* being hit */
                hard_dying_shape = UNONE, soft_dying_shape = 3, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = UNONE, soft_dead_shapes = 4, /* hard dead frames, soft dead frames */
                stationary_shape = 0, moving_shape = 1, /* stationary shape, moving shape */
                teleport_in_shape = UNONE, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = _projectile_yeti, /* melee attack type */
                    repetitions = 0, /* repetitions */
                    error = 0, /* error */
                    range = WORLD_ONE, /* range */
                    attack_shape = 13, /* melee attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE/5, /* dx, dy, dz */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_lava_yeti, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 12*WORLD_ONE, /* range */
                    attack_shape = 2, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE/5, /* dx, dy, dz */
                }
            },

            // LP addition: the VacBobs:
            // they drop either fusion batteries or fusion guns as appropriate,
            // they shoot "minor" fusion bolts (those that don't flip switches)

            new monster_definition /* _civilian_fusion_crew "bob" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian_fusion, 0), /* shape collection */
                vitality = 20, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_civilian_fusion_activation, friendly_activation_sound = _snd_civilian_fusion_kill_the_player, clear_sound = _snd_civilian_fusion_clear, kill_sound = _snd_civilian_fusion_trash_talk, apology_sound = _snd_civilian_fusion_apology, friendly_fire_sound = _snd_civilian_fusion_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_civilian_fusion_wail, /* dying flaming */
                random_sound = _snd_civilian_fusion_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_plasma_magazine, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_fusion_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_fusion_bolt_minor, /* ranged attack type */
                    repetitions = 1, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 10*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_fusion_science "fred" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian_fusion, 1), /* shape collection */
                vitality = 25, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human|_class_assimilated_civilian, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_civilian_fusion_activation, friendly_activation_sound = _snd_civilian_fusion_kill_the_player, clear_sound = _snd_civilian_fusion_clear, kill_sound = _snd_civilian_fusion_trash_talk, apology_sound = _snd_civilian_fusion_apology, friendly_fire_sound = _snd_civilian_fusion_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_civilian_fusion_wail, /* dying flaming */
                random_sound = _snd_civilian_fusion_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_plasma_magazine, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_fusion_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = 3*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_fusion_bolt_minor, /* ranged attack type */
                    repetitions = 2, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 13*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_fusion_security "steve" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian_fusion, 2), /* shape collection */
                vitality = 30, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_attacks_immediately|_monster_is_omniscent|_monster_cannot_be_dropped|_monster_waits_with_clear_shot|_monster_can_die_in_flames|_monster_uses_sniper_ledges, /* flags */

                _class = _class_human_civilian, /* class */
                friends = _class_human|_class_assimilated_civilian, /* friends */
                enemies = (_class_hostile_alien^_class_assimilated_civilian)|_class_native, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = _snd_civilian_fusion_activation, friendly_activation_sound = _snd_civilian_fusion_kill_the_player, clear_sound = _snd_civilian_fusion_clear, kill_sound = _snd_civilian_fusion_trash_talk, apology_sound = _snd_civilian_fusion_apology, friendly_fire_sound = _snd_civilian_fusion_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_civilian_fusion_wail, /* dying flaming */
                random_sound = _snd_civilian_fusion_chatter, random_sound_mask = 0x1f, /* random sound, random sound mask */

                carrying_item_type = _i_plasma_pistol, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_civilian_fusion_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 30*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = NONE, shrapnel_damage = new damage_definition {type = NONE, flags = 0, @base = 0, random = 0}, /* shrapnel radius, shrapnel damage */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 2, soft_dying_shape = 1, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 3, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 9, teleport_out_shape = 8, /* teleport in shape, teleport out shape */

                attack_frequency = TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = _projectile_fusion_bolt_minor, /* ranged attack type */
                    repetitions = 5, /* repetitions */
                    error = NUMBER_OF_ANGLES/150, /* error angle */
                    range = 17*WORLD_ONE, /* range */
                    attack_shape = 5, /* ranged attack shape */

                    dx = 0, dy = 0, dz = WORLD_ONE*3/4, /* dx, dy, dz */
                }
            },

            new monster_definition /* _civilian_fusion_assimilated "evil bob" */
            {
                collection = (short) BUILD_COLLECTION(_collection_civilian_fusion, 3), /* shape collection */
                vitality = 30, immunities = 0, weaknesses = 0, /* vitality, immunities, weaknesses */
                flags = _monster_is_alien|_monster_is_kamakazi|_monster_can_die_in_flames, /* flags */

                _class = _class_assimilated_civilian,
                friends = _class_pfhor, /* friends */
                enemies = _class_player|_class_defender, /* enemies */

                sound_pitch = _normal_frequency, /* sound pitch */
                activation_sound = NONE, friendly_activation_sound = NONE, clear_sound = NONE, kill_sound = NONE, apology_sound = NONE, friendly_fire_sound = _snd_civilian_fusion_stop_shooting_me_you_bastard, /* sounds: activation, friendly activation, clear, kill, apology, friendly-fire */
                flaming_sound = _snd_civilian_fusion_wail, /* dying flaming */
                random_sound = _snd_assimilated_civilian_fusion_chatter, random_sound_mask = 0xf, /* random sound, random sound mask */

                carrying_item_type = NONE, /* carrying item type */

                radius = WORLD_ONE/5, height = (4*WORLD_ONE)/5, /* radius, height */
                preferred_hover_height = 0, /* preferred hover height */
                minimum_ledge_delta = -2*WORLD_ONE, maximum_ledge_delta = WORLD_ONE/3, /* minimum ledge delta, maximum ledge delta */
                external_velocity_scale = FIXED_ONE, /* external velocity scale */
                impact_effect = _effect_assimilated_civilian_fusion_blood_splash, melee_impact_effect = NONE, contrail_effect = NONE, /* impact effect, melee impact effect, contrail effect */

                half_visual_arc = QUARTER_CIRCLE, half_vertical_visual_arc = QUARTER_CIRCLE/3, /* half visual arc, half vertical visual arc */
                visual_range = 15*WORLD_ONE, dark_visual_range = WORLD_ONE, /* visual range, dark visual range */
                intelligence = _intelligence_high, /* intelligence */
                speed = _speed_blinding, /* speed */
                gravity = NORMAL_MONSTER_GRAVITY, terminal_velocity = NORMAL_MONSTER_TERMINAL_VELOCITY, /* gravity, terminal velocity */
                door_retry_mask = _vidmaster_door_retry_mask, /* door retry mask */
                shrapnel_radius = WORLD_ONE, shrapnel_damage = new damage_definition {type = _damage_explosion, flags = _alien_damage, @base = 80, random = 40, scale = FIXED_ONE}, /* shrapnel radius, shrapnel damage  */

                hit_shapes = 10, /* being hit */
                hard_dying_shape = 11, soft_dying_shape = UNONE, /* dying hard (popping), dying soft (falling) */
                hard_dead_shapes = 4, soft_dead_shapes = 0, /* hard dead frames, soft dead frames */
                stationary_shape = 6, moving_shape = 0, /* stationary shape, moving shape */
                teleport_in_shape = 8, teleport_out_shape = UNONE, /* teleport in shape, teleport out shape */

                attack_frequency = 2*TICKS_PER_SECOND, /* attack frequency (for both melee and ranged attacks) */

                /* melee attack */
                melee_attack = new attack_definition
                {
                    type = NONE, /* melee attack type */
                },

                /* ranged attack */
                ranged_attack = new attack_definition
                {
                    type = NONE, /* ranged attack type */
                }
            },
        };
    }
}
