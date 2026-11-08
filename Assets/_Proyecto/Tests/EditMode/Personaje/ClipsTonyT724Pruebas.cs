using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-724: el jugador solo usa clips de Tony (carpeta Assets/Arte/Personajes/Animaciones, los de Mixamo del zip de Tony), nunca clips de Tripo
    /// ni de otra carpeta, y caminar y correr son exactamente Loco_Caminar y Loco_Correr (los del zip Movimiento_base, sin override).
    /// </summary>
    public class ClipsTonyT724Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string CarpetaTony = "Assets/Arte/Personajes/Animaciones/";
        private const string RutaOverrideJugador = "Assets/Arte/Personajes/Animaciones/Overrides/Override_Jugador.overrideController";
        private const string RutaControladorBase = "Assets/Arte/Personajes/Animaciones/ControladorBase.controller";

        private static RuntimeAnimatorController ControladorDelJugador()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Animator animador = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animador, "El prefab del jugador tiene Animator");
            Assert.IsNotNull(animador.runtimeAnimatorController, "El Animator del jugador tiene controlador");
            return animador.runtimeAnimatorController;
        }

        [Test]
        public void ControladorDelJugador_EsElBaseConElOverrideDelJugador()
        {
            RuntimeAnimatorController rac = ControladorDelJugador();
            Assert.AreEqual("Override_Jugador", rac.name, "El jugador usa Override_Jugador: " + AssetDatabase.GetAssetPath(rac));
            Assert.AreEqual(RutaOverrideJugador, AssetDatabase.GetAssetPath(rac));
        }

        [Test]
        public void TodosLosClipsDelJugador_SonDeLaCarpetaDeTony_NoDeTripo()
        {
            RuntimeAnimatorController rac = ControladorDelJugador();
            var fuera = new List<string>();
            foreach (AnimationClip clip in rac.animationClips)
            {
                string ruta = AssetDatabase.GetAssetPath(clip);
                if (!ruta.StartsWith(CarpetaTony))
                {
                    fuera.Add(clip.name + " <- " + ruta);
                }
            }
            Assert.IsEmpty(fuera, "Clips del jugador que no son de Tony: " + string.Join("; ", fuera));
        }

        [Test]
        public void OverrideDelJugador_NoCambiaNingunClip()
        {
            AnimatorOverrideController o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(RutaOverrideJugador);
            Assert.IsNotNull(o);
            Assert.AreEqual(RutaControladorBase, AssetDatabase.GetAssetPath(o.runtimeAnimatorController));
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            foreach (KeyValuePair<AnimationClip, AnimationClip> par in pares)
            {
                // T-739: el jugador sin implante usa los SinImplante de caminar y correr; todo lo demas es el clip de la base.
                if (par.Key.name == "Loco_Caminar" || par.Key.name == "Loco_Correr") { Assert.AreEqual(par.Key.name + "_SinImplante", par.Value.name); continue; }
                Assert.AreSame(par.Key, par.Value == null ? par.Key : par.Value, "El override del jugador cambia " + par.Key.name + " por " + (par.Value != null ? par.Value.name : "nada"));
            }
        }

        [Test]
        public void CaminarYCorrer_SonLosClipsLocoDeTony_EnLosArbolesDeLaCapaBase()
        {
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControladorBase);
            Assert.IsNotNull(c);
            AnimatorStateMachine capaBase = c.layers[0].stateMachine;
            AnimatorState caminar = null, correr = null;
            foreach (ChildAnimatorState s in capaBase.states)
            {
                if (s.state.name == "Caminar") caminar = s.state;
                if (s.state.name == "Correr") correr = s.state;
            }
            Assert.IsNotNull(caminar, "Estado Caminar");
            Assert.IsNotNull(correr, "Estado Correr");
            Assert.AreEqual("Loco_Caminar", ClipAdelante(caminar), "Caminar hacia delante es Loco_Caminar");
            Assert.AreEqual("Loco_Correr", ClipAdelante(correr), "Correr hacia delante es Loco_Correr");
            Assert.AreEqual(CarpetaTony + "Locomocion/Loco_Caminar_SinImplante.fbx", AssetDatabase.GetAssetPath(ClipPorNombre("Loco_Caminar_SinImplante"))); // T-739: el jugador sin implante
            Assert.AreEqual(CarpetaTony + "Locomocion/Loco_Correr_SinImplante.fbx", AssetDatabase.GetAssetPath(ClipPorNombre("Loco_Correr_SinImplante")));
        }

        private static AnimationClip ClipPorNombre(string nombre)
        {
            foreach (AnimationClip clip in ControladorDelJugador().animationClips)
            {
                if (clip.name == nombre) return clip;
            }
            Assert.Fail("El controlador del jugador no tiene " + nombre);
            return null;
        }

        // El clip del hijo que avanza (umbral 0,1) del árbol 2D, o el clip directo del estado.
        private static string ClipAdelante(AnimatorState estado)
        {
            if (estado.motion is AnimationClip directo) return directo.name;
            BlendTree arbol = (BlendTree)estado.motion;
            // T-739: la raiz mezcla sin y con implante (parametro ConImplante); se mira el arbol sin implante, el que usan los demas papeles.
            if (arbol.blendType == BlendTreeType.Simple1D) arbol = (BlendTree)arbol.children[0].motion;
            foreach (ChildMotion hijo in arbol.children)
            {
                if (hijo.position == new Vector2(0f, 1f)) return hijo.motion.name;
            }
            return "?";
        }
    }
}
