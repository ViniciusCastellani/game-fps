# Sons do PigMan

O prefab `PigManMiniBoss` usa tres variacoes de passos pesados, com casco/bota, grave curto e ruido de armadura. Os passos alternam sem repetir o mesmo clipe em sequencia e so tocam durante a locomocao, a cada 0,7 s.

`PigMan_GolpeEspada.wav` combina um grunhido grave de preparo e o corte da espada, com pico perto de 0,65 s, sincronizado ao `atrasoDoDano` atual. Toca ao iniciar a animacao, inclusive quando o jogador esquiva. `PigMan_AtaqueSucedido.wav` toca apenas quando o golpe causa dano: combina um acerto pesado e seco, metal curto e um ronco grave posterior. `PigMan_ImpactoEspada.wav` continua disponivel como alternativa de impacto simples. Ao mudar o tempo do golpe, ajuste tambem o pico do corte no gerador.

Sons mono, PCM 16 bits / 44,1 kHz, com pico de 0,88, fades nas bordas e atenuacao 3D entre 3 e 28 metros. O volume dos passos e 0,82; o da fonte de ataque e 0,56. O acerto usa uma fonte 2D independente com volume 0,85, interrompendo o corte quando a vida do jogador realmente diminui. Esse feedback nao perde volume com a distancia da camera e termina mesmo se o PigMan morrer logo depois. A fonte temporaria e removida apos o clipe. Esses ajustes ficam no prefab. O instalador do PigMan preserva esta configuracao quando recria o prefab.

Geracao deterministica: `python Tools/criar_audio_pigman.py`. Requer ffmpeg, numpy e scipy apenas para recriar os arquivos. Usa `andar_inimigo_sfx.mp3` e `Pig Death sfx.mp3` do proprio projeto como base de foley/voz, combinados com camadas sintetizadas de impacto, ar e metal. O jogo reproduz os WAVs prontos.
