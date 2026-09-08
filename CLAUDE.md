# Fragmento / Valentina — contexto do projeto

> Este arquivo fica na raiz do repositório. Qualquer sessão de Claude Code aberta aqui
> lê ele automaticamente. **Se você é um Claude começando agora, leia isto inteiro
> antes de tocar em qualquer coisa** — e depois leia `docs/ACHADOS.md`, que lista os
> problemas conhecidos que ainda não foram resolvidos.

---

## 1. O que é este projeto

**Valentina** — jogo de terror psicológico em primeira pessoa, feito por 6 alunos para a
**FECART**, com apresentação ao vivo em **26/09/2026**. O board do Trello se chama
"Fragmento", o produto se chama Valentina, e a pasta do projeto Unity ainda se chama
`KRLH funciona2` por herança.

O tema é pressão social: a Valentina enfrenta os monstros que ela mesma criou sobre si.

**Regra número um: o escopo não cresce.** Ele já foi cortado duas vezes. Se algo novo
entra, algo de peso igual sai — trocar, nunca somar. Só o JP corta escopo.

### O que restou do jogo

| | |
|---|---|
| **Capítulo 1** | Quarto real da Valentina, escala 1:1. Pega o cobertor, apaga a luz, dorme. |
| **Fase A — "Os Ursinhos"** | Quarto de brinquedos em escala 3x (o jogador é pequeno). Dezenas de ursinhos idênticos que **congelam quando olhados e teleportam quando não**. Chefe: o Ursão. Lanterna com bateria. Achar 3 ursinhos verdadeiros e levar à cama. |

**Cortado em 03/09/2026 e não se rediscute:** Fase B "Reflexo" (labirinto de espelhos),
o Hub, a cutscene das correntes. Também fora: save game, reflexo real em espelho,
animação facial, multi-idioma.

---

## 2. Onde as coisas ficam

O projeto Unity está na **subpasta** `KRLH funciona2/`, não na raiz do repositório.

```
fragmentofecart/
  CLAUDE.md              <- este arquivo
  docs/                  <- documentação do processo (ver docs/README.md)
  KRLH funciona2/
    Assets/
      Art/               <- modelos, texturas, UI
      Editor/            <- Editor Scripts com [MenuItem]
      Manager/
        Artscripts/      <- efeitos visuais de runtime
        ObjetosScripts/  <- interação
        Player e Câmera/ <- movimento, câmera, stamina
        Scenes/          <- Tela_inicial, Tela_1, Tela_2
        Settings/        <- perfis URP e VolumeProfile
      _Recovery/         <- cenas recuperadas de um acidente. Não apagar sem perguntar.
    ProjectSettings/
```

**Clones conhecidos** (o caminho muda em cada máquina — não presuma):

| Máquina | Caminho |
|---|---|
| Julia | `C:\Users\25011973\Downloads\fragmentofecart` |
| Letícia | `C:\Users\24011766\Documents\GitHub\fragmentofecart` |
| Lucca | `C:\Users\25012025\Documents\fragmentofecart` |
| JP | `C:\Users\25011938\Documents\GitHub\fragmentofecart` |

---

## 3. Stack

- **Unity 6000.3.6f1** — a versão exata importa, está em `ProjectSettings/ProjectVersion.txt`
- **URP 17.3.0** · **Input System novo 1.18.0** · **TextMesh Pro**
- **Blender 5.2** — todos os `.blend` do repo foram salvos nela, e o Blender **não salva
  para versão anterior**. Quem estiver no 4.3 não abre nenhum arquivo da equipe.
- Build: **Windows `.exe`**. Alvo: **40+ fps no PC mais fraco do laboratório**.
- Git + **Git LFS** (56 objetos). Serialização em **Force Text** — é isso que permite
  duas pessoas trabalharem sem corromper cena.

---

## 4. Restrições que não podem ser violadas

Vieram do plano de produção e existem para o projeto caber no prazo:

- **Os ursinhos nunca andam.** Teleportam quando não observados. Isso elimina pathfinding,
  NavMesh e animação de caminhada para dezenas de inimigos.
- **O Ursão anda sem NavMesh** — direção até o jogador + `Raycast` para desviar de parede.
- **Iluminação baked + cenário `Static`.** Sombra em tempo real só nas 2 ou 3 luzes principais.
- **Low poly de verdade:** ursinho < 800 tris · Ursão < 3.000 tris.
- **1 unidade do Blender = 1 metro.** Grade de **0,5 m**. Ver a seção 6, escala.
- **Um modelo, muitos inimigos.** O ursinho é modelado uma vez e vira 40 com cor e escala.
  Texturas num **trim sheet único**.
- **Paleta fechada, nenhuma cor fora dela:**
  `#020106` · `#002046` · `#283A4E` · `#D74143` · `#643847`
- **Uma fonte só**, com acentuação em português testada.
- **PNG com fundo transparente, no dobro do tamanho de exibição.**

---

## 5. Quem faz o quê

| Pessoa | Área |
|---|---|
| **Julia** | Programação — sistemas e monstros |
| **Letícia** | Programação — interação, interface e fluxo |
| **Luigi** | 3D — personagens (Ursão), quarto da Valentina, kit modular |
| **Lucca** | 3D — móveis, props, texturas, otimização |
| **Lucca Vigna** | Arte 2D e interface |
| **João Pedro (JP)** | Gestão (PO), level design e som |

