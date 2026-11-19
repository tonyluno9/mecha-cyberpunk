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
            ("NeonAlto", new[] { "V22", "V23", "V24", "V12" }, 0.5f),
            ("LasLomas", new[] { "V15", "V13" }, 0.7f),
            ("Avenida", new[] { "V16", "V17", "V18", "V13", "V14", "V09", "V10" }, 0.4f),
            // Malecón: superdeportivos, convertibles, taxis y motos.
            ("Malecon", new[] { "V22", "V23", "V24", "V12", "V15", "V09", "MT01", "MT02" }, 0.7f),
            // Puerto: camión, tráiler, microbús y pickups.
            ("Puerto", new[] { "V17", "V18", "V16", "V10", "V03", "V19", "V04" }, 0.8f),
            // Autopista: todo, con más camiones (V16, V17, V18 repetidos).
            ("Autopista", new[] { "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21", "V22", "V23", "V24", "V16", "V17", "V18", "MT01" }, 0.9f),
            // Cerro: viejos, pickups, vochos y motos; nada de tráileres ni camiones por la pendiente.
            ("Cerro", new[] { "V01a", "V01b", "V02", "V03", "V10", "V04", "V14", "MT01", "MT02" }, 0.8f),
        };

        /// <summary>Carros que se suman a la lista general en toda la ciudad (como T-1345).</summary>
        public static readonly string[] Extras = { "V12", "V13", "V14", "V19" };

        /// <summary>Carros nuevos de T-1342/T-1343 que tienen que aparecer en el tráfico (V11 es solo de la policía).</summary>
        public static readonly string[] CarrosNuevos =
        {
            "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21", "V22", "V23", "V24", "MT01", "MT02",
        };

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
