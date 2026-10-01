using UnityEngine;

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

    public void GarantirTocando()
    {
        if (fonteDeAudio != null && !fonteDeAudio.isPlaying)
        {
            Tocar();
        }
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