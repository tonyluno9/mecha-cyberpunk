using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Efectos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1317 (playtest 17-nov, punto 17): ajustes puntuales a Ciudad_Prologo sin rehacer nada más. Idempotente.
    /// a) todos los barriles y tambos llevan BarrilExplosivo y collider (los que no tenían collider reciben uno);
    /// b) postes de la calle con <see cref="ColocarS2oLuz.IntensidadPoste"/> (solo cambia la intensidad);
    /// c) sin el letrero ENCARGOS afuera de la casa (Tablero_C15a y Tablero_Texto; el tablero invisible para interactuar se queda);
    /// d) sin el letrero NOVA grande ni su haz sobre la torre de Nova.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1317Ajustes.Aplicar
    /// </summary>
    public static class ColocarT1317Ajustes
    {
        public const float IntensidadPosteAntes = 40f;
        private static readonly string[] RaicesQueNoSonBarriles = { "JugadorAPie", "MechaEstacionado" };
        private static readonly string[] LetrerosFuera = { "S2t_Casa/Tablero_C15a", "S2t_Casa/Tablero_Texto", "T1103_Luces/Letrero_NOVA", "T1103_Luces/Haz_Nova" };

        /// <summary>True si es un barril o tambo de la ciudad (no el tambor del revólver del jugador ni nada del mecha).</summary>
        public static bool EsBarrilDeCiudad(Transform t)
        {
            // Un barril dentro de otro (el tambo visual T507 hijo de Barril1) cuenta como parte del barril de arriba.
            return InstaladorBarriles.EsNombreDeBarril(t.name) && (t.parent == null || !InstaladorBarriles.EsNombreDeBarril(t.parent.name)) && !t.name.Contains("Tambor") && !RaicesQueNoSonBarriles.Contains(t.root.name)
                   && t.GetComponentInChildren<Renderer>(true) != null;
        }

        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Barriles();
            Postes(ColocarS2oLuz.IntensidadPoste);
            Letreros();
            ColocarS2.Guardar("t1317-ajustes");
        }

        private static void Barriles()
        {
            int total = 0, sinCollider = 0, nuevos = 0, adoptados = 0;
            // El tambo visual de T-507 reemplazó al cilindro Barril1/2 (con collider) pero quedó como hermano: al reventar el barril se quedaba el tambo. Pasa a ser su hijo.
            foreach (Transform tambo in ColocarS2.Todos().Where(x => x.name.StartsWith("T507_Tambo_Barril")).ToList())
            {
                Transform barril = tambo.parent != null ? tambo.parent.Find(tambo.name.Substring("T507_Tambo_".Length)) : null;
                if (barril != null && barril != tambo.parent)
                {
                    tambo.SetParent(barril, true);
                    adoptados++;
                }
            }
            List<Transform> barriles = ColocarS2.Todos().Where(EsBarrilDeCiudad).ToList();
            foreach (Transform t in barriles)
            {
                total++;
                if (t.GetComponentInChildren<Collider>(true) == null)
                {
                    PonerCollider(t);
                    ColocarS2.Informe.AppendLine("  collider nuevo: " + t.root.name + "/" + t.parent?.name + "/" + t.name + " ahora=" + (t.GetComponentInChildren<Collider>(true) != null));
                    sinCollider++;
                }
                if (t.GetComponent<BarrilExplosivo>() == null && t.GetComponentInParent<BarrilExplosivo>() == null)
                {
                    t.gameObject.AddComponent<BarrilExplosivo>();
                    nuevos++;
                }
            }
            ColocarS2.Informe.AppendLine("Barriles/tambos: " + total + ", con collider nuevo " + sinCollider + ", BarrilExplosivo nuevo " + nuevos + ", tambos visuales T507 pasados a hijos " + adoptados);
        }

        private static void PonerCollider(Transform barril)
        {
            MeshFilter mf = barril.GetComponent<MeshFilter>() ?? barril.GetComponentInChildren<MeshFilter>(true);
            if (mf != null && mf.sharedMesh != null)
            {
                BoxCollider box = mf.gameObject.AddComponent<BoxCollider>();
                box.center = mf.sharedMesh.bounds.center;
                box.size = mf.sharedMesh.bounds.size;
                return;
            }
            // Sin malla utilizable: una caja con los límites del renderer, en el espacio local del barril.
            Renderer r = barril.GetComponentInChildren<Renderer>(true);
            if (r == null)
            {
                ColocarS2.Informe.AppendLine("SIN collider posible: " + barril.name);
                return;
            }
            BoxCollider caja = barril.gameObject.AddComponent<BoxCollider>();
            Bounds b = r.bounds;
            caja.center = barril.InverseTransformPoint(b.center);
            Vector3 e = barril.lossyScale;
            caja.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(e.x)), b.size.y / Mathf.Max(0.0001f, Mathf.Abs(e.y)), b.size.z / Mathf.Max(0.0001f, Mathf.Abs(e.z)));
        }

        /// <summary>Solo cambia la intensidad de las luces de poste (las que reconoce <see cref="ColocarS2oLuz.EsPoste"/>).</summary>
        public static int Postes(float intensidad)
        {
            int n = 0;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ColocarS2oLuz.EsPoste))
            {
                l.intensity = intensidad;
                EditorUtility.SetDirty(l);
                n++;
            }
            ColocarS2.Informe.AppendLine("Postes con intensidad " + intensidad + ": " + n);
            return n;
        }

        private static void Letreros()
        {
            foreach (string ruta in LetrerosFuera)
            {
                Transform t = ColocarS2.Buscar(ruta);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                    ColocarS2.Informe.AppendLine("Quitado " + ruta);
                }
            }
        }

        /// <summary>Capturas de T-1317 (no guarda la escena): casa, torre de Nova y calle de noche antes (40) y después de subir los postes.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1317");
            ColocarS2cLook.Foto(new Vector3(-48f, 1.7f, -82.4f), new Vector3(-57f, 1.5f, -82.4f), "docs/capturas/t-1317/casa-sin-letrero.png", 60f);
            ColocarS2cLook.Foto(new Vector3(94f, 2f, -43f), new Vector3(110f, 30f, -58.8f), "docs/capturas/t-1317/torre-nova-sin-letrero.png", 60f);
            Vector3 poste = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ColocarS2oLuz.EsPoste)
                .First(l => l.name != "x" && Mathf.Abs(l.transform.position.x + 52.88f) < 0.5f).transform.position;
            Vector3 desde = poste + new Vector3(4f, -4.7f, 4f);
            Postes(IntensidadPosteAntes);
            ColocarS2cLook.Foto(desde, poste + new Vector3(0f, -6.4f, 0f), "docs/capturas/t-1317/calle-antes.png", 80f);
            Postes(ColocarS2oLuz.IntensidadPoste);
            ColocarS2cLook.Foto(desde, poste + new Vector3(0f, -6.4f, 0f), "docs/capturas/t-1317/calle-despues.png", 80f);
        }
    }
}
