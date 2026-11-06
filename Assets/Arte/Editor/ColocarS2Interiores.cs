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
    /// S2 zona 3 (docs/playtest-05nov.md): el consultorio del Doc (I+D+I) y el taller de Tino (CIC) se arman en su propio local (están en la ruta de la
    /// demo: el Doc y Tino no se mueven) según docs/referencias-interiores/consultorio-doc.png y taller-tino.png; el taller del mecánico (Canepa, frente a
    /// la casa) es un cuarto aparte con fundido, como la habitación (taller-mecanico.png). Idempotente: rehace la raíz S2_Interiores y el cuarto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Interiores.Colocar
    /// </summary>
    public static class ColocarS2Interiores
    {
        public const string NombreRaiz = "S2_Interiores";
        public const string NombreTallerMecanico = "Cuarto_TallerMecanico";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        private const string T = "Assets/Arte/TallerCasa/Prefabs/";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const string L = "Assets/Arte/Callejon/Prefabs/";
        private const string V = "Assets/Arte/Vehiculos/Prefabs/";
        private const string RutaDatosTaller = "Assets/_Proyecto/Mundo/Datos/DatosLugar_Taller.asset";
        private static readonly Color Calido = new Color(1f, 0.72f, 0.42f);
        private static readonly Color Turquesa = new Color(0.3f, 1f, 0.9f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);
        private static readonly Color Cian = new Color(0.45f, 0.85f, 1f);
        private static readonly Color Fluorescente = new Color(0.85f, 1f, 0.95f);

        [MenuItem("Mecha Cyberpunk/S2/3 Interiores (Doc, Tino, mecánico)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Consultorio(raiz);
            TallerTino(raiz);
            TallerMecanico();
            ColocarT550.AislarDelSol();
            ColocarS2.Guardar("s2-interiores");
        }

        // ---------------------------------------------------------------- Consultorio del Doc (I+D+I)

        private static void Consultorio(Transform raiz)
        {
            Apagar("T546_Interiores/ConsultorioDoc");
            // La clínica de El Mercado que T-531 mudó aquí: su mostrador, letrero, cruz y tubo eran cubos. La interacción va por posición y se queda.
            foreach (string n in new[] { "MostradorClinica/Mesa", "MostradorClinica/Letrero", "CruzVerde", "Lampara_Tubo" })
            {
                Transform t = ColocarS2.Buscar("Barrio/Manzana_2/Lugar_MercadoNegro/Interior/Clinica/" + n);
                if (t != null)
                {
                    foreach (Renderer rend in t.GetComponents<Renderer>())
                    {
                        rend.enabled = false;
                    }
                    foreach (Collider col in t.GetComponents<Collider>())
                    {
                        col.enabled = false;
                    }
                }
            }
            Transform r = Grupo(raiz, "ConsultorioDoc");
            // Local de T-531: x -104 a -96, fondo z -16, frente z -10.2 (hacia la calle, +Z), piso 0.22, techo 3.75.
            Caja caja = new Caja(-103.9f, -96.1f, -15.9f, -10.45f, 0.22f, 3.72f);
            Forrar(r, caja, 2, 5, 2, 0.45f, frenteHaciaMasZ: true, centroVano: -100f, anchoVano: 2.6f);

            GameObject sillon = Modelo(r, I + "I07", new Vector3(-102.1f, 0f, -13.4f), 0f, caja.Piso, ejeLargoEnZ: true);
            Modelo(r, I + "I06", new Vector3(-102.1f, 0f, -13.4f), 0f, caja.Techo, colgar: true);
            Modelo(r, I + "I08", new Vector3(-103.6f, 0f, -11.7f), 90f, caja.Piso);              // vitrina de implantes, pared izquierda
            Modelo(r, I + "I08", new Vector3(-96.4f, 0f, -11.7f), 270f, caja.Piso);              // y en la derecha
            Modelo(r, I + "I09", new Vector3(-103.7f, 0f, -14.6f), 90f, caja.Piso);              // lavabos
            Modelo(r, I + "I09", new Vector3(-96.3f, 0f, -13.5f), 270f, caja.Piso);
            Modelo(r, T + "T08_RepisaTaller", new Vector3(-102.1f, 0f, -15.7f), 180f, caja.Piso, escala: 0.85f);   // repisa de medicinas al fondo
            Modelo(r, C + "CY05_ServidorCallejero", new Vector3(-96.9f, 0f, -15.5f), 180f, caja.Piso);
            GameObject gab = Modelo(r, T + "T04_GabineteHerramientas", new Vector3(-98.9f, 0f, -15.5f), 180f, caja.Piso, escala: 0.8f);
            float sobre = gab != null ? ArteT250.Limites(gab).max.y : caja.Piso + 1f;
            Modelo(r, I + "I10", new Vector3(-99.3f, 0f, -15.45f), 200f, sobre, sinCollider: true);   // monitores de signos vitales
            Modelo(r, I + "I10", new Vector3(-98.5f, 0f, -15.45f), 165f, sobre, sinCollider: true);
            Modelo(r, C + "C17_Huacales", new Vector3(-103.2f, 0f, -10.9f), 10f, caja.Piso);
            Modelo(r, C + "C16_Tambo", new Vector3(-96.7f, 0f, -10.9f), 0f, caja.Piso);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-103.85f, 0f, -13.0f), 90f, caja.Piso + 1.1f, sinCollider: true);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-97.8f, 0f, -15.85f), 180f, caja.Piso + 1.1f, sinCollider: true);

            Transform luces = Grupo(r, "Luces");
            Luz(luces, "Tubo_Cian_1", new Vector3(-101.5f, caja.Techo - 0.2f, -12.4f), Cian, 6f, 8f);
            Luz(luces, "Tubo_Cian_2", new Vector3(-98.5f, caja.Techo - 0.2f, -14.2f), Cian, 5.5f, 8f);
            Luz(luces, "Tubo_Fluorescente", new Vector3(-100f, caja.Techo - 0.2f, -11.2f), Fluorescente, 4f, 7f);
            Luz(luces, "Relleno_Fondo", new Vector3(-100f, 2.2f, -15.2f), Cian, 2.5f, 6f);
            Luz(luces, "Vitrina_Izq", new Vector3(-103.3f, 1.4f, -11.7f), Turquesa, 1.5f, 2f);
            Luz(luces, "Vitrina_Der", new Vector3(-96.7f, 1.4f, -11.7f), Turquesa, 1.5f, 2f);
            Luz(luces, "Foco_Calido_Pared", new Vector3(-103.5f, 2.6f, -12.8f), Calido, 1.6f, 4f);
            Luz(luces, "Brillo_Monitores", new Vector3(-98.9f, sobre + 0.3f, -15.0f), Turquesa, 1.2f, 2.5f);
            Foco(luces, "Lampara_Cirugia", new Vector3(-102.1f, caja.Techo - 1.1f, -13.4f), Calido, 9f, 4.5f, 55f);
            ApagarLuzDelLocal("T531_Locales/IDI/Luz_IDI");
            OcultarCascaron("T531_Locales/IDI");
            ColocarS2.Informe.AppendLine("Consultorio: " + r.childCount + " piezas; sillón " + (sillon != null ? ArteT250.Limites(sillon).size.ToString("F2") : "FALTA"));
        }

        // ---------------------------------------------------------------- Taller de Tino (CIC)

        private static void TallerTino(Transform raiz)
        {
            Apagar("T546_Interiores/TallerTino");
            Transform r = Grupo(raiz, "TallerTino");
            // El mostrador y la repisa de cubos de T-417 se quedan solo como colisión (no se ven).
            foreach (string n in new[] { "Mesa", "MesaTapa", "Pared", "Repisa_0", "Repisa_1", "Repisa_2" })
            {
                OcultarCubo("GTA_T417/Lugar_CIC/" + n);
            }
            Transform cic = ColocarS2.Buscar("GTA_T417/Lugar_CIC");
            if (cic != null)
            {
                foreach (Transform t in cic.Cast<Transform>().Where(t => t.name.StartsWith("Caja_")))
                {
                    OcultarCubo("GTA_T417/Lugar_CIC/" + t.name);
                }
            }
            // Local de T-531: x -77.3 a -69.3, fondo z 16, frente z 10.2 (hacia la calle, -Z).
            Caja caja = new Caja(-77.2f, -69.4f, 10.45f, 15.9f, 0.22f, 3.72f);
            Forrar(r, caja, 3, 5, 1, 0.38f, frenteHaciaMasZ: false, centroVano: -73.3f, anchoVano: 2.6f);

            Modelo(r, T + "T08_RepisaTaller", new Vector3(-75.6f, 0f, 15.6f), 180f, caja.Piso, escala: 0.8f);    // repisa con cajas y piezas
            Modelo(r, C + "C17_Huacales", new Vector3(-76.6f, 0f, 13.2f), 80f, caja.Piso);
            Cuadro(r, "Pizarron", "Assets/Arte/Interiores/Materiales/T546_Pizarron_T546_Pizarron.mat", new Vector3(-71.6f, 2.05f, 15.9f), Vector3.back, new Vector2(2.0f, 1.25f));
            GameObject banco1 = Modelo(r, T + "T03_BancoTrabajo", new Vector3(-72.55f, 0f, 13.4f), 0f, caja.Piso);
            Modelo(r, T + "T03_BancoTrabajo", new Vector3(-74.05f, 0f, 13.4f), 0f, caja.Piso);
            float mesa = banco1 != null ? ArteT250.Limites(banco1).max.y : caja.Piso + 0.9f;
            Modelo(r, I + "I21", new Vector3(-72.2f, 0f, 13.5f), 210f, mesa, sinCollider: true);              // lámpara de brazo
            GameObject dron = Modelo(r, C + "CY07b_Dron", new Vector3(-73.0f, 0f, 13.35f), 30f, mesa, escala: 0.35f, sinCollider: true);
            Modelo(r, I + "I20", new Vector3(-76.9f, 0f, 14.4f), 90f, caja.Piso);                            // estante de drones
            Modelo(r, T + "T08_RepisaTaller", new Vector3(-77.0f, 0f, 11.9f), 90f, caja.Piso, escala: 0.7f);
            GameObject escritorio = Modelo(r, I + "I02", new Vector3(-69.8f, 0f, 14.6f), 270f, caja.Piso);    // compu con monitores
            float tablero = escritorio != null ? ArteT250.Limites(escritorio).max.y : caja.Piso + 1f;
            Modelo(r, I + "I10", new Vector3(-69.7f, 0f, 12.9f), 260f, caja.Piso + 0.95f, sinCollider: true);
            Modelo(r, I + "I03", new Vector3(-70.6f, 0f, 14.3f), 100f, caja.Piso);
            Modelo(r, T + "T05_Generador", new Vector3(-76.5f, 0f, 10.9f), 20f, caja.Piso);
            Modelo(r, L + "L04_Llantas", new Vector3(-69.9f, 0f, 11.0f), 0f, caja.Piso);
            Modelo(r, C + "C16_Tambo", new Vector3(-70.0f, 0f, 15.5f), 0f, caja.Piso);
            Modelo(r, T + "T04_GabineteHerramientas", new Vector3(-74.0f, 0f, 15.55f), 180f, caja.Piso, escala: 0.7f);
            Modelo(r, C + "CY07b_Dron", new Vector3(-75.2f, 0f, 12.3f), 0f, caja.Techo - 0.9f, escala: 0.5f, sinCollider: true);
            Modelo(r, C + "CY07b_Dron", new Vector3(-71.4f, 0f, 12.0f), 140f, caja.Techo - 1.0f, escala: 0.5f, sinCollider: true);
            Modelo(r, C + "CY03_MazoCables", new Vector3(-69.45f, 0f, 11.9f), 270f, caja.Piso + 1.1f, sinCollider: true);

            Transform luces = Grupo(r, "Luces");
            SerieFocos(r, new Vector3(-77.1f, caja.Techo - 0.15f, 11.0f), new Vector3(-69.5f, caja.Techo - 0.2f, 14.8f), 16);
            SerieFocos(r, new Vector3(-77.1f, caja.Techo - 0.2f, 15.5f), new Vector3(-69.5f, caja.Techo - 0.15f, 11.6f), 16);
            Luz(luces, "Serie_1", new Vector3(-75.0f, caja.Techo - 0.6f, 12.5f), Calido, 1.2f, 5f);
            Luz(luces, "Serie_2", new Vector3(-71.5f, caja.Techo - 0.6f, 14.0f), Calido, 1.2f, 5f);
            Luz(luces, "Tubo_Calido_Pared", new Vector3(-75.4f, 2.9f, 15.4f), Calido, 2.2f, 6f);
            Foco(luces, "Lampara_Banco", new Vector3(-72.4f, mesa + 0.6f, 13.5f), Calido, 6f, 3.5f, 70f);
            Luz(luces, "Brillo_Monitores", new Vector3(-70.2f, tablero + 0.2f, 14.0f), Turquesa, 1.4f, 3f);
            Luz(luces, "Relleno", new Vector3(-73.3f, 2.6f, 11.5f), Calido, 1.0f, 6f);
            ApagarLuzDelLocal("T531_Locales/CIC/Luz_CIC");
            OcultarCascaron("T531_Locales/CIC");
            Luz(luces, "Relleno_Fondo", new Vector3(-72.0f, 2.4f, 15.0f), Calido, 1.6f, 5f);
            ColocarS2.Informe.AppendLine("Taller de Tino: " + r.childCount + " piezas" + (dron == null ? " (FALTA dron)" : ""));
        }

        // ---------------------------------------------------------------- Taller del mecánico (cuarto aparte)

        private static void TallerMecanico()
        {
            Transform cuartos = ColocarS2.Buscar(ColocarT550.NombreRaiz);
            foreach (string viejo in new[] { NombreTallerMecanico, "PuertaCuarto_Lugar_TallerMecanico" })
            {
                Transform t = cuartos.Find(viejo);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            Object.DestroyImmediate(GameObject.Find("Lugar_TallerMecanico"));

            ColocarT550.Cuarto c = ColocarT550.CrearCuarto(cuartos, NombreTallerMecanico, 1, new Vector3(9f, 4.4f, 8f), 3, 5, 0.55f, IdLugar.Taller);
            Transform r = c.Raiz;
            Vector3 P(float x, float z) => r.TransformPoint(new Vector3(x, 0f, z));
            float piso = r.position.y;
            float techo = piso + 4.4f;
            // Puerta interior en la pared sur (por donde se entra): la hoja se ve de este lado.
            ColocarT550.Puerta(r, ColocarT550.Mat("T550_Madera", new Color(0.30f, 0.18f, 0.09f)), new Vector3(3.5f, 0f, -4f), 0f);
            // Al fondo, la cortina de la cochera (una fachada de local con su cortina, vista desde dentro).
            Modelo(r, C + "C01_FachadaLocal_cy", P(0f, 3.85f), 0f, piso, escala: 1.15f, sinCollider: true);
            // El carro del cliente con el cofre hacia la grúa, la grúa de motor amarilla y lo de un taller.
            Modelo(r, V + "V01a_CarroViejito", P(-0.6f, 0.6f), 90f, piso, sinCollider: false);
            Modelo(r, T + "T07_GruaMotor", P(2.0f, 1.6f), 270f, piso);
            Modelo(r, T + "T04_GabineteHerramientas", P(-3.9f, -1.6f), 90f, piso);
            Modelo(r, T + "T01_ParedTaller", P(-4.4f, 1.4f), 90f, piso, escala: 0.75f);
            Modelo(r, T + "T03_BancoTrabajo", P(4.0f, 1.8f), 270f, piso);
            Modelo(r, T + "T06_Soldadora", P(4.0f, -0.2f), 270f, piso);
            Modelo(r, T + "T05_Generador", P(-3.8f, 3.0f), 90f, piso);
            Modelo(r, L + "L04_Llantas", P(3.7f, 3.2f), 0f, piso);
            Modelo(r, L + "L04_Llantas", P(1.4f, -3.5f), 40f, piso);
            Modelo(r, C + "C16_Tambo", P(-3.9f, -3.4f), 0f, piso);
            Modelo(r, C + "C16_Tambo", P(-3.3f, -3.5f), 0f, piso);
            Modelo(r, C + "C16_Tambo", P(2.9f, 3.4f), 0f, piso);
            Modelo(r, C + "CY03_MazoCables", P(-4.45f, -0.4f), 90f, piso + 1.4f, sinCollider: true);
            Modelo(r, C + "CY03_MazoCables", P(4.45f, 0.8f), 270f, piso + 1.4f, sinCollider: true);
            Luz(c.Luces, "Tubo_Calido_1", P(-1.5f, -0.5f) + Vector3.up * (techo - 0.3f), Calido, 2.6f, 8f);
            Luz(c.Luces, "Tubo_Calido_2", P(1.8f, 2.4f) + Vector3.up * (techo - 0.3f), Calido, 2.4f, 8f);
            Luz(c.Luces, "Neon_Magenta", P(-4.2f, 1.4f) + Vector3.up * 2.4f, Magenta, 1.2f, 3.5f);
            Luz(c.Luces, "Neon_Turquesa", P(4.2f, -0.2f) + Vector3.up * 2.4f, Turquesa, 1.2f, 3.5f);
            Luz(c.Luces, "Relleno", P(0f, -3f) + Vector3.up * 2.2f, Calido, 0.8f, 6f);

            // Afuera: la puerta del taller en la fachada detrás de Canepa (Edificio_45, cara oeste x -37.4), con su LugarMundo.
            Vector3 puertaCalle = new Vector3(-37.35f, 0.22f, -70.2f);
            GameObject lugarGo = new GameObject("Lugar_TallerMecanico");
            lugarGo.transform.position = puertaCalle;
            Transform entrada = new GameObject("Entrada").transform;
            entrada.SetParent(lugarGo.transform, false);
            entrada.position = puertaCalle + new Vector3(-1.6f, 0f, 0f);
            LugarMundo lugar = lugarGo.AddComponent<LugarMundo>();
            SerializedObject lo = new SerializedObject(lugar);
            lo.FindProperty("datos").objectReferenceValue = DatosTaller();
            lo.FindProperty("entrada").objectReferenceValue = entrada;
            lo.ApplyModifiedPropertiesWithoutUndo();
            GameObject fachada = Modelo(lugarGo.transform, T + "H01a_ParedPuerta", puertaCalle + new Vector3(-0.12f, 0f, 0f), 270f, puertaCalle.y, sinCollider: true);
            if (fachada != null)
            {
                fachada.name = "PuertaTaller";
            }

            SerializedObject zo = new SerializedObject(r.Find("ZonaInterior").GetComponent<ZonaInterior>());
            zo.FindProperty("lugar").objectReferenceValue = lugar;
            Light[] luces = c.Luces.GetComponentsInChildren<Light>(true);
            SerializedProperty arr = zo.FindProperty("luces");
            arr.arraySize = luces.Length;
            for (int i = 0; i < luces.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            zo.ApplyModifiedPropertiesWithoutUndo();
            Transform salida = r.Find("SalidaExterior");
            salida.SetPositionAndRotation(entrada.position, Quaternion.LookRotation(Vector3.left, Vector3.up));

            GameObject prefabPantallas = PantallasDeLaCasa();
            GameObject fuera = new GameObject("PuertaCuarto_Lugar_TallerMecanico");
            fuera.transform.SetParent(cuartos, false);
            fuera.transform.SetPositionAndRotation(puertaCalle + new Vector3(-0.3f, 1.1f, 0f), Quaternion.LookRotation(Vector3.left, Vector3.up));
            BoxCollider cf = fuera.AddComponent<BoxCollider>();
            cf.isTrigger = true;
            cf.size = new Vector3(1.4f, 2.2f, 0.5f);
            Configurar(fuera.AddComponent<PuertaCuarto>(), c.Datos, false, prefabPantallas);

            GameObject dentro = new GameObject("PuertaCuarto_Interior");
            dentro.transform.SetParent(r, false);
            dentro.transform.localPosition = new Vector3(3.5f, 1.1f, -4f + 0.3f);
            BoxCollider cd = dentro.AddComponent<BoxCollider>();
            cd.isTrigger = true;
            cd.size = new Vector3(1.0f, 2.2f, 0.5f);
            Configurar(dentro.AddComponent<PuertaCuarto>(), c.Datos, true, prefabPantallas);
            c.Aparicion.localPosition = new Vector3(3.5f, 0.05f, -4f + 1.4f);
            ColocarS2.Informe.AppendLine("Taller del mecánico: cuarto 9 x 4.4 x 8 m en " + r.position + "; puerta de la calle en " + puertaCalle);
        }

        private static DatosLugar DatosTaller()
        {
            DatosLugar d = AssetDatabase.LoadAssetAtPath<DatosLugar>(RutaDatosTaller);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<DatosLugar>();
                AssetDatabase.CreateAsset(d, RutaDatosTaller);
            }
            SerializedObject so = new SerializedObject(d);
            so.FindProperty("id").enumValueIndex = (int)IdLugar.Taller;
            so.FindProperty("nombre").stringValue = "Taller de Canepa";
            so.FindProperty("horario").FindPropertyRelative("siempreAbierto").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(d);
            return d;
        }

        private static GameObject PantallasDeLaCasa()
        {
            PuertaCuarto casa = Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.name == "PuertaCuarto_Lugar_Casa");
            return casa == null ? null : new SerializedObject(casa).FindProperty("prefabPantallas").objectReferenceValue as GameObject;
        }

        private static void Configurar(PuertaCuarto puerta, CuartoInterior datos, bool esSalida, GameObject prefabPantallas)
        {
            SerializedObject so = new SerializedObject(puerta);
            so.FindProperty("cuarto").objectReferenceValue = datos;
            so.FindProperty("esSalida").boolValue = esSalida;
            so.FindProperty("prefabPantallas").objectReferenceValue = prefabPantallas;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Ayudas

        public readonly struct Caja
        {
            public readonly float X0, X1, Z0, Z1, Piso, Techo;
            public Caja(float x0, float x1, float z0, float z1, float piso, float techo)
            {
                X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Piso = piso; Techo = techo;
            }
        }

        /// <summary>Paredes, techo y piso del kit X por dentro del local, y el frente cerrado con un vano de entrada (ladrillo X03 por fuera).</summary>
        private static void Forrar(Transform r, Caja k, int pared, int techo, int piso, float oscuroPiso, bool frenteHaciaMasZ, float centroVano, float anchoVano)
        {
            Material mp = ColocarT550.Mat("S2_Pared_X0" + pared, new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(pared));
            Material mt = ColocarT550.Mat("S2_Techo_X0" + techo, new Color(0.5f, 0.47f, 0.44f), ImportarT550.RutaMaterialPared(techo));
            Material mf = ColocarT550.Mat("S2_Piso_X0" + piso, new Color(0.42f, 0.4f, 0.38f) * oscuroPiso * 2f, ImportarT550.RutaMaterialPared(piso));
            Material ladrillo = ColocarT550.Mat("S2_Pared_X03", new Color(0.62f, 0.6f, 0.58f), ImportarT550.RutaMaterialPared(3));
            Transform f = Grupo(r, "Forro");
            float y0 = k.Piso + 0.012f, y1 = k.Techo;
            Vector3 p(float x, float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(f, "Piso", mf, p(k.X0, y0, k.Z0), p(k.X0, y0, k.Z1), p(k.X1, y0, k.Z1), p(k.X1, y0, k.Z0), Vector3.up, -0.05f);
            ColocarT550.Superficie(f, "Techo", mt, p(k.X0, y1, k.Z0), p(k.X1, y1, k.Z0), p(k.X1, y1, k.Z1), p(k.X0, y1, k.Z1), Vector3.down, 0.05f);
            ColocarT550.Superficie(f, "Pared_Izquierda", mp, p(k.X0, y0, k.Z0), p(k.X0, y1, k.Z0), p(k.X0, y1, k.Z1), p(k.X0, y0, k.Z1), Vector3.right, -0.05f);
            ColocarT550.Superficie(f, "Pared_Derecha", mp, p(k.X1, y0, k.Z1), p(k.X1, y1, k.Z1), p(k.X1, y1, k.Z0), p(k.X1, y0, k.Z0), Vector3.left, 0.05f);
            float zFondo = frenteHaciaMasZ ? k.Z0 : k.Z1;
            float zFrente = frenteHaciaMasZ ? k.Z1 : k.Z0;
            Vector3 haciaDentroFondo = frenteHaciaMasZ ? Vector3.forward : Vector3.back;
            ColocarT550.Superficie(f, "Pared_Fondo", mp, p(k.X0, y0, zFondo), p(k.X0, y1, zFondo), p(k.X1, y1, zFondo), p(k.X1, y0, zFondo), haciaDentroFondo, frenteHaciaMasZ ? -0.05f : 0.05f);
            // Frente: dos paños a los lados del vano y uno encima (alto del vano 2.4 m), por dentro y por fuera.
            float v0 = centroVano - anchoVano * 0.5f, v1 = centroVano + anchoVano * 0.5f, alto = k.Piso + 2.4f;
            Vector3 haciaDentro = -haciaDentroFondo;   // del frente hacia dentro
            Vector3 haciaFuera = haciaDentroFondo;
            void Pano(string n, float xa, float xb, float ya, float yb)
            {
                ColocarT550.Superficie(f, n + "_Dentro", mp, p(xa, ya, zFrente - haciaFuera.z * 0.02f), p(xa, yb, zFrente - haciaFuera.z * 0.02f), p(xb, yb, zFrente - haciaFuera.z * 0.02f), p(xb, ya, zFrente - haciaFuera.z * 0.02f), haciaDentro, haciaFuera.z * 0.08f);
                ColocarT550.Superficie(f, n + "_Fuera", ladrillo, p(xa, ya, zFrente + haciaFuera.z * 0.1f), p(xa, yb, zFrente + haciaFuera.z * 0.1f), p(xb, yb, zFrente + haciaFuera.z * 0.1f), p(xb, ya, zFrente + haciaFuera.z * 0.1f), haciaFuera, 0f);
            }
            Pano("Frente_Izq", k.X0, v0, y0, y1);
            Pano("Frente_Der", v1, k.X1, y0, y1);
            Pano("Frente_Arriba", v0, v1, alto, y1);
            // Los superficies de frente no llevan collider de más en el vano: el collider de cada paño cubre solo su paño.
            foreach (Renderer rend in f.GetComponentsInChildren<Renderer>())
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
            // El NavMesh se horneó con el local abierto: el frente nuevo talla su hueco para que la IA no camine a través de él.
            foreach (Transform t in f.Cast<Transform>().Where(t => t.name.StartsWith("Frente_") && t.name.EndsWith("_Dentro")))
            {
                Obstaculo(t.gameObject);
            }
        }

        /// <summary>Obstáculo de navegación que talla el NavMesh horneado, con la medida de su BoxCollider (un poco más grueso).</summary>
        public static void Obstaculo(GameObject g)
        {
            BoxCollider caja = g.GetComponent<BoxCollider>();
            UnityEngine.AI.NavMeshObstacle o = g.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (o == null)
            {
                o = g.AddComponent<UnityEngine.AI.NavMeshObstacle>(); // sin ??: el null de Unity no es null de C#
            }
            o.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            if (caja != null)
            {
                o.center = caja.center;
                o.size = caja.size + new Vector3(0.1f, 0f, 0.1f);
            }
            else
            {
                Bounds b = ArteT250.Limites(g);
                o.center = g.transform.InverseTransformPoint(b.center);
                o.size = new Vector3(b.size.x / Mathf.Max(g.transform.lossyScale.x, 0.01f), b.size.y / Mathf.Max(g.transform.lossyScale.y, 0.01f), b.size.z / Mathf.Max(g.transform.lossyScale.z, 0.01f));
            }
            o.carving = true;
            o.carveOnlyStationary = true;
        }

        /// <summary>Pone un prefab con su centro en planta en 'centro' (x, z del mundo o local del cuarto ya convertido), girado 'yaw', apoyado en 'y'
        /// (o colgando de 'y' si 'colgar'). 'ejeLargoEnZ' gira 90° más si el lado largo queda en X.</summary>
        public static GameObject Modelo(Transform padre, string ruta, Vector3 centro, float yaw, float y, float escala = 1f, bool colgar = false, bool sinCollider = false, bool ejeLargoEnZ = false)
        {
            string archivo = ruta.EndsWith(".prefab") ? ruta : ruta + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(archivo);
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA " + archivo);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = prefab.name;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            g.transform.localScale *= escala;
            if (ejeLargoEnZ)
            {
                Bounds b0 = ArteT250.Limites(g);
                if (b0.size.x > b0.size.z)
                {
                    g.transform.rotation = Quaternion.Euler(0f, yaw + 90f, 0f);
                }
            }
            Bounds b = ArteT250.Limites(g);
            Vector3 d = new Vector3(centro.x - b.center.x, colgar ? y - b.max.y : y - b.min.y, centro.z - b.center.z);
            g.transform.position += d;
            if (colgar || sinCollider)
            {
                foreach (Collider col in g.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            return g;
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static void Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
        {
            Light l = new GameObject(nombre).AddComponent<Light>();
            l.transform.SetParent(padre, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }

        private static void Foco(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance, float angulo)
        {
            Light l = new GameObject(nombre).AddComponent<Light>();
            l.transform.SetParent(padre, false);
            l.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
            l.type = LightType.Spot;
            l.spotAngle = angulo;
            l.innerSpotAngle = angulo * 0.5f;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.Soft;
        }

        // Serie de focos colgando en curva entre dos puntos del techo: cable negro (LineRenderer) y focos cálidos emisivos.
        private static void SerieFocos(Transform padre, Vector3 a, Vector3 b, int focos)
        {
            Transform serie = Grupo(padre, "SerieFocos");
            Material foco = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Interiores/Materiales/T546_Foco.mat");
            Material cable = ColocarT550.Mat("S2_Cable", new Color(0.04f, 0.04f, 0.045f));
            const int Tramos = 24;
            Vector3[] puntos = new Vector3[Tramos + 1];
            for (int i = 0; i <= Tramos; i++)
            {
                float t = (float)i / Tramos;
                puntos[i] = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.45f;
            }
            LineRenderer lr = serie.gameObject.AddComponent<LineRenderer>();
            lr.positionCount = puntos.Length;
            lr.SetPositions(puntos);
            lr.widthMultiplier = 0.012f;
            lr.sharedMaterial = cable;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 1; i <= focos; i++)
            {
                float t = (float)i / (focos + 1);
                Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.45f + 0.06f);
                GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                f.name = "Foco";
                f.transform.SetParent(serie, false);
                f.transform.position = pos;
                f.transform.localScale = Vector3.one * 0.07f;
                Renderer rend = f.GetComponent<Renderer>();
                rend.sharedMaterial = foco;
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void Cuadro(Transform padre, string nombre, string rutaMaterial, Vector3 pos, Vector3 frente, Vector2 tam)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = nombre;
            q.transform.SetParent(padre, false);
            q.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-frente, Vector3.up));
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            q.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        }

        private static void Apagar(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private static void OcultarCubo(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            Renderer rend = t != null ? t.GetComponent<Renderer>() : null;
            if (rend != null)
            {
                rend.enabled = false;
            }
        }

        // El cascarón de cubos del local (techo y muros) ya lo tapa el forro: deja de verse, su colisión se queda.
        private static void OcultarCascaron(string ruta)
        {
            foreach (string n in new[] { "Techo", "MuroFondo", "MuroIzq", "MuroDer", "Piso" })
            {
                OcultarCubo(ruta + "/" + n);
            }
        }

        private static void ApagarLuzDelLocal(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            Light l = t != null ? t.GetComponent<Light>() : null;
            if (l != null)
            {
                l.enabled = false;
            }
        }
    }
}
