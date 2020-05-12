using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Dialog_ObjectSelector : DialogBase
    {
        [SerializeField]
        private bool submitOnSelection = true;

        [SerializeField]
        private bool showCancelOption = true;

        [SerializeField]
        private Button selectionButtonPrefab = null;

        protected override void Populate(IList<string> options, IList<string> optionLabels)
        {
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];

                var label = optionLabels != null && optionLabels.Count > i ?
                            optionLabels[i] :
                            option;

                CreateButton(label, option);
            }

            if (showCancelOption)
            {
                CreateButton("Cancel", null);
            }
        }

        private void CreateButton(string label, string option)
        {
            var selectionButtonInstance = Instantiate(selectionButtonPrefab, transform);
            selectionButtonInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = label;

            selectionButtonInstance.onClick.AddListener(() =>
            {
                SetSelection(option);

                if (submitOnSelection)
                {
                    Submit();
                }
            });
        }
    }
}
