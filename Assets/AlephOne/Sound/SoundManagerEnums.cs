// Port of Aleph One: Source_Files/Sound/SoundManagerEnums.h
//
// The sound codes only: nothing here plays sound.
using static AlephOne.cstypes;

namespace AlephOne
{
    public static class SoundManagerEnums
    {
        // ghs: moved these here because SoundManager's header file is enormous already

        /* ---------- sound codes */

        /* ambient sound codes */
        public const short _ambient_snd_water = 0;
        public const short _ambient_snd_sewage = 1;
        public const short _ambient_snd_lava = 2;
        public const short _ambient_snd_goo = 3;
        public const short _ambient_snd_under_media = 4;
        public const short _ambient_snd_wind = 5;
        public const short _ambient_snd_waterfall = 6;
        public const short _ambient_snd_siren = 7;
        public const short _ambient_snd_fan = 8;
        public const short _ambient_snd_spht_door = 9;
        public const short _ambient_snd_spht_platform = 10;
        public const short _ambient_snd_heavy_spht_door = 11;
        public const short _ambient_snd_heavy_spht_platform = 12;
        public const short _ambient_snd_light_machinery = 13;
        public const short _ambient_snd_heavy_machinery = 14;
        public const short _ambient_snd_transformer = 15;
        public const short _ambient_snd_sparking_transformer = 16;
        public const short _ambient_snd_machine_binder = 17;
        public const short _ambient_snd_machine_bookpress = 18;
        public const short _ambient_snd_machine_puncher = 19;
        public const short _ambient_snd_electric = 20;
        public const short _ambient_snd_alarm = 21;
        public const short _ambient_snd_night_wind = 22;
        public const short _ambient_snd_pfhor_door = 23;
        public const short _ambient_snd_pfhor_platform = 24;
        public const short _ambient_snd_alien_noise1 = 25;
        public const short _ambient_snd_alien_noise2 = 26;
        // LP addition:
        public const short _ambient_snd_alien_harmonics = 27;

        public const short NUMBER_OF_AMBIENT_SOUND_DEFINITIONS = 28;

        /* random sound codes */
        public const short _random_snd_water_drip = 0;
        public const short _random_snd_surface_explosion = 1;
        public const short _random_snd_underground_explosion = 2;
        public const short _random_snd_owl = 3;
        // LP addition:
        public const short _random_snd_creak = 4;

        public const short NUMBER_OF_RANDOM_SOUND_DEFINITIONS = 5;

        /* sound codes */
        public const short _snd_startup = 0;
        public const short _snd_teleport_in = 1;
        public const short _snd_teleport_out = 2;
        public const short _snd_body_being_crunched = 3;
        // LP change:
        public const short _snd_creak = 4;
        // _snd_nuclear_hard_death,
        public const short _snd_absorbed = 5;

        public const short _snd_breathing = 6;
        public const short _snd_oxygen_warning = 7;
        public const short _snd_suffocation = 8;

        public const short _snd_energy_refuel = 9;
        public const short _snd_oxygen_refuel = 10;
        public const short _snd_cant_toggle_switch = 11;
        public const short _snd_switch_on = 12;
        public const short _snd_switch_off = 13;
        public const short _snd_puzzle_switch = 14;
        public const short _snd_chip_insertion = 15;
        public const short _snd_pattern_buffer = 16;
        public const short _snd_destroy_control_panel = 17;

        public const short _snd_adjust_volume = 18;
        public const short _snd_got_powerup = 19;
        public const short _snd_got_item = 20;

        public const short _snd_bullet_ricochet = 21;
        public const short _snd_metallic_ricochet = 22;
        public const short _snd_empty_gun = 23;

        public const short _snd_spht_door_opening = 24;
        public const short _snd_spht_door_closing = 25;
        public const short _snd_spht_door_obstructed = 26;

        public const short _snd_spht_platform_starting = 27;
        public const short _snd_spht_platform_stopping = 28;

        public const short _snd_owl = 29;
        // LP change:
        public const short _snd_smg_firing = 30;
        public const short _snd_smg_reloading = 31;
        // _snd_unused2,
        // _snd_unused3,

