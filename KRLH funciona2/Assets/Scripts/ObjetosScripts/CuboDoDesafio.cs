using UnityEngine;

public class CuboDoDesafio : MonoBehaviour, IInterativo
{
    [Header("Desafio")]
    [Tooltip("O DesafioDosCubos que conta este cubo. Vazio = procura sozinho na cena.")]
    public DesafioDosCubos desafio;

    [Header("Cores")]
    public Color corNormal = new Color(0.16f, 0.23f, 0.31f);
    public Color corEmDestaque = new Color(0.84f, 0.25f, 0.26f);
    public Color corColetado = new Color(0.39f, 0.22f, 0.28f);

    private Renderer visual;
    private bool coletado;

    public bool Coletado
    {
        get { return coletado; }
    }

    private void Awake()
    {
        visual = GetComponent<Renderer>();

        if (desafio == null)
        {
            desafio = FindFirstObjectByType<DesafioDosCubos>();
        }

        AplicarCor(corNormal);
    }

    // =========================================================
    // INTERAÇÃO
    // =========================================================

    public void Interagir()
    {
        // Cada cubo só conta uma vez
        if (coletado)
        {
            Debug.Log(gameObject.name + " já tinha sido usado.");

            return;
        }

        coletado = true;

        AplicarCor(corColetado);

        Debug.Log("✅ " + gameObject.name + " interagido.");

        if (desafio != null)
        {
            desafio.AvisarCuboColetado(this);
        }
        else
        {
            Debug.LogWarning(
                "⚠️ " + gameObject.name +
                " não tem DesafioDosCubos — a contagem não anda."
            );
        }
    }

    public void Destacar(bool ativo)
    {
        if (coletado)
        {
            AplicarCor(corColetado);

            return;
        }

        AplicarCor(ativo ? corEmDestaque : corNormal);
    }

    // =========================================================
    // COR
    // =========================================================

    private void AplicarCor(Color cor)
    {
        if (visual == null)
            return;

        // A URP usa _BaseColor; o shader padrão usa _Color.
        if (visual.material.HasProperty("_BaseColor"))
        {
            visual.material.SetColor("_BaseColor", cor);
        }
        else
        {
            visual.material.color = cor;
        }
    }

    // Usado pelo DesafioDosCubos quando o desafio recomeça
    public void Reiniciar()
    {
        coletado = false;

        AplicarCor(corNormal);
    }
}
