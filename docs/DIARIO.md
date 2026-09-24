# Diário do projeto

Ordem: o mais recente em cima. Cada entrada registra o que mudou e o que alguém precisa
fazer por causa disso.

---


## 24/09/2026 (quinta)

### Animações do ursinho e rastro de pegadas (Claude Opus 5, a pedido da Julia)

A Julia trouxe dois assets: o **modelo animado do ursinho** (`UrsinhoIdle.blend`,
`UrsinhoWalk.blend`, `UrsinhoRun.blend`, em `Art/Monstro/`) e as **pegadas**
(`Art/pegada esquerda.png` e `pegada direita.png`).

#### Ursinho animado

Os três `.blend` estavam com `loopTime = false` — ligados junto com `loopPose`, senão a
costura salta, que foi o problema do idle da Valentina no dia 22.

**Conferido antes de montar:** os três têm **330 curvas e 33 caminhos idênticos**
(`Armature/mixamorig:*`), com `Armature` em escala 0,01 nos três. Ou seja, **são
intercambiáveis** — não repetiram o problema de escala que os clipes novos da Valentina
tiveram.

- **`Art/Monstro/ursinhoAnimator.controller`** (novo) — `idle ⇄ walk ⇄ run` pelo parâmetro
  `Speed`, mesma regra da Valentina e do Ursão: `<0,1` parado · `0,1–0,75` andando ·
  `>0,75` correndo. Não inventei padrão novo.
- **`Scripts/Monstro/AnimarMonstro.cs`** (novo) — lê a velocidade real do `NavMeshAgent` e
  escreve no `Speed`. É genérico: serve para qualquer monstro com `MonsterAI`.
- O `Animator` foi para o modelo (`UrsinhoIdle` dentro da prefab), com
  `cullingMode = CullUpdateTransforms` — **com 15 monstros na cena, não faz sentido animar
  quem está fora da tela**.
- Tudo na `Monstro.prefab`, então valeu para os 15 de uma vez.

**Testado:** 15/15 com Animator e controller; andando a 2,60 m/s o parâmetro vai a 0,50 e a
animação toca `walk`.

#### Rastro vira pegadas

O `RastroDeLuz.cs` deixou de largar plaquinhas genéricas e agora larga **pegada esquerda e
direita, alternando**, cada uma jogada para o seu lado (`afastamentoLateral`, 0,45 m) e
**girada para onde a Valentina está olhando**. Sem o afastamento elas sairiam em fila
indiana, no meio; sem o giro, apontariam para um lado qualquer.

Materiais novos em `Art/MdPlayer/Materiais/`: `PegadaEsquerda.mat` e `PegadaDireita.mat`,
unlit transparente. As PNGs foram reimportadas com `alphaIsTransparency` e **`wrapMode =
Clamp`** — sem o Clamp a pegada repete na borda do quad.

O passo ficou em **1,6 m** (era 3 m), que é distância de passada. O resto continua igual:
piscina fixa de 200, sem `Collider`, some em **180 s**.

**Testado**, com a Valentina virada para +X:

| pegada | posição | desvio lateral |
|---|---|---|
| `PegadaEsquerda` | (-57,28 · 0,44 · 25,82) | **+0,45 m** |
| `PegadaDireita` | (-55,28 · 0,44 · 24,92) | **−0,45 m** |

Ambas com `rotY = 90`, acompanhando o olhar dela. Zero erros e zero warnings.

> **Detalhe:** o `MeshRenderer` do `Corpo` (o cubo placeholder) já estava desligado na
> prefab — quem desligou não fui eu, e está certo: quem aparece agora é o ursinho.

### ⚠️ Tem um merge do Git parado no meio

`docs/DIARIO.md` está em conflito (`UU`) e existe `.git/MERGE_HEAD` — ou seja, **um `git
merge` foi começado e não terminou**. Os marcadores estão nas linhas 8, 1111 e 1172.

- **Lado HEAD:** as entradas que venho escrevendo (22, 23 e 24/09).
- **Lado que veio:** commit `c92dead` do JP (`Yshinu`), de 22/09 — o **`MonsterAI` v3 com
  occupancy map**, que é o que trouxe o `OccupancyMap.cs` novo.

**Os dois lados são conteúdo bom e não se sobrepõem** — a resolução certa é ficar com os
dois, em ordem de data. Não resolvi sozinho porque é decisão de quem está tocando o Git.
Enquanto o merge não fechar, `git commit` normal não passa.

## 23/09/2026 (terça)

### Ursão: velocidade reduzida e a causa real do pivô/paredes (Claude Opus 5, a pedido da Julia)

Pedido: o Ursão está rápido demais, e continua atravessando parede.

#### Velocidade

`huntSpeed` **6,8 → 4,2**, `walkSpeed` 1,6, `searchSpeed` 2,4. A Valentina anda a 5 e corre a
8, então **agora ela escapa dele até andando** — mas ele nunca desiste, que era o pedido
anterior. O limiar de corrida do `UrsaoMortal` foi para 3,5 para que, caçando a 4,2, ele use
a animação **Run** e não a Walk.

#### O pivô: por que minhas correções anteriores não pegavam

**A animação sobrescreve a posição da `Armature` todo frame.** O primeiro caminho de curva
dos clipes é `Armature.001` — ou seja, o clipe anima o transform do próprio esqueleto.
Qualquer `localPosition` que eu ajustasse nos filhos era apagada no frame seguinte. Por isso
o pivô parecia certo em edição e continuava errado em Play.

**Conserto: um nó intermediário que a animação não controla.** O Ursão foi reconstruído:

```
Ursao   (NavMeshAgent, MonsterAI, UrsaoMortal)
└── Modelo   (Animator; localPosition é a correção do pivô)
    ├── Armature.001   (a animação mexe aqui)
    └── Cube.001
```

Com o `Animator` no `Modelo`, os caminhos dos clipes continuam resolvendo, e o
`Modelo.localPosition` fica livre. Correção aplicada: **(-2,22 · 0,03 · -0,29)**, e a
verificação convergiu para erro **0,000** em um passo.

#### Três ferramentas de medição que mentiram

Perdi várias tentativas confiando em medidas erradas. Vale registrar:

| medida | o que dizia | verdade |
|---|---|---|
| `Renderer.bounds` do modelo | pegada de 1,45 m | 5,46 m |
| `SkinnedMeshRenderer.BakeMesh` + `TransformPoint` | coordenadas que não reagiam ao pai | — |
| posição dos ossos, sem animação aplicada | raio 0,39 m | 3,76 m |

**O que funciona: amostrar os clipes com `SampleAnimation` no objeto do `Animator` e ler a
posição dos ossos em espaço de mundo.** Foi assim que os números abaixo saíram.

#### O tamanho: o urso é um quadrúpede, largo para a altura

Medido por instante, em cada clipe:

| clipe | raio | altura | raio/altura |
|---|---|---|---|
| Idle | 5,38 m | 9,86 m | **0,55** |
| Walk | 3,76 m | 10,32 m | 0,36 |
| Run | 3,69 m | 9,62 m | 0,38 |

Ele é **largo**, não esguio — e num labirinto de corredores de ~2,4 m isso limita a altura
dele. Dimensionei pelos clipes de **movimento** (Walk/Run, que são os que percorrem o mapa),
com `escala 0,2658`:

- raio andando/correndo = **1,00 m**, contra raio 1,2 do agente → **margem de 0,20 m**
- raio parado = 1,43 m, passa 0,23 m do agente — mas parado ele não está entrando em corredor
- **altura = 2,74 m** (Valentina tem 4,33 m)

**Testado em Play Mode:** caçando a 4,20 m/s, raio do corpo 0,84 m contra parede mais próxima
a 3,20 m — **margem de 2,36 m, não encosta**.

### O ponto que precisa da decisão de vocês

**"Urso bem grande" e "não atravessar parede" não cabem juntos neste labirinto.** A proporção
do bicho é raio ≈ 0,37 × altura, e o corredor tem 2,4 m:

| raio do agente | altura possível do urso | quanto do mapa ele alcança |
|---|---|---|
| **1,2 (atual)** | **2,7 m** | **93%** |
| 2,0 | 4,5 m | 65% |
| 3,0 | 6,8 m | 42% |
| 4,0 | 9,0 m | 28% |

Escolhi priorizar **não atravessar parede**, que foi o pedido. Para um urso maior, é mudar
três coisas juntas: a escala do `Modelo`, o `radius` do `NavMeshAgent` **e** o `agentRadius`
do agent type "Ursao" na janela Navigation, e rebakear.

> **O conserto de raiz é no Blender:** os clipes têm ~2,4 m de deslocamento embutido e o
> modelo não está na origem. Reexportando com o modelo centrado na origem e a escala
> aplicada, o `Modelo.localPosition` volta a zero e o urso pode ser bem maior sem estourar
> o corredor.

### Pivô do Ursão deslocado 8 m — era isso que fazia ele atravessar parede (Claude Opus 5, a pedido da Julia)

Os dois problemas relatados eram **o mesmo problema**.

#### O pivô

Os filhos do modelo (`Armature.001` e `Cube.001`) vinham do FBX com `localPosition` de
**(8.47, 0, 0)**. Resultado medido: o pivô ficava em `(-73,20 · -0,18 · 91,10)` e o corpo do
urso em `(-74,83 · 3,95 · 99,10)` — **8,17 m de distância entre onde o NavMesh acha que ele
está e onde ele aparece**.

Como o agente navega pelo pivô, ele desviava das paredes **corretamente** — só que o corpo,
8 m adiante, passava por dentro delas. Não era falha de navegação: era o modelo dessincronizado
do pivô.

**Conserto:** calculei o centro do modelo no espaço local da raiz e apliquei o deslocamento
inverso nos filhos, de forma que o centro caia sobre o pivô em XZ e **os pés em `y = 0`**.

| | antes | depois |
|---|---|---|
| desvio XZ pivô ↔ modelo | **8,17 m** | **0,000 m** |
| pés em relação ao pivô | −0,39 m | **0,000 m** |

#### O tamanho, de novo

Encontrei o Ursão com escala `(0,972 · 1,019 · 1,344)` — **9,47 × 9,03 × 11,16 m**. Alguém
aumentou ele no editor depois da última entrega. Medido o que esse tamanho custa:

| raio | largura | alcança do mapa |
|---|---|---|
| 5,58 (o tamanho em que estava) | 11,2 m | **11%** |
| 4,00 | 8,0 m | 28% |
| 3,00 | 6,0 m | 42% |
| 2,00 | 4,0 m | 65% |
| **1,20** | **2,4 m** | **93%** |

Com 11 m de largura ele alcançava **11% do labirinto** — e o agente continuava com raio 1,2,
que é exatamente a receita para atravessar parede: navegação de bicho pequeno, corpo de bicho
grande.

**Decisão:** afinei **só a pegada** e **mantive a altura que estava lá** (`escalaY = 1,019`).
Ele ficou **2,04 × 9,03 × 2,40 m** — um urso de **9 metros de altura**, mais que o dobro da
Valentina (4,33 m), com pegada de 2,4 m que casa exatamente com `2 × raio do agente`.

#### Testado em Play Mode

| o quê | resultado |
|---|---|
| pegada vs agente | 2,40 m vs 2 × 1,2 = 2,40 m — **casam** |
| corpo dentro de parede | **NÃO**, medido com `OverlapBox` do corpo inteiro durante a caçada a 6,8 m/s |
| afunda no chão? | **não** — osso mais baixo em `y = 0,500`, pivô em `y = 0,500`, diferença **0,000 m** |
| caçada | `Hunt` desde o início, `canSee = true`, fechou de 49,8 m para 17,6 m |
| desfecho | chegou e matou: cena virou `Tela_inicial` |

> **Armadilha, pela segunda vez nesta sessão:** `SkinnedMeshRenderer.bounds` **mente** com
> animação rodando. Em play ele dizia que os pés estavam em `y = −0,74` (afundado 1,2 m);
> medindo pelos **ossos**, estavam em `y = 0,500`, exatos. Já tinha caído nisso ao medir a
> pose sentada da Valentina. **Para medir modelo animado, use osso, nunca bounds.**

### Se quiserem o Ursão mais largo

