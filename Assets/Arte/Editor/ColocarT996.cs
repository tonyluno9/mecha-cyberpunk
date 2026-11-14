using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-996: arregla las escaleras de las pasarelas PS01 de Neón Alto. El primer peldaño del modelo queda a 0.9 m de la banqueta (el jugador a pie
    /// solo sube 0.35 m) y varios props (neones N07, cargadores CY10, tambos y huacales) tapaban el pie de las escaleras.
    /// Pone una rampa invisible (collider) al pie de cada escalera y apaga lo que estorba en la entrada. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT996.Escaleras
    /// </summary>
    public static class ColocarT996
    {
        public const string NombreRampas = "T996_RampasEscalera";
        private const string NombrePasarela = "PS01_Pasarela";
        /// <summary>Centro de cada escalera respecto al borde de la pasarela en x (m).</summary>
        private const float CentroEscalera = 2f;
        /// <summary>Ancho de la rampa (m): lo que mide la escalera entre sus laterales.</summary>
        private const float AnchoRampa = 3.2f;
        /// <summary>Metros de banqueta antes del primer peldaño donde arranca la rampa.</summary>
        private const float ArranqueRampa = 1.0f;
        /// <summary>Altura del primer peldaño del modelo sobre la banqueta (m) y su distancia al borde de la caja.</summary>
        private const float AlturaPrimerPeldano = 0.92f;
        private const float DistanciaPrimerPeldano = 0.6f;
        private const float GrosorRampa = 0.3f;
        /// <summary>Zona de entrada que debe quedar libre (m): antes del pie y sobre los primeros peldaños.</summary>
        private const float LibreAntes = 1.6f;
        private const float LibreDespues = 2.0f;
        private const float LibreAlto = 3f;
        private const float MaxProp = 4f;

        [MenuItem("Mecha Cyberpunk/T-996/Arreglar escaleras de las pasarelas")]
        public static void Escaleras()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform[] pasarelas = ColocarS2.Todos().Where(t => t.name == NombrePasarela && t.gameObject.activeInHierarchy).ToArray();
            foreach (Transform p in pasarelas)
            {
                Transform viejas = p.Find(NombreRampas);
                if (viejas != null)
                {
                    Object.DestroyImmediate(viejas.gameObject);
                }
            }
            Physics.SyncTransforms();
            foreach (Transform p in pasarelas)
            {
                Collider[] cols = p.GetComponentsInChildren<Collider>();
                if (cols.Length == 0)
                {
                    ColocarS2.Informe.AppendLine("PS01 " + p.position.ToString("F1") + " sin collider");
                    continue;
                }
                Bounds caja = cols[0].bounds;
                foreach (Collider c in cols)
                {
                    caja.Encapsulate(c.bounds);
                }
                float suelo = SueloAl(new Vector3(caja.min.x + CentroEscalera, 0f, caja.min.z - 1f), p);
                Transform rampas = new GameObject(NombreRampas).transform;
                rampas.SetParent(p, false);
                foreach (float x in new[] { caja.min.x + CentroEscalera, caja.max.x - CentroEscalera })
                {
                    PonerRampa(rampas, new Vector3(x, suelo, caja.min.z));
                    Despejar(p, new Vector3(x, suelo, caja.min.z));
                }
            }
            Physics.SyncTransforms();
            ColocarS2.Guardar("t996-escaleras");
        }

        private static float SueloAl(Vector3 xz, Transform pasarela)
        {
            float mejor = float.NegativeInfinity;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(xz.x, 3f, xz.z), Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider.transform.IsChildOf(pasarela) && h.collider.attachedRigidbody == null && h.point.y < 1f)
                {
                    mejor = Mathf.Max(mejor, h.point.y);
                }
            }
            return float.IsNegativeInfinity(mejor) ? 0.2f : mejor;
        }

        /// <summary>Cuña de collider desde la banqueta hasta el primer peldaño; la cara de arriba une los dos puntos.</summary>
        private static void PonerRampa(Transform padre, Vector3 pie)
        {
            Vector3 abajo = new Vector3(pie.x, pie.y, pie.z - ArranqueRampa);
            Vector3 arriba = new Vector3(pie.x, pie.y + AlturaPrimerPeldano, pie.z + DistanciaPrimerPeldano);
            Vector3 dir = arriba - abajo;
            float largo = dir.magnitude;
            Quaternion giro = Quaternion.LookRotation(dir.normalized, Vector3.up);
            GameObject g = new GameObject("Rampa");
            g.transform.SetParent(padre, true);
            BoxCollider caja = g.AddComponent<BoxCollider>();
            caja.size = new Vector3(AnchoRampa, GrosorRampa, largo);
            // El centro queda bajo la línea para que la cara de arriba pase por abajo y arriba.
            g.transform.rotation = giro;
            g.transform.position = (abajo + arriba) * 0.5f - giro * Vector3.up * (GrosorRampa * 0.5f);
            ColocarS2.Informe.AppendLine("Rampa en " + abajo.ToString("F2") + " a " + arriba.ToString("F2"));
        }

        /// <summary>Apaga los props chicos que caen en la entrada de la escalera (neones, cargadores, tambos, huacales).</summary>
        private static void Despejar(Transform pasarela, Vector3 pie)
        {
            Vector3 centro = new Vector3(pie.x, pie.y + 0.05f + LibreAlto * 0.5f, pie.z + (LibreDespues - LibreAntes) * 0.5f);
            Vector3 mitad = new Vector3(AnchoRampa * 0.5f, LibreAlto * 0.5f, (LibreAntes + LibreDespues) * 0.5f);
            HashSet<GameObject> apagar = new HashSet<GameObject>();
            foreach (Collider c in Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(pasarela))
                {
                    continue;
                }
                Vector3 t = c.bounds.size;
                if (t.y > MaxProp || Mathf.Max(t.x, t.z) > MaxProp || c.bounds.min.y < pie.y - 0.1f && t.x > 2f && t.z > 2f)
                {
                    ColocarS2.Informe.AppendLine("  se deja (grande o suelo): " + c.name + " " + t.ToString("F1"));
                    continue;
                }
                apagar.Add(c.gameObject);
            }
            foreach (GameObject g in apagar)
            {
                ColocarS2.Informe.AppendLine("  se apaga " + g.name + " (" + g.transform.root.name + ") en " + g.transform.position.ToString("F1"));
                g.SetActive(false);
            }
        }
    }
}