        public const short _snd_heavy_spht_platform_starting = 32;
        public const short _snd_heavy_spht_platform_stopping = 33;

        public const short _snd_fist_hitting = 34;

        public const short _snd_magnum_firing = 35;
        public const short _snd_magnum_reloading = 36;

        public const short _snd_assault_rifle_firing = 37;
        public const short _snd_grenade_launcher_firing = 38;
        public const short _snd_grenade_exploding = 39;
        public const short _snd_grenade_flyby = 40;

        public const short _snd_fusion_firing = 41;
        public const short _snd_fusion_exploding = 42;
        public const short _snd_fusion_flyby = 43;
        public const short _snd_fusion_charging = 44;

        public const short _snd_rocket_exploding = 45;
        public const short _snd_rocket_flyby = 46;
        public const short _snd_rocket_firing = 47;

        public const short _snd_flamethrower = 48;

        public const short _snd_body_falling = 49;
        public const short _snd_body_exploding = 50;
        public const short _snd_bullet_hitting_flesh = 51;

        public const short _snd_fighter_activate = 52;
        public const short _snd_fighter_wail = 53;
        public const short _snd_fighter_scream = 54;
        public const short _snd_fighter_chatter = 55;
        public const short _snd_fighter_attack = 56;
        public const short _snd_fighter_projectile_hit = 57;
        public const short _snd_fighter_projectile_flyby = 58;

        public const short _snd_compiler_attack = 59;
        public const short _snd_compiler_death = 60;
        public const short _snd_compiler_hit = 61;
        public const short _snd_compiler_projectile_flyby = 62;
        public const short _snd_compiler_projectile_hit = 63;

        public const short _snd_cyborg_moving = 64;
        public const short _snd_cyborg_attack = 65;
        public const short _snd_cyborg_hit = 66;
        public const short _snd_cyborg_death = 67;
        public const short _snd_cyborg_projectile_bounce = 68;
        public const short _snd_cyborg_projectile_hit = 69;
        public const short _snd_cyborg_projectile_flyby = 70;

        public const short _snd_hummer_activate = 71;
        public const short _snd_hummer_start_attack = 72;
        public const short _snd_hummer_attack = 73;
        public const short _snd_hummer_dying = 74;
        public const short _snd_hummer_death = 75;
        public const short _snd_hummer_projectile_hit = 76;
        public const short _snd_hummer_projectile_flyby = 77;

        public const short _snd_human_wail = 78;
        public const short _snd_human_scream = 79;
        public const short _snd_human_hit = 80;
        public const short _snd_human_chatter = 81;
        public const short _snd_assimilated_human_chatter = 82;
        public const short _snd_human_trash_talk = 83;
        public const short _snd_human_apology = 84;
        public const short _snd_human_activation = 85;
        public const short _snd_human_clear = 86;
        public const short _snd_human_stop_shooting_me_you_bastard = 87;
        public const short _snd_human_area_secure = 88;
        public const short _snd_kill_the_player = 89;

        public const short _snd_water = 90;
        public const short _snd_sewage = 91;
        public const short _snd_lava = 92;
        public const short _snd_goo = 93;
        public const short _snd_under_media = 94;
        public const short _snd_wind = 95;
        public const short _snd_waterfall = 96;
        public const short _snd_siren = 97;
        public const short _snd_fan = 98;
        public const short _snd_spht_door = 99;
        public const short _snd_spht_platform = 100;
        // LP change:
        public const short _snd_alien_harmonics = 101;
        // _snd_unused4,
        public const short _snd_heavy_spht_platform = 102;
        public const short _snd_light_machinery = 103;
        public const short _snd_heavy_machinery = 104;
        public const short _snd_transformer = 105;
        public const short _snd_sparking_transformer = 106;

        public const short _snd_water_drip = 107;

        public const short _snd_walking_in_water = 108;
        public const short _snd_exit_water = 109;
        public const short _snd_enter_water = 110;
        public const short _snd_small_water_splash = 111;
        public const short _snd_medium_water_splash = 112;
        public const short _snd_large_water_splash = 113;

