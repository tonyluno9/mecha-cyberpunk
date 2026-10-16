using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-082 en Ciudad.unity: letreros de neón del barrio (cantidad, apoyo, locales, día y noche, parpadeo).</summary>
    public class LetrerosPruebas
    {
        private const int Minimo = 30;
        private const int Maximo = 50;
        private const float DistanciaPared = 0.5f;
        private const float AlturaMinima = 3f;
        private const float SalienteMaximaBandera = 1.5f;
        private const float AlturaApoyoAzotea = 0.1f;
        private const float DistanciaLocal = 8f;
        private const float AlturaOjos = 1.7f;
        private const float DiezSegundos = 10f;
        private const float NivelCerrado = 0.35f;
        private const float Tolerancia = 0.001f;
        private const float DeNocheSobreDeDia = 2f;
        private static readonly string[] NombresBandera = { "_Hotel", "_Ciber" };

        private RelojMundo reloj;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            yield return EscenaLugares.Calentar();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            EscenaLugares.Descargar();
        }

        private static ParpadeoNeon[] Letreros()
        {
            return Object.FindObjectsByType<ParpadeoNeon>(FindObjectsSortMode.None).OrderBy(l => l.name).ToArray();
        }

        private static bool EsBandera(ParpadeoNeon letrero)
        {
            return NombresBandera.Any(n => letrero.name.EndsWith(n));
        }

        private static bool EsDeAzotea(ParpadeoNeon letrero)
        {
            return letrero.name.EndsWith("_Hotel_Azotea");
        }

        private static Bounds Limites(ParpadeoNeon letrero)
        {
            Renderer[] renderers = letrero.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static IEnumerable<Vector3> Esquinas(Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            }
        }

        [UnityTest]
        public IEnumerator Cantidad_HayEntre30Y50Letreros_YNingunoGreyboxEnLosEdificios()
        {
            int total = Letreros().Length;
            Assert.GreaterOrEqual(total, Minimo, "Faltan letreros de neón");
            Assert.LessOrEqual(total, Maximo, "Sobran letreros de neón");
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform[] greybox = todos.Where(t => t.name == "Letrero" && t.parent != null && t.parent.name.StartsWith("Edificio_")).ToArray();
            Assert.IsEmpty(greybox, "Los edificios no deben tener cubos 'Letrero' greybox");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Variedad_HayBanderasPlanosYDeAzotea()
        {
            ParpadeoNeon[] letreros = Letreros();
            Assert.IsTrue(letreros.Any(EsBandera), "Faltan letreros de bandera");
            Assert.IsTrue(letreros.Any(EsDeAzotea), "Faltan letreros de azotea");
            Assert.IsTrue(letreros.Any(l => !EsBandera(l) && !EsDeAzotea(l)), "Faltan letreros planos");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Apoyo_CadaLetreroEstaPegadoAUnaFachadaOAzotea_YNoSaleDeMas()
        {
            foreach (ParpadeoNeon letrero in Letreros())
            {
                Transform t = letrero.transform;
                Assert.GreaterOrEqual(t.position.y, AlturaMinima, letrero.name + " está muy bajo");
                RaycastHit golpe;
                if (EsDeAzotea(letrero))
                {
                    Assert.IsTrue(Physics.Raycast(t.position + Vector3.up * AlturaApoyoAzotea, Vector3.down, out golpe, DistanciaPared, ~0, QueryTriggerInteraction.Ignore),
                        letrero.name + ": no hay azotea debajo");
                    Assert.AreEqual("Cuerpo", golpe.collider.name, letrero.name);
                    continue;
                }
                Assert.IsTrue(Physics.Raycast(t.position, -t.forward, out golpe, DistanciaPared, ~0, QueryTriggerInteraction.Ignore),
                    letrero.name + ": no hay fachada a menos de 0.5 m hacia atrás");
                if (EsBandera(letrero))
                {
                    float saliente = Esquinas(Limites(letrero)).Max(c => Vector3.Dot(c - golpe.point, t.forward));
                    Assert.LessOrEqual(saliente, SalienteMaximaBandera, letrero.name + " sale demasiado de la fachada");
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Locales_ElBarElClandestinoYElCasinoTienenSuLetreroCerca_YElDeHorarioSeVe()
        {
            (IdLugar id, string nombre)[] esperados = { (IdLugar.Bar, "Cantina"), (IdLugar.Clandestino, "Peleas"), (IdLugar.Casino, "Casino") };
            ParpadeoNeon[] letreros = Letreros();
            foreach ((IdLugar id, string nombre) in esperados)
            {
                ILugar lugar = EscenaLugares.Lugar(id);
                Vector3 entrada = lugar.PosicionEntrada;
                ParpadeoNeon suyo = letreros.FirstOrDefault(l => l.name.EndsWith("_" + nombre));
                Assert.IsNotNull(suyo, id + ": falta su letrero " + nombre);
                Assert.Less(Vector3.Distance(Limites(suyo).center, entrada), DistanciaLocal, id + ": el letrero está lejos de la entrada");

                LetreroLugar horario = ((Component)lugar).GetComponentInChildren<LetreroLugar>();
                Assert.IsNotNull(horario, id + ": perdió su letrero de horario");
                Vector3 desde = entrada + Vector3.up * AlturaOjos;
                Vector3 hasta = horario.transform.position;
                Assert.IsFalse(Physics.Linecast(desde, hasta, ~0, QueryTriggerInteraction.Ignore), id + ": algo tapa el letrero de horario");
                Vector3 direccion = hasta - desde;
                foreach (ParpadeoNeon letrero in letreros)
                {
                    bool tapa = Limites(letrero).IntersectRay(new Ray(desde, direccion.normalized), out float distancia) && distancia < direccion.magnitude;
                    Assert.IsFalse(tapa, id + ": el letrero " + letrero.name + " tapa el de horario");
                }
            }
            yield return null;
        }

        private float EmisionTotal()
        {
            return Letreros().Sum(l => l.EmisionActual);
        }

        [UnityTest]
        public IEnumerator Emision_A22EsMayorQueA12()
        {
            reloj.AvanzarHasta(EscenaLugares.HoraMediodia);
            yield return null;
            float dia = EmisionTotal();
            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            float noche = EmisionTotal();
            Assert.Greater(dia, 0f, "De día los letreros no deben quedar en negro del todo");
            Assert.Greater(noche, dia * DeNocheSobreDeDia, "De noche los letreros deben brillar bastante más que de día");
        }

        [UnityTest]
        public IEnumerator Casino_CerradoBrillaMenosQueLosDemasDeNoche()
        {
            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            ParpadeoNeon casino = Letreros().First(l => l.name.EndsWith("_Casino"));
            Assert.IsTrue(casino.Cerrado);
            Assert.AreEqual(NivelCerrado, casino.FactorActual, Tolerancia);
            ParpadeoNeon fijo = Letreros().First(l => !l.Parpadea && !l.Cerrado);
            Assert.AreEqual(1f, fijo.FactorActual, Tolerancia, fijo.name + " de noche, a pleno");
        }

        [UnityTest]
        public IEnumerator Parpadeo_UnoDeCada6_YCambianEnDiezSegundos()
        {
            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            ParpadeoNeon[] letreros = Letreros();
            ParpadeoNeon[] viejos = letreros.Where(l => l.Parpadea).ToArray();
            Assert.GreaterOrEqual(viejos.Length, letreros.Length / 6, "Debe parpadear uno de cada seis");
            Assert.LessOrEqual(viejos.Length, letreros.Length / 6 + 2);
            Dictionary<ParpadeoNeon, HashSet<float>> vistos = viejos.ToDictionary(l => l, l => new HashSet<float> { l.FactorActual });
            float fin = Time.time + DiezSegundos;
            while (Time.time < fin)
            {
                yield return null;
                foreach (ParpadeoNeon viejo in viejos)
                {
                    vistos[viejo].Add(viejo.FactorActual);
                }
            }
            foreach (ParpadeoNeon viejo in viejos)
            {
                Assert.GreaterOrEqual(vistos[viejo].Count, 2, viejo.name + " no cambió en 10 s");
            }
        }
    }

}
