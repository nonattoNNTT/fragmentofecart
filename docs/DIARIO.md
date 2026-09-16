# Diário do projeto

Ordem: o mais recente em cima. Cada entrada registra o que mudou e o que alguém precisa
fazer por causa disso.

---

## 16/09/2026 (quarta) — véspera do BETA

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
