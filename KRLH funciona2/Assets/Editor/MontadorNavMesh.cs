using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// =========================================================
// MONTADOR DE NAVMESH - SampleScene (labirinto)
// =========================================================
//
// Menu: Valentina > NavMesh
//
//   1 - Montar e bakear NavMesh   -> cria o objeto "NavMesh" com NavMeshSurface,
//                                    configura, faz o bake e salva o asset em
//                                    Assets/Scenes/SampleScene/NavMesh-SampleScene.asset
//   2 - Criar cubo do Monstro     -> cria o cubo "Monstro" já com NavMeshAgent + MonsterAI,
//                                    posicionado sobre a NavMesh, longe e sem visão do Player
//   3 - Fazer tudo                -> 1 e 2 em sequência
//   Validar NavMesh               -> só o relatório no Console (não altera nada)
//
// Todos os itens são idempotentes: rodar de novo refaz, não duplica.
// Se a SampleScene não estiver aberta, o script pergunta se pode salvar a
// cena atual e abre a SampleScene sozinho.
//
// Por que NavMeshSurface e não Window > AI > Navigation (legado):
//   - o pacote com.unity.ai.navigation já está no projeto;
//   - o NavMeshData vira um asset versionável ao lado da cena;
//   - dá para excluir o Player e o Monstro do bake com NavMeshModifier,
//     sem depender de marcar 584 cubos como Static.
// =========================================================

public static class MontadorNavMesh
{
    // ---------------------------------------------------------
    // CONSTANTES
    // ---------------------------------------------------------

    const string SCENE_PATH   = "Assets/Scenes/SampleScene.unity";
    const string DATA_FOLDER  = "Assets/Scenes/SampleScene";
    const string DATA_PATH    = DATA_FOLDER + "/NavMesh-SampleScene.asset";
    const string MAT_FOLDER   = "Assets/Art/Monstro";
    const string MAT_PATH     = MAT_FOLDER + "/M_Monstro.mat";

    const string NAVMESH_GO   = "NavMesh";
    const string MONSTRO_GO   = "Monstro";
    const string LABIRINTO_GO = "Labirinto";
    const string PLAYER_GO    = "Player";
    const string PLAYER_LAYER = "player";   // já existe no TagManager do projeto

    // Ponto escolhido analisando o labirinto: ~52 m de caminho até o Player,
    // 28 m em linha reta (fora dos 18 m de visão), sem linha de visão direta.
    static readonly Vector3 MONSTRO_SPAWN = new Vector3(-42.4f, 0.4f, 20.6f);

    // Agente "Humanoid" (índice 0). É o tipo padrão do NavMeshAgent, então
    // qualquer objeto que ganhe MonsterAI depois já bate com o bake.
    const float VOXEL_SIZE      = 0.125f; // raio 0,5 / 4 -> mais fiel às paredes finas de 0,23 m
    const int   TILE_SIZE       = 256;
    const float MIN_REGION_AREA = 2f;     // apaga ilhas menores que 2 m² (cantos entre paredes)

    // ---------------------------------------------------------
    // MENUS
    // ---------------------------------------------------------

    [MenuItem("Valentina/NavMesh/1 - Montar e bakear NavMesh (SampleScene)", false, 0)]
    public static void MontarEBakear()
    {
        if (!AbrirSampleScene()) return;
        if (!Bakear()) return;
        Validar();
        SalvarTudo();
        Debug.Log("<b>[Valentina]</b> NavMesh pronta e cena salva. Próximo passo: " +
                  "Valentina > NavMesh > 2 - Criar cubo do Monstro.");
    }

    [MenuItem("Valentina/NavMesh/2 - Criar cubo do Monstro (SampleScene)", false, 1)]
    public static void CriarMonstro()
    {
        if (!AbrirSampleScene()) return;
        if (!CriarCuboMonstro()) return;
        SalvarTudo();
        Debug.Log("<b>[Valentina]</b> Monstro criado e cena salva. Aperte Play para ver ele rondar.");
    }

