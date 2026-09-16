# Registro de uso de IA neste projeto

Este arquivo existe por dois motivos:

1. O JP pediu, em 09/09/2026, que tudo que ele solicita ao Claude e que afeta o projeto
   fique registrado em arquivo, não só na conversa.
2. O **Guia para o Uso Responsável de IA na FECAP** (v2.0, Art. 6, §único) torna
   **obrigatória** a Declaração de Uso de IA em projetos, contendo ferramenta, versão do
   modelo, finalidade, **prompts principais** e **trechos afetados**. Este arquivo é a
   matéria-prima dessa declaração. A regra completa está na seção 11 do `CLAUDE.md`.

Ordem: cronológica. Cada entrada registra **o que o JP pediu**, **o que a IA fez** e **o que
foi afetado**. O que a IA *não* fez também está aqui, porque a declaração precisa ser exata.

---

## Ferramenta

| | |
|---|---|
| Ferramenta | Claude Code (aplicativo desktop da Anthropic, aba Code) |
| Modelo | **Claude Opus 5** — identificador `claude-opus-5` |
| Operador | JP (João Pedro), na máquina dele, autenticado como `jpkbm2010@gmail.com` |
| Autoria dos commits | `Yshinu <jpkbm2010@gmail.com>` (o JP), com a linha `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>` |
| Período coberto | 08/09/2026 a 16/09/2026, uma única sessão contínua |

**Atenção:** outras sessões de Claude Code rodam nas máquinas da Julia e da Letícia, e o JP
tem uma segunda sessão. Os commits `7724485` (01/09), `1b9776d` e `5c01bd2` (08/09, sessão
do JP) e `0220361` (08/09, sessão da Julia) também carregam a tag de coautoria do Claude,
mas **não foram feitos pela sessão registrada aqui**. A Declaração de Uso de IA da entrega
precisa cobrir todas as sessões — este arquivo cobre uma.

---

## Commits feitos por esta sessão

Exatamente quatro. Todos na branch `main`, todos com push para `origin`.

| Commit | Data | O que |
|---|---|---|
| `5a6e16d` | 09/09 11:24 | `docs/ACHADOS.md` (diagnóstico do LFS) e `docs/DIARIO.md` (relatório de 04 a 09/09) |
| `06353b1` | 09/09 11:34 | `CLAUDE.md` — seção 11, regras da FECAP para uso de IA |
| `a3dcd24` | 09/09 11:40 | Remove `Observado.cs`; entrada em `docs/DECISOES.md` |
| `1997089` | 10/09 16:29 | Remove `Observado.cs.meta` órfão; entrada em `docs/DIARIO.md` |

**Nenhum código de jogo foi escrito por esta sessão.** O único `.cs` tocado foi o
`Observado.cs`, que foi *apagado* por decisão do JP — e ele tinha sido escrito por outra
sessão, no commit `5c01bd2`.

---

## Registro cronológico

### 08/09/2026 (terça)

**Pedido:** *"Leia o CLAUDE.md e a pasta docs/ deste repositório. Depois rode ListAgents: o
PC do Luigi deve estar online. Manda uma mensagem para ele com os três itens pendentes."*

**Feito:** leitura completa de `CLAUDE.md`, `docs/README.md`, `docs/ACHADOS.md`,
`docs/DECISOES.md`. O `ListAgents` confirmou o PC do Luigi online.
**Não feito:** a mensagem **não foi entregue** — a sessão não tem ferramenta que alcance
sessões de Remote Control em outras máquinas. O texto foi escrito em arquivo temporário e
mais tarde virou card no Trello.

**Pedido:** *"a Letícia não conseguiu pegar os assets da Julia, tem algo de errado com o
negócio dela? pede pro modelo dela verificar."*

**Feito:** investigação no repositório local. Verificado que os 3 commits da Julia trazem
todos os assets, que `git lfs push origin main --dry-run` retorna zero objetos pendentes e
que o `.gitattributes` cobre `.blend` e `.fbx`. **Conclusão: o lado da Julia está correto.**
Descoberto um clone duplicado (`Documents/GitHub/fragmentofecartt`) na máquina do JP.
Entrada 🟡 escrita em `docs/ACHADOS.md`.

**Pedido:** *"ela baixou o zip, com ele não dá certo?"*

