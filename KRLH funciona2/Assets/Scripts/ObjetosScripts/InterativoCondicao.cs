using UnityEngine;

public class InterativoCondicao : MonoBehaviour, IInterativo
{
    [Header("Estado inicial")]
    [SerializeField] private bool completaInicialmente = false;

    private bool estaCompleta;

    public bool EstaCompleta
    {
        get { return estaCompleta; }
    }

    private void Awake()
    {
        estaCompleta = completaInicialmente;
    }

    public void Interagir()
    {
        estaCompleta = !estaCompleta;

        Debug.Log(
            gameObject.name +
            " → Condição: " +
            (estaCompleta ? "COMPLETA" : "INCOMPLETA")
        );
    }

    public void Destacar(bool ativo)
    {
        // Pode colocar um highlight aqui depois.
    }
}