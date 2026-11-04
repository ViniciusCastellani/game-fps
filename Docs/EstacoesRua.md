# Estacoes urbanas de Rua_TPS

As estacoes ficam em `04_Jogabilidade/Estacoes_Rua`, integradas as fachadas e voltadas para as calcadas.

| Estacao | Local | Uso |
| --- | --- | --- |
| OficinaDerby_Entrada | Calcada oeste, perto do inicio | `[1]` aumenta dano; `[2]` aumenta o pente |
| OficinaDerby_Apoio | Calcada oeste, no meio da rua | Mesmas melhorias, acessiveis durante a partida |
| Farmacia_DePlantao | Farmacia na calcada leste | `[E]` compra cura |
| Deposito_Recarga | Armario industrial na calcada leste | `[E]` compra municao |

Os precos, quantidades, teclas e sons sao copiados das instancias da primeira fase. A aproximacao abre o painel com os valores; sair da area fecha o painel. A compra utiliza o saldo do `SistemaDePontos`.

Em `Rua_TPS`, a loja rapida global do jogador fica desativada, inclusive apos pausar e continuar, para que as teclas `1` e `2` sejam usadas pelas oficinas sem duplicar compras. Os componentes e a configuracao da cena do estadio permanecem preservados.

## Prefabs

Em `Assets/DerbySegundoTempo/Prefabs/Estacoes/Rua`:

- `OficinaDerby.prefab` deriva de `EstacaoUpgrades.prefab` e utiliza `EstacaoDeUpgrade`.
- `FarmaciaBairro.prefab` deriva de `EstacaoCura.prefab` e utiliza `EstacaoDeCura`.
- `DepositoRecarga.prefab` deriva de `EstacaoMunicao.prefab` e utiliza `EstacaoDeMunicao`.

As oficinas possuem bancada, gaveteiro, painel de ferramentas, maleta e arma em manutencao. A farmacia possui balcao, kits de primeiros socorros e sinalizacao verde. A recarga possui gabinete industrial, celulas luminosas e terminal.

Cada variante tem um gatilho de atendimento na frente, um volume solido para o movel e `NavMeshObstacle` com carving. O componente de compra e a interface pertencem a propria variante.

## Edicao e verificacao

- `Derby > Rua TPS > Montar oficinas e estacoes urbanas`: gera os variantes e remonta as instancias em `Estacoes_Rua`.
- `Derby > Rua TPS > Testar compras nas estacoes`: verifica entrada e saida dos gatilhos, compras, desconto de saldo e recusa de compras sem pontos em Play Mode.
- As previas ficam em `Docs/Previews/Rua`; os relatorios locais completos ficam em `Library/EstacoesRua`.
