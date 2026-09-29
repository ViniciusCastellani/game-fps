using UnityEngine;

// Coloque num GameObject vazio na cena Estadio_Futebol (ex: "MusicaDoJogo"),
// com um Audio Source. Deixe "Play On Awake" DESMARCADO no Audio Source —
// esse script já cuida de tocar sozinho quando a partida começa, e de
// parar sozinho quando o jogo termina (vitória ou derrota).
public class MusicaDoJogo : MonoBehaviour
{
    public static MusicaDoJogo instancia;

    public AudioSource fonteDeAudio;
    public AudioClip musicaPrincipal;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        Tocar();
    }

    public void Tocar()
    {
        if (fonteDeAudio == null || musicaPrincipal == null)
        {
            return;
        }

        fonteDeAudio.clip = musicaPrincipal;
        fonteDeAudio.loop = true;
        fonteDeAudio.Play();
    }

    public void Parar()
    {
        if (fonteDeAudio == null)
        {
            return;
        }

        fonteDeAudio.Stop();
    }
}