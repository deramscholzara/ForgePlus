// Port of Aleph One: Source_Files/GameWorld/weapon_definitions.h
using Unity.Scripting.LifecycleManagement;
using static AlephOne.cstypes;
using static AlephOne.items;
using static AlephOne.map;
using static AlephOne.projectiles;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;
using static AlephOne.weapons;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- shell casings */

    public class shell_casing_definition
    {
        public short collection, shape;

        public int x0, y0;
        public int vx0, vy0;
        public int dvx, dvy;
    }

    /* ---------- structures */

    public class trigger_definition
    {
        public short rounds_per_magazine;
        public short ammunition_type;
        public short ticks_per_round;
        public short recovery_ticks;
        public short charging_ticks;
        public short recoil_magnitude;
        public short firing_sound;
        public short click_sound;
        public short charging_sound;
        public short shell_casing_sound;
        public short reloading_sound;
        public short charged_sound;
        public short projectile_type;
        public short theta_error;
        public short dx, dz;
        public short shell_casing_type;
        public short burst_count;
        public short sound_activation_range; /* for Marathon compatibility */

        public trigger_definition Clone()
        {
            return (trigger_definition) MemberwiseClone();
        }
    }

    public class weapon_definition
    {
        public short item_type;
        public short powerup_type;
        public short weapon_class;
        public short flags;

        public int firing_light_intensity;
        public short firing_intensity_decay_ticks;

        /* weapon will come up to FIXED_ONE when fired; idle_height±bob_amplitude should be in
            the range [0,FIXED_ONE] */
        public int idle_height, bob_amplitude, kick_height, reload_height;
        public int idle_width, horizontal_amplitude;

        /* each weapon has three basic animations: idle, firing and reloading.  sounds and frames
            are pulled from the shape collection.  for automatic weapons the firing animation loops
            until the trigger is released or the gun is empty and the gun begins rising as soon as
            the trigger is depressed and is not lowered until the firing animation stops.  for single
            shot weapons the animation loops once; the weapon is raised and lowered as soon as the
            firing animation terminates */
        public short collection;
        public short idle_shape, firing_shape, reloading_shape;
        public short unused;
        public short charging_shape, charged_shape;

        /* How long does it take to ready the weapon? */
        /* load_rounds_tick is the point which you actually load them. */
        public short ready_ticks, await_reload_ticks, loading_ticks, finish_loading_ticks, powerup_ticks;

        public trigger_definition[] weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS] { new trigger_definition(), new trigger_definition() };

        public weapon_definition Clone()
        {
            var copy = (weapon_definition) MemberwiseClone();
            copy.weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS];
            for (int m = 0; m < NUMBER_OF_TRIGGERS; m++) copy.weapons_by_trigger[m] = weapons_by_trigger[m].Clone();
            return copy;
        }
    }

    [NoAutoStaticsCleanup]
    public static class weapon_definitions
    {
        /* TEMPORARY!!! */
        public const short _projectile_ball_dropped = 1000;

        /* ---------- constants */
        public const short NORMAL_WEAPON_DZ = (20);

        /* This is the amount of ammo that charging weapons use at one time.. */
        public const short CHARGING_WEAPON_AMMO_COUNT = 4;

        /* weapon classes */
        public const short _melee_class = 0; /* normal weapon, no ammunition, both triggers do the same thing */
        public const short _normal_class = 1; /* normal weapon, one ammunition type, both triggers do the same thing */
        public const short _dual_function_class = 2; /* normal weapon, one ammunition type, trigger does something different */
        public const short _twofisted_pistol_class = 3; /* two can be held at once (differnet triggers), same ammunition */
        public const short _multipurpose_class = 4; /* two weapons in one (assault rifle, grenade launcher), two different
            ammunition types with two separate triggers; secondary ammunition is discrete (i.e., it
            is never loaded explicitly but appears in the weapon) */

        /* weapon flags */
        public const short _no_flags = 0x0;
        public const short _weapon_is_automatic = 0x01;
        public const short _weapon_disappears_after_use = 0x02;
        public const short _weapon_plays_instant_shell_casing_sound = 0x04;
        public const short _weapon_overloads = 0x08;
        public const short _weapon_has_random_ammo_on_pickup = 0x10;
        public const short _powerup_is_temporary = 0x20;
        public const short _weapon_reloads_in_one_hand = 0x40;
        public const short _weapon_fires_out_of_phase = 0x80;
        public const short _weapon_fires_under_media = 0x100;
        public const short _weapon_triggers_share_ammo = 0x200;
        public const short _weapon_secondary_has_angular_flipping = 0x400;

        // definitions for Marathon compatibility
        public const short _weapon_disappears_after_use_m1 = 0x04;
        public const short _weapon_is_marathon_1 = 0x1000;
        public const short _weapon_flutters_while_firing = 0x2000;

        // enum
        public const short _weapon_in_hand_collection = 1;
        public const short _fist_idle = 0;
        public const short _fist_punching = 1;
        public const short _pistol_idle = 2;
        public const short _pistol_firing = 3;
        public const short _pistol_reloading = 4;
        public const short _shotgun_idle = 5;
        public const short _shotgun_firing = 6;
        public const short _shotgun_reloading = 7;
        public const short _assault_rifle_idle = 8;
        public const short _assault_rifle_firing = 9;
        public const short _assault_rifle_reloading = 10;
        public const short _fusion_idle = 11;
        public const short _fusion_firing = 12;
        public const short _missile_launcher_idle = 13;
        public const short _missile_launcher_firing = 14;
        public const short _flamethrower_idle = 15;
        public const short _flamethrower_transit = 16;
        public const short _flamethrower_firing = 17;
        public const short _assault_rifle_shell_casing = 18;
        public const short _pistol_shell_casing = 19;
        public const short _fusion_charged = 20;
        public const short _alien_weapon_idle = 21;
        public const short _alien_weapon_firing = 22;
        // LP additions:
        public const short _smg_idle = 23;
        public const short _smg_firing = 24;
        public const short _smg_reloading = 25;
        public const short _smg_shell_casing = 26;

        /* ---------- shell casings */

        // shell casing types
        public const short _shell_casing_assault_rifle = 0;
        public const short _shell_casing_pistol = 1;
        public const short _shell_casing_pistol_left = 2;
        public const short _shell_casing_pistol_right = 3;
        // LP additions:
        public const short _shell_casing_smg = 4;

        public const short NUMBER_OF_SHELL_CASING_TYPES = 5;

        public static readonly shell_casing_definition[] shell_casing_definitions = new shell_casing_definition[NUMBER_OF_SHELL_CASING_TYPES]
        {
            new shell_casing_definition // _shell_casing_assault_rifle,
            {
                collection = _collection_weapons_in_hand, shape = 19, /* collection, shape */

                x0 = FIXED_ONE/2 + FIXED_ONE/6, y0 = FIXED_ONE/8, /* x0, y0 */
                vx0 = FIXED_ONE/8, vy0 = FIXED_ONE/32, /* vx0, vy0 */
                dvx = 0, dvy = -FIXED_ONE/256, /* dvx, dvy */
            },

            new shell_casing_definition // _shell_casing_pistol_center
            {
                collection = _collection_weapons_in_hand, shape = 18, /* collection, shape */

                x0 = FIXED_ONE/2 + FIXED_ONE/8, y0 = FIXED_ONE/4, /* x0, y0 */
                vx0 = FIXED_ONE/16, vy0 = FIXED_ONE/32, /* vx0, vy0 */
                dvx = 0, dvy = -FIXED_ONE/400, /* dvx, dvy */
            },

            new shell_casing_definition // _shell_casing_pistol_left
            {
                collection = _collection_weapons_in_hand, shape = 18, /* collection, shape */

                x0 = FIXED_ONE/2 - FIXED_ONE/4, y0 = FIXED_ONE/4, /* x0, y0 */
                vx0 = - FIXED_ONE/16, vy0 = FIXED_ONE/32, /* vx0, vy0 */
                dvx = 0, dvy = -FIXED_ONE/400, /* dvx, dvy */
            },

            new shell_casing_definition // _shell_casing_pistol_right
            {
                collection = _collection_weapons_in_hand, shape = 18, /* collection, shape */

                x0 = FIXED_ONE/2 + FIXED_ONE/4, y0 = FIXED_ONE/4, /* x0, y0 */
                vx0 = FIXED_ONE/16, vy0 = FIXED_ONE/32, /* vx0, vy0 */
                dvx = 0, dvy = -FIXED_ONE/400, /* dvx, dvy */
            },

            // LP addition: clone of assault-rifle casing
            new shell_casing_definition // _shell_casing_smg,
            {
                collection = _collection_weapons_in_hand, shape = 26, /* collection, shape */

                x0 = FIXED_ONE/2 + FIXED_ONE/6, y0 = FIXED_ONE/8, /* x0, y0 */
                vx0 = FIXED_ONE/8, vy0 = FIXED_ONE/32, /* vx0, vy0 */
                dvx = 0, dvy = -FIXED_ONE/256, /* dvx, dvy */
            },
        };

        /* ------------------------ globals */

        public static readonly short[] weapon_ordering_array = new short[]
        {
            _weapon_fist,
            _weapon_pistol,
            _weapon_plasma_pistol,
            _weapon_shotgun,
            _weapon_assault_rifle,
            // LP addition:
            _weapon_smg,
            _weapon_flamethrower,
            _weapon_missile_launcher,
            _weapon_alien_shotgun,
            _weapon_ball
        };

        //#define NUMBER_OF_WEAPONS static_cast<int>(sizeof(weapon_definitions)/sizeof(struct weapon_definition))
        public const short NUMBER_OF_WEAPONS = 10;

        public const short SHOTGUN_BURST_COUNT = 10;
        public const short SHOTGUN_SPREAD = 5;

        public static readonly weapon_definition[] original_weapon_definitions = new weapon_definition[NUMBER_OF_WEAPONS]
        {
            /* Fist*/
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_knife, powerup_type = NONE, weapon_class = _melee_class, flags = _weapon_fires_under_media,

                firing_light_intensity = FIXED_ONE_HALF, firing_intensity_decay_ticks = 0, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/15, bob_amplitude = FIXED_ONE/15, kick_height = FIXED_ONE/16, reload_height = 0,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _fist_idle, firing_shape = _fist_punching, reloading_shape = _fist_idle,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/4, await_reload_ticks = 0, loading_ticks = 0, finish_loading_ticks = 0, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing sound, reloading sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_fist,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_fist,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* Magnum .45 "mega class"- dual fisted */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_magnum, powerup_type = NONE, weapon_class = _twofisted_pistol_class, flags = _weapon_fires_out_of_phase,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/8, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/15, bob_amplitude = FIXED_ONE/25, kick_height = FIXED_ONE/8, reload_height = FIXED_ONE,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _pistol_idle, firing_shape = _pistol_firing, reloading_shape = _pistol_reloading,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/3, await_reload_ticks = 5, loading_ticks = 5, finish_loading_ticks = 5, powerup_ticks = 0, // was NONE

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 8,

                        /* Ammunition type */
                        ammunition_type = _i_magnum_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 10,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_magnum_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = _snd_magnum_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_pistol_bullet,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = (WORLD_ONE_FOURTH/6), dz = -NORMAL_WEAPON_DZ, /* Primary */

                        /* shell casing type */
                        shell_casing_type = _shell_casing_pistol,

                        /* burst count */
                        burst_count = 0
                    },

                    /* left weapon (for consistency)... */
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 8,

                        /* Ammunition type */
                        ammunition_type = _i_magnum_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 10,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_magnum_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = _snd_magnum_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_pistol_bullet,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = -(WORLD_ONE_FOURTH/6), dz = -NORMAL_WEAPON_DZ, /* Primary */

                        /* shell casing type */
                        shell_casing_type = _shell_casing_pistol,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* Fusion Pistol */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_plasma_pistol, powerup_type = NONE, weapon_class = _dual_function_class, flags = _weapon_overloads | _weapon_fires_under_media | _weapon_triggers_share_ammo,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/3, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE, bob_amplitude = FIXED_ONE/25, kick_height = FIXED_ONE/8, reload_height = FIXED_ONE,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _fusion_idle, firing_shape = _fusion_firing, reloading_shape = NONE,
                unused = NONE,
                charging_shape = NONE, charged_shape = _fusion_charged,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/3, await_reload_ticks = TICKS_PER_SECOND/2, loading_ticks = TICKS_PER_SECOND/2, finish_loading_ticks = TICKS_PER_SECOND/2, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 20,

                        /* Ammunition type */
                        ammunition_type = _i_plasma_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/6, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_fusion_firing, click_sound = _snd_empty_gun, charging_sound = _snd_fusion_charging, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_fusion_bolt_minor,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = 0, dz = -4*NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 20,  // this should not be used...

                        /* Ammunition type */
                        ammunition_type = _i_plasma_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = 5, recovery_ticks = 4, charging_ticks = TICKS_PER_SECOND/2,

                        /* recoil magnitude */
                        recoil_magnitude = 20,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_major_fusion_firing, click_sound = _snd_empty_gun, charging_sound = _snd_fusion_charging, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = _snd_major_fusion_charged,

                        /* projectile type */
                        projectile_type = _projectile_fusion_bolt_major,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = 0, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* Assault Rifle */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_assault_rifle, powerup_type = NONE, weapon_class = _multipurpose_class, flags = _weapon_is_automatic,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/5, /* firing intensity, firing decay */


                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/6, bob_amplitude = FIXED_ONE/35, kick_height = FIXED_ONE/16, reload_height = 3*FIXED_ONE/4,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _assault_rifle_idle, firing_shape = _assault_rifle_firing, reloading_shape = _assault_rifle_reloading,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/2, await_reload_ticks = TICKS_PER_SECOND/3, loading_ticks = TICKS_PER_SECOND/3, finish_loading_ticks = TICKS_PER_SECOND/3, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 52,

                        /* Ammunition type */
                        ammunition_type = _i_assault_rifle_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = 0, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_assault_rifle_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = _snd_assault_rifle_shell_casings, reloading_sound = _snd_assault_rifle_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_rifle_bullet,

                        /* theta error */
                        theta_error = 10,

                        /* dx, dz */
                        dx = 0, dz = -NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = _shell_casing_assault_rifle,

                        /* burst count */
                        burst_count = 0
                    },
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 7,

                        /* Ammunition type */
                        ammunition_type = _i_assault_grenade_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = TICKS_PER_SECOND/6, recovery_ticks = (3*TICKS_PER_SECOND)/4 - TICKS_PER_SECOND/6, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 40,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_grenade_launcher_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_grenade,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = -5*NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* Rocket Launcher */
            new weapon_definition
            {
                /* item type, powerup_type, item class, item flags */
                item_type = _i_missile_launcher, powerup_type = NONE, weapon_class = _normal_class, flags = _no_flags,

                firing_light_intensity = FIXED_ONE, firing_intensity_decay_ticks = TICKS_PER_SECOND/3, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = (3*FIXED_ONE)/4, bob_amplitude = FIXED_ONE/50, kick_height = FIXED_ONE/20, reload_height = FIXED_ONE,

                /* horizontal positioning.. */
                idle_width = (FIXED_ONE_HALF/4), horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _missile_launcher_idle, firing_shape = _missile_launcher_firing, reloading_shape = NONE,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND, await_reload_ticks = TICKS_PER_SECOND, loading_ticks = TICKS_PER_SECOND, finish_loading_ticks = TICKS_PER_SECOND, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 2,

                        /* Ammunition type */
                        ammunition_type = _i_missile_launcher_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = TICKS_PER_SECOND/2, recovery_ticks = TICKS_PER_SECOND/10, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 100,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_rocket_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_rocket,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = -WORLD_ONE_FOURTH, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },

                    /* unused */
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_fist,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* flamethrower */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_flamethrower, powerup_type = NONE, weapon_class = _normal_class, flags = _weapon_is_automatic,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/3, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE, bob_amplitude = FIXED_ONE/35, kick_height = FIXED_ONE/15, reload_height = FIXED_ONE/2,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _flamethrower_idle, firing_shape = _flamethrower_firing, reloading_shape = NONE,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND, await_reload_ticks = (2*TICKS_PER_SECOND)/3, loading_ticks = (2*TICKS_PER_SECOND)/3, finish_loading_ticks = (2*TICKS_PER_SECOND)/3, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 7*TICKS_PER_SECOND,

                        /* Ammunition type */
                        ammunition_type = _i_flamethrower_canister,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 2,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_flamethrower, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_flamethrower_burst,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 20, dz = -50,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },

                    /* unused */
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_fist,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = 0,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* alien weapon */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_alien_shotgun, powerup_type = NONE, weapon_class = _multipurpose_class, flags = _weapon_is_automatic | _weapon_disappears_after_use | _weapon_has_random_ammo_on_pickup | _weapon_triggers_share_ammo | _weapon_secondary_has_angular_flipping,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/5, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/6, bob_amplitude = FIXED_ONE/35, kick_height = FIXED_ONE/16, reload_height = 3*FIXED_ONE/4,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _alien_weapon_idle, firing_shape = _alien_weapon_firing, reloading_shape = NONE,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/2, await_reload_ticks = TICKS_PER_SECOND/3, loading_ticks = TICKS_PER_SECOND/3, finish_loading_ticks = TICKS_PER_SECOND/3, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 2000,

                        /* Ammunition type */
                        ammunition_type = _i_alien_shotgun_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = 0, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_enforcer_attack, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_alien_weapon,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = 0, dz = -8*NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },

                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 50,

                        /* Ammunition type */
                        ammunition_type = _i_alien_shotgun_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = 0, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_enforcer_attack, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_alien_weapon,

                        /* theta error */
                        theta_error = 1,

                        /* dx, dz */
                        dx = 0, dz = -8*NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            // #define SHOTGUN_BURST_COUNT 10
            // #define SHOTGUN_SPREAD 5
            /* Shotgun- dual fisted */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_shotgun, powerup_type = NONE, weapon_class = _twofisted_pistol_class, flags = _weapon_reloads_in_one_hand,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/8, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/15, bob_amplitude = FIXED_ONE/25, kick_height = FIXED_ONE/8, reload_height = FIXED_ONE,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _shotgun_idle, firing_shape = _shotgun_firing, reloading_shape = _shotgun_reloading,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/3, await_reload_ticks = 5, loading_ticks = 5, finish_loading_ticks = 5, powerup_ticks = 0, // was NONE

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = SHOTGUN_BURST_COUNT,

                        /* Ammunition type */
                        ammunition_type = _i_shotgun_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 25,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_shotgun_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = _snd_shotgun_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_shotgun_bullet,

                        /* theta error */
                        theta_error = SHOTGUN_SPREAD,

                        /* dx, dz */
                        dx = (WORLD_ONE_FOURTH/6), dz = -NORMAL_WEAPON_DZ, /* Primary */

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = SHOTGUN_BURST_COUNT
                    },

                    /* left weapon (for consistency)... */
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = SHOTGUN_BURST_COUNT,

                        /* Ammunition type */
                        ammunition_type = _i_shotgun_magazine,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 25,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_shotgun_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = _snd_shotgun_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_shotgun_bullet,

                        /* theta error */
                        theta_error = SHOTGUN_SPREAD,

                        /* dx, dz */
                        dx = (WORLD_ONE_FOURTH/6), dz = -NORMAL_WEAPON_DZ, /* Primary */

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = SHOTGUN_BURST_COUNT
                    }
                }
            },

            /* The Ball- Don't Drop It. */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_red_ball, powerup_type = NONE, weapon_class = _normal_class, flags = 0,

                firing_light_intensity = 0, firing_intensity_decay_ticks = 0, /* firing intensity, firing decay */

                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/15, bob_amplitude = FIXED_ONE/15, kick_height = FIXED_ONE/16, reload_height = 0,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _collection_player,
                idle_shape = 30, firing_shape = 30, reloading_shape = 30,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/4, await_reload_ticks = 0, loading_ticks = 0, finish_loading_ticks = 0, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_ball_dropped,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = -150, /* Primary */

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    },

                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 1,

                        /* Ammunition type */
                        ammunition_type = NONE,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = TICKS_PER_SECOND/3, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 0,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = NONE, click_sound = NONE, charging_sound = NONE, shell_casing_sound = NONE, reloading_sound = NONE, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_ball_dropped,

                        /* theta error */
                        theta_error = 0,

                        /* dx, dz */
                        dx = 0, dz = 0, /* Primary */

                        /* shell casing type */
                        shell_casing_type = NONE,

                        /* burst count */
                        burst_count = 0
                    }
                }
            },

            /* LP addition: SMG */
            new weapon_definition
            {
                /* item type, powerup type, item class, item flags */
                item_type = _i_smg, powerup_type = NONE, weapon_class = _normal_class, flags = _weapon_is_automatic|_weapon_fires_under_media,

                firing_light_intensity = 3*FIXED_ONE/4, firing_intensity_decay_ticks = TICKS_PER_SECOND/5, /* firing intensity, firing decay */


                /* idle height, bob amplitude, kick height, reload height */
                idle_height = FIXED_ONE+FIXED_ONE/6, bob_amplitude = FIXED_ONE/35, kick_height = FIXED_ONE/16, reload_height = 3*FIXED_ONE/4,

                /* horizontal positioning.. */
                idle_width = FIXED_ONE_HALF, horizontal_amplitude = 0,

                /* collection, idle, firing, reloading shapes; shell casing, charging, charged */
                collection = _weapon_in_hand_collection,
                idle_shape = _smg_idle, firing_shape = _smg_firing, reloading_shape = _smg_reloading,
                unused = NONE,
                charging_shape = NONE, charged_shape = NONE,

                /* ready/await/load/finish/powerup rounds ticks */
                ready_ticks = TICKS_PER_SECOND/2, await_reload_ticks = TICKS_PER_SECOND/3, loading_ticks = TICKS_PER_SECOND/3, finish_loading_ticks = TICKS_PER_SECOND/3, powerup_ticks = 0,

                weapons_by_trigger = new trigger_definition[NUMBER_OF_TRIGGERS]
                {
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 32,

                        /* Ammunition type */
                        ammunition_type = _i_smg_ammo,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = 0, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_smg_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = _snd_assault_rifle_shell_casings, reloading_sound = _snd_smg_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_smg_bullet,

                        /* theta error */
                        theta_error = 3,

                        /* dx, dz */
                        dx = 0, dz = -NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = _shell_casing_smg,

                        /* burst count */
                        burst_count = 2
                    },
                    new trigger_definition
                    {
                        /* rounds per magazine */
                        rounds_per_magazine = 32,

                        /* Ammunition type */
                        ammunition_type = _i_smg_ammo,

                        /* Ticks per round, recovery ticks, charging ticks */
                        ticks_per_round = NONE, recovery_ticks = 0, charging_ticks = 0,

                        /* recoil magnitude */
                        recoil_magnitude = 5,

                        /* firing, click, charging, shell casing, reload sound */
                        firing_sound = _snd_smg_firing, click_sound = _snd_empty_gun, charging_sound = NONE, shell_casing_sound = _snd_assault_rifle_shell_casings, reloading_sound = _snd_smg_reloading, charged_sound = NONE,

                        /* projectile type */
                        projectile_type = _projectile_smg_bullet,

                        /* theta error */
                        theta_error = 3,

                        /* dx, dz */
                        dx = 0, dz = -NORMAL_WEAPON_DZ,

                        /* shell casing type */
                        shell_casing_type = _shell_casing_smg,

                        /* burst count */
                        burst_count = 2
                    }
                }
            },
        };
    }
}
