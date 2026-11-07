"""Cria foley do PigMan a partir de sons do projeto e camadas sintetizadas.

Requer Python, numpy, scipy e ffmpeg apenas para gerar os assets, nunca em runtime.
Uso: python Tools/criar_audio_pigman.py
"""

import argparse
from pathlib import Path
import subprocess
import uuid

import numpy as np
from scipy import signal
from scipy.io import wavfile

TAXA = 44100


def filtrar(onda, minimo, maximo):
    limites = [minimo, maximo] if minimo else maximo
    tipo = "bandpass" if minimo else "lowpass"
    return signal.sosfilt(signal.butter(3, limites, btype=tipo, fs=TAXA, output="sos"), onda)


def ler(projeto, nome):
    dados = subprocess.check_output([
        "ffmpeg", "-v", "error", "-i", str(projeto / "Assets/DerbySegundoTempo/Audio" / nome),
        "-ac", "1", "-ar", str(TAXA), "-f", "f32le", "pipe:1",
    ])
    onda = np.frombuffer(dados, dtype="<f4").astype(np.float64)
    ativos = np.flatnonzero(np.abs(onda) > np.max(np.abs(onda)) * .015)
    onda = onda[max(0, ativos[0] - 220):min(len(onda), ativos[-1] + 440)]
    return onda / max(np.max(np.abs(onda)), 1e-6)


def baixar_tom(onda, velocidade):
    return np.interp(np.arange(0, len(onda) - 1, velocidade), np.arange(len(onda)), onda)


def inserir(destino, origem, inicio, ganho=1):
    inicio = int(inicio * TAXA)
    tamanho = min(len(origem), len(destino) - inicio)
    if tamanho > 0:
        destino[inicio:inicio + tamanho] += origem[:tamanho] * ganho


def ruido(rng, tamanho, minimo, maximo):
    onda = filtrar(rng.normal(size=tamanho), minimo, maximo)
    return onda / max(np.sqrt(np.mean(onda * onda)), 1e-6)


def corpo(t, grave, final, decaimento):
    # Frequencia cai apos o contato, dando peso sem gerar um tom sustentado.
    fase = 2 * np.pi * (final * t + (grave - final) * .032 * (1 - np.exp(-t / .032)))
    return (np.sin(fase) + .3 * np.sin(2 * fase)) * np.exp(-decaimento * t) * (1 - np.exp(-550 * t))


def armadura(rng, t, decaimento):
    metal = np.zeros_like(t)
    for frequencia, ganho in [(470, .35), (830, .25), (1471, .17), (2377, .1)]:
        frequencia *= rng.uniform(.96, 1.04)
        metal += ganho * np.sin(2 * np.pi * frequencia * t) * np.exp(-(decaimento + frequencia / 250) * t)
    return metal * (1 - np.exp(-700 * t))


def passo(base, indice):
    rng = np.random.default_rng(1200 + indice)
    t = np.arange(int(.60 * TAXA)) / TAXA
    onda = .8 * corpo(t, 108 + indice * 5, 49 + indice * 2, 10)
    inserir(onda, filtrar(baixar_tom(base, .72 + indice * .045), 50, 3200), .003, .48)
    # Contato de casco/bota, cascalho e pecas de armadura soltas.
    onda += .17 * ruido(rng, len(t), 200, 2300) * np.exp(-23 * t) * (1 - np.exp(-700 * t))
    inserir(onda, .35 * armadura(rng, t, 20), .035 + indice * .006)
    return onda


def golpe(voz):
    rng = np.random.default_rng(6201)
    t = np.arange(int(.96 * TAXA)) / TAXA
    onda = np.zeros_like(t)
    grunhido = filtrar(baixar_tom(voz, .65), 75, 1700)
    # Grunhido curto durante o preparo; corte com pico aos 0,65 s do dano.
    grunhido = grunhido[:int(.43 * TAXA)]
    grunhido *= np.minimum(1, np.arange(len(grunhido)) / 300)
    grunhido *= np.minimum(1, (len(grunhido) - np.arange(len(grunhido))) / 1100)
    inserir(onda, grunhido, .035, .48)
    ar = ruido(rng, len(t), 350, 7800)
    varrida = np.exp(-.5 * ((t - .63) / .065) ** 2)
    onda += .36 * ar * varrida
    onda += .13 * signal.chirp(t, f0=420, f1=1900, t1=.7) * varrida
    cauda = np.maximum(t - .65, 0)
    onda += .2 * armadura(rng, cauda, 16) * (t >= .65)
    return onda


def impacto():
    rng = np.random.default_rng(6202)
    t = np.arange(int(.42 * TAXA)) / TAXA
    onda = .8 * corpo(t, 172, 65, 18)
    onda += .2 * ruido(rng, len(t), 110, 2900) * np.exp(-32 * t) * (1 - np.exp(-900 * t))
    onda += .45 * armadura(rng, t, 23)
    return onda


def ataque_sucedido(voz):
    rng = np.random.default_rng(6203)
    t = np.arange(int(.64 * TAXA)) / TAXA
    onda = np.zeros_like(t)
    # Acerto seco e pesado, com metal curto e um ronco posterior do PigMan.
    inserir(onda, impacto(), 0, .72)
    onda += .4 * corpo(t, 138, 52, 14)
    onda += .22 * ruido(rng, len(t), 240, 4300) * np.exp(-55 * t) * (1 - np.exp(-950 * t))
    ronco = filtrar(baixar_tom(voz, .72), 85, 1450)
    ronco = ronco[:int(.28 * TAXA)].copy()
    ronco *= np.minimum(1, np.arange(len(ronco)) / 450)
    ronco *= np.minimum(1, (len(ronco) - np.arange(len(ronco))) / 1500)
    inserir(onda, ronco, .13, .36)
    inserir(onda, armadura(rng, t, 30), .018, .16)
    return onda


def gravar(pasta, nome, onda):
    onda = filtrar(onda, 32, 12000)
    onda -= np.mean(onda)
    # Saturacao suave e margens nos extremos evitam estalos e clipping.
    onda = np.tanh(1.35 * onda)
    onda[:220] *= np.linspace(0, 1, 220)
    onda[-1764:] *= np.linspace(1, 0, 1764)
    onda *= .88 / max(np.max(np.abs(onda)), 1e-6)
    arquivo = pasta / nome
    wavfile.write(arquivo, TAXA, np.round(onda * 32767).astype(np.int16))
    meta = arquivo.with_name(arquivo.name + ".meta")
    if not meta.exists():
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "derby-segundo-tempo/audio/pigman/" + nome).hex
        meta.write_text(f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
""")
    rms = np.sqrt(np.mean(onda * onda))
    print(f"{nome}: {len(onda) / TAXA:.2f}s, pico {np.max(np.abs(onda)):.2f}, RMS {20 * np.log10(rms):.1f} dBFS")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--projeto", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--saida", type=Path)
    args = parser.parse_args()
    pasta = args.saida or args.projeto / "Assets/DerbySegundoTempo/Audio/PigMan"
    pasta.mkdir(parents=True, exist_ok=True)
    base = ler(args.projeto, "andar_inimigo_sfx.mp3")
    voz = ler(args.projeto, "Pig Death sfx.mp3")
    for i in range(3):
        gravar(pasta, f"PigMan_PassoPesado{i + 1:02}.wav", passo(base, i))
    gravar(pasta, "PigMan_GolpeEspada.wav", golpe(voz))
    gravar(pasta, "PigMan_ImpactoEspada.wav", impacto())
    gravar(pasta, "PigMan_AtaqueSucedido.wav", ataque_sucedido(voz))


if __name__ == "__main__":
    main()
