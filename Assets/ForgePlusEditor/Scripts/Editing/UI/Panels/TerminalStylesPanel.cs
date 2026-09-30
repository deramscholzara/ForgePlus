using UnityEngine.UIElements;
using static AlephOne.computer_interface;

namespace ForgePlus.UI
{
    // The pool a terminal's text styles are drawn from: its faces, which a style combines, and its colors, which a
    // style refers to by index. Each is shown as terminal text in it.
    public class TerminalStylesPanel : UIPanel
    {
        private const float SampleScale = 1.5f;
        private const float SampleFontSize = 18f;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/TerminalStyles";
            }
        }

        protected override void OnLoaded()
        {
            var faces = Root.Q("faces");
            faces.Add(CreateSample("-", "Plain", _plain_text, 0));
            faces.Add(CreateSample("B", "Bold", _bold_text, 0));
            faces.Add(CreateSample("I", "Italic", _italic_text, 0));
            faces.Add(CreateSample("U", "Underline", _underline_text, 0));

            var colors = Root.Q("colors");
            for (short color = 0; color < TerminalText.ColorCount; color++)
            {
                colors.Add(CreateSample(color.ToString(), $"Color {color}", _plain_text, color));
            }
        }

        private static VisualElement CreateSample(string key, string name, short face, short color)
        {
            var sample = new VisualElement();
            sample.AddToClassList("fp-terminal-style");

            var keyLabel = new Label(key);
            keyLabel.AddToClassList("fp-terminal-style__key");
            sample.Add(keyLabel);

            var nameLabel = new Label(name) { enableRichText = false };
            nameLabel.AddToClassList("fp-terminal-style__sample");
            nameLabel.style.fontSize = SampleFontSize;
            TerminalText.ApplyStyle(nameLabel, face, color, SampleScale);
            sample.Add(nameLabel);

            return sample;
        }
    }
}