A altura pode crescer à vontade — não afeta navegação. **A largura é que custa alcance**, e a
tabela acima dá o preço. Se aumentar a escala X/Z, tem que aumentar junto o `radius` do
`NavMeshAgent` **e** o `agentRadius` do agent type "Ursao" na janela Navigation, e rebakear —
senão ele volta a atravessar parede.

### Bake das duas NavMeshes, e um `obstacleMask` que não tinha gravado (Claude Opus 5, a pedido da Julia)

Pedido: dar o bake no mapa para atualizar as colisões.

#### Bake

As duas superfícies da `SampleScene` foram rebakeadas: **`NavMesh`** (agente Humanoid, raio
0,5 — jogador e cubinhos) e **`NavMesh Ursao`** (agente Ursao, raio 1,2).

A contagem caiu de **3.505 para 2.738 triângulos**, e isso é bom: era malha velha acumulada.
A cobertura medida numa grade de 783 pontos ficou **melhor** depois:

| malha | cobertura |
|---|---|
| Humanoid | **100%** |
| Ursao | **93%** (era 90%) |

Conferido também: 15/15 cubinhos sobre a malha Humanoid, Ursão sobre a dele a 0,00 m, e
caminho **`PathComplete` com 14 curvas** do Ursão até a Valentina.

**As outras cenas não têm NavMesh** — `Tela_inicial`, `Tela_1`, `Tela_2` e `Tela_1 1` estão
com zero superfícies e zero agentes. Não havia o que bakear nelas.

#### O bug que o bake revelou

Ao testar depois do bake, o Ursão estava em `Patrol` com `canSee = false` — a configuração de
caçar sempre **não tinha gravado**. O `obstacleMask` estava de volta em **0**, e o `Awake` do
`MonsterAI` trata `obstacleMask == 0` como "assume tudo", ou seja, **parede voltava a bloquear
a visão dele**.

Causa: eu tinha usado a **layer 31**, e `1 << 31` em C# é `int.MinValue` (o bit de sinal).
Refeito na **layer 30** (`1 << 30 = 1073741824`, positivo) e gravado por `SerializedObject`.
Confirmado com recarga da cena: **persistiu**.

> Também me enganei no meio do diagnóstico: li `player = NULO` e `isOnNavMesh = false` e achei
> que estava tudo quebrado. Estava **fora do Play Mode** — esses dois campos só são
> preenchidos em runtime. Vale lembrar antes de sair caçando bug que não existe.

#### Estado final, testado

| | |
|---|---|
| cubinhos | **15/15** acharam o jogador, **15/15** na malha |
| Ursão | `Hunt` no primeiro frame, `canSee = true` a **66,9 m** |
| perseguição | 67 m → 38 m → 28 m a 6,80 m/s |
| atravessa parede? | **NÃO**, em todos os instantes medidos |

Zero erros e zero warnings.

### Ursão caça sempre, e parou de atravessar parede (Claude Opus 5, a pedido da Julia)

#### Caçar sempre

Tudo por campo do Inspector do `MonsterAI`, sem tocar em código:

| campo | valor | por quê |
|---|---|---|
| `obstacleMask` | layer 31 (vazia) | **é a chave**: nada na cena está nessa layer, então o `Linecast` que checa "tem parede no meio?" nunca acerta nada. Ele enxerga através do labirinto inteiro |
| `viewDistance` / `viewAngle` | 1000 / 360° | vê de qualquer distância, em qualquer direção |
| `awarenessGain` / `awarenessDecay` | 100 / 0 | certeza imediata, nunca esquece |
| `menaceLimit` / `maxHuntTime` | 999999 | **nunca recua**. Esses dois são o que fazia ele desistir |
| `darkViewMultiplier` | 1 | lanterna apagada não esconde dela |

Descobri que `nearSenseRadius` sozinho não resolveria: no `SensePlayer()` o "sente mesmo por
trás" ainda passa pelo `Linecast` da parede depois. Por isso a `obstacleMask` é o caminho.

**Os cubinhos ficaram como estavam** — `viewDistance` 22, cone de 110°, `menaceLimit` 20.
Eles continuam só perseguindo quem eles acham.

#### Atravessar parede: a causa e o conserto

A NavMesh do projeto é bakeada para o agente **Humanoid, raio 0,5** (1 m de largura). O Ursão
usava raio 1,6 e tinha 5,7 m de largura. O caminho era calculado para um bicho cinco vezes
menor que ele — daí ele cortar quina e entrar na parede.

**Conserto: agent type próprio.** Criado o agent type **"Ursao"** (raio 1,2 · altura 4,6) e um
segundo `NavMeshSurface` na cena (`NavMesh Ursao`) bakeado só para ele. Agora o caminho dele
só passa onde ele cabe de verdade.

> Detalhe que me custou duas tentativas: **`NavMesh.GetSettingsByID()` devolve uma cópia**.
> Mudar `cfg.agentRadius` nela não grava nada, e o bake sai com o raio antigo — por isso as
> primeiras medições davam 100% de cobertura em qualquer raio. O jeito certo é editar
> `ProjectSettings/NavMeshAreas.asset` por `SerializedObject`.

#### O tamanho dele foi uma escolha com número

Medido: quanto do labirinto o Ursão alcança, por raio de agente.

| raio | largura dele | quanto do mapa alcança |
|---|---|---|
| 2,83 | 5,7 m | **40%** |
| 2,00 | 4,0 m | 57% |
| 1,50 | 3,0 m | 72% |
| **1,20** | **2,4 m** | **90%** |
| 0,70 | 1,4 m | 100% |

"Urso bem grande" e "ele chega até mim sempre" puxam para lados opostos: o corredor mais
estreito do labirinto tem 2,1 m. Como o pedido de chegar sempre apareceu duas vezes, priorizei
alcance — **mas sem encolher o bicho**, com **escala não uniforme**: `(0,212 · 0,519 · 0,212)`.

Ele ficou **1,54 × 4,60 × 2,40 m**: alto e esguio. **Mais alto que a Valentina (4,33 m)**, e com
pegada de 2,4 m, que é quase a mesma dela (2,17 m) — ou seja, **ele vai aonde ela vai**.

#### Testado em Play Mode

| o quê | resultado |
|---|---|
| estado inicial | **`Hunt` no primeiro frame**, `canSee = true` a **69 m de distância, através das paredes** |
| perseguição | 69 m → 31 m → 21 m, correndo a 6,80 m/s sem parar |
| atravessa parede? | **NÃO** — cápsula testada contra tudo em vários instantes, só encosta no `Chão` |
| chegou? | sim: cena virou **`Tela_inicial`** |

Zero erros e zero warnings.

### Se quiserem o Ursão mais gordo

É um número só: `agentRadius` do agent type "Ursao" (janela Navigation) junto com a escala X/Z
dele. A tabela acima diz o preço em alcance. Com raio 2,0 ele fica com 4 m de largura e
alcança 57% do mapa — vira um urso que assusta mais mas fica preso em meio labirinto.

### Cubinhos atrasam a Valentina, e o Ursão mata (Claude Opus 5, a pedido da Julia)

#### Duas linhas no `MonsterAI`, e nada mais

O `MonsterAI` já tinha tudo: `OnCatchPlayer()`, o estado `Retreat` que manda o monstro para
o ponto mais longe do mapa, e `retreatTime` no Inspector. Em vez de escrever IA nova,
acrescentei só o gancho:

- `public event System.Action<MonsterAI> AoPegarOJogador` — disparado no `OnCatchPlayer()`
- `public void ForcarRecuo()` — deixa mandar o recuo de fora

Todo o resto vive em componentes novos. O `MonsterAI` não sabe que cubinho ou Ursão existem.

#### Cubinhos que atrasam — `CuboQueAtrasa.cs`

Vai junto com o `MonsterAI` na `Monstro.prefab`, então valeu para os 15 de uma vez.

Encostou na Valentina → ela fica lenta e ele recua. O `retreatTime` do `MonsterAI` foi para
**30 s**, então é esse o tempo que ele fica longe antes de voltar a procurar.

No `PlayerMovement` entraram `fatorDeLentidao` (0,45 = 45% da velocidade) e
`duracaoDaLentidao` (**10 s**), mais o método público `AplicarLentidao()`. Encostar de novo
antes de acabar **renova** o tempo, não soma — senão dois cubos seguidos travariam ela por
20 s.

#### Ursão — `UrsaoMortal.cs`

O modelo já estava no projeto (`urso grande 1.fbx`), com as três animações prontas:
**Idle (14,2 s), Run (0,87 s), Walk (1,43 s)**.

**O `ursoanimate.controller` estava quebrado:** nenhum parâmetro, e as transições
Idle → Run → Walk → Idle **sem condição nenhuma**. Ele ciclava as três animações sozinho
para sempre, sem relação com o que o urso fazia. Refeito com parâmetro `Speed` e a mesma
regra do Animator da Valentina (`<0,1` idle · `0,1–0,75` walk · `>0,75` run), para não
inventar padrão novo. Os três clipes também estavam com `loopTime = false` — agora têm
`loopTime` e `loopPose`.

O `UrsaoMortal.cs` lê a velocidade do `NavMeshAgent` e escreve no `Speed`; e quando o
`MonsterAI` pega a Valentina, carrega a cena. Tem trava de `jaMatou` porque o `MonsterAI`
chama o evento de novo no quadro seguinte e o `LoadScene` dispararia duas vezes.

> **Assumi que "tela de play" é a `Tela_inicial`**, que é a cena com o botão de Play.
> Está no campo `cenaAoMorrer` do Inspector — se era outra, é uma palavra para trocar.

#### A escala do Ursão foi medida, não chutada

O modelo tem **10,93 m de largura**. Medi 284 corredores do labirinto:

| | largura |
|---|---|
| mínima | 2,1 m |
| 10% | 6,0 m |
| mediana | 11,1 m |
| máxima | 39,0 m |

No tamanho original ele trava em boa parte do mapa. Ficou em **escala 0,5 → 5,46 × 4,43 ×
3,28 m** — ainda maior que a Valentina (4,33 m), e passa em ~90% dos corredores.

#### Testado em Play Mode

| passo | resultado |
|---|---|
| cubinho encosta | `🐌 Valentina mais lenta por 10s` + `🟦 Monstro 1 encostou. Recuando por 30s.` |
| efeito | velocidade caiu de **5 para 2,25**; monstro entrou em `Retreat` e já estava a 10,5 m |
| 10 s depois | `🏃 Velocidade normal de volta` — `EstaLenta = false` |
| 30 s depois | monstro voltou de `Retreat` para `Patrol`, caçando de novo |
| Ursão andando | anima trocou para `Walk` sozinha; ele percorreu ~15 m por trecho de ronda |
| Ursão pega | cena virou **`Tela_inicial`**, com o `MainMenu` dentro |

Zero erros e zero warnings no console na rodada inteira.

### O que ainda precisa de olho

- **A NavMesh está bakeada para agente de raio 0,5** (1 m de diâmetro), e o Ursão usa raio
  1,6. Ele anda e persegue — testado — mas o caminho é calculado para um bicho menor do que
  ele, então em corredor apertado ele vai raspar a parede. **O conserto certo é um segundo
  `NavMeshSurface` com um agent type próprio para o Ursão**, e aí ele nunca tentaria passar
  onde não cabe. Não fiz porque muda a configuração de NavMesh do projeto e vale confirmar
  antes.
- **16 `MonsterAI` rodando juntos agora** (15 cubos + Ursão). Continua valendo o aviso de
  desempenho da entrada anterior: se cair fps no PC do laboratório, o primeiro corte é o
  número de cubos.

### 15 monstros no labirinto e rastro de luz na SampleScene (Claude Opus 5, a pedido da Julia)

#### 15 monstros espalhados

O labirinto é `Global Volume/Labirinto`: um chão de **243 × 223 m** com **583 paredes**.

Os 15 são instâncias da `Assets/Art/Monstro/Monstro.prefab` que já existia, agrupadas em
`Monstros`. O `Monstro` que já estava na cena virou o primeiro da lista, em vez de ser
apagado e recriado — nada dele se perdeu.

**Como escolhi onde pôr:** peguei o centro de cada triângulo da NavMesh como candidato
(1.851 pontos, já descontando tudo a menos de 25 m do spawn) e rodei escolha gulosa do ponto
mais distante dos já escolhidos. Resultado: **separação mínima de 53,4 m** entre monstros, de
39 m a 217 m do spawn. Não é aleatório com sorte — é espalhamento medido.

