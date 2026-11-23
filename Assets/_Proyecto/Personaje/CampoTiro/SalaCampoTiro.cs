using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): construye por código la sala del campo de tiro: cuadrada, blanca y limpia (piso, paredes y techo en material sin luz, para que se vea
    /// claro de noche), con líneas de distancia turquesa, dos luces puntuales, una fila de costales como barrera y paredes que no dejan salir.
    /// Las medidas salen de <see cref="ReglaCampoTiro"/>.
    /// </summary>
    public static class SalaCampoTiro
    {
        private const float Grosor = 1f;
        private const float AlturaBarrera = 0.9f;
        private const float GrosorBarrera = 0.8f;
        private const float AlturaLinea = 0.02f;
        private const float AnchoLinea = 0.15f;
        private const float AlturaLuces = 10f;
        private const float AlcanceLuces = 50f;
        private const float IntensidadLuces = 2.5f;
        private const float AnchoCostalPorOmision = 3f;

        /// <summary>Crea la sala con su centro en <paramref name="centro"/> (mundo). Los datos pueden ser null: entonces usa materiales por defecto.</summary>
        public static GameObject Construir(DatosCampoTiro datos, Vector3 centro)
        {
            GameObject raiz = new GameObject("CampoTiro_Sala");
            raiz.transform.position = centro;
            Material blanco = datos != null && datos.MaterialBlanco != null ? datos.MaterialBlanco : MaterialPorOmision(Color.white);
            Material turquesa = datos != null && datos.MaterialTurquesa != null ? datos.MaterialTurquesa : MaterialPorOmision(new Color(0.361f, 0.890f, 0.839f, 1f));

            float ancho = ReglaCampoTiro.MitadAncho * 2f;
            float fondo = ReglaCampoTiro.FondoAdelante - ReglaCampoTiro.FondoAtras;
            float zMedio = (ReglaCampoTiro.FondoAdelante + ReglaCampoTiro.FondoAtras) * 0.5f;
            float alto = ReglaCampoTiro.AlturaSala;
            // Losas por fuera de la sala: la cara que mira hacia adentro es la que se ve.
            Losa(raiz.transform, "Piso", new Vector3(0f, -Grosor * 0.5f, zMedio), new Vector3(ancho + Grosor * 2f, Grosor, fondo + Grosor * 2f), blanco, true);
            Losa(raiz.transform, "Techo", new Vector3(0f, alto + Grosor * 0.5f, zMedio), new Vector3(ancho + Grosor * 2f, Grosor, fondo + Grosor * 2f), blanco, true);
            Losa(raiz.transform, "ParedIzquierda", new Vector3(-ReglaCampoTiro.MitadAncho - Grosor * 0.5f, alto * 0.5f, zMedio), new Vector3(Grosor, alto, fondo), blanco, true);
            Losa(raiz.transform, "ParedDerecha", new Vector3(ReglaCampoTiro.MitadAncho + Grosor * 0.5f, alto * 0.5f, zMedio), new Vector3(Grosor, alto, fondo), blanco, true);
            Losa(raiz.transform, "ParedAtras", new Vector3(0f, alto * 0.5f, ReglaCampoTiro.FondoAtras - Grosor * 0.5f), new Vector3(ancho, alto, Grosor), blanco, true);
            Losa(raiz.transform, "ParedFrente", new Vector3(0f, alto * 0.5f, ReglaCampoTiro.FondoAdelante + Grosor * 0.5f), new Vector3(ancho, alto, Grosor), blanco, true);

            // Líneas de la rejilla turquesa: la barrera, el punto de salida y cada distancia de los objetivos.
            Linea(raiz.transform, "LineaSalida", ReglaCampoTiro.ZInicioJugador, ancho, turquesa);
            Linea(raiz.transform, "LineaBarrera", ReglaCampoTiro.ZBarrera, ancho, turquesa);
            foreach (DistanciaTiro distancia in (DistanciaTiro[])System.Enum.GetValues(typeof(DistanciaTiro)))
            {
                Linea(raiz.transform, "Linea_" + distancia, ReglaCampoTiro.ZBarrera + ReglaCampoTiro.Metros(distancia), ancho, turquesa);
            }

            CrearLuz(raiz.transform, "Luz_Cerca", new Vector3(0f, AlturaLuces, ReglaCampoTiro.ZBarrera + 6f));
            CrearLuz(raiz.transform, "Luz_Lejos", new Vector3(0f, AlturaLuces, ReglaCampoTiro.ZBarrera + 28f));

            GameObject barrera = new GameObject("Barrera");
            barrera.transform.SetParent(raiz.transform, false);
            BoxCollider choque = barrera.AddComponent<BoxCollider>();
            choque.center = new Vector3(0f, AlturaBarrera * 0.5f, ReglaCampoTiro.ZBarrera);
            choque.size = new Vector3(ancho, AlturaBarrera, GrosorBarrera);
            PonerCostales(barrera.transform, datos, ancho);
            return raiz;
        }

        private static void PonerCostales(Transform padre, DatosCampoTiro datos, float ancho)
        {
            GameObject[] prefabs = datos != null ? datos.Costales : null;
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }
            float paso = AnchoCostalPorOmision;
            int cantidad = Mathf.CeilToInt(ancho / paso);
            for (int i = 0; i < cantidad; i++)
            {
                GameObject prefab = prefabs[i % prefabs.Length];
                if (prefab == null)
                {
                    continue;
                }
                GameObject costal = Object.Instantiate(prefab, padre);
                costal.name = "Costales_" + i;
                // Los costales son solo visual: el choque de la barrera es uno solo (así las balas y el cuerpo lo ven igual en todo el ancho).
                foreach (Collider choque in costal.GetComponentsInChildren<Collider>(true))
                {
                    Object.Destroy(choque);
                }
                costal.transform.position = padre.position + new Vector3(-ancho * 0.5f + paso * (i + 0.5f), 0f, ReglaCampoTiro.ZBarrera);
                costal.transform.rotation = Quaternion.identity;
            }
        }

        private static void Losa(Transform padre, string nombre, Vector3 local, Vector3 tamano, Material material, bool conChoque)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nombre;
            cubo.transform.SetParent(padre, false);
            cubo.transform.localPosition = local;
            cubo.transform.localScale = tamano;
            Renderer dibujo = cubo.GetComponent<Renderer>();
            dibujo.sharedMaterial = material;
            dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dibujo.receiveShadows = false;
            if (!conChoque)
            {
                Object.Destroy(cubo.GetComponent<Collider>());
            }
        }

        private static void Linea(Transform padre, string nombre, float z, float ancho, Material material)
        {
            Losa(padre, nombre, new Vector3(0f, AlturaLinea * 0.5f + 0.005f, z), new Vector3(ancho, AlturaLinea, AnchoLinea), material, false);
        }

        private static void CrearLuz(Transform padre, string nombre, Vector3 local)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = local;
            Light luz = objeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = Color.white;
            luz.range = AlcanceLuces;
            luz.intensity = IntensidadLuces;
            luz.shadows = LightShadows.None;
        }

        // Solo se usa si falta el asset DatosCampoTiro (pruebas sin Aplicar): en un build los materiales vienen del asset.
        private static Material MaterialPorOmision(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            return material;
        }
    }
}
