using UnityEngine;

public class MouseParallax2D : MonoBehaviour
{
    [Header("Сила смещения")]
    [SerializeField] private float parallaxX = 0.3f;
    [SerializeField] private float parallaxY = 0.2f;

    [Header("Скорость сглаживания")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Инверсия")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Awake()
    {
        startPosition = transform.position;
        targetPosition = startPosition;
    }

    private void Update()
    {
        Vector3 mousePos = Input.mousePosition;

        // нормализация мыши: центр экрана = 0, края = -1 / 1
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;

        if (invertX) normalizedX *= -1f;
        if (invertY) normalizedY *= -1f;

        float offsetX = normalizedX * parallaxX;
        float offsetY = normalizedY * parallaxY;

        targetPosition = startPosition + new Vector3(offsetX, offsetY, 0f);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
    }
}
