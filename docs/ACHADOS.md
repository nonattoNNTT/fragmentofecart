# Achados

Problemas encontrados nas auditorias, com status. **Resolvido não vira apagado** — saber que
algo já foi problema evita repetir.

Status: 🔴 aberto · 🟡 em andamento · 🟢 resolvido

Auditoria de 03/09/2026, feita em duas máquinas (Julia e JP) de forma independente, e um
inventário dos `.blend` feito na máquina do Lucca. Tudo read-only, nada foi modificado.

---

## 🔴 Zero prefabs no projeto inteiro

Não existe **nenhum** arquivo `.prefab` versionado. Tudo mora dentro das cenas, e a
`Tela_1.unity` tem 316 KB de YAML.

**O que acontece se ninguém mexer:** com a Julia e a Letícia trabalhando ao mesmo tempo, as
duas editando aquela cena, o conflito de merge é irresolvível na prática. Esta equipe já
perdeu o projeto inteiro uma vez.

Extrair Player, UI e móveis do quarto para prefab é o que permite duas pessoas trabalharem
no mesmo dia.

**Observação:** o card "Snap do Kit + Prefabs" do Luigi está marcado como concluído desde
06/08. Os prefabs não estão no repositório, nem nos ZIPs da Letícia, nem na máquina do Lucca.
Se existem, é na máquina do Luigi — que ainda não foi verificada.

---

## 🔴 Escalas inconsistentes entre os móveis do Cap. 1

Medidas reais dos arquivos:

| Modelo | Tamanho atual | Deveria ser | Fator |
|---|---|---|---|
| Cama | 12,83 × 9,30 m | 1,90 × 0,90 m | ~6,4x |
| Parede | 10,45 m de altura | 2,70 m | ~3,9x |
| Armário | 6,74 m | 2,20 m | ~3,1x |
| Mesa2 | 1,38 m | 1,38 m | ✅ 1x |

Não é "está tudo 3x maior" — **cada móvel está numa escala diferente**. A mesa em escala real
ao lado de uma cama de 12 m vai parecer móvel de casa de boneca.

**Reescalar vs refazer, medido arquivo por arquivo:**

- `cama.blend` — escala não uniforme e **não aplicada** (`6,4165 / 4,8179 / 0,2334`). Três
  fatores diferentes nos três eixos. **Não existe um número único que conserte**; reescalar
  por qualquer fator deforma. Tende a refazer.
- `Armário3 1.blend` — escala `1,0 / 1,0 / 1,0` nos dois objetos. Geometria limpa.
  **Provavelmente dá para reescalar sem refazer.**

**Causa raiz:** escala não aplicada no Blender antes de exportar. `Ctrl+A > Apply Scale`
resolve na origem — ver `CLAUDE.md`, seção 6.

---

## 🔴 Defeitos no `untitled 1.fbx` (o kit de parede, 19 objetos)

- **4 objetos com escala negativa** (`Cube`, `Cube.004`, `Cube.005`, `Cube.007`) — geometria
  espelhada, o que vira **normal invertida** na Unity: face preta ou invisível.
- **Escalas de 0,0009 a 0,0641** em todos — o export saiu sem `apply_unit_scale` e sem
  `bake_space_transform`.
- **9 dos 19 objetos sem material nenhum.**
- `Cube.008` com dimensão Y = 0,0 — objeto degenerado, espessura zero.
- `Cube.011` a `Cube.016` — 6 cópias idênticas sem material.
- **Todos os 19 objetos se chamam `Cube.NNN`.** É literalmente o motivo de não ser possível
  confirmar se a cadeira e a lixeira existem no projeto.

---

## 🔴 Dois arquivos de Input System no mesmo projeto

`Assets/InputSystem_Actions.inputactions` (o template padrão da Unity) e
`Assets/Manager/Player e Câmera/PlayerInputActions.inputactions` (o de verdade).

**O que acontece se ninguém mexer:** é causa clássica de "o input simplesmente não responde"
— e consome uma tarde inteira, porque ninguém suspeita do arquivo, suspeita do código.

Vale resolver **antes** de começar o Sprint 3, não depois.

---

## 🔴 `ConfigurarMaterialesMadeira.cs` aponta para um FBX que não existe

Linha 16: `CAMINHO_FBX = "Assets/Art/Modelos quarto/modelos_madeira.fbx"`.

Esse arquivo **existe numa cópia antiga do projeto na máquina do JP**, mas `git log --all`
volta vazio: nunca entrou no repositório. Não é caminho digitado errado — é asset que ficou
de fora do commit.

