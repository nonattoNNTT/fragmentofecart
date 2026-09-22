# Diário do projeto

Ordem: o mais recente em cima. Cada entrada registra o que mudou e o que alguém precisa
fazer por causa disso.

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
