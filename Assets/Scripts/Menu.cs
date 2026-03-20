using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;

public class Menu : MonoBehaviour
{
    [SerializeField]
    private Button play;
    
    [SerializeField]
    private Button exit;
    
    private void Awake()
    {
        this.ValidateSerializedFields();
        
        AddSubscriptions();
    }

    private void OnDestroy()
    {
        RemoveSubscriptions();
    }

    private void AddSubscriptions()
    {
        play.onClick.AddListener(Play);
        exit.onClick.AddListener(Exit);
    }
    
    private void RemoveSubscriptions()
    {
        play.onClick.RemoveListener(Play);
        exit.onClick.RemoveListener(Exit);
    }

    private void Play()
    {
        SceneManager.LoadScene(1);
    }
    
    private void Exit()
    {
        Application.Quit();
    }
}