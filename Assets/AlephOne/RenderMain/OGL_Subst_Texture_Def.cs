// Port of Aleph One: Source_Files/RenderMain/OGL_Subst_Texture_Def.h, OGL_Subst_Texture_Def.cpp, and the parts of
// OGL_Texture_Def.h they use (substitute textures for walls and sprites, from MML)
//
// Not ported: loading and unloading the images (OGL_LoadTextures, OGL_UnloadTextures, OGL_TextureOptionsBase::Load),
// which ForgePlus does itself.
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    public class OGL_TextureOptions
    {
        // OGL_TextureOptionsBase
        public short OpacityType = OGL_Subst_Texture_Def.OGL_OpacType_Crisp;
        public float OpacityScale = 1;
        public float OpacityShift = 0;

        // Names of files to load; these will be extended ones with directory specifications
        public FileSpecifier NormalColors = new FileSpecifier(), NormalMask = new FileSpecifier(), GlowColors = new FileSpecifier(), GlowMask = new FileSpecifier(), OffsetMap = new FileSpecifier();

        public bool NormalIsPremultiplied, GlowIsPremultiplied;
        public short actual_height, actual_width;
        public short Type = -1;
        public short NormalBlend = OGL_Subst_Texture_Def.OGL_BlendType_Crossfade, GlowBlend = OGL_Subst_Texture_Def.OGL_BlendType_Crossfade;
        public float BloomScale = 0;
        public float BloomShift = 0;
        public float GlowBloomScale = 1;
        public float GlowBloomShift = 0;
        public float LandscapeBloom = 0.5f;
        public float MinGlowIntensity = 1;

        // OGL_TextureOptions
        public bool VoidVisible;
        public short TileRatioExp;
        public short Billboard;

        public OGL_TextureOptions Clone()
        {
            var clone = (OGL_TextureOptions) MemberwiseClone();
            clone.NormalColors = new FileSpecifier(NormalColors.GetPath());
            clone.NormalMask = new FileSpecifier(NormalMask.GetPath());
            clone.GlowColors = new FileSpecifier(GlowColors.GetPath());
            clone.GlowMask = new FileSpecifier(GlowMask.GetPath());
            clone.OffsetMap = new FileSpecifier(OffsetMap.GetPath());
            return clone;
        }
    }

    [NoAutoStaticsCleanup]
    public static class OGL_Subst_Texture_Def
    {
        /* ---------- constants (OGL_Texture_Def.h) */

        // Bitmap sets: a CLUT's, then infravision's and silhouettes' (for all CLUTs, then for each)
        public const short INFRAVISION_BITMAP_SET = MAXIMUM_CLUTS_PER_COLLECTION;
        public const short SILHOUETTE_BITMAP_SET = INFRAVISION_BITMAP_SET + 1;
        public const short INFRAVISION_BITMAP_CLUTSPECIFIC = SILHOUETTE_BITMAP_SET + 1;
        public const short SILHOUETTE_BITMAP_CLUTSPECIFIC = 2 * MAXIMUM_CLUTS_PER_COLLECTION + 2;
        public const short NUMBER_OF_OPENGL_BITMAP_SETS = 3 * MAXIMUM_CLUTS_PER_COLLECTION + 2;

        public const short ALL_CLUT_VARIANTS = -1;
        public const short CLUT_VARIANT_NORMAL = 0;
        public const short CLUT_VARIANT_INFRAVISION = 1;
        public const short CLUT_VARIANT_SILHOUETTE = 2;
        public const short NUMBER_OF_CLUT_VARIANTS = 3;

        public const short ALL_CLUTS = -1;

        public const short OGL_OpacType_Crisp = 0;
        public const short OGL_OpacType_Flat = 1;
        public const short OGL_OpacType_Avg = 2;
        public const short OGL_OpacType_Max = 3;
        public const short OGL_NUMBER_OF_OPACITY_TYPES = 4;

        public const short OGL_BlendType_Crossfade = 0;
        public const short OGL_BlendType_Add = 1;
        public const short OGL_BlendType_Crossfade_Premult = 2;
        public const short OGL_BlendType_Add_Premult = 3;
        public const short OGL_NUMBER_OF_BLEND_TYPES = 4;

        // BillboardType
        private const short BillboardType_User = 0;
        private const short BillboardType_XY = 3;

        public static bool IsInfravisionTable(short CLUT)
        {
            return (CLUT == INFRAVISION_BITMAP_SET ||
                    (CLUT >= INFRAVISION_BITMAP_CLUTSPECIFIC &&
                     CLUT < INFRAVISION_BITMAP_CLUTSPECIFIC + MAXIMUM_CLUTS_PER_COLLECTION));
        }

        public static bool IsSilhouetteTable(short CLUT)
        {
            return (CLUT == SILHOUETTE_BITMAP_SET ||
                    (CLUT >= SILHOUETTE_BITMAP_CLUTSPECIFIC &&
                     CLUT < SILHOUETTE_BITMAP_CLUTSPECIFIC + MAXIMUM_CLUTS_PER_COLLECTION));
        }

        /* ---------- globals */

        // Texture-options stuff;
        // defaults for whatever might need them
        private static readonly OGL_TextureOptions DefaultTextureOptions = new OGL_TextureOptions();

        // Each collection's options, by CLUT and bitmap
        private static readonly Dictionary<(short CLUT, short Bitmap), OGL_TextureOptions>[] Collections = CreateCollections();

        private static Dictionary<(short CLUT, short Bitmap), OGL_TextureOptions>[] CreateCollections()
        {
            var collections = new Dictionary<(short CLUT, short Bitmap), OGL_TextureOptions>[NUMBER_OF_COLLECTIONS];
            for (var c = 0; c < NUMBER_OF_COLLECTIONS; c++)
            {
                collections[c] = new Dictionary<(short CLUT, short Bitmap), OGL_TextureOptions>();
            }

            return collections;
        }

        // Deletes a collection's texture-options sequences
        public static void TODelete(short Collection)
        {
            Collections[Collection].Clear();
        }

        // Deletes all of them
        private static void TODelete_All()
        {
            for (short c = 0; c < NUMBER_OF_COLLECTIONS; c++) TODelete(c);
        }

        public static int OGL_CountTextures(short Collection)
        {
            return Collections[Collection].Count;
        }

        // ForgePlus: every collection's options, to tell whether they've changed
        public static IEnumerable<(short Collection, short CLUT, short Bitmap, OGL_TextureOptions Options)> OGL_AllTextureOptions()
        {
            for (short collection = 0; collection < NUMBER_OF_COLLECTIONS; collection++)
            {
                foreach (var entry in Collections[collection])
                {
                    yield return (collection, entry.Key.CLUT, entry.Key.Bitmap, entry.Value);
                }
            }
        }

        public static OGL_TextureOptions OGL_GetTextureOptions(short Collection, short CLUT, short Bitmap)
        {
            if (Collections[Collection].TryGetValue((CLUT, Bitmap), out var options))
            {
                return options;
            }

            if (IsInfravisionTable(CLUT) && CLUT != INFRAVISION_BITMAP_SET)
            {
                if (Collections[Collection].TryGetValue((INFRAVISION_BITMAP_SET, Bitmap), out options))
                {
                    return options;
                }
            }

            if (IsSilhouetteTable(CLUT) && CLUT != SILHOUETTE_BITMAP_SET)
            {
                if (Collections[Collection].TryGetValue((SILHOUETTE_BITMAP_SET, Bitmap), out options))
                {
                    return options;
                }
            }

            if (Collections[Collection].TryGetValue((ALL_CLUTS, Bitmap), out options))
            {
                return options;
            }

            return DefaultTextureOptions;
        }

        public static void reset_mml_opengl_texture()
        {
            TODelete_All();
        }

        public static void parse_mml_opengl_texture(InfoTree root)
        {
            short coll = 0;
            if (!root.read_indexed("coll", ref coll, NUMBER_OF_COLLECTIONS))
                return;
            short bitmap = 0;
            if (!root.read_indexed("bitmap", ref bitmap, short.MaxValue + 1))
                return;
            short clut = ALL_CLUTS;
            root.read_attr_bounded("clut", ref clut, ALL_CLUTS, SILHOUETTE_BITMAP_SET);
            short clut_variant = CLUT_VARIANT_NORMAL;
            root.read_attr_bounded("clut_variant", ref clut_variant, ALL_CLUT_VARIANTS, NUMBER_OF_CLUT_VARIANTS - 1);

            // translate deprecated clut options
            if (clut == INFRAVISION_BITMAP_SET)
            {
                clut = ALL_CLUTS;
                clut_variant = CLUT_VARIANT_INFRAVISION;
            }
            else if (clut == SILHOUETTE_BITMAP_SET)
            {
                clut = ALL_CLUTS;
                clut_variant = CLUT_VARIANT_SILHOUETTE;
            }

            // loop so we can apply "all variants" mode if needed
            for (short var = CLUT_VARIANT_NORMAL; var < NUMBER_OF_CLUT_VARIANTS; var++)
            {
                if (clut_variant != ALL_CLUT_VARIANTS && clut_variant != var)
                    continue;

                // translate clut+variant to internal clut number
                var actual_clut = clut;
                if (var == CLUT_VARIANT_INFRAVISION)
                {
                    if (clut == ALL_CLUTS)
                        actual_clut = INFRAVISION_BITMAP_SET;
                    else
                        actual_clut = (short) (INFRAVISION_BITMAP_CLUTSPECIFIC + clut);
                }
                else if (var == CLUT_VARIANT_SILHOUETTE)
                {
                    if (clut == ALL_CLUTS)
                        actual_clut = SILHOUETTE_BITMAP_SET;
                    else
                        actual_clut = (short) (SILHOUETTE_BITMAP_CLUTSPECIFIC + clut);
                }

                if (!Collections[coll].TryGetValue((actual_clut, bitmap), out var def))
                {
                    def = DefaultTextureOptions.Clone();
                    Collections[coll][(actual_clut, bitmap)] = def;
                }

                root.read_indexed("opac_type", ref def.OpacityType, OGL_NUMBER_OF_OPACITY_TYPES);
                root.read_attr("opac_scale", ref def.OpacityScale);
                root.read_attr("opac_shift", ref def.OpacityShift);
                root.read_attr("void_visible", ref def.VoidVisible);
                root.read_path("normal_image", def.NormalColors);
                root.read_path("offset_image", def.OffsetMap);
                root.read_path("normal_mask", def.NormalMask);
                root.read_path("glow_image", def.GlowColors);
                root.read_path("glow_mask", def.GlowMask);
                root.read_indexed("normal_blend", ref def.NormalBlend, OGL_NUMBER_OF_BLEND_TYPES);
                root.read_indexed("glow_blend", ref def.GlowBlend, OGL_NUMBER_OF_BLEND_TYPES);
                root.read_attr("actual_height", ref def.actual_height);
                root.read_attr("actual_width", ref def.actual_width);
                root.read_attr("type", ref def.Type);
                root.read_attr("normal_premultiply", ref def.NormalIsPremultiplied);
                root.read_attr("glow_premultiply", ref def.GlowIsPremultiplied);
                root.read_attr("normal_bloom_scale", ref def.BloomScale);
                root.read_attr("normal_bloom_shift", ref def.BloomShift);
                root.read_attr("glow_bloom_scale", ref def.GlowBloomScale);
                root.read_attr("glow_bloom_shift", ref def.GlowBloomShift);
                root.read_attr("landscape_bloom", ref def.LandscapeBloom);
                root.read_attr("minimum_glow_intensity", ref def.MinGlowIntensity);
                root.read_attr("tile_ratio_exp", ref def.TileRatioExp);
                root.read_attr_bounded("billboard", ref def.Billboard, BillboardType_User, BillboardType_XY);
            }
        }

        public static void parse_mml_opengl_txtr_clear(InfoTree root)
        {
            short coll = 0;
            if (root.read_indexed("coll", ref coll, NUMBER_OF_COLLECTIONS))
                TODelete(coll);
            else
                TODelete_All();
        }
    }
}
