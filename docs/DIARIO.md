# Diário do projeto

Ordem: o mais recente em cima. Cada entrada registra o que mudou e o que alguém precisa
fazer por causa disso.

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
