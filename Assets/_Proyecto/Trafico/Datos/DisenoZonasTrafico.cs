using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>
    /// T-1389a: la tabla de las zonas de tráfico (dónde están y qué carros salen en cada una). La usan el creador de editor y las pruebas.
    /// Las cajas (dónde está cada zona) viven en Arte.Editor/ColocarT1364ZonasTrafico.Cajas, que también las coloca en la escena.
    /// Los códigos repetidos en una lista pesan más (el tráfico elige por índice).
    /// </summary>
    public static class DisenoZonasTrafico
    {
        /// <summary>Carros por zona (códigos de Carro_*; el prefab de tráfico es CarroTrafico_*) y su mezcla con la lista general.</summary>
        public static readonly (string nombre, string[] carros, float mezcla)[] Zonas =
        {
            ("Deshuesadero", new[] { "V20", "V21" }, 0.7f),
            // T-1683 (T45): menos carros: fuera V16 a V18 (microbús, camión, tráiler) y V22 a V24 (superdeportivos); queda un deportivo (V12).
            ("NeonAlto", new[] { "V12" }, 0.5f),
            ("LasLomas", new[] { "V15", "V13" }, 0.7f),
            ("Avenida", new[] { "V13", "V14", "V09" }, 0.4f),
            // T-1482: se quitaron del mapa Malecón, Puerto, Autopista y Cerro (revertir mapa 19-nov). Sus carros (V03, V04, V01a, V01b, V02 ya salen en la lista general;
            // las motos MT01 y MT02 pasan a los Extras) siguen en los 4 barrios.
        };

        /// <summary>
        /// Carros que se suman a la lista general en toda la ciudad (como T-1345). T-1482: suma las motos MT01/MT02 (antes solo en Malecón, Autopista y Cerro) y
        /// V20/V21 (antes solo en Deshuesadero y Autopista), para que salgan en los 4 barrios.
        /// </summary>
        public static readonly string[] Extras = { "V12", "V13", "V14", "V19", "V20", "V21", "MT01", "MT02" };

        /// <summary>Carros nuevos de T-1342/T-1343 que tienen que aparecer en el tráfico (T-1554: V10 y V11 se descartaron; T-1683: V16 a V18 y V22 a V24 también).</summary>
        public static readonly string[] CarrosNuevos =
        {
            "V12", "V13", "V14", "V15", "V19", "V20", "V21", "MT01", "MT02",
        };

        /// <summary>T-1683 (T45): modelos que ya no salen en el tráfico (los prefabs siguen en disco). Lo mismo que MenosCarrosT1683.CodigosExcluidos.</summary>
        public static readonly string[] Excluidos = { "V01b", "V16", "V17", "V18", "V22", "V23", "V24" };

        /// <summary>Motos que ganan prefab de tráfico en T-1389a.</summary>
        public static readonly string[] MotosNuevas = { "MT01", "MT02" };

        /// <summary>Nombres distintos de las zonas, en el orden de <see cref="Zonas"/>.</summary>
        public static string[] NombresDeZonas()
        {
            string[] r = new string[Zonas.Length];
            for (int i = 0; i < Zonas.Length; i++)
            {
                r[i] = Zonas[i].nombre;
            }
            return r;
        }
    }
}
