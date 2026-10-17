using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// Datos del director del prólogo (T-132): la hora de la noche, los textos de los objetivos y los puntos de la ruta de regreso.
    /// Los valores son propuesta de Tony (D-tony-69). Los textos y horas de T-133 se agregan en T-133.
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

        public float HoraNoche => horaNoche;
        public string TextoIrAlCarro => textoIrAlCarro;
        public string TextoRegresarACasa => textoRegresarACasa;
        public string TextoDormir => textoDormir;
        public string IdPuntoCarro => idPuntoCarro;
        public IReadOnlyList<string> PasosRuta => pasosRuta;
        public IdLugar LugarFinal => lugarFinal;
        public string IdAparicionSalida => idAparicionSalida;
        public string IdAparicionCama => idAparicionCama;
    }
}
