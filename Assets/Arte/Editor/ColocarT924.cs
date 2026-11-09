using System.Linq;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-924 (playtest 9-nov, sesión B). Repetible y mínimo sobre Ciudad_Prologo:
    ///  1. Charcos más reales: se rehacen las 4 máscaras (KitS2c.TexCharco: lóbulos alargados, contorno deformado, orilla suave, borde mojado
    ///     ancho) y se baja un poco el reflejo de los materiales S2c_Charco_*. No se tocan los postes ni sus luces.
    ///  2. Puertas con neón raras: las entradas de Clandestino, Mercado Negro, Casino y Bar tenían todavía el bloque plano "Letrero" del
    ///     greybox (5 x 1.4 m, material Barrio_Letrero_*, sin texto porque su LetreroCanvas ya estaba apagado): un rectángulo magenta o
    ///     naranja brillando sobre la puerta. Se apagan igual que el de la casa (T-861). Los neones de marco (jambas, laterales, borde del
    ///     toldo) siguen: se veían alineados.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT924.Aplicar
    /// </summary>
    public static class ColocarT924
    {
        public const float IntensidadReflejo = 1f;
        public const float OpacidadCharco = 0.86f;

        [MenuItem("Mecha Cyberpunk/T924/Charcos y bloques de letrero")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            Charcos();
            ColocarS2.Abrir();
            LetrerosPlanos();
            ColocarS2.Guardar("t924-charcos-puertas");
        }

        public static void Charcos()
        {
            for (int i = 0; i < ColocarS2oCharcos.Formas; i++)
            {
                KitS2c.TexCharco(i, true);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/S2c_Charco_" + i + ".mat");
                if (m == null)
                {
                    continue;
                }
                m.SetFloat("_Intensidad", IntensidadReflejo);
                m.SetFloat("_Opacidad", OpacidadCharco);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Charcos: " + ColocarS2oCharcos.Formas + " máscaras nuevas, reflejo " + IntensidadReflejo + ", opacidad " + OpacidadCharco);
        }

        /// <summary>Los bloques planos "Letrero" de las entradas (hijos de un Lugar_*) con material Barrio_Letrero_*.</summary>
        public static Transform[] BloquesPlanos()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Letrero" && t.parent != null && t.parent.name.StartsWith("Lugar_"))
                .Where(t =>
                {
                    Renderer r = t.GetComponent<Renderer>();
                    return r != null && r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("Barrio_Letrero_");
                })
                .ToArray();
        }

        private static void LetrerosPlanos()
        {
            foreach (Transform t in BloquesPlanos().Where(t => t.gameObject.activeSelf))
            {
                ColocarS2.Informe.AppendLine("bloque plano apagado: " + ColocarS2.Ruta(t));
                t.gameObject.SetActive(false);
            }
        }
    }
}
