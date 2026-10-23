# Arma laser

A arma está instalada no jogador da cena `Assets/Scenes/Estadio_TPS_Backup_Visual.unity`.
O prefab reutilizável está em `Prefabs/ArmaLaser.prefab`.

O modelo usa corpo chanfrado branco e grafite, empunhadura escura, detalhes em
âmbar e células emissivas em ciano. O cano aponta no eixo local +X, compatível
com a orientação usada por `ArmaNaMao`. O visual de papelão foi desativado
somente nessa cena; os assets originais continuam disponíveis.

Cada tiro válido mostra um feixe ciano com núcleo claro durante 0,09 s, saindo
da `PontaDoCano`. Uma luz breve acompanha o disparo. Há um som sintético próprio
em `Audio/DisparoLaser.wav`.

`Arma` continua controlando dano, munição, alcance, recarga e melhorias. O centro
visual do RectTransform `miraCanvas` é convertido em pixels da tela e em um raio
da câmera do jogador, respeitando o pivô, a escala e o modo de renderização do
Canvas. Se a referência estiver vazia, a arma localiza o RectTransform `Mira`
da mesma cena; sem mira, usa o centro do viewport da câmera. A mira
escolhe o destino, e uma verificação adicional no cano impede que o tiro passe
através de uma cobertura na frente da arma. O personagem e os gatilhos de
interação não absorvem o disparo. As hitboxes do tipo gatilho dos inimigos são
atingidas, inclusive quando estão em objetos filhos sem tag própria; o dano é
enviado ao `InimigoVida` na hierarquia. Se não houver alvo, o feixe segue até o
alcance configurado. Sem munição ou durante recarga, não há feixe nem dano.

Os controles existentes foram preservados. A entrada solicita o disparo, que é
resolvido em LateUpdate depois da câmera e da arma seguirem a mão. `EmissorLaser`
reutiliza dois LineRenderers e permite ajustar cor/material, largura e duração.

## Validação

Onze testes em Play Mode cobrem dano e consumo de munição, disparo sem alvo,
recarga/munição vazia, duração e reutilização do feixe, cobertura bloqueando o cano,
a hitbox do prefab real com consultas globais de gatilhos desativadas, hitbox
filha sem tag e o primeiro inimigo bloqueando tiros destinados ao que está atrás.
Os testes da mira usam o layout real do Canvas TPS com pivô no canto, verificam
dano no alvo sob a mira e alinhamento em pixels após mudanças de escala,
viewport e câmera, além de Canvas em Screen Space Camera.
O relatório está em `Docs/Testes/ArmaLaser.txt`. As prévias reais do Unity estão em
`Docs/Previews/TPS/arma_laser.png` e `Docs/Previews/TPS/disparo_laser.png`.

Menu **Derby > Arma laser > Testar disparos em Play Mode** para repetir os testes.

## Reaplicação

Use **Derby > Arma laser > Instalar na cena de backup** para recriar os assets e
ligar o visual ao jogador. O comando preserva os ajustes abertos e salva a cópia
do estádio. Uma cópia anterior do arquivo fica em `Library/ArmaLaserInstall`.

Se executar novamente o gerador Python do estádio, reaplique a arma pelo menu,
pois esse gerador reconstrói o jogador a partir da cena TPS original.