Verificado: **15/15 com `NavMeshAgent` na NavMesh**, todos instâncias da prefab. Em Play
Mode, 14 dos 15 estavam andando; o outro estava parado, que é comportamento normal do
`MonsterAI` (ele ronda e espera).

#### Rastro de luz, `RastroDeLuz.cs`

Novo, em `Assets/Scripts/Artscripts/`. Vai na raiz do Player, pela prefab, então vale em
qualquer cena.

Deixa uma marca acesa no chão a cada **3 m** andados, e **cada marca some sozinha depois de
180 s (3 minutos)**, apagando no último terço da vida em vez de piscar fora.

**As marcas não são `Light` de verdade, de propósito.** Um rastro com luz real seriam
dezenas de luzes dinâmicas ao mesmo tempo, e a seção 3 do `CLAUDE.md` pede 40+ fps no PC mais
fraco do laboratório e a seção 4 limita sombra em tempo real a 2 ou 3 luzes. São plaquinhas
(`Quad`) com material **unlit transparente**, que custam quase nada. Visualmente lê como
rastro de luz; no profiler não aparece.

Outros cuidados que estão no código e valem lembrar:

- **Piscina fixa de 200 objetos, criada toda no `Start`.** Zero `Instantiate` ou `Destroy`
  durante o jogo. Ao estourar o teto, a marca mais velha é reaproveitada.
- **As marcas não têm `Collider`.** Sem isso elas entrariam na frente do raycast do
  `InteractionSystem` e o jogador não conseguiria clicar em mais nada.
- **`MaterialPropertyBlock`** para o esmaecimento, em vez de `material.color` — senão cada
  marca viraria um material novo, 200 materiais.
- Sombra desligada (`ShadowCastingMode.Off`, `receiveShadows = false`).
- A marca é colada no chão por raycast, não na altura do jogador, e fica 3 cm acima para não
  brigar com o piso.
- Cor **#643847**, da paleta fechada.

**Testado em Play Mode:**

| o quê | resultado |
|---|---|
| piscina | 200 objetos criados no `Start`, todos desligados |
| andar 4 m | 1 marca, deitada no chão (rotação 90° no X), **sem collider**, sombra off |
| validade | com `duracao` baixada para 6 s só na medição, as marcas **sumiram sozinhas**; o valor salvo é 180 s |
| 15 monstros + rastro | zero erros e zero warnings no console |

### O que ainda precisa de olho

- **Não dá para afirmar o custo real em fps.** Medi 184 fps, mas com a janela da Unity sem
  foco e nesta máquina — não é o PC mais fraco do laboratório, que é o alvo que importa.
  **Os 15 `MonsterAI` rodando juntos são a parte cara**, não o rastro. Se cair fps na
  apresentação, o primeiro corte é o número de monstros.
- `distanciaEntreMarcas`, `duracao` e `maxDeMarcas` são campos no Inspector do Player — dá
  para afinar sem tocar em código.

### Desafio dos cubos refeito com os scripts que já existiam (Claude Opus 5, a pedido da Julia)

Pedido: refazer os cubos usando **os scripts da própria equipe**, no mesmo molde do `Cube` de
teste que já estava na `SampleScene` — 5 cubos com condição que somem ao clicar, e um sexto,
vermelho, que teleporta quando as 5 condições estiverem cumpridas.

**Meus dois scripts do dia anterior (`CuboDoDesafio.cs` e `DesafioDosCubos.cs`) foram
apagados.** Eles reimplementavam, pior, o que `InterativoCondicao` + `InterativoToggle` +
`InterativoTrocaCena` já faziam. Conferido por GUID que nenhuma cena ou prefab os usava antes
de apagar; há backup fora do projeto. Isso segue a seção 5 do `CLAUDE.md`: script que não é
mais necessário, apaga.

#### Como ficou montado

| objeto | componentes | configuração |
|---|---|---|
| `CuboCondicao 1..5` | `InterativoCondicao` + `InterativoToggle` | `objetos = [ele mesmo]`, `iniciarLigado = true` |
| `CuboTeleporte` | `InterativoTrocaCena` | `nomeDaCena = "Tela_1 1"`, `possuiCondicoes = true`, as 5 condições ligadas |

**O truque do "some ao clicar" é o `objetos` do `InterativoToggle` apontar para o próprio
cubo.** Ao clicar, ele faz `SetActive(false)` em si mesmo.

E os dois scripts disparam no mesmo clique **de graça**: o `InteractionSystem` faz
`GetComponentsInParent<MonoBehaviour>()` e chama `Interagir()` em **todo** `IInterativo` que
achar no objeto. Confirmado no teste: "2 IInterativo chamados" por cubo. Ou seja, um clique
marca a condição **e** some com o cubo, sem precisar de cola nenhuma.

#### Materiais, dentro da paleta fechada

Criados em `Assets/Art/Materiais/`, usando só cores da seção 4 do `CLAUDE.md`:

- `CuboCondicao.mat` — **#283A4E**, os cinco cubos
- `CuboTeleporte.mat` — **#D74143**, o vermelho pedido; é o vermelho da paleta, não um
  vermelho qualquer

O `corNormal` de cada script foi ajustado para a cor do próprio cubo, senão o primeiro
`Destacar(false)` do `InteractionSystem` pintaria tudo de branco.

#### Testado em Play Mode, o fluxo inteiro

| passo | resultado |
|---|---|
| clicar no vermelho **antes** | "Ainda existem condições que não foram cumpridas" — **não troca de cena** |
| clicar nos 5 cubos | cada um: "Condição: COMPLETA" + "DESLIGADO", e `activeSelf = false` — sumiram |
| clicar no vermelho **depois** | "Todas as condições foram cumpridas! Abrindo cena." |
| resultado | cena ativa passou a ser **`Tela_1 1`**, carregada, com o Player dentro |

Zero erros no console. Mirabilidade dos 6 medida com a varredura de sempre: **25 a 36 de 34
a 36 posições, 50% a 68% da faixa de mira**.

> **Atenção ao nome da cena:** é `Tela_1 1`, **com espaço**, não `Tela1_1`. É o nome real do
> arquivo e é o que o `SceneManager.LoadScene` recebe. Se alguém renomear a cena, esse campo
> quebra em silêncio.

### Câmera de 3ª mais curta e corpo some em 1ª pessoa (Claude Opus 5, a pedido da Julia)

#### Distância da 3ª pessoa

Terceiro ajuste no mesmo campo, agora fechando: `cameraDistance` **1,8 → 1,2**, que com o
Player em escala 2,166 dá **2,60 m**. O histórico: 3,5 (7,58 m) → 1,8 (3,90 m) → **1,2
(2,60 m)**. Para um personagem de 4,33 m, 2,60 m é bem por cima do ombro.

É um campo só no `CameraController` da `Player.prefab`, e o valor vale "para o Player em
escala 1" — o código multiplica pela escala real. Para calcular: **metros no jogo =
`cameraDistance` × 2,166**.

#### O modelo some em 1ª pessoa

Em primeira pessoa a câmera fica dentro do corpo, então dava para ver pescoço e cabelo por
dentro. Agora o modelo é escondido.

No `CameraController.cs`:

- Campos novos: `modeloDoPlayer` (vazio = acha sozinho pelo `Animator`) e
  `esconderModeloEmPrimeiraPessoa` (ligado por padrão, desligue para ver o corpo em 1ª).
- `Awake()` novo, que junta os `Renderer` do modelo. **Precisa ser no `Awake`**: o `Start` já
  chama `AplicarEstado()`, que decide se o corpo aparece.
- `MostrarCorpo(bool)` liga e desliga **só os `Renderer`, nunca o `GameObject`** — desligar o
  objeto pararia o `Animator` e a animação perderia o estado. Confirmado no teste: depois de
  ida e volta entre as câmeras, o Animator continua no `idle`.
- **No filminho o corpo aparece.** O `AplicarEstado()` mostra o corpo quando
  `controleAtivo == false`, porque quem está filmando é a `CameraCinematica` — se seguisse a
  regra da 1ª pessoa, a Valentina sumiria da própria cutscene.

**Testado em Play Mode, os três estados:**

| estado | corpo | câmera |
|---|---|---|
| filminho | **5/5 visível** | cinemática |
| 1ª pessoa | **0/5** | FirstPersonCamera |
| 3ª pessoa | **5/5 visível** | ThirdPersonCamera a 2,60 m, alinhada 0,0°, sem atravessar parede |
| 1ª → 3ª → 1ª | **0/5**, e o Animator continua em `idle` | — |

Zero erros e zero warnings no console na rodada inteira.

### Varredura: tudo o que impedia rodar limpo (Claude Opus 5, a pedido da Julia)

Pedido: *"resolva todos os problemas para rodar tranquilamente agora."*

Auditei as **5 cenas do build**, procurando script faltando, referência quebrada,
`AudioListener` duplicado e `Camera.main` nulo. Depois rodei `Tela_1` e `SampleScene` em
Play Mode com o console limpo.

#### Consertado

| problema | onde | conserto |
|---|---|---|
| **NavMesh corrompida** — `Invalid serialized file header`, `m_NavMeshData` quebrada, Monstro não funcionava | `SampleScene` | rebake com `NavMeshSurface.BuildNavMesh()` e gravação do asset por cima do arquivo podre |
| **Dois `AudioListener` ativos** — warning em loop da Unity | `Tela_1` | o listener saiu das câmeras e foi para a **raiz do Player**, na prefab; e o da `CameraCinematica` foi removido |
| **`Camera.main` = null** — o `MonsterAI.cs` usa `Camera.main` na linha 309 para achar o jogador | `Tela_1`, `Tela_1 1`, `SampleScene` | tag **`MainCamera`** na `FirstPersonCamera`, na prefab. A `ThirdPersonCamera` ficou `Untagged` de propósito, para não haver duas |
| **Player flutuando 0,90 m** acima do chão | `SampleScene` | descido para `y = 0,426` (chão em 0,406); a `AreaDeSpawn` acompanhou |

O `AudioListener` na raiz do Player resolve de vez: antes ele morava nas câmeras, e como o
`CameraRail` desliga as câmeras do Player durante o filminho, o listener ia junto. Na raiz
ele fica sempre ativo, e sempre um só.

#### Resultado medido

| cena | refs quebradas | AudioListeners | `Camera.main` |
|---|---|---|---|
| `Tela_inicial` | 0 | 1 | Main Camera |
| `Tela_1` | 0 | 1 | FirstPersonCamera |
| `Tela_2` | 0 | 1 | Main Camera |
| `Tela_1 1` | 0 | 1 | FirstPersonCamera |
| `SampleScene` | 0 | 1 | FirstPersonCamera |

**Em Play Mode, com o console limpo antes:**

- **`Tela_1`: zero erros, zero warnings.** Filminho rodou, controle voltou, `isGrounded = true`.
- **`SampleScene`: zero erros, zero warnings.** O Monstro está com `isOnNavMesh = true` e
  andando (velocidade 2,60) — a NavMesh voltou a funcionar de verdade.
- **Compilação:** 23 `.cs` no disco, 23 nas assemblies, **nenhum de fora**.
- **Menu:** o botão da `Tela_inicial` está ligado em `MainMenuManager.PlayGame`, que carrega
  `Tela_1`, que está no build. O caminho de entrada do jogo fecha.

#### Item do ACHADOS que dá para fechar

**Os dois arquivos de Input System não são um problema.** O `docs/ACHADOS.md` marcava isso
como 🔴 e como causa clássica de "o input não responde". Verificado por GUID:
`Assets/NÃO MEXER_/InputSystem_Actions.inputactions` (o template da Unity) é referenciado por
**zero** arquivos — está inerte. O de verdade,
`Assets/Scripts/Player e Câmera/PlayerInputActions.inputactions`, é o que está ligado na
`Player.prefab` e nas cenas de `_Recovery`. **Não é preciso apagar nada para o input
funcionar.**

### O que NÃO mexi, de propósito

- **`Tela_1 1.unity` está ligada nas cenas do build**, junto com a `SampleScene`. As duas vão
  para dentro do `.exe`. Mexer nisso é **Build Settings**, que a seção 10 do `CLAUDE.md`
  proíbe para o agente. Decisão de vocês, e é um clique: `File > Build Settings`, desmarcar.
