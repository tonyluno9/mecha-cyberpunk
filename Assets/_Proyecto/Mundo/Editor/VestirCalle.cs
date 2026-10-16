using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-080: viste la calle del barrio con el kit urbano (T-076): postes de luz con cables, transformadores, puestos de tacos,
    /// basureros con grafiti y azoteas con aire acondicionado y tinaco. Lo llama CreadorBarrio al armar el prefab Barrio.
    /// Todo con semilla fija: correrlo dos veces deja el barrio igual. Los colliders del kit se ajustan para dejar libre la
    /// franja de caminar (37.3 a 38.7 m del centro de la manzana) y no salirse de la banqueta.
    /// </summary>
    internal static class VestirCalle
    {
        private const string RutaKit = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const string RutaHule = "Assets/Arte/Materiales/Hule_Negro.mat";
        private const float AltoBanqueta = 0.2f;
        private const float DistanciaPoste = 39.4f;
        private const float DistanciaPuesto = 39.35f;
        private const float DistanciaBasurero = 39.3f;
        private const float SeparacionTransformador = 4f;
        private const int CadaCuantosPostesTransformador = 3;
        private const float AlturaLuz = 7.2f;
        private const float AdelanteLuz = 1.55f;
        private const float AlturaCableLado = 6.2f;
        private const float AlturaCableCruce = 7.0f;
        private const float CombaLado = 0.6f;
        private const float CombaCruce = 0.7f;
        private const float AnchoCable = 0.035f;
        private const int PuntosCable = 9;
        private const float SeparacionCables = 0.12f;
        private const float MitadBrazoCable = 0.55f;
        private const float ClaroPoste = 1.6f;
        private const float ClaroTransformador = 1.8f;
        private const float ClaroPuesto = 2.7f;
        private const float DistanciaCruce = 15.2f;
        private const float ToleranciaCruce = 1f;
        private const float ToleranciaAlineacion = 0.1f;
        private const float MitadAzotea = 1.65f;
        private const float DistanciaEntradaMinima = 7f;

        private sealed class DatosPoste
        {
            public string Lado;
            public Vector2 Posicion;
            public Vector2 Normal;
            public Vector2 Tangente;
            public float Desplazamiento;
        }

        private static readonly List<DatosPoste> postes = new List<DatosPoste>();
        private static readonly List<KeyValuePair<Vector2, float>> ocupados = new List<KeyValuePair<Vector2, float>>();
        private static Material hule;
        private static List<Vector3> puntosEntrada;
        private static Transform grupoCables;

        internal static void Preparar(List<Vector3> entradas)
        {
            postes.Clear();
            puntosEntrada = entradas;
            ocupados.Clear();
            hule = AssetDatabase.LoadAssetAtPath<Material>(RutaHule);
            grupoCables = null;
        }

        // Las piezas del kit con collider van a más de 6 m (con margen) de cualquier entrada.
        private static bool CercaDeEntrada(Vector2 punto)
        {
            foreach (Vector3 entrada in puntosEntrada)
            {
                if (Vector2.Distance(punto, new Vector2(entrada.x, entrada.z)) < DistanciaEntradaMinima)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool EstaOcupado(Vector2 punto, float margen)
        {
            foreach (KeyValuePair<Vector2, float> ocupado in ocupados)
            {
                if (Vector2.Distance(punto, ocupado.Key) < ocupado.Value + margen)
                {
                    return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- piezas del kit

        // Instancia una pieza del kit; con 'colliderCentro' la desempaca y le deja un collider más chico (el original cubre cables y toldos).
        private static GameObject Pieza(string prefab, Transform padre, Vector3 posicion, Quaternion rotacion, string nombre, bool ajustar = false, Vector3 centro = default, Vector3 tamano = default)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaKit + prefab + ".prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(modelo, padre);
            go.name = nombre;
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            if (ajustar)
            {
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                BoxCollider caja = go.GetComponent<BoxCollider>();
                caja.center = centro;
                caja.size = tamano;
            }
            return go;
        }

        private static Vector3 Mundo3(Vector2 punto, float altura)
        {
            return new Vector3(punto.x, altura, punto.y);
        }

        private static Quaternion Mirar(Vector2 direccion)
        {
            return Quaternion.LookRotation(new Vector3(direccion.x, 0f, direccion.y), Vector3.up);
        }

        // ---------------------------------------------------------------- postes y transformadores

        /// <summary>Crea "Farola_N": un Poste_Luz con el brazo hacia la calle y su Light (la prende LuzNocturna). Cada tercer poste lleva un Transformador_Poste al lado.</summary>
        internal static Transform Farola(Transform padre, int numero, Vector2 centroManzana, int manzana, Vector2 normal, float desplazamiento)
        {
            Vector2 tangente = new Vector2(-normal.y, normal.x);
            Vector2 posicion = centroManzana + normal * DistanciaPoste + tangente * desplazamiento;
            if (CercaDeEntrada(posicion))
            {
                return null;
            }
            Transform farola = CreadorBarrio.Vacio("Farola_" + numero, padre, Mundo3(posicion, AltoBanqueta), Mirar(normal));
            Pieza("Poste_Luz", farola, Vector3.zero, Quaternion.identity, "Poste_Luz");

            GameObject luz = new GameObject("Luz");
            luz.transform.SetParent(farola, false);
            luz.transform.localPosition = new Vector3(0f, AlturaLuz, AdelanteLuz);
            Light componente = luz.AddComponent<Light>();
            componente.type = LightType.Point;
            componente.color = new Color(1f, 0.85f, 0.6f);
            componente.range = 22f; // T-079: más luz para que el asfalto mojado la refleje
            componente.intensity = 160f;
            componente.shadows = LightShadows.None;
            componente.enabled = false; // T-041 la prende de noche

            postes.Add(new DatosPoste { Lado = manzana + "_" + normal.x + "_" + normal.y, Posicion = posicion, Normal = normal, Tangente = tangente, Desplazamiento = desplazamiento });
            ocupados.Add(new KeyValuePair<Vector2, float>(posicion, ClaroPoste));

            if ((postes.Count - 1) % CadaCuantosPostesTransformador == 0)
            {
                Vector2 lugar = posicion - tangente * Mathf.Sign(desplazamiento) * SeparacionTransformador;
                if (CercaDeEntrada(lugar))
                {
                    lugar = posicion + tangente * Mathf.Sign(desplazamiento) * SeparacionTransformador;
                }
                Pieza("Transformador_Poste", padre, Mundo3(lugar, AltoBanqueta), Mirar(normal), "Transformador_Poste_" + numero,
                    true, new Vector3(0f, 1.5f, -0.2f), new Vector3(0.6f, 3f, 0.9f));
                ocupados.Add(new KeyValuePair<Vector2, float>(lugar, ClaroTransformador));
            }
            return farola;
        }

        // ---------------------------------------------------------------- cables

        /// <summary>Cables con comba entre postes vecinos del mismo lado de la calle y algunos cruzando la calle (a más de 6 m).</summary>
        internal static void Cables(Transform barrio)
        {
            grupoCables = CreadorBarrio.Vacio("Cables", barrio, Vector3.zero, Quaternion.identity);
            int numero = 0;
            for (int i = 0; i < postes.Count; i++)
            {
                DatosPoste a = postes[i];
                for (int j = i + 1; j < postes.Count; j++)
                {
                    DatosPoste b = postes[j];
                    if (a.Lado == b.Lado && EsVecino(a, b))
                    {
                        CablesLado(a, b, ref numero);
                    }
                    else if (EsCruce(a, b))
                    {
                        CablesCruce(a, b, ref numero);
                    }
                }
            }
        }

        // Vecinos: en el mismo lado y sin otro poste entre los dos.
        private static bool EsVecino(DatosPoste a, DatosPoste b)
        {
            foreach (DatosPoste otro in postes)
            {
                if (otro != a && otro != b && otro.Lado == a.Lado
                    && otro.Desplazamiento > Mathf.Min(a.Desplazamiento, b.Desplazamiento)
                    && otro.Desplazamiento < Mathf.Max(a.Desplazamiento, b.Desplazamiento))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EsCruce(DatosPoste a, DatosPoste b)
        {
            if (a.Lado == b.Lado || Vector2.Dot(a.Normal, b.Normal) > -0.99f || Mathf.Abs(Mathf.Abs(a.Desplazamiento) - 12f) > 0.01f)
            {
                return false;
            }
            Vector2 resta = b.Posicion - a.Posicion;
            float largo = Mathf.Abs(Vector2.Dot(resta, a.Normal));
            float lateral = Mathf.Abs(Vector2.Dot(resta, a.Tangente));
            // El poste de enfrente debe mirar hacia el primero.
            return lateral < ToleranciaAlineacion && Mathf.Abs(largo - DistanciaCruce) < ToleranciaCruce && Vector2.Dot(resta, a.Normal) > 0f;
        }

        private static void CablesLado(DatosPoste a, DatosPoste b, ref int numero)
        {
            Vector2 direccion = (b.Posicion - a.Posicion).normalized;
            for (int k = -1; k <= 1; k++)
            {
                Vector2 lateral = a.Normal * (k * SeparacionCables);
                Vector3 desde = Mundo3(a.Posicion + direccion * MitadBrazoCable + lateral, AltoBanqueta + AlturaCableLado - k * 0.05f);
                Vector3 hasta = Mundo3(b.Posicion - direccion * MitadBrazoCable + lateral, AltoBanqueta + AlturaCableLado - k * 0.05f);
                Cable("Cable_" + (++numero), desde, hasta, CombaLado + (k + 1) * 0.08f);
            }
        }

        private static void CablesCruce(DatosPoste a, DatosPoste b, ref int numero)
        {
            for (int k = 0; k < 2; k++)
            {
                Vector2 lateral = a.Tangente * (k == 0 ? -0.15f : 0.15f);
                Cable("Cable_" + (++numero), Mundo3(a.Posicion + lateral, AltoBanqueta + AlturaCableCruce), Mundo3(b.Posicion + lateral, AltoBanqueta + AlturaCableCruce), CombaCruce + k * 0.1f);
            }
        }

        private static void Cable(string nombre, Vector3 desde, Vector3 hasta, float comba)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(grupoCables, false);
            LineRenderer linea = go.AddComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.positionCount = PuntosCable;
            for (int i = 0; i < PuntosCable; i++)
            {
                float t = i / (PuntosCable - 1f);
                linea.SetPosition(i, Vector3.Lerp(desde, hasta, t) + Vector3.down * (comba * 4f * t * (1f - t)));
            }
            linea.widthMultiplier = AnchoCable;
            linea.numCapVertices = 2;
            linea.generateLightingData = true;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.sharedMaterial = hule;
        }

        // ---------------------------------------------------------------- puestos y basureros

        internal static Transform GrupoPuestos(Transform barrio)
        {
            return CreadorBarrio.Vacio("PuestosTacos", barrio, Vector3.zero, Quaternion.identity);
        }

        /// <summary>Un puesto de tacos en la banqueta de una manzana, con el letrero hacia la calle y la cara hacia el edificio.</summary>
        internal static void PuestoBanqueta(Transform grupo, Vector2 centroManzana, Vector2 normal, float desplazamiento, string nombre)
        {
            Vector2 tangente = new Vector2(-normal.y, normal.x);
            Vector2 posicion = centroManzana + normal * DistanciaPuesto + tangente * desplazamiento;
            PuestoTacos(grupo, Mundo3(posicion, AltoBanqueta), Mirar(-normal), nombre);
            ocupados.Add(new KeyValuePair<Vector2, float>(posicion, ClaroPuesto));
        }

        private static void PuestoTacos(Transform padre, Vector3 posicion, Quaternion rotacion, string nombre)
        {
            Pieza("Puesto_Tacos", padre, posicion, rotacion, nombre, true, new Vector3(0f, 0.85f, 0f), new Vector3(3.1f, 1.7f, 1.2f));
        }

        /// <summary>Los puestos del mercado negro, pegados al muro este del callejón (x = -19.9 local de Manzana_2) y de frente al callejón.</summary>
        internal static void PuestosCallejon(Transform manzana)
        {
            Transform puestos = CreadorBarrio.Vacio("PuestosCallejon", manzana, Vector3.zero, Quaternion.identity);
            float[] profundidades = { -33f, -29f, -9f, -5f };
            for (int i = 0; i < profundidades.Length; i++)
            {
                PuestoTacos(puestos, new Vector3(-20.5f, AltoBanqueta, profundidades[i]), Quaternion.LookRotation(Vector3.left, Vector3.up), "Puesto_Tacos_" + (i + 1));
            }
            CreadorBarrio.Cubo("Caja", puestos, new Vector3(-24.2f, AltoBanqueta + 0.5f, -8f), new Vector3(1.4f, 1f, 1.4f), "Mercado");
        }

        /// <summary>Un Basurero_Grafiti en la banqueta; mira a la calle o a los edificios según el azar.</summary>
        internal static void Basurero(Transform padre, int numero, Vector2 centroManzana, Vector2 normal, float desplazamiento, Random azar)
        {
            Vector2 tangente = new Vector2(-normal.y, normal.x);
            Vector2 posicion = centroManzana + normal * DistanciaBasurero + tangente * desplazamiento;
            Vector2 hacia = azar.NextDouble() < 0.5 ? normal : -normal;
            Pieza("Basurero_Grafiti", padre, Mundo3(posicion, AltoBanqueta), Mirar(hacia), "Basurero_" + numero, true, new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 1.5f, 1.1f));
        }

        // ---------------------------------------------------------------- azoteas

        /// <summary>El aire acondicionado con tinaco del kit sobre una azotea, girado de a 90 grados y sin salirse del techo.</summary>
        internal static void AzoteaKit(Transform padre, float ancho, float fondo, float altura, float preferidoX, float preferidoZ, Random azar)
        {
            float limiteX = Mathf.Max(0f, ancho / 2f - MitadAzotea);
            float limiteZ = Mathf.Max(0f, fondo / 2f - MitadAzotea);
            Vector3 posicion = new Vector3(Mathf.Clamp(preferidoX, -limiteX, limiteX), altura, Mathf.Clamp(preferidoZ, -limiteZ, limiteZ));
            Pieza("Azotea_AC_Tinaco", padre, posicion, Quaternion.Euler(0f, 90f * azar.Next(4), 0f), "Azotea_AC_Tinaco");
        }

        /// <summary>En la mitad de las azoteas pobres: un tinaco negro sobre una plataforma de castillos con la varilla expuesta.</summary>
        internal static void TinacoPobre(Transform padre, float ancho, float altura, Random azar)
        {
            if (azar.NextDouble() >= 0.5)
            {
                return;
            }
            Transform grupo = CreadorBarrio.Vacio("TinacoPobre", padre, new Vector3(ancho * 0.25f, altura, 0f), Quaternion.Euler(0f, 90f * azar.Next(4), 0f));
            const float alto = 2.1f;
            foreach (Vector2 esquina in new[] { new Vector2(-0.7f, -0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, 0.7f) })
            {
                CreadorBarrio.Cubo("Castillo", grupo, new Vector3(esquina.x, alto / 2f, esquina.y), new Vector3(0.18f, alto, 0.18f), "Concreto", false);
                float largoVarilla = 0.35f + (float)azar.NextDouble() * 0.4f;
                Varilla(grupo, new Vector3(esquina.x, alto + largoVarilla / 2f - 0.05f, esquina.y), largoVarilla, (float)azar.NextDouble() * 12f - 6f);
            }
            CreadorBarrio.Cubo("Plataforma", grupo, new Vector3(0f, alto + 0.07f, 0f), new Vector3(1.8f, 0.14f, 1.8f), "ConcretoOscuro", false);
            GameObject tinaco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tinaco.name = "Tinaco";
            tinaco.transform.SetParent(grupo, false);
            tinaco.transform.localPosition = new Vector3(0f, alto + 0.14f + 0.55f, 0f);
            tinaco.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
            tinaco.GetComponent<MeshRenderer>().sharedMaterial = hule;
            Object.DestroyImmediate(tinaco.GetComponent<Collider>());
        }

        private static void Varilla(Transform padre, Vector3 posicion, float largo, float inclinacion)
        {
            GameObject varilla = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            varilla.name = "Varilla";
            varilla.transform.SetParent(padre, false);
            varilla.transform.localPosition = posicion;
            varilla.transform.localRotation = Quaternion.Euler(inclinacion, 0f, -inclinacion);
            varilla.transform.localScale = new Vector3(0.035f, largo / 2f, 0.035f);
            varilla.GetComponent<MeshRenderer>().sharedMaterial = CreadorBarrio.M("Farola");
            Object.DestroyImmediate(varilla.GetComponent<Collider>());
        }
    }
}
