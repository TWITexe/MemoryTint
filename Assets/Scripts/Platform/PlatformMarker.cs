using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class PlatformMarker : MonoBehaviour
{
    [SerializeField] private Transform platformsRoot;
    [SerializeField] private Material platformMaterial;
    [SerializeField] private bool syncSurfaceColorWithSprite = true;

    [ContextMenu("Mark Platforms")]
    public void MarkPlatforms()
    {
        if (platformsRoot == null)
        {
            Debug.LogWarning("PlatformMarker: platformsRoot is not assigned.", this);
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int markedCount = 0;

        Transform[] allTransforms = platformsRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform target in allTransforms)
        {
            if (target == platformsRoot)
                continue;

            SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                continue;

            if (groundLayer >= 0) 
                target.gameObject.layer = groundLayer;

            if (platformMaterial != null) 
                spriteRenderer.sharedMaterial = platformMaterial;

            PaintSurface paintSurface = target.GetComponent<PaintSurface>();
            bool createdNow = false;

            if (paintSurface == null)
            {
                paintSurface = target.gameObject.AddComponent<PaintSurface>();
                createdNow = true;
            }

            if (paintSurface != null && (syncSurfaceColorWithSprite || createdNow))
            {
                paintSurface.SetSurfaceColor(spriteRenderer.color);
            }

            markedCount++;
        }

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log($"PlatformMarker: marked {markedCount} platform objects.", this);
    }
}
