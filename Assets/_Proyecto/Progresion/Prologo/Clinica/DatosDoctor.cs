using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Clinica
{
    /// <summary>Datos del doctor de la clínica (T-196). Los valores son la propuesta D-tony-170.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del doctor", fileName = "DatosDoctor_Nuevo")]
    public class DatosDoctor : ScriptableObject
    {
        [Header("Cobro y descanso")]
        [Tooltip("Lo que cobra por levantarte (Ƀ). Si no te alcanza, el dinero queda en negativo: esa es la deuda.")]
        [Min(0)] [SerializeField] private int cobro = 500;
        [Tooltip("Horas de juego que pasas inconsciente (h).")]
        [Range(0f, 23f)] [SerializeField] private float horasInconsciente = 6f;
        [Tooltip("Vida máxima que le deja al mecha que andaba fuera del taller, como fracción de la máxima (0 a 1). Nunca baja del piso de emergencia.")]
        [Range(0.01f, 1f)] [SerializeField] private float fraccionVidaMecha = 0.2f;

        [Header("Tiempos")]
        [Tooltip("Segundos que se ve caer al jugador antes de que empiece el fundido (s).")]
        [Min(0f)] [SerializeField] private float segundosEspera = 3f;
        [Tooltip("Segundos que la pantalla se queda en negro antes de despertar (s).")]
        [Min(0f)] [SerializeField] private float segundosNegro = 1f;
        [Tooltip("Segundos que se ve el subtítulo del doctor al despertar (s).")]
        [Min(0.5f)] [SerializeField] private float segundosSubtitulo = 5f;
        [Tooltip("Cada cuántos segundos busca al jugador y al mecha si todavía no los tiene (s).")]
        [Min(0.05f)] [SerializeField] private float segundosBusqueda = 0.5f;

        [Header("Textos")]
        [SerializeField] private string textoDoctor = "EL DOC: Otra vez tú. Son quinientos, mijo.";
        [SerializeField] private string textoDeuda = "Me los quedas debiendo.";
        [SerializeField] private string textoObjetivo = "VUELVE A CASA";

        [Header("Puntos del mundo")]
        [Tooltip("Id de la aparición donde despiertas (la camilla de la clínica).")]
        [SerializeField] private string idAparicionCamilla = "Clinica_Camilla";
        [Tooltip("Id de la aparición de respaldo si no hay camilla (la cama de casa).")]
        [SerializeField] private string idAparicionCasa = "Casa_Cama";
        [Tooltip("Lugar al que te manda el objetivo de volver a casa.")]
        [SerializeField] private IdLugar lugarCasa = IdLugar.Casa;

        public int Cobro => cobro;
        public float HorasInconsciente => horasInconsciente;
        public float FraccionVidaMecha => fraccionVidaMecha;
        public float SegundosEspera => segundosEspera;
        public float SegundosNegro => segundosNegro;
        public float SegundosSubtitulo => segundosSubtitulo;
        public float SegundosBusqueda => segundosBusqueda;
        public string TextoDoctor => textoDoctor;
        public string TextoDeuda => textoDeuda;
        public string TextoObjetivo => textoObjetivo;
        public string IdAparicionCamilla => idAparicionCamilla;
        public string IdAparicionCasa => idAparicionCasa;
        public IdLugar LugarCasa => lugarCasa;
    }
}
