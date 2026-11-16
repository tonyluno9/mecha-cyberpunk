using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1004: todos los humanoides NPC (pandilleros, peatones, policía, guardias de Nova, meseras, mecánico...) caminan, corren, saltan, golpean y disparan
    /// con los clips de PJ01. Los que van por override (ControladorBase) los toma ConstructorControladorBase.ClipsPorPapel; los tres modelos con controlador propio
    /// (Policia, Mecanico, MeseraRobot, también usado por las meseras de la ciudad) conservan su máquina de estados y sus parámetros, y solo cambian los clips de cada estado.
    /// Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.NpcClipsPJ01T1004.Aplicar
    /// </summary>
    public static class NpcClipsPJ01T1004
    {
        public const string CarpetaPersonajes = "Assets/Arte/Personajes/";

        /// <summary>Controladores propios de modelo que no son override del ControladorBase.</summary>
        public static readonly string[] ControladoresPropios = { "Policia/Policia", "Mecanico/Mecanico", "MeseraRobot/MeseraRobot" };

        /// <summary>Prefabs Visual_ que son del jugador y no se tocan.</summary>
        public static readonly string[] VisualesDelJugador = { "Visual_woman", "Visual_PJ01", "Visual_Prueba" };

        /// <summary>Estado de los controladores propios -> clip PJ01.</summary>
        public static readonly (string Estado, string Clip)[] ClipsDeEstado =
        {
            ("Idle", "PJ01_Loco_Idle"), ("Caminar", "PJ01_Caminar_SinImplante"), ("Correr", "PJ01_Correr_SinImplante"),
            ("Saltar", "PJ01_Loco_SaltoAire"), ("Golpe", "PJ01_Punos_GolpeDer"), ("RecibirGolpe", "PJ01_Pistola_Dano_Frente"),
            ("Caer", "PJ01_Muerte_Adelante"), ("ApuntarPistola", "PJ01_Pistola_Apuntar"), ("DispararPistola", "PJ01_Pistola_Disparo"),
            ("ApuntarLarga", "PJ01_Rifle_Apuntar"), ("DispararLarga", "PJ01_Rifle_DisparoCaminar"), ("Recargar", "PJ01_Pistola_Recargar"),
            ("Cubrirse", "PJ01_Cobertura_Entrar"), ("Rodar", "PJ01_Loco_Rodar"),
        };

        [MenuItem("Mecha Cyberpunk/Personajes/T-1004 NPC con clips de PJ01")]
        public static void Aplicar()
        {
            ConstructorControladorBase.Construir();
            foreach (string ruta in ControladoresPropios)
            {
                CambiarClips(CarpetaPersonajes + ruta + ".controller");
            }
            AjustarVelocidadesDeVisuales();
            AjustarAlturaPolicia();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1004: los NPC humanoides usan los clips de PJ01.");
        }

        private static void CambiarClips(string rutaControlador)
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaControlador);
            if (controlador == null)
            {
                Debug.LogError("[Personaje] T-1004: falta " + rutaControlador);
                return;
            }
            foreach (AnimatorControllerLayer capa in controlador.layers)
            {
                foreach (ChildAnimatorState hijo in capa.stateMachine.states)
                {
                    foreach ((string estado, string clip) in ClipsDeEstado)
                    {
                        if (hijo.state.name == estado)
                        {
                            hijo.state.motion = ConstructorControladorBase.Clip(clip);
                        }
                    }
                }
            }
            EditorUtility.SetDirty(controlador);
        }

        /// <summary>El policía (escala de T-729 medida con los clips de Mixamo: 1.80 m) mide 1.73 m animado con los de PJ01 (idle más agachado); se sube la escala 1.80 / 1.7307.</summary>
        public const float EscalaPoliciaConClipsPJ01 = 1.5235f;

        public static void AjustarAlturaPolicia()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(EscalarPoliciaRuta);
            try
            {
                Transform modelo = raiz.transform.Find("Giro/Modelo");
                modelo.localScale = Vector3.one * EscalaPoliciaConClipsPJ01;
                PrefabUtility.SaveAsPrefabAsset(raiz, EscalarPoliciaRuta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private const string EscalarPoliciaRuta = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Policia.prefab";

        /// <summary>Los clips de caminar y correr de PJ01 avanzan a 1.293 y 3.19 m/s: el visual escala la rapidez del Animator contra esa medida.</summary>
        private static void AjustarVelocidadesDeVisuales()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Visual_", new[] { "Assets/_Proyecto/Personaje/Visual/Prefabs" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (System.Array.IndexOf(VisualesDelJugador, System.IO.Path.GetFileNameWithoutExtension(ruta)) >= 0)
                {
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    VisualPersonaje visual = contenido.GetComponent<VisualPersonaje>();
                    if (visual == null)
                    {
                        continue;
                    }
                    visual.ConfigurarClips(ReglasAnimacion.VelocidadClipCaminarSinImplante, ReglasAnimacion.VelocidadClipCorrerSinImplante, ReglasAnimacion.RapidezMaximaLocomocion);
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }
    }
}
