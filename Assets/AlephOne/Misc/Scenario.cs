// Port of Aleph One: Source_Files/Misc/Scenario.h, Scenario.cpp (the scenario's name, ID and version, from MML)
//
// Not ported: classic gameplay and network compatibility (can_join).
using Unity.Scripting.LifecycleManagement;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public class Scenario
    {
        private static Scenario m_instance;

        private string m_name = string.Empty;
        private string m_id = string.Empty;
        private string m_version = string.Empty;

        public static Scenario instance()
        {
            if (m_instance == null)
            {
                m_instance = new Scenario();
            }

            return m_instance;
        }

        public string GetName() { return m_name; }
        public string GetID() { return m_id; }
        public string GetVersion() { return m_version; }

        // Each is kept to the length Aleph One's fixed-length fields hold
        public void SetName(string name) { m_name = name.Length > 31 ? name.Substring(0, 31) : name; }
        public void SetID(string id) { m_id = id.Length > 23 ? id.Substring(0, 23) : id; }
        public void SetVersion(string version) { m_version = version.Length > 7 ? version.Substring(0, 7) : version; }

        // ForgePlus: Aleph One reads the scenario once, at startup; ForgePlus reads it again when its scripts change
        public static void reset()
        {
            m_instance = new Scenario();
        }

        public static void reset_mml_scenario()
        {
            // no reset
        }

        public static void parse_mml_scenario(InfoTree root)
        {
            if (root.read_attr_text("name", out string str))
                instance().SetName(str);
            if (root.read_attr_text("id", out str))
                instance().SetID(str);
            if (root.read_attr_text("version", out str))
                instance().SetVersion(str);
        }
    }
}