### 🔒 Só a Julia e a Letícia abrem a Unity

Todo o resto da equipe — inclusive o JP — entrega **arquivos** (`.fbx`, `.png`, `.wav`,
plantas em papel). Ninguém mais importa ou monta nada dentro do editor.

Consequência prática: **toda tarefa que acontece dentro da Unity vai para a Julia ou a
Letícia**. Para mexer em cena, escreva um Editor Script em `Assets/Editor/` com
`[MenuItem]` e peça para uma delas clicar no menu.

---

## 6. Escala — a causa de metade dos problemas atuais

**São dois cenários com escalas diferentes.** Confundir os dois custa o modelo inteiro.

| Cenário | Escala | O que vive nele |
|---|---|---|
| **Quarto real (Cap. 1)** | **1:1** | Cama 0,90 × 1,90 m · armário 2,0–2,2 m · mesa 0,75 m · porta 0,90 × 2,05 m · prateleira 1,0–1,3 m |
| **Quarto de brinquedos (Fase A)** | **3x** | Caixas de brinquedo, blocos gigantes, prateleira gigante, cavalinho |

No quadro do Trello os dois estão misturados na mesma lista, sem marcação. As datas separam:
Sprint 2 (10–14/08) é quarto real; Sprint 3 (17–21/08) é Fase A.

### Antes de exportar do Blender, sempre

1. **`Ctrl+A > Apply Scale`** — é a causa raiz das escalas malucas e das normais invertidas
2. Conferir a dimensão em metros no painel N
3. Nome descritivo, **sem acento**, por objeto — nada de `Cube.001`
4. Todo objeto com material atribuído
5. Export FBX com `apply_unit_scale=True`, `bake_space_transform=True`,
   `axis_forward='-Z'`, `axis_up='Y'`

---

## 7. Convenções de código

Siga o que já existe. Não introduza padrão novo.

- **Classes e campos em inglês** (`PlayerMovement`, `walkSpeed`), **comentários em
  português com acento**.
- Campos de inspetor são `public`, agrupados com `[Header("...")]`.
  O projeto **não usa** `[SerializeField]` privado nem `namespace`.
- Blocos de seção dentro dos scripts:
  ```csharp
  // =========================================================
  // VERIFICAR OBJETO
  // =========================================================
  ```
- Input **sempre** por callback do Input System:
  `public void OnMove(InputAction.CallbackContext context)`
- Checar null antes de usar referência.

### Nunca editar à mão

`.unity`, `.prefab`, `.asset`, `.mat` são YAML com GUID interno. Editar à mão corrompe
referência em silêncio — o objeto vira "Missing" horas depois, longe da causa.

### Script novo precisa do `.meta`

Quem gera o `.meta` é a Unity. Se um `.cs` for commitado sozinho, cada máquina gera um
GUID diferente e a referência quebra para alguém. **Commite sempre o `.cs` junto com o
`.meta` que a Unity gerou.**

---

## 8. A armadilha desta equipe

**Cards marcados como concluídos cujo trabalho nunca foi commitado.** Já aconteceu com o
próprio repositório, com a classe base de interação, com a interação do cobertor, com os
prefabs do kit modular e com o export do ursinho.

**Antes de assumir que algo existe, confira no Git, não no Trello.** Isso vale para
pessoas e para agentes — o histórico em `docs/DIARIO.md` registra duas vezes em que o
próprio Claude errou por ler a lista em vez do arquivo.

Regra prática: **trabalho que não está no Git não existe.** Termine cada bloco de aula com
commit e push, mesmo pela metade.

---

## 9. O que o agente pode fazer sozinho

- Criar e editar scripts em `Assets/Manager/` e `Assets/Editor/`
- Escrever Editor Scripts com `[MenuItem]` para gerar cenário, prefabs, materiais e luzes
- Corrigir erros de compilação a partir do log colado
- Ajustar valores, refatorar, comentar e documentar
- **Registrar o que fez em `docs/DIARIO.md`** — ver `docs/README.md`

## 10. O que o agente NÃO pode fazer

- Instalar Unity, pacotes, Blender ou Git
- Mexer em Player Settings, Quality Settings ou Build Settings
- Gerar a build `.exe`, testar em outro PC, gravar pendrive
- Arrastar referências no Inspector (a menos que faça por Editor Script)
- Julgar se o jogo dá medo, se o texto está legível ou se a fase está divertida
- **Decidir corte de escopo — isso é só do JP**

Quando a tarefa cair nesta lista, pare e escreva o passo a passo em português, com o
caminho exato dos menus, endereçado à **Julia ou à Letícia** se acontecer dentro da Unity.

---

## 11. Como o JP quer trabalhar

- Responder em **português do Brasil**
- Ir direto à execução; explicação curta antes, detalhe depois se for pedido
- Quando chegar informação nova, **reescrever o plano** em vez de remendar o antigo
- Quando houver ambiguidade real, **perguntar antes de assumir**
- Preferir uma entrega funcional hoje a uma entrega elegante amanhã

> **Para colar na parede:** uma fase boa, com som e menu bonitos, testada em outro PC.
> Nada além disso.