- **`ConfigurarMaterialesMadeira.cs`** continua apontando para um FBX que nunca entrou no
  repositório. É Editor Script, trata com dialog e não quebra a build — não atrapalha rodar.
- **Escala do Player (2,166)** dentro de um quarto que não foi feito para ela. É o item de
  escala do `docs/ACHADOS.md`, e é decisão do JP.

### Câmera mais perto, e desafio dos 5 cubos na SampleScene (Claude Opus 5, a pedido da Julia)

#### Câmera menos distante

Ontem a câmera de 3ª pessoa passou a multiplicar os valores do Inspector pela escala do
Player, e com `cameraDistance = 3,5` isso virou **7,58 m** — longe demais.

Na `Player.prefab`: `cameraDistance` **3,5 → 1,8** (3,90 m no mundo) e `cameraHeight`
**1 → 0,5** (1,08 m acima do pivô, era 2,17 m). O `cameraHeight` menor também tira a origem
do teste de parede de dentro do `Teto`, que era o caso degenerado de ontem — agora ela nem
chega lá.

São dois campos no Inspector do Player. Mexa à vontade: os valores valem "para o Player em
escala 1" e o código multiplica pela escala real.

#### Desafio dos 5 cubos

Pedido: cinco cubos perto do Player na `SampleScene`; interagir com os cinco e depois voltar
para a área de spawn.

**Dois scripts novos em `Assets/Scripts/ObjetosScripts/`:**

- **`CuboDoDesafio.cs`** — implementa `IInterativo`, então funciona com o `InteractionSystem`
  que já existe, sem mexer nele. Cada cubo conta **uma vez só**; interagir de novo só loga.
  Troca de cor em três estados (normal / em destaque / coletado), usando `_BaseColor` na URP
  com queda para `_Color` no shader padrão.
- **`DesafioDosCubos.cs`** — conta os coletados, e quando fecha os cinco pede para voltar.
  Fica checando a distância do jogador até a `areaDeSpawn` (só no plano do chão, para pular
  ou degrau não atrapalhar) e conclui quando ele entra no raio. Tem `Reiniciar()` público e
  desenha um **gizmo verde** da área na janela Scene.

**Montado na `SampleScene`:** objeto `DesafioDosCubos` com os filhos `AreaDeSpawn` (no ponto
onde o Player nasce, raio 4 m) e `Cubos` com os 5 cubos, em arco a 6 m do Player, nos ângulos
0°, 60°, 120°, 240° e 300°. Cada cubo é 1,5 × 3 × 1,5 m, apoiado no chão — **altos de
propósito**, para ficarem na linha de visão de um personagem de 4,3 m.

**Mirabilidade medida** (mesma varredura usada no ursinho), com `distanciaInteracao = 3 m`:

| cubo | posições que conseguem mirar | faixa de mira |
|---|---|---|
| 1, 4, 5 | 36/36 | 68% |
| 2, 3 | 25/34 | 50% |

Para comparar: o ursinho da `Tela_1 1`, depois de consertado, dá 19/62 e 12%. Estes são
fáceis de acertar.

**Testado em Play Mode, o fluxo inteiro:**

| passo | resultado |
|---|---|
| interagir com 4 cubos | contagem 1, 2, 3, 4 |
| interagir de novo no cubo 1 | continua 4 — não conta duas vezes |
| pegar o 5º **longe** do spawn | 5/5, `concluido = False`, loga "volte para a área de spawn" |
| voltar a 6 m do centro (fora do raio 4 m) | `concluido = False` |
| entrar a 1,5 m do centro | **`concluido = True`**, loga "DESAFIO CONCLUÍDO" |

### Duas armadilhas encontradas no caminho

1. **A Unity se recusou a compilar o `DesafioDosCubos.cs` por entrada corrompida na
   `Library`.** O arquivo existia, era C# válido, aparecia no `AssetDatabase` como
   `MonoScript` e tinha `.meta` com GUID — mas **não estava na lista de fontes da
   `Assembly-CSharp`** (20 arquivos em vez de 21), e por causa disso a assembly inteira ficava
   quebrada, com um `CS0246` enganoso apontando para o *outro* arquivo. Não adiantou
   `Refresh`, `ImportAsset` com `ForceUpdate`, apagar o `.meta`, nem
   `RequestScriptCompilation` com `CleanBuildCache`. **O que resolveu:** `AssetDatabase
   .DeleteAsset` no arquivo e recriar em seguida. Se acontecer de novo, é esse o caminho —
   e dá para conferir com `CompilationPipeline.GetAssemblies()` comparando com os `.cs` do
   disco.
2. **`collider.bounds` mente logo depois de criar objetos por código no editor.** A primeira
   medição de mirabilidade deu **0% nos cinco cubos**, porque os `bounds` ainda reportavam a
   origem do mundo. Falta `Physics.SyncTransforms()`. Depois dele, 50–68%.

### Achado de passagem, não é meu para consertar

**A NavMesh da `SampleScene` está corrompida.** O console diz
`Invalid serialized file header. File: "Assets/Scenes/SampleScene/NavMesh-SampleScene.asset"`
e em seguida `Failed to create agent because there is no valid NavMesh` — o Monstro não
funciona nessa cena. Não tem relação com os cubos. Conserto: selecionar o objeto `NavMesh`
na cena e clicar em **Bake**.

---

## 22/09/2026 (segunda)

### Fim do trilho devolve a gameplay + canvas somem no filminho (Claude Opus 5, a pedido da Julia)

Pedido: *"quando acaba o trilho ele não está indo para a câmera em primeira pessoa da
gameplay, e quando tá na parte filminho, os canvas devem ser desativados."*

**O que estava errado:** o `MoverCamera()` do `CameraRail.cs`, ao chegar no último ponto,
só fazia `executando = false` e escrevia log. Ninguém desligava a `CameraCinematica` e
ninguém avisava o `CameraController`. Como o `CameraController.Start()` já tinha ligado a
`FirstPersonCamera`, a cena rodava com **duas câmeras ativas em depth 0** — e com **dois
`AudioListener`**, que é o aviso chato no console. Quem ganhava o render era indefinido, e
na prática ficava a cinemática para sempre. A interface toda ficava por cima do filminho, e
o jogador conseguia andar durante ele.

**`CameraRail.cs`** — campos `[SerializeField] private` viraram `public` (seção 7 do
`CLAUDE.md`: o projeto não usa `[SerializeField]` privado; os nomes serializados não mudaram,
então nenhuma referência caiu). Adicionado:

- `EntrarNoModoFilme()` — no `IniciarTrilho()`: desliga os canvas da gameplay, desliga o
  `PlayerMovement`, desliga os scripts de `scriptsPausados` e chama
  `cameraDoPlayer.DesativarControle()`.
- `TerminarTrilho()` — no fim do trilho: desliga a `CameraCinematica` **antes** de ligar a do
  Player (senão dá o conflito de dois `AudioListener` por um frame), devolve os canvas,
  religa os scripts e chama `cameraDoPlayer.AtivarControle()`.
- Os canvas voltam **no estado em que estavam antes do filminho**, guardado em
  `estadoOriginalCanvas`. É de propósito: ligar tudo na força acenderia painel que devia
  estar apagado.

**`CameraController.cs`** — ganhou `public bool controleAtivo` (true por padrão), os métodos
`AtivarControle()` / `DesativarControle()` e um `AplicarEstado()` privado. `Update()` e
`LateUpdate()` saem cedo quando `controleAtivo` é false. O `Start()` agora chama
`AplicarEstado()` em vez de `SetCamera(true)` fixo — **isso é o que faz a ordem dos `Start()`
entre os dois scripts não importar.** Quem rodar por último chega no mesmo estado.

> ⚠️ O `CameraController.cs` está salvo em **Latin-1, não UTF-8** — é por isso que os
> comentários dele aparecem como `C�mera`. Editei preservando os bytes para não piorar.
> Converter o arquivo para UTF-8 conserta a exibição, mas é mudança de arquivo inteiro e
> não foi feita aqui.

**Ligado no Inspector do `CameraRailController`** (pela API, continua tudo editável à mão):

| Campo | Valor |
|---|---|
| `cameraDoPlayer` | `CameraController` do Player |
| `movimentoDoPlayer` | `PlayerMovement` do Player |
| `canvasDaGameplay` | TABtutorial · CrosshairCanvas · StaminaCanvas · InteractionCanvas |
| `scriptsPausados` | `InteractionSystem` do Player |

**`ControlsCanvas` ficou de fora de propósito** — quem liga e desliga ele é o
`ControlsPanel` (tecla TAB), que já faz `SetActive(false)` no próprio `Start()`. Pôr ele na
lista seria duas coisas mandando no mesmo objeto.

**Testado em Play Mode**, nos dois momentos:

| | durante o filminho | depois do trilho |
|---|---|---|
| CameraCinematica | ativa | desligada |
| FirstPersonCamera | desligada | **ativa** |
| AudioListeners ativos | 1 | 1 |
| Canvas da gameplay | todos off | todos de volta (ControlsCanvas segue off, correto) |
| PlayerMovement / InteractionSystem | off | on |
| `isGrounded` | — | True |

### Animação: `walk` entre idle e run, e Valentina sentada na cama durante o filminho (Claude Opus 5, a pedido da Julia)

Três pedidos: pôr a animação de andar entre parado e correndo, pôr a Valentina sentada em
cima da cama enquanto o trilho roda, e voltar ao normal quando acabar. Os três estão feitos
e testados em Play Mode — mas **o caminho até lá revelou um problema de export que vale mais
que a tarefa**.

#### O problema de verdade: os `.blend` novos têm rig em outra escala

As animações estão em `Assets/Art/Animações do player/`: `valentina andando.blend`,
`sentada.blend` e `sentada balancando.blend`.

| arquivo | `Armature` localScale | `Hips` na curva |
|---|---|---|
| `Valentina Idle.blend` | 0,2877 | 1,778 |
| `Valentina Run (1).blend` | 1,1631 | 1,878 |
| **`valentina andando.blend`** | **0,0100** | **344,622** |
| **`sentada.blend`** | **0,0100** | **191,563** |
| **`sentada balancando.blend`** | **0,0100** | **311,098** |

Os três novos estão em **centímetros**, com o `Armature` em 0,01 compensando dentro do
próprio arquivo. O modelo que está na prefab vem do `Valentina Idle.blend`, cujo `Armature`
é 0,2877. Tocar o clipe de andar nesse rig jogava os ossos para **~100x** — a Valentina
subia para 386 m de altura.

É exatamente o `Ctrl+A > Apply Scale` da seção 6 do `CLAUDE.md`, não feito antes de exportar.
E não dava para corrigir escalando uma curva: os clipes têm **330 curvas**, com posição *e*
escala em todos os 33 ossos.

#### Conserto: Humanoid com retargeting

Os 5 arquivos viraram `animationType = Human` com avatar próprio. O retargeting do Mecanim
trabalha em espaço de músculo e **ignora a escala do rig de origem** — é o mecanismo feito
exatamente para isto.

O auto-mapeador acertou 28 ossos no idle e no run, mas só **27 nos três novos: faltou o
`Head`**, que é osso obrigatório, e por isso o avatar saía `isHuman = false`.

**Duas tentativas, a primeira errada — registrado porque o erro é sutil:**

1. ❌ Copiei o `humanDescription` inteiro do idle para os outros. Deu `isHuman = true` em
   todos, mas o `humanDescription` carrega junto o **`skeleton`**, ou seja o bind pose do
   idle. Aplicado num rig que está em centímetros, a normalização sai errada: medido em
   Play Mode com frames reais, o `walk` ficava com o pé **11,82 m acima do pivô**.
2. ✅ Deixei cada arquivo se auto-mapear e acrescentei **só a entrada `Head <- mixamorig:Head`**
   no array `human`, preservando o `skeleton` de cada um.

> **Lição para a próxima:** medir com `anim.Update()` forçado **mente**. Deu resultado
> diferente a cada chamada e chegou a dizer que o `run` estava quebrado quando não estava.
> Só medição com frames reais (uma chamada por estado, deixando o jogo rodar entre elas) é
> confiável.

