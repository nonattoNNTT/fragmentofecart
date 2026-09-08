# Diário do projeto

Ordem: o mais recente em cima. Cada entrada registra o que mudou e o que alguém precisa
fazer por causa disso.

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
