using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Genera Pruebas_Calle.unity (T-064): la calle greybox de la fase T-F4 donde se arma la emboscada. Es la única forma de
    /// cambiar esa escena: cada tarea de T-F4 agrega su parte aquí y la regenera. Nunca se edita a mano.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorEscenaCalle.Crear
    /// </summary>
    public static class CreadorEscenaCalle
    {
        public const string IdAparicion = "Calle_Inicio";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string RutaMaterialPiso = "Assets/_Proyecto/Mecha/Materiales/Calle_Piso.mat";
        private const string RutaMaterialAsfalto = "Assets/_Proyecto/Mecha/Materiales/Calle_Asfalto.mat";
        private const string RutaMaterialEdificio = "Assets/_Proyecto/Mecha/Materiales/Calle_Edificio.mat";

        // Medidas en metros. La calle corre a lo largo de +Z; el jugador empieza en el origen mirando a +Z.
        private const float AnchoPiso = 60f;
        private const float LargoPiso = 120f;
        private const float ZInicioPiso = -10f;
        private const float AnchoCalle = 14f;
        private const float AlturaEdificio = 15f;
        private const float GrosorMuro = 1f;
        private const float ZInicioEdificios = -8f;
        private const float ZFinEdificios = 108f;
        private const float ZCallejonInicio = 82f;
        private const float ZCallejonFin = 92f;
        private const float XBorde = 30f;
        private const float ZMecha = 8f;
        private const float AlturaInicio = 0.05f;

        // Taller (T-066): un cobertizo detrás del inicio, abierto hacia +Z. Su estacionamiento es el centro.
        private const float ZTaller = -5f;
        private const float LadoTaller = 8f;
        private const float AlturaTecho = 6f;
        private const float GrosorTecho = 0.3f;
        private const float GrosorPared = 0.4f;
        // Zona de llamado: la llegada y su radio. Más allá de la zona (más de 75 m del inicio) el mecha no llega.
        private const float ZLlegada = 20f;
        private const float RadioLlamado = 30f;

        // Emboscada (T-071): en la boca del callejón (a la izquierda de la calle), mirando hacia adentro (-X).
        private const string RutaEmboscada = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const float ZEmboscada = 87f;
        private const float XEntradaCallejon = -7f;
        private const float GiroEmboscada = -90f;
        // Punto de llamado junto a la entrada del callejón: zona pequeña que cubre la boca, sin alcanzar el centro de la calle.
        private const float XLlegadaCallejon = -7.5f;
        private const float RadioLlamadoCallejon = 3.5f;

        [MenuItem("Mecha Cyberpunk/Crear escena Pruebas_Calle")]
        public static void Crear()
        {
            Material piso = CrearMaterial(RutaMaterialPiso, new Color(0.2f, 0.21f, 0.23f));
            Material asfalto = CrearMaterial(RutaMaterialAsfalto, new Color(0.08f, 0.08f, 0.1f));
            Material edificio = CrearMaterial(RutaMaterialEdificio, new Color(0.3f, 0.26f, 0.38f));

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject luz = new GameObject("Luz direccional");
            Light componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            float centroZ = ZInicioPiso + LargoPiso / 2f;
            Caja("Piso", new Vector3(0f, -0.5f, centroZ), new Vector3(AnchoPiso, 1f, LargoPiso), piso, true);
            Caja("Asfalto", new Vector3(0f, 0.01f, centroZ), new Vector3(AnchoCalle, 0.02f, LargoPiso), asfalto, false);

            float bordeCalle = AnchoCalle / 2f;
            float anchoEdificio = XBorde - 1f - bordeCalle;
            float centroXEdificio = bordeCalle + anchoEdificio / 2f;
            float centroAltura = AlturaEdificio / 2f;

            // Lado derecho: un solo bloque. Lado izquierdo: dos bloques con el callejón en medio.
            Edificio("Edificio derecho", centroXEdificio, ZInicioEdificios, ZFinEdificios, anchoEdificio, edificio);
            Edificio("Edificio izquierdo 1", -centroXEdificio, ZInicioEdificios, ZCallejonInicio, anchoEdificio, edificio);
            Edificio("Edificio izquierdo 2", -centroXEdificio, ZCallejonFin, ZFinEdificios, anchoEdificio, edificio);

            // Muros de cierre: atrás, al fondo de la calle y al fondo del callejón.
            Caja("Muro trasero", new Vector3(0f, centroAltura, ZInicioEdificios - GrosorMuro / 2f),
                new Vector3(AnchoPiso, AlturaEdificio, GrosorMuro), edificio, true);
            Caja("Muro del fondo", new Vector3(0f, centroAltura, ZFinEdificios + GrosorMuro / 2f),
                new Vector3(AnchoPiso, AlturaEdificio, GrosorMuro), edificio, true);
            Caja("Muro del callejón", new Vector3(-XBorde + GrosorMuro / 2f, centroAltura, (ZCallejonInicio + ZCallejonFin) / 2f),
                new Vector3(GrosorMuro, AlturaEdificio, ZCallejonFin - ZCallejonInicio), edificio, true);

            GameObject punto = new GameObject("PuntoAparicion Calle_Inicio");
            punto.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SerializedObject soPunto = new SerializedObject(punto.AddComponent<PuntoAparicionDePrueba>());
            soPunto.FindProperty("id").stringValue = IdAparicion;
            soPunto.FindProperty("modo").enumValueIndex = (int)ModoJugador.APie;
            soPunto.ApplyModifiedPropertiesWithoutUndo();

            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            jugador.transform.position = new Vector3(0f, AlturaInicio, 0f);
            SerializedObject soAparicion = new SerializedObject(jugador.GetComponent<AparicionJugador>());
            soAparicion.FindProperty("idAparicion").stringValue = IdAparicion;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();

            // El mecha mira hacia el jugador (-Z): su punto de subida queda en el camino.
            GameObject mecha = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecha));
            mecha.transform.SetPositionAndRotation(new Vector3(0f, AlturaInicio, ZMecha), Quaternion.Euler(0f, 180f, 0f));

            ArmarLlamado(edificio);

            // Tecla de prueba del calor (T-067): 9 suma 25, 0 borra.
            new GameObject("CalorDePrueba").AddComponent<CalorDePrueba>();

            GameObject hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
            hud.name = "HudAPie";

            // Teclas de prueba del dinero (T-102): 7 abona 500, 8 carga 500 de deuda. Va al final: LlamadoMechaPruebas usa
            // FindFirstObjectByType y el orden de los objetos de la escena cambia cuál punto de llamado encuentra.
            new GameObject("DineroDePrueba").AddComponent<MechaCyberpunk.Economia.Pruebas.DineroDePrueba>();

            // Reloj de prueba (T-107): la renta cobra al empezar cada día. J salta a las 08:00 del día siguiente.
            new GameObject("RelojDePrueba").AddComponent<MechaCyberpunk.Progresion.Pruebas.RelojDePrueba>();

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] Pruebas_Calle creada.");
        }

        // Taller con cobertizo greybox y un punto de llamado en la calle (dobles de lo de Alan).
        private static void ArmarLlamado(Material material)
        {
            GameObject taller = new GameObject("Taller");
            taller.transform.SetPositionAndRotation(new Vector3(0f, 0f, ZTaller), Quaternion.identity);
            taller.AddComponent<TallerDePrueba>();
            float mitad = LadoTaller / 2f;
            CajaHija(taller.transform, "Techo", new Vector3(0f, AlturaTecho + GrosorTecho / 2f, 0f),
                new Vector3(LadoTaller + GrosorPared * 2f, GrosorTecho, LadoTaller), material);
            CajaHija(taller.transform, "Pared izquierda", new Vector3(-mitad - GrosorPared / 2f, AlturaTecho / 2f, 0f),
                new Vector3(GrosorPared, AlturaTecho, LadoTaller), material);
            CajaHija(taller.transform, "Pared derecha", new Vector3(mitad + GrosorPared / 2f, AlturaTecho / 2f, 0f),
                new Vector3(GrosorPared, AlturaTecho, LadoTaller), material);

            GameObject punto = new GameObject("PuntoLlamado Calle");
            punto.transform.SetPositionAndRotation(new Vector3(0f, 0f, ZLlegada), Quaternion.Euler(0f, 180f, 0f));
            punto.AddComponent<PuntoLlamadoDePrueba>().Configurar(RadioLlamado, true);

            GameObject puntoCallejon = new GameObject("PuntoLlamado Callejón");
            puntoCallejon.transform.SetPositionAndRotation(new Vector3(XLlegadaCallejon, 0f, ZEmboscada), Quaternion.Euler(0f, GiroEmboscada, 0f));
            puntoCallejon.AddComponent<PuntoLlamadoDePrueba>().Configurar(RadioLlamadoCallejon, true);

            GameObject emboscada = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaEmboscada));
            emboscada.transform.SetPositionAndRotation(new Vector3(XEntradaCallejon, 0f, ZEmboscada), Quaternion.Euler(0f, GiroEmboscada, 0f));
        }

        private static void CajaHija(Transform padre, string nombre, Vector3 centroLocal, Vector3 tamano, Material material)
        {
            GameObject caja = Caja(nombre, padre.TransformPoint(centroLocal), tamano, material, true);
            caja.transform.SetParent(padre, true);
        }

        private static void Edificio(string nombre, float centroX, float zInicio, float zFin, float ancho, Material material)
        {
            Caja(nombre, new Vector3(centroX, AlturaEdificio / 2f, (zInicio + zFin) / 2f),
                new Vector3(ancho, AlturaEdificio, zFin - zInicio), material, true);
        }

        private static GameObject Caja(string nombre, Vector3 centro, Vector3 tamano, Material material, bool conCollider)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(caja.GetComponent<Collider>());
            }
            return caja;
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }
    }
}
