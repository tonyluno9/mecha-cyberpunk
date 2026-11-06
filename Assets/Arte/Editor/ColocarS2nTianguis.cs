using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-802 (noche 6-nov, sesión 2): la pelea del Tianguis en menos de 200 s. Los SKULLS del Tianguis eran dos grupos: uno junto al
    /// marcador (3, uno parado arriba de un puesto a 3.4 m) y otro a 180 m, en la otra punta de la zona, donde nadie lo busca. De día sale
    /// uno por grupo y cada uno reaparece a los 40 s con el jugador lejos: siete SKULLS eran siete vueltas de 40 s. Ahora hay cuatro grupos
    /// en la calle del Tianguis, a la vista del marcador y a nivel de calle (3 + 2 + 1 + 1 = los 7 de "VENCE A LOS SKULLS", todos de noche;
    /// de día sale uno por grupo: 4 por vuelta, dos vueltas). Los puestos no se tocan. Idempotente: los grupos 3 y 4 se rehacen cada vez.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nTianguis.Colocar
    /// </summary>
    public static class ColocarS2nTianguis
    {
        private const string RutaGrupos = "GTA_T402";
        private const string Grupo1 = "Pandilla_Calaveras_1";
        private const string Grupo2 = "Pandilla_Calaveras_2";
        private const string Grupo3 = "Pandilla_Calaveras_Tianguis3";
        private const string Grupo4 = "Pandilla_Calaveras_Tianguis4";
        private const string RutaDirector = "GTA_T406";
        private const float SeparacionPandilleros = 1.8f;
        private const float RadioBusqueda = 8f;
        private const float PasoBusqueda = 0.5f;
        private const float AlturaCalle = 0.6f;
        private const float RadioCuerpo = 0.45f;

        // Junto a la calle del Tianguis por donde se llega al marcador (x ≈ -43), dentro de la zona (x > -47) y a más de 35 m de la retirada.
        private static readonly Vector3 Centro2 = new Vector3(-43f, 0f, 76f);
        private static readonly Vector3 Centro3 = new Vector3(-43f, 0f, 97f);
        private static readonly Vector3 Centro4 = new Vector3(-43f, 0f, 108f);

        [MenuItem("Mecha Cyberpunk/S2n/4 Tianguis (SKULLS junto al marcador)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform raiz = ColocarS2.Buscar(RutaGrupos);
            Transform g1 = raiz.Find(Grupo1);
            Transform g2 = raiz.Find(Grupo2);
            foreach (string nombre in new[] { Grupo3, Grupo4 })
            {
                Transform viejo = raiz.Find(nombre);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }

            // Grupo 1: el mismo lugar, pero cada uno parado en la calle (no arriba de un puesto).
            foreach (Transform p in Pandilleros(g1))
            {
                Bajar(p, p.position);
            }
            Mover(g2, Centro2);
            Transform g3 = Copia(g2, raiz, Grupo3, Centro3);
            Transform g4 = Copia(g2, raiz, Grupo4, Centro4);

            MonoBehaviour director = ColocarS2.Buscar(RutaDirector).GetComponents<MonoBehaviour>().First(m => m != null && m.GetType().Name == "DirectorPandillas");
            SerializedObject so = new SerializedObject(director);
            SerializedProperty grupos = so.FindProperty("gruposTianguis");
            Transform[] todos = { g1, g2, g3, g4 };
            grupos.arraySize = todos.Length;
            for (int i = 0; i < todos.Length; i++)
            {
                grupos.GetArrayElementAtIndex(i).objectReferenceValue = todos[i].gameObject;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (Transform g in todos)
            {
                ColocarS2.Informe.AppendLine(g.name + " " + g.position + ": " + string.Join(" ", Pandilleros(g).Select(p => p.position.ToString())));
            }
            ColocarS2.Guardar("s2n-tianguis");
        }

        // Copia del grupo 2 con un solo SKULL (el primero).
        private static Transform Copia(Transform modelo, Transform raiz, string nombre, Vector3 centro)
        {
            Transform g = Object.Instantiate(modelo.gameObject, raiz).transform;
            g.name = nombre;
            foreach (Transform p in Pandilleros(g).Skip(1))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            Mover(g, centro);
            return g;
        }

        private static IEnumerable<Transform> Pandilleros(Transform grupo)
        {
            return grupo.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m != null && m.GetType().Name == "PandilleroAPie").Select(m => m.transform).ToList();
        }

        // Pone el grupo en 'centro' con sus pandilleros en fila sobre Z (la calle corre en Z), cada uno en piso libre.
        private static void Mover(Transform grupo, Vector3 centro)
        {
            List<Transform> lista = Pandilleros(grupo).ToList();
            grupo.position = Libre(centro, lista);
            Physics.SyncTransforms();
            for (int i = 0; i < lista.Count; i++)
            {
                float desvio = (i - (lista.Count - 1) * 0.5f) * SeparacionPandilleros;
                Bajar(lista[i], grupo.position + Vector3.forward * desvio, lista);
            }
        }

        private static void Bajar(Transform pandillero, Vector3 deseado, List<Transform> ignorar = null)
        {
            Vector3 p = Libre(deseado, ignorar ?? new List<Transform> { pandillero });
            pandillero.position = p;
            Physics.SyncTransforms();
        }

        // El punto de piso a nivel de calle más cercano a 'deseado' donde cabe una persona sin tocar nada sólido.
        private static Vector3 Libre(Vector3 deseado, List<Transform> ignorar)
        {
            for (float r = 0f; r <= RadioBusqueda; r += PasoBusqueda)
            {
                int pasos = r == 0f ? 1 : Mathf.CeilToInt(2f * Mathf.PI * r / PasoBusqueda);
                for (int k = 0; k < pasos; k++)
                {
                    float a = k * 2f * Mathf.PI / pasos;
                    Vector3 q = deseado + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (!Physics.Raycast(new Vector3(q.x, 10f, q.z), Vector3.down, out RaycastHit piso, 20f, ~0, QueryTriggerInteraction.Ignore) || piso.point.y > AlturaCalle)
                    {
                        continue;
                    }
                    Vector3 pie = piso.point;
                    bool choca = Physics.OverlapCapsule(pie + Vector3.up * (RadioCuerpo + 0.1f), pie + Vector3.up * 1.6f, RadioCuerpo, ~0, QueryTriggerInteraction.Ignore)
                        .Any(c => !ignorar.Any(t => c.transform.IsChildOf(t)));
                    if (!choca)
                    {
                        return pie;
                    }
                }
            }
            Debug.LogWarning("[Arte] T-802: sin piso libre cerca de " + deseado);
            return deseado;
        }
    }
}