**Resultado final, pé de apoio em relação ao pivô:**

| estado | antes | depois |
|---|---|---|
| idle | ok | 0,55 m |
| walk | **11,82 m** (voando) | 0,45 m |
| run | ok | 0,35 m |
| sentada | — | quadris 0,32 m acima do pivô, pernas penduradas |

A variação de 0,35 a 0,55 m é a fase da passada, não defeito — é a altura do osso do
tornozelo.

#### O que foi mexido

- **Loop ligado** em `Valentina Idle`, `valentina andando` e `sentada` — estavam com
  `loopTime = false`, o idle inclusive. O idle **nunca tinha feito loop**.
- **`playerAnimator.controller`**: estados `walk` (clipe andando) e `sentada`; parâmetro
  `Sentada` (Bool). As transições diretas idle↔run foram trocadas por
  **idle ⇄ walk ⇄ run**: `Speed > 0,1` entra em walk, `Speed > 0,75` entra em run.
  `AnyState → sentada` com `Sentada = true`; `sentada → idle` com `Sentada = false`.
- **`PlayerMovement.UpdateAnimation()`**: o `Speed` antes era só `moveInput.magnitude`, que
  não distingue andar de correr. Agora é `magnitude × (IsSprinting() ? 1 : 0,5)` — andando
  vai até 0,5, com shift vai até 1,0 — e usa o `SetFloat` com amortecimento
  (`suavizacaoAnimacao`, novo campo público, 0,1 por padrão).
- **Prefab do Player**: o `Animator` ganhou o `Valentina IdleAvatar` (estava sem avatar, o
  que era aceitável em Generic e é obrigatório em Humanoid). `applyRootMotion` continua
  `false` — quem move é o `CharacterController`.
- **`CameraRail.cs`**: campos `poseSentada`, `animatorDaValentina` e `parametroSentada`.
  `SentarNaCama()` desliga o `CharacterController` (senão a PhysX devolve o Player para o
  lugar de antes), teleporta para o marcador, religa e liga o `Sentada`. No
  `TerminarTrilho()` o `Sentada` volta a `false`.
- **`Tela_1`**: objeto vazio **`PoseSentadaValentina`** em `(-4,18 · 2,48 · -10,50)`,
  rotação Y 270°. O Y é o topo do colchão (`Camapd3`) e o X é a beirada menos 0,35 m, para
  as pernas caírem para fora da cama, viradas para dentro do quarto. **É só mover esse
  objeto na cena para ajustar a pose** — não precisa tocar em código.

### Idle saltando no loop e câmera de 3ª pessoa bizarra (Claude Opus 5, a pedido da Julia)

#### 1. O idle saltando — erro meu, de hoje mais cedo

Relato: *"a animação de idle tocando toda hora, tá muito estranho."*

**Fui eu que causei.** Liguei o `loopTime` do idle na entrega das animações, mas **não liguei
o `loopPose`**. Sem `loopPose`, a Unity repete o clipe cru: se o último frame não é igual ao
primeiro, salta na costura toda vez que dá a volta.

Medido, diferença entre o primeiro e o último frame:

| clipe | `loopPose` antes | maior diferença na costura |
|---|---|---|
| `Valentina Idle` | **false** | **1,0085** no ombro esquerdo |
| `valentina andando` | **false** | 1,6724 no `RootT.z` |
| `Valentina Run (1)` | true | (já estava costurado) |

O idle dura 0,97 s — então o ombro pulava uma unidade inteira **a cada 0,97 segundo**. É
exatamente o "toda hora" do relato.

**Conserto:** `loopPose = true` no idle e no andando. Agora os três clipes cíclicos têm
`loopTime` e `loopPose` ligados. O `sentada` fica sem `loopPose` de propósito — a costura
dele mede 0,0000, já fecha sozinho.

#### 2. A câmera de 3ª pessoa — três problemas somados

Relato: *"a câmera em 3ª pessoa tá bugando muito, ela tá bem bizarrinha."*

| # | problema | efeito |
|---|---|---|
| 1 | `collisionLayers` = **Everything**, e o `SphereCast` **nascia dentro da cápsula do próprio Player** | cast dentro de um collider devolve **distância 0** → a câmera grudava no mínimo, dentro da cabeça da Valentina |
| 2 | valores em unidades fixas (`cameraDistance` 3,5) com o Player em **escala 2,166** | 3,5 m atrás de um personagem de 4,3 m equivale a 1,37 m num personagem de 1,7 m — câmera dentro do corpo |
| 3 | a câmera ficava deslocada no ombro mas **olhava para o pivô puro** | quanto mais perto da parede, mais ela girava para dentro — o giro esquisito |

**Consertos em `CameraController.UpdateThirdPersonCamera()`:**

- **`SphereCastAll` no lugar de `SphereCast`**, descartando as batidas cujo
  `transform.IsChildOf(transform)` — ou seja, o próprio Player. Resolve o (1) sem precisar
  mexer em layer nenhuma. *(O projeto tem uma layer `player` (3) criada e não usada — o
  Player está na `Default`. Usar ela seria o caminho "certo", mas mexe no
  `camadasInteracao` do `InteractionSystem` e no `MonsterAI`, então não fui por aí.)*
- **Tudo multiplicado pela escala do Player** (`cameraDistance`, `cameraHeight`,
  `shoulderOffset`, `collisionRadius`, `collisionOffset` e o mínimo de 0,15). Os valores do
  Inspector agora significam "para o Player em escala 1". Resolve o (2) e **não quebra de
  novo se a escala mudar** — e ela já mudou duas vezes hoje, de 1,8 para 2,166.
- **A origem do cast passou a incluir o deslocamento do ombro.** Antes o teste de parede
  saía do pivô e a câmera ia parar em outro lugar: ela terminava num ponto que o teste nunca
  tinha conferido.
- **A câmera agora olha na direção em que o Player olha**, em vez de mirar no pivô.
  Resolve o (3).
- `currentCameraDistance` passou de `Mathf.Min(...)` para receber o `targetDistance`. Com o
  `Min` ela encolhia e **não voltava** enquanto ainda houvesse parede, mesmo que a parede
  estivesse mais longe.

**Um quarto problema apareceu durante o teste:** o `Teto` era acertado com distância 0,
porque `cameraHeight` escalado põe a origem do cast a **5,76 m** e o quarto tem só ~6,3 m de
pé-direito. Mesma armadilha do (1), mas com o cenário. Agora batidas com
`distance <= 0,001` são descartadas — não medem nada.

**Medido em Play Mode:**

| situação | antes | depois |
|---|---|---|
| dentro da cápsula da Valentina | sim | **não** |
| distância no canto do quarto (parede real a 1,95 m) | 0,32 m (grudada) | **1,52 m**, correto |
| distância em espaço aberto | 0,32 m (grudada) | **7,58 m**, o máximo |
| desalinhamento com o olhar | girava | **0,0°** |
| atravessa parede? | — | **não** |

### Ainda vale olhar

- **`cameraHeight = 1` deixa a origem da câmera a 5,76 m** num quarto de ~6,3 m. Está
  funcionando porque o código ignora a batida degenerada, mas a câmera fica quase raspando o
  teto. Baixar o `cameraHeight` para ~0,5 no Inspector daria mais folga. É ajuste de
  enquadramento, não é minha decisão.
- De novo, o fundo de tudo é a **escala do Player (2,166)** dentro de um quarto que não foi
  feito para ela. `docs/ACHADOS.md`.

### Pose voltou para o lugar antigo e virou ajustável com o jogo rodando (Claude Opus 5, a pedido da Julia)

Pedido: *"coloca ela no lugar antigo e deixa de um jeito que eu consiga posicionar ela no
lugar enquanto toca o filminho."*

O `PoseSentadaValentina` voltou para **`(-2,160 · 0,381 · -10,020)`, rotação 0** — o mesmo
lugar onde o Player já nascia na `Tela_1`. A posição na beirada da cama foi desfeita.

**O que mudou no `CameraRail.cs`:** antes o `SentarNaCama()` teleportava a Valentina uma vez
e pronto — arrastar o marcador depois não fazia nada. Agora existe `AcompanharPose()`,
chamada **todo frame** enquanto o filminho roda (tanto durante o trilho quanto durante a
espera do fim). A Valentina fica **colada no marcador**.

Na prática: com o jogo rodando, arraste o **`PoseSentadaValentina`** na janela Scene e ela
vai junto, na hora. Dá para achar a pose olhando o resultado em vez de chutar número.

Tem um campo novo, `seguirPoseDuranteOFilminho` (ligado por padrão). Desligando, volta ao
comportamento antigo de posicionar só uma vez no começo.

**Detalhe que fazia isso não funcionar:** o `CharacterController` agora fica desligado o
**filminho inteiro** (antes era desligado e religado só no teleporte). Com ele ligado a
PhysX devolve o Player para o lugar de antes no frame seguinte, e o arrasto não "pega".
Ele é religado no `TerminarTrilho()`, guardado em `controladorDoPlayer`.

> **Para guardar a pose que você achou:** posição mexida em Play Mode se perde ao parar. No
> marcador, clique com o botão direito no componente **Transform → Copy Component**, saia do
> Play Mode, e **Paste Component Values**. Aí é só salvar a cena.

**Testado em Play Mode, rodada limpa:**

| momento | resultado |
|---|---|
| início (t=0) | Player em `(-2,160 · 0,381 · -10,020)`, `Sentada = true`, CC desligado, 0 canvas |
| arrastando o marcador no meio | movi para `(-3,5 · 1,2 · -9,0)` rot 45° e depois `(-6,0 · 2,5 · -11,0)` — a Valentina seguiu nos dois |
| fim (t≈22,8s) | volta exata para `(-2,160 · 0,381 · -10,020)`, `isGrounded = true`, velocidade zero, estado `idle`, `Sentada = false`, cinemática desligada, 4 canvas de volta |

Como ela termina no chão e não mais em cima da cama, **o item 1 da lista abaixo (ela ficava
de pé em cima da cama) deixou de acontecer** — mas continua valendo se alguém puser a pose
na cama de novo.

### Três coisas que ficam para vocês

1. **Quando o filminho acaba, a Valentina fica de pé EM CIMA da cama.** Medido: ela vai de
   `y = 2,48` para `y = 3,21`. O `BoxCollider` da `Mapa/Cama` tem topo em **3,13**, bem acima
   do colchão visível (**2,48**) — então "em cima do colchão" é dentro do collider, e o
   CharacterController a empurra para cima. Ela fica **estável e apoiada**, não é cuspida
   nem cai. Mas se a ideia é ela levantar e já estar no chão, falta um segundo marcador de
   "pose de saída". Não fiz porque onde ela deve ficar de pé é level design, não é minha
   decisão.
2. **A cápsula dela atravessa o `Teto` em pé na cama** (topo da cápsula em 7,54 m contra
   teto em ~6,7 m). É consequência do Player estar em **escala 2,166** — de novo a escala do
   `docs/ACHADOS.md`.
3. **`sentada balancando.blend` não está sendo usado** e é o único que continua sem loop
   (2,17 s). Se a ideia era sentar e ficar balançando, dá para encadear: `sentada` uma vez e
   depois `sentada balancando` em loop. Usei o `sentada` porque foi o que foi pedido.

> **O certo mesmo** continua sendo reexportar os três `.blend` do Blender com
> `Ctrl+A > Apply Scale`, a partir do mesmo armature do idle. O Humanoid está segurando a
> peça, e está testado, mas é remendo em cima de export errado.

### Ursinho impossível de clicar na `Tela_1 1` — era o armário, não o ursinho (Claude Opus 5, a pedido da Julia)

Relato: *"na scene tela 1_1 tem um ursinho e a colisão dele tá horrível, tá muito difícil
clicar nele."*

**O `BoxCollider` do Ursinho está certo** — acompanha o modelo (AABB 2,35 × 1,91 × 2,26
contra mesh de 1,63 × 1,38 × 1,40 girado 70°). Não foi tocado.

**A causa é o `Mapa/Armário3 1/Cube`: `MeshCollider` com `convex = true`.** Convex manda a
PhysX trocar a malha por um **casco convexo**, que tapa todo vão e reentrância. O armário
(296 tris, com vãos) virou um **bloco maciço de 4,84 × 5,32 × 3,47 m** — e o ursinho fica
inteiro dentro desse volume. Todo raio do `InteractionSystem` batia no armário antes de
chegar no ursinho.

