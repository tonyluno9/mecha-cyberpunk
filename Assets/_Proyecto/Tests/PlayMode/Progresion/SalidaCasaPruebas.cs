using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-804: caminar por las puertas reales de la casa (la del cuarto y la de la calle) y por el portón del taller, sin teletransporte.
    /// Atajo anotado: en Ciudad_Prologo la W y la E simuladas no le llegan al jugador mientras está la pantalla de inicio de S1 (las pruebas de
    /// recorrido de la base fallan por eso), así que se camina con el CharacterController del propio jugador (choca con todo igual que con W) y la
    /// puerta se abre con <see cref="IPuerta.IntentarAbrir"/>, que es lo que hace la E, comprobando antes que la E la ofrece (PuertaCandidata).
    /// </summary>
    public static class SalidaCasaPruebas
    {
        private const float SegundosTramo = 12f;
        private const float Llegada = 0.3f;
        private const float SegundosApertura = 1.1f;
        private const float Velocidad = 2.5f;
        private const float Gravedad = 2f;
        /// <summary>Distancia a la hoja del lado del cuarto: el ropero queda junto a la puerta, a su derecha.</summary>
        public const float FrenteCuarto = 0.55f;

        /// <summary>Del cuarto a la banqueta: puerta del cuarto, pasillo, puerta de la calle.</summary>
        public static IEnumerator Salir(Func<bool> hasta = null)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            IPuerta cuarto = PuertaDeLaCasa(true);
            IPuerta calle = PuertaDeLaCasa(false);
            yield return Caminar(jugador, cuarto.Posicion + Vector3.forward * FrenteCuarto + Vector3.left * 0.9f, hasta);
            yield return Caminar(jugador, cuarto.Posicion + Vector3.forward * FrenteCuarto, hasta);
            if (Corta(hasta)) yield break;
            yield return Abrir(jugador, cuarto, "la puerta del cuarto");
            yield return Caminar(jugador, cuarto.Posicion + Vector3.back * 1.0f, hasta);
            yield return Caminar(jugador, calle.Posicion + Vector3.left * 1.1f, hasta);
            if (Corta(hasta)) yield break;
            yield return Abrir(jugador, calle, "la puerta de la calle");
            yield return Caminar(jugador, calle.Posicion + Vector3.right * 2.5f, hasta);
        }

        private static bool Corta(Func<bool> hasta) => hasta != null && hasta();

        /// <summary>La puerta de la casa del cuarto (la más cercana al cuarto) o la de la calle.</summary>
        public static IPuerta PuertaDeLaCasa(bool delCuarto)
        {
            Vector3 cuarto = GameObject.Find("Cuarto_Habitacion").transform.position;
            IPuerta[] puertas = Registro<IPuerta>.Todos.Where(p => p.Lugar == IdLugar.Casa).OrderBy(p => Vector3.Distance(p.Posicion, cuarto)).ToArray();
            Assert.AreEqual(2, puertas.Length, "La casa tiene la puerta del cuarto y la de la calle");
            return delCuarto ? puertas[0] : puertas[1];
        }

        /// <summary>Mira la puerta, comprueba que la E la ofrece y la abre como la E.</summary>
        public static IEnumerator Abrir(IJugador jugador, IPuerta puerta, string que)
        {
            Vector3 rumbo = Plano(puerta.Posicion - jugador.Raiz.position);
            jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(rumbo.normalized, Vector3.up)));
            yield return null;
            yield return null;
            InteraccionAPie interaccion = ((Component)jugador).GetComponent<InteraccionAPie>();
            Assert.AreSame(puerta, interaccion.PuertaCandidata, "La E ofrece " + que);
            Assert.AreEqual(ResultadoPuerta.Abierta, puerta.IntentarAbrir(), "Abre " + que);
            Assert.IsTrue(puerta.EstaAbierta);
            float fin = Time.time + SegundosApertura;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        /// <summary>Camina con el CharacterController del jugador hasta 'destino'; falla si algo lo detiene (una puerta cerrada, un mueble).</summary>
        public static IEnumerator Caminar(IJugador jugador, Vector3 destino, Func<bool> hasta = null)
        {
            CharacterController cc = ((Component)jugador).GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "El jugador camina con su CharacterController");
            float fin = Time.time + SegundosTramo;
            while (Plano(destino - jugador.Raiz.position).magnitude > Llegada && !Corta(hasta))
            {
                Assert.Less(Time.time, fin, "No llegó caminando a " + destino + " (está en " + jugador.Raiz.position + ")");
                Vector3 paso = Plano(destino - jugador.Raiz.position);
                paso = Vector3.ClampMagnitude(paso.normalized * Velocidad * Time.deltaTime, paso.magnitude);
                cc.Move(paso + Vector3.down * Gravedad * Time.deltaTime);
                yield return null;
            }
        }

        public static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