Quem abrir aquele menu e clicar toma erro. O script trata com dialog e não crasha, mas é
armadilha. Não quebra a build (é Editor Script).

---

## 🔴 O ursinho está modelado e nunca foi exportado

`Untitled.blend` **é o ursinho** — 13 objetos, textura própria, focinho, ~2,22 m de altura.
Modelo de verdade, não resto.

Mas o único `.fbx` do repositório inteiro é o `untitled 1.fbx`, que é o kit de parede.
**Não existe FBX de ursinho em lugar nenhum.**

O card do Luigi diz "Export do Ursinho + Conferência de Escala", concluído em 13/08.

**Isso é meia hora de trabalho, não um dia** — e destrava a Fase A inteira, que é a única
fase que sobrou no jogo.

---

## 🟡 Um computador com teclado dentro do repositório

`Untitled 1.blend` são **78 objetos**: monitor, base e **74 teclas separadas**, cada uma um
objeto de 12 tris, com texturas "PC" e "TECAFO".

Não é prop de quarto de brinquedos nem do quarto real. Ou é de outra cena, ou é herança do
repositório antigo. **Se entrar no jogo assim, são 74 draw calls por um teclado.**

Aguardando decisão do JP: fica ou sai.

---

## 🟡 Texturas de UI superdimensionadas

- `números/*.png` a **1920 × 1920** — para dígitos
- `Staminabar/*.png` a **2000 × 2000**
- `fragmento.png` a 2172 × 724
- `chão textura1.png` — maior asset único, **3,6 MB** a 2048 × 2048

E **todos os `.meta` estão no `maxTextureSize: 2048` padrão**, ou seja, nada é reduzido na
importação.

Com alvo de 40+ fps no PC mais fraco do laboratório, baixar os números e a stamina para 512
é o ganho de memória mais barato que existe aqui.

---

## 🟡 Cinco GIFs de tecla sem nenhuma referência

`teclas/C/C.gif`, `teclas/Click direito.gif`, `teclas/Espaço/Espaço.gif`,
`teclas/Click esquerdo/Click esquerdo12.png` e `Click esquerdo22.png`.

**Não presuma que é sobra.** Como existe o `ControlsPanel.cs`, é mais provável que sejam
teclas que **esqueceram de ligar no painel de controles** do que lixo para apagar. Vale a
Letícia olhar antes de qualquer limpeza.

---

## 🟡 Restos com nome provisório

`Untitled.blend` (é o ursinho), `Untitled 1.blend` (é o computador), `untitled 1.fbx` (é o
kit de parede), `Untitled.003.png`, e um `Patinho.blend` de 730 KB solto na raiz de `Assets/`.

**Cuidado ao limpar:** `Untitled.blend` e `Patinho.blend` são referenciados pela `Tela_1`,
não só pelas cenas de `_Recovery`. Apagar achando que só o recovery usa quebra a cena principal.

Renomear é seguro pela Unity (ela atualiza as referências), mas **não é seguro por fora dela**
— renomear no Explorer quebra os `.meta`.

---

## 🟢 O que foi verificado e está correto

Vale registrar, porque significa que não precisa ser reverificado:

- **Nenhum `using UnityEditor` fora de `Editor/`** — só 2 arquivos, ambos no lugar certo.
  Não vazam para o `.exe`.
- **Nome de arquivo bate com o nome da classe** nos 13 `.cs`.
- **Nenhuma classe duplicada.**
- **Nenhum GUID duplicado** entre 191 assets. Nenhum `.meta` órfão, nenhum asset sem `.meta`,
  nenhum `m_Script: {fileID: 0}` nas cenas.
- **As 3 cenas do build existem no disco** e os GUIDs batem com os `.meta`.
- **LFS impecável** — 56 arquivos, **zero binário acima de 5 MB fora dele**. Verificado por
  assinatura de binário, não só pelo contador: `cama.blend` é Zstandard real,
  `untitled 1.fbx` é Kaydara FBX 7400.
- **`.gitattributes` cobre** `.fbx .blend .png .wav .psd .tga .mp3 .ogg` e mais.
- **Nada de `Library/`, `Temp/`, `obj/`, `.csproj` ou `.sln` versionado.**
- **Nenhum conteúdo duplicado** (md5 em todos os rastreados).
- Os ZIPs antigos da Letícia **não escondiam trabalho perdido** — verificado por carimbo de
  data. Podem ser descartados (3,7 GB, quase tudo `Library` regenerável) depois de ela confirmar.
