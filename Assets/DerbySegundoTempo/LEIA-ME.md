# Derby Segundo Tempo

Abra `Cenas/Menus/TelaInicial.unity` para jogar a campanha.

## Cenas e progressao

1. `TelaInicial`: menu principal.
2. `Estadio_TPS_Backup_Visual`: primeira fase; o nome foi mantido por ser a cena principal escolhida.
3. `Rua_TPS`: segunda fase; base jogavel com rua, edificios, coberturas, estacoes e NavMesh propria.
4. `TelaPausa`: cena de pausa preservada. Cada fase tambem mantem o painel de pausa existente, acionado com Esc.

A vitoria em uma horda oferece **Proxima Horda**. A ultima horda do estadio oferece **Ir para Rua**. Ao concluir a rua, **Jogar Novamente** reinicia a campanha no estadio. Reiniciar uma fase preserva a horda atual; voltar ao menu inicia uma campanha nova.

As cenas anteriores `Estadio_Futebol` e `Estadio_TPS` ficam em `Cenas/Arquivo`, fora da lista de build.

## Modos de jogo

Fluxo: `TelaInicial` > **INICIAR** > **NORMAL** (campanha estadio e rua, como antes) ou **INFINITO** > escolha do cenario (**Estadio** ou **Rua**) > **Iniciar sobrevivencia**. Esc volta uma tela nos menus.

Regras do modo Infinito:

- O tutorial abre em toda partida; o tempo e os inimigos so comecam ao fechar o tutorial.
- Os inimigos nascem sem parar, com limite de vivos ao mesmo tempo e um PigMan periodico.
- O HUD mostra o tempo de sobrevivencia e os abates; o cronometro e o contador de ordas do modo Normal ficam ocultos.
- Os pontos continuam sendo gastos nas estacoes. O placar final soma todos os pontos conquistados, mesmo os gastos.
- Ao morrer, a tela de resultado mostra cenario, pontos, abates e tempo, com os botoes **Tentar novamente**, **Escolher cenario** e **Menu inicial**.

Ajustes: em cada fase, `00_Sistemas/ModoInfinito` > `GeradorDeInimigosInfinito`. Padroes: intervalo inicial 3 s, minimo 1,2 s, reducao de 0,6 s por minuto, 3 inimigos no inicio, no maximo 12 vivos, um PigMan a cada 90 s (no maximo 1 vivo) e pontos de spawn a pelo menos 8 m do jogador.

O modo Normal nao muda e e o padrao ao abrir uma fase direto no editor: o modo Infinito so vale quando escolhido pelo menu.

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
- `Derby > Modos de Jogo > Montar menus e modo infinito`: monta os menus em `TelaInicial` e o modo Infinito nas duas fases (pode ser repetido sem duplicar objetos).
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
