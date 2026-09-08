# docs/ — o registro do projeto

Esta pasta existe porque o Fragmento é feito por 6 pessoas em 4 máquinas diferentes, com
sessões de Claude Code rodando em várias delas ao mesmo tempo. Sem um lugar comum, cada
máquina redescobre as mesmas coisas — e já redescobriu.

Nada aqui é asset da Unity. A pasta fica **fora de `KRLH funciona2/Assets/`** de propósito:
assim a Unity não gera `.meta` e não trata documentação como conteúdo do jogo.

## Os arquivos

| Arquivo | Para quê | Quando escrever |
|---|---|---|
| [`DIARIO.md`](DIARIO.md) | O que foi feito, em ordem cronológica | Toda vez que algo mudar no projeto |
| [`DECISOES.md`](DECISOES.md) | Decisões tomadas e **por quê** | Quando o JP decidir algo que muda o rumo |
| [`ACHADOS.md`](ACHADOS.md) | Problemas encontrados e o status de cada um | Quando alguém achar um problema, e quando resolver |

O `CLAUDE.md` na raiz do repositório é diferente: ele é a **regra**, não o histórico.
Se uma decisão do `DECISOES.md` mudar uma regra, o `CLAUDE.md` também precisa ser atualizado.

## Como escrever

**Entrada de diário** — data, o que mudou, e o que qualquer pessoa precisa fazer por causa
disso. Se não houver consequência para ninguém, provavelmente não precisa entrar.

**Decisão** — o que foi decidido, quem decidiu, e o motivo. O motivo é a parte que importa:
daqui a três semanas ninguém lembra por que a Fase B caiu, e sem o motivo alguém tenta
trazer de volta.

**Achado** — o que está errado, onde, e o que acontece se ninguém mexer. Quando resolver,
marque como resolvido em vez de apagar — saber que já foi problema evita repetir.

## Regra que vale para tudo aqui

Antes de escrever que algo existe, **confira no Git, não no Trello**. O histórico deste
projeto tem cinco casos de card marcado como concluído cujo arquivo nunca foi commitado,
e dois casos em que o próprio Claude afirmou que um asset existia porque leu a lista de
tarefas em vez de olhar o disco.
