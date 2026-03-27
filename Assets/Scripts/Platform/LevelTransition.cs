using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Utils;

public class LevelTransition : MonoBehaviour
{
    [SerializeField] private Color requiredColor = Color.white; // требуемый цвет для перехода на некст уровень
    [SerializeField] int sceneNumber;
    [SerializeField] float timeToFadeNextLevelTransition;  // время для Fade при переходе на новый уровень
    [SerializeField] private BackgroundRevealController backgroundReveal;
    [SerializeField] private bool playRevealBeforeLoad = true;
    [SerializeField] private float revealDurationBeforeLoad = 1.8f;

    private bool isTransitioning;

    private void Awake()
    {
        this.ValidateSerializedFields();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning)
            return;

        if (!collision.TryGetComponent(out BrushColorController brushColor))
            return;

        // сравнение двух цветов, если они очень похожи или идентичны - проходит!
        Vector3 colorDiff = new Vector3(
            brushColor.CurrentColor.r - requiredColor.r,
            brushColor.CurrentColor.g - requiredColor.g,
            brushColor.CurrentColor.b - requiredColor.b);
        float differenceSqr = colorDiff.sqrMagnitude;

        if (differenceSqr < 0.01f)
        {
            PlayerFadeController fade = collision.gameObject.GetComponent<PlayerFadeController>();
            if (fade != null)
            {
                fade.FadeOut(() =>
                {
                    collision.GetComponent<PlayerMoveController>().LockMove();
                    Debug.Log("Отлично! Новый уровень)");
                    isTransitioning = true;

                    if (playRevealBeforeLoad && backgroundReveal != null)
                    {
                        StartCoroutine(RevealThenLoad());
                    }
                    else
                    {
                        LoadNextScene();
                    }
                });
            }
        }
    }
    private IEnumerator RevealThenLoad()
    {
        // тут поменял метод, первый параметр - к чему стреимится альфа, второй - время за которое всё проиходит
        backgroundReveal.PlayFinalReveal(0, revealDurationBeforeLoad); 

        yield return new WaitForSeconds(Mathf.Max(0f, revealDurationBeforeLoad));
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (FadeScreen.instance != null)
        {
            FadeScreen.instance.FadeIn(timeToFadeNextLevelTransition, () => { SceneManager.LoadScene(sceneNumber); });
            return;
        }

        SceneManager.LoadScene(sceneNumber);
    }
}
