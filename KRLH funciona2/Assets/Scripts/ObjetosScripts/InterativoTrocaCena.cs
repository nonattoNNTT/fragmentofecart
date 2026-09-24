using UnityEngine;
using UnityEngine.SceneManagement;

public class InterativoTrocaCena : MonoBehaviour, IInterativo
{
    [Header("Cena")]
    [SerializeField] private string nomeDaCena;

    [Header("Condições")]
    [SerializeField] private bool possuiCondicoes = false;

    [Tooltip("Quantas condições bastam para liberar. 0 = precisa de todas.")]
    [SerializeField] private int quantasPrecisa = 0;

    [SerializeField] private InterativoCondicao[] condicoes;

    [Header("Highlight")]
    [SerializeField] private Renderer objetoVisual;

    [SerializeField] private Color corNormal = Color.white;

    [SerializeField] private Color corDestacada = Color.yellow;

    public void Interagir()
    {
        if (!possuiCondicoes)
        {
            TrocarCena();
            return;
        }

        if (CondicoesSuficientes())
        {
            Debug.Log(
                "Condições cumpridas! Abrindo cena."
            );

            TrocarCena();
        }
    }

    // 'quantasPrecisa' em 0 mantém o comportamento antigo: exige todas.
    // Com 5 e oito cubos na lista, cinco quaisquer já liberam.
    private bool CondicoesSuficientes()
    {
        if (condicoes == null ||
            condicoes.Length == 0)
        {
            return true;
        }

        int completas = 0;
        int validas = 0;

        foreach (InterativoCondicao condicao in condicoes)
        {
            if (condicao == null)
                continue;

            validas++;

            if (condicao.EstaCompleta)
            {
                completas++;
            }
        }

        int precisa = quantasPrecisa > 0
            ? Mathf.Min(quantasPrecisa, validas)
            : validas;

        if (completas >= precisa)
        {
            return true;
        }

        Debug.Log(
            "Faltam " + (precisa - completas) +
            " cubo(s). " + completas + " de " + precisa + "."
        );

        return false;
    }

    private void TrocarCena()
    {
        if (string.IsNullOrEmpty(nomeDaCena))
        {
            Debug.LogError(
                "O nome da cena não foi configurado em " +
                gameObject.name
            );

            return;
        }

        SceneManager.LoadScene(nomeDaCena);
    }

    public void Destacar(bool ativo)
    {
        if (objetoVisual == null)
            return;

        objetoVisual.material.color =
            ativo
                ? corDestacada
                : corNormal;
    }
}