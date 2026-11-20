using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Lo que el prólogo de Tony espera del barrio (T-092): reloj guardable, puntos de historia, apariciones y los prefabs de la casa y el clandestino.</summary>
    public class PrologoMundoPruebas
    {
        private const float MetrosAlPuntoDeUso = 1f;
        private const float MetrosDelCarroAlClandestino = 25f;
        private const float RadioLibre = 0.8f;
        private const float AlturaLibre = 1.3f;     // la esfera queda sobre el asfalto (0.5 m de holgura abajo)
        private const float HoraGuardada = 10.5f;
        private const float Tolerancia = 0.01f;

        [TearDown]
        public void TearDown()
        {
            EscenaLugares.Descargar();
        }

        private static IGuardable Guardable(string clave)
        {
            IGuardable encontrado = null;
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == clave)
                {
                    Assert.IsNull(encontrado, "Una sola clave " + clave);
                    encontrado = g;
                }
            }
            return encontrado;
        }

        private static IPuntoHistoria Historia(string id)
        {
            foreach (IPuntoHistoria p in Registro<IPuntoHistoria>.Todos)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            Assert.Fail("Falta el punto de historia " + id);
            return null;
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            Assert.Fail("Falta la aparición " + id);
            return null;
        }

        private static IInteractuable Interactuable(string nombre)
        {
            foreach (IInteractuable i in Registro<IInteractuable>.Todos)
            {
                if (((Component)i).name.StartsWith(nombre))
                {
                    return i;
                }
            }
            Assert.Fail("Falta el interactuable " + nombre);
            return null;
        }

        private static string Estorbos(Vector3 posicion)
        {
            string nombres = "";
            foreach (Collider c in Physics.OverlapSphere(posicion + Vector3.up * AlturaLibre, RadioLibre, ~0, QueryTriggerInteraction.Ignore))
            {
                nombres += c.transform.root.name + "/" + c.name + " ";
            }
            return nombres;
        }

        private static Bounds Zona(IdLugar id)
        {
            foreach (ZonaInterior zona in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                if (zona.GetComponentInParent<LugarMundo>().Id == id)
                {
                    return zona.GetComponent<BoxCollider>().bounds;
                }
            }
            Assert.Fail("Falta la zona interior de " + id);
            return default;
        }

        [UnityTest]
        public IEnumerator Reloj_EsGuardableConLaClaveReloj_YCargaDiaYHoraSinAvisarDiaNiHoraEntera()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = (RelojMundo)Registro<IRelojMundo>.Actual;
            IGuardable guardable = Guardable("reloj");
            Assert.IsNotNull(guardable, "El reloj se registra como IGuardable con clave reloj");
            Assert.AreSame(reloj, guardable);
            reloj.Pausar(true);
            reloj.AvanzarHasta(HoraGuardada);
            int dia = reloj.Dia;
            string guardado = guardable.Guardar();
            Assert.IsNotEmpty(guardado);

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            int dias = 0;
            int horas = 0;
            int franjas = 0;
            reloj.DiaCambio += _ => dias++;
            reloj.HoraEnteraCambio += _ => horas++;
            reloj.FranjaCambio += _ => franjas++;
            guardable.Cargar(guardado);
            Assert.AreEqual(dia, reloj.Dia);
            Assert.AreEqual(HoraGuardada, reloj.Hora, Tolerancia);
            Assert.AreEqual(0, dias, "Cargar no avisa DiaCambio");
            Assert.AreEqual(0, horas, "Ni HoraEnteraCambio");
            Assert.AreEqual(1, franjas, "Pero sí la franja, para que las luces se pongan al día");
        }

        [UnityTest]
        public IEnumerator Reloj_CargarUnTextoQueNoSeEntiende_AvisaYNoCambiaNada()
        {
            yield return EscenaLugares.CargarCiudad();
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            float hora = reloj.Hora;
            int dia = reloj.Dia;
            reloj.Pausar(true);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("guardado del reloj"));
            Guardable("reloj").Cargar("esto no es json");
            Assert.AreEqual(dia, reloj.Dia);
            Assert.AreEqual(hora, reloj.Hora, Tolerancia);
        }

        [UnityTest]
        public IEnumerator PuntosDeHistoria_CarroYEsquina_EstanEnLaCalleYLibres()
        {
            yield return EscenaLugares.CargarCiudad();
            Pose carro = Historia("Prologo_Carro").Pose;
            Vector3 entrada = EscenaLugares.Lugar(IdLugar.Clandestino).PosicionEntrada;
            Assert.Less(Vector3.Distance(carro.position, entrada), MetrosDelCarroAlClandestino, "El carro queda cerca del clandestino");
            Assert.AreEqual(0, Physics.OverlapSphere(carro.position + Vector3.up * AlturaLibre, RadioLibre, ~0, QueryTriggerInteraction.Ignore).Length, "Prologo_Carro está libre; choca con: " + Estorbos(carro.position));
            Assert.IsTrue(Physics.Raycast(carro.position + Vector3.up, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore), "Con piso debajo");
            Pose esquina = Historia("Prologo_Esquina_1").Pose;
            Assert.IsFalse(Physics.CheckSphere(esquina.position + Vector3.up * AlturaLibre, RadioLibre, ~0, QueryTriggerInteraction.Ignore), "Prologo_Esquina_1 está libre");
            Assert.IsTrue(Physics.Raycast(esquina.position + Vector3.up, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore));
            Historia("Prologo_Carro").Activar(true);   // no debe lanzar aunque no tenga objeto
            Historia("Prologo_Carro").Activar(false);
        }

        [UnityTest]
        public IEnumerator Apariciones_CasaCamaYClandestinoSalida_EstanDondeDeben()
        {
            yield return EscenaLugares.CargarCiudad();
            IPuntoAparicion cama = Aparicion("Casa_Cama");
            Assert.AreEqual(ModoJugador.APie, cama.Modo);
            Assert.IsTrue(Zona(IdLugar.Casa).Contains(cama.Pose.position + Vector3.up), "Casa_Cama está dentro de la casa");
            IPuntoAparicion salida = Aparicion("Clandestino_Salida");
            Assert.AreEqual(ModoJugador.APie, salida.Modo);
            Assert.Less(Vector3.Distance(salida.Pose.position, EscenaLugares.Lugar(IdLugar.Clandestino).PosicionEntrada), 0.1f, "Clandestino_Salida está en la Entrada");
            Assert.IsNotNull(Aparicion("Casa_Puerta"));
        }

        [UnityTest]
        public IEnumerator Interactuables_CamaYEspejo_EstanColocadosYRespondenSinErrores()
        {
            yield return EscenaLugares.CargarCiudad();
            IInteractuable cama = Interactuable("CamaJugador");
            IInteractuable espejo = Interactuable("EspejoJugador");
            Bounds casa = Zona(IdLugar.Casa);
            Assert.IsTrue(casa.Contains(cama.Posicion + Vector3.up), "La cama está en la casa");
            Assert.IsTrue(casa.Contains(espejo.Posicion + Vector3.up), "El espejo está en la casa");
            Vector3 puntoCama = GameObject.Find("Prop_Cama").transform.Find("PuntoUso").position;
            Assert.Less(Vector3.Distance(cama.Posicion, puntoCama), MetrosAlPuntoDeUso, "La cama se usa desde el punto de uso del prop");
            foreach (IInteractuable i in new[] { cama, espejo })
            {
                Assert.IsNotNull(i.Texto);
                Assert.DoesNotThrow(() => { bool puede = i.PuedeUsar; });
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
