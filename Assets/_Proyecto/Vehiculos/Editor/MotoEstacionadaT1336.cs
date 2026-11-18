using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1336: la moto V08 estacionada en la ciudad se atora a ~3 m al robarla (CiudadCarrosPruebas). Causa probable: ColocarT511.Cabe barre 10 m hacia adelante con una caja
    /// que va de 1.1 a 2.3 m sobre el piso (pensada para un carro); algo bajo (barril, bolardo, barrera, banqueta alta) por delante no lo ve, y la moto (230 kg)
    /// no lo empuja. Aquí se barre a la altura del cuerpo de la moto (de su carrocería hacia arriba) y se reacomoda la que tenga algo adelante.
    /// </summary>
    public static class MotoEstacionadaT1336
    {
        public const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const float MetrosLibres = 10f;
        /// <summary>Lo que de verdad atora: algo a menos de esto al frente (a 7 m de un carro la moto sale bien al robarla).</summary>
        public const float MetrosQueAtoran = 4f;
        private const float AltoInferior = 0.35f;
        private const float AltoSuperior = 1.4f;
        private const float MedioAncho = 0.5f;
        private const float AdelanteDelCentro = 1.15f;

        /// <summary>True si hay <see cref="MetrosLibres"/> sin nada por delante del morro de la moto (ignora sus propios colliders y los triggers).</summary>
        public static bool AdelanteLibre(Transform moto, out string choque) => AdelanteLibre(moto, MetrosLibres, out choque);

        public static bool AdelanteLibre(Transform moto, float metros, out string choque)
        {
            Physics.SyncTransforms();
            float medioAlto = (AltoSuperior - AltoInferior) * 0.5f;
            Vector3 origen = moto.position + Vector3.up * (AltoInferior + medioAlto) + moto.forward * AdelanteDelCentro;
            RaycastHit[] golpes = Physics.BoxCastAll(origen, new Vector3(MedioAncho, medioAlto, 0.05f), moto.forward, moto.rotation, metros,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            choque = null;
            float mejor = float.MaxValue;
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.transform.IsChildOf(moto) || g.distance >= mejor)
                {
                    continue;
                }
                mejor = g.distance;
                choque = g.collider.name + " a " + g.distance.ToString("F1") + " m";
            }
            return choque == null;
        }

        /// <summary>True si el cuerpo de la moto no se encima con nada en su lugar.</summary>
        private static bool Cabe(Transform moto)
        {
            Physics.SyncTransforms();
            float medioAlto = (AltoSuperior - AltoInferior) * 0.5f;
            Vector3 centro = moto.position + Vector3.up * (AltoInferior + medioAlto);
            foreach (Collider otro in Physics.OverlapBox(centro, new Vector3(MedioAncho, medioAlto, 1.1f), moto.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!otro.transform.IsChildOf(moto))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Las motos estacionadas de la escena abierta.</summary>
        public static List<CarroEstacionado> Motos()
        {
            return Object.FindObjectsByType<CarroEstacionado>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.GetComponent<ManejoMoto>() != null).OrderBy(c => c.name).ToList();
        }

        /// <summary>Abre Ciudad_Prologo y a cada moto con algo por delante la gira 180 grados o la corre a lo largo de su eje hasta que tenga 10 m libres. Guarda la escena.</summary>
        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaCiudad, OpenSceneMode.Single);
            float[] corrimientos = { 0f, 1.5f, -1.5f, 3f, -3f, 4.5f, -4.5f, 6f, -6f, 9f, -9f };
            bool cambio = false;
            foreach (CarroEstacionado moto in Motos())
            {
                Transform t = moto.transform;
                if (AdelanteLibre(t, MetrosQueAtoran, out string choque))
                {
                    Debug.Log("[Vehiculos] T-1336: " + moto.name + " no tiene nada a menos de " + MetrosQueAtoran + " m al frente.");
                    continue;
                }
                Vector3 origen = t.position;
                Quaternion giro = t.rotation;
                bool puesta = false;
                foreach (float corrimiento in corrimientos)
                {
                    foreach (float vuelta in new[] { 0f, 180f, 90f, 270f })
                    {
                        Quaternion rotacion = giro * Quaternion.Euler(0f, vuelta, 0f);
                        t.SetPositionAndRotation(origen + rotacion * Vector3.forward * corrimiento, rotacion);
                        if (Cabe(t) && AdelanteLibre(t, out _))
                        {
                            puesta = true;
                            break;
                        }
                    }
                    if (puesta)
                    {
                        break;
                    }
                }
                if (puesta)
                {
                    cambio = true;
                    Debug.Log("[Vehiculos] T-1336: " + moto.name + " tenía " + choque + " por delante; ahora en " + t.position.ToString("F1") + " giro " + t.eulerAngles.y.ToString("F0") + ".");
                }
                else
                {
                    t.SetPositionAndRotation(origen, giro);
                    Debug.LogWarning("[Vehiculos] T-1336: " + moto.name + " no encontró lugar con " + MetrosLibres + " m libres (choque: " + choque + ").");
                }
            }
            if (cambio)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
            }
        }
    }
}
