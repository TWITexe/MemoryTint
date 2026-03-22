using UnityEngine;
using Utils;

public class MenuEntryPoint : MonoBehaviour
{
    [SerializeField]
    private Menu menu;

    [SerializeField]
    private SettingsMenu settings;

    private void Awake()
    {
        this.ValidateSerializedFields();
        
        menu.gameObject.SetActive(true);
        settings.gameObject.SetActive(false);
        settings.InitializeAndLoad();
    }
}