**Feito:** prova por `git cat-file`: o blob do `Untitled.blend` tem 131 bytes — é o ponteiro
do LFS, e é isso que o botão "Download ZIP" do GitHub empacota. 56 arquivos e 13,6 MB de arte
não vêm no ZIP. Entrada do `ACHADOS.md` reescrita com a causa confirmada.

**Pedido:** *"procura os parâmetros dessa máquina, memória RAM, memória de vídeo."*

**Feito:** consulta por WMI e registro do Windows. Dell XPS 8960, i7-14700, 32 GB DDR5 em
pente único (single-channel), RTX 4060 com 8 GB de VRAM. Registrado no `DIARIO.md` de 09/09
como máquina de referência — com o aviso de que ela **não serve** para validar o alvo de
40 fps no PC mais fraco do laboratório.

### 09/09/2026 (quarta)

**Pedido:** *"tem alguma forma mais fácil de administrar todos os PCs sem a necessidade de uma
conversa pra cada? eu perco muitos tokens com isso."*

**Feito:** diagnóstico. A sessão do JP roda em `Downloads/KRLH funciona2 (3)`, que não é o
repositório, e por estar dentro de `Downloads` carrega junto o `CLAUDE.md` de outro trabalho
("O Vigia") — 9.492 bytes de regras erradas em todo turno. Existem três cópias do projeto na
máquina. Proposto usar Trello e `docs/` como canal em vez de conversa.
**Não feito:** a sessão não foi movida de pasta, nenhum arquivo foi apagado.

**Pedido:** *"é que tá tudo na minha sessão né."*

**Feito:** descoberto que a memória do Claude é chaveada pelo caminho da pasta. As 12
memórias do projeto foram **copiadas** (não movidas) para a chave do repositório, para
sobreviverem a uma mudança de pasta.

**Pedido:** *"eu esqueci de te mandar fazer o relatório desses últimos dias, você tá fazendo?"*

**Feito:** respondido que não. Lido o `docs/DIARIO.md` (parava em 03/09). Escritas as
entradas de **09/09** e **04 a 08/09**: o ALPHA de 10/09 estava sem critério de saída (a
Fase B foi cortada e nada foi escrito no lugar), e o trabalho da Julia estava sem backup
desde 02/09. *Esta segunda afirmação era verdadeira na hora e foi corrigida por acréscimo em
10/09 — ver abaixo.*

**Pedido:** *"faz isso pra mim, e não esquece que eu preciso que você documente tudo que eu
pedi e estou falando pra você que afete o projeto diretamente."*

**Feito:** commit `5a6e16d` e push. Quatro cards criados no Trello (board Fragmento, lista
"A Fazer"): Julia (commitar), JP (definir o ALPHA), Letícia (clone com LFS), Luigi (ursinho,
prefabs, escala). A instrução de documentar tudo foi gravada como memória permanente.

**Pedido:** *"tem 3 pessoas commitando em PCs diferentes, isso dá problema?"*

**Feito:** análise só. `.gitattributes` está correto, mas o UnityYAMLMerge não está
configurado em nenhuma máquina, não existe nenhum `.prefab` e a `Tela_1.unity` tinha 296 KB.
**Não documentado** em arquivo — ficou na conversa, contra a própria regra deste registro.

**Pedido:** *"guarda na sua memória, pra projetos escolares precisa seguir isso:
[link do Guia da FECAP]."*

**Feito:** PDF baixado e texto extraído com `pdftotext`. Gravado como memória. Como é regra
que afeta a entrega, virou a **seção 11 do `CLAUDE.md`** — commit `06353b1` e push.

**Pedido:** *"não estamos usando o Observado.cs eu acho."*

**Feito:** verificado. Sem `.meta` em lugar nenhum, não anexado a nenhum objeto, não chamado
por nenhum script. Código morto no repositório.

**Pedido:** *"acho que nem vale usar ele, só apagar mesmo."*

**Feito:** `git rm` do `Observado.cs`. Entrada em `docs/DECISOES.md` com o comando de
recuperação e a pergunta em aberto (a detecção de campo de visão continua no plano ou cai
para a versão simples?). Commit `a3dcd24` e push.

### 10/09/2026 (quinta) — dia do ALPHA

**Pedido:** *"vê o último commit, vê se algo mudou."*

**Feito:** `git fetch`. Cinco commits novos da Julia, push em 09/09 às 12:48 — cerca de uma
hora depois do card entrar no Trello. Verificado que o GUID do `Observado` aparecia 0 vezes na
`Tela_1.unity`: a remoção do arquivo não quebrou nada. Sobrou um `Observado.cs.meta` órfão,
gerado pela Unity da Julia antes da remoção.

