using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1665 (T46): los drones de patrulla de Nova (<see cref="RutaAerea"/>) son inmunes. Este componente les da una esfera de impacto
    /// (con Rigidbody cinemático para que la evitación de obstáculos de <see cref="RutaAerea"/> no la tome por un muro) y atrapa el daño por
    /// <see cref="IRecibeDanio"/>: no resta vida a nada, muestra un destello holográfico cian (<see cref="EfectoEscudoHolografico"/>) en el punto del
    /// impacto y suena el escudo de Nova. No toca el movimiento del dron.
    /// Se instala solo en todos los <see cref="RutaAerea"/> de la escena al cargarla (<see cref="Instalar"/> también sirve a mano).
    /// Los drones de persecución (DronMundo) NO lo llevan: se pueden derribar a balazos (T-730).
    /// </summary>
    public class EscudoHolograficoDron : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        /// <summary>Sonido del escudo de Nova al recibir un impacto (existe en CatalogoSonidos).</summary>
        public const string IdSonido = "NOV_ESCUDO_IMPACTO";
        public const string NombreEsfera = "EscudoDron_Impacto";
        private const float RadioMinimo = 0.5f;
        private const float RadioMaximo = 3f;
        private const float RadioSinModelo = 1f;
        private const float FraccionDelTamano = 0.6f;

        private static int totalImpactos;
        private static int totalDestellos;
        private static float danioPasado;

        private float ultimoDestello = -1f;
        private float ultimoSonido = -1f;
        private int impactos;
        private int destellos;

        /// <summary>Impactos que ha recibido este dron.</summary>
        public int Impactos => impactos;
        /// <summary>Destellos que ha mostrado este dron.</summary>
        public int Destellos => destellos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            totalImpactos = 0;
            totalDestellos = 0;
            danioPasado = 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlIniciar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            InstalarEnEscena();
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => InstalarEnEscena();

        /// <summary>Instala el escudo en todos los drones de patrulla activos que aún no lo tengan. Devuelve cuántos instaló.</summary>
        public static int InstalarEnEscena()
        {
            int n = 0;
            foreach (RutaAerea ruta in FindObjectsByType<RutaAerea>(FindObjectsSortMode.None))
            {
                // Solo los drones de Nova (Nova_DR..., ver DronesZonasT1398); los de SKULLS, policía y las naves de la ciudad no.
                if (!EsDronNova(ruta.transform))
                {
                    continue;
                }
                if (Instalar(ruta.gameObject) != null)
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>True si el dron (o uno de sus padres) se llama Nova_DR...</summary>
        public static bool EsDronNova(Transform t)
        {
            for (Transform x = t; x != null; x = x.parent)
            {
                if (x.name.StartsWith("Nova_DR", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Le pone el escudo a un dron. Devuelve null si ya lo tiene o si ya recibe daño por otro lado.</summary>
        public static EscudoHolograficoDron Instalar(GameObject dron)
        {
            if (dron == null || dron.GetComponent<EscudoHolograficoDron>() != null || dron.GetComponentInChildren<IRecibeDanio>() != null)
            {
                return null;
            }
            EscudoHolograficoDron escudo = dron.AddComponent<EscudoHolograficoDron>();
            escudo.CrearEsfera();
            return escudo;
        }

        private void CrearEsfera()
        {
            Renderer[] visuales = GetComponentsInChildren<Renderer>();
            Vector3 centro = Vector3.zero;
            float radio = RadioSinModelo;
            if (visuales.Length > 0)
            {
                Bounds b = visuales[0].bounds;
                foreach (Renderer r in visuales)
                {
                    b.Encapsulate(r.bounds);
                }
                centro = transform.InverseTransformPoint(b.center);
                radio = Mathf.Clamp(b.extents.magnitude * FraccionDelTamano, RadioMinimo, RadioMaximo);
            }
            GameObject go = new GameObject(NombreEsfera);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = centro;
            SphereCollider esfera = go.AddComponent<SphereCollider>();
            esfera.radius = radio / Mathf.Max(0.01f, transform.lossyScale.x);
            Rigidbody cuerpo = go.AddComponent<Rigidbody>(); // cinemático: RutaAerea ignora lo que tiene Rigidbody al revisar su paso
            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (info.Cantidad <= 0f || !ReglaEscudoDron.Absorbe(info.Tipo))
            {
                return ResultadoDanio.Ignorado;
            }
            impactos++;
            totalImpactos++;
            if (ReglaEscudoDron.PuedeMostrar(Time.time, ultimoDestello, ReglaEscudoDron.SegundosEntreDestellos))
            {
                ultimoDestello = Time.time;
                destellos++;
                totalDestellos++;
                EfectoEscudoHolografico.Mostrar(info.Punto, transform);
            }
            if (ReglaEscudoDron.PuedeMostrar(Time.time, ultimoSonido, ReglaEscudoDron.SegundosEntreSonidos))
            {
                ultimoSonido = Time.time;
                Sonidos.ReproducirEn(IdSonido, info.Punto);
            }
            // Contrato: bloqueo perfecto con 0 de daño aplicado y sin KO.
            return new ResultadoDanio(0f, TipoBloqueo.Perfecto, false);
        }

        /// <summary>Estado del escudo para comprobar en Play: drones con escudo, impactos, destellos y vida perdida (debe ser 0).</summary>
        public static string DepurarEscudo()
        {
            EscudoHolograficoDron[] todos = FindObjectsByType<EscudoHolograficoDron>(FindObjectsSortMode.None);
            return "[EscudoDron] drones con escudo=" + todos.Length + " | impactos recibidos=" + totalImpactos + " | efectos mostrados=" + totalDestellos
                + " | efectos activos ahora=" + EfectoEscudoHolografico.Activos + " | vida perdida=" + danioPasado + " (intacta)";
        }
    }
}
