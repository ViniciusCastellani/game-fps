# Cenário TPS — revisão visual

Abra `Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity` no Unity.

A cena original `Estadio_TPS.unity`, o prefab `soccer_stadium`, os dados originais
de navegação, a lógica dos controles e a lista de cenas do build permanecem intactas. O controle
expõe sua velocidade horizontal planejada para a bola consultar o movimento antes da colisão.

## Elementos da cópia

O grupo `Melhorias_Visuais_TPS` contém 25 instâncias de sete prefabs. A arma laser
está ligada separadamente ao jogador. No cenário existem:

- Seis placas de publicidade baixas e quatro balizadores no perímetro.
- Quatro bandeirinhas de escanteio, pequenas e presas a mastros de 1,8 m.
- Dez barreiras de concreto novas, distribuídas em quatro grupos abertos no meio do campo.
- Uma bola de futebol dinâmica a poucos passos da posição inicial do jogador.

As placas, colunas e coberturas extras dos postos médico e de suprimentos foram
removidas. Os itens, os gatilhos e os textos originais exibidos por proximidade
continuam funcionando pelo sistema existente, com a área frontal livre.

As posições das bandeirinhas vêm dos quatro encontros das linhas brancas no
atlas do gramado original, projetados sobre sua malha. Todos os elementos novos
usam a altura da superfície do gramado, incluindo as transformações do estádio.

As barreiras reaproveitam modelos e materiais do projeto. Cada uma tem um
BoxCollider sólido e um NavMeshObstacle com carving ativado. A navegação é
recortada em execução, sem regravar os dados compartilhados pela cena original.
Os grupos ficam a mais de quatro metros dos pontos de surgimento de inimigos
e preservam passagens entre as formações. A região dos postos permanece livre.

As bandeirinhas, placas e balizadores são decorativos, sem colisores. As artes
usam uma malha plana com UV explícitas: topo +Y e frente -Z. A paleta dos assentos
e da publicidade é aplicada por overrides somente nesta cena. A iluminação e a
atmosfera originais foram preservadas.

## Validação

A revisão foi importada e renderizada no Unity 6000.5.10f1 em onze posições,
incluindo vista superior, escanteio, obstáculos, mensagens de interação, a bola e a arma laser.
As imagens revisadas estão em `Docs/Previews/TPS`.

As dez barreiras passaram nas verificações de colisão por raios no Unity.
Foi conferida a presença do carving de navegação em todas elas. O comportamento da bola foi testado separadamente em Play Mode. As demais
verificações não incluem uma partida completa.

O menu **Derby > Cenario TPS > Gerar previas da cena de backup** gera novamente
as imagens em `Library/CenarioTPSPreview`. A ferramenta usa uma cena de preview
isolada e não salva nem substitui a cena aberta pelo usuário. As mensagens são
ativadas apenas nessa preview para conferir a visibilidade.

Na raiz do projeto:

```sh
python3 Tools/test_cenario_tps.py
```

Os oito testes cobrem contato com o piso, apoios, UV/orientação das placas,
referências, quatro escanteios, área livre dos postos e colisores/carving das
barreiras afastadas dos pontos de surgimento e a configuração física/visual da bola.
Requer NumPy e PyYAML.

## Bola de futebol

O prefab `Prefabs/BolaFutebol.prefab` contém Rigidbody, SphereCollider sólido,
material físico e o componente `BolaFutebol`. O visual de pentágonos pretos e
hexágonos brancos é uma malha procedural. A bola é colocada em `(7.95, -22.577, 3910.2)`,
à frente da posição inicial do jogador.

Basta andar contra a bola para empurrá-la. Usando a corrida existente (`Fire3`),
o impulso é maior. Não há botão extra nem alteração das teclas do personagem.
A bola gira, colide com o cenário, desacelera no gramado e volta ao ponto inicial
se cair para fora da geometria do estádio.

No Inspector do componente `BolaFutebol`, ajuste `multiplicadorAndando`,
`multiplicadorCorrendo`, `velocidadeMaxima` e `desaceleracaoNoSolo` para mudar a sensação.
O toque só acrescenta a velocidade necessária e possui intervalo entre contatos.
Um personagem parado não gera impulso extra.

Quatro testes de física em Play Mode passaram: contato andando/correndo,
contato parado, rolagem com desaceleração e colisão em alta velocidade.
Os picos medidos no teste foram 4,827 m/s andando e 8,303 m/s correndo.
O teste usa o prefab real e colisões em uma cena física isolada.
O relatório está em `Docs/Testes/BolaFutebol.txt`.

Para repetir, use **Derby > Bola de futebol > Testar fisica em Play Mode**.
Os testes ficam em `Assets/Tests/PlayMode`; os resultados detalhados são gravados
em `Library/BolaFutebolTests`.

## Regeneração

```sh
python3 Tools/criar_cenario_tps_backup.py
```

Requer NumPy e Pillow. A geração substitui a cena visual e os assets gerados:
preserve ajustes manuais antes de executá-la. Aguarde o Unity concluir as
importações antes de reabrir a cena ou gerar prévias.

A cena está fora da lista do build. Para testar reinício por SceneManager ou
distribuir essa variação, adicione-a ao Build Profile.

## Arma laser

A arma de papelão foi substituída na cópia do estádio pelo prefab
`Assets/DerbySegundoTempo/Armas/Laser/Prefabs/ArmaLaser.prefab`, com feixe ciano a cada tiro.
Consulte `Assets/DerbySegundoTempo/Armas/Laser/LEIA-ME.md` para detalhes e testes.
Depois de regenerar o estádio pelo Python, reaplique a arma pelo menu
**Derby > Arma laser > Instalar na cena de backup**.
