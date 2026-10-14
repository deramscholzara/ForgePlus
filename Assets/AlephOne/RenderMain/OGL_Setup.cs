// Port of Aleph One: Source_Files/RenderMain/OGL_Setup.cpp (its MML: substitute textures)
//
// Not ported: setting up OpenGL, models, shaders and fog (and their MML).
namespace AlephOne
{
    public static class OGL_Setup
    {
        public static void reset_mml_opengl()
        {
            OGL_Subst_Texture_Def.reset_mml_opengl_texture();
            // Not ported: reset_mml_opengl_model(), reset_mml_opengl_shader(), and the fog
        }

        public static void parse_mml_opengl(InfoTree root)
        {
            // texture options / clear, in order
            foreach (var v in root.children())
            {
                if (v.name == "texture")
                    OGL_Subst_Texture_Def.parse_mml_opengl_texture(v);
                else if (v.name == "txtr_clear")
                    OGL_Subst_Texture_Def.parse_mml_opengl_txtr_clear(v);
            }

            // Not ported: models, shaders and fog
        }
    }
}
