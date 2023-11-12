using InControl;
using OC2Jetpack.Extension;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace OC2Jetpack
{
    public static class JetpackKeyboardRebind
    {
        public static Key[] jetpackKey = { Key.None, Key.None, Key.None };
        // combined, split1, split2

        public static readonly PlayerInputLookup.LogicalButtonID jetpackButtonID = (PlayerInputLookup.LogicalButtonID)666;

        public static void RefreshBindingText(KeyboardRebindElement keyboardRebindElement, int id)
        {
            string keyBindingsText = (jetpackKey[id] == Key.None) ? "Disabled" : keyboardRebindElement.KeyToString(jetpackKey[id]);
            keyboardRebindElement.SetKeyBindingsText(keyBindingsText);
        }

        private static void OnStartRebind(KeyboardRebindController keyboardRebindController, KeyboardRebindElement keyboardRebindElement, int id)
        {
            if (keyboardRebindController.ShowRebindDialog(keyboardRebindElement))
            {
                PCPadInputProvider.StartListeningForBinding(delegate (Key key)
                {
                    keyboardRebindController.HideRebindDialog();
                    jetpackKey[id] = (key == Key.Escape) ? Key.None : key;
                    RefreshBindingText(keyboardRebindElement, id);
                });
            }
        }

        private static void AddRebindUI(Transform parent, int id)
        {
            if (parent.childCount == 9)
            {
                GameObject rebindObj = GameObject.Instantiate(parent.GetChild(6).gameObject);
                rebindObj.name = "KeyboardRebind_10";
                rebindObj.transform.SetParent(parent, false);
                T17Text text = rebindObj.transform.GetChild(0).GetComponent<T17Text>();
                text.text = "Jet";
                text.m_LocalizationTag = "\"Jet\"";

                KeyboardRebindController keyboardRebindController = rebindObj.GetComponentInParent<KeyboardRebindController>();
                KeyboardRebindButtonElement keyboardRebindButtonElement = rebindObj.GetComponent<KeyboardRebindButtonElement>();
                keyboardRebindButtonElement.Awake();
                keyboardRebindButtonElement.set_m_ButtonID(jetpackButtonID);
                PadSide side = id == 0 ? PadSide.Both : (id == 1 ? PadSide.Left : PadSide.Right);
                keyboardRebindButtonElement.set_m_Side(side);

                Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
                buttonClickedEvent.AddListener(delegate ()
                {
                    OnStartRebind(keyboardRebindController, keyboardRebindButtonElement, id);
                });
                rebindObj.GetComponent<T17Button>().onClick = buttonClickedEvent;
            }
            RefreshBindingText(parent.GetChild(9).GetComponent<KeyboardRebindElement>(), id);
        }

        public static void AddAllRebindUI()
        {
            GameObject PCContent = GameObject.Find("PCContent");
            if (PCContent == null) return;
            Transform parent = PCContent.transform.GetChild(1).GetChild(0).GetChild(1);
            AddRebindUI(parent, 0);
            parent = PCContent.transform.GetChild(0).GetChild(0).GetChild(1);
            AddRebindUI(parent, 1);
            parent = PCContent.transform.GetChild(0).GetChild(1).GetChild(1);
            AddRebindUI(parent, 2);
        }
    }
}
