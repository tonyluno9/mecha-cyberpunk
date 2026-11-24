using UnityEngine;

namespace MechaCyberpunk.VR.Ruta
{
    /// <summary>VR30: un chevrón de la ruta: dónde va, hacia dónde apunta (en el plano) y a cuántos metros de los pies está sobre la ruta.</summary>
    public readonly struct PuntoRutaVR
    {
        public readonly Vector3 Posicion;
        public readonly Vector3 Direccion;
        public readonly float Metros;

        public PuntoRutaVR(Vector3 posicion, Vector3 direccion, float metros)
        {
            Posicion = posicion;
            Direccion = direccion;
            Metros = metros;
        }
    }
}
