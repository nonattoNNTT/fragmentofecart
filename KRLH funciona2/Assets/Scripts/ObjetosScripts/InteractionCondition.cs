using UnityEngine;

public class InteractionCondition : MonoBehaviour
{
    [Header("Condition")]
    [SerializeField] private bool conditionCompleted = false;

    public bool IsCompleted
    {
        get { return conditionCompleted; }
    }

    // Chamado quando o jogador interage com este objeto
    public void Interact()
    {
        conditionCompleted = true;

        Debug.Log(
            "Condição liberada pelo objeto: " +
            gameObject.name
        );
    }
}