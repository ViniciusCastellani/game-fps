# Derby Segundo Tempo

Abra `Cenas/Menus/TelaInicial.unity` para jogar a campanha.

## Cenas e progressao

1. `TelaInicial`: menu principal.
2. `Estadio_TPS_Backup_Visual`: primeira fase; o nome foi mantido por ser a cena principal escolhida.
3. `Rua_TPS`: segunda fase; base jogavel com rua, edificios, coberturas, estacoes e NavMesh propria.
4. `TelaPausa`: cena de pausa preservada. Cada fase tambem mantem o painel de pausa existente, acionado com Esc.

A vitoria em uma horda oferece **Proxima Horda**. A ultima horda do estadio oferece **Ir para Rua**. Ao concluir a rua, **Jogar Novamente** reinicia a campanha no estadio. Reiniciar uma fase preserva a horda atual; voltar ao menu inicia uma campanha nova.

As cenas anteriores `Estadio_Futebol` e `Estadio_TPS` ficam em `Cenas/Arquivo`, fora da lista de build.

## Pastas

- `Cenas/Menus`, `Cenas/Fases`, `Cenas/Arquivo`: cenas e seus dados de navegacao.
- `Scripts/Runtime`: sistemas, jogador, combate, inimigos, interacao, interface, cameras, audio e cenario.
- `Scripts/Editor`: montadores, instaladores, previas e validadores exclusivos do editor.
- `Arte`: modelos, materiais, imagens e animacoes compartilhados.
- `Audio`: sons compartilhados do jogo.
- `Prefabs`: jogador, inimigos, estacoes, coletaveis e cenario.
- `Cenarios/Estadio` e `Cenarios/Rua`: recursos especificos de cada ambiente.
- `Armas/Laser`: recursos da arma laser.
- `Configuracoes`: renderizacao e entrada.
- `Documentacao`: arquivos do template original.

`Assets/Terceiros` preserva a estrutura interna dos pacotes importados. `Assets/TextMesh Pro` preserva os recursos do TMP. `Assets/Tests` contem as verificacoes do projeto. Nomes internos de esqueletos, animacoes e pacotes de terceiros foram mantidos.

## Hierarquia das fases

Os grupos da cena usam transformacao neutra e preservam a posicao mundial dos objetos:

```text
00_Sistemas
01_Jogador
02_Cameras
03_Cenario
04_Jogabilidade
    InimigosAtivos
    PontosDeSpawn
05_Iluminacao
06_Interface
07_Audio
```

Objetos que pertencem a prefabs continuam vinculados a eles. Os inimigos instanciados durante a partida aparecem em `04_Jogabilidade/InimigosAtivos`.

## Ferramentas

- `Derby > Projeto > Validar campanha e organizacao`: testa menu, hordas, troca de fase, pausa, vitoria final e reinicio.
- `Derby > Projeto > Organizar projeto e implementar Rua TPS`: migracao inicial; com a estrutura nova existente, apenas confirma a lista de build.
- `Tools/test_cenario_tps.py`: verificacoes de geometria e referencias do estadio.

A migracao usa `AssetDatabase.MoveAsset`, preservando os arquivos `.meta` e GUIDs. Os caminhos usados em scripts e ferramentas sao atualizados junto com as pastas. Relatorios e copias anteriores das quatro cenas ficam em `Library/Organizacao` (arquivos locais, fora do versionamento).

`Rua_TPS` e uma primeira versao funcional do cenario urbano. O modelo importado de cidade continua em `Arte/Modelos` para evolucao artistica posterior.
