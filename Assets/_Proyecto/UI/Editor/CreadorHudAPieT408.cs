using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-408 en HudAPie.prefab: quita la flecha giratoria y su distancia, agrega el <see cref="MarcadorObjetivo"/>, le pone el estilo del kit,
    /// pasa los colores del config a la paleta y acomoda cada grupo en su ranura de <see cref="DisposicionHud"/> con los tamaños de la guía.
    /// Idempotente: se corre en cada <see cref="Crear"/>.
    /// </summary>
    public static partial class CreadorHudAPie
    {
        private const float AnchoEsquina = 640f;
        private const float AnchoCentro = 1100f;

        private static void AcomodarT408(ConfigHudAPie config, EstiloCyberfix estilo)
        {
            KitUI.FijarEstilo(estilo);
            PasarConfigAPaleta(config, estilo);
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                HudAPie hud = raiz.GetComponent<HudAPie>();
                SerializedObject so = new SerializedObject(hud);
                so.FindProperty("estilo").objectReferenceValue = estilo;
                so.ApplyModifiedPropertiesWithoutUndo();

                Transform objetivo = raiz.transform.Find("Objetivo");
                Quitar(objetivo, "Flecha");
                Quitar(objetivo, "Distancia");
                if (raiz.GetComponentInChildren<MarcadorObjetivo>(true) == null)
                {
                    GameObject marcador = new GameObject("Marcador", typeof(RectTransform));
                    marcador.transform.SetParent(raiz.transform, false);
                    KitUI.Estirar((RectTransform)marcador.transform);
                    marcador.AddComponent<MarcadorObjetivo>();
                }

                Ubicar(raiz, "Cancion", DisposicionHud.Cancion, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "Renta", DisposicionHud.Renta, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "Calor", DisposicionHud.Calor, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "Objetivo", DisposicionHud.Objetivo, AnchoCentro, RolTexto.Texto);
                Ubicar(raiz, "Vida", DisposicionHud.Vida, config.AnchoBarra, RolTexto.Hud);
                Ubicar(raiz, "Dinero", DisposicionHud.Dinero, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "AvisoDinero", DisposicionHud.AvisoDinero, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "Ayuda", DisposicionHud.AyudaControles, DisposicionHud.AnchoAyudaControles, RolTexto.Ayuda);
                Ubicar(raiz, "Arma", DisposicionHud.Arma, AnchoEsquina, RolTexto.Hud);
                Ubicar(raiz, "Interaccion", DisposicionHud.Interaccion, DisposicionHud.AnchoCentroAbajo, RolTexto.Texto);
                Ubicar(raiz, "Llamado", DisposicionHud.Llamado, DisposicionHud.AnchoCentroAbajo, RolTexto.Texto);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Debug.Log("[UI] T-408: HudAPie sin flecha giratoria, con MarcadorObjetivo y acomodado por DisposicionHud.");
        }

        private static void Quitar(Transform padre, string nombre)
        {
            Transform hijo = padre != null ? padre.Find(nombre) : null;
            if (hijo != null)
            {
                Object.DestroyImmediate(hijo.gameObject);
            }
        }

        private static void Ubicar(GameObject raiz, string nombre, RanuraHud ranura, float ancho, RolTexto rol)
        {
            Transform grupo = raiz.transform.Find(nombre);
            if (grupo == null)
            {
                Debug.LogWarning("[UI] T-408: HudAPie no tiene el grupo " + nombre);
                return;
            }
            KitUI.Colocar((RectTransform)grupo, ranura, ancho);
            foreach (Text texto in grupo.GetComponentsInChildren<Text>(true))
            {
                KitUI.AplicarEstilo(texto, rol);
            }
        }

        // Colores del HUD a pie con la paleta de la guía (D-tony-239).
        private static void PasarConfigAPaleta(ConfigHudAPie config, EstiloCyberfix estilo)
        {
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("colorVida").colorValue = estilo.Turquesa;
            so.FindProperty("colorVidaBaja").colorValue = estilo.Magenta;
            so.FindProperty("colorTexto").colorValue = estilo.Turquesa;
            so.FindProperty("colorAviso").colorValue = estilo.Naranja;
            so.FindProperty("colorRegresa").colorValue = estilo.Magenta;
            so.FindProperty("colorCalor").colorValue = estilo.Naranja;
            so.FindProperty("colorDeuda").colorValue = estilo.Magenta;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
