using System.Collections;
using System.Linq;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>S1 (T-650/T-651): el arma va en la palma, el cañón al frente, la mano izquierda en el arma, y con arma en mano se usan las animaciones de arma al caminar y correr.</summary>
    public class ArmaEnManoS1Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
            return a.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        private Transform Arma(string nombre) => Visual().GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find(nombre);

        // Centro de la palma: entre la muñeca y el nudillo del dedo medio.
        private static Vector3 Palma(Animator a) =>
            Vector3.Lerp(a.GetBoneTransform(HumanBodyBones.RightHand).position, a.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position, 0.5f);

        private IEnumerator Fotos(string sufijo, Transform arma)
        {
            Transform t = jugador.transform;
            Vector3 p = arma.position + arma.forward * 0.05f;
            CapturaS1.Foto("s1-" + sufijo + "-cuerpo", t, new Vector3(2.4f, 1.0f, 0.3f), new Vector3(0f, 0.95f, 0.3f), 45f, 700, 700);
            CapturaS1.FotoMira("s1-" + sufijo + "-izq", t, p, new Vector3(-1f, 0.25f, 0.2f), 0.8f);
            CapturaS1.FotoMira("s1-" + sufijo + "-der", t, p, new Vector3(1f, 0.25f, 0.2f), 0.8f);
            CapturaS1.FotoMira("s1-" + sufijo + "-frente", t, p, new Vector3(0.35f, 0.5f, 1f), 0.8f);
            yield return null;
        }




        private class PerfilHombre : MechaCyberpunk.Core.IPerfilJugador
        {
            public string Nombre => "T";
            public string Alias => "T";
            public MechaCyberpunk.Core.AparienciaJugador Apariencia => new MechaCyberpunk.Core.AparienciaJugador(1, 0, 0);
            public string NombrePandilla => "P";
            public int ColorPandilla => 0;
            public event System.Action Cambio { add { } remove { } }
        }

        [UnityTest]
        public IEnumerator ModeloHombre_ConPistola_AgarreEnLaPalmaYCanonAlFrente()
        {
            yield return Calentar();
            GameObject host = new GameObject("ModeloSegunPerfilS1");
            var perfil = new PerfilHombre();
            MechaCyberpunk.Core.Registro<MechaCyberpunk.Core.IPerfilJugador>.Agregar(perfil);
            var modelo = host.AddComponent<MechaCyberpunk.Personaje.Apariencia.ModeloSegunPerfil>();
            modelo.Configurar(new[]
            {
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab"),
            });
            yield return null;
            yield return null;
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            Assert.IsTrue(visual.GetComponentInChildren<SkinnedMeshRenderer>().name.Contains("0f9d95ea"), "Es el hombre");
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Transform pistola = Arma("Pistola");
            Assert.IsNotNull(pistola, "La pistola pasó a la mano del hombre");
            yield return Esperar(1f);
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.12f, "En reposo la empuñadura está en la palma del hombre (m)");
            Press(raton.rightButton);
            yield return Esperar(0.9f);
            yield return Fotos("hombre-pistola-apuntar", pistola);
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(pistola.forward, Vector3.up).normalized, visual.transform.forward), 0.9f, "Apuntando el cañón mira al frente (conserva la inclinación de la animación)");
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.12f, "Apuntando sigue en la palma");
            Release(raton.rightButton);
            MechaCyberpunk.Core.Registro<MechaCyberpunk.Core.IPerfilJugador>.Quitar(perfil);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator ConPistola_CaminarYCorrer_LasPiernasUsanPistolaCaminarYCorrer()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            int capa = a.GetLayerIndex(ReglasAnimacion.CapaPiernas);
            Assert.GreaterOrEqual(capa, 0, "El controlador tiene la capa Piernas");
            yield return Esperar(0.4f);
            Assert.AreEqual(0f, a.GetLayerWeight(capa), 0.01f, "Sin arma, las piernas son de la capa base");
            Press(teclado.wKey);
            yield return Esperar(0.8f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.01f, "Con los puños caminar usa la caminata normal");
            Release(teclado.wKey);
            yield return Esperar(0.6f);

            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.3f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.01f, "Quieto con pistola, piernas de la base");
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(1f, visual.PesoPiernasArma, 0.05f, "Caminando con pistola sin apuntar: piernas de Pistola_Caminar");
            Assert.IsTrue(EnEstado(a, capa, "PistolaPiernas"));
            Assert.AreEqual(1f, a.GetLayerWeight(capa), 0.05f);
            Assert.AreEqual("Pistola_Caminar", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);

            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(1f, visual.PesoPiernasArma, 0.05f);
            Assert.AreEqual("Pistola_Correr", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name, "Con Shift, Pistola_Correr");
            float esperada = Mathf.Clamp(visual.Velocidad / (ReglasAnimacion.VelocidadClipArma(TipoArmaVisual.Pistola, visual.Velocidad) * visual.EscalaClips), 0.6f, 2.8f);
            Assert.AreEqual(esperada, a.speed, esperada * 0.15f, "Los pies siguen la velocidad real con el clip del arma");
            Release(teclado.leftShiftKey);

            // Apuntando se queda la locomoción de la base (con dirección).
            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.05f, "Apuntando, la base maneja las piernas");
            Release(raton.rightButton);
            Release(teclado.wKey);

            // Arma larga: Rifle_Caminar y Rifle_Correr.
            yield return Esperar(0.6f);
            visual.TipoArma = TipoArmaVisual.ArmaLarga;
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.IsTrue(EnEstado(a, capa, "LargaPiernas"));
            Assert.AreEqual("Rifle_Caminar", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Rifle_Correr", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Pistola_AgarreEnLaPalma_CanonAlFrente_EnReposoCaminarYApuntar()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Transform pistola = Arma("Pistola");
            Assert.IsTrue(AgarresArmas.TryObtener("Pistola", out AgarreArma agarre));
            Assert.AreEqual(0f, Vector3.Distance(agarre.Posicion, pistola.localPosition), 0.001f, "Posición del arma = la del agarre");
            Transform mano = a.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.AreEqual(mano, pistola.parent.parent, "El arma cuelga del hueso RightHand (por el socket)");
            yield return Esperar(1f);
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.07f, "En reposo la empuñadura está en la palma (m)");
            Assert.Greater(Vector3.Dot(pistola.forward, mano.up), 0.97f, "El cañón sigue la dirección de los dedos");
            Assert.Greater(Vector3.Dot(pistola.forward, visual.transform.forward), 0.5f, "En reposo el cañón mira hacia adelante");
            Assert.Greater(Vector3.Dot(pistola.up, Vector3.up), 0.5f, "La corredera queda arriba");
            yield return Fotos("pistola-reposo", pistola);

            Press(teclado.wKey);
            yield return Esperar(1.2f);
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.07f, "Caminando sigue en la palma");
            Assert.Greater(Vector3.Dot(pistola.forward, visual.transform.forward), 0.8f, "Caminando el cañón mira hacia adelante");
            yield return Fotos("pistola-caminar", pistola);
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.Greater(Vector3.Dot(pistola.forward, visual.transform.forward), 0.8f, "Corriendo el cañón mira hacia adelante");
            yield return Fotos("pistola-correr", pistola);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.6f);

            Press(raton.rightButton);
            yield return Esperar(0.9f);
            yield return Fotos("pistola-apuntar", pistola);
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(pistola.forward, Vector3.up).normalized, visual.transform.forward), 0.9f, "Apuntando el cañón mira al frente (conserva la inclinación de la animación)");
            Assert.Less(Mathf.Abs(Vector3.Dot(pistola.right, Vector3.up)), 0.35f, "La pistola no está torcida");
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.07f, "Apuntando sigue en la palma");

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.1f);
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(pistola.forward, Vector3.up).normalized, visual.transform.forward), 0.95f, "Disparando el cañón sigue al frente (el retroceso solo lo levanta)");
            yield return Fotos("pistola-disparo", pistola);
            Release(raton.rightButton);
            yield return Esperar(0.8f);

            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Esperar(0.7f);
            Assert.Less(Vector3.Distance(pistola.position, Palma(a)), 0.1f, "Recargando el arma sigue en la mano");
            yield return Fotos("pistola-recargar", pistola);
        }

        [UnityTest]
        public IEnumerator ManoIzquierda_PistolaALasDosManosAlApuntar_YSueltaAlRecargar()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ApuntadoIK ik = visual.GetComponentInChildren<ApuntadoIK>(true);
            jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            Transform pistola = Arma("Pistola");
            Transform agarre = pistola.Find(AgarresArmas.NombreAgarreIzquierdo);
            Transform izquierda = a.GetBoneTransform(HumanBodyBones.LeftHand);
            Assert.IsNotNull(agarre, "La pistola tiene el punto de la mano izquierda");
            yield return Esperar(1f);
            Assert.AreEqual(0f, ik.PesoManoIzquierda, 0.05f, "En reposo la pistola va a una mano");

            Press(raton.rightButton);
            yield return Esperar(0.9f);
            Assert.AreEqual(1f, ik.PesoManoIzquierda, 0.05f, "Apuntando la pistola va a dos manos");
            Assert.Less(Vector3.Distance(izquierda.position, agarre.position), 0.12f, "La mano izquierda está en el agarre del arma (m)");
            Release(raton.rightButton);
            yield return Esperar(0.6f);

            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Esperar(0.4f);
            Assert.AreEqual(0f, ik.PesoManoIzquierda, 0.05f, "Recargando la mano izquierda se suelta");
        }

        [UnityTest]
        public IEnumerator ManoIzquierda_ArmaLarga_VaSiempreAlArma_YElArmaEstaEnLaPalma()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ApuntadoIK ik = visual.GetComponentInChildren<ApuntadoIK>(true);
            Transform socket = ik.SocketArma;
            socket.Find("Pistola").gameObject.SetActive(false);
            Transform fusil = socket.Find("Fusil");
            fusil.gameObject.SetActive(true);
            visual.TipoArma = TipoArmaVisual.ArmaLarga;
            Transform agarre = fusil.Find(AgarresArmas.NombreAgarreIzquierdo);
            Transform izquierda = a.GetBoneTransform(HumanBodyBones.LeftHand);
            yield return Esperar(1f);
            Assert.AreEqual(1f, ik.PesoManoIzquierda, 0.05f, "El arma larga va a dos manos aunque no se apunte");
            Assert.Less(Vector3.Distance(izquierda.position, agarre.position), 0.27f, "La mano izquierda está en el guardamano (m)");
            Assert.Greater(Vector3.Dot(fusil.forward, visual.transform.forward), 0.9f, "El arma larga se lleva lista al frente, no cruzada sobre el pecho");
            yield return Fotos("fusil-reposo", fusil);
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            Assert.Less(Vector3.Distance(izquierda.position, agarre.position), 0.27f, "Caminando la mano izquierda sigue en el arma");
            Assert.Greater(Vector3.Dot(fusil.forward, visual.transform.forward), 0.9f, "Caminando el arma larga sigue al frente");
            yield return Fotos("fusil-caminar", fusil);
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.Less(Vector3.Distance(izquierda.position, agarre.position), 0.27f, "Corriendo la mano izquierda sigue en el arma");
            yield return Fotos("fusil-correr", fusil);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Press(raton.rightButton);
            yield return Esperar(0.9f);
            yield return Fotos("fusil-apuntar", fusil);
            Assert.Greater(Vector3.Dot(fusil.forward, visual.transform.forward), 0.95f, "Apuntando el cañón del arma larga mira al frente");
            Assert.Less(Vector3.Distance(izquierda.position, agarre.position), 0.27f);
            Release(raton.rightButton);
        }
    }
}
