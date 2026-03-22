using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    [SerializeField] private Color requiredColor = Color.white; // требуемый цвет для перехода на некст уровень
    [SerializeField] int sceneNumber;
    [SerializeField] float timeToFadeStartLevelTransition; // время для Fade вначале уровня
    [SerializeField] float timeToFadeNextLevelTransition;  // время для Fade при переходе на новый уровень
    

    private void Start()
    {
        if (FadeScreen.instance != null)
            FadeScreen.instance.FadeOut(timeToFadeNextLevelTransition);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        BrushColorController brushColor = collision.gameObject.GetComponent<BrushColorController>();
        Debug.Log(collision.gameObject.GetComponent<BrushColorController>());
        if (brushColor == null) return;

        // сравнение двух цветов, если они очень похожи или идентичны - проходит!
        float difference = Vector3.Distance(
            new Vector3(brushColor.CurrentColor.r, brushColor.CurrentColor.g, brushColor.CurrentColor.b),
            new Vector3(requiredColor.r, requiredColor.g, requiredColor.b));

        if (difference < 0.1f)
        {
            Debug.Log("Отлично! Новый уровень)");

            if(FadeScreen.instance != null)
                FadeScreen.instance.FadeIn(timeToFadeStartLevelTransition, () => { SceneManager.LoadScene(sceneNumber); });
        }
    }

}
