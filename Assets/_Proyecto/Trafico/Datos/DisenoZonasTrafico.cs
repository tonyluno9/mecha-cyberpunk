using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>
    /// T-1389a: la tabla de las zonas de tráfico (dónde están y qué carros salen en cada una). La usan el creador de editor y las pruebas.
    /// Las cajas del centro (Avenida, LasLomas, NeonAlto, Deshuesadero) son un reparto provisional de los 4 barrios actuales; las de las zonas nuevas salen de docs/plan-zonas-18nov.md.
    /// Los códigos repetidos en una lista pesan más (el tráfico elige por índice).
    /// </summary>
    public static class DisenoZonasTrafico
    {
        /// <summary>Una caja de zona (sin girar). Varias cajas pueden llevar el mismo nombre.</summary>
        public readonly struct Caja
        {
            public readonly string nombre;
            public readonly Vector3 centro;
            public readonly Vector3 tamano;

            public Caja(string nombre, Vector3 centro, Vector3 tamano)
            {
                this.nombre = nombre;
                this.centro = centro;
                this.tamano = tamano;
            }

            public bool Contiene(Vector3 p)
            {
                Vector3 m = tamano * 0.5f;
                Vector3 d = p - centro;
                return Mathf.Abs(d.x) <= m.x && Mathf.Abs(d.y) <= m.y && Mathf.Abs(d.z) <= m.z;
            }

            /// <summary>Si las dos cajas se encimen (más de 1 cm de traslape en los tres ejes).</summary>
            public bool Encima(Caja otra)
            {
                Vector3 suma = (tamano + otra.tamano) * 0.5f;
                Vector3 d = centro - otra.centro;
                return suma.x - Mathf.Abs(d.x) > 0.01f && suma.y - Mathf.Abs(d.y) > 0.01f && suma.z - Mathf.Abs(d.z) > 0.01f;
            }
        }

        public static readonly Caja[] Cajas =
        {
            // Centro (4 barrios actuales, x -135..135, z -88..88). Provisional.
            new Caja("LasLomas", new Vector3(-91f, 0f, 0f), new Vector3(88f, 30f, 176f)),
            new Caja("Avenida", new Vector3(0f, 0f, 0f), new Vector3(94f, 30f, 176f)),
            new Caja("NeonAlto", new Vector3(91f, 0f, 44f), new Vector3(88f, 30f, 88f)),
            new Caja("Deshuesadero", new Vector3(91f, 0f, -44f), new Vector3(88f, 30f, 88f)),
            // Zonas nuevas (plan-zonas-18nov). El Malecón acaba en x = 115 para no pisar el Puerto.
            new Caja("Malecon", new Vector3(-107.5f, 0f, -233.5f), new Vector3(445f, 40f, 57f)),
            new Caja("Puerto", new Vector3(222.5f, 0f, -147.5f), new Vector3(215f, 40f, 205f)),
            new Caja("Cerro", new Vector3(-387.5f, 20f, 22.5f), new Vector3(185f, 80f, 385f)),
            new Caja("Autopista", new Vector3(-45f, 10f, 250f), new Vector3(570f, 30f, 44f)),
            new Caja("Autopista", new Vector3(300f, 5f, 155f), new Vector3(44f, 30f, 200f)),
        };

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
