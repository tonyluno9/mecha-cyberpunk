using System.Collections;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-738 (A6 final): con el clip puro la mano de PJ01 retargeteada no queda como en Blender; el agarre de la pistola se compensa por clip (sin tocar huesos) para que
    /// apunte al frente (menos de 3 grados de la mira), con la corredera arriba y en la palma. Mide Apuntar, Disparo y caminar de lado. Medidas en Logs/t738-medidas.txt;
    /// MEDIR_T738=1 solo mide la mano sin compensacion (para recalcular los valores de CorreccionesAgarrePJ01). Capturas pj01-agarre-final-*.png con CAPTURAR_S1=1.
    /// </summary>
    public class AgarreFinalPJ01T738Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;
        private Transform pistola;
        private AgarrePistolaPJ01 agarre;

        private static bool SoloMedir => System.Environment.GetEnvironmentVariable("MEDIR_T738") == "1";

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            pistola = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            agarre = anim.GetComponent<AgarrePistolaPJ01>();
            Assert.IsNotNull(agarre, "El jugador lleva AgarrePistolaPJ01");
            if (SoloMedir) { agarre.enabled = false; }
            yield return Esperar(0.8f);
        }

        private static void Anotar(string linea)
        {
            Debug.Log("T738 " + linea);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.AppendAllText("Logs/t738-medidas.txt", linea + "\n");
        }

        // Giro de la mano derecha en el espacio del cuerpo (lo que se mide para calcular el agarre).
        private Quaternion ManoRelativa() => Quaternion.Inverse(visual.transform.rotation) * anim.GetBoneTransform(HumanBodyBones.RightHand).rotation;

        private static string Q(Quaternion q) => "new Quaternion(" + q.x.ToString("F5") + "f, " + q.y.ToString("F5") + "f, " + q.z.ToString("F5") + "f, " + q.w.ToString("F5") + "f)";

        private Camera Camara() => jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Movimiento.CamaraHombro>().GetComponent<Camera>();

        // Angulo (grados) entre el canon y la linea de la boca al punto que marca el centro de la camara (a 30 m).
        private float AnguloAMira()
        {
            Camera c = Camara();
            Vector3 punto = c.transform.position + c.transform.forward * 30f;
            Transform boca = pistola.Find("Boca");
            Vector3 origen = boca != null ? boca.position : pistola.position;
            return Vector3.Angle(pistola.forward, punto - origen);
        }

        private float AnguloAlFrente() => Vector3.Angle(pistola.forward, visual.transform.forward);

        private float Ladeo() => Mathf.Asin(Mathf.Clamp(Vector3.Dot(pistola.right, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;

        private static Vector3 Palma(Animator a) =>
            Vector3.Lerp(a.GetBoneTransform(HumanBodyBones.RightHand).position, a.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position, 0.5f);

        // Promedia varios cuadros: devuelve el giro medio de la mano y cuanto se aparta (max) de el.
        private IEnumerator Muestrear(string nombre, float segundos, System.Action<Quaternion, float, float, float> alTerminar)
        {
            var muestras = new List<Quaternion>();
            float fin = Time.time + segundos, maxMira = 0f, maxLadeo = 0f, maxPalma = 0f;
            while (Time.time < fin)
            {
                muestras.Add(ManoRelativa());
                maxMira = Mathf.Max(maxMira, AnguloAMira());
                maxLadeo = Mathf.Max(maxLadeo, Mathf.Abs(Ladeo()));
                maxPalma = Mathf.Max(maxPalma, Vector3.Distance(pistola.position, Palma(anim)));
                yield return null;
            }
            Vector4 suma = Vector4.zero;
            foreach (Quaternion m in muestras)
            {
                Quaternion q = Quaternion.Dot(m, muestras[0]) < 0f ? new Quaternion(-m.x, -m.y, -m.z, -m.w) : m;
                suma += new Vector4(q.x, q.y, q.z, q.w);
            }
            suma.Normalize();
            var medio = new Quaternion(suma.x, suma.y, suma.z, suma.w);
            float dispersion = 0f;
            foreach (Quaternion m in muestras) { dispersion = Mathf.Max(dispersion, Quaternion.Angle(m, medio)); }
            Anotar(nombre + ": mano relativa=" + Q(medio) + " dispersion=" + dispersion.ToString("F1") + " | canon a la mira max=" + maxMira.ToString("F1") + " al frente=" + AnguloAlFrente().ToString("F1") +
                " ladeo max=" + maxLadeo.ToString("F1") + " palma max=" + maxPalma.ToString("F3") + " m");
            alTerminar(medio, maxMira, maxLadeo, maxPalma);
        }

        private void Fotos(string nombre)
        {
            Transform t = jugador.transform;
            Vector3 p = pistola.position + pistola.forward * 0.05f;
            CapturaS1.Foto("pj01-agarre-final-" + nombre + "-frente", t, new Vector3(0.2f, 1.25f, 3.0f), new Vector3(0f, 1.15f, 0.3f), 36f, 700, 700);
            CapturaS1.Foto("pj01-agarre-final-" + nombre + "-lado", t, new Vector3(3.0f, 1.2f, 0.3f), new Vector3(0f, 1.15f, 0.3f), 36f, 700, 700);
            CapturaS1.FotoMira("pj01-agarre-final-" + nombre + "-cerca", t, p, new Vector3(1f, 0.3f, 0.1f), 0.7f, 28f, 700);
        }

        [UnityTest]
        public IEnumerator Apuntar_Quieto_CanonALaMira_CorrederaArriba_EnLaPalma()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            yield return Muestrear("quieto", 0.6f, (q, mira, ladeo, palma) =>
            {
                if (SoloMedir) { return; }
                Assert.Less(mira, 3f, "Apuntar: el canon a menos de 3 grados de la mira");
                Assert.Less(ladeo, 3f, "Apuntar: corredera arriba");
                Assert.Less(palma, 0.07f, "Apuntar: empunadura en la palma");
            });
            Fotos("apuntar");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apuntar_Disparo_CanonALaMiraAlSalirElTiro_EnLaPalma()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            // Serie de los primeros cuadros del clip (el retroceso solo sube el canon un poco despues).
            var sb = new StringBuilder("disparo serie (t normalizado: mano relativa | a la mira):");
            float primeraMira = -1f, primerLadeo = 0f, primeraPalma = 0f;
            Quaternion primera = Quaternion.identity;
            for (int i = 0; i < 6; i++)
            {
                float t = anim.GetCurrentAnimatorStateInfo(1).normalizedTime;
                if (i == 1) { primera = ManoRelativa(); primeraMira = AnguloAMira(); primerLadeo = Mathf.Abs(Ladeo()); primeraPalma = Vector3.Distance(pistola.position, Palma(anim)); if (i == 1) { Fotos("disparo"); } }
                sb.Append(" [" + t.ToString("F2") + ": " + Q(ManoRelativa()) + " | " + AnguloAMira().ToString("F1") + "]");
                yield return null;
            }
            Anotar(sb.ToString());
            Anotar("disparo primer cuadro: mano relativa=" + Q(primera) + " a la mira=" + primeraMira.ToString("F1") + " ladeo=" + primerLadeo.ToString("F1") + " palma=" + primeraPalma.ToString("F3"));
            if (!SoloMedir)
            {
                Assert.Less(primeraMira, 3f, "Disparo: el canon a menos de 3 grados de la mira al salir el tiro");
                Assert.Less(primerLadeo, 3f, "Disparo: corredera arriba");
                Assert.Less(primeraPalma, 0.07f, "Disparo: empunadura en la palma");
            }
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator ConLaCompensacionApagada_LaPistolaVuelveAlAgarreDeTony_YSinTocarHuesos()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(1.6f);
            Transform mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Quaternion manoConCompensacion = mano.localRotation;
            Assert.Greater(Quaternion.Angle(Quaternion.Euler(AgarresArmas.GiroPJ01), pistola.localRotation), 5f, "Con la compensacion la pistola ya no lleva el giro de agarre.txt");
            agarre.Usar = false;
            yield return Esperar(0.2f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(AgarresArmas.GiroPJ01), pistola.localRotation), 0.2f, "Apagada, vuelve al agarre de Tony (referencia)");
            Assert.Less(Vector3.Distance(new Vector3(0.0129f, 0.1181f, 0.0080f), pistola.localPosition), 0.0005f, "La posicion nunca se toca");
            Assert.Less(Quaternion.Angle(manoConCompensacion, mano.localRotation), 3f, "La compensacion no mueve la mano (solo cambia el reloj del clip)");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apuntar_CaminarDeLado_CanonALaMira_CorrederaArriba_EnLaPalma()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(1.6f);
            foreach ((string nombre, UnityEngine.InputSystem.Controls.KeyControl tecla) in new[] { ("lateral-derecha", teclado.dKey), ("lateral-izquierda", teclado.aKey), ("atras", teclado.sKey), ("adelante", teclado.wKey) })
            {
                Press(tecla);
                yield return Esperar(1.2f);
                yield return Muestrear(nombre, 1.0f, (q, mira, ladeo, palma) =>
                {
                    if (SoloMedir) { return; }
                    if (nombre != "adelante" && nombre != "atras")
                    {
                        Assert.Less(mira, 3f, nombre + ": el canon a menos de 3 grados de la mira (maximo del ciclo)");
                        Assert.Less(ladeo, 3f, nombre + ": corredera arriba");
                    }
                    Assert.Less(palma, 0.07f, nombre + ": empunadura en la palma");
                });
                if (nombre == "lateral-derecha") { Fotos("lateral"); }
                Release(tecla);
                yield return Esperar(0.6f);
            }
            Release(raton.rightButton);
        }
    }
}
