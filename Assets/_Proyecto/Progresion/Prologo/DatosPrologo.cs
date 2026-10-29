using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// Datos del director del prólogo (T-132): la hora de la noche, los textos de los objetivos y los puntos de la ruta de regreso.
    /// Los valores de la noche son propuesta de Tony (D-tony-69); los del despertar (T-133), D-tony-58 y D-tony-70.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del prólogo", fileName = "DatosPrologo_Nuevo")]
    public class DatosPrologo : ScriptableObject
    {
        [Header("La noche del campeonato")]
        [Tooltip("Hora a la que empieza la pelea del tutorial (h).")]
        [Range(0f, 23.99f)] [SerializeField] private float horaNoche = 22f;

        [Header("Objetivos")]
        [SerializeField] private string textoIrAlCarro = "Ve a tu carro";
        [SerializeField] private string textoRegresarACasa = "Regresa a casa en tu carro";
        [SerializeField] private string textoDormir = "Duerme en tu cama";
        [Tooltip("Objetivo de la salida del clandestino cuando el jugador ya va en el carro: apunta a la emboscada (T-136, D-tony-187).")]
        [SerializeField] private string textoIrALaEmboscada = "Maneja a casa";

        [Header("Puntos del mundo")]
        [Tooltip("Id del IPuntoHistoria del carro, estacionado cerca del clandestino.")]
        [SerializeField] private string idPuntoCarro = "Prologo_Carro";
        [Tooltip("Ids de los IPuntoHistoria de la ruta de regreso, en orden (las esquinas). Al final el objetivo es el lugar de la casa.")]
        [SerializeField] private List<string> pasosRuta = new List<string> { "Prologo_Esquina_1" };
        [Tooltip("Lugar al que termina la ruta de regreso.")]
        [SerializeField] private IdLugar lugarFinal = IdLugar.Casa;
        [Tooltip("Id de la aparición a pie donde se reanuda una partida a media noche, afuera del clandestino.")]
        [SerializeField] private string idAparicionSalida = "Clandestino_Salida";
        [Tooltip("Id de la aparición donde se reanuda una partida en casa.")]
        [SerializeField] private string idAparicionCama = "Casa_Cama";

        [Header("El despertar en el presente (T-133, D-tony-58)")]
        [Tooltip("Hora a la que despierta el jugador (h). Cuando abre el clandestino de Alan (20:00): el jugador no espera.")]
        [Range(0f, 23.99f)] [SerializeField] private float horaDespertar = 20f;
        [Tooltip("Vida del mecha al despertar, como fracción de la máxima (0 a 1). Nunca baja del piso de emergencia.")]
        [Range(0.01f, 1f)] [SerializeField] private float fraccionVidaDespertar = 0.45f;
        [Tooltip("Aviso de una línea al despertar.")]
        [SerializeField] private string textoAvisoDespertar = "Tu mecha quedó fregado. Puedes repararlo en el banco del taller antes de pelear.";
        [Tooltip("Segundos que se ve el aviso al despertar (s).")]
        [Min(0.5f)] [SerializeField] private float segundosAvisoDespertar = 6f;
        [SerializeField] private string textoIrAlClandestino = "Ve al clandestino: tu primera pelea casera";
        [SerializeField] private string textoHablarEncargado = "Habla con el encargado (E)";
        [Tooltip("Lugar al que va el jugador a su primera pelea casera.")]
        [SerializeField] private IdLugar lugarClandestino = IdLugar.Clandestino;

        public float HoraNoche => horaNoche;
        public string TextoIrAlCarro => textoIrAlCarro;
        public string TextoRegresarACasa => textoRegresarACasa;
        public string TextoDormir => textoDormir;
        public string TextoIrALaEmboscada => textoIrALaEmboscada;
        public string IdPuntoCarro => idPuntoCarro;
        public IReadOnlyList<string> PasosRuta => pasosRuta;
        public IdLugar LugarFinal => lugarFinal;
        public string IdAparicionSalida => idAparicionSalida;
        public string IdAparicionCama => idAparicionCama;
        public float HoraDespertar => horaDespertar;
        public float FraccionVidaDespertar => fraccionVidaDespertar;
        public string TextoAvisoDespertar => textoAvisoDespertar;
        public float SegundosAvisoDespertar => segundosAvisoDespertar;
        public string TextoIrAlClandestino => textoIrAlClandestino;
        public string TextoHablarEncargado => textoHablarEncargado;
        public IdLugar LugarClandestino => lugarClandestino;
    }
}
