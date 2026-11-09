using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-733 (A3): reglas del Animator del jugador con PJ01 y los clips de Tony, con entrada simulada: la pistola solo se ve al apuntar (Sacar y Guardar),
    /// apuntar caminando en cuatro direcciones, no se corre apuntando, agacharse y pararse con clips, no se dispara agachado, rodar 2.2 m en 1 s.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/pj01-*.png.
    /// </summary>
    public class AnimatorPJ01T733Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;

        private IEnumerator Preparar(bool pistola)
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (pistola)
            {
                Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            }
            yield return Esperar(0.8f);
        }

        private static string Estado(Animator a, int capa)
        {
            AnimatorStateInfo info = a.IsInTransition(capa) ? a.GetNextAnimatorStateInfo(capa) : a.GetCurrentAnimatorStateInfo(capa);
            foreach (string n in new[]
            {
                "Idle", "Caminar", "Correr", "AgachadoQuieto", "AgachadoCaminar", "Agacharse", "Pararse", "Rodar",
                "Vacio", "PistolaSacar", "PistolaGuardar", "PistolaIdle", "PistolaApuntar", "PistolaAgachado", "PistolaDisparo",
            })
            {
                if (info.IsName(n)) { return n; }
            }
            return "?";
        }

        // Altura de la cabeza sobre los pies del jugador (no sobre el mundo: el cuerpo puede estar todavia posandose).
        private float AlturaCabeza() => anim.GetBoneTransform(HumanBodyBones.Head).position.y - jugador.transform.position.y;

        private GameObject Pistola() => visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola").gameObject;

        private IEnumerator Pulsar(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        // Dos Press/Release en el mismo cuadro se pisan en el teclado de prueba: cada cambio de tecla va en su cuadro.
        private IEnumerator Cambiar(params (KeyControl tecla, bool abajo)[] cambios)
        {
            foreach ((KeyControl tecla, bool abajo) in cambios)
            {
                if (abajo) { Press(tecla); } else { Release(tecla); }
                yield return null;
            }
        }

        private IEnumerator HastaEstado(int capa, string estado, float limite)
        {
            float fin = Time.time + limite;
            while (Estado(anim, capa) != estado && Time.time < fin)
            {
                yield return null;
            }
        }

        private void Foto(string nombre)
        {
            CapturaS1.Foto("pj01-" + nombre, jugador.transform, new Vector3(3.2f, 1.1f, 1.2f), new Vector3(0f, 0.9f, 0.2f), 40f, 700, 700);
        }

        // ---- La pistola solo se ve al apuntar ----

        [UnityTest]
        public IEnumerator Pistola_NoSeVeAunqueEsteSeleccionada_AparecaAlApuntarConSacar_YDesapareceAlGuardar()
        {
            yield return Preparar(true);
            yield return Esperar(0.5f);
            Assert.IsFalse(Pistola().activeInHierarchy, "Con la pistola seleccionada y sin apuntar no se ve");
            Assert.AreEqual("Vacio", Estado(anim, 1));
            Foto("pistola-guardada");

            Press(raton.rightButton);
            yield return HastaEstado(1, "PistolaSacar", 0.5f);
            Assert.AreEqual("PistolaSacar", Estado(anim, 1), "Entrar a apuntar es Pistola_Sacar");
            yield return Esperar(0.45f); // T-734: aparece con el evento de Pistola_Sacar (0.30 s)
            Assert.IsTrue(Pistola().activeInHierarchy, "Al apuntar se ve");
            Foto("sacar");
            yield return Esperar(1.3f);
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1), "Terminado Sacar queda apuntando");
            Foto("apuntar");

            Release(raton.rightButton);
            yield return HastaEstado(1, "PistolaGuardar", 0.5f);
            Assert.AreEqual("PistolaGuardar", Estado(anim, 1), "Dejar de apuntar es Pistola_Guardar");
            yield return Esperar(0.7f); // T-734: desaparece con el evento de Pistola_Guardar (0.53 s)
            Assert.IsFalse(Pistola().activeInHierarchy, "Al dejar de apuntar no se ve");
            Foto("guardar");
            yield return Esperar(1.2f);
            Assert.AreEqual("Vacio", Estado(anim, 1));
        }

        // ---- Apuntar caminando ----

        [UnityTest]
        public IEnumerator ApuntarCaminando_MezclaElArbol2D_AdelanteAtrasYDeLado()
        {
            yield return Preparar(true);
            Press(raton.rightButton);
            yield return Esperar(1.4f);
            Assert.AreEqual(0f, anim.GetFloat("ApuntarZ"), 0.15f, "Quieto apunta quieto");

            Press(teclado.dKey);
            yield return Esperar(0.8f);
            Assert.Greater(anim.GetFloat("ApuntarX"), 0.6f, "De lado a la derecha");
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1));
            Foto("apuntar-lateral-derecha");
            yield return Cambiar((teclado.dKey, false), (teclado.aKey, true));
            yield return Esperar(0.9f);
            Assert.Less(anim.GetFloat("ApuntarX"), -0.6f, "De lado a la izquierda");
            Foto("apuntar-lateral-izquierda");
            yield return Cambiar((teclado.aKey, false), (teclado.sKey, true));
            yield return Esperar(0.9f);
            Assert.Less(anim.GetFloat("ApuntarZ"), -0.6f, "Retrocediendo");
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1));
            Foto("apuntar-atras");
            Release(teclado.sKey);
            yield return null;
            Press(teclado.wKey);
            yield return Esperar(0.9f);
            Assert.Greater(anim.GetFloat("ApuntarZ"), 0.6f, "Hacia adelante");
            Release(teclado.wKey);
            yield return null;
            Release(raton.rightButton);
        }

        // ---- No corre apuntando ----

        [UnityTest]
        public IEnumerator Correr_SoloSinApuntar_ApuntandoCaminaConLocoCaminar()
        {
            yield return Preparar(true);
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Assert.Greater(controlador.Velocidad.magnitude, 6.5f, "Con Shift corre (sin apuntar)");
            Assert.AreEqual("Correr", Estado(anim, 0));

            Press(raton.rightButton);
            yield return Esperar(1.8f);
            Assert.LessOrEqual(controlador.Velocidad.magnitude, datos.VelocidadCaminar + 0.1f, "Apuntando no corre aunque tenga Shift");
            Assert.AreNotEqual("Correr", Estado(anim, 0), "Apuntando no juega Loco_Correr");
            Assert.AreEqual("Caminar", Estado(anim, 0));
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            yield return null;
            Release(raton.rightButton);
        }

        // ---- Agachado ----

        [UnityTest]
        public IEnumerator Agachado_AgacharseQuietoCaminarEnCuatroDireccionesYPararse()
        {
            yield return Preparar(false);
            float cabezaDePie = AlturaCabeza();
            yield return Pulsar(teclado.leftCtrlKey);
            yield return HastaEstado(0, "Agacharse", 0.4f);
            Assert.AreEqual("Agacharse", Estado(anim, 0), "Primero Loco_Agacharse");
            yield return Esperar(1.3f);
            Assert.AreEqual("AgachadoQuieto", Estado(anim, 0), "Despues Loco_AgachadoIdle");
            Assert.Less(AlturaCabeza(), cabezaDePie - 0.12f, "La cabeza baja");
            Assert.AreEqual(0f, visual.PesoCapaArriba, 0.05f, "Sin pose de arma agachado");
            Foto("agachado-quieto");

            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("AgachadoCaminar", Estado(anim, 0));
            Assert.Greater(anim.GetFloat("VelZ"), 0.6f);
            Foto("agachado-caminar");
            Release(teclado.wKey);
            yield return null;
            // De lado y hacia atras solo hay clips cuando el cuerpo mira a la camara (apuntando): sin apuntar gira hacia donde camina.
            Press(raton.rightButton);
            yield return null;
            Press(teclado.dKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("AgachadoCaminar", Estado(anim, 0));
            Assert.Greater(anim.GetFloat("VelX"), 0.6f, "Lateral derecha");
            Foto("agachado-lateral");
            Release(teclado.dKey);
            yield return null;
            Press(teclado.sKey);
            yield return Esperar(1.0f);
            Assert.Less(anim.GetFloat("VelZ"), -0.6f, "Hacia atras");
            Foto("agachado-atras");
            Release(teclado.sKey);
            yield return null;
            Release(raton.rightButton);
            yield return Esperar(0.8f);

            yield return Pulsar(teclado.leftCtrlKey);
            yield return HastaEstado(0, "Pararse", 0.4f);
            Assert.AreEqual("Pararse", Estado(anim, 0), "Para levantarse Loco_Pararse");
            yield return Esperar(1.3f);
            Assert.AreEqual("Idle", Estado(anim, 0));
            Assert.AreEqual(cabezaDePie, AlturaCabeza(), 0.12f, "Vuelve a su altura");
        }

        [UnityTest]
        public IEnumerator Agachado_ApuntandoConPistola_UsaLosClipsDePistolaAgachada()
        {
            yield return Preparar(true);
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.2f);
            Press(raton.rightButton);
            yield return Esperar(1.6f);
            Assert.AreEqual("PistolaAgachado", Estado(anim, 1), "Apuntando agachado: Pistola_AgachadoIdle");
            Assert.AreEqual("AgachadoQuieto", Estado(anim, 0));
            Assert.IsTrue(Pistola().activeInHierarchy);
            Foto("agachado-apuntando");
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("PistolaAgachado", Estado(anim, 1), "Caminando: Pistola_AgachadoCaminar (mismo arbol)");
            Assert.AreEqual("AgachadoCaminar", Estado(anim, 0));
            Foto("agachado-apuntando-caminar");
            Release(teclado.wKey);
            yield return null;
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Agachado_AlDispararSePara_YDisparaConLaMismaPulsacion()
        {
            yield return Preparar(true);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.0f);
            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.IsTrue(cubierta.AgachadoLibre);
            int antes = armas.Balas;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.3f);
            Assert.AreEqual(antes - 1, armas.Balas, "T-936: agachado, la primera pulsación ya dispara");
            Assert.IsFalse(cubierta.AgachadoLibre, "y se para");
            Release(raton.rightButton);
        }

        // ---- Rodar ----

        [UnityTest]
        public IEnumerator Rodar_EsLocoRodar_Avanza2Punto2MetrosEnUnSegundo_SinEncogerNiGirarElModelo_YTerminaAgachado()
        {
            yield return Preparar(false);
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            Transform modelo = anim.transform;
            Vector3 escala = modelo.localScale;
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.2f);
            Press(teclado.wKey);
            yield return Esperar(0.6f);
            Vector3 inicio = jugador.transform.position;
            float t0 = Time.time;
            yield return Pulsar(teclado.spaceKey);
            Assert.IsTrue(cubierta.Rodando, "Agachado el espacio rueda");
            float maxGiro = 0f;
            float cambioEscala = 0f;
            bool vioRodar = false;
            int fotos = 0;
            while (cubierta.Rodando && Time.time - t0 < 3f)
            {
                yield return null;
                vioRodar |= Estado(anim, 0) == "Rodar";
                maxGiro = Mathf.Max(maxGiro, Quaternion.Angle(Quaternion.identity, modelo.localRotation));
                cambioEscala = Mathf.Max(cambioEscala, Mathf.Abs(modelo.localScale.y - escala.y));
                if (fotos < 3 && Time.time - t0 > 0.2f + fotos * 0.25f)
                {
                    fotos++;
                    Foto("rodar-" + fotos);
                }
            }
            float duracion = Time.time - t0;
            Release(teclado.wKey);
            float recorrido = Vector3.ProjectOnPlane(jugador.transform.position - inicio, Vector3.up).magnitude;
            Assert.AreEqual(1f, duracion, 0.15f, "La rodada dura 1 s");
            Assert.AreEqual(2.2f, recorrido, 0.35f, "Avanza 2.2 m");
            Assert.IsTrue(vioRodar, "El Animator juega Loco_Rodar");
            Assert.Less(maxGiro, 0.1f, "El modelo no se gira a mano: la vuelta es del clip");
            Assert.Less(cambioEscala, 0.001f, "El modelo no se encoge a mano");
            yield return Esperar(1.3f);
            Assert.IsTrue(cubierta.AgachadoLibre, "Termina agachado");
            Assert.IsTrue(Estado(anim, 0) == "AgachadoQuieto" || Estado(anim, 0) == "AgachadoCaminar", "Estado al terminar: " + Estado(anim, 0));
            Assert.AreEqual(escala.y, modelo.localScale.y, 0.001f);
            Foto("rodar-final");
        }

        // ---- Aliados y enemigos ----

        [UnityTest]
        public IEnumerator Aliados_Enemigos_YNPC_SiguenAndandoAgachandoseYApuntando()
        {
            yield return Calentar();
            string[] visuales = { "Visual_woman2", "Visual_man", "Visual_SkullHombre", "Visual_GuardiaNova", "Visual_Doctor", "Visual_Hacker", "Visual_RobotHombre" };
            string fallos = "";
            for (int i = 0; i < visuales.Length; i++)
            {
                GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + visuales[i] + ".prefab"),
                    new Vector3(8f + i * 2f, 0.05f, 6f), Quaternion.identity);
                VisualPersonaje v = go.GetComponent<VisualPersonaje>();
                Animator a = v.Animador;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                v.LeerDelPadre = false;
                yield return Esperar(0.4f);
                if (Estado(a, 0) != "Idle") { fallos += visuales[i] + " idle=" + Estado(a, 0) + "; "; }
                v.Velocidad = 2.5f;
                yield return Esperar(0.8f);
                if (Estado(a, 0) != "Caminar") { fallos += visuales[i] + " caminar=" + Estado(a, 0) + "; "; }
                v.Velocidad = 0f;
                v.Agachado = true;
                yield return Esperar(1.4f);
                if (Estado(a, 0) != "AgachadoQuieto") { fallos += visuales[i] + " agachado=" + Estado(a, 0) + "; "; }
                v.Agachado = false;
                yield return Esperar(1.4f);
                if (Estado(a, 0) != "Idle") { fallos += visuales[i] + " parado=" + Estado(a, 0) + "; "; }
                v.TipoArma = TipoArmaVisual.Pistola;
                v.Apuntando = true;
                yield return Esperar(1.6f);
                if (Estado(a, 1) != "PistolaApuntar") { fallos += visuales[i] + " apuntar=" + Estado(a, 1) + "; "; }
                Object.Destroy(go);
            }
            Assert.AreEqual("", fallos);
        }
    }
}
