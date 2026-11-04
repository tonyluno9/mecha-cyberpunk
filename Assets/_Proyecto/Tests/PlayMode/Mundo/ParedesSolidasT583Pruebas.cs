using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-583: en la ciudad, toda pared grande (edificios, muros, fachadas) tiene un collider sólido que la cubre; si no, el jugador a pie la atraviesa
    /// ("jugador metido en paredes"). Mide qué estructuras grandes visibles no tienen collider dentro de sus límites.
    /// </summary>
    public class ParedesSolidasT583Pruebas
    {
        private const float AnchoMinimo = 3f;
        private const float AlturaMinima = 3f;
        private const float AlturaMaximaDelPiso = 1f;
        private const float FraccionDelCentro = 0.5f;
        private const float GrosorMinimo = 0.5f;        // ventanas, grafitis y recubrimientos de interior son láminas: la pared de verdad va debajo
        private const float AlturaAerea = 6f;           // letreros, grúas y pantallas en lo alto: nadie camina ahí

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EscenaLugares.CargarCiudad();
            yield return EscenaLugares.Calentar();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            EscenaLugares.Descargar();
        }

        private static bool EsEstructura(MeshRenderer r)
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.GetComponent<MeshFilter>() == null || r.GetComponent<MeshFilter>().sharedMesh == null)
            {
                return false;
            }
            Vector3 t = r.bounds.size;
            bool ancho = Mathf.Max(t.x, t.z) >= AnchoMinimo && Mathf.Min(t.x, t.z) >= GrosorMinimo;
            return ancho && t.y >= AlturaMinima && t.y < 200f
                && r.GetComponentInParent<Rigidbody>() == null && r.GetComponentInParent<CharacterController>() == null && r.GetComponentInParent<Animator>() == null;
        }

        [UnityTest]
        public IEnumerator Ciudad_TodaEstructuraGrandeTieneColliderSolido()
        {
            yield return null;
            List<string> sin = new List<string>();
            int total = 0;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!EsEstructura(r))
                {
                    continue;
                }
                if (r.bounds.min.y > AlturaAerea || EsPasoDeEntrada(r.transform))
                {
                    continue;
                }
                total++;
                Bounds b = r.bounds;
                Collider[] dentro = Physics.OverlapBox(b.center, b.extents * FraccionDelCentro, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (dentro.Length == 0)
                {
                    sin.Add(Ruta(r.transform) + " " + b.size.ToString("F1") + " @ " + b.center.ToString("F0"));
                }
            }
            Debug.Log("[T-583] estructuras grandes " + total + ", sin collider " + sin.Count + "\n" + string.Join("\n", sin.Take(80)));
            Assert.IsEmpty(sin, "Estructuras grandes sin collider (" + sin.Count + " de " + total + "): " + string.Join("; ", sin.Take(25)));
        }

        // Los marcos de las entradas (ArteNoche/Entradas) enmarcan un paso: se cruzan a propósito.
        private static bool EsPasoDeEntrada(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name == "Entradas")
                {
                    return true;
                }
            }
            return false;
        }

        private static string Ruta(Transform t)
        {
            string ruta = t.name;
            for (int i = 0; i < 3 && t.parent != null; i++)
            {
                t = t.parent;
                ruta = t.name + "/" + ruta;
            }
            return ruta;
        }
    }
}
