using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-858 (playtest 6-nov tarde): los dos callejones sin salida (los encontró <see cref="MapaPasoS2t"/>) ahora llegan a otra calle.
    /// - El del mercado (x = -25, de z 10 a 45) chocaba con los edificios 13 y 14 de la manzana 2 (6 m de ancho): se quitan con las piezas de
    ///   fachada que vivían sobre su huella, y el callejón sale derecho a la calle de la orilla norte (z = 85).
    /// - El de la casa (z = -60, de x -55 a -87) terminaba en una reja contra los edificios 39 y 36 de la manzana 4: se les recortan
    ///   <see cref="Recorte"/> m de su lado norte (la cara y sus fachadas se recorren a la orilla nueva), se quita la reja y el callejón sigue
    ///   hasta la calle de la orilla oeste (x = -133).
    /// Idempotente: los recortes se miden desde el lado sur de cada edificio, que no se mueve.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tSalidas.Colocar
    /// </summary>
    public static class ColocarS2tSalidas
    {
        public static readonly string[] Quitar = { "Barrio/Manzana_2/Edificios/Edificio_13", "Barrio/Manzana_2/Edificios/Edificio_14", "Barrio/Manzana_4/Callejon/L05_RejaPorton_Visual_T250" };
        /// <summary>Huella de los edificios 13 y 14 (las piezas de fachada de ArteNoche que caen ahí se apagan).</summary>
        public static readonly Bounds HuellaMercado = new Bounds(new Vector3(-22f, 15f, 64.5f), new Vector3(6.4f, 40f, 39.5f));
        public static readonly string[] Recortar = { "Barrio/Manzana_4/Edificios/Edificio_39", "Barrio/Manzana_4/Edificios/Edificio_36" };
        /// <summary>Orilla sur fija de los edificios recortados y la orilla norte que les queda (z).</summary>
        public const float SurRecortados = -84f;
        public const float NorteOriginal = -59f;
        public const float Recorte = 5f;
        public static float NorteNuevo => NorteOriginal - Recorte;

        [MenuItem("Mecha Cyberpunk/S2t/5 Callejones con salida")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            foreach (string r in Quitar)
            {
                Transform t = ColocarS2.Buscar(r);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
                ColocarS2.Informe.AppendLine((t != null ? "apagado " : "ya no está / FALTA ") + r);
            }
            int piezas = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && HuellaMercado.Contains(r.bounds.center) && r.transform.root.name == "Barrio" && RutaDe(r.transform).Contains("/ArteNoche/")).ToArray())
            {
                Pieza(r.transform).gameObject.SetActive(false);
                piezas++;
            }
            ColocarS2.Informe.AppendLine("piezas de fachada sobre la huella de 13 y 14: " + piezas);

            foreach (string r in Recortar)
            {
                Transform e = ColocarS2.Buscar(r);
                if (e == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA " + r);
                    continue;
                }
                RecortarNorte(e);
            }
            ColocarS2.Guardar("s2t-salidas");
        }

        private static string RutaDe(Transform t) => t.parent == null ? t.name : RutaDe(t.parent) + "/" + t.name;

        // La pieza a apagar: el objeto del modelo (si el renderer es su "Visual"), no solo la malla.
        private static Transform Pieza(Transform t) => t.name == "Visual" && t.parent != null ? t.parent : t;

        // Deja la caja del edificio de SurRecortados a NorteNuevo y recorre a la orilla nueva lo que estaba en su cara norte.
        private static void RecortarNorte(Transform edificio)
        {
            Transform cuerpo = edificio.Find("Cuerpo");
            Renderer rc = cuerpo.GetComponent<Renderer>();
            Bounds antes = rc.bounds;
            float delta = NorteNuevo - antes.max.z;
            if (Mathf.Abs(delta) < 0.01f)
            {
                ColocarS2.Informe.AppendLine(edificio.name + ": ya recortado");
                return;
            }
            Vector3 escala = cuerpo.localScale;
            float largoNuevo = NorteNuevo - SurRecortados;
            // La caja es un cubo de 1 m escalado: el largo en z del mundo sale de su escala (sin giro).
            cuerpo.localScale = new Vector3(escala.x, escala.y, escala.z * largoNuevo / antes.size.z);
            cuerpo.position += new Vector3(0f, 0f, (SurRecortados + NorteNuevo) * 0.5f - rc.bounds.center.z);
            int recorridas = 0, apagadas = 0;
            List<Transform> cosas = edificio.GetComponentsInChildren<Renderer>(true).Select(r => Pieza(r.transform)).Where(t => t != cuerpo).Distinct().ToList();
            cosas.AddRange(Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && RutaDe(r.transform).Contains("/ArteNoche/") && r.bounds.center.x > antes.min.x && r.bounds.center.x < antes.max.x
                    && r.bounds.center.z > NorteNuevo - 0.5f && r.bounds.center.z < antes.max.z + 1.5f
                    // La junta: lo que sale hacia el sur desde la hilera de enfrente (pegada en z = NorteOriginal) es de ella, no se toca.
                    && !(r.bounds.center.z > antes.max.z - 0.8f && r.bounds.center.z < antes.max.z - 0.05f))
                .Select(r => Pieza(r.transform)));
            foreach (Transform t in cosas.Distinct())
            {
                Renderer r = t.GetComponentInChildren<Renderer>();
                if (r == null)
                {
                    continue;
                }
                Bounds b = r.bounds;
                bool enCaraNorte = b.size.z < 1.5f && b.min.z > antes.max.z - 0.6f && b.center.z >= antes.max.z - 0.05f;
                if (enCaraNorte)
                {
                    t.position += new Vector3(0f, 0f, delta);
                    recorridas++;
                }
                else if (b.center.z > NorteNuevo)
                {
                    t.gameObject.SetActive(false);
                    apagadas++;
                }
            }
            ColocarS2.Informe.AppendLine(edificio.name + ": recortado a z " + SurRecortados + ".." + NorteNuevo + "; " + recorridas + " piezas recorridas a la cara nueva, " + apagadas + " apagadas");
        }
    }
}
