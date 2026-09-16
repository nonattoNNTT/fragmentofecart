using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interação")]
    [SerializeField] private float distanciaInteracao = 5f;
    [SerializeField] private LayerMask camadasInteracao = ~0;

    [Header("Câmera")]
    [SerializeField] private Camera cameraAtual;

    [Header("Destaque")]
    [SerializeField] private float velocidadePiscar = 6f;

    private PlayerInputActions controls;

    private IInterativo[] interativosAtuais;

    private float tempoPiscar;
    private bool destaqueAtivo;

    private void Awake()
    {
        controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.Interact.performed += AoPressionarInteragir;
    }

    private void OnDisable()
    {
        controls.Player.Interact.performed -= AoPressionarInteragir;

        controls.Disable();

        RemoverDestaque();
    }

    private void Update()
    {
        EncontrarInterativo();
        AtualizarPiscar();
    }

    private void EncontrarInterativo()
    {
        Camera cam = ObterCameraAtiva();

        if (cam == null)
        {
            RemoverDestaque();
            return;
        }

        Ray raio = new Ray(
            cam.transform.position,
            cam.transform.forward
        );

        Debug.DrawRay(
            raio.origin,
            raio.direction * distanciaInteracao,
            Color.green
        );

        if (Physics.Raycast(
            raio,
            out RaycastHit hit,
            distanciaInteracao,
            camadasInteracao,
            QueryTriggerInteraction.Ignore))
        {
            MonoBehaviour[] componentes =
                hit.collider.GetComponentsInParent<MonoBehaviour>();

            List<IInterativo> encontrados =
                new List<IInterativo>();

            foreach (MonoBehaviour componente in componentes)
            {
                if (componente is IInterativo interativo)
                {
                    if (!encontrados.Contains(interativo))
                    {
                        encontrados.Add(interativo);
                    }
                }
            }

            if (encontrados.Count > 0)
            {
                IInterativo[] novosInterativos =
                    encontrados.ToArray();

                if (!MesmosInterativos(
                    novosInterativos,
                    interativosAtuais))
                {
                    RemoverDestaque();

                    interativosAtuais =
                        novosInterativos;

                    AtivarDestaque();
                }

                return;
            }
        }

        RemoverDestaque();
    }

    private void AoPressionarInteragir(
        InputAction.CallbackContext contexto)
    {
        if (interativosAtuais == null)
            return;

        foreach (IInterativo interativo in interativosAtuais)
        {
            if (interativo != null)
            {
                interativo.Interagir();
            }
        }
    }

    private Camera ObterCameraAtiva()
    {
        if (cameraAtual != null &&
            cameraAtual.isActiveAndEnabled)
        {
            return cameraAtual;
        }

        Camera[] cameras =
            FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Camera cam in cameras)
        {
            if (cam.isActiveAndEnabled)
            {
                return cam;
            }
        }

        return null;
    }

    private void AtivarDestaque()
    {
        destaqueAtivo = true;
        tempoPiscar = 0f;

        if (interativosAtuais == null)
            return;

        foreach (IInterativo interativo in interativosAtuais)
        {
            if (interativo != null)
            {
                interativo.Destacar(true);
            }
        }
    }

    private void AtualizarPiscar()
    {
        if (!destaqueAtivo ||
            interativosAtuais == null)
            return;

        tempoPiscar += Time.deltaTime;

        bool estado =
            Mathf.Sin(
                tempoPiscar * velocidadePiscar
            ) > 0f;

        foreach (IInterativo interativo in interativosAtuais)
        {
            if (interativo != null)
            {
                interativo.Destacar(estado);
            }
        }
    }

    private void RemoverDestaque()
    {
        if (interativosAtuais != null)
        {
            foreach (IInterativo interativo in interativosAtuais)
            {
                if (interativo != null)
                {
                    interativo.Destacar(false);
                }
            }
        }

        interativosAtuais = null;
        destaqueAtivo = false;
        tempoPiscar = 0f;
    }

    private bool MesmosInterativos(
        IInterativo[] a,
        IInterativo[] b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        foreach (IInterativo interativoA in a)
        {
            bool encontrou = false;

            foreach (IInterativo interativoB in b)
            {
                if (interativoA == interativoB)
                {
                    encontrou = true;
                    break;
                }
            }

            if (!encontrou)
                return false;
        }

        return true;
    }
}