using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>
    /// T-1531: los sonidos nuevos del 20-nov. Cada ID de herramientas/sonidos/ids-20nov.csv tiene archivo y política, los alias apuntan a IDs que existen,
    /// el catálogo los trae todos (corre antes Mecha Cyberpunk/Sonido/Crear catalogo) y ningún ID que pida el código se queda sin archivo.
    /// </summary>
    public class SonidosNuevos20novPruebas
    {
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");
        private static readonly Regex Literal = new Regex("\"((?:JUG|PUN|ARM|VEH|PAN|CAL|ZON|ECO|UI|MUN|AMB|MUS|HUD|TIE|MEL|IMP|EXP|FUE|POL|NOV|NPC|ROB|DRN|CLI|OBJ|EXO|CAS|MIS|RAD|INT|CON|PAS|NAV|DES|MOTO)_[A-Z0-9_]+)\"");

        private struct Fila
        {
            public string Id;
            public string Archivo;
            public string Estado;
            public string Carpeta;
        }

        private static string Raiz => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static List<string[]> LeerCsv(string ruta)
        {
            List<string[]> filas = new List<string[]>();
            Regex campos = new Regex("(?:^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");
            foreach (string linea in File.ReadAllLines(ruta).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                List<string> c = new List<string>();
                foreach (Match m in campos.Matches(linea)) c.Add(m.Groups[1].Value.Trim('"'));
                filas.Add(c.ToArray());
            }
            return filas;
        }

        private static List<Fila> Ids20nov()
        {
            List<Fila> r = new List<Fila>();
            foreach (string[] c in LeerCsv(Path.Combine(Raiz, "herramientas/sonidos/ids-20nov.csv")))
            {
                r.Add(new Fila { Archivo = c[1], Carpeta = c[2], Estado = c[6], Id = Variante.Match(Path.GetFileNameWithoutExtension(c[1])).Groups[1].Value });
            }
            return r;
        }

        /// <summary>IDs de la fuente que sí se usan (sin los quitados).</summary>
        private static SortedSet<string> IdsVivos() => new SortedSet<string>(Ids20nov().Where(f => f.Estado != "quitado").Select(f => f.Id));

        private static SortedSet<string> enDisco;

        private static SortedSet<string> IdsConArchivoEnDisco()
        {
            if (enDisco != null) return enDisco;
            SortedSet<string> ids = new SortedSet<string>();
            string audio = Path.Combine(Raiz, "Assets/Arte/Audio");
            foreach (string f in Directory.GetFiles(audio, "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext != ".wav" && ext != ".mp3") continue;
                // T65: solo cuentan las carpetas nuevas (01_UI a 19_Voces y Musica); las viejas ya no se leen.
                string carpeta = f.Substring(audio.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                if (!Regex.IsMatch(carpeta, "^(0[1-9]|1[0-9])_") && carpeta != "Musica") continue;
                ids.Add(Variante.Match(Path.GetFileNameWithoutExtension(f)).Groups[1].Value);
            }
            foreach (string d in MapaSonidos20nov.IdsDescartados) ids.Remove(d);
            enDisco = ids;
            return ids;
        }

        private static bool Existe(string id) => MapaSonidos19nov.EsNuevo(id) || IdsConArchivoEnDisco().Contains(id);

        [Test]
        public void CadaIdDelCsv_TieneArchivoEnDisco()
        {
            List<string> sin = Ids20nov().Where(f => f.Estado != "quitado" && !File.Exists(Path.Combine(Raiz, f.Archivo))).Select(f => f.Archivo).ToList();
            Assert.IsEmpty(sin, "archivos del CSV que no estan: " + string.Join(", ", sin));
        }

        [Test]
        public void CadaIdDelCsv_TienePolitica_YEstaEncendido()
        {
            foreach (string id in IdsVivos())
            {
                Assert.IsTrue(MapaSonidos19nov.EsNuevo(id) || ReglaMezclaSonidos.EncendidoPorDefecto(id), id + ": no es un ID nuevo conocido");
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.IsNotNull(p.Grupo, id + ": sin grupo");
                Assert.GreaterOrEqual(p.MaxVoces, 1, id);
                Assert.That(p.Volumen, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), id);
                Assert.IsTrue(ReglaMezclaSonidos.EncendidoPorDefecto(id), id);
            }
        }

        [Test]
        public void LosIdsNuevosDel20nov_SonLosQueNoExistian_YTodosEstanEnElCsv()
        {
            SortedSet<string> vivos = IdsVivos();
            foreach (string id in MapaSonidos20nov.IdsNuevos)
            {
                Assert.IsTrue(vivos.Contains(id), id + " no esta en ids-20nov.csv");
                Assert.IsFalse(Array.IndexOf(MapaSonidos19nov.IdsNuevos, id) >= 0, id + " ya era del 19-nov");
            }
            Assert.AreEqual(MapaSonidos20nov.IdsNuevos.Length, MapaSonidos20nov.IdsNuevos.Distinct().Count());
        }

        [Test]
        public void LosIdsDel19nov_QueSeRenombraron_YaNoSePiden()
        {
            foreach (string viejo in new[] { "ARM_W07_RAYO", "ARM_NOVA_DISPARO", "ARM_NOVA_CARGA_DISPARO", "POL_NOVA_AVISO", "POL_RADIO_DESPACHO", "UI_CAMARA_LENTA" })
            {
                Assert.IsFalse(Array.IndexOf(MapaSonidos19nov.IdsNuevos, viejo) >= 0, viejo + " sigue en la lista del 19-nov");
                Assert.IsNotNull(MapaSonidos19nov.Nuevo(viejo), viejo + " debe ser alias");
            }
            Assert.AreEqual("ARM_W07_RAYO_LOOP", MapaSonidos19nov.Nuevo("ARM_W07_RAYO"));
            Assert.AreEqual("ARM_W07_GATILLO", MapaSonidos19nov.Nuevo("ARM_NOVA_DISPARO"));
            Assert.AreEqual("ARM_W07_SIN_ENERGIA", MapaSonidos19nov.Nuevo("ARM_NOVA_CARGA_DISPARO"));
            Assert.AreEqual("NOV_CAIDA_Y_ENCENDIDO", MapaSonidos19nov.Nuevo("POL_NOVA_AVISO"));
            Assert.AreEqual("POL_RADIO_AVISO", MapaSonidos19nov.Nuevo("POL_RADIO_DESPACHO"));
            Assert.AreEqual("EXO_RUEDA_CAMARA_LENTA_ENTRAR", MapaSonidos19nov.Nuevo("UI_CAMARA_LENTA"));
            Assert.AreEqual("EXP_CARRO", MapaSonidos19nov.Nuevo("VEH_EXPLOSION"));
        }

        [Test]
        public void CadaAlias_ApuntaAUnIdQueExiste_YNoEncadena()
        {
            foreach (MapaSonidos19nov.Par par in MapaSonidos20nov.Pares)
            {
                Assert.IsTrue(Existe(par.Nuevo), par.Viejo + " -> " + par.Nuevo + " no existe");
                Assert.IsFalse(MapaSonidos20nov.EstaMapeado(par.Nuevo), par.Nuevo + ": un alias apunta a otro alias");
                Assert.AreEqual(par.Nuevo, MapaSonidos19nov.Nuevo(par.Viejo), par.Viejo);
                Assert.IsTrue(MapaSonidos19nov.EstaMapeado(par.Viejo));
            }
            foreach (MapaSonidos19nov.Par par in MapaSonidos19nov.Pares)
            {
                Assert.IsTrue(Existe(par.Nuevo), par.Viejo + " -> " + par.Nuevo + " no existe");
            }
        }

        [Test]
        public void ElAliasDelDespacho_YElMegafono_SuenanConLasVocesAviso()
        {
            Assert.AreEqual("POL_RADIO_AVISO", MapaSonidos19nov.Nuevo("POL_RADIO_DESPACHO"));
            Assert.AreEqual("POL_RADIO_AVISO", MapaSonidos19nov.Nuevo("CAL_PATRULLA_MEGAFONO"));
            Assert.IsTrue(MapaSonidos20nov.EsDescartado("POL_RADIO_DESPACHO"));
            Assert.AreEqual(ReglaMezclaSonidos.Para("POL_RADIO_AVISO").Grupo, ReglaMezclaSonidos.Para("POL_RADIO_DESPACHO").Grupo);
            Assert.Greater(ReglaMezclaSonidos.Para("CAL_PATRULLA_MEGAFONO").DistanciaMaxima, ReglaMezclaSonidos.Para("POL_RADIO_AVISO").DistanciaMaxima, "el altavoz se oye mas lejos");
        }

        [Test]
        public void LosAliasHeredanLaPoliticaDelNuevo()
        {
            Assert.AreEqual(ReglaMezclaSonidos.Para("NOV_CAIDA_Y_ENCENDIDO").DistanciaMaxima, ReglaMezclaSonidos.Para("POL_NOVA_AVISO").DistanciaMaxima);
            Assert.AreEqual(ReglaMezclaSonidos.Para("EXO_RUEDA_CAMARA_LENTA_ENTRAR").Volumen, ReglaMezclaSonidos.Para("UI_CAMARA_LENTA").Volumen, 1e-5f);
            Assert.AreEqual(ReglaMezclaSonidos.Para("EXP_CARRO").Grupo, ReglaMezclaSonidos.Para("VEH_EXPLOSION").Grupo);
            Assert.AreEqual(ReglaMezclaSonidos.Para("HUD_NOTIFICACION").Volumen, ReglaMezclaSonidos.Para("UI_GUARDADO").Volumen, 1e-5f);
            Assert.AreEqual(ReglaMezclaSonidos.Para("IMP_BALA_ZUMBIDO").Volumen, ReglaMezclaSonidos.Para("UI_INDICADOR_DANIO").Volumen, 1e-5f);
        }

        [Test]
        public void Sincronizar_EnciendeTodosLosNuevos_YLosAlias()
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            c.FijarEncendido("EXP_CARRO", false);
            c.SincronizarInterruptores();
            foreach (string id in MapaSonidos20nov.IdsNuevos) Assert.IsTrue(c.Encendido(id), id);
            foreach (MapaSonidos19nov.Par par in MapaSonidos20nov.Pares)
            {
                if (par.Encender) Assert.IsTrue(c.Encendido(par.Viejo), par.Viejo);
            }
        }

        [Test]
        public void ElAliasSuenaConElArchivoNuevo()
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            AudioClip viejo = AudioClip.Create("viejo", 441, 1, 44100, false);
            AudioClip nuevo = AudioClip.Create("nuevo", 441, 1, 44100, false);
            c.Fijar(new List<CatalogoSonidos.Entrada>
            {
                new CatalogoSonidos.Entrada { id = "POL_NOVA_AVISO", finales = new[] { viejo } },
                new CatalogoSonidos.Entrada { id = "NOV_CAIDA_Y_ENCENDIDO", finales = new[] { nuevo } },
            });
            Assert.AreSame(nuevo, c.Elegir("POL_NOVA_AVISO"));
        }

        [Test]
        public void Politica_JerarquiaYVolumenes_DeCadaIdNuevo()
        {
            foreach (TablaMezcla20nov.Fila f in TablaMezcla20nov.Filas)
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(f.Id);
                Assert.That(p.Volumen, Is.InRange(0.02f, 1f), f.Id);
                Assert.IsNotNull(p.Grupo, f.Id);
                Assert.GreaterOrEqual(p.MaxVoces, 1, f.Id);
                bool bucle = f.Bucles > 0;
                // Nada suena identico dos veces; los bucles de ambiente casi no varian para que no bombeen.
                if (!bucle) Assert.Greater(p.VariacionTono + p.VariacionVolumen, 0f, f.Id + ": sin variacion");
                else Assert.LessOrEqual(p.VariacionTono, 0.03f, f.Id + ": un bucle no debe variar mucho");
                // Lo del mundo es 3D con distancia; 2D solo UI/HUD/radio/jugador/zona/clima.
                if (p.DistanciaMaxima == 0f) Assert.IsTrue(Es2D(f.Id), f.Id + " es 2D y no deberia");
                SeccionSonido s = ReglaCatalogoSonidos.SeccionDe(f.Id);
                int prioridad = ReglaMezclaSonidos.Prioridad(f.Id);
                if (s == SeccionSonido.Ambiente) Assert.AreEqual(200, prioridad, f.Id + ": el ambiente va abajo");
                else if (f.Id != "FUE_LOOP_GRANDE") Assert.Less(prioridad, 200, f.Id);
                Assert.AreEqual(Math.Max(1, f.Bucles), ReglaMezclaSonidos.MaxBuclesDelId(f.Id), f.Id);
            }
        }

        private static bool Es2D(string id)
        {
            string[] p = { "AMB_", "CLI_", "HUD_", "MIS_", "PAN_", "RAD_", "EXO_RUEDA", "JUG_LATIDO", "JUG_ZUMBIDO", "POL_ESTRELLA", "CON_" };
            return p.Any(x => id.StartsWith(x, StringComparison.Ordinal));
        }

        [Test]
        public void LasDistancias_SonLasPedidas()
        {
            Assert.AreEqual(250f, ReglaMezclaSonidos.Para("POL_SIRENA_MURO").DistanciaMaxima);
            Assert.AreEqual(120f, ReglaMezclaSonidos.Para("ROB_PES_LLEGADA").DistanciaMaxima);
            foreach (string id in new[] { "NOV_PASOS_CAMINAR", "NOV_PASOS_CORRER", "ROB_PERRO_MOVER" })
            {
                Assert.That(ReglaMezclaSonidos.Para(id).DistanciaMaxima, Is.InRange(25f, 30f), id);
            }
            foreach (string id in new[] { "OBJ_REFRIGERADOR", "OBJ_AIRE_ACONDICIONADO", "OBJ_TRANSFORMADOR", "OBJ_CAMARA_SEGURIDAD", "OBJ_MAQUINA_EXPENDEDORA", "OBJ_TELE_REINICIO" })
            {
                Assert.That(ReglaMezclaSonidos.Para(id).DistanciaMaxima, Is.InRange(6f, 12f), id);
            }
            foreach (string id in new[] { "INT_TALLER_CANEPA", "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR", "INT_ARMERIA_VENTILADOR", "INT_BODEGA_GOTERAS" })
            {
                Assert.AreEqual(15f, ReglaMezclaSonidos.Para(id).DistanciaMaxima, id);
            }
            Assert.AreNotEqual(ReglaMezclaSonidos.Para("INT_TIENDA_TINO_RADIO").Grupo, ReglaMezclaSonidos.Para("INT_TIENDA_TINO_VENTILADOR").Grupo, "suenan juntos: no se cortan");
        }

        [Test]
        public void LasSecciones_ElAmbienteVaEnSuGrupo_YLasVocesArriba()
        {
            foreach (string id in new[] { "AMB_LOMAS", "CLI_VIENTO", "CLI_LLUVIA_FUERTE", "OBJ_REFRIGERADOR", "INT_TALLER_CANEPA", "INT_BODEGA_GOTERAS", "AMB_SIRENA_LOOP" })
            {
                Assert.AreEqual(SeccionSonido.Ambiente, ReglaCatalogoSonidos.SeccionDe(id), id);
            }
            foreach (string id in new[] { "INT_PUERTA_MADERA", "INT_RECOGER_DINERO", "INT_HACKEO_EXITO" })
            {
                Assert.AreEqual(SeccionSonido.Efectos, ReglaCatalogoSonidos.SeccionDe(id), id);
            }
            foreach (string id in new[] { "NPC_H_DOLOR", "NPC_M_DOLOR", "ROB_VOZ_ALERTA", "PAN_HAS_MUERTO", "PAN_ARRESTADO_ESPOSAS" })
            {
                Assert.AreEqual(SeccionSonido.Voces, ReglaCatalogoSonidos.SeccionDe(id), id);
            }
            Assert.AreEqual(SeccionSonido.Efectos, ReglaCatalogoSonidos.SeccionDe("ROB_SERVOS"));
            Assert.AreEqual(SeccionSonido.Interfaz, ReglaCatalogoSonidos.SeccionDe("MIS_TEMPORIZADOR"));
            Assert.AreEqual(SeccionSonido.Interfaz, ReglaCatalogoSonidos.SeccionDe("HUD_MENSAJE_CELULAR"));
            Assert.AreEqual(SeccionSonido.Musica, ReglaCatalogoSonidos.SeccionDe("RAD_EST1_CUMBIA_PERCUSION"));
            Assert.AreEqual(SeccionSonido.Musica, ReglaCatalogoSonidos.SeccionDe("RAD_CAMBIAR_ESTACION"));
        }

        [Test]
        public void LaPrioridad_DisparosVocesYAvisosArriba_AmbienteObjetosYClimaAbajo()
        {
            int ambiente = ReglaMezclaSonidos.Prioridad("AMB_LOMAS");
            foreach (string id in new[] { "ARM_W08_DISPARO", "DRN_PES_AMETRALLADORA", "NPC_H_DOLOR", "ROB_VOZ_ALERTA", "NOV_CAIDA_Y_ENCENDIDO", "HUD_MENSAJE_CELULAR", "MIS_TEMPORIZADOR", "POL_RADIO_AVISO" })
            {
                Assert.Less(ReglaMezclaSonidos.Prioridad(id), 64 + 1, id);
                Assert.Less(ReglaMezclaSonidos.Prioridad(id), ambiente, id);
            }
            foreach (string id in new[] { "OBJ_REFRIGERADOR", "CLI_VIENTO", "INT_TALLER_CANEPA" })
            {
                Assert.AreEqual(ambiente, ReglaMezclaSonidos.Prioridad(id), id);
            }
            Assert.Greater(ReglaMezclaSonidos.Prioridad("NOV_PASOS_CAMINAR"), ReglaMezclaSonidos.Prioridad("NOV_ESCUDO_IMPACTO"));
            Assert.Less(ReglaMezclaSonidos.Prioridad("NOV_PASOS_CAMINAR"), ambiente);
        }

        [Test]
        public void ElNivelAlOido_ElAmbienteQuedaMuyPorDebajoDeDisparosVocesYAvisos()
        {
            // nivel al oido = nivel del archivo normalizado al pico (niveles-20nov.csv) + volumen de la politica + ganancia de la seccion
            Dictionary<string, float> alOido = new Dictionary<string, float>();
            foreach (string[] c in LeerCsv(Path.Combine(Raiz, "herramientas/sonidos/niveles-20nov.csv")))
            {
                if (c[11].Length == 0) continue; // sin volumen asignado (los del 19-nov)
                float lufsNorm = float.Parse(c[8], System.Globalization.CultureInfo.InvariantCulture);
                float ganancia = SeccionGanancia(ReglaCatalogoSonidos.SeccionDe(c[0]));
                float vol = ReglaMezclaSonidos.Para(c[0]).Volumen;
                alOido[c[0]] = lufsNorm + 20f * Mathf.Log10(vol * ganancia);
            }
            Assert.Greater(alOido.Count, 50);
            float ambMax = alOido.Where(k => ReglaCatalogoSonidos.SeccionDe(k.Key) == SeccionSonido.Ambiente && k.Key != "AMB_SIRENA_LOOP" && MapaSonidos20nov.Bucles(k.Key) > 0).Max(k => k.Value);
            float vocesMin = alOido.Where(k => ReglaCatalogoSonidos.SeccionDe(k.Key) == SeccionSonido.Voces && !k.Key.StartsWith("PAN_ARRESTADO")).Min(k => k.Value);
            Assert.Greater(vocesMin - ambMax, 10f, "las voces deben quedar al menos 10 dB sobre el ambiente");
            Assert.Greater(alOido["NOV_CAIDA_Y_ENCENDIDO"], ambMax + 10f);
            Assert.Greater(alOido["HUD_MENSAJE_CELULAR"], ambMax + 8f);
            Assert.Greater(alOido["EXP_CARRO"], alOido["HUD_MENSAJE_CELULAR"]);
        }

        private static float SeccionGanancia(SeccionSonido s)
        {
            switch (s)
            {
                case SeccionSonido.Voces: return CatalogoSonidos.MezclaVoces;
                case SeccionSonido.Ambiente: return CatalogoSonidos.MezclaAmbiente;
                case SeccionSonido.Musica: return CatalogoSonidos.MezclaMusica;
                case SeccionSonido.Interfaz: return CatalogoSonidos.MezclaInterfaz;
                default: return CatalogoSonidos.MezclaEfectos;
            }
        }

        [Test]
        public void ElCsvDeNiveles_CoincideConLaTablaDeMezcla()
        {
            int n = 0;
            foreach (string[] c in LeerCsv(Path.Combine(Raiz, "herramientas/sonidos/niveles-20nov.csv")))
            {
                if (c[11].Length == 0) continue;
                PoliticaSonido p = ReglaMezclaSonidos.Para(c[0]);
                Assert.AreEqual(float.Parse(c[11], System.Globalization.CultureInfo.InvariantCulture), p.Volumen, 0.001f, c[0] + ": volumen");
                Assert.AreEqual(float.Parse(c[17], System.Globalization.CultureInfo.InvariantCulture), p.DistanciaMaxima, 0.01f, c[0] + ": distancia");
                Assert.AreEqual(c[14], p.Grupo, c[0]);
                n++;
            }
            Assert.AreEqual(TablaMezcla20nov.Filas.Length, n, "cada fila de la tabla tiene su linea en niveles-20nov.csv");
        }

        [Test]
        public void ElCatalogo_TieneTodosLosIdsNuevos_Corre_CrearCatalogo_Antes()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>("Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset");
            Assert.IsNotNull(catalogo);
            HashSet<string> conClip = new HashSet<string>(catalogo.Entradas.Where(e => e != null && e.finales != null && e.finales.Length > 0).Select(e => e.id));
            List<string> faltan = IdsVivos().Where(id => !conClip.Contains(id) && !MapaSonidos20nov.EsDescartado(id)).ToList();
            Assert.IsEmpty(faltan, "IDs sin entrada en el catalogo (corre Mecha Cyberpunk/Sonido/Crear catalogo): " + string.Join(", ", faltan));
            foreach (string d in MapaSonidos20nov.IdsDescartados) Assert.IsFalse(conClip.Contains(d), d + ": no debe estar en el catalogo");
            catalogo.SincronizarInterruptores();
            foreach (string id in MapaSonidos20nov.IdsNuevos) Assert.IsTrue(catalogo.Encendido(id), id + " apagado");
        }

        private static bool QuedaAnotadoComoFaltante(string id)
        {
            string ruta = Path.Combine(Raiz, "docs/sonidos-faltan.md");
            return File.Exists(ruta) && File.ReadAllText(ruta).Contains(id);
        }

        [Test]
        public void NingunIdQuePideElCodigo_QuedaSinArchivo()
        {
            SortedSet<string> conArchivo = IdsConArchivoEnDisco();
            // los provisionales y los generados por GeneradorSonidosProvisionales ya estan en disco (Assets/Arte/Audio/Provisional)
            SortedDictionary<string, List<string>> usados = new SortedDictionary<string, List<string>>();
            foreach (string f in Directory.GetFiles(Path.Combine(Raiz, "Assets/_Proyecto"), "*.cs", SearchOption.AllDirectories))
            {
                string n = f.Replace('\\', '/');
                if (n.Contains("/Tests/") || n.Contains("/Editor/")) continue;
                foreach (Match m in Literal.Matches(File.ReadAllText(f)))
                {
                    if (!usados.TryGetValue(m.Groups[1].Value, out List<string> donde)) usados[m.Groups[1].Value] = donde = new List<string>();
                    donde.Add(Path.GetFileName(n));
                }
            }
            List<string> sin = new List<string>();
            foreach (string id in usados.Keys)
            {
                if (conArchivo.Contains(id)) continue;
                if (conArchivo.Any(x => x.StartsWith(id + "_", StringComparison.Ordinal))) continue; // un prefijo (ROB_VOZ), no un ID
                string alias = MapaSonidos19nov.Nuevo(id);
                if (alias != null && conArchivo.Contains(alias)) continue;
                if (QuedaAnotadoComoFaltante(id) || MapaSonidos20nov.EsDescartado(id)) continue; // T65: el sonido viejo se desconecto y esta en docs/sonidos-faltan.md
                sin.Add(id + " <- " + string.Join("|", usados[id].Distinct()));
            }
            Assert.IsEmpty(sin, "IDs pedidos por el codigo sin archivo (ni alias a uno): " + string.Join("; ", sin));
        }
    }
}