    [MenuItem("Valentina/NavMesh/3 - Fazer tudo (NavMesh + Monstro)", false, 2)]
    public static void FazerTudo()
    {
        if (!AbrirSampleScene()) return;
        if (!Bakear()) return;
        if (!CriarCuboMonstro()) return;
        Validar();
        SalvarTudo();
        Debug.Log("<b>[Valentina]</b> Tudo pronto e salvo. Aperte Play.");
    }

    [MenuItem("Valentina/NavMesh/Validar NavMesh (relatório no Console)", false, 20)]
    public static void ValidarMenu()
    {
        if (!AbrirSampleScene()) return;
        Validar();
    }

    // ---------------------------------------------------------
    // ABRIR A CENA CERTA
    // ---------------------------------------------------------

    static bool AbrirSampleScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Valentina", "Saia do Play Mode antes de rodar este menu.", "OK");
            return false;
        }

        Scene ativa = SceneManager.GetActiveScene();
        if (ativa.path == SCENE_PATH) return true;

        if (!System.IO.File.Exists(SCENE_PATH))
        {
            EditorUtility.DisplayDialog("Valentina",
                "Não achei a cena em:\n" + SCENE_PATH + "\n\nEla deveria estar em Assets/Scenes/.", "OK");
            return false;
        }

        // Pergunta se pode salvar o que estiver aberto; cancelar aborta tudo
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        return SceneManager.GetActiveScene().path == SCENE_PATH;
    }

    // ---------------------------------------------------------
    // BAKE
    // ---------------------------------------------------------

    static bool Bakear()
    {
        GameObject labirinto = GameObject.Find(LABIRINTO_GO);
        if (labirinto == null)
        {
            EditorUtility.DisplayDialog("Valentina",
                "Não achei o objeto \"" + LABIRINTO_GO + "\" na SampleScene.\n" +
                "É ele que segura as paredes e o Chão. Sem ele não há o que bakear.", "OK");
            return false;
        }

        // Player fora do bake e na camada "player" (assim a visão do monstro
        // não é bloqueada pela cápsula dele, só pelas paredes em Default)
        GameObject player = AcharPlayer();
        if (player != null)
        {
            IgnorarNoBake(player);
            AplicarCamadaPlayer(player);
        }
        else
        {
            Debug.LogWarning("[Valentina] Player não encontrado na SampleScene. " +
                             "O bake segue, mas o Monstro não vai ter alvo.");
        }

        // Monstro (se já existe) também fora do bake, senão ele abre um buraco na malha
        GameObject monstro = GameObject.Find(MONSTRO_GO);
        if (monstro != null) IgnorarNoBake(monstro);

        // Objeto NavMesh (idempotente)
        GameObject navGo = GameObject.Find(NAVMESH_GO);
        if (navGo == null)
        {
            navGo = new GameObject(NAVMESH_GO);
            Undo.RegisterCreatedObjectUndo(navGo, "Criar NavMesh");
        }
        navGo.transform.SetParent(null);
        navGo.transform.position = Vector3.zero;
        navGo.transform.rotation = Quaternion.identity;
        navGo.transform.localScale = Vector3.one;

        NavMeshSurface surface = navGo.GetComponent<NavMeshSurface>();
        if (surface == null) surface = navGo.AddComponent<NavMeshSurface>();

        ConfigurarSurface(surface);

        // Asset antigo fora, para não acumular lixo na pasta da cena
        if (surface.navMeshData != null)
        {
            string antigo = AssetDatabase.GetAssetPath(surface.navMeshData);
            if (!string.IsNullOrEmpty(antigo) && antigo != DATA_PATH) AssetDatabase.DeleteAsset(antigo);
        }
        surface.RemoveData();
        surface.navMeshData = null;
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(DATA_PATH) != null) AssetDatabase.DeleteAsset(DATA_PATH);

        // Bake síncrono: quando esta linha volta, a malha já existe em memória
        double t0 = EditorApplication.timeSinceStartup;
        surface.BuildNavMesh();
        double dt = EditorApplication.timeSinceStartup - t0;

        if (surface.navMeshData == null)
        {
            EditorUtility.DisplayDialog("Valentina",
                "O bake não gerou nada. Confira se o Chão tem BoxCollider e está ativo.", "OK");
            return false;
        }

        // Salva o NavMeshData como asset ao lado da cena (mesma convenção da Unity)
        GarantirPasta(DATA_FOLDER);
        surface.navMeshData.name = "NavMesh-SampleScene";
        AssetDatabase.CreateAsset(surface.navMeshData, DATA_PATH);
        AssetDatabase.SaveAssets();

        // Garante que a malha registrada no NavMesh é a do asset recém-criado
        surface.RemoveData();
        surface.AddData();

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("<b>[Valentina]</b> Bake concluído em " + dt.ToString("0.0") + " s. Asset: " + DATA_PATH);
        return true;
    }

    static void ConfigurarSurface(NavMeshSurface s)
    {
        // Humanoid: raio 0,5 · altura 2 · degrau 0,4 · rampa 45°
        s.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;

        // Geometria: colisores físicos de tudo que estiver em Default.
        // Os cubos do labirinto e o Chão têm BoxCollider; UI e player ficam de fora.
        s.collectObjects = CollectObjects.All;
        s.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        s.layerMask = ~0;
        int ui = LayerMask.NameToLayer("UI");
        int pl = LayerMask.NameToLayer(PLAYER_LAYER);
        if (ui >= 0) s.layerMask &= ~(1 << ui);
        if (pl >= 0) s.layerMask &= ~(1 << pl);

        s.defaultArea = 0; // Walkable
        s.ignoreNavMeshAgent = true;
        s.ignoreNavMeshObstacle = true;

        // Qualidade: voxel menor que o padrão (0,166) para as paredes finas
        // não sumirem nem engordarem; tile 256 mantém o bake rápido.
        s.overrideVoxelSize = true;
        s.voxelSize = VOXEL_SIZE;
        s.overrideTileSize = true;
        s.tileSize = TILE_SIZE;
        s.minRegionArea = MIN_REGION_AREA;
        s.buildHeightMesh = false; // chão plano: não precisa
    }

    static void IgnorarNoBake(GameObject go)
    {
        NavMeshModifier mod = go.GetComponent<NavMeshModifier>();
        if (mod == null) mod = go.AddComponent<NavMeshModifier>();
        mod.ignoreFromBuild = true;
        EditorUtility.SetDirty(mod);
    }

    static void AplicarCamadaPlayer(GameObject player)
    {
        int camada = LayerMask.NameToLayer(PLAYER_LAYER);
        if (camada < 0)
        {
            Debug.LogWarning("[Valentina] A camada \"" + PLAYER_LAYER + "\" não existe em Tags and Layers. " +
                             "O Player fica em Default e a visão do Monstro pode ser bloqueada por ele mesmo.");
            return;
        }
        foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer != camada)
            {
                t.gameObject.layer = camada;
                EditorUtility.SetDirty(t.gameObject);
            }
        }
    }

    static GameObject AcharPlayer()
    {
        GameObject go = null;
        try { go = GameObject.FindWithTag("Player"); } catch (UnityException) { }
        if (go == null) go = GameObject.Find(PLAYER_GO);
        return go;
    }

    // ---------------------------------------------------------
    // MONSTRO
    // ---------------------------------------------------------

    static bool CriarCuboMonstro()
    {
        if (!NavMesh.SamplePosition(MONSTRO_SPAWN, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            EditorUtility.DisplayDialog("Valentina",
                "Não existe NavMesh perto do ponto de nascimento do Monstro.\n" +
                "Rode antes: Valentina > NavMesh > 1 - Montar e bakear NavMesh.", "OK");
            return false;
        }

        // Idempotência: apaga o Monstro anterior
        GameObject antigo = GameObject.Find(MONSTRO_GO);
        while (antigo != null)
        {
            Object.DestroyImmediate(antigo);
            antigo = GameObject.Find(MONSTRO_GO);
        }

        // Raiz sem escala com o pé no chão: é nela que ficam NavMeshAgent e MonsterAI.
        // O cubo é só o "corpo" (filho). Quando o modelo do Ursão chegar, basta
        // apagar o Corpo e pôr o modelo no lugar — a IA e o agente não mudam.
        // (NavMeshAgent escala baseOffset/height pelo scale do transform, por isso
        // a raiz precisa ser 1,1,1.)
        GameObject monstro = new GameObject(MONSTRO_GO);
        Undo.RegisterCreatedObjectUndo(monstro, "Criar Monstro");
        monstro.transform.position = hit.position;
        monstro.transform.rotation = Quaternion.identity;
        monstro.transform.localScale = Vector3.one;

        GameObject corpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        corpo.name = "Corpo";
        corpo.transform.SetParent(monstro.transform, false);
        corpo.transform.localScale = new Vector3(1f, 2f, 1f); // 1 x 2 x 1 m
        corpo.transform.localPosition = new Vector3(0f, 1f, 0f); // pivô no centro -> sobe metade

        MeshRenderer mr = corpo.GetComponent<MeshRenderer>();
        Material mat = GarantirMaterialMonstro();
        if (mr != null && mat != null) mr.sharedMaterial = mat;

        NavMeshAgent agent = monstro.AddComponent<NavMeshAgent>();
        agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.baseOffset = 0f; // raiz já está no chão
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        MonsterAI ai = monstro.AddComponent<MonsterAI>();
        GameObject player = AcharPlayer();
        if (player != null) ai.player = player.transform;
        ai.obstacleMask = 1 << LayerMask.NameToLayer("Default"); // só parede bloqueia a visão

        IgnorarNoBake(monstro);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("<b>[Valentina]</b> Monstro criado em " + hit.position.ToString("F1") + ".");
        return true;
    }

    static Material GarantirMaterialMonstro()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        mat = new Material(shader);
        // Vermelho da paleta fechada do projeto (#D74143)
        Color cor = new Color(0.843f, 0.255f, 0.263f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", cor);

        GarantirPasta(MAT_FOLDER);
        AssetDatabase.CreateAsset(mat, MAT_PATH);
        return mat;
    }

    // ---------------------------------------------------------
    // VALIDAÇÃO - relatório no Console
    // ---------------------------------------------------------

    static void Validar()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>[Valentina] Relatório da NavMesh - SampleScene</b>");

        int nTri = tri.indices.Length / 3;
        if (nTri == 0)
        {
            sb.AppendLine("  Nenhum triângulo. A NavMesh não está bakeada ou não está carregada.");
            Debug.LogWarning(sb.ToString());
            return;
        }

        // Área total e componentes conexas (união por vértice, com tolerância
        // para os vértices duplicados na borda dos tiles)
        float areaTotal = 0f;
        int[] pai = new int[tri.vertices.Length];
        for (int i = 0; i < pai.Length; i++) pai[i] = i;
        Dictionary<Vector3Int, int> porPosicao = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < tri.vertices.Length; i++)
        {
            Vector3 v = tri.vertices[i];
            Vector3Int k = new Vector3Int(Mathf.RoundToInt(v.x * 100f), Mathf.RoundToInt(v.y * 100f), Mathf.RoundToInt(v.z * 100f));
            if (porPosicao.TryGetValue(k, out int j)) Unir(pai, i, j); else porPosicao[k] = i;
        }
        float[] areaTri = new float[nTri];
        for (int t = 0; t < nTri; t++)
        {
            int a = tri.indices[t * 3], b = tri.indices[t * 3 + 1], c = tri.indices[t * 3 + 2];
            Unir(pai, a, b); Unir(pai, b, c);
            areaTri[t] = Vector3.Cross(tri.vertices[b] - tri.vertices[a], tri.vertices[c] - tri.vertices[a]).magnitude * 0.5f;
            areaTotal += areaTri[t];
        }
        Dictionary<int, float> areaPorRegiao = new Dictionary<int, float>();
        for (int t = 0; t < nTri; t++)
        {
            int r = Raiz(pai, tri.indices[t * 3]);
            areaPorRegiao[r] = (areaPorRegiao.TryGetValue(r, out float acc) ? acc : 0f) + areaTri[t];
        }
        int maiorRegiao = -1; float maiorArea = 0f;
        foreach (KeyValuePair<int, float> kv in areaPorRegiao)
            if (kv.Value > maiorArea) { maiorArea = kv.Value; maiorRegiao = kv.Key; }

        sb.AppendLine("  Triângulos: " + nTri + "   Vértices: " + tri.vertices.Length);
        sb.AppendLine("  Área caminhável: " + areaTotal.ToString("0") + " m²");
        sb.AppendLine("  Regiões desconectadas: " + areaPorRegiao.Count +
                      "   (maior: " + maiorArea.ToString("0") + " m² = " + (100f * maiorArea / areaTotal).ToString("0.0") + "%)");

        // Player e Monstro sobre a malha? Na região principal?
        GameObject player = AcharPlayer();
        GameObject monstro = GameObject.Find(MONSTRO_GO);
        Vector3? pPlayer = Amostrar(player != null ? player.transform.position : (Vector3?)null, "Player", sb, tri, pai, maiorRegiao);
        Vector3? pMonstro = Amostrar(monstro != null ? monstro.transform.position : MONSTRO_SPAWN,
                                     monstro != null ? "Monstro" : "Ponto de nascimento do Monstro", sb, tri, pai, maiorRegiao);

        // Caminho Monstro -> Player
        if (pPlayer.HasValue && pMonstro.HasValue)
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(pMonstro.Value, pPlayer.Value, NavMesh.AllAreas, path);
            float comprimento = 0f;
            for (int i = 1; i < path.corners.Length; i++) comprimento += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            sb.AppendLine("  Caminho Monstro -> Player: " + path.status + " · " + comprimento.ToString("0.0") + " m · " +
                          path.corners.Length + " cantos · linha reta " +
                          Vector3.Distance(pMonstro.Value, pPlayer.Value).ToString("0.0") + " m");
            if (path.status != NavMeshPathStatus.PathComplete)
                sb.AppendLine("  <color=orange>ATENÇÃO: o Monstro não alcança o Player. Veja se há parede fechando a região.</color>");
        }

        if (areaPorRegiao.Count > 1)
            sb.AppendLine("  Obs.: regiões extras são bolsões fechados por paredes (o labirinto tem alguns). " +
                          "Só importa que Player e Monstro estejam na principal.");

        Debug.Log(sb.ToString());
    }

    static Vector3? Amostrar(Vector3? pos, string rotulo, StringBuilder sb, NavMeshTriangulation tri, int[] pai, int maiorRegiao)
    {
        if (!pos.HasValue) { sb.AppendLine("  " + rotulo + ": não está na cena."); return null; }
        if (!NavMesh.SamplePosition(pos.Value, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            sb.AppendLine("  <color=orange>" + rotulo + ": FORA da NavMesh (nada em 3 m de " + pos.Value.ToString("F1") + ")</color>");
            return null;
        }
        // Região = a do vértice mais próximo do ponto amostrado
        int melhor = -1; float melhorDist = float.MaxValue;
        for (int i = 0; i < tri.vertices.Length; i++)
        {
            float d = (tri.vertices[i] - hit.position).sqrMagnitude;
            if (d < melhorDist) { melhorDist = d; melhor = i; }
        }
        bool principal = melhor >= 0 && Raiz(pai, melhor) == maiorRegiao;
        sb.AppendLine("  " + rotulo + ": sobre a NavMesh em " + hit.position.ToString("F1") +
                      (principal ? " · região principal" : " · <color=orange>região SECUNDÁRIA</color>"));
        return hit.position;
    }

    static int Raiz(int[] pai, int i)
    {
        while (pai[i] != i) { pai[i] = pai[pai[i]]; i = pai[i]; }
        return i;
    }

    static void Unir(int[] pai, int a, int b)
    {
        int ra = Raiz(pai, a), rb = Raiz(pai, b);
        if (ra != rb) pai[ra] = rb;
    }

    // ---------------------------------------------------------
    // UTILIDADES
    // ---------------------------------------------------------

    static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        string pai = System.IO.Path.GetDirectoryName(caminho).Replace('\\', '/');
        string nome = System.IO.Path.GetFileName(caminho);
        if (!AssetDatabase.IsValidFolder(pai)) GarantirPasta(pai);
        AssetDatabase.CreateFolder(pai, nome);
    }

    static void SalvarTudo()
    {
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
