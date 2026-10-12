using UnityEngine;

namespace MechaCyberpunk.Mecha.Llamado
{
    /// <summary>Copia de los datos de un punto de llamado (el contrato IPuntoLlamadoMecha) para decidir sin depender del mundo.</summary>
    public readonly struct PuntoLlamado
    {
        public readonly Pose Llegada;
        public readonly float Radio;
        public readonly bool Habilitado;

        public PuntoLlamado(Pose llegada, float radio, bool habilitado)
        {
            Llegada = llegada;
            Radio = radio;
            Habilitado = habilitado;
        }
    }
}
