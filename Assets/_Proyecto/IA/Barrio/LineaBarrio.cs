using System;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T15): una línea de una escena del barrio. Texto vacío = solo gesto. Si hay <see cref="Audio"/> el subtítulo dura lo que el clip;
    /// si hay <see cref="IdVozSku"/> suena esa voz de SKULLS (catálogo, p. ej. SKU_RISA_BURLA); si no hay ninguno, solo subtítulo.
    /// </summary>
    [Serializable]
    public class LineaBarrio
    {
        [SerializeField] private RolBarrio quien;
        [Tooltip("Nombre que sale en el subtítulo (SKULL, SEÑORA...).")]
        [SerializeField] private string hablante = string.Empty;
        [SerializeField] private string texto = string.Empty;
        [Tooltip("Voz grabada (opcional). Si está, el subtítulo dura lo que el clip.")]
        [SerializeField] private AudioClip audio;
        [Tooltip("ID de voz de SKULLS del catálogo (opcional, solo SKULLS).")]
        [SerializeField] private string idVozSku = string.Empty;
        [SerializeField] private GestoBarrio gesto;
        [Tooltip("Pausa antes de la línea (s).")]
        [Min(0f)] [SerializeField] private float retraso = 0.4f;
        [Tooltip("Segundos que dura la línea; 0 = según el largo del texto o el clip (s).")]
        [Min(0f)] [SerializeField] private float duracion;

        public RolBarrio Quien => quien;
        public string Hablante => hablante;
        public string Texto => texto;
        public AudioClip Audio => audio;
        public string IdVozSku => idVozSku;
        public GestoBarrio Gesto => gesto;
        public float Retraso => retraso;
        public float Duracion => duracion;

        public LineaBarrio() { }

        public LineaBarrio(RolBarrio quien, string hablante, string texto, GestoBarrio gesto = GestoBarrio.Ninguno, string idVozSku = "", float retraso = 0.4f, float duracion = 0f)
        {
            this.quien = quien;
            this.hablante = hablante;
            this.texto = texto;
            this.gesto = gesto;
            this.idVozSku = idVozSku;
            this.retraso = retraso;
            this.duracion = duracion;
        }
    }
}