O `InteractionSystem` usa um `Physics.Raycast` fino da câmera, com
`QueryTriggerInteraction.Ignore`: se tem casco na frente, não existe ângulo que salve.

**Medido, varrendo 72 ângulos × todas as posições onde a cápsula do Player cabe, mirando de
−10° a +70°:**

| | posições de onde dá pra acertar | faixa de mira que acerta |
|---|---|---|
| **antes** (`convex = true`) | 4 de 61 | 1% |
| **depois** (`convex = false`) | **19 de 62** | **12%** |

**Feito:** `convex = false` no `Mapa/Armário3 1/Cube`. Só isso. Zero Rigidbody na cena
inteira — `convex` só é obrigatório em `MeshCollider` de Rigidbody não-cinemático, então
aqui não servia para nada.

**Conferido que não abriu buraco:** com o casco ligado o Player cabia em 5 de 150 pontos
dentro do volume do armário; sem o casco, 7 de 150. A malha real continua barrando — o
jogador não passou a entrar dentro do móvel.

### O que eu testei e decidi NÃO fazer

- **`SphereCast` no lugar do `Raycast`** para perdoar a mira: medido, vai de 12% para 15%
  da faixa, e **diminui** as posições válidas de 19 para 18 (a esfera encosta no cenário
  antes). Não compensa mexer no sistema de interação inteiro por isso.
- **Aumentar o `distanciaInteracao`** (está 3 m): de 3 para 4 m sobe de 19 para 21 posições
  e satura — 5 m e 6 m dão exatamente o mesmo. Não é o gargalo.
- **Desligar `convex` da cama e da `mesa2`** (têm o mesmo problema, `Mapa/cama/Cube`,
  `Cube.001`, `Cube.004` e `Mapa/Quarto/mesa2`): medido, **não muda nada** para o ursinho —
  19/62 com ou sem. Como mexer neles altera onde o jogador anda, ficou de fora. **Continuam
  com `convex = true` e o mesmo defeito latente** — quando algum objeto perto da cama ou da
  mesa ficar difícil de clicar, é isto aqui.

### O pano de fundo que não é meu para resolver

O Player desta cena tem **escala 2,166** — cápsula de **4,33 m** com o olho a 3,21 m do chão.
O ursinho tem 1,9 m de altura e mora entre o armário e a parede. Mesmo com o armário
consertado, é um gigante olhando para baixo num boneco pequeno num vão apertado: daí as
19 posições de 62, e não 62 de 62. Encolher o Player é a **escala** do `docs/ACHADOS.md`, e
é decisão do JP.

> Nota: a escala do Player na `Tela_1 1` (2,166) é **diferente** da `Tela_1` (1,8). São a
> mesma prefab com override de escala por cena.

### Espera de 4s no último ponto antes da gameplay (Claude Opus 5, a pedido da Julia)

Pedido: *"coloca um time de uns 4 segundos antes de começar a jogar quando chega no final
do trilho."*

`CameraRail.cs` ganhou `public float esperaNoFim = 4f`. Ao chegar no último ponto a câmera
**fica parada lá** por esse tempo — interface ainda escondida, Player ainda congelado — e só
depois o controle volta. Com `esperaNoFim = 0` devolve na hora, como era antes.

O fim do trilho virou dois passos: `ComecarEspera()` (para o trilho e arma o cronômetro) e
`EsperarNoFim()` (conta no `Update` e chama o `TerminarTrilho()` no fim). O `Update()` checa
`esperando` **antes** de `executando`, senão o `return` de cima mataria a contagem.

**Medido em Play Mode:** com `duracaoPorTrecho = 4` e 4 pontos (3 trechos), o trilho acaba
em 12s; em t=16,9s a `CameraCinematica` estava desligada, a `FirstPersonCamera` ligada e os
4 canvas de volta. Bate com os 12 + 4 = 16s.

**Observação:** o `duracaoPorTrecho` estava 3 no começo desta sessão e está **4** na cena
salva agora. Não fui eu — mudou no Inspector durante o trabalho. Fica registrado porque muda
a duração do filminho de 9s para 12s.

### ⚠️ Recompilar durante o Play Mode mata o trilho

Achado testando: se a Unity recompilar script com o jogo rodando, o domain reload **zera os
campos privados** do `CameraRail` (`executando`, `esperando`, `pontoAtual`, `tempoEspera`)
sem chamar o `Start()` de novo. O trilho para no meio, o controle nunca volta e o jogo fica
travado na câmera cinemática, sem erro nenhum no console.

É problema **só do editor**, não vai para a `.exe`. Mas custa tempo de debug achando bug que
não existe. Se travar assim: sair do Play Mode e entrar de novo.

### Armadilha achada no meio do caminho

**Nenhuma câmera da `Tela_1` tem a tag `MainCamera`** — `Camera.main` devolve `null` na cena
inteira. O `InteractionSystem` não se importa (tem `ObterCameraAtiva()` com fallback), mas o
**`MonsterAI.cs` usa `Camera.main` na linha 309** para achar o jogador. Na `Tela_1` isso
seria `null` silencioso. Não mexi — é da área do Monstro, e taggear câmera é decisão de quem
montou a cena.

**Detalhe de teste, para quem for repetir:** com a janela da Unity sem foco o jogo congela no
frame 2 (`Run In Background` desligado no projeto). Liguei `Application.runInBackground` só
em runtime para medir; **o Player Settings não foi tocado** — conferido depois, continua
`False`.

### Colisão do player com o chão — consertado no `Player.prefab` (Claude Opus 5, a pedido da Julia)

Sintoma relatado: "a colisão do player com o chão não está funcionando". **O
`PlayerMovement.cs` não tinha nada de errado** — o bug estava todo na montagem do
`Assets/Art/MdPlayer/Player.prefab`. Três peças do Player estavam em alturas diferentes:

| Peça | Onde estava | Onde deveria estar |
|---|---|---|
| `CharacterController` | `center = (0, 0, 0)` — cápsula centrada no pivô, metade dela (1,8 m) **abaixo** do pivô | pé da cápsula no pivô |
| Modelo `Valentina Idle` | `localPos = (0, 2.428, 0.480)` — flutuando **4,37 m acima** do pivô e 0,86 m à frente | nos pés, centrado |
| `CapsuleCollider` extra | `center = (0, 3.244, 0.542)` — collider solto boiando ~5 m no ar | não devia existir |

Com isso o Player na `Tela_1` ficava com o pé da cápsula em **y = −0,54** contra um chão
cujo topo é **y = 0,361**: nascia **90 cm enterrado no chão**. O PhysX não resolve isso como
"apoiado" — empurra, escorrega e a gravidade continua somando. Daí o "não colide".

O `CapsuleCollider` extra foi claramente uma tentativa anterior de consertar por cima:
envolvia o *modelo flutuante*, não o personagem. Um `CharacterController` **já é** o
collider — o segundo collider não ajuda e ainda é acertado pelos raycasts da câmera e da
interação.

**Feito** (pela API da Unity, nada de YAML na mão):

- `CharacterController.center` → `(0, 1, 0)`. Pé da cápsula exatamente no pivô.
  Altura continua 2 × escala 1,8 = **3,60 m**; raio 0,9 m.
- `Valentina Idle.localPosition` → `(0, 0.154, 0)`. Pés no pivô, centrado no eixo da cápsula.
- `CapsuleCollider` extra **removido**.
- `CameraPivot.localPosition` → `(0, 1.868, 0)`, ou seja, olhos a **3,36 m** do chão (94% da
  altura do modelo). Estava em `(0, 3.781, 0.696)` — mirado no modelo flutuante; se ficasse
  lá, a câmera ficaria 3 m acima da cabeça depois da correção.
- Na `Tela_1`, o Player foi para `(-2.160, 0.381, -10.020)` — pivô 2 cm acima do chão.

**Verificado em Play Mode**, não só no papel: após 585 frames, `isGrounded = True`,
`collisionFlags = Below`, `velocity = (0,0,0)`, `y = 0,3811` estável. Cápsula sem penetrar
nada. Varredura de 360 pontos na sala: **zero buracos no chão**.

### O que ainda precisa de alguém

- **`Tela_1 1.unity` e `SampleScene.unity` também usam o `Player.prefab`.** A correção do
  prefab vale para as três cenas, mas **o significado do pivô mudou** — antes ele era o meio
  da cápsula, agora é o pé. Quem abrir essas duas cenas precisa reposicionar o Player no
  chão (ou deixar cair, que ele se apoia sozinho). A `Tela_1`, que é a do build, já está
  ajustada e salva.
- **`CameraRail` está com o array `pontos` vazio** e cospe `UnassignedReferenceException`
  em loop no console. Não tem relação com a colisão, mas polui o console e esconde erro de
  verdade. `CameraRail.cs` ainda não está commitado.
- A altura de 3,60 m do personagem **não foi mexida** — é consequência da escala 1,8 do
  prefab dentro de um quarto de teto a ~6,7 m. Se a decisão for trazer o quarto para 1:1
  (ver `docs/ACHADOS.md`), a escala do Player entra junto. Isso é decisão do JP, não foi
  tocada aqui.

---
## 22/09/2026 (terça) — 4 dias para a FECART

### `MonsterAI` v3 — ele agora tem um mapa mental de onde você pode estar

JP: *"melhora ainda mais a IA, procura formas e pesquisa sobre outras IAs perseguidoras
que pode servir de base."* Pesquisadas três referências e trazido o que dá para medir:

| Referência | O que foi copiado |
|---|---|
| **Alien: Isolation** (Creative Assembly) | cones de visão sobrepostos · medidor de ameaça (*menace gauge*) para dosar o recuo · busca em anel (*donut search*) ao reabrir a procura |
| **Halo 3 / Third Eye Crime** (Damián Isla) | **occupancy map** — a crença do monstro sobre onde você está, difundida pela NavMesh |
| **Resident Evil 2 remake** (Mr. X) | os sentidos apertam quanto mais tempo ele passa sem te achar |

**O que entrou:**

- **`OccupancyMap.cs`** (arquivo novo) — grade de 4 m sobre a NavMesh, 3.534 células
  andáveis no labirinto. Ver ela = toda a certeza numa célula; o tempo passar = a certeza
  escorre para as vizinhas **andáveis** (não atravessa parede); olhar para um canto = aquele
  canto zera. A busca varre corredor, segue porta e **nunca volta para onde ele acabou de
  olhar** — sem nenhuma regra dizendo isso, é o que emerge das três operações.
- **Difusão direcional** — a dúvida escorre para o lado em que ela sumiu, não em círculo.
- **Busca em anel** — varreu tudo e não achou? Enquanto a pista for recente ele conclui
  "ela foi mais longe do que eu achava" e semeia um anel no raio que ela já teria alcançado.
  Antes disso ele desistia em 6 s.
- **Quem encerra a busca é a pista esfriar** (`searchGiveUpTime`, 45 s), não um cronômetro.
- **Três cones de visão** — frente (22 m/110°), canto do olho (10 m/200°, desconfia mais
  devagar) e colado (4 m, por trás inclusive).
- **Escuro conta**: com a lanterna dela apagada o alcance dele cai 35 %.
- **Medidor de ameaça** no lugar do cronômetro de caçada: recuar depende da **pressão que
  ele já colocou em você** (perto e te vendo pesa 3x mais que longe), não do relógio.
- **Faro que aperta**: 40 s sem nenhum sinal e visão/audição ganham +60 % de alcance.

**Medido em Play Mode**, mesmo cenário nos dois: ela é vista, corre 79 m em sprint pelo
labirinto e se esconde.

| | com o mapa | sem o mapa (v2) |
|---|---|---|
| Chegou a que distância dela | **12,7 m** | 32,1 m |
| Tempo procurando | **41 s** | 16 s |
| Distância andada | 214 m | 208 m |

Com tudo ligado (Director + emboscada), no mesmo cenário, ele **achou ela** (0,0 m) em 15 s.

**Custo: 0,025 ms por passo do mapa, 0,13 ms por segundo de jogo.** Não encosta nos 40 fps.

