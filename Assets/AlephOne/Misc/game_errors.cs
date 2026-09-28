// Port of Aleph One: Source_Files/Misc/game_errors.h, game_errors.cpp
using static AlephOne.csalerts;

namespace AlephOne
{
    public static class game_errors
    {
        // types
        public const short systemError = 0;
        public const short gameError = 1;
        public const short NUMBER_OF_TYPES = 2;

        // Game Errors
        public const short errNone = 0;
        public const short errMapFileNotSet = 1;
        public const short errIndexOutOfRange = 2;
        public const short errTooManyOpenFiles = 3;
        public const short errUnknownWadVersion = 4;
        public const short errWadIndexOutOfRange = 5;
        public const short errServerDied = 6;
        public const short errUnsyncOnLevelChange = 7;
        public const short NUMBER_OF_GAME_ERRORS = 8;

        private static short last_type = systemError;
        private static short last_error = errNone;

        public static void set_game_error(short type, short error_code)
        {
            assert(type >= 0 && type < NUMBER_OF_TYPES);
            last_type = type;
            last_error = error_code;
            // #ifdef DEBUG: if(type==gameError) assert(error_code>=0 && error_code<NUMBER_OF_GAME_ERRORS);
        }

        public static short get_game_error(out short type)
        {
            type = last_type;
            return last_error;
        }

        public static bool error_pending()
        {
            return (last_error != 0);
        }

        public static void clear_game_error()
        {
            last_error = 0;
            last_type = 0;
        }
    }
}
