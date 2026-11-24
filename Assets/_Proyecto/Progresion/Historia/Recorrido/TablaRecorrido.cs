using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625: textos y duraciones de la tabla 10.2. Son los valores de los assets C01 a C09 (los crea <c>CreadorRecorrido.Aplicar</c>) y el respaldo
    /// del juego si un asset falta. Los objetivos van aquí para que quepan en el HUD y se cambien en un solo lugar.
    /// </summary>
    public static class TablaRecorrido
    {
        public readonly struct FilaCinematica
        {
            public readonly string Id;
            public readonly string Nombre;
            public readonly string Texto;
            public readonly float Duracion;

            public FilaCinematica(string id, string nombre, string texto, float duracion)
            {
                Id = id;
                Nombre = nombre;
                Texto = texto;
                Duracion = duracion;
            }
        }

        public static readonly FilaCinematica[] Cinematicas =
        {
            new FilaCinematica("C01", "EL RECUERDO", "Un niño corre en el campo con su hermano. Su mamá los llama. Despierta.", 9f),
            new FilaCinematica("C02", "EL MALETÍN", "Los SKULLS dejan un maletín de NOVA para asaltar a un vendedor. Él lo roba sin que lo vean. En su cuarto lo abre: dinero, una llave y un suero. Por la ventana oye que esta noche nadie cuidará la ranura del muro.", 23f),
            new FilaCinematica("C03", "LA DECISIÓN", "Está a punto de escapar. Oye gritos: los SKULLS golpean a la gente del barrio por el maletín. Se da la vuelta.", 22f),
            new FilaCinematica("C04", "EL SUERO", "El Doc le inyecta el suero: ahora aguanta cualquier mejora.", 5f),
            new FilaCinematica("C05", "EL INVENTOR", "El Inventor le mejora la pistola.", 4f), // ya no se usa en la historia (T-59); queda por compatibilidad
            new FilaCinematica("C06", "LA CHICA", "Choca con una chica que lleva a su abuela; la ciudad ya no le da medicinas.", 8f),
            new FilaCinematica("C07", "EL HACKER", "El Hacker conecta la llave: las pandillas son los carceleros de NOVA, y NOVA graba a todos desde el principio.", 6f),
            new FilaCinematica("C08", "EL HERMANO", "En lo alto de la torre lo espera el jefe de NOVA: su hermano.", 6f),
            new FilaCinematica("C09", "LAS PUERTAS", "Abre las puertas de la torre con la llave. Todo se vuelve blanco.", 4f),
        };

        public const string ObjetivoSalir = "SAL A LA CALLE";
        public const string ObjetivoRanura = "VE A LA RANURA DEL MURO";
        public const string ObjetivoDoc = "VE CON EL DOC";
        public const string ObjetivoMejora = "INSTALA UNA MEJORA CON EL DOC";
        public const string ObjetivoTino = "VE CON TINO";
        public const string ObjetivoUsarProtesis = "USA TU PRÓTESIS (TAB)";
        /// <summary>VR20: el mismo objetivo en VR (la prótesis se activa con el gesto o con grip + gatillo izquierdos, no con Tab).</summary>
        public const string ObjetivoUsarProtesisVR = "USA TU PRÓTESIS (GESTO)";
        public const string ObjetivoInventor = "COMPRA UN ARMA CON TINO";
        public const string ObjetivoComer = "COMPRA ALGO EN LA MÁQUINA";
        public const string PensamientoHambre = "Tengo hambre...";
        public const string AvisoMaquina = "ACÉRCATE A LA MÁQUINA Y PULSA {0}";
        public const string AvisoRueda = "MANTÉN {0} Y ELIGE LA PISTOLA PARA EQUIPARLA";
        public const string ObjetivoTianguis = "LIBERA EL TIANGUIS";
        public const string ObjetivoJefeSkulls = "VENCE AL JEFE DE LOS SKULLS";
        public const string ObjetivoNeon = "VE A NEÓN ALTO";
        public const string ObjetivoJefeSnakes = "VENCE AL JEFE DE LOS SNAKES";
        public const string ObjetivoHacker = "VE CON EL HACKER";
        public const string ObjetivoTorre = "VE A LA TORRE DE NOVA";
        public const string ObjetivoJefeNova = "VENCE AL JEFE DE NOVA";
        public const string AvisoSinAtacar = "NO PUEDES CON ELLOS... TODAVÍA";
        public const string AvisoMaletin = "MALETÍN: ";
        public const string AvisoPistola = "PISTOLA MEJORADA";
        public const string TituloFin = "CYBER CITY";
        public const string LineaFin = "¿YA SALISTE?";
        public const string TituloContinuara = "CONTINUARÁ...";
        public const string TeclasContinuara = "ENTER: VOLVER AL TÍTULO";
        public const int DineroMaletin = 20000;
        public const string ZonaNeon = "neon";

        /// <summary>Crea (en memoria) los datos de una cinemática desde la tabla: respaldo cuando el asset no está.</summary>
        public static DatosCinematica CrearRespaldo(string id)
        {
            foreach (FilaCinematica fila in Cinematicas)
            {
                if (fila.Id == id)
                {
                    DatosCinematica datos = ScriptableObject.CreateInstance<DatosCinematica>();
                    datos.name = "DatosCinematica_" + id + "_Respaldo";
                    datos.Configurar(fila.Id, fila.Nombre, fila.Texto, fila.Duracion);
                    return datos;
                }
            }
            return null;
        }

        /// <summary>Los textos de objetivo, para revisar que quepan en el HUD (32 letras, D-tony-240).</summary>
        public static IEnumerable<string> Objetivos()
        {
            yield return ObjetivoSalir;
            yield return ObjetivoRanura;
            yield return ObjetivoDoc;
            yield return ObjetivoMejora;
            yield return ObjetivoInventor;
            yield return ObjetivoTino;
            yield return ObjetivoUsarProtesis;
            yield return ObjetivoUsarProtesisVR;
            yield return ObjetivoComer;
            yield return ObjetivoTianguis;
            yield return ObjetivoJefeSkulls;
            yield return ObjetivoNeon;
            yield return ObjetivoJefeSnakes;
            yield return ObjetivoHacker;
            yield return ObjetivoTorre;
            yield return ObjetivoJefeNova;
        }
    }
}
