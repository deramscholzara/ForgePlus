# AlephOne data layer

A C# port of the parts of the [Aleph One](https://github.com/Aleph-One-Marathon/alephone) engine
that read and write Marathon data files (wads, maps, shapes, physics, and the definitions in sounds files). Aleph One is the source of
truth: this code mirrors its source as literally as C# allows, so it can be compared side by side
with the original and kept in sync with it.

Ported from Aleph One commit `6497ea3b6906ee7e2d7b7206b5c760a4039877db` (2026-09-20).
Aleph One is licensed under the GNU GPL v3, the same license as ForgePlus.

## Rules

- **No Unity dependencies.** `AlephOne.asmdef` sets `noEngineReferences`, so nothing here can use
  `UnityEngine`. Conversions to Unity types (textures, meshes, vectors) belong in ForgePlus.
- **Only Aleph One code lives here.** Code that serves ForgePlus alone (choosing a physics model,
  decoding bitmaps for textures, ...) belongs in ForgePlus, which calls the port rather than
  reimplementing what the port can do.
- **Folders mirror `Source_Files/`.** `Files/wad.cpp` becomes `Files/wad.cs`, and so on. `SDL/` holds
  the stand-in for SDL2 (see below), which isn't part of `Source_Files/`.
- **Names are kept verbatim.** Types, fields, constants, enums, and functions keep Aleph One's exact
  (snake_case) names, so `polygon_data.floor_height` and `unpack_polygon_data()` can be found in both
  code bases with the same search.
- **Structs become classes with public fields**, in the same order as the C declarations. Fixed-size
  C arrays become arrays allocated to their full length. `SIZEOF_*` constants are kept and asserted.
  Struct assignment is `Clone()`.
- **Free functions live in a static class named after their source file** (for example
  `wad.read_wad_header`). Constants, enums, and macros live alongside them. Use
  `using static AlephOne.<file>;` to call them unqualified, as the C code does.
- **Packing uses `Packing.cs`**, which mirrors `Packing.h` (`StreamToValue`, `ValueToStream`,
  `StreamToList`, `StreamToBytes`, ...) over a `StreamPointer` that stands in for `uint8* S`. Pack and
  unpack functions take `(StreamPointer S, IList<T> Objects, int Count)`.
- **Errors behave like Aleph One.** Functions return `bool`/`null` where Aleph One does, and record the
  error with `game_errors.set_game_error`. `assert`/`vassert`/`vhalt` throw an `AlephOneAssertion`,
  since in Aleph One they halt.
- **Only omit what an editor can never need** (the running game, networking, rendering, sound
  playback, Lua, MML), and say so in a `Not ported:` comment in the file header or at the point of
  omission.

## Conventions

These are stated here once, instead of in the files.

- **Globals.** Aleph One keeps the loaded level and the physics model in globals. Here they are
  objects whose fields carry the globals' names, passed as the first parameter wherever Aleph One
  reads the globals:
  - `MapLevel` holds the map globals (`static_world`, `EndpointList`, `LightList`, `PlatformList`,
    ...). Aleph One's `dynamic_world->*_count` fields are the counts of those lists.
  - `PhysicsModel` holds the physics globals (`monster_definitions[]`, `effect_definitions[]`,
    `projectile_definitions[]`, `physics_models[]`, `weapon_definitions[]`). The `original_*` tables
    are the built-in definitions, and `init_*_definitions()` copies them into a model. Where Aleph One
    has both `unpack_x(uint8 *Stream, size_t Count)`, for the global array, and
    `unpack_x(uint8 *Stream, x *Objects, size_t Count)`, only the second is ported; pass the model's
    array.
  - The current map file (`set_map_file()`) and physics file (`set_physics_file()`) are parameters.

  Other state stays global as in Aleph One (the shapes file and its collections, the open resource
  files and the files the scenario's pictures come from, the flood map, the temporary index lists of
  `map_constructors`), so only work that doesn't change it can run on other threads: ForgePlus builds
  a level's `PhysicsModel` on a worker thread, and decodes the loaded shapes' bitmaps in parallel,
  which is why the last error (`game_errors`) is kept per thread.
- **Pointers.** A pointer into a buffer is the array and an offset (`byte *buffer` is `buffer,
  buffer_offset`; a bitmap's `pixel8 *row_addresses[]` are offsets into `bitmap_definition.pixels`).
  An output pointer is `out`, or `ref` where Aleph One leaves it unchanged on failure. `out` parameters
  are set first even where Aleph One doesn't set them.
- **Sizes.** Comparisons against `size_t` are unsigned: `(uint) index < (uint) count`, as in
  `csmacros.GetMemberWithBounds`.
- **Commented-out code** is Aleph One code the port doesn't need (mostly the running game's state,
  such as `// dynamic_world->side_count++;`), or Aleph One's own commented-out code. It is kept so the
  files line up with the original.
