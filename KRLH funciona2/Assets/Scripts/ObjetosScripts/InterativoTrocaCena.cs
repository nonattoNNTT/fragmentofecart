using UnityEngine;
using UnityEngine.SceneManagement;

public class InterativoTrocaCena : MonoBehaviour, IInterativo
{
    [Header("Cena")]
    [SerializeField] private string nomeDaCena;

    [Header("Condições")]
    [SerializeField] private bool possuiCondicoes = false;

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

        if (TodasCondicoesCompletas())
        {
            Debug.Log(
                "Todas as condições foram cumpridas! Abrindo cena."
            );

            TrocarCena();
        }
        else
        {
            Debug.Log(
                "Ainda existem condições que não foram cumpridas."
            );
        }
    }

    private bool TodasCondicoesCompletas()
    {
        if (condicoes == null ||
            condicoes.Length == 0)
        {
            return true;
        }

        foreach (InterativoCondicao condicao in condicoes)
        {
            if (condicao == null)
                continue;

            if (!condicao.EstaCompleta)
            {
                Debug.Log(
                    "Condição ainda não cumprida: " +
                    condicao.gameObject.name
                );

                return false;
            }
        }

        return true;
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