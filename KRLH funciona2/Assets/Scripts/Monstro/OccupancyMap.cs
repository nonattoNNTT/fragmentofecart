using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// =========================================================
// OCCUPANCY MAP - o mapa de "onde ela pode estar"
// =========================================================
//
// Isto NAO e o mapa da fase. E a CRENCA do monstro sobre onde o jogador
// esta: uma grade por cima da NavMesh e, em cada celula, um numero que
// responde "qual a chance de ela estar aqui agora".
//
// Tres operacoes sustentam o comportamento inteiro:
//
//   SetKnown(pos)    -> vi ela: zera tudo e poe toda a certeza na celula dela
//   Diffuse()        -> o tempo passou: a certeza escorre para as celulas
//                       vizinhas, porque ela pode ter andado para la
//   ClearVisible()   -> estou olhando para aquele canto e nao tem ninguem:
//                       aquele canto vira zero
//
// O que EMERGE disso e o que parece inteligencia. Ele varre o corredor em
// vez de sortear pontos soltos; nao volta para onde acabou de olhar; e a
// busca escorre pelas portas, porque a difusao so passa por celula que
// existe na NavMesh - certeza nao atravessa parede.
//
// A tecnica e a de Damian Isla (Halo 3 e o jogo "Third Eye Crime"). O
// ponto dela nao e acertar onde o jogador esta: e ERRAR de um jeito
// convincente, que e o que separa um monstro que assusta de um monstro
// que sabe demais.
//
// Custo: a grade do labirinto (240 x 220 m em celulas de 4 m) tem umas
// 3.300 celulas, e so as celulas com certeza acima de zero pagam Linecast.
// Na pratica sao menos de dez por atualizacao, quatro vezes por segundo.
// =========================================================

public class OccupancyMap
{
    // ---------------------------------------------------------
    // ESTADO
    // ---------------------------------------------------------

    public float cellSize = 4f;
    public int width;
    public int height;
    public Vector3 origin;          // canto (minX, minZ) da grade

    private float[] belief;         // a certeza de cada celula
    private float[] scratch;        // buffer da difusao, para nao alocar todo quadro
    private bool[] walkable;        // a celula existe na NavMesh?
    private float[] groundY;        // altura do chao ali, para mirar e desenhar certo

    public bool IsBuilt { get { return belief != null; } }

    // Quanta certeza sobrou no total. Cai quando ele olha e nao acha.
    // Perto de zero significa "procurei em tudo que eu achava, ela nao esta la".
    public float Confidence { get; private set; }

    // ---------------------------------------------------------
    // MONTAGEM - uma vez so, no Awake
    // ---------------------------------------------------------

