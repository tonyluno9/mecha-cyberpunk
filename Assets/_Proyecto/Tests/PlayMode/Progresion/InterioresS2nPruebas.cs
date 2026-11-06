using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Tests.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// S2n (T-812a) en la escena real (Ciudad_Prologo): del vano del CIC se llega caminando al banco de Tino y el aviso de E es el del banco; en el
    /// consultorio, la silla del Doc sienta al jugador sobre el cojín del sillón I07 con la espalda hacia el cabezal, y la cámara cercana ve al
    /// jugador y al brazo. Con CAPTURAR_S2N_INTERIORES=1 guarda docs/capturas/s2n-silla-consultorio.png.
    /// </summary>
    public class InterioresS2nPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_S2N_INTERIORES";
        /// <summary>Justo adentro del vano del CIC (ColocarS2Interiores: vano en x -73.3, frente z 10.45).</summary>
        private static readonly Vector3 VanoCic = new Vector3(-73.3f, 0.25f, 10.9f);
        private const int Dinero = 500;
        private const float SegundosEspera = 8f;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }
            public event Action<int, MotivoDinero> Cambio;
            public bool PuedePagar(int cantidad) => cantidad >= 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                Dinero += cantidad;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo) => Abonar(-cantidad, motivo);
        }

        private IJugador jugador;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 30 && Registro<IJugador>.Actual == null; i++)
            {
                yield return null;
            }
            jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "Ciudad_Prologo trae al jugador");
            // El director de la historia pone al jugador en la cama al arrancar: se espera a que lo haga antes de moverlo.
            float espera = Time.realtimeSinceStartup + 20f;
            MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas director = null;
            while ((director == null || !director.Iniciado) && Time.realtimeSinceStartup < espera)
            {
                director = UnityEngine.Object.FindFirstObjectByType<MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas>();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest]
        public IEnumerator Banco_DesdeElVanoDelCic_SeLlegaCaminandoYElAvisoEsElDelBanco()
        {
            BancoArmas banco = UnityEngine.Object.FindFirstObjectByType<BancoArmas>();
            Assert.IsNotNull(banco, "El CIC trae el banco de armas (ColocarS2nBancoArmas.Colocar)");
            Vector3 punto = banco.Posicion;
            jugador.Colocar(new Pose(VanoCic, Quaternion.LookRotation(SalidaCasaPruebas.Plano(punto - VanoCic))));
            yield return null;
            yield return SalidaCasaPruebas.Caminar(jugador, punto);
            Vector3 haciaBanco = SalidaCasaPruebas.Plano(banco.transform.position - jugador.Raiz.position);
            jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(haciaBanco)));
            InteraccionAPie interaccion = ((Component)jugador).GetComponentInChildren<InteraccionAPie>();
            Assert.IsNotNull(interaccion);
            float fin = Time.realtimeSinceStartup + SegundosEspera;
            while (!ReferenceEquals(interaccion.InteractuableCandidato, banco) && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.AreSame(banco, interaccion.InteractuableCandidato, "Frente al banco, la E es del banco (candidato: " + interaccion.InteractuableCandidato + ", jugador en " + jugador.Raiz.position + ")");
            Assert.IsTrue(banco.PuedeUsar, "El banco se puede usar a pie");
        }

        [UnityTest]
        public IEnumerator Silla_EnElConsultorio_SientaSobreElCojinYLaCamaraVeAlJugadorYAlBrazo()
        {
            SillaClinica silla = SillaClinica.Actual;
            Assert.IsNotNull(silla, "El consultorio trae la silla (ColocarS2nSillaClinica.Colocar)");
            if (Registro<ICartera>.Actual == null)
            {
                Registro<ICartera>.Agregar(new CarteraDoble());
            }
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(Dinero, MotivoDinero.Otro);
            VidaAPie vida = ((Component)jugador).GetComponentInChildren<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(vida.Maxima * 0.5f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            jugador.Colocar(new Pose(silla.Posicion, Quaternion.identity));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(silla.PuedeUsar, "Herido y con dinero la silla se ofrece: " + silla.Texto);
            silla.Usar();
            float fin = Time.realtimeSinceStartup + SegundosEspera;
            while (silla.Barra < 0.75f && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.IsTrue(silla.Sentado, "Sentado");
            Transform asiento = silla.transform.Find("Asiento");
            Animator animador = ((Component)jugador).GetComponentInChildren<Animator>();
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
            Transform pie = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Assert.Less(Vector3.Distance(cadera.position, asiento.position), 0.15f, "La cadera en el asiento");
            Transform sillon = GameObject.Find("S2_Interiores/ConsultorioDoc/I07").transform;
            Bounds b = Limites(sillon);
            Assert.Greater(cadera.position.y, b.min.y + b.size.y * 0.6f, "La cadera sobre el cojín, no dentro del sillón");
            Assert.Greater(Vector3.Dot(pie.position - cadera.position, asiento.forward), 0.3f, "Las piernas hacia donde mira (los pies del sillón)");
            Assert.Less(Vector3.Dot(cabeza.position - cadera.position, asiento.forward), 0.1f, "La espalda hacia el cabezal");
            Assert.IsTrue(b.Contains(new Vector3(pie.position.x, b.center.y, pie.position.z)), "Los pies quedan sobre el sillón: " + pie.position);
            Camera camara = silla.CamaraCercana;
            Vector3 vp = camara.WorldToViewportPoint(cabeza.position);
            Assert.IsTrue(vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f, "La cámara ve la cabeza del jugador: " + vp);
            Transform brazo = GameObject.Find("S2_Interiores/ConsultorioDoc/I06").transform;
            Bounds bb = Limites(brazo);
            Vector3 vb = camara.WorldToViewportPoint(new Vector3(bb.center.x, bb.min.y + 0.2f, bb.center.z));
            Assert.IsTrue(vb.z > 0f && vb.x > 0f && vb.x < 1f && vb.y > 0f && vb.y < 1f, "La cámara ve la punta del brazo robótico: " + vb);
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/s2n-silla-consultorio.png", camara);
            }
            fin = Time.realtimeSinceStartup + SegundosEspera;
            while (silla.EnSecuencia && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.IsFalse(silla.EnSecuencia, "Termina la secuencia");
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.01f, "Curado");
        }

        [Test]
        public void Cic_TinoNoAtraviesaMueblesYLosBancosSonDeMesaDeTrabajo()
        {
            Transform tino = GameObject.Find("GTA_T417/Apoyo_TINO").transform;
            Transform taller = GameObject.Find("S2_Interiores/TallerTino").transform;
            Vector3 pie = tino.position;
            foreach (Collider c in Physics.OverlapCapsule(pie + Vector3.up * 0.35f, pie + Vector3.up * 1.2f, 0.28f, ~0, QueryTriggerInteraction.Ignore))
            {
                Assert.IsFalse(c.transform.IsChildOf(taller), "Tino atraviesa " + c.name + " (" + c.transform.parent.name + ")");
            }
            int bancos = 0;
            foreach (Transform t in taller)
            {
                if (t.name != "T03_BancoTrabajo")
                {
                    continue;
                }
                bancos++;
                Bounds b = Limites(t);
                Assert.AreEqual(2.0f, Mathf.Max(b.size.x, b.size.z), 0.25f, "Banco de ~2 m de largo: " + b.size);
                Vector3 enTino = new Vector3(pie.x, b.center.y, pie.z);
                Assert.Greater(Mathf.Sqrt(b.SqrDistance(enTino)), 0.25f, "Tino no está dentro del banco " + b.min + ".." + b.max);
            }
            Assert.AreEqual(2, bancos, "Dos bancos de trabajo");
            Transform pizarron = taller.Find("Pizarron");
            Assert.Less(pizarron.position.z, 15.9f - 0.01f, "El pizarrón despegado de la pared (sin encimarse)");
            Assert.Greater(pizarron.position.z, 15.9f - 0.08f, "El pizarrón pegado a la pared");
        }

        /// <summary>T-811 y T-812: lo que está en el piso de cada local no se encima con otra pieza del piso ni se mete en las paredes.</summary>
        [Test]
        public void Interiores_LoDelPisoNoSeEncimaNiAtraviesaParedes([Values("TallerTino", "ConsultorioDoc")] string local)
        {
            Transform r = GameObject.Find("S2_Interiores/" + local).transform;
            // Las cajas de ColocarS2Interiores (por dentro del forro).
            Rect caja = local == "TallerTino" ? Rect.MinMaxRect(-77.2f, 10.45f, -69.4f, 15.9f) : Rect.MinMaxRect(-103.9f, -15.9f, -96.1f, -10.45f);
            const float Piso = 0.22f;
            const float Tolerancia = 0.04f;
            System.Collections.Generic.List<(string, Bounds)> piezas = new System.Collections.Generic.List<(string, Bounds)>();
            foreach (Transform t in r)
            {
                if (t.name == "Forro" || t.name == "Luces" || t.name == "SerieFocos" || t.GetComponentsInChildren<Renderer>().Length == 0)
                {
                    continue;
                }
                Bounds b = Limites(t);
                if (b.min.y > Piso + Tolerancia)
                {
                    continue;
                }
                Assert.GreaterOrEqual(b.min.x, caja.xMin - Tolerancia, t.name + " se mete en la pared izquierda");
                Assert.LessOrEqual(b.max.x, caja.xMax + Tolerancia, t.name + " se mete en la pared derecha");
                Assert.GreaterOrEqual(b.min.z, caja.yMin - Tolerancia, t.name + " se mete en la pared del frente o del fondo");
                Assert.LessOrEqual(b.max.z, caja.yMax + Tolerancia, t.name + " se mete en la pared del frente o del fondo");
                piezas.Add((t.name, b));
            }
            Assert.Greater(piezas.Count, 8, "El local trae sus muebles");
            for (int i = 0; i < piezas.Count; i++)
            {
                for (int j = i + 1; j < piezas.Count; j++)
                {
                    Bounds a = piezas[i].Item2, b = piezas[j].Item2;
                    float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                    Assert.IsFalse(x > Tolerancia && z > Tolerancia, piezas[i].Item1 + " se encima con " + piezas[j].Item1);
                }
            }
        }

        private static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