**Fontes consultadas** (para a Declaração de Uso de IA):
- *Revisiting the AI of Alien: Isolation* — Tommy Thompson, AI and Games
  https://www.aiandgames.com/p/revisiting-alien-isolation
- *The Perfect Organism: The AI of Alien: Isolation* — Game Developer
  https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation
- *Third Eye Crime: Building a Stealth Game Around Occupancy Maps* — Damián Isla (AIIDE)
  https://cdn.aaai.org/ojs/12663/12663-52-16180-1-2-20201228.pdf
- *Resident Evil 2 director talks Mr. X's AI* — PC Gamer
  https://www.pcgamer.com/resident-evil-2s-director-talks-mr-xs-ai-scary-footsteps-and-the-dmx-mod/

**Como a equipe mexe nisso (Julia/Letícia):** tudo é campo do Inspector no
`Assets/Art/Monstro/Monstro.prefab`, agrupado em `[Header]`. Para **ver** a cabeça dele:
Play → selecionar `Monstro` → os quadrados laranja na Scene são onde ele acha que você
está. Desligar em `showMemoryGizmo`; desligar o sistema todo em `memoryEnabled` (ele volta
para a busca da v2 sem quebrar nada).

## 16/09/2026 (quarta) — véspera do BETA

### Menu "Valentina" removido — tudo vira prefab e Inspector (JP, 16h20)

JP: *"não é pra fazer por essas abas que é mais fácil pra VOCÊ. é pra fazer o que é mais
fácil pra equipe em geral, não faça tudo por comando, e sim com que possamos alterar depois."*

Ele está certo: o item "Criar cubo do Monstro" apagava e recriava o objeto, então qualquer
ajuste feito no Inspector se perdia no clique seguinte. Feito:

- **`Assets/Art/Monstro/Monstro.prefab`** — o Monstro agora é prefab (raiz com `NavMeshAgent`
  + `MonsterAI` + `NavMeshModifier`, filho `Corpo`). A instância na `SampleScene` está
  conectada a ele. Ajuste no prefab vale para qualquer cena; arrastar para outra cena é só
  arrastar.
- **`Assets/Editor/MontadorNavMesh.cs` apagado.** O menu `Valentina > NavMesh` não existe
  mais. Nada se perdeu: o objeto `NavMesh` da cena tem o `NavMeshSurface` com o bake salvo.
- Regra registrada na seção 5 do `CLAUDE.md`.

**Como mexer daqui em diante (Julia/Letícia):**

| Quer | Faz |
|---|---|
| Refazer a NavMesh depois de mover parede | Selecionar `NavMesh` na Hierarchy → Inspector → **Bake** |
| Mudar velocidade, visão, ouvido do monstro | Selecionar `Monstro` → Inspector → campos com `[Header]`; **Overrides → Apply** se quiser que valha para o prefab |
| Trocar o cubo pelo modelo do Ursão | Abrir o prefab, apagar `Corpo`, pôr o modelo como filho na posição (0, 0, 0) com o pé no chão |
| Ver o que ele está pensando | Play → selecionar `Monstro` → `currentThought` e `stateHistory` no Inspector |
| Pôr o monstro em outra cena | Arrastar `Monstro.prefab` para a cena; a cena precisa ter a própria `NavMeshSurface` bakeada |


### `MonsterAI` v2 — "a IA ainda tá MUITO burra, melhora o máximo que conseguir" (JP, 15h)

Oito causas concretas, todas medidas no labirinto antes de mexer:

| # | Burrice | Correção |
|---|---|---|
| 1 | `huntSpeed` 3,4 < Player andando (5): **nunca pegava ninguém** | 6,3 — pega quem anda, não pega quem corre (8); a stamina decide |
| 2 | Ronda em círculo de 12 m num mapa de 240 m | Ronda por cobertura: 12–32 m, evita os 10 últimos pontos, 30 % de "faro" para o lado do Player |
| 3 | Investigação desistia no meio do corredor (timer contava desde a decisão) | Orçamento de viagem pelo comprimento real do caminho; `investigateTime` conta da chegada |
| 4 | Parado, cone fixo — dava para ficar do lado dele | Gira a cabeça ±75° quando parado; a 4 m "sente" mesmo fora do cone (parede ainda bloqueia) |
| 5 | Perdeu de vista → ponto aleatório em 5 m | Extrapola o rumo por 1,5 s; depois plano de busca ranqueado: rumo dela, atrás da quina de onde perdeu, alcançável |
| 6 | Ouvido só valia ao entrar em Investigate; ruído tremia o destino todo frame | Re-mira ao ouvir de novo; pista do ouvido atualiza a cada 0,5 s |
| 7 | Aceitava destino em bolsão fechado e ficava na parede | Todo destino passa por `IsReachable` (caminho completo) + detector de travado (2 s) |
| 8 | Emboscada nunca acontecia (precisava de pontos manuais) | Sem pontos, acha sozinho uma quina escondida de onde ela vem, a ≤ 14 m de caminho |

Bug pré-existente corrigido de quebra: certeza vinda do **ouvido** fazia Hunt ↔ Investigate
alternar a cada quadro (tocando som a cada troca). Agora enquanto ouve, continua caçando o som.

**Medido em Play Mode com um driver de teste** (arquivo temporário, não commitado):

| Teste | Antes | Depois |
|---|---|---|
| A · Player foge andando (5 m/s), visto | impossível pegar | pego em 2,3 s, grudado por 89 m |
| B · Player corre 8 m/s por 9 quinas e para escondido | — | perdeu aos 8,3 s, reencontrou aos 9,0 s, pegou aos 11,2 s |
| C · Ronda 90 s, Player a 166 m, sem Director | 10 células de 8 m · 32 m de alcance | ~29 células · 87 m de alcance |
| D · Player parado a 8 m, 120° fora do cone | nunca visto | visto aos 3,9 s |
| E · Emboscada automática (chance forçada a 100 %) | nunca (precisava de pontos manuais) | 4 s após perder de vista foi para uma quina escondida do último ponto visto, esperou 12 s e voltou a rondar |

A busca de quina varre 36 ângulos em 3 raios (5/8/12 m) e aceita desvio de até 30 m de caminho —
as paredes do labirinto têm 11–12 m, então dar a volta numa custa mais que os 14 m da primeira
versão, que nunca achava quina. O campo `stateHistory` no Inspector mostra as últimas trocas de
estado com o tempo, para depurar sem Console.

Ajustes de calibragem ficam no Inspector do `Monstro` (todos `public` com `[Header]`).
O `Corpo` (cubo) continua sendo só o filho; o Ursão entra no lugar dele sem mexer na IA.


### NavMesh completa na SampleScene + cubo do Monstro com `MonsterAI` (sessão nova, à tarde)

**Pedido do JP:** pegar a última versão do GitHub, fazer a NavMesh completa na `SampleScene`
e criar um cubo que futuramente será o monstro, com o `MonsterAI.cs` que ele enviou.

**Contexto:** a `SampleScene` é o labirinto de teste — 583 paredes (cubos de 0,23 m de
espessura, 8,9 m de altura) sobre um Chão de 243 × 222 m, tudo sob o objeto `Labirinto`.
Isso **não muda a regra 4 do `CLAUDE.md`** (ursinhos não andam, Ursão sem NavMesh): a NavMesh
aqui serve ao monstro de teste do labirinto. Se virar regra para o Ursão, é decisão do JP e
vai para `DECISOES.md`.

**Feito, tudo por Editor Script** (`Assets/Editor/MontadorNavMesh.cs`, menu **Valentina >
NavMesh**), rodado e conferido dentro da Unity 6000.3.6f1 nesta máquina:

- Objeto `NavMesh` com `NavMeshSurface` (pacote AI Navigation 2.0.9, já no projeto).
  Agente Humanoid (raio 0,5 · altura 2), geometria por **colisores físicos**, voxel 0,125
  (padrão seria 0,167 — as paredes finas pedem mais precisão), tile 256, `minRegionArea` 2.
  Bake salvo em `Assets/Scenes/SampleScene/NavMesh-SampleScene.asset`.
- **Resultado do bake:** 1.916 triângulos, 45.957 m² caminháveis, 4 regiões (a principal
  tem 89 % — as outras são bolsões fechados por parede, sem saída mesmo). Conferido que
  **nenhuma das 583 paredes tem NavMesh dentro** e que a borda fica a 0,5 m da parede.
- `Player` recebeu `NavMeshModifier` (ignorar no bake) e foi para a camada **`player`** —
  só nesta cena, por override do prefab. Motivo: o `MonsterAI` usa `Linecast` com máscara
  "tudo menos a camada do jogador"; com Player e paredes em Default, ou o monstro via
  através da parede ou a cápsula do jogador bloqueava a própria visão.
- Cubo **`Monstro`**: raiz sem escala com o pé no chão (`NavMeshAgent` + `MonsterAI` +
  `NavMeshModifier`) e filho `Corpo` (cubo 1 × 2 × 1 m, material `M_Monstro` no vermelho
  `#D74143` da paleta). Nasce em (−42,4 · 0,45 · 20,6): 47 m de caminho e 28 m em linha reta
  do Player, com parede no meio. `obstacleMask` = Default (só parede bloqueia a visão).
- `MonsterAI.cs` copiado para `Assets/Scripts/Monstro/`. **Compilou sem aviso.**
- **Testado em Play Mode:** agente sobre a malha, patrulha andando a 1,6 m/s; Player
  teleportado a 6 m sem parede → `Hunt` → "Peguei ela."; depois voltou a `Patrol` e
  `Investigate`. Zero erros no Console.
- **Teste controlado de visão** (monstro parado, Player a 5 m dentro do cone): com parede
  no meio `canSee=false`, awareness 0; sem parede `canSee=true`, awareness 1, `Hunt`.

**Pegadinha registrada:** `NavMeshAgent` escala `baseOffset` e `height` pelo `scale` do
transform. Um cubo com scale (1, 2, 1) e `baseOffset` 1 flutua 1 m acima do chão. Por isso
a raiz do Monstro é 1,1,1 e o cubo é filho. Quando o modelo do Ursão chegar, é só trocar o
`Corpo` — agente e IA ficam.

**O que a Julia/Letícia precisam fazer:** nada para a NavMesh funcionar — está bakeada e
salva na cena. Se mexerem nas paredes, selecionar `NavMesh` → **Bake** no Inspector
(o menu `Valentina` foi removido às 16h20, ver entrada acima). Sons do monstro
(`footstepClips`, `spotSounds` etc.) estão vazios — arrastar quando o JP entregar os `.wav`.


### Tentativa de contato direto com as outras máquinas — falhou

O JP pediu comunicação direta com as sessões da Julia, da Letícia e do Luigi. A ferramenta
de mensagem entre sessões passou a existir nesta sessão, e foi usada. Resultado nas três:
**"No agent named ... is reachable"**. As sessões de Remote Control que estavam online em
08/09 não estão rodando hoje. Nenhuma mensagem foi entregue.

### Zero commits em seis dias

O último commit de conteúdo é o `75746ed` da Julia, de 09/09 às 12:48. Desde então só
entraram commits de documentação, todos do JP. Só existe a branch `main` no servidor.

O JP relata que a Julia **commitou em 15/09 na máquina dela, sem dar push**. Isso é
invisível daqui — commit sem push existe em uma cópia só. É o mesmo risco de 03/09,
agora na véspera do BETA. Ela está 2 commits atrás; o `pull` mescla limpo porque os dois só
tocaram `docs/`.

### Três cards marcados como concluídos ontem — e o Git diz que não

Os três cards criados em 09/09 foram movidos para **"Revisão do PO"** e marcados como
completos em **15/09 entre 18:01 e 18:03**, com **nenhum comentário**. Verificado em
`origin/main` na mesma hora:

| Card | Trello diz | Git diz |
|---|---|---|
| Luigi — ursinho, prefabs, escala | Concluído 18:01 | **Nenhum FBX de ursinho. Zero `.prefab`.** Nada dele entrou no repositório. |
| Letícia — clone com LFS | Concluído 18:01 | **Zero commits dela**, como sempre. Não dá para verificar o clone daqui. |
| JP — critério do ALPHA | Concluído 18:03 | **Nenhuma entrada em `DECISOES.md`.** Se foi decidido, não foi escrito. |

