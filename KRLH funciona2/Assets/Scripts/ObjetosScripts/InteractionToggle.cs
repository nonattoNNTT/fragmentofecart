using UnityEngine;

public class InterativoToggle : MonoBehaviour, IInterativo
{
    [Header("Objetos que serão ativados/desativados")]
    [SerializeField] private GameObject[] objetos;

    [Header("Estado inicial")]
    [SerializeField] private bool iniciarLigado = true;

    [Header("Highlight")]
    [SerializeField] private Renderer objetoVisual;

    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corDestacada = Color.yellow;

    private bool ligado;

    private void Awake()
    {
        ligado = iniciarLigado;

        AplicarEstado();

        // Garante que começa com a aparência normal
        Destacar(false);
    }

    public void Interagir()
    {
        ligado = !ligado;

        AplicarEstado();

        Debug.Log(
            gameObject.name + " ? " +
            (ligado ? "LIGADO" : "DESLIGADO")
        );
    }

    private void AplicarEstado()
    {
        foreach (GameObject objeto in objetos)
        {
            if (objeto == null)
                continue;

            // Procura Light no próprio objeto
            Light luz = objeto.GetComponent<Light>();

            if (luz != null)
            {
                luz.enabled = ligado;
            }
            else
            {
                // Se não for uma Light, ativa/desativa normalmente
                objeto.SetActive(ligado);
            }
        }
    }

    public void Destacar(bool ativo)
    {
        if (objetoVisual == null)
            return;

        objetoVisual.material.color =
            ativo ? corDestacada : corNormal;
    }
}