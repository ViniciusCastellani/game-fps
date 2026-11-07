# Derby Segundo Tempo

Abra `Cenas/Menus/TelaInicial.unity` para jogar a campanha.

## Cenas e progressao

1. `TelaInicial`: menu principal; **INICIAR** abre `SelecaoDeModo` e **INFINITO** abre `SelecaoDeCenario`.
2. `Estadio_TPS_Backup_Visual`: primeira fase; o nome foi mantido por ser a cena principal escolhida.
3. `Rua_TPS`: segunda fase; base jogavel com rua, edificios, coberturas, estacoes e NavMesh propria.
4. `TelaPausa`: cena de pausa preservada. Cada fase tambem mantem o painel de pausa existente, acionado com Esc.

A vitoria em uma horda oferece **Proxima Horda**. A ultima horda do estadio oferece **Ir para Rua**. Ao concluir a rua, **Jogar Novamente** reinicia a campanha no estadio. Reiniciar uma fase preserva a horda atual; voltar ao menu inicia uma campanha nova.

As cenas anteriores `Estadio_Futebol` e `Estadio_TPS` ficam em `Cenas/Arquivo`, fora da lista de build.

## Modos de jogo

Fluxo com cenas separadas: `TelaInicial` > **INICIAR** > `SelecaoDeModo` > **CLÁSSICO** (campanha estadio e rua) ou **INFINITO** > `SelecaoDeCenario` > miniatura de **Estadio** ou **Rua** > **Iniciar sobrevivencia**. Esc ou **Voltar** retorna a cena anterior. As duas cenas de selecao fazem parte do build.

Regras do modo Infinito:

- O tutorial abre em toda partida; o tempo e os inimigos so comecam ao fechar o tutorial.
- A primeira horda tem 6 inimigos, com 3 surgindo no inicio. Cada horda seguinte acrescenta 2 inimigos. No maximo 12 ficam vivos ao mesmo tempo.
- Elimine todos os inimigos da horda para conclui-la. A proxima comeca automaticamente apos 5 segundos, sem limite de hordas e sem trocar de cenario.
- Um PigMan pode ocupar uma vaga da horda a cada 90 segundos, com no maximo um vivo. Ele tambem precisa ser eliminado para concluir a horda.
- O HUD mostra tempo de sobrevivencia, abates, horda atual e inimigos restantes (incluindo os que ainda vao surgir). Pausa e tutorial interrompem o tempo, os spawns e o intervalo entre hordas.
- A partida termina quando a vida chega a zero. Nao ha derrota por tempo nem tela de vitoria no infinito.
- O resultado mostra cenario, **pontos totais coletados** (incluindo os gastos nas estacoes), **saldo nao gasto**, **inimigos abatidos**, **hordas sobrevividas** e tempo. A horda em andamento nao entra no total de hordas sobrevividas.
- **Tentar novamente** reinicia a sobrevivencia na mesma fase e zera os contadores. **Escolher cenario** abre `SelecaoDeCenario`, lembrando a ultima fase jogada. **Menu inicial** retorna a `TelaInicial`.

Ajustes: em cada fase, `00_Sistemas/ModoInfinito` > `GeradorDeInimigosInfinito`. Alem da quantidade de inimigos e intervalo entre hordas, o intervalo de spawn comeca em 3 s, chega ao minimo de 1,2 s e diminui 0,6 s por minuto. Os pontos de spawn priorizam distancia de pelo menos 8 m do jogador.

O modo Classico preserva a campanha e e o padrao ao abrir uma fase direto no editor: o modo Infinito so vale quando escolhido pelo menu.

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
- `Derby > Modos de Jogo > Montar menus e modo infinito`: monta `TelaInicial`, `SelecaoDeModo`, `SelecaoDeCenario` e o modo Infinito nas duas fases (pode ser repetido sem duplicar objetos).
- `Derby > Modos de Jogo > Validar modos de jogo`: confere as cenas e roda os testes de Play Mode dos modos de jogo e da campanha. Relatorios em `Library/ModosDeJogo`.
- `Tools/test_cenario_tps.py`: verificacoes de geometria e referencias do estadio.

Comandos de terminal (feche o Editor antes; em maquina sem tela use `xvfb-run -a` na frente do segundo e do terceiro):

```bash
UNITY=~/Unity/Hub/Editor/6000.5.10f1/Editor/Unity
PROJ=/home/salzani/Documents/college/games/game-fps
mkdir -p "$PROJ/Library/ModosDeJogo"
# montar menus e modo infinito
"$UNITY" -batchmode -projectPath "$PROJ" -executeMethod MontadorModosDeJogo.MontarTudo \
  -logFile "$PROJ/Library/ModosDeJogo/montagem.log" -quit
# testes dos modos e regressao da campanha
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "ModosDeJogoTests;CampanhaTests;EstacoesRuaTests;PigManTests;GolEasterEggTests" \
  -testResults "$PROJ/Library/ModosDeJogo/testes.xml" -logFile "$PROJ/Library/ModosDeJogo/testes.log"
# regressao completa
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Library/ModosDeJogo/testes-completos.xml" -logFile "$PROJ/Library/ModosDeJogo/testes-completos.log"
```

A migracao usa `AssetDatabase.MoveAsset`, preservando os arquivos `.meta` e GUIDs. Os caminhos usados em scripts e ferramentas sao atualizados junto com as pastas. Relatorios e copias anteriores das quatro cenas ficam em `Library/Organizacao` (arquivos locais, fora do versionamento).

`Rua_TPS` e uma primeira versao funcional do cenario urbano. O modelo importado de cidade continua em `Arte/Modelos` para evolucao artistica posterior.
