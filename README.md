# ForgePlus
--- ABOUT FORGE+ ---

Forge+ is a Unity Engine based map editor for Bungie's Marathon Durandal and Marathon Infinity, as well as the open source project, Aleph One.

Forge+ was originally written by Richard L Harrington, and presently builds upon another open source map editor, Weland, by Gregory Smith (treellama).

Weland on GitHub:
https://github.com/treellama/weland

Viewing capabilities are mostly finished, but editing capabilities are currently limited to:
 - Texture alignment
 - Texture assignment
 - Light assignment


--- ATTAINING & RUNNING A BUILD ---

To run it, simply download a build for your platform from here:
https://github.com/deramscholzara/ForgePlus/releases

...then decompress and run as you would any other program for that OS.

Note for Mac: decompressing with the standard Archive Utility will corrupt the app - you should use a different decompressor, such as:
https://www.keka.io/en/


--- BUILDING YOUR OWN COPY ---

To build your own copy, you'll need to:
 
 - Download and install a version of Unity (from unity.com) matching or exceeding the version specified in ProjectSettings/ProjectVersion.txt (the free license is fine, but will add a watermark on application startup)
 
 - Make sure when you install, you include the platform(s) you intend to build for.  Presently, only tested with:
    - Windows (IL2CPP)
    - Linux (IL2CPP)
    - Mac (Mono)
 
 - Using Unity, open the root of the repo, as this is the root of the Unity project.

 - Follow normal Unity build procedures:
    - File > Build Settings...
    - Select target platform.
    - Click Build or Build & Run.