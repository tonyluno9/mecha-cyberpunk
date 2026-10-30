using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-417 del creador de la ciudad de pandillas: los 5 personajes de apoyo (muñecos de prueba con letrero), el CIC en la banqueta de Suburbios con
    /// su tienda de mejoras de armas (DatosTienda_CIC) y los peatones al mínimo. Idempotente; la raíz es GTA_T417.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT417 = "GTA_T417";
        private const string CarpetaApoyo = "Assets/_Proyecto/Progresion/Apoyo/Datos";
        private const string RutaTiendaCic = "Assets/_Proyecto/Economia/Datos/DatosTienda_CIC.asset";
        private const string RutaTiendaMercado = "Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset";
        private const string RutaConfigPeatones = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        private const string RutaCatalogoObjetosT417 = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";
        private const int PeatonesDeDiaT417 = 4;
        private const int PeatonesDeNocheT417 = 6;
        private const float DistanciaMinCic = 10f;
        private const float DistanciaMaxCic = 25f;
        private const float DistanciaMinPuertaCic = 7f;

        private static readonly Color ColorCic = new Color(0.1f, 0.9f, 0.85f);

        private struct DefinicionApoyo
        {
            public string Id, Titulo, Nombre, Funcion;
            public Color Color;
            public ServicioApoyo Servicio;
            public int Costo;
            public string[] Frases;
        }

        private static readonly DefinicionApoyo[] PersonajesT417 =
        {
            new DefinicionApoyo { Id = "inventor", Titulo = "EL INVENTOR", Nombre = "TINO", Funcion = "MEJORA TUS ARMAS", Color = new Color(1f, 0.5f, 0.1f),
                Servicio = ServicioApoyo.Mejoras, Frases = new[] { "Dame fierro y te lo dejo mejor.", "Un arma buena es una amiga.", "Todo se puede mejorar, mijo." } },
            new DefinicionApoyo { Id = "doc", Titulo = "EL DOC", Nombre = "JOEL", Funcion = "TE CURA", Color = Color.white,
                Servicio = ServicioApoyo.Curar, Costo = 50, Frases = new[] { "Listo, como nuevo. Los implantes: pronto.", "Ten más cuidado, mijo.", "Vuelve cuando quieras." } },
            new DefinicionApoyo { Id = "hacker", Titulo = "EL HACKER", Nombre = "SERGIO", Funcion = "TE DA INFORMACIÓN", Color = new Color(0.1f, 0.9f, 0.85f),
                Servicio = ServicioApoyo.Informacion, Frases = new[] { "Hoy la red anda tranquila.", "Todo se sabe, si sabes dónde mirar.", "Sin pistas por ahora." } },
            new DefinicionApoyo { Id = "mecanico", Titulo = "EL MECÁNICO", Nombre = "CANEPA", Funcion = "ARREGLA TU CARRO", Color = new Color(1f, 0.85f, 0.1f),
                Servicio = ServicioApoyo.Taller, Costo = 100, Frases = new[] { "Pintado. Ahora nadie te conoce.", "Otro color y a rodar.", "Así sí." } },
            new DefinicionApoyo { Id = "director", Titulo = "EL DIRECTOR", Nombre = "ESTRADA", Funcion = "SOLO PLATICA", Color = new Color(0.65f, 0.65f, 0.7f),
                Servicio = ServicioApoyo.Platica, Frases = new[] { "Yo solo miro, no peleo.", "Esta ciudad da para una película.", "Siéntate, mijo, platiquemos." } }
        };

        [MenuItem("Mecha Cyberpunk/Pandillas/T-417 Profes y peatones")]
        public static void T417()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T417Interno();
            Guardar(escena);
        }

        private static void T417Interno()
        {
            PonerPeatonesT417();
            DatosTienda tiendaCic = CrearTiendaCicT417();
            DatosPersonajeApoyo[] datos = new DatosPersonajeApoyo[PersonajesT417.Length];
            for (int i = 0; i < datos.Length; i++)
            {
                datos[i] = CrearDatosApoyoT417(PersonajesT417[i]);
            }

            GameObject anterior = BuscarRaiz(RaizT417);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            Transform raiz = new GameObject(RaizT417).transform;
            Physics.SyncTransforms();

            Transform casa = BuscarTransform(IdAparicion);
            Vector3 centroCasa = casa != null ? casa.position : Vector3.zero;

            // 1. El CIC en la banqueta de Suburbios.
            Vector3 puntoCic = ElegirPuntoCicT417(centroCasa, out Vector3 haciaCalle);
            Transform lugarCic = new GameObject("Lugar_CIC").transform;
            lugarCic.SetParent(raiz, false);
            lugarCic.SetPositionAndRotation(puntoCic, Quaternion.LookRotation(haciaCalle, Vector3.up));
            MostradorTienda mostrador = ArmarCicT417(lugarCic, tiendaCic);

            // 2. Los cinco personajes.
            Vector3 detrasMesa = puntoCic - haciaCalle * 1.2f;
            PonerPersonajeT417(raiz, datos[0], detrasMesa, haciaCalle, mostrador);
            PonerPersonajeCercaT417(raiz, datos[1], BuscarTransform("EntradaClinica"), 1.5f, 2.6f);
            PonerPersonajeCercaT417(raiz, datos[2], BuscarTransform("Lugar_Casino"), 1.5f, 2.6f);
            PonerPersonajeCercaT417(raiz, datos[3], CarroMasCercanoT417(centroCasa), 2.2f, 2.9f);
            PonerPersonajeCercaT417(raiz, datos[4], BuscarTransform("Lugar_Bar"), 1.5f, 2.6f);
            Debug.Log("[Progresion] CreadorCiudadPandillas.T417: CIC en " + puntoCic + ", 5 personajes de apoyo.");
        }

        private static void PonerPeatonesT417()
        {
            Object config = AssetDatabase.LoadAssetAtPath<Object>(RutaConfigPeatones);
            if (config == null)
            {
                Debug.LogWarning("[T-417] No existe " + RutaConfigPeatones);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("peatonesDeDia").intValue = PeatonesDeDiaT417;
            so.FindProperty("peatonesDeNoche").intValue = PeatonesDeNocheT417;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // La tienda del CIC: solo las 15 mejoras de armas (nivel 1 a 3), con los mismos precios que El Mercado.
        private static DatosTienda CrearTiendaCicT417()
        {
            DatosTienda tienda = AssetDatabase.LoadAssetAtPath<DatosTienda>(RutaTiendaCic);
            if (tienda == null)
            {
                tienda = ScriptableObject.CreateInstance<DatosTienda>();
                AssetDatabase.CreateAsset(tienda, RutaTiendaCic);
            }
            DatosTienda mercado = AssetDatabase.LoadAssetAtPath<DatosTienda>(RutaTiendaMercado);
            SerializedObject so = new SerializedObject(tienda);
            so.FindProperty("nombre").stringValue = "CIC";
            SerializedProperty lista = so.FindProperty("ofertas");
            lista.ClearArray();
            foreach (DatosOferta oferta in mercado.Ofertas)
            {
                if (oferta != null && oferta.name.StartsWith("DatosOferta_Arma_") && !oferta.name.EndsWith("_0"))
                {
                    lista.arraySize++;
                    lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = oferta;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tienda);
            AssetDatabase.SaveAssets();
            return tienda;
        }

        private static DatosPersonajeApoyo CrearDatosApoyoT417(DefinicionApoyo d)
        {
            System.IO.Directory.CreateDirectory(CarpetaApoyo);
            string ruta = CarpetaApoyo + "/DatosPersonajeApoyo_" + d.Id + ".asset";
            DatosPersonajeApoyo datos = AssetDatabase.LoadAssetAtPath<DatosPersonajeApoyo>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosPersonajeApoyo>();
                AssetDatabase.CreateAsset(datos, ruta);
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("id").stringValue = d.Id;
            so.FindProperty("titulo").stringValue = d.Titulo;
            so.FindProperty("nombre").stringValue = d.Nombre;
            so.FindProperty("funcion").stringValue = d.Funcion;
            so.FindProperty("color").colorValue = d.Color;
            so.FindProperty("servicio").enumValueIndex = (int)d.Servicio;
            so.FindProperty("costo").intValue = d.Costo;
            SerializedProperty frases = so.FindProperty("frases");
            frases.arraySize = d.Frases.Length;
            for (int i = 0; i < d.Frases.Length; i++)
            {
                frases.GetArrayElementAtIndex(i).stringValue = d.Frases[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            return datos;
        }

        // Un punto de banqueta a 10-25 m de la puerta de la casa, lejos de toda puerta y con espacio para la mesa y el inventor.
        private static Vector3 ElegirPuntoCicT417(Vector3 centro, out Vector3 haciaCalle)
        {
            List<Vector3> puertas = new List<Vector3>();
            foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("Puerta") || t.name.Contains("Entrada"))
                    {
                        puertas.Add(t.position);
                    }
                }
            }
            Vector3 mejor = centro;
            float mejorPuntaje = float.MaxValue;
            haciaCalle = Vector3.forward;
            for (float radio = DistanciaMinCic; radio <= DistanciaMaxCic; radio += 1.5f)
            {
                for (int a = 0; a < 24; a++)
                {
                    float angulo = a * 15f * Mathf.Deg2Rad;
                    Vector3 candidato = centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radio;
                    if (!Physics.Raycast(candidato + Vector3.up * 4f, Vector3.down, out RaycastHit piso, 8f) || piso.normal.y < 0.95f
                        || piso.point.y > AlturaMaxBanqueta + centro.y)
                    {
                        continue;
                    }
                    Vector3 p = piso.point;
                    Vector3 haciaCasa = centro - p;
                    haciaCasa.y = 0f;
                    haciaCasa.Normalize();
                    // Espacio libre: la mesa, el inventor y la repisa en una caja de 4 x 3 m alineada con la dirección a la casa.
                    Quaternion giro = Quaternion.LookRotation(haciaCasa, Vector3.up);
                    if (Physics.CheckBox(p + Vector3.up * 1.4f, new Vector3(2.2f, 1.1f, 2.0f), giro, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    float minPuerta = float.MaxValue;
                    foreach (Vector3 puerta in puertas)
                    {
                        minPuerta = Mathf.Min(minPuerta, Vector3.Distance(p, puerta));
                    }
                    if (minPuerta < DistanciaMinPuertaCic)
                    {
                        continue;
                    }
                    // Mientras más cerca de la casa, mejor (el jugador lo encuentra pronto).
                    if (radio < mejorPuntaje)
                    {
                        mejorPuntaje = radio;
                        mejor = p;
                        haciaCalle = haciaCasa;
                    }
                }
                if (mejorPuntaje < float.MaxValue)
                {
                    break;
                }
            }
            if (mejorPuntaje == float.MaxValue)
            {
                Debug.LogWarning("[T-417] No encontré banqueta libre para el CIC; queda junto a la casa.");
            }
            return mejor;
        }

        private static MostradorTienda ArmarCicT417(Transform lugar, DatosTienda tienda)
        {
            Material madera = MaterialT407("T417_Mesa", new Color(0.3f, 0.2f, 0.12f), Color.black);
            Material metal = MaterialT407("T417_Metal", new Color(0.2f, 0.21f, 0.25f), Color.black);
            Material cajaMat = MaterialT407("T417_Caja", new Color(0.5f, 0.4f, 0.25f), Color.black);
            Material neon = MaterialT407("T417_NeonCic", ColorCic * 0.4f, ColorCic * 3f);
            Material fondo = MaterialT407("T417_FondoCic", new Color(0.05f, 0.05f, 0.07f), Color.black);
            Vector3 p = lugar.position;
            Vector3 f = lugar.forward;
            Vector3 lado = lugar.right;
            Quaternion giro = lugar.rotation;

            CajaT407(lugar, "Mesa", p + Vector3.up * 0.45f, new Vector3(2.4f, 0.9f, 0.9f), madera, true, giro);
            CajaT407(lugar, "MesaTapa", p + Vector3.up * 0.93f, new Vector3(2.6f, 0.08f, 1.05f), metal, false, giro);
            Vector3 fondoRepisa = p - f * 2.4f;
            CajaT407(lugar, "Pared", fondoRepisa + Vector3.up * 1.6f, new Vector3(3.4f, 3.2f, 0.12f), metal, true, giro);
            for (int nivel = 0; nivel < 3; nivel++)
            {
                CajaT407(lugar, "Repisa_" + nivel, fondoRepisa + f * 0.2f + Vector3.up * (0.6f + nivel * 0.55f), new Vector3(3.0f, 0.06f, 0.4f), madera, false, giro);
                for (int c = -1; c <= 1; c++)
                {
                    float alto = 0.28f + 0.04f * ((nivel + c + 3) % 3);
                    CajaT407(lugar, "Caja_" + nivel + "_" + c, fondoRepisa + f * 0.2f + lado * (c * 0.9f) + Vector3.up * (0.63f + nivel * 0.55f + alto * 0.5f),
                        new Vector3(0.55f, alto, 0.32f), cajaMat, false, giro);
                }
            }

            // El letrero de neón "CIC" arriba de la pared, mirando a la calle.
            Vector3 centroLetrero = fondoRepisa + Vector3.up * 3.65f + f * 0.1f;
            CajaT407(lugar, "Letrero_Fondo", centroLetrero, new Vector3(1.8f, 0.7f, 0.06f), fondo, false, giro);
            Text t = TextoT407(lugar, "Letrero_CIC", "CIC", centroLetrero + f * 0.05f, Quaternion.LookRotation(-f, Vector3.up), 0.6f, ColorCic, 1.7f);
            t.fontSize = 55;
            CajaT407(lugar, "Letrero_NeonArriba", centroLetrero + Vector3.up * 0.33f + f * 0.05f, new Vector3(1.8f, 0.04f, 0.04f), neon, false, giro);
            CajaT407(lugar, "Letrero_NeonAbajo", centroLetrero + Vector3.down * 0.33f + f * 0.05f, new Vector3(1.8f, 0.04f, 0.04f), neon, false, giro);

            // Una luz fría sobre la mesa.
            GameObject luz = new GameObject("Luz_Fria");
            luz.transform.SetParent(lugar, false);
            luz.transform.position = p + Vector3.up * 2.6f + f * 0.2f;
            Light foco = luz.AddComponent<Light>();
            foco.type = LightType.Point;
            foco.color = new Color(0.6f, 0.85f, 1f);
            foco.intensity = 6f;
            foco.range = 9f;

            // La tienda: un MostradorTienda en la mesa con DatosTienda_CIC. Comparte la PantallaTienda de El Mercado (una sola en la escena).
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            CatalogoObjetos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(RutaCatalogoObjetosT417);
            if (pantalla == null || catalogo == null)
            {
                Debug.LogError("[T-417] Falta la PantallaTienda de la escena o el CatalogoObjetos_Base.");
            }
            GameObject objeto = new GameObject("MostradorCIC");
            objeto.transform.SetParent(lugar, false);
            objeto.transform.SetPositionAndRotation(p + Vector3.up * 0.3f, giro);
            MostradorTienda mostrador = objeto.AddComponent<MostradorTienda>();
            SerializedObject so = new SerializedObject(mostrador);
            so.FindProperty("datos").objectReferenceValue = tienda;
            so.FindProperty("catalogo").objectReferenceValue = catalogo;
            so.FindProperty("pantalla").objectReferenceValue = pantalla;
            so.ApplyModifiedPropertiesWithoutUndo();
            return mostrador;
        }

        private static GameObject PonerPersonajeT417(Transform raiz, DatosPersonajeApoyo datos, Vector3 posicion, Vector3 mirada, MostradorTienda mostrador)
        {
            GameObject go = new GameObject("Apoyo_" + datos.Nombre);
            go.transform.SetParent(raiz, false);
            go.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(mirada, Vector3.up));
            Material material = MaterialT407("T417_" + datos.Id, datos.Color * 0.8f, Color.black);
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(go.transform, false);
            cuerpo.transform.localPosition = Vector3.up * 0.9f;
            cuerpo.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            cuerpo.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(go.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.55f, 0.3f);
            visor.transform.localScale = new Vector3(0.45f, 0.12f, 0.12f);
            visor.GetComponent<MeshRenderer>().sharedMaterial = MaterialT407("T417_Visor", new Color(0.04f, 0.04f, 0.06f), Color.black);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            PersonajeApoyo apoyo = go.AddComponent<PersonajeApoyo>();
            SerializedObject so = new SerializedObject(apoyo);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("mostrador").objectReferenceValue = mostrador;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // Junto al ancla: el primer punto de piso libre entre dos radios, mirando hacia afuera del ancla (hacia la calle).
        private static void PonerPersonajeCercaT417(Transform raiz, DatosPersonajeApoyo datos, Transform ancla, float radioMin, float radioMax)
        {
            if (ancla == null)
            {
                Debug.LogWarning("[T-417] Falta el ancla de " + datos.Nombre);
                return;
            }
            Vector3 punto = ancla.position;
            Vector3 afuera = ancla.forward;
            bool hallado = false;
            for (float radio = radioMin; radio <= radioMax && !hallado; radio += 0.3f)
            {
                for (int a = 0; a < 16 && !hallado; a++)
                {
                    float angulo = a * 22.5f * Mathf.Deg2Rad;
                    Vector3 direccion = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                    Vector3 candidato = ancla.position + direccion * radio;
                    if (!Physics.Raycast(candidato + Vector3.up * 2.5f, Vector3.down, out RaycastHit piso, 6f) || piso.normal.y < 0.95f)
                    {
                        continue;
                    }
                    if (Physics.CheckCapsule(piso.point + Vector3.up * 0.5f, piso.point + Vector3.up * 1.4f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    punto = piso.point;
                    afuera = direccion;
                    hallado = true;
                }
            }
            if (!hallado)
            {
                Debug.LogWarning("[T-417] Sin piso libre cerca de " + ancla.name + " para " + datos.Nombre);
            }
            PonerPersonajeT417(raiz, datos, punto, afuera, null);
        }

        private static Transform CarroMasCercanoT417(Vector3 centro)
        {
            System.Type tipo = System.Type.GetType(TipoCarroEstacionado);
            Transform mejor = null;
            float distancia = float.MaxValue;
            if (tipo != null)
            {
                foreach (Component c in Object.FindObjectsByType(tipo, FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    float d = (c.transform.position - centro).sqrMagnitude;
                    if (d < distancia)
                    {
                        distancia = d;
                        mejor = c.transform;
                    }
                }
            }
            return mejor;
        }
    }
}