**Pedido:** *"coloca em dia."*

**Feito:** `git pull`. `git rm` do `.meta` órfão. Entrada de 10/09 no `DIARIO.md`
registrando a entrega da Julia e corrigindo por acréscimo a afirmação de 09/09. Commit
`1997089` e push. Comentário e conclusão no card da Julia no Trello.

### 16/09/2026 (terça)

**Pedido:** *"você tem aquele relatório que eu pedi com as suas ações até então?"*

**Feito:** este arquivo.

---

## Ações fora do repositório

| Onde | O quê |
|---|---|
| Trello, board Fragmento | 4 cards criados em "A Fazer" (09/09). Comentário e conclusão no card da Julia (10/09). |
| Memória do Claude (máquina do JP) | 2 memórias novas: a instrução de documentar tudo, e o Guia da FECAP. 12 memórias existentes copiadas para a chave do repositório. |
| Arquivos temporários | Mensagem ao Luigi e instruções à Letícia, no scratchpad da sessão. Conteúdo migrado para os cards; os arquivos somem com a sessão. |

## O que esta sessão NÃO fez

- Não escreveu nem alterou nenhum código de jogo.
- Não abriu Unity nem Blender.
- Não entregou mensagem a nenhuma sessão remota — não há ferramenta para isso.
- Não moveu a sessão de pasta, não apagou `Downloads/CLAUDE.md`, a cópia em `Downloads`
  nem o clone `fragmentofecartt`. Foram apontados como pendentes de decisão do JP.
- Não decidiu nada de escopo. As decisões (apagar o `Observado.cs`, o que o ALPHA significa)
  foram e continuam sendo do JP.

## Arquivos afetados por esta sessão

```
CLAUDE.md                                              (seção 11 adicionada)
docs/ACHADOS.md                                        (1 entrada nova)
docs/DECISOES.md                                       (1 entrada nova)
docs/DIARIO.md                                         (3 entradas novas)
docs/USO-DE-IA.md                                      (este arquivo)
KRLH funciona2/Assets/Manager/ObjetosScripts/Observado.cs       (removido)
KRLH funciona2/Assets/Manager/ObjetosScripts/Observado.cs.meta  (removido)
```

## Prompts principais

Os pedidos do JP estão citados textualmente nas entradas acima. Em resumo, a IA foi usada
para: **ler e auditar o repositório**, **diagnosticar problemas de Git e LFS**, **escrever a
documentação de processo** (`docs/`), **redigir cards de tarefa**, e **executar remoções e
commits a pedido**. Não foi usada para produzir conteúdo do jogo.

---

## Sessão 2 — 16/09/2026 (quarta, tarde): NavMesh e Monstro

Sessão **diferente** da registrada acima (aberta sem pasta e movida para o repositório).
Mesma ferramenta, mesmo modelo (`claude-opus-5`), mesmo operador (JP).

**Pedido:** *"Pega a ultima versão do github do meu projeto da Fecart 'Fragmento' e faz o
NavMesh completo dentro da cena SampleScene"* e, em seguida, *"preciso que você crie um cubo
dentro da cena, que futuramente vai ser o monstro, segue a IA que deveria ser aplicavel
dentro dele"* (anexo `MonsterAI.cs`, escrito fora desta sessão).

**Feito:** `git pull` (HEAD `6abe5e9`), abertura da Unity 6000.3.6f1 pelo MCP, escrita do
Editor Script, execução dos menus, bake, validação e teste em Play Mode. Detalhes e números
na entrada de 16/09 do `DIARIO.md`.

**Esta sessão escreveu código de jogo.** Arquivos afetados:

```
KRLH funciona2/Assets/Editor/MontadorNavMesh.cs                       (novo — escrito pela IA)
KRLH funciona2/Assets/Scripts/Monstro/MonsterAI.cs                    (novo — enviado pelo JP, copiado sem alteração)
KRLH funciona2/Assets/Scenes/SampleScene.unity                        (alterado pelo Editor Script, não à mão)
KRLH funciona2/Assets/Scenes/SampleScene/NavMesh-SampleScene.asset    (gerado pelo bake)
KRLH funciona2/Assets/Art/Monstro/M_Monstro.mat                       (gerado pelo Editor Script)
docs/DIARIO.md · docs/USO-DE-IA.md                                    (entradas novas)
```

