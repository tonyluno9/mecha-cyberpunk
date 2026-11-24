using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-historia-libre-23nov (escalada progresiva, 24-nov): una fase de la escalada del modo libre. Dura <see cref="Segundos"/> o hasta que matas
    /// <see cref="Muertes"/> de <see cref="TiposMuertes"/> (o <see cref="MuertesAlterna"/> de <see cref="TiposMuertesAlterna"/>): lo que pase primero
    /// sube a la siguiente. Mientras dura, el director de respuesta solo genera <see cref="Tipos"/> y las estrellas se sostienen en <see cref="Estrellas"/>.
    /// Los datos viven en <see cref="ParametrosRespuesta.FasesLibre"/> (ConfigRespuesta).
    /// </summary>
    [Serializable]
    public class FaseModoLibre
    {
        [Tooltip("Nombre para la consola y el reporte.")]
        public string Nombre = string.Empty;
        [Tooltip("Tiempo máximo de la fase (s); 0 = sin fin (la última).")]
        [Min(0f)] public float Segundos;
        [Tooltip("Bajas de TiposMuertes que suben a la siguiente fase; 0 = sin esa condición.")]
        [Min(0)] public int Muertes;
        [Tooltip("Qué bajas cuentan para Muertes.")]
        public TipoRespuesta[] TiposMuertes = new TipoRespuesta[0];
        [Tooltip("Segunda condición de bajas (por ejemplo patrullas inutilizadas); 0 = sin ella.")]
        [Min(0)] public int MuertesAlterna;
        [Tooltip("Qué bajas cuentan para MuertesAlterna.")]
        public TipoRespuesta[] TiposMuertesAlterna = new TipoRespuesta[0];
        [Tooltip("Qué unidades de respuesta llegan en esta fase.")]
        public TipoRespuesta[] Tipos = new TipoRespuesta[0];
        [Tooltip("Estrellas que se sostienen durante la fase (1 a 5).")]
        [Range(1, 5)] public int Estrellas = 1;
        [Tooltip("True: NOVA toma el lugar de la policía (la policía que queda se retira y los drones llegan en cápsula).")]
        public bool ModoNova;

        public FaseModoLibre()
        {
        }

        public FaseModoLibre(string nombre, float segundos, int estrellas, bool modoNova, TipoRespuesta[] tipos,
            int muertes, TipoRespuesta[] tiposMuertes, int muertesAlterna = 0, TipoRespuesta[] tiposMuertesAlterna = null)
        {
            Nombre = nombre;
            Segundos = segundos;
            Estrellas = estrellas;
            ModoNova = modoNova;
            Tipos = tipos ?? new TipoRespuesta[0];
            Muertes = muertes;
            TiposMuertes = tiposMuertes ?? new TipoRespuesta[0];
            MuertesAlterna = muertesAlterna;
            TiposMuertesAlterna = tiposMuertesAlterna ?? new TipoRespuesta[0];
        }

        /// <summary>True si la fase deja generar este tipo.</summary>
        public bool Permite(TipoRespuesta tipo) => Contiene(Tipos, tipo);

        public static bool Contiene(TipoRespuesta[] lista, TipoRespuesta tipo)
        {
            if (lista == null)
            {
                return false;
            }
            for (int i = 0; i < lista.Length; i++)
            {
                if (lista[i] == tipo)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
