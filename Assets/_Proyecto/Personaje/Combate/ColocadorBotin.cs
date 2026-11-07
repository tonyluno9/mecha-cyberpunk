using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Pone en el piso el botín de los enemigos que caen (T-686): su arma (<see cref="ArmaEnPiso"/>, se recoge con E) y su dinero
    /// (<see cref="DineroEnPiso"/>, se recoge al pasar). Atiende <see cref="EventosBotin"/>; los modelos salen de
    /// Resources/DatosBotin_Base. Se suscribe una vez al arrancar el juego.
    /// </summary>
    public static class ColocadorBotin
    {
        public const string NombreDatos = "DatosBotin_Base";
        private const float AlturaRayo = 2f;
        private const float AlcanceRayo = 6f;
        private const float AlturaArma = 0.1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AlArrancar()
        {
            EventosBotin.Soltado -= Colocar;
            EventosBotin.Soltado += Colocar;
        }

        /// <summary>Suscribe el colocador (para pruebas que limpiaron los suscriptores).</summary>
        public static void Suscribir()
        {
            AlArrancar();
        }

        private static void Colocar(InfoBotin info)
        {
            DatosBotin datos = Resources.Load<DatosBotin>(NombreDatos);
            Vector3 piso = BuscarPiso(info.Posicion);
            float separacion = datos != null ? datos.Separacion : 1f;
            float vida = datos != null ? datos.SegundosEnPiso : 120f;
            Vector3 lado = Vector3.right * separacion;
            if (info.Dinero > 0)
            {
                DineroEnPiso dinero = DineroEnPiso.Crear(piso - lado * 0.5f, info.Dinero);
                Object.Destroy(dinero.gameObject, vida);
            }
            if (info.Arma != ArmaBotin.Ninguna && datos != null)
            {
                SoltarArma(datos, info.Arma, piso + lado * 0.5f, vida);
            }
        }

        private static void SoltarArma(DatosBotin datos, ArmaBotin tipo, Vector3 piso, float vida)
        {
            ArmaAPie arma = tipo == ArmaBotin.Escopeta ? ArmaAPie.Escopeta : ArmaAPie.Pistola;
            foreach (ArmaEnPiso existente in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None))
            {
                if (existente != null && existente.Arma == arma && (existente.transform.position - piso).sqrMagnitude < datos.DistanciaArmaRepetida * datos.DistanciaArmaRepetida)
                {
                    return;
                }
            }
            GameObject objeto;
            Vector3 punto = piso + Vector3.up * AlturaArma;
            Quaternion giro = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (tipo == ArmaBotin.Escopeta && datos.PrefabEscopeta != null)
            {
                objeto = Object.Instantiate(datos.PrefabEscopeta, punto, giro);
            }
            else if (datos.ModeloPistola != null)
            {
                objeto = new GameObject("PistolaEnPiso");
                objeto.transform.SetPositionAndRotation(punto, giro);
                GameObject modelo = Object.Instantiate(datos.ModeloPistola, objeto.transform);
                modelo.name = "W01";
                modelo.transform.localPosition = Vector3.up * 0.03f;
                modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                objeto.AddComponent<ArmaEnPiso>();
            }
            else
            {
                return;
            }
            Object.Destroy(objeto, vida);
        }

        private static Vector3 BuscarPiso(Vector3 posicion)
        {
            if (Physics.Raycast(posicion + Vector3.up * AlturaRayo, Vector3.down, out RaycastHit h, AlcanceRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return h.point;
            }
            return posicion;
        }
    }
}