        public const short _snd_walking_in_lava = 114;
        public const short _snd_enter_lava = 115;
        public const short _snd_exit_lava = 116;
        public const short _snd_small_lava_splash = 117;
        public const short _snd_medium_lava_splash = 118;
        public const short _snd_large_lava_splash = 119;

        public const short _snd_walking_in_sewage = 120;
        public const short _snd_exit_sewage = 121;
        public const short _snd_enter_sewage = 122;
        public const short _snd_small_sewage_splash = 123;
        public const short _snd_medium_sewage_splash = 124;
        public const short _snd_large_sewage_splash = 125;

        public const short _snd_walking_in_goo = 126;
        public const short _snd_exit_goo = 127;
        public const short _snd_enter_goo = 128;
        public const short _snd_small_goo_splash = 129;
        public const short _snd_medium_goo_splash = 130;
        public const short _snd_large_goo_splash = 131;

        public const short _snd_major_fusion_firing = 132;
        public const short _snd_major_fusion_charged = 133;

        public const short _snd_assault_rifle_reloading = 134;
        public const short _snd_assault_rifle_shell_casings = 135;

        public const short _snd_shotgun_firing = 136;
        public const short _snd_shotgun_reloading = 137;

        public const short _snd_ball_bounce = 138;
        public const short _snd_you_are_it = 139;
        public const short _snd_got_ball = 140;

        public const short _snd_computer_interface_logon = 141;
        public const short _snd_computer_interface_logout = 142;
        public const short _snd_computer_interface_page = 143;

        public const short _snd_heavy_spht_door = 144;
        public const short _snd_heavy_spht_door_opening = 145;
        public const short _snd_heavy_spht_door_closing = 146;
        public const short _snd_heavy_spht_door_open = 147;
        public const short _snd_heavy_spht_door_closed = 148;
        public const short _snd_heavy_spht_door_obstructed = 149;

        public const short _snd_hunter_activate = 150;
        public const short _snd_hunter_attack = 151;
        public const short _snd_hunter_dying = 152;
        public const short _snd_hunter_landing = 153;
        public const short _snd_hunter_exploding = 154;
        public const short _snd_hunter_projectile_hit = 155;
        public const short _snd_hunter_projectile_flyby = 156;

        public const short _snd_enforcer_activate = 157;
        public const short _snd_enforcer_attack = 158;
        public const short _snd_enforcer_projectile_hit = 159;
        public const short _snd_enforcer_projectile_flyby = 160;

        public const short _snd_yeti_melee_attack = 161;
        public const short _snd_yeti_melee_attack_hit = 162;
        public const short _snd_yeti_projectile_attack = 163;
        public const short _snd_yeti_projectile_sewage_attack_hit = 164;
        public const short _snd_yeti_projectile_sewage_flyby = 165;
        public const short _snd_yeti_projectile_lava_attack_hit = 166;
        public const short _snd_yeti_projectile_lava_flyby = 167;
        public const short _snd_yeti_dying = 168;

        public const short _snd_machine_binder = 169;
        public const short _snd_machine_bookpress = 170;
        public const short _snd_machine_puncher = 171;
        public const short _snd_electric = 172;
        public const short _snd_alarm = 173;
        public const short _snd_night_wind = 174;

        public const short _snd_surface_explosion = 175;
        public const short _snd_underground_explosion = 176;

        public const short _snd_defender_attack = 177;
        public const short _snd_defender_hit = 178;
        public const short _snd_defender_flyby = 179;
        public const short _snd_defender_being_hit = 180;
        public const short _snd_defender_exploding = 181;

        public const short _snd_tick_chatter = 182;
        public const short _snd_tick_falling = 183;
        public const short _snd_tick_flapping = 184;
        public const short _snd_tick_exploding = 185;

        public const short _snd_ceiling_lamp_exploding = 186;

        public const short _snd_pfhor_platform_starting = 187;
        public const short _snd_pfhor_platform_stopping = 188;
        public const short _snd_pfhor_platform = 189;

