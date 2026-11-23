using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.IA.APie;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-82: los NPCs humanos caminan a la velocidad de PJ01_Caminar (1.62 m/s) y corren a la de PJ01_Trote (4.53 m/s), como el jugador.
    /// Aplicar() corrige los assets (idempotente); MedirSkulls() se llama en Play y devuelve la velocidad real, el clip activo y el averageSpeed de cada SKULL del inicio.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.VelocidadesNpcsT82.Aplicar
    /// </summary>
    public static class VelocidadesNpcsT82
    {
        public const float Caminar = 1.62f, Trote = 4.53f;

        private static readonly string[] Pandilleros =
        {
            "DatosPandilleroAPie_Base", "DatosPandilleroAPie_Calaveras", "DatosPandilleroAPie_CalaverasE19E20", "DatosPandilleroAPie_EliteNovaE18",
            "DatosPandilleroAPie_JefeCalaveras", "DatosPandilleroAPie_JefeNova", "DatosPandilleroAPie_JefeSnakesE11", "DatosPandilleroAPie_JefeViboras",
            "DatosPandilleroAPie_Nova", "DatosPandilleroAPie_Policia", "DatosPandilleroAPie_Viboras", "DatosPandilleroAPie_ViborasE10",
        };

        [MenuItem("Mecha Cyberpunk/T-82/Velocidades de los NPCs como el jugador")]
        public static string Aplicar()
        {
            var sb = new StringBuilder();
            foreach (string n in Pandilleros)
            {
                Poner("Assets/_Proyecto/IA/Datos/" + n + ".asset", "velocidad", Trote, sb);
            }
            // Guardia pesado humano (E12): camina con PJ01_Caminar (el robot pesado E16, el perro y los jefes robot quedan como estaban).
            Poner("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_GuardiaPesadoE12.asset", "velocidad", Caminar, sb);
            foreach (string n in new[] { "Skulls", "Snakes", "Guardias", "Policia", "Nova" })
            {
                Poner("Assets/_Proyecto/IA/Resources/Confrontacion/Config_" + n + ".asset", "velocidadCaminar", Caminar, sb);
            }
            Poner("Assets/_Proyecto/IA/Datos/DatosAcoso_Base.asset", "velocidadHuida", Trote, sb);
            Poner("Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset", "velocidad", Caminar, sb);
            Poner("Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset", "multiplicadorHuida", 2.8f, sb); // 1.62 * 2.8 = 4.54
            AssetDatabase.SaveAssets();
            string r = sb.ToString();
            Debug.Log("[IA] T-82:\n" + r);
            return r;
        }

        private static void Poner(string ruta, string campo, float valor, StringBuilder sb)
        {
            Object o = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            if (o == null)
            {
                sb.AppendLine("FALTA " + ruta);
                return;
            }
            var so = new SerializedObject(o);
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                sb.AppendLine("SIN CAMPO " + campo + " en " + ruta);
                return;
            }
            float antes = p.floatValue;
            p.floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(o);
            sb.AppendLine(System.IO.Path.GetFileName(ruta) + "." + campo + ": " + antes + " -> " + valor);
        }

        /// <summary>En Play: por cada SKULL del recorrido inicial (SkullRecorrido_N), velocidad real (m/s), clips activos con peso y su averageSpeed.</summary>
        public static string MedirSkulls()
        {
            var sb = new StringBuilder();
            var lista = new List<PandilleroAPie>();
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.name.StartsWith("SkullRecorrido"))
                {
                    lista.Add(p);
                }
            }
            if (lista.Count == 0)
            {
                return "No hay SkullRecorrido_* en la escena (el recorrido no ha empezado o ya terminó).";
            }
            foreach (PandilleroAPie p in lista)
            {
                CharacterController cc = p.GetComponent<CharacterController>() ?? p.GetComponentInChildren<CharacterController>();
                float real = cc != null ? new Vector2(cc.velocity.x, cc.velocity.z).magnitude : -1f;
                Animator an = p.GetComponentInChildren<Animator>();
                sb.Append(p.name).Append(": velocidad real ").Append(real.ToString("0.00")).Append(" m/s");
                if (an == null)
                {
                    sb.AppendLine(" (sin Animator)");
                    continue;
                }
                sb.Append(", Animator.speed ").Append(an.speed.ToString("0.00"));
                string ctrl = an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "(sin controlador)";
                sb.Append(", controlador ").Append(ctrl).Append(", clips: ");
                foreach (AnimatorClipInfo ci in an.GetCurrentAnimatorClipInfo(0))
                {
                    if (ci.weight > 0.05f && ci.clip != null)
                    {
                        sb.Append(ci.clip.name).Append(" (peso ").Append(ci.weight.ToString("0.00")).Append(", averageSpeed ")
                            .Append(ci.clip.averageSpeed.magnitude.ToString("0.00")).Append(") ");
                    }
                }
                sb.AppendLine();
            }
            string r = sb.ToString();
            Debug.Log("[IA] T-82 MedirSkulls:\n" + r);
            return r;
        }
    }
}
