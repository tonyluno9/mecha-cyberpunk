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
    /// T-1361: recorridos en Play por partes de la Colonia del Cerro (prefab T1355_Cerro solo): a pie por la calle base y el zigzag,
    /// subir y bajar las 3 escaleras largas, en carro por la base y el zigzag, y parkour por cadenas de azoteas y PuntoTrepar de acceso.
    /// Corre en su propia clase: las pruebas de policía/IA rompen la entrada de las de Personaje en la misma corrida.
    /// </summary>
    public class RecorridoCerroT1361Pruebas : BaseRecorridoZonaT1361
    {
        private const string PrefabCerro = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        private const float VacioCerro = -5f;
        private const float EscalaLarga = 2f;
        private const float RapidezCalle = 10f;
        private const float RapidezAPie = 5.5f;
        private const float HolguraSegundos = 20f;
        private const int PartesZigzag = 3;
        private const int AzoteasPorCadena = 5;
        private const float HuecoMaximoCadena = 4f;
        private const float DesnivelMaximoCadena = 3f;
        private const float PenalizaContramarcha = 5f;
        private const float MargenAlturaPierna = 0.5f;
        private const float DistanciaReferenciaPierna = 10f;
        private const float RadioLlegadaEscalera = 2.5f;
        private const float AntesYDespuesEscalera = 2f;
        private const int MaxAccesos = 4;

        protected override string RutaZona => PrefabCerro;
        protected override float AlturaVacio => VacioCerro;

        private struct EscaleraLarga
        {
            public Vector3 Bajo;
            public Vector3 Alto;
            public Vector3 Direccion;
        }

        // ------------------------------------------------------------------------------------------------------------ a pie

        private IEnumerator CaminarParte(RutaRecorridoT1361 ruta, float escala)
        {
            yield return PonerEnRuta(ruta);
            Time.timeScale = escala;
            float maxSegundos = ruta.Largo / RapidezAPie + HolguraSegundos;
            yield return Caminar(ruta, maxSegundos);
            Time.timeScale = 1f;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_CalleBase_DeExtremoAExtremo()
        {
            yield return Preparar();
            RutaRecorridoT1361 ruta = SinBordes(RutaDe("CalleZona_Base"));
            yield return CaminarParte(ruta, EscalaLarga);
            AfirmarParte("calle base");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Zigzag_Pierna1()
        {
            yield return Preparar();
            yield return CaminarParte(RutaDe("CalleZona_Zigzag").Partir(PartesZigzag)[0], EscalaLarga);
            AfirmarParte("zigzag parte 1");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Zigzag_Pierna2()
        {
            yield return Preparar();
            yield return CaminarParte(RutaDe("CalleZona_Zigzag").Partir(PartesZigzag)[1], EscalaLarga);
            AfirmarParte("zigzag parte 2");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APie_Zigzag_Pierna3YPlaza()
        {
            yield return Preparar();
            yield return CaminarParte(RutaDe("CalleZona_Zigzag").Partir(PartesZigzag)[2], EscalaLarga);
            AfirmarParte("zigzag parte 3 y plaza");
        }

        // ------------------------------------------------------------------------------------------------------------ escaleras largas

        /// <summary>Las escaleras CE01 agrupadas por su z (una por cada escalera larga), de norte a sur.</summary>
        private List<EscaleraLarga> EscalerasLargas()
        {
            Transform escaleras = zona.transform.Find("Escaleras");
            Assert.IsNotNull(escaleras, "El prefab del Cerro debe traer el grupo Escaleras");
            SortedDictionary<int, List<Renderer>> grupos = new SortedDictionary<int, List<Renderer>>();
            foreach (Renderer r in escaleras.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.parent == null || !r.transform.parent.name.StartsWith("CE01_EscaleraCerro"))
                {
                    continue;
                }
                int clave = Mathf.RoundToInt(r.bounds.center.z);
                if (!grupos.ContainsKey(clave))
                {
                    grupos[clave] = new List<Renderer>();
                }
                grupos[clave].Add(r);
            }
            List<EscaleraLarga> lista = new List<EscaleraLarga>();
            foreach (KeyValuePair<int, List<Renderer>> par in grupos.Reverse())
            {
                if (par.Value.Count < 5)
                {
                    continue;
                }
                Renderer bajo = par.Value.OrderBy(r => r.bounds.center.y).First();
                Renderer alto = par.Value.OrderByDescending(r => r.bounds.center.y).First();
                Vector3 b = new Vector3(bajo.bounds.center.x, bajo.bounds.max.y, bajo.bounds.center.z);
                Vector3 a = new Vector3(alto.bounds.center.x, alto.bounds.max.y, alto.bounds.center.z);
                Vector3 d = a - b;
                d.y = 0f;
                lista.Add(new EscaleraLarga { Bajo = b, Alto = a, Direccion = d.normalized });
            }
            return lista;
        }

        private IEnumerator SubirYBajar(int indice)
        {
            yield return Preparar();
            List<EscaleraLarga> lista = EscalerasLargas();
            Assert.GreaterOrEqual(lista.Count, indice + 1, "Se esperaban al menos " + (indice + 1) + " escaleras largas CE01 y hay " + lista.Count);
            EscaleraLarga e = lista[indice];
            RutaRecorridoT1361 calleBase = RutaDe("CalleZona_Base");
            RutaRecorridoT1361 zigzag = RutaDe("CalleZona_Zigzag");
            Vector3 entrada = calleBase.PuntoEn(calleBase.Proyectar(e.Bajo, 0f, calleBase.Largo));
            Vector3 pieEscalera = e.Bajo - e.Direccion * AntesYDespuesEscalera;
            Vector3 cima = e.Alto + e.Direccion * AntesYDespuesEscalera;
            RutaRecorridoT1361 subida = new RutaRecorridoT1361(new[] { entrada, pieEscalera, cima });

            yield return PonerEnRuta(subida);
            float yBase = jugador.transform.position.y;
            yield return Caminar(subida, subida.Largo / RapidezAPie + HolguraSegundos, RadioLlegadaEscalera);
            bool subio = llego;
            float yArriba = jugador.transform.position.y;
            float sZig = zigzag.Proyectar(cima, 0f, zigzag.Largo, out float distanciaPierna);
            if (distanciaPierna < DistanciaReferenciaPierna)
            {
                float yPierna = zigzag.PuntoEn(sZig).y;
                if (Mathf.Abs(yArriba - yPierna) > MargenAlturaPierna)
                {
                    Hallazgo("ARRIBA a altura " + yArriba.ToString("F2") + " y la pierna 2 esta a " + yPierna.ToString("F2"), jugador.transform.position, true);
                    subio = false;
                }
            }
            else
            {
                Nota("sin pierna del zigzag a menos de " + DistanciaReferenciaPierna + " m de la cima (" + distanciaPierna.ToString("F1") + " m): no se compara la altura");
            }
            Assert.IsTrue(subio, "escalera " + indice + ": no llegó arriba");
            Assert.AreEqual(0, caidas, "escalera " + indice + ": cayó al subir");
            Assert.LessOrEqual(atascos, MaxAtascosPorParte, "escalera " + indice + ": atascos al subir");

            int atascosSubida = atascos;
            RutaRecorridoT1361 bajada = subida.Invertida();
            Poner(new Vector3(cima.x, yArriba + 0.05f, cima.z), bajada.Direccion(0f));
            yield return Esperar(0.5f);
            yield return Caminar(bajada, bajada.Largo / RapidezAPie + HolguraSegundos, RadioLlegadaEscalera);
            float yAbajo = jugador.transform.position.y;
            Assert.IsTrue(llego, "escalera " + indice + ": no llegó abajo");
            Assert.AreEqual(yBase, yAbajo, MargenAlturaPierna, "escalera " + indice + ": no quedó a la altura de la calle base");
            Assert.AreEqual(0, caidas, "escalera " + indice + ": cayó al bajar");
            Assert.LessOrEqual(atascos - atascosSubida, MaxAtascosPorParte, "escalera " + indice + ": atascos al bajar");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Escalera1_Z120_SubeYBaja()
        {
            yield return SubirYBajar(0);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Escalera2_Z60_SubeYBaja()
        {
            yield return SubirYBajar(1);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Escalera3_Z0_SubeYBaja()
        {
            yield return SubirYBajar(2);
        }

        // ------------------------------------------------------------------------------------------------------------ en carro

        private IEnumerator ManejarParte(RutaRecorridoT1361 ruta)
        {
            CrearCarro(ruta);
            yield return Esperar(1f);
            Time.timeScale = EscalaLarga;
            yield return Manejar(ruta, ruta.Largo / (RapidezCalle * 0.5f) + HolguraSegundos, RapidezCalle);
            Time.timeScale = 1f;
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Carro_CalleBase_Completa()
        {
            yield return Preparar();
            yield return ManejarParte(SinBordes(RutaDe("CalleZona_Base")));
            AfirmarParteCarro("calle base");
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Carro_Zigzag_Parte1()
        {
            yield return Preparar();
            yield return ManejarParte(SinBordes(RutaDe("CalleZona_Zigzag")).Partir(PartesZigzag)[0]);
            AfirmarParteCarro("zigzag parte 1");
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Carro_Zigzag_Parte2()
        {
            yield return Preparar();
            yield return ManejarParte(SinBordes(RutaDe("CalleZona_Zigzag")).Partir(PartesZigzag)[1]);
            AfirmarParteCarro("zigzag parte 2");
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator Carro_Zigzag_Parte3YPlaza()
        {
            yield return Preparar();
            yield return ManejarParte(SinBordes(RutaDe("CalleZona_Zigzag")).Partir(PartesZigzag)[2]);
            AfirmarParteCarro("zigzag parte 3 y plaza");
        }

        // ------------------------------------------------------------------------------------------------------------ parkour

        private List<Superficie> Azoteas()
        {
            Transform casas = zona.transform.Find("Casas");
            Assert.IsNotNull(casas, "El prefab del Cerro debe traer el grupo Casas");
            List<Superficie> lista = new List<Superficie>();
            foreach (Transform casa in casas)
            {
                Transform cuerpo = casa.Find("Cuerpo");
                if (cuerpo == null || casa.Find("AzoteaT1318") == null)
                {
                    continue;
                }
                Collider col = cuerpo.GetComponent<Collider>();
                if (col == null)
                {
                    continue;
                }
                lista.Add(new Superficie { Nombre = casa.name, Caja = col.bounds });
            }
            return lista;
        }

        private static List<int> Vecinas(List<Superficie> todas, int i)
        {
            List<int> lista = new List<int>();
            for (int j = 0; j < todas.Count; j++)
            {
                if (j != i && Hueco(todas[i], todas[j]) < HuecoMaximoCadena && Mathf.Abs(todas[i].Techo - todas[j].Techo) <= DesnivelMaximoCadena)
                {
                    lista.Add(j);
                }
            }
            return lista;
        }

        private static int MasCercana(List<Superficie> todas, Vector3 punto, HashSet<int> usadas)
        {
            int mejor = -1;
            float distancia = float.MaxValue;
            for (int i = 0; i < todas.Count; i++)
            {
                if (usadas.Contains(i) || Vecinas(todas, i).Count == 0)
                {
                    continue;
                }
                float d = Plano(todas[i].Centro, punto);
                if (d < distancia)
                {
                    distancia = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        private static List<Superficie> Cadena(List<Superficie> todas, int inicio)
        {
            List<int> indices = new List<int> { inicio };
            Vector3 anterior = Vector3.zero;
            while (indices.Count < AzoteasPorCadena)
            {
                int actual = indices[indices.Count - 1];
                int mejor = -1;
                float puntaje = float.MaxValue;
                foreach (int j in Vecinas(todas, actual))
                {
                    if (indices.Contains(j))
                    {
                        continue;
                    }
                    Vector3 dir = todas[j].Centro - todas[actual].Centro;
                    dir.y = 0f;
                    float p = Hueco(todas[actual], todas[j]) + (Vector3.Dot(anterior, dir.normalized) < 0f ? PenalizaContramarcha : 0f);
                    if (p < puntaje)
                    {
                        puntaje = p;
                        mejor = j;
                    }
                }
                if (mejor < 0)
                {
                    break;
                }
                anterior = todas[mejor].Centro - todas[actual].Centro;
                anterior.y = 0f;
                anterior.Normalize();
                indices.Add(mejor);
            }
            return indices.Select(i => todas[i]).ToList();
        }

        /// <summary>Las 6 cadenas: 3 que salen de las cimas de las escaleras largas y 3 de la calle base (a 20, 50 y 80 % de su largo).</summary>
        private List<List<Superficie>> CadenasElegidas()
        {
            List<Superficie> todas = Azoteas();
            Assert.Greater(todas.Count, 10, "Se esperaban azoteas (AzoteaT1318 con Cuerpo) en las casas del Cerro");
            List<Vector3> puntos = EscalerasLargas().Select(e => e.Alto).ToList();
            RutaRecorridoT1361 calleBase = RutaDe("CalleZona_Base");
            foreach (float f in new[] { 0.2f, 0.5f, 0.8f })
            {
                puntos.Add(calleBase.PuntoEn(calleBase.Largo * f));
            }
            HashSet<int> usadas = new HashSet<int>();
            List<List<Superficie>> cadenas = new List<List<Superficie>>();
            foreach (Vector3 punto in puntos)
            {
                int inicio = MasCercana(todas, punto, usadas);
                if (inicio < 0)
                {
                    continue;
                }
                usadas.Add(inicio);
                cadenas.Add(Cadena(todas, inicio));
            }
            return cadenas;
        }

        private IEnumerator CorrerCadena(int numero)
        {
            yield return Preparar();
            List<List<Superficie>> cadenas = CadenasElegidas();
            Assert.GreaterOrEqual(cadenas.Count, numero + 1, "Se esperaban 6 cadenas de azoteas y hay " + cadenas.Count);
            List<Superficie> cadena = cadenas[numero];
            Assert.GreaterOrEqual(cadena.Count, 2, "La cadena " + numero + " no tiene dos azoteas vecinas");
            Nota("cadena " + numero + ": " + string.Join(" > ", cadena.Select(s => s.Nombre + "@" + s.Techo.ToString("F1"))));
            Vector3 c = cadena[0].Centro;
            CrearJugador(new Vector3(c.x, cadena[0].Techo + 0.05f, c.z), Vector3.forward);
            yield return Esperar(0.5f);
            for (int k = 0; k < cadena.Count - 1; k++)
            {
                yield return SaltarEntre(cadena[k], cadena[k + 1]);
            }
            Assert.AreEqual(0, saltosFallidos, "Saltos de la cadena " + numero + " que fallan (ver Logs/t1361-recorrido-*.txt)");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena1_CimaEscalera1() { yield return CorrerCadena(0); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena2_CimaEscalera2() { yield return CorrerCadena(1); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena3_CimaEscalera3() { yield return CorrerCadena(2); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena4_CalleBaseSur() { yield return CorrerCadena(3); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena5_CalleBaseCentro() { yield return CorrerCadena(4); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_Cadena6_CalleBaseNorte() { yield return CorrerCadena(5); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Parkour_PuntosTreparDeAcceso_SubenDesdeLaCalle()
        {
            yield return Preparar();
            List<PuntoTrepar> puntos = zona.GetComponentsInChildren<PuntoTrepar>(true).OrderBy(p => p.name).ThenBy(p => p.transform.position.z).ToList();
            List<PuntoTrepar> accesos = puntos.Where(p => p.name.StartsWith("Acceso")).ToList();
            if (accesos.Count == 0)
            {
                accesos = puntos;
            }
            Assert.Greater(accesos.Count, 0, "El Cerro debe traer PuntoTrepar de acceso a las azoteas");
            foreach (PuntoTrepar p in accesos.Take(MaxAccesos))
            {
                yield return Subir(p);
            }
            Assert.AreEqual(0, treparFallidos, "PuntoTrepar de acceso que no suben (ver Logs/t1361-recorrido-*.txt)");
        }
    }
}
