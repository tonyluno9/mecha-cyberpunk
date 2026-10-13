using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-077: arma los 6 finales de calle del barrio. Cada uno se construye en un marco local: +z va hacia fuera del barrio,
    /// x cruza la calle y el muro de fondo ocupa z de 0 a 2. Retenes con patrulla y torretas al este y al norte, obras al
    /// oeste y al sur. Lo llama CreadorBarrio al armar el prefab Barrio (usa CreadorTodo.Crear, no este creador suelto).
    /// </summary>
    internal static class CreadorFinales
    {
        private const string RutaConfigTorreta = "Assets/_Proyecto/Mundo/Datos/ConfigTorreta_Mundo.asset";

        // Cierre (m): línea de jersey o valla, a 2 m del muro, que cubre calle (14 m) y banquetas (3 m por lado).
        private const float LineaCierre = -2f;
        private const float MitadCierre = 10.5f;
        private const float LargoPieza = 3f;
        private const int PiezasCierre = 7;
        private const float AltoJersey = 1.2f;
        private const float AltoRejaTotal = 3.4f;
        private const float GrosorJersey = 1.2f;
        private const float GrosorReja = 0.5f;
        private const float AltoValla = 3.2f;
        private const float GrosorValla = 0.5f;
        private const float GrosorMuro = 2f;
        private const float AltoMuroEste = 5f;
        private const float AltoMuro = 4f;
        private const float MargenMuro = 4f;

        // Calle norte-sur: centro en x = ±47 (mitad del paso de manzana).
        private const float CentroCalleNorteSur = 47f;

        // Patrulla greybox (m).
        private const float AnchoPatrulla = 1.9f;
        private const float LargoPatrulla = 4.6f;
        private const float LateralPatrulla = -4.5f;
        private const float ProfundidadPatrulla = -6f;
        private const float GiroPatrulla = 90f;

        // Letrero de la autopista: 100 px por metro.
        private const float PixelesPorMetro = 100f;
        private const float AnchoLetrero = 8f;
        private const float AltoLetrero = 1.6f;
        private const int TamanoLetrero = 70;
        private const int TamanoMinimoLetrero = 20;
        private const string TextoLetrero = "ALTO · CONTROL CORPORATIVO";

        private static readonly Color ColorTextoLetrero = new Color(1f, 0.3f, 0.2f);

        internal static void Crear(Transform padre)
        {
            ConfigTorreta config = ObtenerConfig();
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float mitadX = CreadorBarrio.MitadBarrioX;
            float mitadZ = CreadorBarrio.MitadBarrioZ;

            Transform reten = Cierre("Final_Reten", padre, new Vector3(mitadX, 0f, 0f), 90f);
            Muro(reten, "Muro_Este", 2f * mitadZ + MargenMuro, AltoMuroEste, "ConcretoOscuro");
            Reten(reten, config, fuente, 0f);

            Transform obra = Cierre("Final_Obra", padre, new Vector3(-mitadX, 0f, 0f), -90f);
            Muro(obra, "Valla_Obra_Oeste", 2f * mitadZ + MargenMuro, AltoMuro, "Valla");
            ObraConMaquinas(obra);

            Transform norteOeste = Cierre("Final_Reten_Norte_Oeste", padre, new Vector3(-CentroCalleNorteSur, 0f, mitadZ), 0f);
            Reten(norteOeste, config, fuente, 0.35f);
            Transform norteEste = Cierre("Final_Reten_Norte_Este", padre, new Vector3(CentroCalleNorteSur, 0f, mitadZ), 0f);
            Reten(norteEste, config, fuente, 0.17f);

            // El muro del norte es el de la autopista elevada (Final_Autopista).
            Transform sur = Vacio("Final_Obra_Sur", padre, Vector3.zero, Quaternion.identity);
            Cubo("Valla_Obra_Sur", sur, new Vector3(0f, AltoMuro / 2f, -mitadZ - GrosorMuro / 2f),
                new Vector3(2f * mitadX + MargenMuro, AltoMuro, GrosorMuro), "Valla");
            foreach (float cx in new[] { -CentroCalleNorteSur, CentroCalleNorteSur })
            {
                Transform cierre = Cierre("Cierre_" + (cx < 0f ? "Oeste" : "Este"), sur, new Vector3(cx, 0f, -mitadZ), 180f);
                ObraConTubos(cierre);
            }
        }

        private static ConfigTorreta ObtenerConfig()
        {
            ConfigTorreta config = AssetDatabase.LoadAssetAtPath<ConfigTorreta>(RutaConfigTorreta);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigTorreta>();
                AssetDatabase.CreateAsset(config, RutaConfigTorreta);
            }
            return config;
        }

        private static Transform Vacio(string nombre, Transform padre, Vector3 posicion, Quaternion rotacion)
        {
            return CreadorBarrio.Vacio(nombre, padre, posicion, rotacion);
        }

        private static GameObject Cubo(string nombre, Transform padre, Vector3 posicion, Vector3 escala, string material, bool conCollider = true)
        {
            return CreadorBarrio.Cubo(nombre, padre, posicion, escala, material, conCollider);
        }

        private static Transform Cierre(string nombre, Transform padre, Vector3 posicion, float giro)
        {
            return Vacio(nombre, padre, posicion, Quaternion.Euler(0f, giro, 0f));
        }

        private static void Muro(Transform t, string nombre, float ancho, float alto, string material)
        {
            Cubo(nombre, t, new Vector3(0f, alto / 2f, GrosorMuro / 2f), new Vector3(ancho, alto, GrosorMuro), material);
        }

        private static float Lateral(int i)
        {
            return -MitadCierre + LargoPieza / 2f + i * LargoPieza;
        }

        // ---------------------------------------------------------------- retén

        private static void Reten(Transform t, ConfigTorreta config, Font fuente, float faseTorreta)
        {
            for (int i = 0; i < PiezasCierre; i++)
            {
                Cubo("Jersey", t, new Vector3(Lateral(i), AltoJersey / 2f, LineaCierre), new Vector3(LargoPieza, AltoJersey, GrosorJersey), "Concreto");
            }
            Cubo("Reja", t, new Vector3(0f, (AltoJersey + AltoRejaTotal) / 2f, LineaCierre),
                new Vector3(2f * MitadCierre, AltoRejaTotal - AltoJersey, GrosorReja), "Reja");

            Cubo("Caseta", t, new Vector3(5.5f, 1.4f, -9f), new Vector3(3f, 2.8f, 3f), "Concreto");
            Cubo("TechoCaseta", t, new Vector3(5.5f, 2.95f, -9f), new Vector3(3.6f, 0.3f, 3.6f), "ConcretoOscuro");

            Patrulla(t, "Patrulla", new Vector3(LateralPatrulla, 0f, ProfundidadPatrulla), config, faseTorreta);
            LetreroAlto(t, fuente);
        }

        // Pórtico con el letrero, sobre las banquetas: no estorba el carril.
        private static void LetreroAlto(Transform t, Font fuente)
        {
            const float profundidad = -14f;
            const float lateralPoste = 9.6f;
            const float altoPoste = 6.2f;
            const float altoViga = 6f;
            const float altoLetrero = 5f;
            Cubo("PostePortico", t, new Vector3(-lateralPoste, altoPoste / 2f, profundidad), new Vector3(0.5f, altoPoste, 0.5f), "ConcretoOscuro");
            Cubo("PostePortico", t, new Vector3(lateralPoste, altoPoste / 2f, profundidad), new Vector3(0.5f, altoPoste, 0.5f), "ConcretoOscuro");
            Cubo("VigaPortico", t, new Vector3(0f, altoViga, profundidad), new Vector3(2f * lateralPoste + 0.5f, 0.5f, 0.5f), "ConcretoOscuro");
            Cubo("Aviso", t, new Vector3(0f, altoLetrero, profundidad), new Vector3(AnchoLetrero, AltoLetrero, 0.25f), "AvisoCorp", false);

            // El Canvas se lee mirando hacia su +Z: con yaw 0 se lee desde dentro del barrio.
            GameObject canvasObjeto = new GameObject("AvisoCanvas", typeof(RectTransform));
            canvasObjeto.transform.SetParent(t, false);
            canvasObjeto.transform.localPosition = new Vector3(0f, altoLetrero, profundidad - 0.15f);
            canvasObjeto.transform.localScale = Vector3.one / PixelesPorMetro;
            Canvas canvas = canvasObjeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObjeto.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(AnchoLetrero * PixelesPorMetro, AltoLetrero * PixelesPorMetro);

            GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform));
            textoObjeto.transform.SetParent(rect, false);
            Text texto = textoObjeto.AddComponent<Text>();
            texto.font = fuente;
            texto.text = TextoLetrero;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.fontStyle = FontStyle.Bold;
            texto.fontSize = TamanoLetrero;
            texto.color = ColorTextoLetrero;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = TamanoMinimoLetrero;
            texto.resizeTextMaxSize = TamanoLetrero;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;
            RectTransform rectTexto = texto.rectTransform;
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = Vector2.zero;
            rectTexto.offsetMax = Vector2.zero;
        }

        // Patrulla de seguridad corporativa estacionada atravesada: su eje largo queda a lo ancho de la calle.
        private static void Patrulla(Transform padre, string nombre, Vector3 posicion, ConfigTorreta config, float faseTorreta)
        {
            Transform p = Vacio(nombre, padre, posicion, Quaternion.Euler(0f, GiroPatrulla, 0f));
            Cubo("Carroceria", p, new Vector3(0f, 0.65f, 0f), new Vector3(AnchoPatrulla, 0.8f, LargoPatrulla), "Patrulla");
            Cubo("Cabina", p, new Vector3(0f, 1.35f, -0.2f), new Vector3(AnchoPatrulla - 0.2f, 0.65f, 2.4f), "Patrulla");
            Cubo("FranjaIzquierda", p, new Vector3(-AnchoPatrulla / 2f - 0.01f, 0.7f, 0f), new Vector3(0.04f, 0.3f, 2.4f), "PatrullaBlanco", false);
            Cubo("FranjaDerecha", p, new Vector3(AnchoPatrulla / 2f + 0.01f, 0.7f, 0f), new Vector3(0.04f, 0.3f, 2.4f), "PatrullaBlanco", false);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                foreach (float z in new[] { -1.5f, 1.5f })
                {
                    Cubo("Rueda", p, new Vector3(x, 0.25f, z), new Vector3(0.3f, 0.5f, 0.6f), "ConcretoOscuro", false);
                }
            }
            Cubo("BarraLuces", p, new Vector3(0f, 1.75f, -0.2f), new Vector3(1.2f, 0.1f, 0.35f), "ConcretoOscuro", false);
            GameObject rojo = Cubo("TorretaRoja", p, new Vector3(-0.3f, 1.87f, -0.2f), new Vector3(0.5f, 0.14f, 0.3f), "TorretaRoja", false);
            GameObject azul = Cubo("TorretaAzul", p, new Vector3(0.3f, 1.87f, -0.2f), new Vector3(0.5f, 0.14f, 0.3f), "TorretaAzul", false);
            Light luzRoja = CrearLuz("LuzRoja", rojo.transform, new Color(1f, 0.1f, 0.1f));
            Light luzAzul = CrearLuz("LuzAzul", azul.transform, new Color(0.15f, 0.3f, 1f));

            LuzTorreta torreta = p.gameObject.AddComponent<LuzTorreta>();
            SerializedObject so = new SerializedObject(torreta);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("visualRojo").objectReferenceValue = rojo.GetComponent<Renderer>();
            so.FindProperty("visualAzul").objectReferenceValue = azul.GetComponent<Renderer>();
            so.FindProperty("luzRoja").objectReferenceValue = luzRoja;
            so.FindProperty("luzAzul").objectReferenceValue = luzAzul;
            so.FindProperty("faseInicial").floatValue = faseTorreta;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Light CrearLuz(string nombre, Transform padre, Color color)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = Vector3.up * 3f;
            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
            return luz;
        }

        // ---------------------------------------------------------------- obras

        private static void Vallas(Transform t)
        {
            for (int i = 0; i < PiezasCierre; i++)
            {
                Cubo("Valla", t, new Vector3(Lateral(i), AltoValla / 2f, LineaCierre), new Vector3(LargoPieza, AltoValla, GrosorValla), i % 2 == 0 ? "Valla" : "RayaBlanca");
            }
            const int conos = 8;
            const float pasoConos = 1.7f;
            for (int i = 0; i < conos; i++)
            {
                Cubo("Cono", t, new Vector3(-conos * pasoConos / 2f + pasoConos / 2f + i * pasoConos, 0.35f, LineaCierre - 1.5f), new Vector3(0.45f, 0.7f, 0.45f), "Valla", false);
            }
        }

        private static void ObraConMaquinas(Transform t)
        {
            Vallas(t);
            Cubo("Excavadora", t, new Vector3(4.5f, 1.4f, -7f), new Vector3(4f, 2.2f, 3f), "Maquina");
            Cubo("BrazoExcavadora", t, new Vector3(4.5f, 3f, -7f), new Vector3(5f, 0.6f, 0.6f), "Maquina");
            GameObject monton = Cubo("MontonTierra", t, new Vector3(-4.5f, 1f, -7f), new Vector3(5f, 2f, 3.5f), "Tierra");
            monton.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        }

        private static void ObraConTubos(Transform t)
        {
            Vallas(t);
            Cubo("Tubos", t, new Vector3(4f, 0.6f, -6f), new Vector3(5f, 1.2f, 1.5f), "ConcretoOscuro");
            Cubo("TubosApilados", t, new Vector3(-4.5f, 0.6f, -7f), new Vector3(4f, 1.2f, 1.5f), "ConcretoOscuro");
        }
    }
}
