using UnityEngine;

public class Interactable : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    // Подсвечиваем / убираем подсветку
    public void SetHighlight(bool highlight)
    {
        if (rend == null) return;

        rend.GetPropertyBlock(mpb);

        if (highlight)
        {
            mpb.SetColor("_EmissionColor", new Color(1.2f, 0.55f, 0f, 1f)); // яркий оранжевый
            rend.material.EnableKeyword("_EMISSION");
        }
        else
        {
            mpb.SetColor("_EmissionColor", Color.black);
        }

        rend.SetPropertyBlock(mpb);
    }
}