    public void Build(Bounds bounds, float cell, float sampleRadius)
    {
        cellSize = Mathf.Max(1f, cell);
        origin = new Vector3(bounds.min.x, bounds.center.y, bounds.min.z);
        width = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / cellSize));

        int total = width * height;
        belief = new float[total];
        scratch = new float[total];
        walkable = new bool[total];
        groundY = new float[total];

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                Vector3 center = CellCenter(i, j);

                // Celula so entra no mapa se existe chao andavel ali. E isso
                // que faz a certeza escorrer pelas portas e parar nas paredes.
                if (NavMesh.SamplePosition(center, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                {
                    walkable[index] = true;
                    groundY[index] = hit.position.y;
                }
            }
        }

        Confidence = 0f;
    }

    public int WalkableCount()
    {
        if (!IsBuilt) return 0;

        int count = 0;
        for (int i = 0; i < walkable.Length; i++)
        {
            if (walkable[i]) count++;
        }

        return count;
    }

    // ---------------------------------------------------------
    // ENTRADA DE INFORMACAO
    // ---------------------------------------------------------

    // Vi ela. Esquece todo o resto: a certeza inteira vai para a celula dela.
    public void SetKnown(Vector3 position)
    {
        if (!IsBuilt) return;

        System.Array.Clear(belief, 0, belief.Length);

        int index = IndexOf(position);

        if (index < 0)
        {
            Confidence = 0f;
            return;
        }

        belief[index] = 1f;
        Confidence = 1f;
    }

    // Ouvi alguma coisa por ali. Som nao entrega o ponto, entrega a regiao:
    // espalha a certeza num circulo em vez de cravar numa celula.
    public void AddNoise(Vector3 position, float radius, float weight)
    {
        if (!IsBuilt) return;

        int centerI, centerJ;
        if (!CellOf(position, out centerI, out centerJ)) return;

        int span = Mathf.Max(1, Mathf.CeilToInt(radius / cellSize));
        float added = 0f;

        for (int j = centerJ - span; j <= centerJ + span; j++)
        {
            for (int i = centerI - span; i <= centerI + span; i++)
            {
                if (!Inside(i, j)) continue;

                int index = j * width + i;
                if (!walkable[index]) continue;

                float distance = Vector3.Distance(CellCenter(i, j), position);
                if (distance > radius) continue;

                float falloff = 1f - (distance / Mathf.Max(0.01f, radius));
                belief[index] += falloff * weight;
                added += falloff * weight;
            }
        }

        Confidence += added;
        Normalize();
    }

    // Ela nao evaporou: se ele varreu tudo que achava e nao achou, e porque
    // ela foi MAIS LONGE do que ele imaginava. Semeia um anel em volta do
    // ultimo rastro, no raio que ela teria alcancado no tempo decorrido.
    //
    // O miolo fica vazio de proposito - o miolo ele acabou de checar. E o
    // "donut search" do Alien: Isolation, e e o que transforma "desisti" em
    // "entao ela ja esta mais longe".
    public void AddRing(Vector3 center, float innerRadius, float outerRadius, float weight)
    {
        if (!IsBuilt) return;

        int centerI, centerJ;
        CellOf(center, out centerI, out centerJ);

        int span = Mathf.Max(1, Mathf.CeilToInt(outerRadius / cellSize));
        float added = 0f;

        for (int j = centerJ - span; j <= centerJ + span; j++)
        {
            for (int i = centerI - span; i <= centerI + span; i++)
            {
                if (!Inside(i, j)) continue;

                int index = j * width + i;
                if (!walkable[index]) continue;

                float distance = Vector3.Distance(CellCenter(i, j), center);
                if (distance < innerRadius || distance > outerRadius) continue;

                belief[index] += weight;
                added += weight;
            }
        }

        Confidence += added;
        Normalize();
    }

    // ---------------------------------------------------------
    // PASSAGEM DO TEMPO
    // ---------------------------------------------------------

    // Ela pode ter andado. Cada celula entrega uma fracao da sua certeza para
    // as vizinhas andaveis - nunca para dentro de parede. E por isso que a
    // busca dele "escorre" pelo corredor em vez de ficar rodando num ponto.
    //
    // "drift" e a direcao em que ela estava indo quando sumiu: a certeza
    // escorre mais para aquele lado. Sem isso a duvida vira um circulo em
    // volta do ponto, e circulo nao e o que uma pessoa que viu alguem correr
    // para a esquerda pensa.
    public void Diffuse(float rate, Vector3 drift, float driftBias)
    {
        if (!IsBuilt) return;

        rate = Mathf.Clamp01(rate);
        System.Array.Clear(scratch, 0, scratch.Length);

        drift.y = 0f;
        bool hasDrift = driftBias > 0f && drift.sqrMagnitude > 0.01f;
        if (hasDrift) drift.Normalize();

        // Peso de cada vizinha: 1 no caso simetrico, mais que 1 no rumo dela
        float wLeft = 1f, wRight = 1f, wDown = 1f, wUp = 1f;

        if (hasDrift)
        {
            wLeft = Mathf.Max(0.05f, 1f + driftBias * -drift.x);
            wRight = Mathf.Max(0.05f, 1f + driftBias * drift.x);
            wDown = Mathf.Max(0.05f, 1f + driftBias * -drift.z);
            wUp = Mathf.Max(0.05f, 1f + driftBias * drift.z);
        }

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                float value = belief[index];

                if (value <= 0.00001f) continue;

                int n0 = NeighbourIndex(i - 1, j);
                int n1 = NeighbourIndex(i + 1, j);
                int n2 = NeighbourIndex(i, j - 1);
                int n3 = NeighbourIndex(i, j + 1);

                float total = 0f;
                if (n0 >= 0) total += wLeft;
                if (n1 >= 0) total += wRight;
                if (n2 >= 0) total += wDown;
                if (n3 >= 0) total += wUp;

                if (total <= 0f)
                {
                    scratch[index] += value;
                    continue;
                }

                float given = value * rate;

                scratch[index] += value - given;

                if (n0 >= 0) scratch[n0] += given * wLeft / total;
                if (n1 >= 0) scratch[n1] += given * wRight / total;
                if (n2 >= 0) scratch[n2] += given * wDown / total;
                if (n3 >= 0) scratch[n3] += given * wUp / total;
            }
        }

        float[] swap = belief;
        belief = scratch;
        scratch = swap;
    }

    // Estou olhando para la e nao tem ninguem: aquilo vira zero.
    // So paga Linecast em celula que ainda tem certeza - o resto e ignorado,
    // e e isso que deixa o mapa barato o bastante para rodar no PC do lab.
    public int ClearVisible(Vector3 eyes, Vector3 forward, float distance, float halfAngle,
                            float heightOffset, LayerMask obstacleMask, float nearRadius)
    {
        if (!IsBuilt) return 0;

        int cleared = 0;
        float cosLimit = Mathf.Cos(Mathf.Min(179f, halfAngle) * Mathf.Deg2Rad);

        int centerI, centerJ;
        CellOf(eyes, out centerI, out centerJ);
        int span = Mathf.Max(1, Mathf.CeilToInt(distance / cellSize) + 1);

        for (int j = centerJ - span; j <= centerJ + span; j++)
        {
            for (int i = centerI - span; i <= centerI + span; i++)
            {
                if (!Inside(i, j)) continue;

                int index = j * width + i;
                if (belief[index] <= 0.0005f) continue;

                Vector3 center = CellCenter(i, j, groundY[index]);
                Vector3 toCell = center - eyes;
                toCell.y = 0f;

                float cellDistance = toCell.magnitude;
                if (cellDistance > distance) continue;

                // Fora do cone, mas colado nele, ainda conta como "olhei"
                if (cellDistance > nearRadius && cellDistance > 0.01f)
                {
                    float dot = Vector3.Dot(forward.normalized, toCell / cellDistance);
                    if (dot < cosLimit) continue;
                }

                if (Physics.Linecast(eyes, center + Vector3.up * heightOffset, obstacleMask)) continue;

                Confidence -= belief[index];
                belief[index] = 0f;
                cleared++;
            }
        }

        if (Confidence < 0f) Confidence = 0f;
        return cleared;
    }

    public void Clear()
    {
        if (!IsBuilt) return;

        System.Array.Clear(belief, 0, belief.Length);
        Confidence = 0f;
    }

    // ---------------------------------------------------------
    // LEITURA - para onde eu vou olhar agora
    // ---------------------------------------------------------

    // Devolve os melhores lugares para checar, do mais promissor ao menos.
    // A nota e a certeza da celula menos um desconto pela distancia: um
    // canto quase certo a 80 m perde para um canto provavel a 10 m, que e
    // como uma pessoa procurando de verdade decide.
    public int GetBestCandidates(Vector3 from, int count, float distancePenalty, List<Vector3> results)
    {
        results.Clear();
        if (!IsBuilt || count <= 0) return 0;

        List<float> scores = new List<float>();

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                float value = belief[index];

                if (value <= 0.0005f) continue;

                Vector3 center = CellCenter(i, j, groundY[index]);
                float score = value - Vector3.Distance(from, center) * distancePenalty;

                int slot = results.Count;
                while (slot > 0 && scores[slot - 1] < score) slot--;

                if (slot >= count) continue;

                results.Insert(slot, center);
                scores.Insert(slot, score);

                if (results.Count > count)
                {
                    results.RemoveAt(results.Count - 1);
                    scores.RemoveAt(scores.Count - 1);
                }
            }
        }

        return results.Count;
    }

    public bool TryGetPeak(out Vector3 position)
    {
        position = Vector3.zero;
        if (!IsBuilt) return false;

        float best = 0.0005f;
        bool found = false;

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;

                if (belief[index] <= best) continue;

                best = belief[index];
                position = CellCenter(i, j, groundY[index]);
                found = true;
            }
        }

        return found;
    }

    // ---------------------------------------------------------
    // DESENHO - para a equipe VER a cabeca do monstro na Scene
    // ---------------------------------------------------------

    public void DrawGizmos(float maxAlpha)
    {
        if (!IsBuilt) return;

        float peak = 0.0005f;
        for (int i = 0; i < belief.Length; i++)
        {
            if (belief[i] > peak) peak = belief[i];
        }

        Vector3 size = new Vector3(cellSize * 0.85f, 0.12f, cellSize * 0.85f);

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                float value = belief[index];

                if (value <= 0.0005f) continue;

                float t = Mathf.Clamp01(value / peak);
                Gizmos.color = new Color(1f, 1f - t * 0.7f, 0f, Mathf.Lerp(0.12f, maxAlpha, t));
                Gizmos.DrawCube(CellCenter(i, j, groundY[index]) + Vector3.up * 0.1f, size);
            }
        }
    }

    // ---------------------------------------------------------
    // CONTAS DA GRADE
    // ---------------------------------------------------------

    private void Normalize()
    {
        if (Confidence <= 1f) return;

        float factor = 1f / Confidence;

        for (int i = 0; i < belief.Length; i++)
        {
            if (belief[i] > 0f) belief[i] *= factor;
        }

        Confidence = 1f;
    }

    private Vector3 CellCenter(int i, int j)
    {
        return new Vector3(origin.x + (i + 0.5f) * cellSize, origin.y, origin.z + (j + 0.5f) * cellSize);
    }

    private Vector3 CellCenter(int i, int j, float y)
    {
        return new Vector3(origin.x + (i + 0.5f) * cellSize, y, origin.z + (j + 0.5f) * cellSize);
    }

    private bool Inside(int i, int j)
    {
        return i >= 0 && i < width && j >= 0 && j < height;
    }

    private bool CellOf(Vector3 position, out int i, out int j)
    {
        i = Mathf.FloorToInt((position.x - origin.x) / cellSize);
        j = Mathf.FloorToInt((position.z - origin.z) / cellSize);
        return Inside(i, j);
    }

    // Indice da celula, ja resolvendo o caso de cair em cima de parede:
    // procura a celula andavel mais perto, senao a informacao se perderia.
    private int IndexOf(Vector3 position)
    {
        int i, j;
        if (!CellOf(position, out i, out j)) return -1;

        int index = j * width + i;
        if (walkable[index]) return index;

        for (int ring = 1; ring <= 2; ring++)
        {
            for (int dj = -ring; dj <= ring; dj++)
            {
                for (int di = -ring; di <= ring; di++)
                {
                    if (!Inside(i + di, j + dj)) continue;

                    int candidate = (j + dj) * width + (i + di);
                    if (walkable[candidate]) return candidate;
                }
            }
        }

        return -1;
    }

    private int NeighbourIndex(int i, int j)
    {
        if (!Inside(i, j)) return -1;

        int index = j * width + i;
        return walkable[index] ? index : -1;
    }
}
