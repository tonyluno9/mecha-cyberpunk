using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-804 (playtest del 6-nov): puertas reales y sin teletransporte. La habitación se muda de su cuarto aparte (1000, 0, 1000) a dentro de la casa,
    /// detrás de la puerta de la calle: puerta de la calle (bisagra, E) → pasillo → puerta del cuarto (bisagra, E) → cuarto. La cama, el espejo, el punto
    /// donde despiertas y los postes de la guarida se mudan con el cuarto; la ZonaInterior de la casa cubre pasillo y cuarto. El taller del mecánico se
    /// arma dentro de la planta baja del Edificio_45, frente a la casa, con portón abierto, luz, carro en el elevador y letrero del kit; Canepa atiende ahí
    /// y el destino MECÁNICO del mapa (Lugar_TallerMecanico) apunta al portón. Ya no queda ninguna PuertaCuarto de la casa ni del taller.
    /// Idempotente. Orden si se rehace todo: ColocarT550.Colocar → ColocarS2Casa.Colocar → ColocarS2nLimpieza.Limpiar → esto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nPuertas.Todo
    /// </summary>
    public static class ColocarS2nPuertas
    {
        public const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigPuertas_Mundo.asset";
        public const string RutaCuarto = "T550_Cuartos/Cuarto_Habitacion";
        public const string RutaCasa = "Barrio/Manzana_4/Lugar_Casa";
        public const string NombreTaller = "S2n_TallerMecanico";
        public const string NombreLugarTaller = "Lugar_TallerMecanico";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const string L = "Assets/Arte/Callejon/Prefabs/";
        private const string T = "Assets/Arte/TallerCasa/Prefabs/";
        private const string V = "Assets/Arte/Vehiculos/Prefabs/";
        private static readonly Color Calido = new Color(1f, 0.72f, 0.42f);
        private static readonly Color Turquesa = new Color(0.3f, 1f, 0.9f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);

        // ---- Casa. Frente x -56.95 mirando a +X; puerta de la calle z -78.5..-75.5; el cuerpo de la casa (x -61..-58) deja libre ese pasillo.
        /// <summary>Centro del piso del cuarto (3 x 4 x 2.7 m, sin girar): x -64.5..-61.5, z -75.5..-71.5. Su pared de la puerta (z -75.5) da al pasillo.</summary>
        public static readonly Vector3 CentroCuarto = new Vector3(-63f, 0.2f, -73.5f);
        public const float Suelo = 0.2f;
        public const float AltoInterior = 2.7f;
        public const float MarcoX = -57.3f;
        public const float PasilloX0 = -63.6f, PasilloX1 = -57.75f, PasilloZ0 = -78.5f, PasilloZ1 = -75.5f;
        /// <summary>Vano de la puerta del cuarto en la pared norte del pasillo (x) y su alto.</summary>
        public const float VanoX0 = -62.5f, VanoX1 = -61.5f, AltoVano = 2.2f;

        // ---- Taller del mecánico dentro de la planta baja del Edificio_45 (cuerpo x -37..-12.3, z -84..-59.3; fachada oeste en x -37).
        // T-883 (playtest 7-nov): a escala real, cabe un carro con espacio: 16 m de fondo (antes 9) y 5.4 m de alto (antes 4.6).
        public const float TallerX0 = -37f, TallerX1 = -21f, TallerZ0 = -75.67f, TallerZ1 = -67.67f, TallerTecho = 5.4f;
        /// <summary>Cuánto se recorrió el fondo del taller (T-883): lo que iba contra la pared del fondo se mueve esto en x.</summary>
        private const float Fondo = TallerX1 + 28f;
        public const float PortonZ0 = -74.9f, PortonZ1 = -68.45f, PortonAlto = 4.3f;
        private const float AltoPlantaBaja = 6.2f;
        private static readonly float[] BahiasTaller = { -73.67f, -69.67f };
        /// <summary>Lámina de T-506: el elevador lleva textura (nada de greybox liso).</summary>
        private const string MetalConTextura = "Assets/Arte/Exteriores/Materiales/T506_Metal_Oscuro.mat";

        [MenuItem("Mecha Cyberpunk/S2n/2 Puertas reales (casa, cuarto, taller del mecánico)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Casa();
            TallerMecanico();
            Locales();
            ColocarT550.AislarDelSol();
            ColocarS2.Guardar("s2n-puertas");
        }

        /// <summary>Colocar y capturas (casa por dentro, cuarto, taller y la calle frente a la casa) lado a lado con las referencias.</summary>
        public static void Todo()
        {
            Colocar();
            Capturas();
        }

        // ================================================================ Casa

        public static void Casa()
        {
            Transform casa = ColocarS2.Buscar(RutaCasa);
            Transform r = ColocarS2.Buscar(RutaCuarto);
            if (casa == null || r == null)
            {
                ColocarS2.Informe.AppendLine("FALTA la casa o el cuarto");
                return;
            }
            LugarMundo lugar = casa.GetComponent<LugarMundo>();
            MoverCuarto(r);
            QuitarPuertasCuarto(IdLugar.Casa);
            // El interior viejo (ya no se pisa) deja de verse: el cuarto y el pasillo nuevos están donde estaba.
            foreach (string n in new[] { "Interior/Revestimiento", "Interior/Sala" })
            {
                Transform t = casa.Find(n);
                if (t != null)
                {
                    foreach (Renderer rend in t.GetComponentsInChildren<Renderer>(true))
                    {
                        rend.enabled = false;
                    }
                }
            }
            ParedDeLaPuerta(r);
            Pasillo(r);
            PuertaLocal calle = PuertaDeLaCalle(casa);
            PuertaLocal cuarto = PuertaDelCuarto(r, lugar);
            Zona(r, calle);
            Guarida();
            ColocarS2.Informe.AppendLine("Casa: cuarto en " + r.position + ", pasillo x " + PasilloX0 + ".." + PasilloX1 + "; puertas con bisagra: " + calle.name + ", " + cuarto.name);
        }

        /// <summary>Mueve el cuarto (y lo que vivía en él sin ser su hijo: cama, espejo, punto de despertar) a CentroCuarto, sin girarlo.</summary>
        private static void MoverCuarto(Transform r)
        {
            Vector3 delta = CentroCuarto - r.position;
            if (delta.magnitude < 0.01f)
            {
                return;
            }
            Bounds viejo = new Bounds(r.position + Vector3.up * 1.5f, new Vector3(3.4f, 6f, 4.4f));
            List<Transform> mover = new List<Transform>();
            foreach (Transform t in ColocarS2.Todos().OrderBy(Profundidad))
            {
                if (t == r || t.IsChildOf(r) || !viejo.Contains(t.position) || mover.Any(m => t.IsChildOf(m)))
                {
                    continue;
                }
                mover.Add(t);
            }
            foreach (Transform t in mover)
            {
                t.position += delta;
                ColocarS2.Informe.AppendLine("  mudado con el cuarto: " + ColocarS2.Ruta(t));
            }
            r.position = CentroCuarto;
        }

        private static int Profundidad(Transform t)
        {
            int n = 0;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                n++;
            }
            return n;
        }

        /// <summary>Borra las puertas de fundido (PuertaCuarto) del lugar: ya se entra caminando.</summary>
        public static void QuitarPuertasCuarto(IdLugar lugar)
        {
            foreach (PuertaCuarto p in Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.Cuarto != null && p.Cuarto.Lugar == lugar)
                {
                    ColocarS2.Informe.AppendLine("  sin teletransporte: borrada " + ColocarS2.Ruta(p.transform));
                    Object.DestroyImmediate(p.gameObject);
                }
            }
        }

        // La pared de la puerta del cuarto era entera (se salía por fundido): se rehace con su vano.
        private static void ParedDeLaPuerta(Transform r)
        {
            Transform vieja = r.Find("Pared_Puerta");
            Material m = (vieja != null ? vieja : r.Find("Pared_Fondo")).GetComponent<Renderer>().sharedMaterial;
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            foreach (string n in new[] { "Pared_Puerta_Izq", "Pared_Puerta_Arriba" })
            {
                Transform t = r.Find(n);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            float z = PasilloZ1, y0 = Suelo, y1 = Suelo + AltoInterior;
            float x0 = CentroCuarto.x - 1.5f;
            ColocarT550.Superficie(r, "Pared_Puerta_Izq", m, new Vector3(VanoX0, y0, z), new Vector3(VanoX0, y1, z), new Vector3(x0, y1, z), new Vector3(x0, y0, z), Vector3.forward, -0.1f);
            ColocarT550.Superficie(r, "Pared_Puerta_Arriba", m, new Vector3(VanoX1, y0 + AltoVano, z), new Vector3(VanoX1, y1, z), new Vector3(VanoX0, y1, z), new Vector3(VanoX0, y0 + AltoVano, z), Vector3.forward, -0.1f);
        }

        // Pasillo de la entrada: del umbral de la calle (x -57.75) a la pared oeste (x -63.6), forrado como el cuarto, con su luz.
        private static void Pasillo(Transform r)
        {
            Transform viejo = r.Find("Pasillo");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Transform p = new GameObject("Pasillo").transform;
            p.SetParent(r, false);
            Material pared = r.Find("Pared_Fondo").GetComponent<Renderer>().sharedMaterial;
            Material piso = r.Find("Piso").GetComponent<Renderer>().sharedMaterial;
            Material techo = r.Find("Techo").GetComponent<Renderer>().sharedMaterial;
            const float Dentro = 0.03f; // los cubos de la casa quedan detrás del forro
            // Las paredes llegan hasta el marco de la puerta de la calle (x -57.3): detrás de él ya no queda hueco hacia la fachada.
            float x0 = PasilloX0, x1 = MarcoX, z0 = PasilloZ0 + Dentro, z1 = PasilloZ1 - Dentro, y0 = Suelo + 0.012f, y1 = Suelo + AltoInterior;
            Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(p, "Piso", piso, v(x0, y0, z0), v(x0, y0, z1), v(x1, y0, z1), v(x1, y0, z0), Vector3.up, -0.1f);
            ColocarT550.Superficie(p, "Techo", techo, v(x0, y1, z0), v(x1, y1, z0), v(x1, y1, z1), v(x0, y1, z1), Vector3.down, 0.1f);
            ColocarT550.Superficie(p, "Pared_Sur", pared, v(x1, y0, z0), v(x1, y1, z0), v(x0, y1, z0), v(x0, y0, z0), Vector3.forward, -0.1f);
            ColocarT550.Superficie(p, "Pared_Oeste", pared, v(x0, y0, z0), v(x0, y1, z0), v(x0, y1, z1), v(x0, y0, z1), Vector3.right, -0.1f);
            // Pared norte: la del cuarto por el otro lado, con el vano de su puerta.
            ColocarT550.Superficie(p, "Pared_Norte_Oeste", pared, v(x0, y0, z1), v(x0, y1, z1), v(VanoX0, y1, z1), v(VanoX0, y0, z1), Vector3.back, 0.1f);
            ColocarT550.Superficie(p, "Pared_Norte_Este", pared, v(VanoX1, y0, z1), v(VanoX1, y1, z1), v(x1, y1, z1), v(x1, y0, z1), Vector3.back, 0.1f);
            ColocarT550.Superficie(p, "Pared_Norte_Arriba", pared, v(VanoX0, y0 + AltoVano, z1), v(VanoX0, y1, z1), v(VanoX1, y1, z1), v(VanoX1, y0 + AltoVano, z1), Vector3.back, 0.1f);
            // Lo de un pasillo: lámpara de jaula, medidores y cables.
            ColocarS2Interiores.Modelo(p, C + "N01", new Vector3(-59.6f, 0f, PasilloZ0 + 0.2f), 180f, Suelo + 2.15f, 0.8f, sinCollider: true);
            ColocarS2Interiores.Modelo(p, C + "C14_RegistroMedidores", new Vector3(-61.2f, 0f, PasilloZ0 + 0.15f), 180f, Suelo + 1.2f, 0.5f, sinCollider: true);
            ColocarS2Interiores.Modelo(p, C + "CY03_MazoCables", new Vector3(-63.4f, 0f, -77.6f), 270f, Suelo + 1.0f, 0.8f, sinCollider: true);
            Transform luces = r.Find("Luces");
            Transform vieja = luces.Find("Pasillo_Luz");
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            ColocarT550.Foco(r, "Pasillo_Luz", r.InverseTransformPoint(new Vector3(-60.2f, Suelo + 2.2f, -77f)), Calido, 3.2f, 6.5f, luces);
            foreach (Renderer rend in p.GetComponentsInChildren<Renderer>())
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>La hoja de la puerta de la calle gira sobre su orilla sur y solo abre con E.</summary>
        private static PuertaLocal PuertaDeLaCalle(Transform casa)
        {
            PuertaLocal puerta = casa.Find("Puerta").GetComponent<PuertaLocal>();
            Transform hoja = casa.Find("Puerta/Hoja");
            // La orilla de la hoja que da al sur (z menor): el eje local cuyo mundo es más paralelo a Z.
            Vector3 eje = Mathf.Abs(hoja.right.z) > Mathf.Abs(hoja.forward.z) ? Vector3.right * Mathf.Sign(-hoja.right.z) : Vector3.forward * Mathf.Sign(-hoja.forward.z);
            Bisagra(hoja.GetComponent<HojaPuerta>(), eje * 0.5f, null);
            SoloConE(puerta);
            return puerta;
        }

        // Puerta del cuarto: hoja L02 que gira sobre su orilla este (junto a la pared derecha del cuarto), con su collider y su umbral.
        private static PuertaLocal PuertaDelCuarto(Transform r, LugarMundo lugar)
        {
            Transform vieja = r.Find("Puerta_Cuarto");
            Transform modelo = r.Find("Puerta_Cuarto/Hoja/Puerta_L02") ?? r.Find("S2_Cuarto/Puerta_L02");
            if (modelo != null)
            {
                modelo.SetParent(r.Find("S2_Cuarto"), true);
            }
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            GameObject go = new GameObject("Puerta_Cuarto");
            go.transform.SetParent(r, false);
            go.transform.position = new Vector3((VanoX0 + VanoX1) * 0.5f, Suelo, PasilloZ1);
            Transform umbral = new GameObject("Umbral").transform;
            umbral.SetParent(go.transform, false);
            umbral.localPosition = Vector3.up * 1.1f;
            Bounds b = modelo != null ? ArteT250.Limites(modelo.gameObject) : new Bounds(go.transform.position + Vector3.up * 1.05f, new Vector3(1f, 2.1f, 0.08f));
            Transform hoja = new GameObject("Hoja").transform;
            hoja.SetParent(go.transform, false);
            hoja.position = b.center;
            BoxCollider caja = hoja.gameObject.AddComponent<BoxCollider>();
            caja.size = new Vector3(Mathf.Min(b.size.x, VanoX1 - VanoX0), b.size.y, Mathf.Max(b.size.z, 0.06f));
            if (modelo != null)
            {
                modelo.SetParent(hoja, true);
            }
            HojaPuerta hp = hoja.gameObject.AddComponent<HojaPuerta>();
            Bisagra(hp, new Vector3(caja.size.x * 0.5f, 0f, 0f), umbral, caja, new Vector3(1.6f, 2.2f, 2.4f));
            PuertaLocal puerta = go.AddComponent<PuertaLocal>();
            SerializedObject so = new SerializedObject(puerta);
            so.FindProperty("lugar").objectReferenceValue = lugar;
            so.FindProperty("hoja").objectReferenceValue = hp;
            so.ApplyModifiedPropertiesWithoutUndo();
            SoloConE(puerta);
            return puerta;
        }

        /// <summary>Configura una hoja con bisagra: 'bisagraLocal' en el espacio local de la hoja antes de su escala.</summary>
        public static void Bisagra(HojaPuerta hoja, Vector3 bisagraLocal, Transform umbral, Collider colliderHoja = null, Vector3? tamanoUmbral = null)
        {
            SerializedObject so = new SerializedObject(hoja);
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigPuertas>(RutaConfig);
            so.FindProperty("conBisagra").boolValue = true;
            so.FindProperty("bisagraLocal").vector3Value = bisagraLocal;
            if (colliderHoja != null)
            {
                so.FindProperty("colliderHoja").objectReferenceValue = colliderHoja;
            }
            if (umbral != null)
            {
                so.FindProperty("umbral").objectReferenceValue = umbral;
            }
            if (tamanoUmbral.HasValue)
            {
                so.FindProperty("tamanoUmbral").vector3Value = tamanoUmbral.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SoloConE(PuertaLocal puerta)
        {
            SerializedObject so = new SerializedObject(puerta);
            so.FindProperty("abrirAlAcercarse").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // La zona de la casa cubre el pasillo y el cuarto (en su caja también cae un rincón del cuerpo de la casa al que no se llega).
        private static void Zona(Transform r, PuertaLocal calle)
        {
            Transform zona = r.Find("ZonaInterior");
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(CentroCuarto.x - 1.5f, Suelo, PasilloZ0), new Vector3(PasilloX1, Suelo + AltoInterior, CentroCuarto.z + 2f));
            zona.position = b.center;
            zona.rotation = Quaternion.identity;
            BoxCollider caja = zona.GetComponent<BoxCollider>();
            caja.center = Vector3.zero;
            caja.size = b.size - new Vector3(0.1f, 0.1f, 0.1f);
            SerializedObject so = new SerializedObject(zona.GetComponent<ZonaInterior>());
            so.FindProperty("puerta").objectReferenceValue = calle;
            Light[] luces = r.Find("Luces").GetComponentsInChildren<Light>(true);
            SerializedProperty arr = so.FindProperty("luces");
            arr.arraySize = luces.Length;
            for (int i = 0; i < luces.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Los postes de la guarida (armero, garaje, cama) junto a la cama: en el pasillo, pegados a la pared del cuarto.</summary>
        private static void Guarida()
        {
            Transform raiz = ColocarS2.Buscar("GTA_T448");
            if (raiz == null)
            {
                return;
            }
            // A menos de 4 m de la cama (x -63.45, z -73.45): pegados a la pared norte del pasillo.
            Vector3[] lugares = { new Vector3(-63.25f, Suelo, -75.95f), new Vector3(-61.15f, Suelo, -75.95f), new Vector3(-60.45f, Suelo, -75.95f) };
            // Los postes de T-448 son cubos (poste y letrero brillante): se apagan y cada punto lleva un mueble del kit que se lee como lo que es.
            string[] muebles = { T + "T04_GabineteHerramientas", T + "H11_Buro", L + "L04_Llantas" };
            float[] escalas = { 0.6f, 1f, 0.55f };
            int i = 0;
            foreach (Transform t in raiz.Cast<Transform>().Where(t => t.name.StartsWith("Guarida_")).OrderBy(t => t.name))
            {
                if (i >= lugares.Length)
                {
                    break;
                }
                t.position = lugares[i];
                t.rotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (Renderer rend in t.GetComponentsInChildren<Renderer>(true))
                {
                    rend.enabled = false;
                }
                foreach (Collider col in t.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }
                Transform viejo = t.Find("Mueble");
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                GameObject mueble = ColocarS2Interiores.Modelo(t, muebles[i], new Vector3(lugares[i].x, 0f, lugares[i].z + 0.12f), 0f, Suelo, escalas[i], sinCollider: true);
                if (mueble != null)
                {
                    mueble.name = "Mueble";
                }
                i++;
            }
        }

        // ================================================================ Taller del mecánico

        public static void TallerMecanico()
        {
            // El cuarto aparte del taller y sus puertas de fundido ya no existen.
            QuitarPuertasCuarto(IdLugar.Taller);
            Transform viejo = ColocarS2.Buscar(ColocarT550.NombreRaiz + "/" + ColocarS2Interiores.NombreTallerMecanico);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Object.DestroyImmediate(GameObject.Find(NombreTaller));
            Transform raiz = new GameObject(NombreTaller).transform;
            Transform edificio = ColocarS2.Buscar("Barrio/Manzana_5/Edificios/Edificio_45");
            if (edificio == null)
            {
                ColocarS2.Informe.AppendLine("FALTA Edificio_45");
                return;
            }
            Cuerpo(edificio, raiz);
            DespejarLaCalle();
            Fachada(edificio, raiz);
            Forro(raiz);
            Muebles(raiz);
            Luces(raiz);
            LugarYZona(raiz);
            Canepa();
            ColocarS2.Informe.AppendLine("Taller del mecánico: planta baja del Edificio_45, x " + TallerX0 + ".." + TallerX1 + ", z " + TallerZ0 + ".." + TallerZ1);
        }

        // El cubo del edificio (una sola caja con collider) se parte en cuatro alrededor del taller.
        private static void Cuerpo(Transform edificio, Transform raiz)
        {
            Transform cuerpo = edificio.Find("Cuerpo");
            Renderer rend = cuerpo.GetComponent<Renderer>();
            BoxCollider col = cuerpo.GetComponent<BoxCollider>();
            // Con el renderer ya apagado (segunda pasada) sus bounds no sirven: se miden de la malla.
            Bounds mb = cuerpo.GetComponent<MeshFilter>().sharedMesh.bounds;
            Bounds b = new Bounds(cuerpo.TransformPoint(mb.center), Vector3.Scale(mb.size, cuerpo.lossyScale));
            Material m = rend.sharedMaterial;
            rend.enabled = false;
            col.enabled = false;
            Transform g = Grupo(raiz, "Cuerpo");
            void Caja(string n, float x0, float x1, float y0, float y1, float z0, float z1)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = n;
                c.transform.SetParent(g, false);
                c.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
                c.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
                c.GetComponent<Renderer>().sharedMaterial = m;
                GameObjectUtility.SetStaticEditorFlags(c, StaticEditorFlags.BatchingStatic);
                ColocarS2Interiores.Obstaculo(c);
            }
            Caja("Cuerpo_Sur", b.min.x, b.max.x, b.min.y, b.max.y, b.min.z, TallerZ0);
            Caja("Cuerpo_Norte", b.min.x, b.max.x, b.min.y, b.max.y, TallerZ1, b.max.z);
            Caja("Cuerpo_Fondo", TallerX1, b.max.x, b.min.y, b.max.y, TallerZ0, TallerZ1);
            Caja("Cuerpo_Arriba", b.min.x, TallerX1, TallerTecho, b.max.y, TallerZ0, TallerZ1);
        }

        // Lo que tapaba el portón desde la casa: el carro robable de T-511 estacionado enfrente, la pantalla LED de la banqueta y la franja de
        // ArteNoche que cruzaba el vano.
        private static void DespejarLaCalle()
        {
            Transform carro = ColocarS2.Buscar("GTA_T511/CarroRobable_17");
            if (carro != null && carro.position.z < -60f && carro.position.z > -80f)
            {
                carro.position += new Vector3(0f, 0f, 22f);
            }
            Transform pantalla = ColocarS2.Buscar("Barrio/ArteNoche/Calle_tianguis/CY02_PantallaVertical_Noche");
            if (pantalla != null && pantalla.position.z < -67.5f && pantalla.position.z > -76f)
            {
                Bounds bp = ArteT250.Limites(pantalla.gameObject);
                pantalla.position += new Vector3(0f, 0f, TallerZ1 + 2.1f - bp.center.z); // frente a la bahía de al lado
            }
            Transform pisos = ColocarS2.Buscar("Barrio/ArteNoche/Pisos/Edificio_45_Oeste");
            if (pisos != null)
            {
                // La franja cruzaba el vano y el piso con ventanas de abajo tapaba el letrero: los que caen sobre el portón se apagan.
                foreach (Transform t in pisos.Cast<Transform>().Where(t => t.name.StartsWith("C23_Franja") || t.name.StartsWith("C20") || t.name.StartsWith("C21")))
                {
                    Bounds b = ArteT250.Limites(t.gameObject);
                    if (b.min.y < AltoPlantaBaja + 1f && b.center.z > TallerZ0 && b.center.z < TallerZ1)
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Fachada oeste: fuera las dos bahías del kit donde va el portón; pilares y dintel de ladrillo, letrero del kit encima.
        private static void Fachada(Transform edificio, Transform raiz)
        {
            Transform lado = edificio.Find("Lado_Oeste");
            foreach (Transform t in lado.Cast<Transform>().Where(t => t.name.StartsWith("Fachada_Modular") && BahiasTaller.Any(z => Mathf.Abs(t.position.z - z) < 0.5f)))
            {
                t.gameObject.SetActive(false);
            }
            Transform f = Grupo(raiz, "Fachada");
            Material ladrillo = ColocarT550.Mat("S2_Pared_X03", new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(3));
            float x = TallerX0 - 0.02f, y0 = Suelo, y1 = Suelo + AltoPlantaBaja - 0.2f;
            Vector3 v(float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(f, "Pilar_Sur", ladrillo, v(y0, TallerZ0), v(y1, TallerZ0), v(y1, PortonZ0), v(y0, PortonZ0), Vector3.left, 0.1f);
            ColocarT550.Superficie(f, "Pilar_Norte", ladrillo, v(y0, PortonZ1), v(y1, PortonZ1), v(y1, TallerZ1), v(y0, TallerZ1), Vector3.left, 0.1f);
            ColocarT550.Superficie(f, "Dintel", ladrillo, v(Suelo + PortonAlto, PortonZ0), v(y1, PortonZ0), v(y1, PortonZ1), v(Suelo + PortonAlto, PortonZ1), Vector3.left, 0.1f);
            // Letrero del kit sobre el dintel, mirando a la calle.
            // Caja de letrero C15a del kit (sin neón propio: el L10 es VANACOIN y el L14 es el CIC de Tino) con el nombre en uGUI encima, como las pantallas del mundo.
            GameObject letrero = ColocarS2Interiores.Modelo(f, C + "C15a_LetreroHorizontal", new Vector3(TallerX0 - 0.15f, 0f, (PortonZ0 + PortonZ1) * 0.5f), 90f, Suelo + PortonAlto + 0.15f, 1f, sinCollider: true);
            if (letrero != null)
            {
                letrero.name = "Letrero_Taller";
                Ajustar(letrero, 4.0f, 1.45f);
                Bounds lb = ArteT250.Limites(letrero);
                letrero.transform.position += new Vector3(TallerX0 - 0.03f - lb.max.x, Suelo + PortonAlto + 0.12f - lb.min.y, (PortonZ0 + PortonZ1) * 0.5f - lb.center.z);
                lb = ArteT250.Limites(letrero);
                Texto(f, "TALLER CANEPA", new Vector3(lb.min.x - 0.1f, lb.center.y - lb.size.y * 0.04f, lb.center.z), new Vector2(lb.size.z * 0.66f, lb.size.y * 0.4f));
            }
            Light l = Luz(f, "Luz_Letrero", new Vector3(TallerX0 - 1.2f, Suelo + PortonAlto + 1.4f, (PortonZ0 + PortonZ1) * 0.5f), Calido, 5f, 5f);
            l.shadows = LightShadows.None;
            foreach (Renderer rend in f.GetComponentsInChildren<Renderer>())
            {
                if (rend.name.StartsWith("Pilar") || rend.name == "Dintel")
                {
                    rend.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        // Letrero en un Canvas de mundo (uGUI Text, regla del proyecto), mirando a la calle (-X).
        private static void Texto(Transform padre, string texto, Vector3 centro, Vector2 tam)
        {
            GameObject go = new GameObject("Letrero_Taller_Texto", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(padre, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            const float PixelesPorMetro = 100f;
            rt.sizeDelta = tam * PixelesPorMetro;
            rt.localScale = Vector3.one / PixelesPorMetro;
            rt.SetPositionAndRotation(centro, Quaternion.LookRotation(Vector3.right, Vector3.up)); // la cara del texto mira a -X
            GameObject t = new GameObject("Texto", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            t.transform.SetParent(go.transform, false);
            RectTransform trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            UnityEngine.UI.Text txt = t.GetComponent<UnityEngine.UI.Text>();
            txt.text = texto;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = 200;
            txt.color = new Color(1f, 0.62f, 0.2f);
        }

        /// <summary>Escala un modelo para que su lado mayor horizontal mida 'ancho' sin pasar de 'alto'.</summary>
        private static void Ajustar(GameObject g, float ancho, float alto)
        {
            Bounds b = ArteT250.Limites(g);
            float lado = Mathf.Max(b.size.x, b.size.z, 0.01f);
            float k = Mathf.Min(ancho / lado, alto / Mathf.Max(b.size.y, 0.01f));
            g.transform.localScale *= k;
        }

        private static void Forro(Transform raiz)
        {
            Transform f = Grupo(raiz, "Forro");
            Material pared = ColocarT550.Mat("S2_Pared_X03", new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(3));
            Material techo = ColocarT550.Mat("S2_Techo_X05", new Color(0.5f, 0.47f, 0.44f), ImportarT550.RutaMaterialPared(5));
            Material piso = ColocarT550.Mat("S2n_Piso_Taller", new Color(0.24f, 0.23f, 0.22f), ImportarT550.RutaMaterialPared(1));
            const float D = 0.04f;
            float x0 = TallerX0 + 0.12f, x1 = TallerX1 - D, z0 = TallerZ0 + D, z1 = TallerZ1 - D, y0 = Suelo + 0.012f, y1 = TallerTecho - D;
            Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(f, "Piso", piso, v(TallerX0 - 0.02f, y0, z0), v(TallerX0 - 0.02f, y0, z1), v(x1, y0, z1), v(x1, y0, z0), Vector3.up, -0.1f);
            ColocarT550.Superficie(f, "Techo", techo, v(x0, y1, z0), v(x1, y1, z0), v(x1, y1, z1), v(x0, y1, z1), Vector3.down, 0.1f);
            ColocarT550.Superficie(f, "Pared_Sur", pared, v(x1, y0, z0), v(x1, y1, z0), v(x0, y1, z0), v(x0, y0, z0), Vector3.forward, -0.1f);
            ColocarT550.Superficie(f, "Pared_Norte", pared, v(x0, y0, z1), v(x0, y1, z1), v(x1, y1, z1), v(x1, y0, z1), Vector3.back, 0.1f);
            ColocarT550.Superficie(f, "Pared_Fondo", pared, v(x1, y0, z1), v(x1, y1, z1), v(x1, y1, z0), v(x1, y0, z0), Vector3.left, 0.1f);
            // Caras de dentro de los pilares y del dintel.
            ColocarT550.Superficie(f, "Pilar_Sur_Dentro", pared, v(x0, y0, PortonZ0), v(x0, y1, PortonZ0), v(x0, y1, z0), v(x0, y0, z0), Vector3.right, -0.1f);
            ColocarT550.Superficie(f, "Pilar_Norte_Dentro", pared, v(x0, y0, z1), v(x0, y1, z1), v(x0, y1, PortonZ1), v(x0, y0, PortonZ1), Vector3.right, -0.1f);
            ColocarT550.Superficie(f, "Dintel_Dentro", pared, v(x0, Suelo + PortonAlto, z1 > PortonZ1 ? PortonZ1 : z1), v(x0, y1, PortonZ1), v(x0, y1, PortonZ0), v(x0, Suelo + PortonAlto, PortonZ0), Vector3.right, -0.1f);
            ColocarT550.Superficie(f, "Dintel_Abajo", pared, v(TallerX0 - 0.02f, Suelo + PortonAlto, PortonZ0), v(x0, Suelo + PortonAlto, PortonZ0), v(x0, Suelo + PortonAlto, PortonZ1), v(TallerX0 - 0.02f, Suelo + PortonAlto, PortonZ1), Vector3.down, 0.1f);
            foreach (Renderer rend in f.GetComponentsInChildren<Renderer>())
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void Muebles(Transform raiz)
        {
            Transform m = Grupo(raiz, "Muebles");
            float p = Suelo;
            // Elevador de dos postes con el carro del cliente arriba, la nariz hacia la calle.
            Vector3 centroCarro = new Vector3(-29f, 0f, -73.0f);
            Transform elevador = Grupo(m, "Elevador");
            GameObject carro = ColocarS2Interiores.Modelo(elevador, V + "V01a_CarroViejito", centroCarro, 90f, p + 1.55f, sinCollider: true);
            Bounds bc = carro != null ? ArteT250.Limites(carro) : new Bounds(centroCarro + Vector3.up * 2.5f, new Vector3(4.2f, 1.9f, 2.1f));
            if (carro != null && bc.size.z > bc.size.x)
            {
                carro.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                bc = ArteT250.Limites(carro);
                carro.transform.position += new Vector3(centroCarro.x - bc.center.x, p + 1.55f - bc.min.y, centroCarro.z - bc.center.z);
                bc = ArteT250.Limites(carro);
            }
            Material amarillo = ColocarT550.Mat("S2n_Elevador_Amarillo", new Color(1f, 0.72f, 0.12f), MetalConTextura);
            Material metal = ColocarT550.Mat("S2n_Elevador_Metal", new Color(0.45f, 0.45f, 0.5f), MetalConTextura);
            float alto = 3.6f;
            foreach (float lado in new[] { -1f, 1f })
            {
                float z = centroCarro.z + lado * (bc.extents.z + 0.35f);
                Pieza(elevador, "Poste", amarillo, new Vector3(centroCarro.x, p + alto * 0.5f, z), new Vector3(0.32f, alto, 0.32f), true);
                Pieza(elevador, "Base", metal, new Vector3(centroCarro.x, p + 0.03f, z), new Vector3(0.7f, 0.06f, 0.7f), false);
                Pieza(elevador, "Carro_Poste", metal, new Vector3(centroCarro.x, p + 1.4f, z - lado * 0.05f), new Vector3(0.42f, 0.5f, 0.42f), false);
                foreach (float frente in new[] { -1f, 1f })
                {
                    // Brazos que van del poste bajo el carro, en diagonal hacia sus puntos de apoyo.
                    Vector3 a = new Vector3(centroCarro.x, p + 1.45f, z);
                    Vector3 bpt = new Vector3(centroCarro.x + frente * bc.extents.x * 0.55f, p + 1.45f, centroCarro.z + lado * bc.extents.z * 0.35f);
                    GameObject brazo = Pieza(elevador, "Brazo", amarillo, (a + bpt) * 0.5f, new Vector3(0.12f, 0.08f, Vector3.Distance(a, bpt)), false);
                    brazo.transform.rotation = Quaternion.LookRotation(bpt - a, Vector3.up);
                    Pieza(elevador, "Apoyo", metal, bpt + Vector3.up * 0.05f, new Vector3(0.16f, 0.12f, 0.16f), false);
                }
            }
            Pieza(elevador, "Travesano", amarillo, new Vector3(centroCarro.x, p + alto - 0.1f, centroCarro.z), new Vector3(0.22f, 0.2f, bc.size.z + 0.7f + 0.32f), false);
            // Lo de un taller, contra las paredes (como el cuarto aparte de S2, ahora en su sitio).
            ColocarS2Interiores.Modelo(m, T + "T07_GruaMotor", new Vector3(-31.6f + Fondo * 0.5f, 0f, -70.4f), 0f, p);
            ColocarS2Interiores.Modelo(m, T + "T04_GabineteHerramientas", new Vector3(-29.4f + Fondo, 0f, -75.15f), 180f, p);
            ColocarS2Interiores.Modelo(m, T + "T01_ParedTaller", new Vector3(-28.45f + Fondo, 0f, -71.5f), 90f, p, 0.75f);
            ColocarS2Interiores.Modelo(m, T + "T03_BancoTrabajo", new Vector3(-30.0f + Fondo, 0f, -68.2f), 0f, p);
            ColocarS2Interiores.Modelo(m, T + "T06_Soldadora", new Vector3(-32.4f + Fondo, 0f, -68.25f), 0f, p);
            ColocarS2Interiores.Modelo(m, T + "T05_Generador", new Vector3(-28.7f + Fondo, 0f, -68.5f), 90f, p);
            ColocarS2Interiores.Modelo(m, T + "T08_RepisaTaller", new Vector3(-28.45f + Fondo, 0f, -74.0f), 90f, p, 0.85f);
            ColocarS2Interiores.Modelo(m, L + "L04_Llantas", new Vector3(-35.6f, 0f, -68.6f), 0f, p);
            ColocarS2Interiores.Modelo(m, L + "L04_Llantas", new Vector3(-30.2f + Fondo, 0f, -74.6f), 40f, p);
            ColocarS2Interiores.Modelo(m, C + "C16_Tambo", new Vector3(-36.1f, 0f, -75.1f), 0f, p);
            ColocarS2Interiores.Modelo(m, C + "C16_Tambo", new Vector3(-35.45f, 0f, -75.15f), 0f, p);
            ColocarS2Interiores.Modelo(m, C + "C16_Tambo", new Vector3(-28.7f + Fondo, 0f, -69.9f), 0f, p);
            ColocarS2Interiores.Modelo(m, C + "CY03_MazoCables", new Vector3(-28.2f + Fondo, 0f, -73.0f), 90f, p + 1.6f, sinCollider: true);
            ColocarS2Interiores.Modelo(m, C + "CY03_MazoCables", new Vector3(-34.0f, 0f, -67.85f), 0f, p + 1.6f, sinCollider: true);
            foreach (float x in new[] { -33.8f, -29f, -24.2f })
            {
                GameObject lampara = ColocarS2Interiores.Modelo(m, "Assets/Arte/Clandestino/Prefabs/A05_LamparaColgante", new Vector3(x, 0f, -71.7f), 90f, TallerTecho - 0.05f, 0.5f, colgar: true);
                if (lampara != null)
                {
                    lampara.name = "Lampara_Taller";
                }
            }
        }

        private static GameObject Pieza(Transform padre, string nombre, Material m, Vector3 centro, Vector3 tam, bool conCollider)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = nombre;
            c.transform.SetParent(padre, false);
            c.transform.position = centro;
            c.transform.localScale = tam;
            c.GetComponent<Renderer>().sharedMaterial = m;
            if (!conCollider)
            {
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
            return c;
        }

        private static void Luces(Transform raiz)
        {
            Transform l = Grupo(raiz, "Luces");
            Luz(l, "Tubo_Calido_1", new Vector3(-33.8f, TallerTecho - 0.7f, -71.7f), Calido, 9f, 11f);
            Luz(l, "Tubo_Calido_2", new Vector3(-29f, TallerTecho - 0.7f, -71.7f), Calido, 8f, 11f);
            Luz(l, "Tubo_Calido_3", new Vector3(-24.2f, TallerTecho - 0.7f, -71.7f), Calido, 8f, 11f);
            Luz(l, "Relleno_Fondo", new Vector3(-29.0f + Fondo, 2.2f, -71.7f), Calido, 4f, 8f);
            Luz(l, "Neon_Magenta", new Vector3(-31.0f + Fondo, 2.6f, -68.1f), Magenta, 3f, 5f);
            Luz(l, "Neon_Turquesa", new Vector3(-28.5f + Fondo, 2.6f, -72.8f), Turquesa, 3f, 5f);
            Luz(l, "Bajo_Carro", new Vector3(-29f, 1.0f, -73.0f), Calido, 3f, 5f);
            Light foco = Luz(l, "Foco_Porton", new Vector3(-36.4f, TallerTecho - 0.3f, -71.7f), Calido, 6f, 12f);
            foco.type = LightType.Spot;
            foco.spotAngle = 110f;
            foco.transform.rotation = Quaternion.Euler(60f, 270f, 0f); // hacia la calle: se ve encendido desde la casa
        }

        private static Light Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
        {
            Light luz = new GameObject(nombre).AddComponent<Light>();
            luz.transform.SetParent(padre, false);
            luz.transform.position = pos;
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            luz.shadows = LightShadows.None;
            return luz;
        }

        // El LugarMundo del taller (el mapa lo busca por nombre) va al portón, y su ZonaInterior cubre el taller. Sin puerta: el portón está abierto.
        private static void LugarYZona(Transform raiz)
        {
            GameObject lugarGo = GameObject.Find(NombreLugarTaller);
            if (lugarGo == null)
            {
                lugarGo = new GameObject(NombreLugarTaller);
            }
            lugarGo.transform.SetPositionAndRotation(new Vector3(TallerX0, Suelo, (PortonZ0 + PortonZ1) * 0.5f), Quaternion.Euler(0f, 270f, 0f));
            foreach (string n in new[] { "PuertaTaller" })
            {
                Transform t = lugarGo.transform.Find(n);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            Transform entrada = lugarGo.transform.Find("Entrada");
            if (entrada == null)
            {
                entrada = new GameObject("Entrada").transform;
                entrada.SetParent(lugarGo.transform, false);
            }
            entrada.position = new Vector3(TallerX0 - 2f, Suelo, (PortonZ0 + PortonZ1) * 0.5f);
            LugarMundo lugar = lugarGo.GetComponent<LugarMundo>();
            if (lugar == null)
            {
                lugar = lugarGo.AddComponent<LugarMundo>();
            }
            SerializedObject lo = new SerializedObject(lugar);
            lo.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosLugar>("Assets/_Proyecto/Mundo/Datos/DatosLugar_Taller.asset");
            lo.FindProperty("entrada").objectReferenceValue = entrada;
            lo.ApplyModifiedPropertiesWithoutUndo();

            GameObject zonaGo = new GameObject("ZonaInterior");
            zonaGo.transform.SetParent(raiz, false);
            zonaGo.transform.position = new Vector3((TallerX0 + TallerX1) * 0.5f, Suelo + (TallerTecho - Suelo) * 0.5f, (TallerZ0 + TallerZ1) * 0.5f);
            BoxCollider caja = zonaGo.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(TallerX1 - TallerX0 - 0.1f, TallerTecho - Suelo - 0.1f, TallerZ1 - TallerZ0 - 0.1f);
            ZonaInterior zona = zonaGo.AddComponent<ZonaInterior>();
            SerializedObject zo = new SerializedObject(zona);
            zo.FindProperty("lugar").objectReferenceValue = lugar;
            zo.FindProperty("luces").arraySize = 0; // las luces siempre prendidas: el taller se ve desde la calle
            zo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Canepa atiende en el portón de su taller, mirando a la calle.
        private static void Canepa()
        {
            Transform canepa = ColocarS2.Buscar("GTA_T417/Apoyo_CANEPA");
            if (canepa != null)
            {
                canepa.SetPositionAndRotation(new Vector3(-35.4f, Suelo, -70.6f), Quaternion.Euler(0f, 270f, 0f));
            }
        }

        // ================================================================ Locales con puerta (consultorio, CIC)

        /// <summary>Las puertas de los locales del Doc (I+D+I) y de Tino (CIC), si tienen hoja, también giran con bisagra y abren con E.</summary>
        private static void Locales()
        {
            foreach (string ruta in RutasPuertasLocales)
            {
                Transform t = ColocarS2.Buscar(ruta);
                PuertaLocal puerta = t != null ? t.GetComponent<PuertaLocal>() : null;
                HojaPuerta hoja = t != null ? t.GetComponentInChildren<HojaPuerta>(true) : null;
                if (puerta == null || hoja == null)
                {
                    ColocarS2.Informe.AppendLine("  sin hoja de puerta: " + ruta);
                    continue;
                }
                Transform h = hoja.transform;
                Vector3 eje = Mathf.Abs(h.right.x) > Mathf.Abs(h.forward.x) ? Vector3.right : Vector3.forward;
                Bisagra(hoja, eje * 0.5f, null);
                SoloConE(puerta);
                ColocarS2.Informe.AppendLine("  bisagra y E: " + ruta);
            }
        }

        public static readonly string[] RutasPuertasLocales = { };

        // ================================================================ Capturas

        public static void Capturas()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("Logs/s2n");
            Transform r = ColocarS2.Buscar(RutaCuarto);
            Vector3 P(float x, float y, float z) => r.TransformPoint(new Vector3(x, y, z));
            ColocarS2.Foto(P(1.1f, 1.65f, -1.85f), P(-0.4f, 1.0f, 1.9f), "Logs/s2n/casa-cuarto.png", 70f);
            ColocarS2.LadoALado("docs/referencias-interiores/habitacion.png", "Logs/s2n/casa-cuarto.png", "docs/capturas/s2n-casa-cuarto.png");
            ColocarS2.Foto(new Vector3(-58.3f, 1.6f, -77.6f), new Vector3(-62.6f, 1.2f, -76f), "Logs/s2n/casa-pasillo.png", 70f);
            ColocarS2.Foto(P(-0.6f, 1.6f, 1.2f), P(1f, 1.1f, -2f), "Logs/s2n/casa-puerta-cuarto.png", 70f);
            ColocarS2.LadoALado("Logs/s2n/casa-pasillo.png", "Logs/s2n/casa-puerta-cuarto.png", "docs/capturas/s2n-casa-pasillo.png");
            ColocarS2.Foto(new Vector3(-36.0f, 1.7f, -68.4f), new Vector3(-29f, 1.6f, -74f), "Logs/s2n/taller-dentro.png", 70f);
            ColocarS2.LadoALado("docs/referencias-interiores/taller-mecanico.png", "Logs/s2n/taller-dentro.png", "docs/capturas/s2n-taller-mecanico-dentro.png");
            ColocarS2.Foto(new Vector3(-54.5f, 1.7f, -77.5f), new Vector3(-37f, 2.4f, -71.7f), "Logs/s2n/taller-desde-casa.png", 60f);
            ColocarS2.Foto(new Vector3(-44f, 1.7f, -71.7f), new Vector3(-32f, 2f, -71.7f), "Logs/s2n/taller-calle.png", 60f);
            ColocarS2.LadoALado("Logs/s2n/taller-desde-casa.png", "Logs/s2n/taller-calle.png", "docs/capturas/s2n-taller-mecanico-calle.png");
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }
    }
}
