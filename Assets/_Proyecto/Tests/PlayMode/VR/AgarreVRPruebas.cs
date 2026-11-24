using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR3/VR35: el grip sujeta a un SKULL real (prefab de la calle) y lo lleva en la mano; con el implante de BRAZOS al soltarlo sale lanzado con la
    /// velocidad de la mano (y al chocar contra una pared pierde vida). Golpearlo con la otra mano le baja vida y lo deja en la mano. Sin implante
    /// también lo sujeta, pero al soltarlo solo cae.
    /// </summary>
    public class AgarreVRPruebas : BaseJugadorVR
    {
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private static readonly Vector3 PosicionSkull = new Vector3(0.2f, 0.05f, 0.75f);
        private static readonly Vector3 ManoDerInicio = new Vector3(0.2f, 1.2f, 0.3f);

        private ImplantesDoble implantes;
        private PandilleroAPie skull;
        private AgarreImplanteVR agarre;

        private sealed class ImplantesDoble : IImplantes
        {
            private readonly int[] niveles = new int[4];
            public int Cantidad { get { int n = 0; foreach (int x in niveles) if (x > 0) n++; return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "DOBLE" : string.Empty;

            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) Instalado?.Invoke(espacio, nivel);
            }
        }

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            AgarreImplanteVR.ForzarImplante = null;
            implantes = new ImplantesDoble();
        }

        public override void TearDown()
        {
            Registro<IImplantes>.Quitar(implantes);
            AgarreImplanteVR.ForzarImplante = null;
            if (skull != null)
            {
                Object.Destroy(skull.gameObject);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // Jugador VR en el origen mirando a +Z y un SKULL justo enfrente, al alcance de la mano derecha.
        private IEnumerator Preparar(int nivelBrazos)
        {
            implantes.Poner(EspacioImplante.Brazos, nivelBrazos);
            yield return ArmarVR();
            // El prefab del jugador registra su propio servicio de implantes: el doble va después para ser el actual.
            Registro<IImplantes>.Agregar(implantes);
            agarre = jugador.GetComponent<AgarreImplanteVR>();
            Assert.IsNotNull(agarre, "AgarreImplanteVR debe agregarse solo al armarse el rig VR.");
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            Assert.IsNotNull(molde, "Falta el prefab " + RutaSkull);
            GameObject g = Object.Instantiate(molde, PosicionSkull, Quaternion.Euler(0f, 180f, 0f));
            skull = g.GetComponent<PandilleroAPie>();
            Assert.IsNotNull(skull);
            PonerMano(true, ManoDerInicio, Quaternion.identity);
            yield return Cuadros(4);
            Assert.AreEqual(nivelBrazos, agarre.NivelBrazos, "El nivel de BRAZOS se lee del IImplantes registrado.");
        }

        private IEnumerator AgarrarConDerecha()
        {
            Mantener(controlDer.gripButton);
            yield return Cuadros(2);
        }

        // Mueve la mano derecha en línea recta en <paramref name="segundos"/> y suelta el grip en el último paso (la mano aún va rápido).
        private IEnumerator LanzarConDerecha(Vector3 desde, Vector3 hasta, float segundos)
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                float f = Mathf.Clamp01(t / segundos);
                PonerMano(true, Vector3.Lerp(desde, hasta, f), Quaternion.identity);
                if (f >= 1f)
                {
                    Soltar(controlDer.gripButton);
                    yield return null;
                    yield break;
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConImplante_GripCercaDeUnSkull_LoAgarraYSigueALaMano()
        {
            yield return Preparar(1);
            yield return AgarrarConDerecha();
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(true);
            Assert.IsNotNull(cuerpo, "Con BRAZOS el grip a menos de 0.5 m agarra al SKULL.");
            Assert.AreEqual(skull.gameObject, cuerpo.gameObject);
            Assert.IsFalse(skull.enabled, "Su IA se apaga mientras se sostiene.");
            Assert.IsFalse(skull.GetComponent<CharacterController>().enabled, "Su CharacterController se apaga mientras se sostiene.");
            Assert.IsTrue(cuerpo.Cuerpo.isKinematic);

            Vector3 desfase = skull.transform.position - adaptador.ManoDerecha.transform.position;
            yield return MoverMano(true, ManoDerInicio, ManoDerInicio + new Vector3(0.4f, 0.25f, 0f), 0.5f, Quaternion.identity);
            yield return Cuadros(2);
            Vector3 ahora = skull.transform.position - adaptador.ManoDerecha.transform.position;
            Debug.Log("[VR] Desfase SKULL-mano al agarrar " + desfase.ToString("F3") + ", tras mover la mano " + ahora.ToString("F3"));
            Assert.Less(Vector3.Distance(desfase, ahora), 0.05f, "El SKULL sigue a la mano.");

            // Soltar con la mano quieta: cae, y al quedarse quieto vuelve a su IA en el piso.
            yield return Cuadros(10);
            Soltar(controlDer.gripButton);
            yield return Cuadros(2);
            Assert.IsNull(agarre.Sostenido(true));
            float fin = Time.time + 5f;
            while (skull.GetComponent<CuerpoAgarradoVR>() != null && Time.time < fin)
            {
                yield return null;
            }
            yield return null;
            Assert.IsNull(skull.GetComponent<CuerpoAgarradoVR>(), "El vuelo termina (quieto o a los 3 s).");
            Assert.IsNull(skull.GetComponent<Rigidbody>(), "El Rigidbody temporal se quita.");
            Assert.IsTrue(skull.enabled, "Vuelve a su IA.");
            Assert.IsTrue(skull.GetComponent<CharacterController>().enabled, "Vuelve su CharacterController.");
            Assert.Less(Vector3.Angle(skull.transform.up, Vector3.up), 1f, "Queda derecho.");
            Assert.Less(Mathf.Abs(skull.transform.position.y), 0.3f, "Queda en el piso.");
        }

        [UnityTest]
        public IEnumerator SoltarMoviendoLaManoRapido_LoLanzaYAlChocarConUnaParedPierdeVida()
        {
            yield return Preparar(1);
            Caja(new Vector3(0.2f, 2f, 5.5f), new Vector3(8f, 4f, 1f));
            yield return AgarrarConDerecha();
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(true);
            Assert.IsNotNull(cuerpo);
            float vidaAntes = skull.Actual;

            yield return LanzarConDerecha(ManoDerInicio, ManoDerInicio + new Vector3(0f, 0.15f, 0.6f), 0.15f);
            yield return null;
            Assert.IsNull(agarre.Sostenido(true), "Al soltar el grip ya no lo sostiene.");
            Assert.AreEqual(CuerpoAgarradoVR.Estado.Volando, cuerpo.EstadoActual);
            float rapidez = cuerpo.Cuerpo.linearVelocity.magnitude;
            Debug.Log("[VR] Rapidez del SKULL lanzado: " + rapidez.ToString("0.0") + " m/s, mano " + adaptador.ManoDerecha.Velocidad.magnitude.ToString("0.0"));
            Assert.Greater(rapidez, 8f, "Sale lanzado con la velocidad de la mano por el multiplicador del implante.");
            Assert.Greater(cuerpo.Cuerpo.linearVelocity.z, 0f, "Hacia donde iba la mano.");

            float fin = Time.time + 2f;
            while (cuerpo != null && cuerpo.ChoquesConDanio == 0 && Time.time < fin)
            {
                yield return null;
            }
            Debug.Log("[VR] Vida del SKULL: " + vidaAntes + " -> " + skull.Actual);
            Assert.Less(skull.Actual, vidaAntes, "Chocar rápido contra la pared le quita vida.");
        }

        [UnityTest]
        public IEnumerator SinImplante_ElGripLoSujeta_YAlSoltarloNoSaleLanzado()
        {
            yield return Preparar(0);
            yield return AgarrarConDerecha();
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(true);
            Assert.IsNotNull(cuerpo, "VR35: sin BRAZOS el grip a menos de 0.5 m también sujeta al SKULL.");
            Assert.IsFalse(skull.enabled, "Su IA se pausa mientras se sostiene.");
            yield return LanzarConDerecha(ManoDerInicio, ManoDerInicio + new Vector3(0f, 0.15f, 0.6f), 0.15f);
            yield return null;
            Assert.IsNull(agarre.Sostenido(true), "Al soltar el grip lo libera.");
            Assert.Less(cuerpo.Cuerpo.linearVelocity.magnitude, 2f, "Sin implante no sale lanzado (solo cae).");
        }

        [UnityTest]
        public IEnumerator GolpearConLaOtraManoMientrasLoSostienes_LeBajaVidaYSigueEnLaMano()
        {
            yield return Preparar(1);
            GolpesManosVR golpes = jugador.GetComponent<GolpesManosVR>();
            Assert.IsNotNull(golpes, "GolpesManosVR debe agregarse solo al armarse el rig.");
            yield return AgarrarConDerecha();
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(true);
            Assert.IsNotNull(cuerpo);
            float vidaAntes = skull.Actual;

            Vector3 desde = new Vector3(-0.35f, 1.3f, 0.3f);
            Vector3 hasta = new Vector3(0.3f, 1.3f, 0.8f);
            PonerMano(false, desde, Quaternion.identity);
            yield return Segundos(0.2f); // que no cuente el salto a "desde"
            const int Pasos = 6;
            for (int i = 1; i <= Pasos && golpes.Golpes == 0; i++)
            {
                PonerMano(false, Vector3.Lerp(desde, hasta, (float)i / Pasos), Quaternion.identity);
                yield return null;
            }
            yield return Cuadros(2);
            Debug.Log("[VR] Golpe al sostenido: " + golpes.Golpes + " golpes, vida " + vidaAntes + " -> " + skull.Actual);
            Assert.GreaterOrEqual(golpes.Golpes, 1, "La mano izquierda rápida contra el SKULL sostenido es un golpe.");
            Assert.IsTrue(golpes.UltimoASostenido);
            Assert.IsFalse(golpes.UltimoConDerecha);
            Assert.Less(skull.Actual, vidaAntes, "El golpe le hace daño.");
            Assert.AreEqual(cuerpo, agarre.Sostenido(true), "Sigue en la mano derecha (no sale volando).");
            Soltar(controlDer.gripButton);
            yield return null;
        }
    }
}