**É a armadilha documentada na seção 8 do `CLAUDE.md`**, mais três vezes numa noite. A
regra do `docs/README.md` resolve o empate: *confira no Git, não no Trello*. Pelo Git, os
três estão abertos.

Não é possível saber daqui quem marcou. O intervalo de 70 segundos entre os três sugere
uma passada só, de uma pessoa.

---

## 10/09/2026 (quinta) — dia do ALPHA

### A Julia entregou — corrige o registro de ontem

A entrada de 09/09 diz que o trabalho dela estava sem backup há 7 dias. **Era verdade na hora
em que foi escrita** — o `git log` só via até 02/09 porque o push dela ainda não tinha saído.
Ela deu push em **09/09 às 12:48**, cerca de uma hora depois do card entrar no Trello.

Três commits com conteúdo chegaram:

| Commit | O que |
|---|---|
| `0220361` | 59 arquivos, **+8.219 linhas** — fonte Sketchy, teclas C/E/Shift/Tab, texturas da cama, `TabTutorial.cs`, `PlayerInputActions.cs` |
| `0cc5291` | Shader Graph e material do Poster |
| `75746ed` | `ss155.blend`, texturas, ajustes de material |

A `Tela_1.unity` recebeu **mais de 4.600 linhas** de mudança somando os três. O maior risco
isolado do projeto — trabalho de montagem de cena existindo em uma máquina só — está fechado.

### `.meta` órfão removido

O `Observado.cs` foi apagado em 09/09 às 11:40; a Unity da Julia já tinha gerado o
`Observado.cs.meta`, que entrou no push dela às 12:48. O merge deixou o `.meta` sem par —
o único órfão do repositório, contra a propriedade "nenhum `.meta` órfão" que a auditoria de
03/09 tinha verificado. Removido nesta entrada. **Nada estava anexado a ele**: o GUID
`9ac0b5071dde43a4d94ba6626c530093` aparecia 0 vezes na `Tela_1.unity`.

### O que a entrega da Julia NÃO resolveu

Os três 🔴 do [`ACHADOS.md`](ACHADOS.md) continuam abertos, e um deles piorou:

- **Zero prefabs** — e a `Tela_1.unity` cresceu mais 4.600 linhas. Quanto maior a cena, pior
  o conflito quando a Letícia voltar a trabalhar em paralelo.
- **Nenhum FBX de ursinho** — só o `untitled 1.fbx`, que é o kit de parede.
- **Dois arquivos de Input System** ainda coexistindo.

### Dois pontos novos para alguém olhar

**A fonte entrou:** `Sketchy.ttf` mais o `Sketchy SDF.asset`. O `CLAUDE.md` exige uma fonte
só, com acentuação em português testada. Numa fonte desenhada à mão, faltar `ã`, `ç` ou `õ`
é risco real — **alguém precisa escrever uma frase com acento e olhar na tela.** Isso é
julgamento humano.

**Os nomes provisórios voltaram:** `CADEIRAofJHJJHJ.png`, `ss155.blend`,
`textura mesa12222.png`, `Material.mat`, `New Shader Graph.shadergraph`. A
`textura mesa1.png` virou `CADEIRAofJHJJHJ.png` — textura de mesa com nome de cadeira. É a
mesma armadilha do `Untitled.blend` já registrada no `ACHADOS`. Duas mensagens de commit são
literalmente "Commit".

---

## 09/09/2026 (quarta) — ⚠️ véspera do ALPHA

### O alerta que importa mais que tudo nesta entrada

**O ALPHA é amanhã, 10/09 (quinta), e o Sprint 3 continua sem começar.**

O critério de saída do ALPHA no plano era "Fase Reflexo jogável de ponta a ponta". A Fase B
foi cortada em 03/09, então **esse critério não existe mais e nada foi escrito no lugar**.
Amanhã chega um marco sem definição de "pronto". Isso é decisão do JP e precisa sair hoje:
ou o ALPHA passa a significar "Fase A jogável de ponta a ponta", ou a data muda.

### Risco aberto há 7 dias: o trabalho da Julia continua sem backup

O último commit dela é de **02/09**. As 15 alterações de arte registradas em 03/09 —
incluindo a `Tela_1.unity` com **+1219/−337 linhas**, que é montagem de cena de verdade —
**continuam só no disco da máquina dela, sete dias depois**.

Esta equipe já perdeu o projeto inteiro uma vez. É o maior risco isolado do projeto hoje, e
o conserto é um Ctrl+S na Unity mais um commit.

### Letícia sem os assets — causa encontrada e provada

Relato de que ela não conseguia pegar os assets da Julia. **A Julia está correta**: os 3
commits dela trazem tudo, os 56 arquivos de LFS estão no servidor
(`git lfs push --dry-run` retorna zero pendentes) e o `.gitattributes` cobre `.blend` e `.fbx`.

**A causa é o ZIP.** O botão "Download ZIP" do GitHub não resolve LFS — empacota os
ponteiros de 131 bytes. Detalhe completo com a prova em [`ACHADOS.md`](ACHADOS.md).

É **reincidência**: em 03/09 já tínhamos descoberto que ela trabalhava por ZIP e criado um
clone na máquina dela. Ou o clone não pegou, ou ela voltou para o ZIP. Vale confirmar qual
dos dois antes de fechar.

### Trabalho preparado, ainda não entregue

- **Recado ao Luigi** com as 3 pendências: exportar o ursinho (o `Untitled.blend` é ele, o FBX
  nunca foi commitado), conferir se os prefabs do kit existem na máquina dele, e aplicar
  escala (`Ctrl+A > Apply Scale`) antes de todo export.
- **Instruções de conserto para a Letícia**: `git lfs install` + `clone` em pasta nova.

Os dois estão escritos mas **não entregues** — a sessão do JP não alcança as sessões de
Remote Control das outras máquinas. O canal correto é o Trello.

### Higiene de ambiente descoberta na máquina do JP

- A pasta de trabalho dele (`Downloads/KRLH funciona2 (3)`) **não é o repositório** — é uma
  terceira cópia solta do projeto Unity.
- Por estar dentro de `Downloads`, toda sessão carrega junto o `CLAUDE.md` do **O Vigia**
  (outro trabalho escolar): ~9,5 KB de regras erradas em todo turno.
- Existe um clone duplicado, `Documents/GitHub/fragmentofecartt` (dois T), parado em 01/09.

**Máquina de referência do JP:** Dell XPS 8960 · i7-14700 (20C/28T) · 32 GB DDR5 5600
(pente único, single-channel, 1 slot livre) · **RTX 4060 com 8 GB de VRAM**.
Registrado porque o alvo do BETA é "40+ fps no PC mais fraco do laboratório" — **esta
máquina não serve para validar isso**. As texturas superdimensionadas e o teclado de 74
draw calls não vão aparecer como problema aqui.

---

## 04 a 08/09/2026 — seis dias, só documentação entrou

Registrado porque o silêncio também é informação.

As provas terminaram em **04/09**. De 04 a 07/09 **nenhum commit entrou no repositório**.

Em **08/09** o JP empurrou dois commits, que são o trabalho escrito em 03/09 finalmente
versionado:

- `1b9776d` — `CLAUDE.md` (221 linhas) e a pasta `docs/` (416 linhas)
- `5c01bd2` — `Observado.cs`, 218 linhas: a detecção de campo de visão

**Nada de gameplay entrou.** Nenhum commit da Julia, nenhum da Letícia, nenhum asset do
Luigi. O `Observado.cs` **continua sem nunca ter compilado** — não há Unity na máquina do JP.
É código revisado, não testado, e ninguém abriu ele na Unity desde que foi escrito.

Somando: restam **13 dias de aula** até a FECART (26/09), o Sprint 3 não começou, e o único
código do sistema de visão que existe nunca rodou.

---

## 03/09/2026 (quinta)

### Documentação criada
Criados o `CLAUDE.md` na raiz do repositório e esta pasta `docs/`. Motivo: em um único dia
três máquinas diferentes abriram o projeto pela primeira vez e **nenhuma tinha contexto** —
cada sessão redescobriu as mesmas coisas do zero.

### Três máquinas conectadas
Julia, Letícia e Lucca passaram a rodar sessões de Claude Code ligadas à sessão do JP por
Remote Control. Isso permitiu inspecionar as três máquinas no mesmo dia.

### Push do trabalho preso na máquina da Julia
O commit `15602e7` ("Commit 12:45", de 02/09) existia só no disco dela e **nunca tinha sido
enviado**. Continha modelos da cama e da mesa, texturas, materiais e pôsteres. Empurrado.

Continuam sem commit na máquina dela 15 alterações de arte, incluindo a `Tela_1.unity` com
**+1219/−337 linhas** — montagem de cena de verdade. Ela precisa dar Ctrl+S na Unity e
commitar; não foi commitado por outra pessoa porque cena aberta e não salva vai pela metade.

### Descoberto: a Letícia nunca teve um clone
Ela vinha trabalhando de ZIPs baixados com "Download ZIP" do repositório antigo "Fecart",
extraídos em 05/08 e 10/08 — sem `.git`, sem LFS, com o projeto na raiz.

**Verificado por carimbo de data: não havia trabalho perdido ali.** O único arquivo criado
depois da extração foi um `NewMonoBehaviourScript.cs` de 309 bytes, template padrão vazio.
Clone novo criado na máquina dela, sincronizado, LFS íntegro, Unity 6000.3.6f1.

### Auditoria completa do projeto
Feita em duas máquinas de forma independente, mais um inventário dos `.blend` pelo Blender
CLI. Resultado em [`ACHADOS.md`](ACHADOS.md). Resumo: **a base do Git está limpa**; os
problemas estão nos assets 3D e na ausência de prefabs.

### Trello reorganizado
24 cards arquivados (Fase B, Hub, cutscene) e 14 datas movidas de sexta para quinta.
Motivo em [`DECISOES.md`](DECISOES.md).

### `Observado.cs` escrito
A detecção de campo de visão — a tarefa mais cara do projeto (13,3 h no PERT). Escrito para
a Julia não começar de página em branco na terça.

Faz três checagens, da mais barata para a mais cara: distância → campo de visão da câmera
(`TestPlanesAABB`) → `Linecast` para parede. Testa **cinco pontos** do objeto em vez de só o
centro, porque meio ursinho visível atrás de uma caixa precisa contar como visto — senão ele
anda na cara do jogador. Tem `intervaloDeChecagem` de 0,1 s para aguentar 40 ursinhos, e
expõe `TempoSemSerVisto`, que o card do teleporte vai precisar.

**Nunca compilou** — não há Unity na máquina do JP. É código revisado, não testado.

### Dois erros do próprio Claude, registrados de propósito
1. Afirmei que cama, cadeira, armário, mesa, lixeira e cobertor estavam no repositório.
   **Li os cards do Trello, não os arquivos.** Só existem cama, armário e mesa.
2. Afirmei que a "Prateleira + Porta-Retrato" era do quarto de brinquedos (3x). É do quarto
   real (1:1). Se o Lucca tivesse modelado, seria o trabalho inteiro perdido.

Nos dois casos quem pegou foi a sessão que foi olhar no disco. **Confira no Git, não no
Trello** — a regra vale para agentes também.

---

## 02/09/2026 (quarta)

Reunião N°15 registrada no Trello. Levantamento do estado real do projeto: Sprints 1 e 2
concluídos, **39 tarefas paradas em "Revisão do PO"** sem nunca chegarem em "Concluído", e o
Sprint 3 — o sistema de visão, o coração do jogo — **nunca iniciado**.

Descoberto que **não há aula na sexta**, e que todos os marcos do plano estavam marcados
para sexta.

---

## 01/09/2026 (terça)

Repositório criado no GitHub por `nonattoNNTT` (Julia): `Initial commit` às 15:23 e
`Adiciona projeto Unity e configura Git LFS` às 15:36.

O card "Git + Repositório" estava marcado como concluído desde 04/08 — quatro semanas antes
do repositório existir.

**Divergência registrada:** o `PlayerMovement.cs` commitado usa `CharacterController` e é
idêntico byte a byte a uma cópia antiga solta em `Downloads`. O card "Movimento e Câmera"
descreve `Rigidbody + Capsule Collider, velocity no FixedUpdate`. O trabalho de Rigidbody da
Julia **não está no repositório**.
