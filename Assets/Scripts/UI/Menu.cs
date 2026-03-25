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

    [SerializeField] int startSceneIndex;
    [SerializeField] float fadeTimeForStart;
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
        FadeScreen.instance.FadeIn(fadeTimeForStart, () => { SceneManager.LoadScene(startSceneIndex); });
    }
    
    private void Exit()
    {
        Application.Quit();
    }
}
