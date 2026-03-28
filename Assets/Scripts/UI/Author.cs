using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class Author : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text nicknameText;

        [SerializeField]
        private TMP_Text roleText;

        [SerializeField]
        private AuthorStruct authorStruct;

        [SerializeField]
        private Button linkButton;

        private void Awake()
        {
            this.ValidateSerializedFields();
            linkButton = GetComponent<Button>();

            Set();
        }

        private void Set()
        {
            nicknameText.text = authorStruct.Nickname;
            roleText.text = authorStruct.Role;

            linkButton.onClick.RemoveAllListeners();

            if (!string.IsNullOrWhiteSpace(authorStruct.Url))
            {
                linkButton.interactable = true;
                linkButton.onClick.AddListener(() => Application.OpenURL(authorStruct.Url));
            }
            else
                linkButton.interactable = false;
        }

        private void OnDestroy()
        {
            linkButton.onClick.RemoveAllListeners();
        }
    }
}
