using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core.Trepar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1361: recorridos en Play por partes del Puerto (prefab T1357_Puerto solo): a pie por las 4 calles y por 4 pasillos entre bloques,
    /// en carro por las calles en circuito, y parkour (trepar a los bloques por Escalera_Suelo y Escalera_Fila, saltar entre contenedores, bajar).
    /// </summary>
    public class RecorridoPuertoT1361Pruebas : BaseRecorridoZonaT1361
    {
        private const string PrefabPuerto = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        private const float VacioPuerto = -3f;
        private const float EscalaLarga = 2f;
        private const float RapidezCalle = 9f;
        private const float RapidezAPie = 5.5f;
        private const float HolguraSegundos = 20f;
        private const int CallesPuerto = 4;
        private const int PasillosAProbar = 4;
        private const float PasilloMinimo = 2.8f;
        private const float PasilloMaximo = 4.5f;
        private const float TraslapeMinimo = 10f;
        private const float MargenExtremoPasillo = 0.5f;
        private const int BloquesAProbar = 4;
        private const int BloquesConSaltos = 3;
        private const float HuecoMaximoContenedores = 3f;
        private const float DesnivelMaximoContenedores = 2.7f;
        private const float HuecoMinimoSalto = 0.3f;
        private const float MargenAlturaFila = 0.8f;
        private const float DistanciaCaminarAFila = 15f;
        private const float DistanciaAntesDeEscalera = 1.5f;
        private const float AlcanceBajada = 6f;
        private const float AlturaSueloBajada = 0.5f;
        private const float SegundosBajada = 10f;

        protected override string RutaZona => PrefabPuerto;
        protected override float AlturaVacio => VacioPuerto;

        // ------------------------------------------------------------------------------------------------------------ a pie

        private IEnumerator CaminarParte(RutaRecorridoT1361 ruta)
        {
            yield return PonerEnRuta(ruta);
            Time.timeScale = EscalaLarga;
            yield return Caminar(ruta, ruta.Largo / RapidezAPie + HolguraSegundos);
            Time.timeScale = 1f;
        }

        private IEnumerator CalleAPie(int numero)
        {
            yield return Preparar();
            yield return CaminarParte(RutaDe("Calle_" + numero));
            AfirmarParte("Calle_" + numero);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Calle1() { yield return CalleAPie(1); }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Calle2() { yield return CalleAPie(2); }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Calle3() { yield return CalleAPie(3); }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Calle4() { yield return CalleAPie(4); }

        private List<Transform> Bloques()
        {
            return zona.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Bloque_"))
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
        }

        private static Bounds CajaDe(Transform raiz)
        {
            Bounds caja = new Bounds(raiz.position, Vector3.zero);
            bool primero = true;
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger)
                {
                    continue;
                }
                if (primero)
                {
                    caja = c.bounds;
                    primero = false;
                }
                else
                {
                    caja.Encapsulate(c.bounds);
                }
            }
            return caja;
        }

        /// <summary>Pasillos entre bloques vecinos (hueco de 2.8 a 4.5 m): la línea del centro de un extremo al otro del traslape, de los más largos.</summary>
        private List<RutaRecorridoT1361> Pasillos()
        {
            List<Transform> bloques = Bloques();
            List<Bounds> cajas = bloques.Select(CajaDe).ToList();
            List<(RutaRecorridoT1361 ruta, bool alolargoX)> lista = new List<(RutaRecorridoT1361 ruta, bool alolargoX)>();
            for (int i = 0; i < cajas.Count; i++)
            {
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    Bounds a = cajas[i];
                    Bounds b = cajas[j];
                    float traslapeX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float traslapeZ = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                    float huecoZ = Mathf.Max(b.min.z - a.max.z, a.min.z - b.max.z);
                    float huecoX = Mathf.Max(b.min.x - a.max.x, a.min.x - b.max.x);
                    if (traslapeX >= TraslapeMinimo && huecoZ >= PasilloMinimo && huecoZ <= PasilloMaximo)
                    {
                        float z = huecoZ > 0f && b.min.z > a.max.z ? (a.max.z + b.min.z) * 0.5f : (b.max.z + a.min.z) * 0.5f;
                        float x0 = Mathf.Max(a.min.x, b.min.x) + MargenExtremoPasillo;
                        float x1 = Mathf.Min(a.max.x, b.max.x) - MargenExtremoPasillo;
                        lista.Add((new RutaRecorridoT1361(new[] { new Vector3(x0, 0f, z), new Vector3(x1, 0f, z) }), true));
                    }
                    else if (traslapeZ >= TraslapeMinimo && huecoX >= PasilloMinimo && huecoX <= PasilloMaximo)
                    {
                        float x = b.min.x > a.max.x ? (a.max.x + b.min.x) * 0.5f : (b.max.x + a.min.x) * 0.5f;
                        float z0 = Mathf.Max(a.min.z, b.min.z) + MargenExtremoPasillo;
                        float z1 = Mathf.Min(a.max.z, b.max.z) - MargenExtremoPasillo;
                        lista.Add((new RutaRecorridoT1361(new[] { new Vector3(x, 0f, z0), new Vector3(x, 0f, z1) }), false));
                    }
                }
            }
            // Mitad a lo largo de x y mitad a lo largo de z si los hay; dentro de cada grupo, los más largos.
            List<RutaRecorridoT1361> elegidos = new List<RutaRecorridoT1361>();
            elegidos.AddRange(lista.Where(p => p.alolargoX).OrderByDescending(p => p.ruta.Largo).Take(PasillosAProbar / 2).Select(p => p.ruta));
            elegidos.AddRange(lista.Where(p => !p.alolargoX).OrderByDescending(p => p.ruta.Largo).Take(PasillosAProbar / 2).Select(p => p.ruta));
            foreach (RutaRecorridoT1361 r in lista.OrderByDescending(p => p.ruta.Largo).Select(p => p.ruta))
            {
                if (elegidos.Count >= PasillosAProbar)
                {
                    break;
                }
                if (!elegidos.Contains(r))
                {
                    elegidos.Add(r);
                }
            }
            return elegidos;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_CuatroPasillosEntreBloques_DeUnExtremoAOtro()
        {
            yield return Preparar();
            List<RutaRecorridoT1361> pasillos = Pasillos();
            Assert.AreEqual(PasillosAProbar, pasillos.Count, "Se esperaban " + PasillosAProbar + " pasillos entre bloques (hueco 2.8 a 4.5 m)");
            int fallidos = 0;
            for (int i = 0; i < pasillos.Count; i++)
            {
                int atascosAntes = atascos;
                yield return PonerEnRuta(pasillos[i]);
                yield return Caminar(pasillos[i], pasillos[i].Largo / RapidezAPie + HolguraSegundos, 1.5f, 2f);
                Nota("pasillo " + i + " de " + pasillos[i].Largo.ToString("F1") + " m: " + (llego ? "llega" : "NO llega") + ", atascos " + (atascos - atascosAntes));
                if (!llego || atascos - atascosAntes > MaxAtascosPorParte)
                {
                    fallidos++;
                }
            }
            Assert.AreEqual(0, caidas, "Caídas al vacío en los pasillos");
            Assert.AreEqual(0, fallidos, "Pasillos que no se recorren (ver Logs/t1361-recorrido-*.txt)");
        }

        // ------------------------------------------------------------------------------------------------------------ en carro

        /// <summary>Cada calle por separado, de inicio a fin; para a 8 m de un extremo que está en el borde de la zona (unión con otra zona).</summary>
        private IEnumerator CalleEnCarro(int numero)
        {
            yield return Preparar();
            RutaRecorridoT1361 ruta = SinBordes(RutaDe("Calle_" + numero));
            CrearCarro(ruta);
            yield return Esperar(1f);
            Time.timeScale = EscalaLarga;
            yield return Manejar(ruta, ruta.Largo / (RapidezCalle * 0.5f) + HolguraSegundos, RapidezCalle);
            Time.timeScale = 1f;
            AfirmarParteCarro("Calle_" + numero);
        }

        [UnityTest, Timeout(500000)]
        public IEnumerator Carro_Calle1() { yield return CalleEnCarro(1); }

        [UnityTest, Timeout(500000)]
        public IEnumerator Carro_Calle2() { yield return CalleEnCarro(2); }

        [UnityTest, Timeout(500000)]
        public IEnumerator Carro_Calle3() { yield return CalleEnCarro(3); }

        [UnityTest, Timeout(500000)]
        public IEnumerator Carro_Calle4() { yield return CalleEnCarro(4); }

        // ------------------------------------------------------------------------------------------------------------ parkour

        private List<Transform> BloquesRepartidos(int cuantos)
        {
            List<Transform> todos = Bloques();
            Assert.GreaterOrEqual(todos.Count, cuantos, "Se esperaban al menos " + cuantos + " bloques de contenedores");
            return Enumerable.Range(0, cuantos).Select(i => todos[i * todos.Count / cuantos]).ToList();
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Parkour_TreparSueloYFila_YBajar()
        {
            yield return Preparar();
            foreach (Transform bloque in BloquesRepartidos(BloquesAProbar))
            {
                List<PuntoTrepar> puntos = bloque.GetComponentsInChildren<PuntoTrepar>(true).ToList();
                PuntoTrepar suelo = puntos.FirstOrDefault(p => p.name.StartsWith("Escalera_Suelo"));
                if (suelo == null)
                {
                    treparFallidos++;
                    Nota(bloque.name + ": sin Escalera_Suelo");
                    continue;
                }
                yield return Subir(suelo);
                Vector3 llegada = jugador.transform.position;
                PuntoTrepar fila = puntos
                    .Where(p => p.name.StartsWith("Escalera_Fila") && Mathf.Abs(p.transform.position.y - llegada.y) < MargenAlturaFila)
                    .OrderBy(p => Plano(p.transform.position, llegada)).FirstOrDefault();
                if (fila == null)
                {
                    Nota(bloque.name + ": no hay Escalera_Fila a la altura " + llegada.y.ToString("F1") + " de la azotea del primer contenedor");
                }
                else
                {
                    Vector3 pie = fila.PieAbajo - fila.Frente * DistanciaAntesDeEscalera;
                    if (Plano(pie, llegada) < DistanciaCaminarAFila)
                    {
                        RutaRecorridoT1361 caminata = new RutaRecorridoT1361(new[] { llegada, pie });
                        yield return Caminar(caminata, caminata.Largo / RapidezAPie + HolguraSegundos, 1.5f, 2f, false);
                        if (!llego)
                        {
                            Nota(bloque.name + ": no se llega caminando por arriba a " + fila.name + " (se sube desde su pie)");
                        }
                    }
                    yield return Subir(fila);
                }
                // Bajar: seguir caminando hacia afuera hasta quedar en el patio (y < 0.5) o 10 s; un escalón sobre otro contenedor no es falla.
                Vector3 centro = CajaDe(bloque).center;
                float limiteBajada = Time.time + SegundosBajada;
                while (Time.time < limiteBajada && !(controlador.EnPiso && jugador.transform.position.y < AlturaSueloBajada))
                {
                    Vector3 desde = jugador.transform.position;
                    Vector3 fuera = desde - centro;
                    fuera.y = 0f;
                    fuera = fuera.sqrMagnitude > 0f ? fuera.normalized : Vector3.forward;
                    RutaRecorridoT1361 bajada = new RutaRecorridoT1361(new[] { desde, desde + fuera * AlcanceBajada });
                    yield return Caminar(bajada, Mathf.Min(3f, limiteBajada - Time.time), 1f, 2f, false);
                }
                yield return Esperar(1f);
                if (!controlador.EnPiso || jugador.transform.position.y > AlturaSueloBajada)
                {
                    treparFallidos++;
                    Hallazgo("BAJAR de " + bloque.name + ": no queda en el patio (y " + jugador.transform.position.y.ToString("F2") + ")", jugador.transform.position, true);
                }
            }
            Assert.AreEqual(0, caidas, "Caídas al vacío");
            Assert.AreEqual(0, treparFallidos, "Subidas o bajadas que fallan (ver Logs/t1361-recorrido-*.txt)");
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Parkour_SaltarEntreContenedoresDelMismoBloque()
        {
            yield return Preparar();
            int pares = 0;
            foreach (Transform bloque in BloquesRepartidos(BloquesConSaltos))
            {
                List<Superficie> contenedores = new List<Superficie>();
                foreach (Transform t in bloque.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("PT01_Contenedor"))
                    {
                        continue;
                    }
                    Collider col = t.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => !c.isTrigger);
                    if (col != null)
                    {
                        contenedores.Add(new Superficie { Nombre = bloque.name + "/" + t.name + "@" + t.position.y.ToString("F1"), Caja = col.bounds });
                    }
                }
                // Solo contenedores expuestos (nada encima) y vecinos inmediatos (el punto medio no cae sobre otro contenedor).
                List<Superficie> expuestos = contenedores.Where(c => !contenedores.Any(o => o.Nombre != c.Nombre && o.Techo > c.Techo + 0.5f
                    && Mathf.Abs(o.Centro.x - c.Centro.x) < o.Caja.extents.x + c.Caja.extents.x - 0.1f
                    && Mathf.Abs(o.Centro.z - c.Centro.z) < o.Caja.extents.z + c.Caja.extents.z - 0.1f)).ToList();
                List<(Superficie a, Superficie b)> candidatos = new List<(Superficie a, Superficie b)>();
                for (int i = 0; i < expuestos.Count; i++)
                {
                    for (int j = 0; j < expuestos.Count; j++)
                    {
                        if (i == j || Hueco(expuestos[i], expuestos[j]) > HuecoMaximoContenedores
                            || Mathf.Abs(expuestos[i].Techo - expuestos[j].Techo) > DesnivelMaximoContenedores
                            || Plano(expuestos[i].Centro, expuestos[j].Centro) <= 0.5f)
                        {
                            continue;
                        }
                        Superficie sa = expuestos[i];
                        Superficie sb = expuestos[j];
                        Vector3 medio = (sa.Centro + sb.Centro) * 0.5f;
                        bool atravesado = contenedores.Any(o => o.Nombre != sa.Nombre && o.Nombre != sb.Nombre
                            && Mathf.Abs(medio.x - o.Centro.x) < o.Caja.extents.x && Mathf.Abs(medio.z - o.Centro.z) < o.Caja.extents.z);
                        if (!atravesado)
                        {
                            candidatos.Add((sa, sb));
                        }
                    }
                }
                if (candidatos.Count == 0)
                {
                    Nota(bloque.name + ": sin contenedores contiguos");
                    continue;
                }
                (Superficie a, Superficie b) par = candidatos
                    .OrderBy(c => Hueco(c.a, c.b) < HuecoMinimoSalto ? 1 : 0)
                    .ThenBy(c => Mathf.Abs(c.a.Techo - c.b.Techo))
                    .ThenBy(c => Hueco(c.a, c.b)).First();
                pares++;
                if (jugador == null)
                {
                    CrearJugador(new Vector3(par.a.Centro.x, par.a.Techo + 0.05f, par.a.Centro.z), Vector3.forward);
                    yield return Esperar(0.5f);
                }
                yield return SaltarEntre(par.a, par.b);
            }
            Assert.Greater(pares, 0, "Ningún bloque tiene dos contenedores contiguos para saltar");
            Assert.AreEqual(0, saltosFallidos, "Saltos entre contenedores que fallan (ver Logs/t1361-recorrido-*.txt)");
        }
    }
}
