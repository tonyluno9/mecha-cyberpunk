using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Lógica pura del turno de ataque de una pandilla: por cada lado solo un peleador a la vez puede preparar un golpe.
    /// <see cref="Pedir"/> da el turno si está libre o ya es de quien lo pide. Si quien lo tiene queda en KO, se desactiva
    /// o se destruye, el turno se libera solo.
    /// </summary>
    public class TurnosAtaque
    {
        private readonly Dictionary<LadoPelea, IPeleador> duenos = new Dictionary<LadoPelea, IPeleador>();

        /// <summary>Instancia compartida por la escena; se limpia al entrar a Play.</summary>
        public static TurnosAtaque Compartido { get; } = new TurnosAtaque();

        /// <summary>Pide el turno de su lado. Devuelve true si ahora es suyo.</summary>
        public bool Pedir(IPeleador peleador)
        {
            if (peleador == null)
            {
                return false;
            }
            LadoPelea lado = peleador.Lado;
            if (duenos.TryGetValue(lado, out IPeleador dueno) && dueno != peleador && EstaVigente(dueno))
            {
                return false;
            }
            duenos[lado] = peleador;
            return true;
        }

        /// <summary>Suelta el turno si era suyo. Si no lo tenía, no hace nada.</summary>
        public void Soltar(IPeleador peleador)
        {
            if (peleador != null && duenos.TryGetValue(peleador.Lado, out IPeleador dueno) && dueno == peleador)
            {
                duenos.Remove(peleador.Lado);
            }
        }

        /// <summary>Quién tiene el turno del lado, o null si está libre (o su dueño ya no vale).</summary>
        public IPeleador Dueno(LadoPelea lado)
        {
            if (duenos.TryGetValue(lado, out IPeleador dueno) && EstaVigente(dueno))
            {
                return dueno;
            }
            duenos.Remove(lado);
            return null;
        }

        public bool Tiene(IPeleador peleador) => peleador != null && Dueno(peleador.Lado) == peleador;

        public void Limpiar() => duenos.Clear();

        private static bool EstaVigente(IPeleador dueno)
        {
            if (dueno is Object objeto && objeto == null)
            {
                return false;
            }
            Transform raiz = dueno.Raiz;
            if (raiz == null || !raiz.gameObject.activeInHierarchy)
            {
                return false;
            }
            return dueno.Vida == null || !dueno.Vida.EstaKO;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Compartido.Limpiar();
    }
}
