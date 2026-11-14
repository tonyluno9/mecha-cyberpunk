using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 pandillas (14-nov): cinco grupos de SKULLS que molestan a un civil (<see cref="GrupoAcoso"/>). Dos en plazas y dos en lotes (las
    /// áreas abiertas y planas más grandes de la ciudad, de al menos 10 x 10 m, lejos de los lugares de la historia, del casino, del jugador y de las
    /// otras pandillas) y uno en una azotea baja con acceso de S2_Techos y con otra azotea vecina a espaldas del grupo. Tamaños 3, 4, 4, 5 y 3.
    /// Los grupos están siempre (día y noche). Idempotente: rehace la raíz <see cref="Raiz"/>. Todo sale de sondeos de física.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Pandillas.Colocar
    /// </summary>
    public static class ColocarS2Pandillas
    {
        public const string Raiz = "S2_PandillasAcoso";
        public const string RutaDatosAcoso = "Assets/_Proyecto/IA/Datos/DatosAcoso_Base.asset";
        private const string RutaDatosCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private static readonly string[] VisualesCivil = { "Visual_man", "Visual_woman", "Visual_woman2" };

        private static readonly int[] Tamanos = { 3, 4, 4, 5, 3 };

        // Búsqueda en el suelo
        private const float XMin = -230f;
        private const float XMax = 230f;
        private const float ZMin = -210f;
        private const float ZMax = 210f;
        private const float PasoMalla = 2f;
        private const float AlturaSuelo = 1.0f;
        private const float SueloMinY = 0.1f;
        private const float SueloMaxY = 0.45f;
        private const float PlanoTolerancia = 0.08f;
        private const float MitadMinima = 5f;
        private const float MitadMaxima = 9f;
        private const float DespejeBajo = 0.4f;
        private const float DespejeAlto = 2.4f;
        private const float NucleoLibre = 3.2f;
        private const float GrosorMaximoProp = 0.9f;
        private const float SeparacionEntreGrupos = 25f;
        private const float DistLugares = 20f;
        private const float DistCasino = 30f;
        private const float DistJugador = 30f;
        private const float DistPandillas = 25f;
        private const float DistCalle = 6f;
        private const float XMaxCiudad = 200f;

        // Búsqueda en azoteas
        private const float TopMinAzotea = 5.5f;
        private const float TopMaxAzotea = 7.2f;
        private const float RadioAzotea = 2.6f;
        private const float DifAltoVecina = 1.8f;
        private const float VecinaMin = 6f;
        private const float VecinaMax = 14f;
        private const float DistAcceso = 14f;
        private const float DistMuroAzotea = 0.6f;

        private const float AlturaCapsulaMin = 0.45f;
        private const float AlturaCapsula = 1.7f;
        private const float RadioCapsula = 0.35f;
        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.3f;
        private const float ArcoAzotea = 60f;

        private static readonly string[] TiposLugar =
        {
            "PersonajeApoyo", "Reclutable", "MostradorTienda", "PuntoAparicion", "PuntoHistoriaMundo", "JefePandilla", "MarcaLugar",
            "MostradorPeleas", "DoctorClinica"
        };

        private sealed class Sitio
        {
            public Vector3 centro;
            public float mitad;
            public string tipo;
            public Vector3 haciaVecina;   // azotea: dirección horizontal hacia la azotea vecina
            public string detalle;
        }

        [MenuItem("Mecha Cyberpunk/S2/Pandillas que acosan")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform vieja = ColocarS2.Buscar(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Physics.SyncTransforms();

            DatosAcoso acoso = AsegurarDatos();
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosCalaveras);
            GameObject prefabPandillero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            if (acoso == null || calaveras == null || prefabPandillero == null)
            {
                Debug.LogError("[Arte] S2 pandillas: faltan los datos o el prefab de SKULLS.");
                return;
            }

            List<Vector3> lugares = Lugares(out Bounds casino);
            List<Vector3> jugador = Referencias("JugadorAPie", "MechaEstacionado");
            List<Vector3> pandillasViejas = PandillasExistentes();

            List<Sitio> elegidos = new List<Sitio>();
            List<Sitio> suelo = BuscarSuelo(lugares, casino, jugador, pandillasViejas);
            ColocarS2.Informe.AppendLine("Candidatos de suelo: " + suelo.Count);
            // Primero las dos plazas (las más grandes), luego dos lotes (las siguientes).
            string[] tiposSuelo = { "Plaza", "Plaza", "Lote", "Lote" };
            foreach (string tipo in tiposSuelo)
            {
                Sitio s = suelo.FirstOrDefault(c => elegidos.All(e => Vector3.Distance(e.centro, c.centro) >= SeparacionEntreGrupos));
                if (s == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA un sitio para " + tipo);
                    continue;
                }
                s.tipo = tipo;
                elegidos.Add(s);
            }
            Sitio azotea = BuscarAzotea(lugares, casino, jugador, pandillasViejas, elegidos);
            if (azotea != null)
            {
                elegidos.Add(azotea);
            }
            else
            {
                ColocarS2.Informe.AppendLine("FALTA una azotea");
            }

            GameObject raiz = new GameObject(Raiz);
            for (int i = 0; i < elegidos.Count; i++)
            {
                CrearGrupo(raiz.transform, i, elegidos[i], Tamanos[i], acoso, calaveras, prefabPandillero);
            }
            ColocarS2.Informe.AppendLine("Grupos: " + elegidos.Count + ", SKULLS: " + elegidos.Select((e, i) => Tamanos[i]).Sum());
            ColocarS2.Guardar("s2-pandillas");
        }

        // ------------------------------------------------------------------ datos

        private static DatosAcoso AsegurarDatos()
        {
            DatosAcoso datos = AssetDatabase.LoadAssetAtPath<DatosAcoso>(RutaDatosAcoso);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosAcoso>();
                AssetDatabase.CreateAsset(datos, RutaDatosAcoso);
                AssetDatabase.SaveAssets();
            }
            return datos;
        }

        // ------------------------------------------------------------------ referencias a evitar

        private static List<Vector3> Lugares(out Bounds casino)
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (Transform t in ColocarS2.Todos())
            {
                if (t.name.StartsWith("Lugar_") || t.GetComponents<MonoBehaviour>().Any(m => m != null && TiposLugar.Contains(m.GetType().Name)))
                {
                    puntos.Add(t.position);
                }
            }
            casino = new Bounds(new Vector3(100000f, 0f, 100000f), Vector3.zero);
            bool primero = true;
            foreach (string nombre in new[] { "T930_Casino", "S2o_Casino" })
            {
                Transform c = ColocarS2.Buscar(nombre);
                if (c == null)
                {
                    continue;
                }
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.bounds.center.x > 300f)
                    {
                        continue;
                    }
                    if (primero)
                    {
                        casino = r.bounds;
                        primero = false;
                    }
                    else
                    {
                        casino.Encapsulate(r.bounds);
                    }
                }
            }
            return puntos;
        }

        private static List<Vector3> Referencias(params string[] raices)
        {
            List<Vector3> r = new List<Vector3>();
            foreach (string n in raices)
            {
                Transform t = ColocarS2.Buscar(n);
                if (t != null)
                {
                    r.Add(t.position);
                }
            }
            return r;
        }

        private static List<Vector3> PandillasExistentes()
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.GetComponent<GrupoAcoso>() != null)
                {
                    continue;
                }
                puntos.Add(g.transform.position);
                foreach (PandilleroAPie p in g.GetComponentsInChildren<PandilleroAPie>(true))
                {
                    puntos.Add(p.transform.position);
                }
            }
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                puntos.Add(j.transform.position);
            }
            return puntos;
        }

        private static bool Lejos(Vector3 p, List<Vector3> lugares, Bounds casino, List<Vector3> jugador, List<Vector3> pandillas)
        {
            Vector3 plano = new Vector3(p.x, 0f, p.z);
            if (lugares.Any(l => Vector3.Distance(plano, new Vector3(l.x, 0f, l.z)) < DistLugares))
            {
                return false;
            }
            if (jugador.Any(l => Vector3.Distance(plano, new Vector3(l.x, 0f, l.z)) < DistJugador))
            {
                return false;
            }
            if (pandillas.Any(l => Vector3.Distance(plano, new Vector3(l.x, 0f, l.z)) < DistPandillas))
            {
                return false;
            }
            Vector3 cerca = casino.ClosestPoint(new Vector3(p.x, casino.center.y, p.z));
            return Vector3.Distance(new Vector3(cerca.x, 0f, cerca.z), plano) >= DistCasino;
        }

        // ------------------------------------------------------------------ suelo

        private static bool SueloPlano(Vector3 centro, float mitad, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(centro.x, AlturaSuelo, centro.z), Vector3.down, out RaycastHit h0, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = h0.point.y;
            if (y < SueloMinY || y > SueloMaxY)
            {
                return false;
            }
            for (float dx = -mitad; dx <= mitad + 0.01f; dx += mitad / 2f)
            {
                for (float dz = -mitad; dz <= mitad + 0.01f; dz += mitad / 2f)
                {
                    Vector3 o = new Vector3(centro.x + dx, AlturaSuelo, centro.z + dz);
                    if (!Physics.Raycast(o, Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(h.point.y - y) > PlanoTolerancia || h.normal.y < 0.95f || EsCalle(h.collider))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool EsCalle(Collider c)
        {
            string n = c.name;
            return n.Contains("Calle") || n.Contains("Arroyo") || n.Contains("Asfalto") || n.Contains("Cruzada");
        }

        // Despejado: nada sólido en el centro (donde se para el grupo) y, en el resto de la plaza, solo cosas delgadas (postes, bolardos, letreros).
        private static bool Despejado(Vector3 centro, float mitad, float y)
        {
            Vector3 c = new Vector3(centro.x, y + (DespejeBajo + DespejeAlto) * 0.5f, centro.z);
            float alto = (DespejeAlto - DespejeBajo) * 0.5f;
            float nucleo = Mathf.Min(mitad, NucleoLibre);
            if (Physics.CheckBox(c, new Vector3(nucleo, alto, nucleo), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            foreach (Collider col in Physics.OverlapBox(c, new Vector3(mitad, alto, mitad), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 t = col.bounds.size;
                if (t.x > GrosorMaximoProp || t.z > GrosorMaximoProp)
                {
                    return false;
                }
            }
            return true;
        }

        // Lejos de las calles de asfalto (no es una plaza si está pegada al tráfico).
        private static bool LejosDeCalle(Vector3 centro, float mitad)
        {
            float r = mitad + DistCalle;
            for (float a = 0f; a < 360f; a += 30f)
            {
                Vector3 p = centro + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
                if (Physics.Raycast(new Vector3(p.x, AlturaSuelo, p.z), Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) && EsCalle(h.collider))
                {
                    return false;
                }
            }
            return true;
        }

        private static List<Sitio> BuscarSuelo(List<Vector3> lugares, Bounds casino, List<Vector3> jugador, List<Vector3> pandillas)
        {
            List<Sitio> todos = new List<Sitio>();
            int planos = 0, despejados = 0;
            for (float x = XMin; x <= XMax; x += PasoMalla)
            {
                for (float z = ZMin; z <= ZMax; z += PasoMalla)
                {
                    if (Mathf.Abs(x) > XMaxCiudad)
                    {
                        continue;
                    }
                    Vector3 c = new Vector3(x, 0f, z);
                    if (!SueloPlano(c, MitadMinima, out float y))
                    {
                        continue;
                    }
                    c.y = y;
                    planos++;
                    if (!Despejado(c, MitadMinima, y))
                    {
                        continue;
                    }
                    despejados++;
                    if (!Lejos(c, lugares, casino, jugador, pandillas))
                    {
                        continue;
                    }
                    float mitad = MitadMinima;
                    while (mitad + 1f <= MitadMaxima && SueloPlano(c, mitad + 1f, out _) && Despejado(c, mitad + 1f, y))
                    {
                        mitad += 1f;
                    }
                    todos.Add(new Sitio { centro = c, mitad = mitad, detalle = "suelo y=" + y.ToString("F2") });
                }
            }
            ColocarS2.Informe.AppendLine("Suelo: planos " + planos + ", despejados " + despejados + ", y lejos de lo demás " + todos.Count);
            // Los más abiertos primero; a igual tamaño, los más lejos de la calle de asfalto.
            return todos.OrderByDescending(s => s.mitad).ThenByDescending(s => LejosDeCalle(s.centro, s.mitad) ? 1 : 0)
                .ThenBy(s => s.centro.x).ThenBy(s => s.centro.z).ToList();
        }

        // ------------------------------------------------------------------ azotea

        private static bool SobreAzotea(Vector3 xz, float yAzotea, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(xz.x, TopMaxAzotea + 4f, xz.z), Vector3.down, out RaycastHit h, 12f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = h.point.y;
            return h.normal.y > 0.95f && Mathf.Abs(y - yAzotea) <= DifAltoVecina && y >= TopMinAzotea - 0.5f && y <= TopMaxAzotea;
        }

        private static Sitio BuscarAzotea(List<Vector3> lugares, Bounds casino, List<Vector3> jugador, List<Vector3> pandillas, List<Sitio> otros)
        {
            List<PuntoTrepar> accesos = Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            List<(Sitio sitio, float puntaje)> candidatos = new List<(Sitio, float)>();
            foreach (string zona in new[] { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado" })
            {
                Transform g = ColocarS2.Buscar(zona);
                if (g == null)
                {
                    continue;
                }
                foreach (BoxCollider col in g.GetComponentsInChildren<BoxCollider>(false))
                {
                    if (col.name != "Cuerpo" || !col.transform.parent.name.StartsWith("Casa_"))
                    {
                        continue;
                    }
                    Bounds b = col.bounds;
                    if (b.max.y < TopMinAzotea || b.max.y > TopMaxAzotea)
                    {
                        continue;
                    }
                    for (float x = b.min.x; x <= b.max.x; x += 1f)
                    {
                        for (float z = b.min.z; z <= b.max.z; z += 1f)
                        {
                            Vector3 xz = new Vector3(x, 0f, z);
                            if (!SobreAzotea(xz, b.max.y, out float y) || !AzoteaPlana(xz, y) || !Despejado(new Vector3(x, y, z), RadioAzotea, y))
                            {
                                continue;
                            }
                            Vector3 c = new Vector3(x, y, z);
                            if (!Lejos(c, lugares, casino, jugador, pandillas) || otros.Any(o => Vector3.Distance(o.centro, c) < SeparacionEntreGrupos))
                            {
                                continue;
                            }
                            PuntoTrepar acceso = accesos.Where(a => Mathf.Abs(a.Salida.y - y) < 0.6f && HorizontalDist(a.Salida, c) <= DistAcceso)
                                .OrderBy(a => HorizontalDist(a.Salida, c)).FirstOrDefault();
                            if (acceso == null)
                            {
                                continue;
                            }
                            // La azotea vecina: la dirección cardinal con azotea a 6-14 m a un alto parecido, y la propia azotea sigue libre hasta ahí.
                            foreach (Vector3 d in dirs)
                            {
                                if (!HayVecina(c, d, y, out float lejos))
                                {
                                    continue;
                                }
                                Sitio s = new Sitio
                                {
                                    centro = c, mitad = RadioAzotea, tipo = "Azotea", haciaVecina = d,
                                    detalle = "azotea " + col.transform.parent.name + " (" + zona + ") y=" + y.ToString("F2") + ", acceso " + acceso.name + " a " + HorizontalDist(acceso.Salida, c).ToString("F1")
                                        + " m, vecina hacia " + d + " a " + lejos.ToString("F1") + " m"
                                };
                                // Mejor si el acceso queda cerca (no lo cuida nadie), la vecina cerca de 9 m y el grupo lejos de los bordes (los pretiles).
                                float puntaje = -HorizontalDist(acceso.Salida, c) - Mathf.Abs(lejos - 9f) + (BordeLejos(c, d) ? 5f : 0f);
                                candidatos.Add((s, puntaje));
                            }
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Candidatos de azotea: " + candidatos.Count);
            return candidatos.OrderByDescending(c => c.puntaje).ThenBy(c => c.sitio.centro.x).ThenBy(c => c.sitio.centro.z).Select(c => c.sitio).FirstOrDefault();
        }

        private static float HorizontalDist(Vector3 a, Vector3 b) => Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));

        // La azotea es plana en el radio del grupo (los techos vecinos de otra altura hacen escalón).
        private static bool AzoteaPlana(Vector3 xz, float y)
        {
            for (float a = 0f; a < 360f; a += 45f)
            {
                Vector3 p = xz + Quaternion.Euler(0f, a, 0f) * Vector3.forward * RadioAzotea;
                if (!Physics.Raycast(new Vector3(p.x, TopMaxAzotea + 4f, p.z), Vector3.down, out RaycastHit h, 12f, ~0, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(h.point.y - y) > PlanoTolerancia || h.normal.y < 0.95f)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HayVecina(Vector3 c, Vector3 d, float y, out float distancia)
        {
            distancia = 0f;
            // Camina hacia la vecina: debe haber un hueco (otra casa distinta) a un alto parecido, sin pared más alta de por medio.
            bool salioDeSuCasa = false;
            for (float t = RadioAzotea + 1f; t <= VecinaMax; t += 0.5f)
            {
                Vector3 p = c + d * t;
                bool hay = SobreAzotea(p, y, out float yp);
                if (!hay)
                {
                    salioDeSuCasa = true;
                    continue;
                }
                if (salioDeSuCasa && t >= VecinaMin)
                {
                    distancia = t;
                    return true;
                }
            }
            return false;
        }

        // El grupo no queda pegado al borde por donde llega la vecina: queda al menos 4 m de la orilla de ese lado (para que se pueda disparar desde la otra azotea sin oído).
        private static bool BordeLejos(Vector3 c, Vector3 d)
        {
            for (float t = 0.5f; t <= 5f; t += 0.5f)
            {
                if (!SobreAzotea(c + d * t, c.y, out _))
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ construir un grupo

        private static void CrearGrupo(Transform raiz, int indice, Sitio sitio, int tamano, DatosAcoso acoso, DatosPandilleroAPie calaveras, GameObject prefabPandillero)
        {
            string nombre = "Acoso_" + (indice + 1) + "_" + sitio.tipo;
            GameObject grupo = new GameObject(nombre);
            grupo.transform.SetParent(raiz, false);
            grupo.transform.position = sitio.centro;

            // Civil en el centro: Visual al azar (determinista por el índice).
            GameObject civilObj = new GameObject("Civil");
            civilObj.transform.SetParent(grupo.transform, false);
            civilObj.transform.position = sitio.centro;
            CharacterController cc = civilObj.AddComponent<CharacterController>();
            cc.height = AlturaCuerpo;
            cc.radius = RadioCuerpo;
            cc.center = new Vector3(0f, AlturaCuerpo * 0.5f, 0f);
            string visual = VisualesCivil[indice % VisualesCivil.Length];
            GameObject prefabVisual = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisuales + visual + ".prefab");
            GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(prefabVisual, civilObj.transform);
            v.name = "Visual";
            v.transform.localPosition = Vector3.zero;
            v.transform.localRotation = Quaternion.identity;
            CivilAcosado civil = civilObj.AddComponent<CivilAcosado>();
            SerializedObject soCivil = new SerializedObject(civil);
            soCivil.FindProperty("datos").objectReferenceValue = acoso;
            soCivil.ApplyModifiedPropertiesWithoutUndo();

            // Pandilleros: en círculo (arco de 120 grados dado la espalda a la azotea vecina) alrededor del civil.
            bool arco = sitio.tipo == "Azotea";
            float radio = Mathf.Lerp(acoso.RadioCirculoMinimo, acoso.RadioCirculoMaximo, (indice % 3) / 2f);
            if (tamano >= 5)
            {
                radio = Mathf.Max(radio, acoso.RadioCirculoMaximo);
            }
            float anguloInicio = (indice * 47f) % 360f;
            for (int i = 0; i < tamano; i++)
            {
                Vector3 dir;
                if (arco)
                {
                    float a = tamano == 1 ? 0f : Mathf.Lerp(-ArcoAzotea, ArcoAzotea, i / (float)(tamano - 1));
                    dir = Quaternion.Euler(0f, a, 0f) * sitio.haciaVecina;
                }
                else
                {
                    dir = Quaternion.Euler(0f, anguloInicio + 360f * i / tamano, 0f) * Vector3.forward;
                }
                Vector3 pos = sitio.centro + dir * radio;
                if (Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos = h.point;
                }
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefabPandillero, grupo.transform);
                p.name = "Pandillero_" + (i + 1);
                Vector3 mira = sitio.centro - pos;
                mira.y = 0f;
                p.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira.normalized, Vector3.up));
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = calaveras;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }

            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject soG = new SerializedObject(pandilla);
            soG.FindProperty("datos").objectReferenceValue = calaveras;
            soG.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            soG.ApplyModifiedPropertiesWithoutUndo();
            GrupoAcoso acosoGrupo = grupo.AddComponent<GrupoAcoso>();
            SerializedObject soA = new SerializedObject(acosoGrupo);
            soA.FindProperty("datos").objectReferenceValue = acoso;
            soA.FindProperty("civil").objectReferenceValue = civil;
            soA.ApplyModifiedPropertiesWithoutUndo();

            ColocarS2.Informe.AppendLine(nombre + " (" + tamano + " SKULLS + civil " + visual + ") en " + sitio.centro.ToString("F1") + " mitad libre " + sitio.mitad.ToString("F0") + " m: " + sitio.detalle);
        }
    }
}
