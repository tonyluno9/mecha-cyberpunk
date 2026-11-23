using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79) y T85: construye por código la sala del campo de tiro: un cubo cerrado y oscuro con cuadrícula holográfica cian en piso, paredes y techo
    /// (con niebla oscura la rejilla se pierde a lo lejos), líneas de distancia, una barrera de costales CB02 en una sola línea sin huecos con material
    /// holográfico, la terminal de configuración y paredes que no dejan salir. Las medidas salen de <see cref="ReglaCampoTiro"/>.
    /// </summary>
    public static class SalaCampoTiro
    {
        private const float Grosor = 1f;
        private const float AlturaBarreraPorOmision = 0.9f;
        private const float GrosorBarreraPorOmision = 0.8f;
        private const float AlturaLinea = 0.02f;
        private const float AnchoLinea = 0.15f;
        private const float AnchoCostalPorOmision = 3f;
        private const float MetrosPorCelda = 2f;
        private const float ZPlataformaJugador = ReglaCampoTiro.ZInicioJugador;
        private const float RadioPlataformaJugador = 1.2f;

        /// <summary>Raíz de la terminal (para las pruebas); null si no hay sala.</summary>
        public static TerminalCampoTiro UltimaTerminal { get; private set; }

        /// <summary>Crea la sala con su centro en <paramref name="centro"/> (mundo). Los datos pueden ser null: entonces usa materiales por defecto.</summary>
        public static GameObject Construir(DatosCampoTiro datos, Vector3 centro)
        {
            GameObject raiz = new GameObject("CampoTiro_Sala");
            raiz.transform.position = centro;
            Material rejilla = datos != null && datos.MaterialRejilla != null ? datos.MaterialRejilla : MaterialPorOmision(new Color(0.02f, 0.04f, 0.07f, 1f));
            Material turquesa = datos != null && datos.MaterialTurquesa != null ? datos.MaterialTurquesa : MaterialPorOmision(new Color(0.361f, 0.890f, 0.839f, 1f));

            float ancho = ReglaCampoTiro.MitadAncho * 2f;
            float fondo = ReglaCampoTiro.FondoAdelante - ReglaCampoTiro.FondoAtras;
            float zMedio = (ReglaCampoTiro.FondoAdelante + ReglaCampoTiro.FondoAtras) * 0.5f;
            float alto = ReglaCampoTiro.AlturaSala;
            // Losas por fuera de la sala: la cara que mira hacia adentro es la que se ve. Cada una con el tiling de su cara para que las celdas sean cuadradas.
            Losa(raiz.transform, "Piso", new Vector3(0f, -Grosor * 0.5f, zMedio), new Vector3(ancho + Grosor * 2f, Grosor, fondo + Grosor * 2f), Celdas(rejilla, ancho, fondo), true);
            Losa(raiz.transform, "Techo", new Vector3(0f, alto + Grosor * 0.5f, zMedio), new Vector3(ancho + Grosor * 2f, Grosor, fondo + Grosor * 2f), Celdas(rejilla, ancho, fondo), true);
            Losa(raiz.transform, "ParedIzquierda", new Vector3(-ReglaCampoTiro.MitadAncho - Grosor * 0.5f, alto * 0.5f, zMedio), new Vector3(Grosor, alto, fondo), Celdas(rejilla, fondo, alto), true);
            Losa(raiz.transform, "ParedDerecha", new Vector3(ReglaCampoTiro.MitadAncho + Grosor * 0.5f, alto * 0.5f, zMedio), new Vector3(Grosor, alto, fondo), Celdas(rejilla, fondo, alto), true);
            Losa(raiz.transform, "ParedAtras", new Vector3(0f, alto * 0.5f, ReglaCampoTiro.FondoAtras - Grosor * 0.5f), new Vector3(ancho, alto, Grosor), Celdas(rejilla, ancho, alto), true);
            Losa(raiz.transform, "ParedFrente", new Vector3(0f, alto * 0.5f, ReglaCampoTiro.FondoAdelante + Grosor * 0.5f), new Vector3(ancho, alto, Grosor), Celdas(rejilla, ancho, alto), true);

            // Líneas turquesa: el punto de salida, la barrera y cada distancia de los objetivos.
            Linea(raiz.transform, "LineaSalida", ReglaCampoTiro.ZInicioJugador, ancho, turquesa);
            Linea(raiz.transform, "LineaBarrera", ReglaCampoTiro.ZBarrera, ancho, turquesa);
            foreach (DistanciaTiro distancia in (DistanciaTiro[])System.Enum.GetValues(typeof(DistanciaTiro)))
            {
                Linea(raiz.transform, "Linea_" + distancia, ReglaCampoTiro.ZBarrera + ReglaCampoTiro.Metros(distancia), ancho, turquesa);
            }

            Material holo = datos != null ? datos.MaterialHolo : null;
            // Plataforma de aparición del jugador: un disco holográfico bajo sus pies.
            GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "PlataformaJugador";
            disco.transform.SetParent(raiz.transform, false);
            disco.transform.localPosition = new Vector3(0f, 0.01f, ZPlataformaJugador);
            disco.transform.localScale = new Vector3(RadioPlataformaJugador * 2f, 0.01f, RadioPlataformaJugador * 2f);
            Object.Destroy(disco.GetComponent<Collider>());
            Renderer dibujoDisco = disco.GetComponent<Renderer>();
            dibujoDisco.sharedMaterial = holo != null ? holo : turquesa;
            dibujoDisco.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject barrera = new GameObject("Barrera");
            barrera.transform.SetParent(raiz.transform, false);
            BoxCollider choque = barrera.AddComponent<BoxCollider>();
            Vector2 medida = PonerCostales(barrera.transform, datos, ancho, holo != null ? holo : turquesa);
            float alturaBarrera = medida.x > 0f ? medida.x : AlturaBarreraPorOmision;
            float grosorBarrera = medida.y > 0f ? medida.y : GrosorBarreraPorOmision;
            choque.center = new Vector3(0f, alturaBarrera * 0.5f, ReglaCampoTiro.ZBarrera);
            choque.size = new Vector3(ancho, alturaBarrera, grosorBarrera);

            UltimaTerminal = TerminalCampoTiro.Crear(raiz.transform, new Vector3(ReglaCampoTiro.XTerminal, 0f, ReglaCampoTiro.ZTerminal), holo != null ? holo : turquesa);
            return raiz;
        }

        /// <summary>Una plataforma holográfica de <paramref name="altura"/> metros bajo un objetivo (hija de <paramref name="padre"/>, en <paramref name="posicionMundo"/>).</summary>
        public static GameObject CrearPlataforma(Transform padre, Vector3 posicionMundo, float altura, DatosCampoTiro datos)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Plataforma";
            cubo.transform.SetParent(padre, true);
            cubo.transform.position = posicionMundo + Vector3.up * (altura * 0.5f);
            cubo.transform.localScale = new Vector3(ReglaCampoTiro.SeparacionObjetivos * 0.8f, altura, 2f);
            Object.Destroy(cubo.GetComponent<Collider>());
            Renderer dibujo = cubo.GetComponent<Renderer>();
            dibujo.sharedMaterial = datos != null && datos.MaterialHolo != null ? datos.MaterialHolo : MaterialPorOmision(new Color(0.361f, 0.890f, 0.839f, 0.5f));
            dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return cubo;
        }

        // Una línea recta de costales pegados: cada uno se mide (bounds) y se coloca justo después del anterior. Devuelve (altura máxima, fondo máximo).
        private static Vector2 PonerCostales(Transform padre, DatosCampoTiro datos, float ancho, Material holo)
        {
            GameObject[] prefabs = datos != null ? datos.Costales : null;
            if (prefabs == null || prefabs.Length == 0)
            {
                return Vector2.zero;
            }
            float cursor = -ancho * 0.5f;
            float alturaMax = 0f;
            float fondoMax = 0f;
            int i = 0;
            int guardia = 0;
            while (cursor < ancho * 0.5f - 0.01f && guardia++ < 200)
            {
                GameObject prefab = prefabs[i % prefabs.Length];
                i++;
                if (prefab == null)
                {
                    continue;
                }
                GameObject costal = Object.Instantiate(prefab, padre);
                costal.name = "Costales_" + (i - 1);
                foreach (Collider choque in costal.GetComponentsInChildren<Collider>(true))
                {
                    Object.Destroy(choque);
                }
                costal.transform.rotation = Quaternion.identity;
                costal.transform.position = padre.position;
                Bounds limites = MedirDibujos(costal.transform, out bool hay);
                if (!hay || limites.size.x < 0.05f)
                {
                    Object.Destroy(costal);
                    cursor += AnchoCostalPorOmision;
                    continue;
                }
                // El borde izquierdo del costal en el cursor, la base en el piso y su centro en la línea de la barrera.
                // Los límites vienen en coordenadas del mundo: se pasan a relativos al padre (la sala está a 800 m de altura).
                Vector3 min = limites.min - padre.position;
                Vector3 centroLocal = limites.center - padre.position;
                Vector3 desvio = new Vector3(cursor - min.x, -min.y, ReglaCampoTiro.ZBarrera - centroLocal.z);
                costal.transform.position = padre.position + desvio;
                cursor += limites.size.x;
                alturaMax = Mathf.Max(alturaMax, limites.size.y);
                fondoMax = Mathf.Max(fondoMax, limites.size.z);
                foreach (Renderer dibujo in costal.GetComponentsInChildren<Renderer>(true))
                {
                    int cuantos = Mathf.Max(1, dibujo.sharedMaterials.Length);
                    Material[] lista = new Material[cuantos];
                    for (int k = 0; k < cuantos; k++)
                    {
                        lista[k] = holo;
                    }
                    dibujo.sharedMaterials = lista;
                    dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            return new Vector2(alturaMax, fondoMax);
        }

        private static Bounds MedirDibujos(Transform raiz, out bool hay)
        {
            hay = false;
            Bounds limites = new Bounds(raiz.position, Vector3.zero);
            foreach (Renderer dibujo in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (!(dibujo is MeshRenderer) && !(dibujo is SkinnedMeshRenderer))
                {
                    continue;
                }
                if (!hay)
                {
                    limites = dibujo.bounds;
                    hay = true;
                }
                else
                {
                    limites.Encapsulate(dibujo.bounds);
                }
            }
            return limites;
        }

        // Copia del material con el tiling para que las celdas midan MetrosPorCelda en una cara de a por b metros.
        private static Material Celdas(Material baseRejilla, float a, float b)
        {
            Material copia = new Material(baseRejilla);
            Vector2 repeticion = new Vector2(a / MetrosPorCelda, b / MetrosPorCelda);
            if (copia.HasProperty("_BaseMap"))
            {
                copia.SetTextureScale("_BaseMap", repeticion);
            }
            copia.mainTextureScale = repeticion;
            return copia;
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

        // Solo se usa si falta el asset DatosCampoTiro (pruebas sin Aplicar): en un build los materiales vienen del asset.
        internal static Material MaterialPorOmision(Color color)
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
