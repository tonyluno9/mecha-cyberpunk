using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// Arma (o rearma) el Animator Controller del visual de un personaje con todos los estados y parámetros de <see cref="ReglasVisual"/> (T-473).
    /// Recibe los clips ya cargados; un clip que falta deja el estado sin movimiento y se avisa.
    /// </summary>
    public static class ConstructorControladorVisual
    {
        private const float Mezcla = 0.1f;
        private const float SalidaUnaVez = 0.9f;

        /// <summary>Crea el controlador en la ruta o lo vacía y lo rearma si ya existe (conserva su GUID). Devuelve la lista de clips que faltaron.</summary>
        public static AnimatorController Construir(string ruta, IDictionary<ClipVisual, AnimationClip> clips, out List<ClipVisual> faltan)
        {
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            if (c == null)
            {
                c = AnimatorController.CreateAnimatorControllerAtPath(ruta);
            }
            Vaciar(c);

            foreach (string p in ReglasVisual.ParametrosFloat) { c.AddParameter(p, AnimatorControllerParameterType.Float); }
            foreach (string p in ReglasVisual.ParametrosBool) { c.AddParameter(p, AnimatorControllerParameterType.Bool); }
            foreach (string p in ReglasVisual.ParametrosInt) { c.AddParameter(p, AnimatorControllerParameterType.Int); }
            foreach (string p in ReglasVisual.ParametrosTrigger) { c.AddParameter(p, AnimatorControllerParameterType.Trigger); }
            c.parameters = ConEnSueloVerdadero(c.parameters);

            AnimatorStateMachine m = c.layers[0].stateMachine;
            faltan = new List<ClipVisual>();
            var e = new Dictionary<ClipVisual, AnimatorState>();
            foreach (ClipVisual clip in System.Enum.GetValues(typeof(ClipVisual)))
            {
                AnimatorState estado = m.AddState(clip.ToString());
                if (clips != null && clips.TryGetValue(clip, out AnimationClip movimiento) && movimiento != null)
                {
                    estado.motion = movimiento;
                }
                else
                {
                    faltan.Add(clip);
                }
                e[clip] = estado;
            }
            m.defaultState = e[ClipVisual.Idle];

            AnimatorState idle = e[ClipVisual.Idle];
            AnimatorState caminar = e[ClipVisual.Caminar];
            AnimatorState correr = e[ClipVisual.Correr];
            AnimatorState[] suelo = { idle, caminar, correr };

            // Locomoción por Velocidad.
            Unir(idle, caminar, false, 0f, Mayor(ReglasVisual.Velocidad, ReglasVisual.UmbralCaminar));
            Unir(caminar, idle, false, 0f, Menor(ReglasVisual.Velocidad, ReglasVisual.UmbralCaminar));
            Unir(caminar, correr, false, 0f, Mayor(ReglasVisual.Velocidad, ReglasVisual.UmbralCorrer));
            Unir(correr, caminar, false, 0f, Menor(ReglasVisual.Velocidad, ReglasVisual.UmbralCorrer));

            // Saltar: sale del suelo y vuelve al tocarlo.
            foreach (AnimatorState s in suelo)
            {
                Unir(s, e[ClipVisual.Saltar], false, 0f, Bool(ReglasVisual.EnSuelo, false));
            }
            Unir(e[ClipVisual.Saltar], idle, false, 0f, Bool(ReglasVisual.EnSuelo, true));

            // Apuntar con pistola o arma larga.
            foreach (AnimatorState s in suelo)
            {
                Unir(s, e[ClipVisual.ApuntarPistola], false, 0f, Bool(ReglasVisual.Apuntando, true), Entero(ReglasVisual.TipoArma, (int)TipoArmaVisual.Pistola));
                Unir(s, e[ClipVisual.ApuntarLarga], false, 0f, Bool(ReglasVisual.Apuntando, true), Entero(ReglasVisual.TipoArma, (int)TipoArmaVisual.ArmaLarga));
            }
            AnimatorState[] apuntar = { e[ClipVisual.ApuntarPistola], e[ClipVisual.ApuntarLarga] };
            TipoArmaVisual[] tipos = { TipoArmaVisual.Pistola, TipoArmaVisual.ArmaLarga };
            ClipVisual[] disparos = { ClipVisual.DispararPistola, ClipVisual.DispararLarga };
            for (int i = 0; i < 2; i++)
            {
                Unir(apuntar[i], idle, false, 0f, Bool(ReglasVisual.Apuntando, false));
                Unir(apuntar[i], idle, false, 0f, new AnimatorCondition { parameter = ReglasVisual.TipoArma, mode = AnimatorConditionMode.NotEqual, threshold = (int)tipos[i] });
                // Disparar desde cualquier estado con ese arma en la mano (reinicia si se dispara seguido).
                AnimatorStateTransition d = m.AddAnyStateTransition(e[disparos[i]]);
                Configurar(d, false, 0f, true, Disparador(ReglasVisual.Disparar), Entero(ReglasVisual.TipoArma, (int)tipos[i]), Bool(ReglasVisual.Caer, false));
                // Al terminar: vuelve a apuntar si sigue apuntando, o a Idle.
                Unir(e[disparos[i]], apuntar[i], true, SalidaUnaVez, Bool(ReglasVisual.Apuntando, true));
                Unir(e[disparos[i]], idle, true, SalidaUnaVez, Bool(ReglasVisual.Apuntando, false));
            }

            // Acciones de una vez, desde cualquier estado.
            DeCualquiera(m, e[ClipVisual.Golpe], idle, ReglasVisual.Golpe);
            DeCualquiera(m, e[ClipVisual.RecibirGolpe], idle, ReglasVisual.Herido);
            DeCualquiera(m, e[ClipVisual.Rodar], idle, ReglasVisual.Rodar);
            DeCualquiera(m, e[ClipVisual.Recargar], idle, ReglasVisual.Recargar);

            // Estados que duran mientras un bool está activo.
            AnimatorStateTransition aCaer = m.AddAnyStateTransition(e[ClipVisual.Caer]);
            Configurar(aCaer, false, 0f, false, Bool(ReglasVisual.Caer, true));
            Unir(e[ClipVisual.Caer], idle, false, 0f, Bool(ReglasVisual.Caer, false));
            AnimatorStateTransition aCubrir = m.AddAnyStateTransition(e[ClipVisual.Cubrirse]);
            Configurar(aCubrir, false, 0f, false, Bool(ReglasVisual.Cubierto, true), Bool(ReglasVisual.Caer, false));
            Unir(e[ClipVisual.Cubrirse], idle, false, 0f, Bool(ReglasVisual.Cubierto, false));

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        private static AnimatorControllerParameter[] ConEnSueloVerdadero(AnimatorControllerParameter[] lista)
        {
            foreach (AnimatorControllerParameter p in lista)
            {
                if (p.name == ReglasVisual.EnSuelo)
                {
                    p.defaultBool = true;
                }
            }
            return lista;
        }

        private static void Vaciar(AnimatorController c)
        {
            while (c.parameters.Length > 0)
            {
                c.RemoveParameter(0);
            }
            while (c.layers.Length > 1)
            {
                c.RemoveLayer(c.layers.Length - 1);
            }
            AnimatorStateMachine m = c.layers[0].stateMachine;
            foreach (ChildAnimatorState s in m.states) { m.RemoveState(s.state); }
            foreach (AnimatorStateTransition t in m.anyStateTransitions) { m.RemoveAnyStateTransition(t); }
        }

        private static void DeCualquiera(AnimatorStateMachine m, AnimatorState estado, AnimatorState vuelta, string disparador)
        {
            AnimatorStateTransition t = m.AddAnyStateTransition(estado);
            Configurar(t, false, 0f, false, Disparador(disparador), Bool(ReglasVisual.Caer, false));
            Unir(estado, vuelta, true, SalidaUnaVez);
        }

        private static void Unir(AnimatorState desde, AnimatorState hacia, bool salida, float tiempo, params AnimatorCondition[] condiciones)
        {
            AnimatorStateTransition t = desde.AddTransition(hacia);
            Configurar(t, salida, tiempo, false, condiciones);
        }

        private static void Configurar(AnimatorStateTransition t, bool salida, float tiempo, bool sobreSiMismo, params AnimatorCondition[] condiciones)
        {
            t.hasExitTime = salida;
            t.exitTime = tiempo;
            t.duration = Mezcla;
            t.canTransitionToSelf = sobreSiMismo;
            foreach (AnimatorCondition cond in condiciones)
            {
                t.AddCondition(cond.mode, cond.threshold, cond.parameter);
            }
        }

        private static AnimatorCondition Mayor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Greater, threshold = v };
        private static AnimatorCondition Menor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Less, threshold = v };
        private static AnimatorCondition Bool(string p, bool v) => new AnimatorCondition { parameter = p, mode = v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot };
        private static AnimatorCondition Entero(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Equals, threshold = v };
        private static AnimatorCondition Disparador(string p) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.If };
    }
}
