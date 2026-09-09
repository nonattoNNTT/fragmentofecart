# Decisões

Ordem: a mais recente em cima. Cada decisão registra **quem decidiu** e **por quê** — o
motivo é a parte que importa, porque é o que impede alguém de desfazer a decisão daqui a
duas semanas sem saber o que estava em jogo.

---

## 09/09/2026 — Apagar o `Observado.cs`

**Decidiu:** JP

O `Observado.cs` foi removido do repositório. O JP avaliou que não valia usar.

**Estado dele quando foi apagado:** 218 linhas, nunca importado por nenhuma Unity (não
existia `.meta` em lugar nenhum), não estava anexado a nenhum objeto e nenhum outro script
o chamava. Era código morto no repositório — apagar não removeu nada de dentro do jogo.

**Isto reverte parcialmente a decisão de 03/09** ("Manter o plano do sistema de visão"),
cujo argumento era que a Julia começaria a detecção com o arquivo pronto em vez de partir
de página em branco. Sem ele, a página em branco voltou — e a detecção de campo de visão
é a tarefa mais cara do projeto (13,3 h no PERT).

**Recuperável a qualquer momento**, o arquivo está no histórico:

```
git show 5c01bd2:"KRLH funciona2/Assets/Manager/ObjetosScripts/Observado.cs" > Observado.cs
```

**Em aberto, e é do JP:** a detecção de campo de visão continua no plano para a Fase A, ou
o projeto cai para a versão simples — teleporte por tempo, sem checar visão — que o plano
de produção mantém disponível desde 04/08? A resposta muda o que a Julia faz amanhã.

---

## 03/09/2026 — Manter o plano do sistema de visão, sem cortar para a versão simples

**Decidiu:** JP

A detecção de campo de visão não tinha começado (nenhum `.cs` tocado desde 17/08) e havia
duas saídas: manter o plano, ou cortar para a versão simples — teleporte por tempo, sem
checar visão.

**O JP escolheu manter.** A Julia começa a detecção na semana de 08/09, com o `Observado.cs`
já escrito para não partir de página em branco.

**O que isso implica:** a semana de 08 a 10/09 fica com detecção + teleporte + Ursão +
coletáveis + menu. É a semana mais apertada do projeto. Se travar de novo, a versão simples
continua disponível — está descrita no plano de produção desde 04/08.

---

## 03/09/2026 — Blender 5.2 é a versão oficial

**Decidiu:** forçado pelos arquivos, confirmado na prática

Todos os 5 `.blend` do repositório foram salvos no **Blender 5.2.44**, e o Blender não salva
para versão anterior. O 4.3 retorna *"Cannot read blend file, incomplete header, may be from
a newer version"* em todos eles.

**Não existe a opção "todos no 4.3".** Ou a equipe sobe para o 5.2, ou os assets são refeitos.

**Efeito colateral:** sessões de Claude com Blender 4.3 não conseguem abrir nenhum arquivo da
equipe. Inspeção de `.blend` depende de uma máquina com 5.2.

---

## 03/09/2026 — Dois cenários, duas escalas

**Decidiu:** já estava no plano de produção; explicitado aqui porque estava causando erro

- **Quarto real (Cap. 1): escala 1:1.** Cama 0,90 × 1,90 m, armário 2,0–2,2 m, mesa 0,75 m.
- **Quarto de brinquedos (Fase A): escala 3x.** O jogador é pequeno; modelar grande disfarça
  a falta de detalhe e deixa a fase assustadora de graça.

**Por que precisou virar decisão escrita:** no Trello os dois cenários estão misturados na
mesma lista do Lucca, sem marcação. Um Claude leu o quadro e disse que a "Prateleira +
Porta-Retrato" era da Fase A (3x) quando é do quarto real (1:1). A diferença é 1,0 m contra
3,0 m — o modelo inteiro perdido.

**Como distinguir:** pela data. Sprint 2 (10–14/08) é quarto real. Sprint 3 (17–21/08) é Fase A.

---

## 03/09/2026 — O cobertor fica

**Decidiu:** JP

O JP tinha mandado cortar a interação com o cobertor, deixando só o ursinho. Mas a Letícia
marcou o card como concluído na mesma manhã, antes da decisão chegar.

**Cortar deixou de economizar tempo** — o tempo já tinha sido gasto. Só destruiria trabalho.
Mantido.

---

## 03/09/2026 — Marcos fecham na quinta, não na sexta

**Decidiu:** JP, a partir de uma correção de fato

O plano original marcava todos os marcos para sexta-feira: Prototype 14/08, Vertical Slice
28/08, Alpha 11/09, Beta 18/09. **Mas a equipe não tem aula na sexta** — o horário é segunda,
terça, quarta e quinta.

Quatro meses de marcos marcados para um dia em que ninguém trabalha.

**Novas datas:** ALPHA 10/09 · BETA 17/09 · GOLD 24/09 · FECART 26/09 (sábado).
Restam **13 dias de aula**, não 17.

---

## 03/09/2026 — Fase B, Hub e cutscene das correntes cortados

**Decidiu:** JP

A conta não fechava: ~150 tarefas restantes para 13 dias de aula, o que dá ~32 h por pessoa
contra ~44 h de backlog só na programação.

Aplicados os cortes de emergência na ordem que o próprio plano de produção definiu em 04/08:

| Corte | Nº na lista | O que sai |
|---|---|---|
| **Fase B "Reflexo"** | 6º | Labirinto de espelhos inteiro: monstro, espelhos quebráveis, portas, Manequim, kit do labirinto, transição ilustrada |
| **Hub** | 3º | O Cap. 1 liga direto na Fase A |
| **Cutscene das correntes** | 7º | Já era 4 segundos; sai inteira |

**24 cards arquivados no Trello** (arquivados, não deletados — se a Fase B voltar depois da
feira, está tudo lá).

**Por que a Fase B e não outra coisa:** o checkpoint de 28/08 do plano já mandava cancelá-la
se faltassem 3 ou mais itens da Vertical Slice. Faltavam todos. E a Reunião N°13 da equipe já
tinha combinado que "chegar até o sprint 4 é o MÍNIMO".

**Nunca cortar, em hipótese nenhuma:** som, menu, tela de game over, build testada em outra
máquina. São essas quatro coisas que fazem o jogo parecer profissional na feira.
