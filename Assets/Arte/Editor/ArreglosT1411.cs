using System;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1411: deja los assets al día con las pruebas del EditMode del 19-nov (ver docs/tareas/hechas/T-1411-pruebas-look.md). Se puede repetir.
    /// 1) Cerro: ninguna luz pasa de 18 m (tope de rendimiento de T-1365; T-1392 le había puesto 22 m al sodio).
    /// 2) Patrulla: ruedas al hueco otra vez (MedirRuedasT1007.Ajustar; la camioneta de T-1389 dejó una llanta al 79,9 %).
    /// 3) Variantes de color de V02, V03 y V04: se rehacen sobre el CarroTrafico rearmado en T-1397.
    /// 4) Personajes nuevos de T-1389b: clips de PJ01 (override de su papel) y se vuelven a armar peatones, Nova y SKULLS.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ArreglosT1411.Aplicar
    /// </summary>
    public static class ArreglosT1411
    {
        [MenuItem("Mecha Cyberpunk/T1411/Aplicar arreglos de pruebas")]
        public static void Aplicar()
        {
            Paso("Cerro: luces a 18 m", LucesDelCerro);
            Paso("Patrulla: ruedas al hueco", MedirRuedasT1007.Ajustar);
            Paso("Variantes de color de los carros", ColocarS2nCarros.RehacerVariantes);
            Paso("Clips de PJ01 en los personajes nuevos", PersonajesNuevosT1389b.ConectarTodosClipsPJ01);
            Paso("Peatones nuevos", PersonajesNuevosT1389b.ConectarPeatones);
            Paso("Enemigos de Nova", PersonajesNuevosT1389b.ConectarNova);
            Paso("Enemigos de SKULLS", PersonajesNuevosT1389b.ConectarSkulls);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-1411 aplicado.");
        }

        private static void Paso(string nombre, Action accion)
        {
            try
            {
                accion();
                Debug.Log("[Arte] T-1411 " + nombre + ": listo.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Arte] T-1411 " + nombre + ": " + e);
            }
        }

        /// <summary>Baja a <see cref="RendimientoCerroT1365.AlcanceMaximoLuz"/> toda luz no direccional del prefab del Cerro que lo pase.</summary>
        public static void LucesDelCerro()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RendimientoCerroT1365.RutaPrefab);
            try
            {
                int bajadas = 0;
                foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
                {
                    if (l.type != LightType.Directional && l.range > RendimientoCerroT1365.AlcanceMaximoLuz)
                    {
                        l.range = RendimientoCerroT1365.AlcanceMaximoLuz;
                        bajadas++;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RendimientoCerroT1365.RutaPrefab);
                Debug.Log("[Arte] T-1411: " + bajadas + " luces del Cerro bajadas a " + RendimientoCerroT1365.AlcanceMaximoLuz + " m.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
