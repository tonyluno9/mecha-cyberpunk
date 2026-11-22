using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1645: red de seguridad. Nada que sea un vehículo (carro estacionado o robable, tráfico, patrulla) puede estar fuera del muro ni haber caído
    /// del mapa. El CarroRobable_17 viene colocado en la escena en (-20, -127.8), 40 m más allá del muro, y caía al vacío; este vigilante lo corrige
    /// sin importar quién lo genere. Cada <see cref="SegundosEntreRevisiones"/> s revisa los vehículos: un estacionado o robable fuera se apaga, y uno
    /// que se mueve (tráfico, patrulla) se reubica adentro del muro sobre el piso. Un carro ocupado (el del jugador) no se toca. Sin muro en la escena
    /// (<see cref="AreaMuro.Actual"/> null) solo vigila la caída bajo <see cref="AlturaMinima"/>. No cambia la conducción ni la persecución.
    /// </summary>
    public class VigilanteVehiculosMuro : MonoBehaviour
    {
        public const float SegundosEntreRevisiones = 1f;
        /// <summary>Más abajo que esto (y) un vehículo se dio por caído del mapa.</summary>
        public const float AlturaMinima = -5f;
        /// <summary>Cuánto (m) se puede pasar del área sin contar como fuera: más que la tolerancia del tráfico (3 m) para no tocar a uno dando vuelta.</summary>
        public const float ToleranciaFuera = 6f;
        /// <summary>Cuánto (m) hacia adentro del área se reubica un vehículo.</summary>
        public const float MargenReubicado = 8f;

        public enum Accion { Nada, Apagar, Reubicar }

        private static VigilanteVehiculosMuro instancia;
        private readonly List<GameObject> vistos = new List<GameObject>();
        private float proxima;

        /// <summary>Cuántos vehículos apagó o reubicó este vigilante desde que arrancó la escena (para pruebas).</summary>
        public static int Corregidos { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            Corregidos = 0;
            if (instancia == null)
            {
                GameObject go = new GameObject("VigilanteVehiculosMuro_T1645");
                go.hideFlags = HideFlags.DontSave;
                instancia = go.AddComponent<VigilanteVehiculosMuro>();
                DontDestroyOnLoad(go);
            }
        }

        /// <summary>Lógica pura: qué hacer con un vehículo en <paramref name="posicion"/>. Un carro ocupado nunca se toca.</summary>
        public static Accion Decidir(Vector3 posicion, Rect? area, bool ocupado, bool esProp)
        {
            if (ocupado)
            {
                return Accion.Nada;
            }
            bool fuera = posicion.y < AlturaMinima
                || (area.HasValue && (posicion.x < area.Value.xMin - ToleranciaFuera || posicion.x > area.Value.xMax + ToleranciaFuera
                    || posicion.z < area.Value.yMin - ToleranciaFuera || posicion.z > area.Value.yMax + ToleranciaFuera));
            if (!fuera)
            {
                return Accion.Nada;
            }
            return esProp ? Accion.Apagar : Accion.Reubicar;
        }

        /// <summary>El punto dentro del área más cercano a <paramref name="p"/>, metido <see cref="MargenReubicado"/> m. Sin área devuelve el mismo punto.</summary>
        public static Vector3 PuntoAdentro(Vector3 p, Rect? area)
        {
            if (!area.HasValue)
            {
                return p;
            }
            Rect a = area.Value;
            float m = Mathf.Min(MargenReubicado, Mathf.Min(a.width, a.height) * 0.25f);
            return new Vector3(Mathf.Clamp(p.x, a.xMin + m, a.xMax - m), p.y, Mathf.Clamp(p.z, a.yMin + m, a.yMax - m));
        }

        private void Update()
        {
            if (Time.time < proxima)
            {
                return;
            }
            proxima = Time.time + SegundosEntreRevisiones;
            Revisar();
        }

        /// <summary>Una pasada sobre todos los vehículos de la escena. Devuelve cuántos corrigió.</summary>
        public static int Revisar()
        {
            Rect? area = AreaMuro.Actual();
            int corregidos = 0;
            HashSet<GameObject> hechos = new HashSet<GameObject>();
            foreach (CarroViejo c in Object.FindObjectsByType<CarroViejo>(FindObjectsSortMode.None))
            {
                if (hechos.Add(c.gameObject))
                {
                    corregidos += Corregir(c.gameObject, area, c.Ocupado, c.GetComponent<CarroTrafico>() == null && c.GetComponent<PatrullaMundo>() == null);
                }
            }
            foreach (CarroTrafico c in Object.FindObjectsByType<CarroTrafico>(FindObjectsSortMode.None))
            {
                if (hechos.Add(c.gameObject))
                {
                    corregidos += Corregir(c.gameObject, area, false, false);
                }
            }
            foreach (PatrullaMundo p in Object.FindObjectsByType<PatrullaMundo>(FindObjectsSortMode.None))
            {
                if (hechos.Add(p.gameObject))
                {
                    corregidos += Corregir(p.gameObject, area, false, false);
                }
            }
            Corregidos += corregidos;
            return corregidos;
        }

        private static int Corregir(GameObject go, Rect? area, bool ocupado, bool esProp)
        {
            Accion accion = Decidir(go.transform.position, area, ocupado, esProp);
            if (accion == Accion.Nada)
            {
                return 0;
            }
            Vector3 antes = go.transform.position;
            if (accion == Accion.Reubicar)
            {
                Vector3 destino = PuntoAdentro(antes, area);
                Vector3 origen = new Vector3(destino.x, 3f, destino.z);
                if (Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, 60f, ~0, QueryTriggerInteraction.Ignore) && golpe.point.y > AlturaMinima)
                {
                    Rigidbody cuerpo = go.GetComponent<Rigidbody>();
                    go.transform.position = new Vector3(destino.x, golpe.point.y + 0.5f, destino.z);
                    if (cuerpo != null && !cuerpo.isKinematic)
                    {
                        cuerpo.linearVelocity = Vector3.zero;
                        cuerpo.angularVelocity = Vector3.zero;
                    }
                    Physics.SyncTransforms();
                    Debug.LogWarning("[Trafico] T-1645: " + go.name + " estaba fuera del muro o caído en " + antes + "; reubicado en " + go.transform.position + ".");
                    return 1;
                }
            }
            Debug.LogWarning("[Trafico] T-1645: " + go.name + " estaba fuera del muro o caído en " + antes + "; apagado.");
            go.SetActive(false);
            return 1;
        }
    }
}
