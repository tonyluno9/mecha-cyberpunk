using System.Collections.Generic;
using MechaCyberpunk.Economia;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Crea las 5 piezas de serie y las 80 de rama (T-166, valores provisionales de D-tony-120), el CatalogoPiezas_Base,
    /// y las agrega a CatalogoObjetos_Base. Idempotente: no pisa valores de un asset que ya existe y no repite entradas.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorPiezas.Crear
    /// </summary>
    public static class CreadorPiezas
    {
        private const string Carpeta = "Assets/_Proyecto/Mecha/Datos/Piezas";
        private const string RutaCatalogoPiezas = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaCatalogoObjetos = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";
        private const int ValorVentaDeSerie = 100;
        private const int MaximoEnInventario = 2;

        private static readonly int[] PreciosPorNivel = { 500, 800, 1200, 1800 };
        private static readonly string[] Romanos = { "I", "II", "III", "IV" };
        private static readonly RamaPieza[] Ramas = { RamaPieza.Militar, RamaPieza.Tech, RamaPieza.CuerpoACuerpo, RamaPieza.Fuego };
        private static readonly string[] NombresRama = { "MILITAR", "TECH", "CUERPO A CUERPO", "FUEGO" };
        private static readonly string[] IdsRama = { "militar", "tech", "cuerpoacuerpo", "fuego" };
        private static readonly string[] IdsRanura = { "brazoizq", "brazoder", "torso", "piernas", "cabina" };
        private static readonly string[] NombresRanura = { "BRAZO IZQ.", "BRAZO DER.", "TORSO", "PIERNAS", "CABINA" };

        // Bonos POR NIVEL por rama (orden: militar, tech, cuerpo a cuerpo, fuego).
        private static readonly float[] VidaTorso = { 14f, 22f, 30f, 12f };
        private static readonly float[] VidaPiernas = { 4f, 6f, 10f, 3f };
        private static readonly float[] VelocidadPiernas = { 0.08f, 0.15f, 0.25f, 0.12f };
        private static readonly float[] DanioBrazo = { 3f, 2f, 1f, 2.5f };
        private static readonly float[] VidaBrazo = { 2f, 4f, 5f, 2f };
        private static readonly float[] RecargaBrazo = { 0f, 0f, 0f, -0.03f };
        private static readonly float[] VidaCabina = { 4f, 6f, 8f, 3f };

        [MenuItem("Mecha Cyberpunk/Crear T-166 piezas")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto/Mecha/Datos", "Piezas");
            var todas = new List<DatosPieza>();
            for (int r = 0; r < 5; r++)
            {
                var ranura = (RanuraPieza)r;
                todas.Add(Pieza($"{Carpeta}/DatosPieza_{IdsRanura[r]}_serie.asset", $"pieza_{IdsRanura[r]}_serie_0",
                    $"{NombresRanura[r]} DE SERIE", ranura, RamaPieza.DeSerie, 0, 0, ValorVentaDeSerie, 0, 0, 0, 0));
                for (int m = 0; m < 4; m++)
                {
                    for (int n = 1; n <= 4; n++)
                    {
                        int precio = PreciosPorNivel[n - 1];
                        float vida = 0f, danio = 0f, vel = 0f, recarga = 0f;
                        switch (ranura)
                        {
                            case RanuraPieza.Torso: vida = VidaTorso[m] * n; break;
                            case RanuraPieza.Piernas: vida = VidaPiernas[m] * n; vel = VelocidadPiernas[m] * n; break;
                            case RanuraPieza.Cabina: vida = VidaCabina[m] * n; break;
                            default: danio = DanioBrazo[m] * n; vida = VidaBrazo[m] * n; recarga = RecargaBrazo[m] * n; break;
                        }
                        string id = $"pieza_{IdsRanura[r]}_{IdsRama[m]}_{n}";
                        todas.Add(Pieza($"{Carpeta}/DatosPieza_{IdsRanura[r]}_{IdsRama[m]}_{n}.asset", id,
                            $"{NombresRanura[r]} {NombresRama[m]} {Romanos[n - 1]}", ranura, Ramas[m], n, precio,
                            precio / 2, vida, danio, vel, recarga));
                    }
                }
            }

            var catalogoPiezas = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogoPiezas);
            if (catalogoPiezas == null)
            {
                catalogoPiezas = ScriptableObject.CreateInstance<CatalogoPiezas>();
                AssetDatabase.CreateAsset(catalogoPiezas, RutaCatalogoPiezas);
            }
            AgregarFaltantes(catalogoPiezas, "piezas", todas);

            var catalogoObjetos = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(RutaCatalogoObjetos);
            if (catalogoObjetos == null)
            {
                Debug.LogError("[Mecha] Falta CatalogoObjetos_Base: corre CreadorInventario primero.");
            }
            else
            {
                AgregarFaltantes(catalogoObjetos, "objetos", todas);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Mecha] T-166 piezas creadas: {todas.Count}.");
        }

        private static void AgregarFaltantes(Object catalogo, string campo, List<DatosPieza> piezas)
        {
            var so = new SerializedObject(catalogo);
            SerializedProperty lista = so.FindProperty(campo);
            var presentes = new HashSet<Object>();
            for (int i = 0; i < lista.arraySize; i++) presentes.Add(lista.GetArrayElementAtIndex(i).objectReferenceValue);
            bool cambio = false;
            foreach (DatosPieza p in piezas)
            {
                if (presentes.Contains(p)) continue;
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = p;
                cambio = true;
            }
            if (!cambio) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogo);
        }

        private static DatosPieza Pieza(string ruta, string id, string nombre, RanuraPieza ranura, RamaPieza rama, int nivel,
            int precio, int venta, float vida, float danio, float vel, float recarga)
        {
            var p = AssetDatabase.LoadAssetAtPath<DatosPieza>(ruta);
            if (p != null) return p; // ya existe: no se pisan los valores ajustados en el inspector (D-tony-109)
            p = ScriptableObject.CreateInstance<DatosPieza>();
            AssetDatabase.CreateAsset(p, ruta);
            var so = new SerializedObject(p);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Pieza;
            so.FindProperty("valorVenta").intValue = venta;
            so.FindProperty("maximo").intValue = MaximoEnInventario;
            so.FindProperty("ranura").enumValueIndex = (int)ranura;
            so.FindProperty("rama").enumValueIndex = (int)rama;
            so.FindProperty("nivel").intValue = nivel;
            so.FindProperty("precio").intValue = precio;
            so.FindProperty("bonoVida").floatValue = vida;
            so.FindProperty("bonoDanio").floatValue = danio;
            so.FindProperty("bonoVelocidad").floatValue = vel;
            so.FindProperty("bonoRecarga").floatValue = recarga;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(p);
            return p;
        }
    }
}
