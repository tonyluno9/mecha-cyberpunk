using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-741 (A7): pasada final de PJ01 jugando con entrada simulada. Apuntar, caminar de lado y hacia atrás apuntando, agachado con y sin pistola, sacar y guardar, daño frente/espalda/fuerte,
    /// rodar y caminar/correr con y sin implante, cada uno con su comprobación del estado del Animator y una foto de frente y otra de lado (docs/capturas/pj01-verif-*.png con CAPTURAR_S1=1).
    /// Las reglas finas (no corre apuntando, no dispara agachado, eventos de sacar y guardar, daño aditivo, agarre) las prueban las clases de T-733 a T-740, que se corren aparte.
    /// </summary>
    public class VerificacionPJ01T741Pruebas : BaseJugadorAPie
    {
        private sealed class ImplantesFalsos : IImplantes
        {
            private readonly int[] niveles = new int[4];
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "X" : "";
            public int Cantidad { get { int n = 0; foreach (int v in niveles) { if (v > 0) { n++; } } return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) { Instalado?.Invoke(espacio, nivel); }
            }
        }

        private VisualPersonaje visual;
        private Animator anim;
        private VidaAPie vida;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vida = jugador.GetComponent<VidaAPie>();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.8f);
        }

        private static IEnumerator Asentar()
        {
            for (int i = 0; i < 6; i++) { yield return null; }
            yield return Esperar(0.2f);
        }

        private static string Estado(Animator a, int capa)
        {
            AnimatorStateInfo info = a.IsInTransition(capa) ? a.GetNextAnimatorStateInfo(capa) : a.GetCurrentAnimatorStateInfo(capa);
            foreach (string n in new[]
            {
                "Idle", "Caminar", "Correr", "AgachadoQuieto", "AgachadoCaminar", "Agacharse", "Pararse", "Rodar",
                "Vacio", "PistolaSacar", "PistolaGuardar", "PistolaApuntar", "PistolaAgachado", "PistolaDisparo", "DanoFrente", "DanoEspalda", "DanoFuerte",
            })
            {
                if (info.IsName(n)) { return n; }
            }
            return "?";
        }

        private string ClipBase()
        {
            string mejor = "";
            float peso = 0f;
            foreach (AnimatorClipInfo c in anim.GetCurrentAnimatorClipInfo(0)) { if (c.weight > peso) { peso = c.weight; mejor = c.clip.name; } }
            return mejor;
        }

        private void Fotos(string nombre)
        {
            CapturaS1.Foto("pj01-verif-" + nombre + "-frente", jugador.transform, new Vector3(0.3f, 1.2f, 3.2f), new Vector3(0f, 0.95f, 0.1f), 40f, 700, 700);
            CapturaS1.Foto("pj01-verif-" + nombre + "-lado", jugador.transform, new Vector3(3.2f, 1.1f, 0.2f), new Vector3(0f, 0.95f, 0.1f), 40f, 700, 700);
        }

        private IEnumerator Mantener(KeyControl tecla, float segundos)
        {
            Press(tecla);
            yield return Esperar(segundos);
        }

        private void Impacto(bool deFrente, float fraccionDeVida)
        {
            Vector3 direccion = deFrente ? -jugador.transform.forward : jugador.transform.forward;
            var info = new InfoDanio(vida.Maxima * fraccionDeVida, TipoDanio.Disparo, jugador.transform.position + Vector3.up, direccion, null);
            EventosPelea.PublicarImpacto(null, info, new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false));
        }

        private IEnumerator HastaCapa(int capa, string estado, float tope)
        {
            float t0 = Time.time;
            while (Estado(anim, capa) != estado && Time.time - t0 < tope) { yield return null; }
        }


        // Distancia entre manos y de la mano izquierda a la pistola, en largos de brazo, con el cuerpo de PJ01 y con el esqueleto de Mixamo del clip (la fuente del retarget).
        private static string Manos(Animator a, Transform pistola)
        {
            Transform hi = a.GetBoneTransform(HumanBodyBones.LeftHand), hd = a.GetBoneTransform(HumanBodyBones.RightHand);
            Transform hombro = a.GetBoneTransform(HumanBodyBones.RightUpperArm), codo = a.GetBoneTransform(HumanBodyBones.RightLowerArm);
            float brazo = Vector3.Distance(hombro.position, codo.position) + Vector3.Distance(codo.position, hd.position);
            Transform dedo = a.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            string s = "manos separadas " + (Vector3.Distance(hi.position, hd.position) / brazo).ToString("F2") + " brazos";
            if (pistola != null) { s += ", mano izq a la pistola " + (Vector3.Distance(Vector3.Lerp(hi.position, dedo.position, 0.5f), pistola.position) / brazo).ToString("F2") + " brazos"; }
            Transform pulgarI = a.GetBoneTransform(HumanBodyBones.LeftThumbProximal), menI = a.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
            s += ", ancho de la mano izq (pulgar-meñique) " + (Vector3.Distance(pulgarI.position, menI.position) / brazo).ToString("F2");
            return s;
        }

        [UnityTest]
        public IEnumerator Medida_PoseDeApuntar_PJ01ContraElEsqueletoDeMixamo()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            anim.speed = 0f;
            yield return null;
            Transform pistola = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            AnimationClip clip = null;
            foreach (UnityEngine.Object o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Arte/Personajes/Animaciones/Pistola/Pistola_Apuntar.fbx")) { if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { clip = c; } }
            float t = (anim.GetCurrentAnimatorStateInfo(1).normalizedTime % 1f) * clip.length;
            GameObject fuente = (GameObject)UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Personajes/Animaciones/Pistola/Pistola_Apuntar.fbx"), new Vector3(6f, 0f, 0f), Quaternion.identity);
            Animator af = fuente.GetComponent<Animator>();
            af.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            clip.SampleAnimation(fuente, t);
            yield return null;
            clip.SampleAnimation(fuente, t);
            string pj = Manos(anim, pistola);
            string mx = Manos(af, null);
            Debug.Log("T741 PJ01: " + pj);
            Debug.Log("T741 Mixamo: " + mx);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.AppendAllText("Logs/t741-medidas.txt", "PJ01 en Unity: " + pj + "\nMixamo (fuente del clip): " + mx + "\n");
            UnityEngine.Object.Destroy(fuente);
            anim.speed = 1f;
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apuntar_ConMovimiento_QuietoDeLadoYHaciaAtras()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1));
            Fotos("apuntar");
            yield return Mantener(teclado.dKey, 1.2f);
            Assert.Greater(anim.GetFloat("ApuntarX"), 0.6f);
            Fotos("apuntar-lateral-derecha");
            Release(teclado.dKey);
            yield return null;
            yield return Mantener(teclado.aKey, 1.2f);
            Assert.Less(anim.GetFloat("ApuntarX"), -0.6f);
            Fotos("apuntar-lateral-izquierda");
            Release(teclado.aKey);
            yield return null;
            yield return Mantener(teclado.sKey, 1.2f);
            Assert.Less(anim.GetFloat("ApuntarZ"), -0.6f);
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1));
            Fotos("apuntar-atras");
            Release(teclado.sKey);
            yield return null;
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Sacar_Y_Guardar_LaPistola()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return HastaCapa(1, "PistolaSacar", 0.5f);
            Assert.AreEqual("PistolaSacar", Estado(anim, 1));
            yield return Esperar(0.45f);
            Fotos("sacar");
            yield return Esperar(1.4f);
            Release(raton.rightButton);
            yield return HastaCapa(1, "PistolaGuardar", 0.5f);
            Assert.AreEqual("PistolaGuardar", Estado(anim, 1));
            yield return Esperar(0.4f);
            Fotos("guardar");
            yield return Esperar(1.2f);
            Assert.AreEqual("Vacio", Estado(anim, 1));
        }

        [UnityTest]
        public IEnumerator Agachado_SinPistola_ConPistola()
        {
            yield return Preparar();
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(1.3f);
            Assert.AreEqual("AgachadoQuieto", Estado(anim, 0));
            Fotos("agachado-sin-pistola");
            Press(raton.rightButton);
            yield return Esperar(1.8f);
            Assert.AreEqual("PistolaAgachado", Estado(anim, 1));
            Fotos("agachado-con-pistola");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Dano_Frente_Espalda_Fuerte_SigueApuntando()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            foreach ((string nombre, bool frente, float fraccion, string estado) in new[] { ("frente", true, 0.05f, "DanoFrente"), ("espalda", false, 0.05f, "DanoEspalda"), ("fuerte", true, 0.4f, "DanoFuerte") })
            {
                Impacto(frente, fraccion);
                yield return HastaCapa(2, estado, 0.3f);
                Assert.AreEqual(estado, Estado(anim, 2), "Daño " + nombre);
                yield return Esperar(0.12f);
                Fotos("dano-" + nombre);
                yield return Esperar(0.9f);
                Assert.AreEqual("PistolaApuntar", Estado(anim, 1), "Sigue apuntando tras el daño " + nombre);
                vida.CurarTodo();
            }
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Rodar_AgachadoYAvanzando()
        {
            yield return Preparar();
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(1.2f);
            Press(teclado.wKey);
            yield return Esperar(0.6f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            Assert.IsTrue(cubierta.Rodando);
            float t0 = Time.time;
            int fotos = 0;
            while (cubierta.Rodando && Time.time - t0 < 3f)
            {
                yield return null;
                if (fotos < 3 && Time.time - t0 > 0.2f + fotos * 0.25f) { fotos++; Fotos("rodar-" + fotos); }
            }
            Release(teclado.wKey);
            Assert.AreEqual(3, fotos, "La rodada dura para tres fotos");
            yield return Esperar(1.3f);
            Fotos("rodar-final");
        }

        [UnityTest]
        public IEnumerator CaminarYCorrer_SinImplanteYConImplante()
        {
            yield return Preparar();
            var implantes = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(implantes);
            yield return Asentar();
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("PJ01_Caminar_SinImplante", ClipBase());
            Fotos("caminar-sin-implante");
            implantes.Poner(EspacioImplante.Piernas, 2);
            implantes.Poner(EspacioImplante.Brazos, 2);
            yield return Asentar();
            Assert.AreEqual("PJ01_Caminar_ConImplante", ClipBase());
            Fotos("caminar-con-implante");
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Assert.AreEqual("PJ01_Correr_ConImplante", ClipBase());
            Fotos("correr-con-implante");
            implantes.Poner(EspacioImplante.Piernas, 0);
            implantes.Poner(EspacioImplante.Brazos, 0);
            yield return Asentar();
            Assert.AreEqual("PJ01_Correr_SinImplante", ClipBase());
            Fotos("correr-sin-implante");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }
    }
}