        public const short _snd_pfhor_door_opening = 190;
        public const short _snd_pfhor_door_closing = 191;
        public const short _snd_pfhor_door_obstructed = 192;
        public const short _snd_pfhor_door = 193;

        public const short _snd_pfhor_switch_off = 194;
        public const short _snd_pfhor_switch_on = 195;

        public const short _snd_juggernaut_firing = 196;
        public const short _snd_juggernaut_warning = 197;
        public const short _snd_juggernaut_exploding = 198;
        public const short _snd_juggernaut_preparing_to_fire = 199;

        public const short _snd_enforcer_exploding = 200;

        public const short _snd_alien_noise1 = 201;
        public const short _snd_alien_noise2 = 202;

        // LP addition: this means that there are more Moo sound types
        // than M2 ones.
        public const short _snd_civilian_fusion_wail = 203;
        public const short _snd_civilian_fusion_scream = 204;
        public const short _snd_civilian_fusion_hit = 205;
        public const short _snd_civilian_fusion_chatter = 206;
        public const short _snd_assimilated_civilian_fusion_chatter = 207;
        public const short _snd_civilian_fusion_trash_talk = 208;
        public const short _snd_civilian_fusion_apology = 209;
        public const short _snd_civilian_fusion_activation = 210;
        public const short _snd_civilian_fusion_clear = 211;
        public const short _snd_civilian_fusion_stop_shooting_me_you_bastard = 212;
        public const short _snd_civilian_fusion_area_secure = 213;
        public const short _snd_civilian_fusion_kill_the_player = 214;

        public const short NUMBER_OF_SOUND_DEFINITIONS = 215;

        // enum
        public const short NUMBER_OF_SOUND_VOLUME_LEVELS = 8;

        public const short MAXIMUM_SOUND_VOLUME_BITS = 8;
        public const short MAXIMUM_SOUND_VOLUME = 1<<MAXIMUM_SOUND_VOLUME_BITS;

        // sound sources
        public const short _8bit_22k_source = 0;
        public const short _16bit_22k_source = 1;

        public const short NUMBER_OF_SOUND_SOURCES = 2;

        public enum AudioFormat
        {
            _8_bit,
            _16_bit,
            _32_float
        }

        public enum ChannelType
        {
            _mono = 1,
            _stereo = 2,
            _quad = 4,
            _5_1 = 6,
            _6_1 = 7,
            _7_1 = 8
        }

        // initialization flags (some of these are used by the prefs, which fixes them)
        public const short _dynamic_tracking_flag = 0x0002; /* tracks sound sources during idle_proc [prefs] */
        public const short _doppler_shift_flag = 0x0004; /* adjusts sound pitch during idle_proc */
        public const short _ambient_sound_flag = 0x0008; /* plays and tracks ambient sounds [prefs] */
        public const short _16bit_sound_flag = 0x0010; /* loads 16bit audio instead of 8bit [prefs] */
        public const short _more_sounds_flag = 0x0020; /* loads all permutations; only loads #0 if false [prefs] */
        public const short _3d_sounds_flag = 0x0040; /* enable 3D sounds instead of emulating 2D panning */
        public const short _hrtf_flag = 0x0080; /* play sounds using HRTF [prefs] */
        public const short _extra_memory_flag = 0x0100; /* double usual memory */
        public const short _extra_extra_memory_flag = 0x0200; /* LP: quadruple usual memory, because RAM is more available */
        public const short _lower_restart_delay = 0x0400; /* ghs: restart sounds faster */
        public const short _mute_dialogs = 0x0800;

        // _sound_obstructed_proc() flags
        public const short _sound_was_obstructed = 0x0001; // no clear path between source and listener
        public const short _sound_was_media_obstructed = 0x0002; // source and listener are on different sides of the media
        public const short _sound_was_media_muffled = 0x0004; // source and listener both under the same media

        // frequencies
        public const int _lower_frequency = FIXED_ONE-FIXED_ONE/8;
        public const int _normal_frequency = FIXED_ONE;
        public const int _higher_frequency = FIXED_ONE+FIXED_ONE/8;
        public const int _m1_high_frequency = FIXED_ONE+FIXED_ONE/4;
    }
}
