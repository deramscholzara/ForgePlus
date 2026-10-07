using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The pool a terminal's text styles are drawn from: its faces, which a style combines, and its colors, which a
    // style refers to by index. Each is shown as terminal text in it, and clicking it applies it to the group's text.
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
            var colors = Root.Q("colors");

            foreach (var style in TerminalText.Styles)
            {
                (style.Kind == TerminalSource.StyleKind.Color ? colors : faces).Add(CreateSample(style));
            }
        }

        private static VisualElement CreateSample(TerminalText.Style style)
        {
            var sample = new VisualElement();
            sample.AddToClassList("fp-terminal-style");
            sample.RegisterCallback<ClickEvent>(clickEvent => ForgePlusUI.Instance.Terminals.ChooseStyle(style.Kind, style.Value));

            var keyLabel = new Label(style.Key);
            keyLabel.AddToClassList("fp-terminal-style__key");
            sample.Add(keyLabel);

            var nameLabel = new Label(style.Name) { enableRichText = false };
            nameLabel.AddToClassList("fp-terminal-style__sample");
            nameLabel.style.fontSize = SampleFontSize;
            TerminalText.ApplyStyle(nameLabel, style.Face, style.ColorIndex, SampleScale);
            sample.Add(nameLabel);

            return sample;
        }
    }
}