- **Naming exceptions**, where C# doesn't allow Aleph One's names:
  - `interface.h` is the class `@interface`, and `flood_map()` is `flood_map.flood_map_()`.
  - A header named like its struct (`collection_definition.h`) keeps its constants in the struct's
    class (`collection_definition._wall_collection`).
  - A table whose name is its header's (`item_definitions[]` in `item_definitions.h`) lives in the
    class of the `.cpp` that includes the header (`items.item_definitions`, `scenery.scenery_definitions`,
    `media.media_definitions`), which holds its accessor too.
  - FilmProfile.cpp's functions and globals are `FilmProfileGlobals`.
  - Types with no Aleph One counterpart are PascalCase (`MapLevel`, `PhysicsModel`, `LoadedWad`,
    `StreamPointer`).
- **Tags** are `const uint` literals, so Aleph One's `switch (tag)` statements are kept.
- **Visibility.** Some functions that are `static` in Aleph One are public because ForgePlus uses them
  (`map.Environments`, `scenery.get_scenery_definition`, `import_definitions.get_physics_wad_data`,
  ...). Everything else keeps Aleph One's visibility.
- **Failures.** Where Aleph One checks (an assert, a `NULL` return), the port checks the same way.
  Where Aleph One doesn't (dividing by a zero vertex count, indexing past a table, dereferencing a
  `NULL` definition), the port doesn't either, and the .NET exception (`DivideByZeroException`,
  `IndexOutOfRangeException`, `NullReferenceException`) takes the place of the crash.
- **`// ForgePlus:`** marks code, and behaviour, that isn't Aleph One's: a function, a field, a block
  inside a ported function, or, as a section heading (`/* ---------- ForgePlus ... */`), everything
  below it. The ForgePlus additions are:
  - `MapLevel` and its `LoadedWad`, and `PhysicsModel`.
  - Saving a level whole (`game_wad.save_level()` and what it uses): every chunk a level was loaded
    with, in its original order, so an unedited level is saved byte for byte. Chunks the port doesn't
    load (terminals, scripts, embedded physics, shapes and sounds patches, anything unknown) are kept in
    `MapLevel.loaded_wad` and saved as they were.
  - `game_wad.get_level_directory()`, and `game_wad.process_map_wad_physics()`, which is the physics
    section of `process_map_wad()` on its own.
  - `[NoAutoStaticsCleanup]` on every type with statics. Unity's analyzers require each such type to say
    whether Unity should reset its statics when Play mode starts without a domain reload; the port
    says no with its own copy of the attribute (`CSeries/NoAutoStaticsCleanupAttribute.cs`), and
    ForgePlus resets the loaded shapes and last error itself.
  - `game_errors` keeps its last error per thread.
  - `computer_interface`'s terminal font: Aleph One measures terminal text with its interface font
    (Courier Prime 12), which isn't ported, so `char_width()` and `_get_font_line_height()` give that
    font's metrics (7 pixels a character, 12 a line), and `text_at()` reads a NUL past the end of a
    terminal's text, where Aleph One's buffer carries on.
  - `images.picture_to_surface()` stops at the end of a picture's data (Aleph One would read NOPs forever).
  - The deviations: `new_media()` keeps a medium as loaded (it doesn't mark the slot used or update the
    medium, which would change what is saved), platforms keep their polygons' native heights,
    `process_map_wad()` pads a short map info chunk with zeros, and `load_shapes_patch()` stops at the
    end of a truncated patch.

## SDL

`SDL/SDL_surface.cs` stands in for the software surfaces Aleph One decodes pictures into (`SDL_Surface`,
`SDL_CreateRGBSurface`, `SDL_SetPaletteColors`): 8-bit paletted, 16-bit and 32-bit, their pixels little-endian
and their rows padded to 4 bytes as SDL's are.

`SDL/SDL_rwops.cs` stands in for the parts of SDL2's `SDL_rwops.h` and `SDL_endian.h` that Aleph One
uses: `SDL_RWops` over a file or memory, `SDL_RWread`, `SDL_RWwrite`, `SDL_RWseek`, `SDL_RWtell`,
`SDL_ReadBE16` and `SDL_ReadBE32`, with SDL's results (short counts, zeros past the end, `-1` from a
failed seek, `NULL` for a file that can't be opened). It is where .NET's I/O exceptions become those
results; `FileHandler.cs` (`OpenedFile`, `FileSpecifier`) is built on it as Aleph One's is on SDL.
