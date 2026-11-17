using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-403 y T-416: crea los 20 DatosArma (5 armas por 4 niveles, números de D-tony-214 y D-tony-253), el CatalogoArmas_Base, las 19 ofertas del Mercado
    /// (4 armas y 15 mejoras), y las agrega al CatalogoObjetos_Base y a DatosTienda_MercadoNegro. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorArmas.Crear
    /// </summary>
    public static class CreadorArmas
    {
        public const string CarpetaArmas = "Assets/_Proyecto/Personaje/Datos/Armas";
        public const string RutaCatalogoArmas = "Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset";
        private const string RutaCatalogoObjetos = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";
        private const string RutaTienda = "Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset";
        private const string CarpetaOfertas = "Assets/_Proyecto/Economia/Datos";

        private class Familia
        {
            public ArmaAPie arma;
            public string clave;
            public string nombre;
            public float[] danio;
            public int perdigones = 1;
            public int[] cargador;
            public float[] recarga;
            public float entre;
            public bool automatica;
            public float dispersion;
            public float dispersionApuntando;
            public float alcance;
            public int[] precio;
            // T-1013: armas especiales.
            public TipoDisparoArma tipo = TipoDisparoArma.Bala;
            public float[] radio;
            public float velocidad = 22f;
            public float carga;
            public float rayo;
        }

        private static readonly Familia[] Familias =
        {
            new Familia { arma = ArmaAPie.Punos, clave = "punos", nombre = "Puños", danio = new[] { 20f, 26f, 32f, 40f },
                cargador = new[] { 1, 1, 1, 1 }, recarga = new[] { 0.45f, 0.4f, 0.35f, 0.3f }, entre = 0.45f, alcance = 2f,
                precio = new[] { 0, 100, 200, 350 } },
            new Familia { arma = ArmaAPie.Pistola, clave = "pistola", nombre = "Pistola", danio = new[] { 25f, 30f, 36f, 44f },
                cargador = new[] { 8, 10, 12, 15 }, recarga = new[] { 1.4f, 1.2f, 1.0f, 0.8f }, entre = 0.25f, alcance = 60f,
                precio = new[] { 150, 150, 300, 500 } },
            new Familia { arma = ArmaAPie.Escopeta, clave = "escopeta", nombre = "Escopeta", danio = new[] { 9f, 11f, 13f, 16f }, perdigones = 8,
                cargador = new[] { 4, 5, 6, 8 }, recarga = new[] { 2.4f, 2.0f, 1.7f, 1.4f }, entre = 0.9f, alcance = 20f,
                dispersion = 6f, dispersionApuntando = 4f, precio = new[] { 350, 250, 450, 700 } },
            new Familia { arma = ArmaAPie.Fusil, clave = "fusil", nombre = "Fusil", danio = new[] { 15f, 18f, 21f, 25f },
                cargador = new[] { 30, 30, 35, 40 }, recarga = new[] { 2.0f, 1.8f, 1.6f, 1.3f }, entre = 0.11f, automatica = true, alcance = 70f,
                dispersion = 2.5f, dispersionApuntando = 1.2f, precio = new[] { 700, 350, 600, 850 } },
            new Familia { arma = ArmaAPie.Sniper, clave = "sniper", nombre = "Sniper", danio = new[] { 70f, 85f, 100f, 120f },
                cargador = new[] { 4, 5, 6, 7 }, recarga = new[] { 2.8f, 2.5f, 2.2f, 1.9f }, entre = 1.2f, alcance = 150f,
                dispersion = 8f, dispersionApuntando = 0f, precio = new[] { 2200, 400, 650, 900 } },
            // T-1013: las armas nuevas (imageassets/modelos3d/armas_nuevas).
            new Familia { arma = ArmaAPie.Subfusil, clave = "subfusil", nombre = "Subfusil", danio = new[] { 11f, 13f, 15f, 18f },
                cargador = new[] { 25, 30, 35, 40 }, recarga = new[] { 1.6f, 1.4f, 1.25f, 1.1f }, entre = 0.07f, automatica = true, alcance = 45f,
                dispersion = 4f, dispersionApuntando = 2.5f, precio = new[] { 500, 300, 500, 700 } },
            new Familia { arma = ArmaAPie.Revolver, clave = "revolver", nombre = "Revólver", danio = new[] { 55f, 65f, 78f, 95f },
                cargador = new[] { 6, 6, 6, 6 }, recarga = new[] { 2.2f, 2.0f, 1.7f, 1.5f }, entre = 0.55f, alcance = 70f,
                dispersion = 1.5f, dispersionApuntando = 0.3f, precio = new[] { 400, 300, 500, 700 } },
            new Familia { arma = ArmaAPie.Lanzagranadas, clave = "lanzagranadas", nombre = "Lanzagranadas", danio = new[] { 120f, 140f, 165f, 200f },
                cargador = new[] { 6, 6, 6, 6 }, recarga = new[] { 3.0f, 2.7f, 2.3f, 2.0f }, entre = 0.8f, alcance = 60f,
                precio = new[] { 1500, 600, 900, 1300 }, tipo = TipoDisparoArma.Proyectil, radio = new[] { 4.5f, 5f, 5.5f, 6f }, velocidad = 28f },
            new Familia { arma = ArmaAPie.Granada, clave = "granada", nombre = "Granada", danio = new[] { 150f, 170f, 195f, 230f },
                cargador = new[] { 1, 1, 1, 1 }, recarga = new[] { 0.6f, 0.55f, 0.5f, 0.45f }, entre = 0.8f, alcance = 40f,
                precio = new[] { 300, 300, 500, 700 }, tipo = TipoDisparoArma.Lanzada, radio = new[] { 5f, 5.5f, 6f, 6.5f }, velocidad = 16f },
            new Familia { arma = ArmaAPie.RiflePlasma, clave = "rifleplasma", nombre = "Rifle de plasma", danio = new[] { 400f, 470f, 550f, 650f },
                cargador = new[] { 3, 3, 4, 5 }, recarga = new[] { 3.0f, 2.7f, 2.3f, 2.0f }, entre = 0.5f, alcance = 80f,
                precio = new[] { 2500, 800, 1200, 1600 }, tipo = TipoDisparoArma.Rayo, carga = 3f, rayo = 5f }
        };

        private static readonly string[] Numerales = { string.Empty, " I", " II", " III" };

        [MenuItem("Mecha Cyberpunk/Crear T-403 armas")]
        public static void Crear()
        {
            CrearDatos();
            CreadorJugadorAPie.RegenerarPrefabTerceraPersona();
            Debug.Log("[Personaje] T-403/T-416: 5 armas, catálogo y El Mercado creados.");
        }

        public static void CrearDatos()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaArmas))
            {
                Directory.CreateDirectory(CarpetaArmas);
                AssetDatabase.Refresh();
            }
            var armas = new List<DatosArma>();
            var bases = new List<DatosOferta>();
            var mejoras = new List<DatosOferta>();
            foreach (Familia f in Familias)
            {
                for (int nivel = 0; nivel < 4; nivel++)
                {
                    DatosOferta oferta = f.precio[nivel] > 0 ? CrearOferta(f, nivel) : null;
                    if (oferta != null)
                    {
                        (nivel == 0 ? bases : mejoras).Add(oferta);
                    }
                    armas.Add(CrearArma(f, nivel, oferta));
                }
            }
            var ofertas = new List<DatosOferta>(bases);
            ofertas.AddRange(mejoras);

            var catalogo = CargarOCrear<CatalogoArmas>(RutaCatalogoArmas);
            AsignarLista(catalogo, "armas", armas);
            ReemplazarArmas(AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaCatalogoObjetos), "objetos", armas);
            ReemplazarArmas(AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaTienda), "ofertas", ofertas);
            AssetDatabase.SaveAssets();
        }

        // Quita los vacíos (assets borrados) y las armas u ofertas de armas que ya estaban, y agrega las nuevas en su orden.
        private static void ReemplazarArmas<T>(ScriptableObject destino, string campo, List<T> nuevos) where T : Object
        {
            if (destino == null)
            {
                Debug.LogError("[Personaje] CreadorArmas: falta el asset que recibe '" + campo + "'.");
                return;
            }
            var so = new SerializedObject(destino);
            SerializedProperty lista = so.FindProperty(campo);
            for (int i = lista.arraySize - 1; i >= 0; i--)
            {
                Object elemento = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                bool esArma = elemento is DatosArma || (elemento is DatosOferta && elemento.name.StartsWith("DatosOferta_Arma_"));
                if (elemento == null || esArma)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    lista.DeleteArrayElementAtIndex(i);
                }
            }
            foreach (T elemento in nuevos)
            {
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = elemento;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
        }

        private static string NombreNivel(Familia f, int nivel) => f.nombre + Numerales[nivel];

        private static DatosOferta CrearOferta(Familia f, int nivel)
        {
            var oferta = CargarOCrear<DatosOferta>(CarpetaOfertas + "/DatosOferta_Arma_" + f.clave + "_" + nivel + ".asset");
            var so = new SerializedObject(oferta);
            so.FindProperty("id").stringValue = "oferta_arma_" + f.clave + "_" + nivel;
            so.FindProperty("nombre").stringValue = NombreNivel(f, nivel);
            so.FindProperty("descripcion").stringValue = nivel == 0 ? "Arma nueva." : "Mejora: más daño, más cargador, recarga rápida.";
            so.FindProperty("precio").intValue = f.precio[nivel];
            so.FindProperty("tipo").enumValueIndex = (int)TipoOferta.Pieza;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(oferta);
            return oferta;
        }

        private static DatosArma CrearArma(Familia f, int nivel, DatosOferta oferta)
        {
            var arma = CargarOCrear<DatosArma>(CarpetaArmas + "/DatosArma_" + f.clave + "_" + nivel + ".asset");
            var so = new SerializedObject(arma);
            so.FindProperty("id").stringValue = "arma_" + f.clave + "_" + nivel;
            so.FindProperty("nombre").stringValue = NombreNivel(f, nivel);
            so.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Arma;
            so.FindProperty("valorVenta").intValue = 0;
            so.FindProperty("maximo").intValue = 1;
            so.FindProperty("oferta").objectReferenceValue = oferta;
            so.FindProperty("arma").enumValueIndex = (int)f.arma;
            so.FindProperty("nivel").intValue = nivel;
            so.FindProperty("precio").intValue = f.precio[nivel];
            so.FindProperty("danio").floatValue = f.danio[nivel];
            so.FindProperty("perdigones").intValue = f.perdigones;
            so.FindProperty("cargador").intValue = f.cargador[nivel];
            so.FindProperty("recarga").floatValue = f.recarga[nivel];
            so.FindProperty("tiempoEntreDisparos").floatValue = f.entre;
            so.FindProperty("automatica").boolValue = f.automatica;
            so.FindProperty("dispersion").floatValue = f.dispersion;
            so.FindProperty("dispersionApuntando").floatValue = f.dispersionApuntando;
            so.FindProperty("alcance").floatValue = f.alcance;
            so.FindProperty("tipoDisparo").enumValueIndex = (int)f.tipo;
            so.FindProperty("radioExplosion").floatValue = f.radio != null ? f.radio[nivel] : 0f;
            so.FindProperty("velocidadProyectil").floatValue = f.velocidad;
            so.FindProperty("segundosCarga").floatValue = f.carga;
            so.FindProperty("segundosRayo").floatValue = f.rayo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arma);
            return arma;
        }

        private static T CargarOCrear<T>(string ruta) where T : ScriptableObject
        {
            T existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null)
            {
                return existente;
            }
            T nuevo = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(nuevo, ruta);
            return nuevo;
        }

        private static void AsignarLista<T>(ScriptableObject destino, string campo, List<T> elementos) where T : Object
        {
            var so = new SerializedObject(destino);
            SerializedProperty lista = so.FindProperty(campo);
            lista.arraySize = elementos.Count;
            for (int i = 0; i < elementos.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = elementos[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
        }
    }
}
