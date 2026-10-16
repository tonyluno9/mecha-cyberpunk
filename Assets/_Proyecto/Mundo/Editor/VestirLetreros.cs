using System.Collections.Generic;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-082: cambia los letreros greybox de los edificios por letreros de neón del kit (T-076 y T-082): de bandera
    /// (perpendiculares a la fachada), planos sobre la fachada y algunos en azoteas, más el letrero propio del clandestino
    /// (PELEAS), el bar (cantina) y el casino (CASINO, más tenue por estar cerrado). Cada letrero es un objeto
    /// "LetreroNeon_N_Modelo" con su <see cref="ParpadeoNeon"/> (día y noche, y uno de cada seis parpadea).
    /// Todo con semilla fija: correrlo dos veces deja el barrio igual. El letrero de horario de cada local no se tapa.
    /// Lo llama CreadorBarrio antes de guardar el prefab.
    /// </summary>
    internal static class VestirLetreros
    {
        internal const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigNeon_Mundo.asset";
        internal const string PrefijoLetrero = "LetreroNeon_";
        private const string RutaKit = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const int Semilla = 82;
        private const float EscalaBandera = 1.5f; // las banderas se ven chicas de lejos: 1.5 veces el modelo (salen 1.1 m)
        private const int SemillaParpadeo = 8200;
        private const int CuposCentro = 20;
        private const int CuposEste = 12;
        private const int CuposPobre = 6;
        private const float AlturaMinimaPlano = 6.3f;
        private const float AlturaMinimaBandera = 6.8f;
        private const float MargenAzotea = 0.7f;
        private const float MargenLateral = 0.6f;
        private const float MargenBandera = 1f;
        private const float HolguraSuperior = 0.7f;
        private const float HolguraPared = 0.01f;
        private const float EntradaPared = 0.03f;
        private const float RetiroAzotea = 0.5f;
        private const float SeparacionLetreros = 0.3f;
        private const float AlturaLetreroLugar = 5.5f;
        private const float XCantina = 3.6f;
        private const float AlturaCantina = 3.3f;

        private enum Tipo
        {
            Plano,
            Bandera,
            Azotea
        }

        private readonly struct Modelo
        {
            public readonly string Prefab;
            public readonly Tipo Tipo;

            public Modelo(string prefab, Tipo tipo)
            {
                Prefab = prefab;
                Tipo = tipo;
            }
        }

        private sealed class Medida
        {
            public GameObject Prefab;
            public Bounds Limites; // en el espacio del prefab, en su pose original
        }

        private static readonly Modelo[] Patron =
        {
            new Modelo("Letrero_Birria", Tipo.Plano), new Modelo("Letrero_Hotel", Tipo.Bandera), new Modelo("Letrero_Refacciones", Tipo.Plano),
            new Modelo("Letrero_Hotel_Azotea", Tipo.Azotea), new Modelo("Letrero_Ciber", Tipo.Bandera), new Modelo("Letrero_Lavanderia", Tipo.Plano),
            new Modelo("Letrero_Flecha", Tipo.Plano), new Modelo("Letrero_Ciber", Tipo.Bandera), new Modelo("Letrero_Birria", Tipo.Plano),
            new Modelo("Letrero_Lavanderia", Tipo.Plano), new Modelo("Letrero_Hotel_Azotea", Tipo.Azotea), new Modelo("Letrero_Hotel", Tipo.Bandera)
        };

        private static readonly Vector3[] Normales = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        private static readonly string[] NombresLado = { "Norte", "Sur", "Este", "Oeste" };

        private static readonly Dictionary<string, Medida> medidas = new Dictionary<string, Medida>();
        private static readonly List<Bounds> colocados = new List<Bounds>();
        private static ConfigNeon config;
        private static Transform grupo;
        private static int numero;
        private static int siguiente;

        /// <summary>Viste el barrio con letreros. Devuelve cuántos puso.</summary>
        internal static int Vestir(Transform raiz)
        {
            medidas.Clear();
            colocados.Clear();
            numero = 0;
            siguiente = 0;
            config = CargarOCrearConfig();
            grupo = CreadorBarrio.Vacio("LetrerosNeon", raiz, Vector3.zero, Quaternion.identity);

            QuitarGreybox(raiz);
            ColocarLugares(raiz);
            ColocarEnEdificios(raiz);
            MarcarParpadeos();
            return numero;
        }

        private static ConfigNeon CargarOCrearConfig()
        {
            ConfigNeon datos = AssetDatabase.LoadAssetAtPath<ConfigNeon>(RutaConfig);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<ConfigNeon>();
                AssetDatabase.CreateAsset(datos, RutaConfig);
            }
            return datos;
        }

        // Los cubos "Letrero" de los edificios planos se quitan (los de los Lugar_* son el fondo del letrero de horario y se quedan).
        private static void QuitarGreybox(Transform raiz)
        {
            List<GameObject> sobran = new List<GameObject>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Letrero" && t.parent != null && t.parent.name.StartsWith("Edificio_"))
                {
                    sobran.Add(t.gameObject);
                }
            }
            foreach (GameObject objeto in sobran)
            {
                Object.DestroyImmediate(objeto);
            }
        }

        private static Medida Medir(string prefab)
        {
            if (medidas.TryGetValue(prefab, out Medida medida))
            {
                return medida;
            }
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaKit + prefab + ".prefab");
            GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            copia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Renderer[] renderers = copia.GetComponentsInChildren<Renderer>();
            Bounds limites = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                limites.Encapsulate(renderers[i].bounds);
            }
            Object.DestroyImmediate(copia);
            medida = new Medida { Prefab = asset, Limites = limites };
            medidas[prefab] = medida;
            return medida;
        }

        // ---------------------------------------------------------------- locales

        private static void ColocarLugares(Transform raiz)
        {
            Transform clandestino = raiz.Find("Manzana_2/Lugar_Clandestino");
            Transform bar = raiz.Find("Manzana_5/Lugar_Bar");
            Transform casino = raiz.Find("Manzana_3/Lugar_Casino");
            // Sobre el letrero de horario (que llega a 5.1 m) y al centro de la puerta.
            ColocarEnLugar(clandestino, "Letrero_Peleas", 0f, AlturaLetreroLugar, false);
            // La cantina, de pie al lado de la puerta, fuera del ancho del letrero de horario.
            ColocarEnLugar(bar, "Letrero_Cantina", XCantina, AlturaCantina, false);
            ColocarEnLugar(casino, "Letrero_Casino", 0f, AlturaLetreroLugar, true);
        }

        private static void ColocarEnLugar(Transform lugar, string prefab, float x, float baseY, bool cerrado)
        {
            Transform dintel = lugar.Find("Dintel");
            float zFrente = dintel.localPosition.z + dintel.localScale.z / 2f;
            Medida medida = Medir(prefab);
            float atras = -medida.Limites.min.z;
            Vector3 local = new Vector3(x, baseY, zFrente + atras - HolguraPared);
            Crear(prefab, lugar.TransformPoint(local), lugar.rotation, cerrado);
        }

        // ---------------------------------------------------------------- edificios

        private sealed class Candidato
        {
            public Transform Edificio;
            public string Zona;
        }

        private static void ColocarEnEdificios(Transform raiz)
        {
            Random azar = new Random(Semilla);
            List<Candidato> centro = new List<Candidato>();
            List<Candidato> este = new List<Candidato>();
            List<Candidato> pobre = new List<Candidato>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Edificio_") || t.parent == null || t.parent.name != "Edificios")
                {
                    continue;
                }
                string manzana = t.parent.parent.name;
                Candidato candidato = new Candidato { Edificio = t, Zona = manzana };
                if (manzana == "Manzana_1" || manzana == "Manzana_4")
                {
                    pobre.Add(candidato);
                }
                else if (manzana == "Manzana_2" || manzana == "Manzana_5")
                {
                    centro.Add(candidato);
                }
                else
                {
                    este.Add(candidato);
                }
            }
            Llenar(centro, CuposCentro, azar);
            Llenar(este, CuposEste, azar);
            Llenar(pobre, CuposPobre, azar);
        }

        private static void Barajar(List<Candidato> lista, Random azar)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                Candidato guardado = lista[i];
                lista[i] = lista[j];
                lista[j] = guardado;
            }
        }

        private static void Llenar(List<Candidato> candidatos, int cupos, Random azar)
        {
            Barajar(candidatos, azar);
            int puestos = 0;
            foreach (Candidato candidato in candidatos)
            {
                if (puestos >= cupos)
                {
                    break;
                }
                if (IntentarEdificio(candidato.Edificio, azar, candidato.Zona == "Manzana_1" || candidato.Zona == "Manzana_4"))
                {
                    puestos++;
                }
            }
        }

        private static bool IntentarEdificio(Transform edificio, Random azar, bool pobre)
        {
            List<int> lados = new List<int>();
            for (int i = 0; i < Normales.Length; i++)
            {
                if (edificio.Find("Lado_" + NombresLado[i]) != null)
                {
                    lados.Add(i);
                }
            }
            if (lados.Count == 0)
            {
                return false;
            }
            int lado = lados[azar.Next(lados.Count)];
            for (int intento = 0; intento < Patron.Length; intento++)
            {
                Modelo modelo = Patron[(siguiente + intento) % Patron.Length];
                if (pobre && modelo.Tipo == Tipo.Azotea)
                {
                    continue; // las azoteas pobres llevan lámina inclinada
                }
                if (IntentarColocar(edificio, lado, modelo, azar))
                {
                    siguiente = (siguiente + intento + 1) % Patron.Length;
                    return true;
                }
            }
            return false;
        }

        private static bool IntentarColocar(Transform edificio, int lado, Modelo modelo, Random azar)
        {
            Transform cuerpo = edificio.Find("Cuerpo");
            Vector3 tamano = cuerpo.localScale;
            Vector3 normal = Normales[lado];
            Vector3 tangente = new Vector3(-normal.z, 0f, normal.x);
            float mitad = (normal.x != 0f ? tamano.x : tamano.z) / 2f;
            float largo = normal.x != 0f ? tamano.z : tamano.x;
            float altura = tamano.y;
            float sueloY = edificio.position.y;
            Vector3 pared = edificio.position + normal * mitad;
            Medida medida = Medir(modelo.Prefab);
            Bounds b = medida.Limites;
            Quaternion rotacion = Quaternion.LookRotation(normal, Vector3.up);

            Vector3 posicion;
            Quaternion giroModelo = Quaternion.identity;
            Bounds ocupado;
            switch (modelo.Tipo)
            {
                case Tipo.Plano:
                {
                    float rango = largo / 2f - b.size.x / 2f - MargenLateral;
                    float yMax = sueloY + altura - b.size.y - HolguraSuperior;
                    if (rango < 0f || yMax < AlturaMinimaPlano)
                    {
                        return false;
                    }
                    float y = AlturaMinimaPlano + (float)azar.NextDouble() * (yMax - AlturaMinimaPlano);
                    float u = ((float)azar.NextDouble() * 2f - 1f) * rango;
                    posicion = pared + tangente * u + normal * (-b.min.z - HolguraPared);
                    posicion.y = y;
                    ocupado = Caja(posicion, rotacion, b);
                    break;
                }
                case Tipo.Bandera:
                {
                    float rango = largo / 2f - MargenBandera;
                    float yMax = sueloY + altura - b.size.y * EscalaBandera - HolguraSuperior;
                    if (rango < 0f || yMax < AlturaMinimaBandera)
                    {
                        return false;
                    }
                    float y = AlturaMinimaBandera + (float)azar.NextDouble() * (yMax - AlturaMinimaBandera);
                    float u = ((float)azar.NextDouble() * 2f - 1f) * rango;
                    posicion = pared + tangente * u + normal * EntradaPared;
                    posicion.y = y;
                    // El cuerpo de la bandera sale hacia +X o -X del prefab; se gira para que el brazo mire a la pared (-Z del letrero).
                    Vector3 haciaPared = b.center.x > 0f ? Vector3.left : Vector3.right;
                    giroModelo = Quaternion.FromToRotation(haciaPared, Vector3.back);
                    ocupado = Caja(posicion, rotacion * giroModelo, new Bounds(b.center * EscalaBandera, b.size * EscalaBandera));
                    break;
                }
                default:
                {
                    if (edificio.Find("Piso2") != null)
                    {
                        return false;
                    }
                    float rango = largo / 2f - b.size.x / 2f - MargenLateral;
                    if (rango < 0f)
                    {
                        return false;
                    }
                    float u = ((float)azar.NextDouble() * 2f - 1f) * rango;
                    posicion = pared + tangente * u - normal * (RetiroAzotea + b.max.z);
                    posicion.y = sueloY + altura;
                    ocupado = Caja(posicion, rotacion, b);
                    break;
                }
            }

            if (Choca(ocupado, edificio, modelo.Tipo == Tipo.Azotea))
            {
                return false;
            }
            Crear(modelo.Prefab, posicion, rotacion, false, giroModelo, modelo.Tipo == Tipo.Bandera ? EscalaBandera : 1f);
            return true;
        }

        private static Bounds Caja(Vector3 posicion, Quaternion rotacion, Bounds local)
        {
            Bounds mundo = new Bounds(posicion + rotacion * local.center, Vector3.zero);
            foreach (Vector3 esquina in Esquinas(local))
            {
                mundo.Encapsulate(posicion + rotacion * esquina);
            }
            return mundo;
        }

        private static IEnumerable<Vector3> Esquinas(Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            }
        }

        // No pisa a otro letrero y, en azotea, tampoco a lo que ya hay encima del techo (AC, tinaco...).
        private static bool Choca(Bounds ocupado, Transform edificio, bool enAzotea)
        {
            Bounds holgado = ocupado;
            holgado.Expand(SeparacionLetreros);
            foreach (Bounds otro in colocados)
            {
                if (holgado.Intersects(otro))
                {
                    return true;
                }
            }
            if (enAzotea)
            {
                foreach (Renderer r in edificio.GetComponentsInChildren<Renderer>())
                {
                    if (r.name != "Cuerpo" && !r.transform.parent.name.StartsWith("Lado_") && holgado.Intersects(r.bounds))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- creación

        private static void Crear(string prefab, Vector3 posicion, Quaternion rotacion, bool cerrado, Quaternion? giroModelo = null, float escala = 1f)
        {
            Medida medida = Medir(prefab);
            numero++;
            Transform letrero = CreadorBarrio.Vacio(PrefijoLetrero + numero + "_" + prefab.Replace("Letrero_", ""), grupo, Vector3.zero, Quaternion.identity);
            letrero.SetPositionAndRotation(posicion, rotacion);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(medida.Prefab, letrero);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = giroModelo ?? Quaternion.identity;
            Bounds limites = new Bounds(medida.Limites.center * escala, medida.Limites.size * escala);
            colocados.Add(Caja(posicion, rotacion * (giroModelo ?? Quaternion.identity), limites));
            modelo.transform.localScale = Vector3.one * escala;

            // Los letreros entran en el horneado de las sondas de reflejo (solo se hornea lo marcado ReflectionProbeStatic): los charcos reflejan el neón.
            foreach (Transform parte in letrero.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(parte.gameObject, StaticEditorFlags.ReflectionProbeStatic);
            }

            ParpadeoNeon neon = letrero.gameObject.AddComponent<ParpadeoNeon>();
            SerializedObject so = new SerializedObject(neon);
            so.FindProperty("config").objectReferenceValue = config;
            SerializedProperty lista = so.FindProperty("renderers");
            Renderer[] renderers = letrero.GetComponentsInChildren<Renderer>();
            lista.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            so.FindProperty("semilla").intValue = SemillaParpadeo + numero;
            so.FindProperty("cerrado").boolValue = cerrado;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Uno de cada N letreros parpadea (el primero, PELEAS, y luego cada sexto).
        private static void MarcarParpadeos()
        {
            int cada = Mathf.Max(1, config.UnoDeCada);
            int indice = 0;
            foreach (Transform letrero in grupo)
            {
                if (indice % cada == 0)
                {
                    SerializedObject so = new SerializedObject(letrero.GetComponent<ParpadeoNeon>());
                    so.FindProperty("parpadea").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                indice++;
            }
        }
    }
}
