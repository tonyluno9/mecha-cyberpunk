using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Genera Pruebas_Misiones.unity (T-184): un piso de 90 x 60 m con calles de cajas, un <see cref="PuntoMisionDePrueba"/> por cada id de
    /// las misiones (más los dos escondites), el taller, tres lugares, el mercado negro y todas las herramientas de prueba. Idempotente:
    /// si lo que se arma es igual a lo que ya hay, no se guarda de nuevo. Las tareas siguientes (T-185 a T-187) agregan su parte DENTRO de
    /// <see cref="Crear"/>. Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaMisiones.Crear
    /// </summary>
    public static class CreadorEscenaMisiones
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity";
        public const string IdInicio = "Misiones_Inicio";

        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaPeleasDeMision = "Assets/_Proyecto/Pelea/Prefabs/PeleasDeMision.prefab";
        private const string RutaMostradorTienda = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";
        private const string RutaContactoBar = "Assets/_Proyecto/UI/Prefabs/ContactoBar.prefab";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales/";
        private const string RutaCaptura = "docs/capturas/T-184-1.png";
        private const float HoraAnochecer = 20f;

        // Medidas en metros (1 unidad = 1 m). El piso va de x -45 a 45 y de z -30 a 30.
        private const float AnchoPiso = 90f;
        private const float LargoPiso = 60f;
        private const float AlturaMuro = 4f;
        private const float GrosorMuro = 0.4f;
        private const float AnchoCalle = 8f;
        private const float AlturaLetrero = 3f;
        private const float TamanoLetra = 0.5f;
        private const float SeparacionLetreroMuro = 0.02f;
        private const float TamanoLetraPiso = 0.9f;
        private const float RadioCilindro = 0.75f;
        private const float AlturaCilindro = 2f;
        private const float AlturaEdificio = 7f;
        private const float LadoEdificio = 4f;
        private const float DesvioEdificio = 4.5f;
        private const float SeparacionCallejon = 3.5f;
        private const float LargoCallejon = 5f;
        private const float InicioCallejon = 2f;

        /// <summary>Un punto de misión de la escena: su id y su sitio en el piso (x, z en metros).</summary>
        public readonly struct SitioPunto
        {
            public readonly string Id;
            public readonly float X;
            public readonly float Z;

            public SitioPunto(string id, float x, float z)
            {
                Id = id;
                X = x;
                Z = z;
            }
        }

        // Cuadrícula de 15 m: columnas x = -37.5 ... 37.5 y filas z = -7.5, 7.5, 22.5 (la fila -22.5 es del taller y los lugares).
        // Las calles en cruz (x = 0 y z = 0) quedan libres entre las columnas y las filas de los puntos.
        public static readonly SitioPunto[] Puntos =
        {
            new SitioPunto("Mision_Encargo_DonaLupe", -37.5f, 22.5f),
            new SitioPunto("Mision_Botin_Farmacia", -22.5f, 22.5f),
            new SitioPunto("Mision_Encargo_Taquero", -7.5f, 22.5f),
            new SitioPunto("Mision_Pelea_Callejon", 7.5f, 22.5f),
            new SitioPunto("Mision_Encargo_Vecindad", 22.5f, 22.5f),
            new SitioPunto("Mision_Botin_Subestacion", 37.5f, 22.5f),
            new SitioPunto("Mision_Botin_Paqueteria", -37.5f, 7.5f),
            new SitioPunto("Mision_Destino_TorreCorp", -22.5f, 7.5f),
            new SitioPunto("Mision_Destino_Moroso", -7.5f, 7.5f),
            new SitioPunto("Mision_Pelea_Baldio", 7.5f, 7.5f),
            new SitioPunto("Mision_Destino_Muelle", 22.5f, 7.5f),
            new SitioPunto("Mision_Botin_BodegaCorp", 37.5f, 7.5f),
            new SitioPunto("Mision_Botin_Camion", -37.5f, -7.5f),
            new SitioPunto("Mision_Pelea_Camion", -22.5f, -7.5f),
            new SitioPunto("Mision_Escondite_Azotea", -7.5f, -7.5f),
            new SitioPunto("Mision_Escondite_Lavanderia", 7.5f, -7.5f),
        };

        public static readonly Vector3 PosicionInicio = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 PosicionTaller = new Vector3(-37.5f, 0f, -22.5f);
        // La casa queda a 25 m o más de cualquier botín (T-185): el robo no se cumple por pasar cerca de ella.
        public static readonly Vector3 PosicionCasa = new Vector3(-7.5f, 0f, -22.5f);
        public static readonly Vector3 PosicionBar = new Vector3(-22.5f, 0f, -22.5f);
        // El contacto del bar (T-186) está dentro del bar, a la derecha de la entrada.
        public static readonly Vector3 PosicionContacto = new Vector3(-20.5f, 0f, -21.5f);
        public static readonly Vector3 PosicionMercado = new Vector3(7.5f, 0f, -22.5f);
        public static readonly Vector3 PosicionLlamadoCalle = new Vector3(0f, 0f, 14f);
        public static readonly Vector3 PosicionMuroReputacion = new Vector3(44f, 0f, 0f);

        private static Material matPiso;
        private static Material matAsfalto;
        private static Material matMuro;
        private static Material matCallejon;
        private static Material matTaller;
        private static Material matCasa;
        private static Material matBar;
        private static Material matMercado;
        private static Material matBanco;

        [MenuItem("Mecha Cyberpunk/Crear escena Pruebas_Misiones")]
        public static void Crear()
        {
            CargarMateriales();

            string firmaActual = null;
            if (File.Exists(RutaEscena))
            {
                firmaActual = CreadorEscenaPrologo.Firma(EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single));
            }

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ArmarMundo();
            ArmarPuntos();
            ArmarTaller();
            ArmarLugares();
            ArmarMuroReputacion();
            ArmarPeleas();
            ArmarJugadorYServicios();

            string firmaNueva = CreadorEscenaPrologo.Firma(escena);
            if (firmaActual != null && firmaActual == firmaNueva)
            {
                Debug.Log("[Progresion] Pruebas_Misiones ya estaba completa; no se guarda de nuevo.");
                return;
            }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] Pruebas_Misiones creada.");
        }

        /// <summary>
        /// Captura aérea (T-184-1) en memoria con Camera.Render, sin guardar la escena. Pinta los cilindros con el color de su tipo.
        /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaMisiones.CapturarVistaAerea
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Capturar vista aérea de Pruebas_Misiones")]
        public static void CapturarVistaAerea()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var materiales = new List<Material>();
            foreach (PuntoMisionDePrueba punto in Object.FindObjectsByType<PuntoMisionDePrueba>(FindObjectsSortMode.None))
            {
                SerializedObject so = new SerializedObject(punto);
                var cilindro = so.FindProperty("cilindro").objectReferenceValue as Renderer;
                if (cilindro == null) continue;
                Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", PuntoMisionDePrueba.ColorDeTipo(punto.Tipo));
                cilindro.sharedMaterial = material;
                materiales.Add(material);
            }

            GameObject luzCaptura = new GameObject("LuzCaptura");
            Light luz = luzCaptura.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.2f;
            luzCaptura.transform.rotation = Quaternion.Euler(70f, 20f, 0f);

            GameObject objetoCamara = new GameObject("CamaraCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.orthographic = true;
            camara.orthographicSize = 31f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 100f;
            objetoCamara.transform.SetPositionAndRotation(new Vector3(0f, 40f, 0f), Quaternion.Euler(90f, 0f, 0f));

            const int ancho = 1800;
            const int alto = 1200;
            RenderTexture destino = new RenderTexture(ancho, alto, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            textura.Apply();
            RenderTexture.active = anterior;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            camara.targetTexture = null;
            destino.Release();
            Object.DestroyImmediate(destino);
            Object.DestroyImmediate(objetoCamara);
            Object.DestroyImmediate(luzCaptura);
            foreach (Material material in materiales) Object.DestroyImmediate(material);
            Debug.Log("[Progresion] Captura guardada en " + RutaCaptura);
        }

        // ---------- Secciones ----------

        private static void ArmarMundo()
        {
            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            componenteLuz.color = new Color(0.55f, 0.6f, 0.9f);
            componenteLuz.intensity = 0.5f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CreadorEscenaPrologo.Caja("Piso", new Vector3(0f, -0.5f, 0f), new Vector3(AnchoPiso, 1f, LargoPiso), matPiso, true);
            float mitadX = AnchoPiso / 2f;
            float mitadZ = LargoPiso / 2f;
            CreadorEscenaPrologo.Caja("Muro sur", new Vector3(0f, AlturaMuro / 2f, -mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro norte", new Vector3(0f, AlturaMuro / 2f, mitadZ), new Vector3(AnchoPiso, AlturaMuro, GrosorMuro), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro oeste", new Vector3(-mitadX, AlturaMuro / 2f, 0f), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);
            CreadorEscenaPrologo.Caja("Muro este", new Vector3(mitadX, AlturaMuro / 2f, 0f), new Vector3(GrosorMuro, AlturaMuro, LargoPiso), matMuro, true);

            // La calle en cruz: dos franjas de asfalto sin collider.
            CreadorEscenaPrologo.Caja("Calle este-oeste", new Vector3(0f, 0.01f, 0f), new Vector3(AnchoPiso, 0.02f, AnchoCalle), matAsfalto, false);
            CreadorEscenaPrologo.Caja("Calle norte-sur", new Vector3(0f, 0.02f, 0f), new Vector3(AnchoCalle, 0.02f, LargoPiso), matAsfalto, false);
        }

        private static void ArmarPuntos()
        {
            GameObject padre = new GameObject("Puntos de misión");
            foreach (SitioPunto sitio in Puntos)
            {
                TipoPuntoMision tipo = ReglaMisiones.TipoDePunto(sitio.Id).Value;
                Vector3 posicion = new Vector3(sitio.X, 0f, sitio.Z);
                string nombre = sitio.Id.Substring("Mision_".Length);

                GameObject objeto = new GameObject("Punto " + sitio.Id);
                objeto.transform.SetParent(padre.transform, false);
                objeto.transform.position = posicion;

                GameObject cilindro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cilindro.name = "Cilindro";
                Object.DestroyImmediate(cilindro.GetComponent<Collider>());
                cilindro.transform.SetParent(objeto.transform, false);
                cilindro.transform.localPosition = new Vector3(0f, AlturaCilindro / 2f, 0f);
                cilindro.transform.localScale = new Vector3(RadioCilindro * 2f, AlturaCilindro / 2f, RadioCilindro * 2f);

                PuntoMisionDePrueba punto = objeto.AddComponent<PuntoMisionDePrueba>();
                SerializedObject so = new SerializedObject(punto);
                so.FindProperty("id").stringValue = sitio.Id;
                so.FindProperty("tipo").enumValueIndex = (int)tipo;
                so.FindProperty("cilindro").objectReferenceValue = cilindro.GetComponent<MeshRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();

                Letrero("Letrero " + nombre, nombre.Replace('_', '\n'), posicion + new Vector3(0f, 0f, -2.5f));

                bool callejon = sitio.Id == "Mision_Pelea_Callejon" || sitio.Id == "Mision_Pelea_Baldio";
                if (callejon)
                {
                    // Dos muros a los lados forman el callejón.
                    float centroZ = sitio.Z + InicioCallejon + LargoCallejon / 2f;
                    CreadorEscenaPrologo.Caja("Callejón oeste " + nombre, new Vector3(sitio.X - SeparacionCallejon, AlturaEdificio / 2f, centroZ),
                        new Vector3(GrosorMuro, AlturaEdificio, LargoCallejon), matCallejon, true);
                    CreadorEscenaPrologo.Caja("Callejón este " + nombre, new Vector3(sitio.X + SeparacionCallejon, AlturaEdificio / 2f, centroZ),
                        new Vector3(GrosorMuro, AlturaEdificio, LargoCallejon), matCallejon, true);
                }
                else
                {
                    // Un edificio en diagonal, fuera de las líneas que unen los puntos.
                    CreadorEscenaPrologo.Caja("Edificio " + nombre, new Vector3(sitio.X + DesvioEdificio, AlturaEdificio / 2f, sitio.Z + DesvioEdificio),
                        new Vector3(LadoEdificio, AlturaEdificio, LadoEdificio), matMuro, true);
                }
            }
        }

        private static void ArmarTaller()
        {
            // El estacionamiento mira al este (+X); el banco queda a 3 m del mecha (desplazamiento del doble del taller).
            Quaternion rotacion = Quaternion.Euler(0f, 90f, 0f);
            GameObject taller = new GameObject("Taller");
            taller.transform.SetPositionAndRotation(PosicionTaller, rotacion);
            TallerDePrueba contrato = taller.AddComponent<TallerDePrueba>();

            Vector3 banco = contrato.PuntoInteraccion;
            CreadorEscenaPrologo.Hija(taller, CreadorEscenaPrologo.Caja("Taller banco", new Vector3(banco.x, 0.5f, banco.z), new Vector3(1.5f, 1f, 0.8f), matBanco, false));
            CreadorEscenaPrologo.Caja("Taller suelo", new Vector3(PosicionTaller.x, 0.03f, PosicionTaller.z), new Vector3(8f, 0.02f, 9f), matTaller, false);
            Letrero("Letrero TALLER", "TALLER", PosicionTaller + new Vector3(0f, 0f, 5.5f));

            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha));
            mecha.transform.SetPositionAndRotation(PosicionTaller, rotacion);

            // El punto de llamado: el mecha viene a la calle frente al taller.
            GameObject llamado = new GameObject("PuntoLlamado Taller");
            llamado.transform.SetPositionAndRotation(PosicionTaller + new Vector3(8f, 0f, 8f), Quaternion.Euler(0f, 90f, 0f));
            llamado.AddComponent<PuntoLlamadoDePrueba>().Configurar(40f, true);

            // Otro punto de llamado en la calle norte-sur, cerca de los callejones (T-185): el mecha llega mirando al norte.
            GameObject llamadoCalle = new GameObject("PuntoLlamado Calle");
            llamadoCalle.transform.SetPositionAndRotation(PosicionLlamadoCalle, Quaternion.identity);
            llamadoCalle.AddComponent<PuntoLlamadoDePrueba>().Configurar(40f, true);
        }

        private static void ArmarLugares()
        {
            // Casa, Bar y Mercado negro: lugares de prueba siempre abiertos (el horario por defecto no cierra nunca).
            CreadorEscenaPrologo.ArmarLugar("Lugar Casa", IdLugar.Casa, "Casa", PosicionCasa, new Vector3(8f, 3f, 6f), PosicionCasa + new Vector3(0f, 0f, 3f));
            CreadorEscenaPrologo.Caja("Casa suelo", new Vector3(PosicionCasa.x, 0.03f, PosicionCasa.z), new Vector3(7f, 0.02f, 5f), matCasa, false);
            Letrero("Letrero CASA", "CASA", PosicionCasa + new Vector3(0f, 0f, 3.5f));

            CreadorEscenaPrologo.ArmarLugar("Lugar Bar", IdLugar.Bar, "Bar", PosicionBar, new Vector3(8f, 3f, 6f), PosicionBar + new Vector3(0f, 0f, 3f));
            CreadorEscenaPrologo.Caja("Bar suelo", new Vector3(PosicionBar.x, 0.03f, PosicionBar.z), new Vector3(7f, 0.02f, 5f), matBar, false);
            Letrero("Letrero BAR", "BAR", PosicionBar + new Vector3(0f, 0f, 3.5f));
            GameObject contacto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaContactoBar));
            contacto.transform.position = PosicionContacto;

            CreadorEscenaPrologo.Vacio("Sitio_Mercado", PosicionMercado);
            CreadorEscenaPrologo.ArmarLugar("Lugar MercadoNegro", IdLugar.MercadoNegro, "Mercado negro", PosicionMercado, new Vector3(8f, 3f, 6f), PosicionMercado + new Vector3(0f, 0f, 3f));
            GameObject tienda = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostradorTienda));
            tienda.transform.position = PosicionMercado;
            CreadorEscenaPrologo.Caja("Mercado suelo", new Vector3(PosicionMercado.x, 0.03f, PosicionMercado.z), new Vector3(7f, 0.02f, 5f), matMercado, false);
            Letrero("Letrero MERCADO", "MERCADO", PosicionMercado + new Vector3(0f, 0f, 3.5f));
        }

        private static void ArmarMuroReputacion()
        {
            // Un muro al final de la calle este-oeste, con el letrero que reacciona a la reputación (T-182).
            GameObject muro = CreadorEscenaPrologo.Caja("Muro reputación", new Vector3(PosicionMuroReputacion.x, 2.5f, 0f), new Vector3(GrosorMuro, 5f, 7f), matMuro, true);
            // El texto cuelga de un padre sin escala (el muro está escalado 0.4 × 5 × 7 y deformaría el letrero), pegado a la cara oeste
            // del muro (la que mira a la calle) y con el texto de cara al oeste.
            GameObject soporte = new GameObject("Letrero reputación");
            soporte.transform.SetPositionAndRotation(new Vector3(PosicionMuroReputacion.x - GrosorMuro / 2f - SeparacionLetreroMuro, 3f, 0f), Quaternion.Euler(0f, 90f, 0f));
            GameObject texto = new GameObject("Texto reputación");
            texto.transform.SetParent(soporte.transform, false);
            TextMesh mesh = texto.AddComponent<TextMesh>();
            ConfigurarTexto(mesh, "", TamanoLetra);

            ReaccionReputacionDePrueba reaccion = muro.AddComponent<ReaccionReputacionDePrueba>();
            SerializedObject so = new SerializedObject(reaccion);
            so.FindProperty("letrero").objectReferenceValue = mesh;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ArmarPeleas()
        {
            // Las peleas de misión (T-185): levantan una emboscada en los puntos Pelea cuando el paso en curso es Pelear.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPeleasDeMision);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("Falta PeleasDeMision.prefab; corre antes CreadorPeleasMision.Crear.");
            }
            PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void ArmarJugadorYServicios()
        {
            // 21:00 de noche (la noche empieza a las 20:00); la tecla J salta a las 8:00.
            RelojDePrueba reloj = new GameObject("RelojDePrueba").AddComponent<RelojDePrueba>();
            SerializedObject soReloj = new SerializedObject(reloj);
            soReloj.FindProperty("horaAnochecer").floatValue = HoraAnochecer;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("GuardadoDePrueba").AddComponent<GuardadoDePrueba>();
            new GameObject("DineroDePrueba").AddComponent<MechaCyberpunk.Economia.Pruebas.DineroDePrueba>();
            new GameObject("HistoriaLibreDePrueba").AddComponent<HistoriaLibreDePrueba>();

            GameObject inventario = new GameObject("InventarioDePrueba");
            MechaCyberpunk.Economia.Pruebas.InventarioDePrueba prueba = inventario.AddComponent<MechaCyberpunk.Economia.Pruebas.InventarioDePrueba>();
            SerializedObject soInventario = new SerializedObject(prueba);
            soInventario.FindProperty("brazo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            soInventario.FindProperty("kit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            soInventario.ApplyModifiedPropertiesWithoutUndo();

            CreadorEscenaPrologo.Aparicion(IdInicio, ModoJugador.APie, PosicionInicio, Quaternion.identity);
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.SetPositionAndRotation(PosicionInicio + new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = IdInicio;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Ayudas ----------

        // Letrero: un tablero de pie (con el texto de cara al sur) y el mismo texto tendido en el piso para verlo desde el aire.
        private static void Letrero(string nombre, string texto, Vector3 posicion)
        {
            GameObject raiz = new GameObject(nombre);
            raiz.transform.position = posicion;
            Texto(raiz, texto, posicion + new Vector3(0f, AlturaLetrero, 0f), Quaternion.identity, TamanoLetra);
            Texto(raiz, texto, posicion + new Vector3(0f, 0.05f, -1.2f), Quaternion.Euler(90f, 0f, 0f), TamanoLetraPiso);
        }

        private static void Texto(GameObject padre, string texto, Vector3 posicion, Quaternion rotacion, float tamano)
        {
            GameObject objeto = new GameObject("Texto " + texto.Replace('\n', ' '));
            objeto.transform.SetParent(padre.transform, false);
            objeto.transform.SetPositionAndRotation(posicion, rotacion);
            ConfigurarTexto(objeto.AddComponent<TextMesh>(), texto, tamano);
        }

        private static void ConfigurarTexto(TextMesh mesh, string texto, float tamano)
        {
            mesh.text = texto;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 100;
            mesh.characterSize = tamano / 10f;
            mesh.color = Color.white;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = fuente;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        private static void CargarMateriales()
        {
            matPiso = CargarMaterial("Prologo_Piso");
            matAsfalto = CargarMaterial("Prologo_Asfalto");
            matMuro = CargarMaterial("Prologo_Edificio");
            matCallejon = CargarMaterial("Prologo_Clandestino");
            matTaller = CargarMaterial("Prologo_Taller");
            matCasa = CargarMaterial("Prologo_Casa");
            matBar = CargarMaterial("Prologo_Mostrador");
            matMercado = CargarMaterial("Prologo_Cama");
            matBanco = CargarMaterial("Prologo_Banco");
        }

        private static Material CargarMaterial(string nombre)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + nombre + ".mat");
            if (material == null)
            {
                throw new System.InvalidOperationException("Falta el material " + nombre + "; corre antes CreadorEscenaPrologo.Crear.");
            }
            return material;
        }
    }
}
