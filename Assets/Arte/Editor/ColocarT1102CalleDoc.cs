using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1102: la calle de norte a sur junto a la calle del Doc (x = -110.5, de la avenida z = -10 a la calle del sur) rehecha con cuadras de
    /// verdad: casas del kit armadas a mano (de 1 a 3 pisos, cada una con su color), tienditas con la cortina a medio subir, taquería, vulcanizadora,
    /// lavandería, lote baldío con malla y mural, postes con lámpara cada ~14 m, bancos, sillas y un remate al fondo (cantina) en lugar del muro liso.
    /// Apaga (SetActive false, no borra) las casas, postes y pretiles viejos de S2c_Suburbios que caían en la franja; se quedan el I+D+I del Doc,
    /// las cuatro esquinas de K05 con sus semáforos y cruces peatonales, el arroyo, las banquetas, el callejón, las casas que sostienen los accesos
    /// de techo del andador (12, 13, 18 y 19) y la esquina de la avenida (23 y 24).
    /// Idempotente: rehace su raíz <see cref="Raiz"/>. No corre ColocarS2cSuburbios.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1102CalleDoc.Colocar
    /// </summary>
    public static class ColocarT1102CalleDoc
    {
        public const string Raiz = "T1102_CalleDoc";
        public const int Semilla = 11020;
        public const float XCalle = ColocarS2cCalle.XCruce;
        public const float XOriente = ColocarS2cCalle.XCruce + ColocarS2cCalle.MedioCruce;     // línea de fachadas oriente (-104.75)
        public const float XPoniente = ColocarS2cCalle.XCruce - ColocarS2cCalle.MedioCruce;    // línea de fachadas poniente (-116.25)
        public const float ZRemate = -87f;
        public const float XBancoOriente = -105.4f;
        private const float AnchoColliderPoste = 0.5f;
        public const string CarpetaTexturas = KitS2c.CarpetaTexturas;

        /// <summary>Casas viejas de la franja (S2c_Suburbios/Casas/...) con su centro y su tamaño en planta (x, z, ancho x, ancho z), de T-1102.</summary>
        public static readonly (string nombre, float x, float z, float ax, float az)[] CasasViejas =
        {
            ("Casa_9", -101.0f, -28.9f, 8.5f, 4.9f), ("Casa_10", -101.0f, -23.9f, 8.4f, 5.1f), ("Casa_11", -101.3f, -18.6f, 9.1f, 5.8f),
            ("Casa_14", -102.7f, -74.0f, 7.8f, 8.0f), ("Casa_15", -119.2f, -28.4f, 8.1f, 5.8f), ("Casa_16", -119.5f, -23.6f, 7.5f, 4.3f),
            ("Casa_17", -119.2f, -18.9f, 8.1f, 5.7f), ("Casa_20", -119.6f, -74.0f, 7.3f, 7.9f), ("Casa_45", -119.9f, -81.2f, 7.4f, 6.4f),
            ("Casa_46", -119.2f, -81.0f, 8.9f, 6.0f), ("Casa_47", -101.6f, -81.5f, 6.3f, 7.0f), ("Casa_48", -102.5f, -81.0f, 7.5f, 6.1f),
        };

        /// <summary>Casas viejas que se quedan: sostienen los accesos y tablones de techo del andador (12, 13, 18, 19) o son la esquina de la avenida (23, 24).</summary>
        public static readonly string[] CasasQueSeQuedan = { "Casa_12", "Casa_13", "Casa_18", "Casa_19", "Casa_23", "Casa_24" };
        public static readonly string[] PostesViejos = { "Poste_1", "Poste_2", "Poste_3", "Poste_4" };

        private static KitS2c.Obra o;
        private static Transform casas, props, piso, postes;
        private static float arroyo, banqueta;
        private static readonly List<Vector3> altosOriente = new List<Vector3>();
        private static readonly List<Vector3> altosPoniente = new List<Vector3>();

        private static Material Madera => KitS2c.Mat("T1102_Madera", () => KitS2c.Lit(new Color(0.42f, 0.27f, 0.15f), 0.2f, KitS2c.TexMuro()));
        private static Material Plastico => KitS2c.Mat("T1102_Plastico", () => KitS2c.Lit(new Color(0.82f, 0.82f, 0.78f), 0.45f));
        private static Material PlasticoRojo => KitS2c.Mat("T1102_PlasticoRojo", () => KitS2c.Lit(new Color(0.75f, 0.12f, 0.1f), 0.45f));
        private static Material Metal => KitS2c.Mat("T1102_Metal", () => KitS2c.Lit(new Color(0.16f, 0.17f, 0.19f), 0.5f, KitS2c.TexMuro()));
        private static Material Tierra => KitS2c.Mat("T1102_Tierra", () => KitS2c.Lit(new Color(0.24f, 0.19f, 0.14f), 0.05f, KitS2c.TexMuro()));
        private static Material LetreroPintado => KitS2c.Mat("T1102_LetreroPintado", () => KitS2c.Lit(new Color(0.95f, 0.62f, 0.12f), 0.15f, KitS2c.TexMuro()));
        private static Material BrasaMat => KitS2c.Mat("T1102_Brasa", () => KitS2c.Emision(new Color(3.2f, 1.1f, 0.25f), Texture2D.whiteTexture));
        private static Material VitrinaMat => KitS2c.Mat("T1102_Vitrina", () => KitS2c.Emision(new Color(1.3f, 2.1f, 2.6f), Texture2D.whiteTexture));
        private static Material InteriorMat => KitS2c.Mat("T1102_Interior", () => KitS2c.Emision(new Color(1.2f, 0.55f, 0.2f), Texture2D.whiteTexture));

        [MenuItem("Mecha Cyberpunk/S2c/T-1102 Calle del Doc con cuadras")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            int apagados = Apagar();
            o = KitS2c.Empezar(Raiz, Semilla);
            altosOriente.Clear();
            altosPoniente.Clear();
            arroyo = KitS2c.Suelo(new Vector3(XCalle, 0f, -70f), 0.2f);
            // El arroyo ya está hecho: su cara de arriba es lo más alto en el centro de la calle.
            banqueta = arroyo + ColocarS2cCalle.AltoBanqueta;
            casas = Hijo("Casas");
            props = Hijo("Props");
            piso = Hijo("Piso");
            postes = Hijo("Postes");
            List<Vector3> puntasO = new List<Vector3>(), puntasP = new List<Vector3>();

            CuadraNorte();
            CuadraCentro();
            Andador(puntasO, puntasP);
            CuadraSur();
            Remate(puntasO);
            Postes(puntasO, puntasP);
            Cablear(puntasO, altosOriente, altosPoniente);
            Cablear(puntasP, altosPoniente, altosOriente);
            for (int i = 0; i < 8; i++)
            {
                float z = i < 5 ? -12f - i * 6.5f : -60f - (i - 4) * 5.5f;
                if (z < -37f && z > -49f)
                {
                    z -= 12f;
                }
                KitS2c.Charco(o, piso, new Vector3(XCalle + o.Rango(-2.6f, 2.6f), arroyo, z + o.Rango(-1f, 1f)), o.Rango(1.2f, 3f), o.Rango(0.8f, 1.8f), o.Rango(0f, 180f));
            }
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("T1102: " + apagados + " objetos viejos apagados, " + o.Casas + " casas, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos + " charcos, " + o.Neones + " neones");
            ColocarS2.Guardar("t1102-calle-doc");
        }

        // ================================================================ Capturas

        public static readonly (string nombre, Vector3 desde, Vector3 hacia, float campo)[] Tomas =
        {
            ("norte-a-sur", new Vector3(XCalle, 1.7f, -12f), new Vector3(XCalle, 1.9f, -60f), 66f),
            ("sur-a-norte", new Vector3(XCalle, 1.7f, -82f), new Vector3(XCalle, 2.2f, -30f), 66f),
            ("cruce-k01", new Vector3(-113.6f, 1.7f, -46.6f), new Vector3(-106.5f, 1.6f, -38.5f), 70f),
            ("taqueria", new Vector3(-109.2f, 1.7f, -26f), new Vector3(XOriente, 1.2f, -28.5f), 70f),
            ("andador", new Vector3(-110.5f, 1.7f, -70f), new Vector3(-110.5f, 1.6f, -58f), 66f),
            ("remate", new Vector3(XCalle, 1.7f, -70f), new Vector3(XCalle, 3.5f, -87f), 62f),
            ("alto", new Vector3(XCalle, 40f, -52f), new Vector3(XCalle, 0f, -47f), 60f),
        };

        /// <summary>Capturas de noche como queda la escena (y con una luz direccional temporal) en docs/capturas/t-1102/.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            foreach ((string nombre, Vector3 desde, Vector3 hacia, float campo) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/t-1102/" + nombre + "-noche.png", campo);
            }
            GameObject sol = new GameObject("SolTemporalT1102");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.6f;
            l.color = new Color(1f, 0.95f, 0.85f);
            sol.transform.rotation = Quaternion.Euler(48f, 35f, 0f);
            foreach ((string nombre, Vector3 desde, Vector3 hacia, float campo) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/t-1102/" + nombre + "-luz.png", campo);
            }
            Object.DestroyImmediate(sol);
            ColocarS2.LadoALado("imageassets/referencias/look/K05_EsquinaBarrio.png", "docs/capturas/t-1102/cruce-k01-noche.png", "docs/capturas/t-1102/comparacion-k05.png");
        }

        // ================================================================ Lo viejo

        private static int Apagar()
        {
            int n = 0;
            Transform cs = ColocarS2.Buscar("S2c_Suburbios/Casas");
            foreach (var c in CasasViejas)
            {
                Transform t = cs == null ? null : cs.Find(c.nombre);
                if (t != null && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    n++;
                }
            }
            Transform ps = ColocarS2.Buscar("S2c_Suburbios/Postes");
            foreach (string p in PostesViejos)
            {
                Transform t = ps == null ? null : ps.Find(p);
                if (t != null && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    n++;
                }
            }
            n += AdelgazarPostesK05();
            // Los pretiles de azotea de S2_Techos que quedaban sobre las casas apagadas.
            Transform pretiles = ColocarS2.Buscar("S2_Techos/Pretiles");
            if (pretiles != null)
            {
                foreach (Transform t in pretiles)
                {
                    if (!t.gameObject.activeSelf)
                    {
                        continue;
                    }
                    Vector3 p = t.position;
                    Renderer r = t.GetComponentInChildren<Renderer>(true);
                    if (r != null)
                    {
                        p = r.bounds.center;
                    }
                    if (CasasViejas.Any(c => Mathf.Abs(p.x - c.x) < c.ax * 0.5f + 0.3f && Mathf.Abs(p.z - c.z) < c.az * 0.5f + 0.3f))
                    {
                        t.gameObject.SetActive(false);
                        n++;
                    }
                }
            }
            return n;
        }

        /// <summary>
        /// Los postes de las esquinas de K05 que caen sobre las banquetas de la calle (el del NO en z = -26.75 y el del SE en z = -59.25, a la entrada
        /// del andador) traen una caja de 3 x 1.9 m que cubre su brazo y tapa la banqueta entera: se adelgaza al pie del poste (T-1102).
        /// </summary>
        private static int AdelgazarPostesK05()
        {
            int n = 0;
            Transform cs = ColocarS2.Buscar("S2c_Suburbios/Casas");
            if (cs == null)
            {
                return 0;
            }
            foreach (Transform esquina in cs.Cast<Transform>().Where(t => t.name == "Esquina_K05"))
            {
                foreach (Transform poste in esquina.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Poste_") && Mathf.Abs(t.position.x - XCalle) < 8f))
                {
                    foreach (BoxCollider bc in poste.GetComponents<BoxCollider>().Where(b => b.size.x > AnchoColliderPoste + 0.01f || b.size.z > AnchoColliderPoste + 0.01f))
                    {
                        bc.center = new Vector3(0f, bc.center.y, 0f);
                        bc.size = new Vector3(AnchoColliderPoste, bc.size.y, AnchoColliderPoste);
                        n++;
                    }
                }
            }
            return n;
        }

        // ================================================================ Cuadras

        private static KitS2c.Estilo Estilo(string planta, int pisos, int color, string pisoAlto = null, bool tinaco = true, bool foco = true, Color? neon = null, bool antena = false, bool aire = false, bool? toldo = null)
        {
            return new KitS2c.Estilo
            {
                PlantaBaja = planta, Pisos = pisos, ColorPiso = color, PisoAlto = pisoAlto ?? KitS2c.PisoFachada, Tinaco = tinaco, Foco = foco, Neon = neon,
                NeonBandera = true, Antena = antena, AireAzotea = aire, Toldo = toldo, LucesVentanas = 0.55f,
            };
        }

        /// <summary>Una casa del kit con el frente en la línea de fachada de la calle; <paramref name="z"/> es el centro de su frente.</summary>
        private static Transform Casa(bool oriente, int n, float z0, float z1, float fondo, KitS2c.Estilo e)
        {
            float x = oriente ? XOriente : XPoniente;
            Vector3 normal = oriente ? Vector3.left : Vector3.right;
            Vector3 centro = new Vector3(x, banqueta, (z0 + z1) * 0.5f);
            float azotea = KitS2c.Casa(o, casas, centro, normal, Mathf.Abs(z1 - z0), fondo, e);
            Transform casa = casas.GetChild(casas.childCount - 1);
            casa.name = "Fachada_" + (oriente ? "O" : "P") + "_" + n.ToString("00") + "_" + e.Pisos + "p_c" + e.ColorPiso;
            (oriente ? altosOriente : altosPoniente).Add(centro + Vector3.up * (azotea - 0.4f) + normal * 0.05f);
            return casa;
        }

        /// <summary>Casa en la línea de fachada de una calle que corre en x (el remate): <paramref name="x"/> es el centro de su frente, mira al norte.</summary>
        private static Transform CasaRemate(int n, float x0, float x1, float fondo, KitS2c.Estilo e)
        {
            Vector3 centro = new Vector3((x0 + x1) * 0.5f, banqueta, ZRemate);
            float azotea = KitS2c.Casa(o, casas, centro, Vector3.forward, Mathf.Abs(x1 - x0), fondo, e);
            Transform casa = casas.GetChild(casas.childCount - 1);
            casa.name = "Remate_" + n.ToString("00") + "_" + e.Pisos + "p_c" + e.ColorPiso;
            altosPoniente.Add(centro + Vector3.up * (azotea - 0.4f) + Vector3.forward * 0.05f);
            return casa;
        }

        private static void CuadraNorte()
        {
            // ---- Poniente (de la esquina de la avenida a la esquina de K01): vecindad de 3 pisos y tiendita de abarrotes.
            Transform vecindad = Casa(false, 1, -16f, -23f, 7f, Estilo(KitS2c.PlantaVivienda, 3, 1, "C20b_PisoDosVentanas_cy", true, true, null, false, true, true));
            Balcones(vecindad, false, -19.5f, 5.2f);
            SegundoTinaco(vecindad, false, -19.5f);
            Transform tienda = Casa(false, 2, -23f, -31.25f, 7f, Estilo(KitS2c.PlantaLocal, 1, 3, null, false, true, KitS2c.Magenta, false, false, false));
            Cortina(tienda, 0.5f, new Color(1f, 0.7f, 0.35f));
            LetreroHorizontal(new Vector3(XPoniente, banqueta + 3.15f, -27.1f), Vector3.right, 2.6f, new Color(1f, 0.82f, 0.4f));
            Silla(new Vector3(XPoniente + 0.42f, banqueta, -24.6f), 90f);
            Silla(new Vector3(XPoniente + 0.42f, banqueta, -25.9f), 80f);
            Medidor(false, -29.6f, 0f);

            // ---- Oriente (del Doc a la esquina de K01): casa verde agua, callejoncito y taquería.
            Transform verde = Casa(true, 1, -16f, -21.5f, 8f, Estilo(KitS2c.PlantaVivienda, 2, 3, null, true, true, null, true, false, false));
            Medidor(true, -20.4f, 0f);
            Callejoncito(-21.5f, -24f);
            // Dos pisos (taquería con vivienda arriba): su azotea es la de la ruta de parkour Ruta_S2c_Suburbios_01 (barda, contenedor y escalera por atrás,
            // en z = -28.75), que antes subía a la Casa_9; la salida de la escalera está a 6.69 m.
            Transform taqueria = Casa(true, 2, -24f, -31.25f, 8f, Estilo(KitS2c.PlantaLocal, 2, 1, null, false, true, KitS2c.Turquesa, false, true, true));
            Cortina(taqueria, 0.8f, new Color(1f, 0.55f, 0.2f));
            Taqueria(-27.6f);
            Neon(taqueria, true, new Color(1f, 0.5f, 0.2f));
        }

        private static void CuadraCentro()
        {
            // Entre la esquina de K01 (z = -54.75) y el andador (z = -59) solo cabe una casa por lado: son las 12 y 18, que sostienen los accesos de techo.
            // Se les pone encima lo que pedía el diseño: la vulcanizadora (oriente) y la lavandería (poniente).
            Vector3 ori = new Vector3(XOriente, banqueta, -56.9f), pon = new Vector3(XPoniente, banqueta, -56.9f);
            // Vulcanizadora: llantas apiladas afuera y letrero pintado.
            for (int i = 0; i < 2; i++)
            {
                KitS2c.Pieza(props, "L04_Llantas", ori + new Vector3(0.5f, 0f, -0.9f + i * 1.7f), Quaternion.Euler(0f, 90f * i, 0f), Vector3.one * 0.5f, false).name = "Llantas_" + i;
            }
            Caja(props, "LetreroVulcanizadora", new Vector3(XOriente - 0.06f, banqueta + 3.0f, -56.9f), new Vector3(0.1f, 0.55f, 3.2f), LetreroPintado, false);
            Caja(props, "LetreroVulcanizadora_Llanta", new Vector3(XOriente - 0.1f, banqueta + 3.0f, -55.5f), new Vector3(0.1f, 0.38f, 0.38f), Metal, false);
            KitS2c.Luz(props, new Vector3(XOriente - 0.8f, banqueta + 3.1f, -56.9f), LightType.Point, KitS2c.Calido, 3f, 5f, 0f);
            // Lavandería: vitrina iluminada con luz fría y letrero de bandera.
            Caja(props, "VitrinaLavanderia", new Vector3(XPoniente + 0.05f, banqueta + 1.35f, -56.9f), new Vector3(0.06f, 1.0f, 2.4f), VitrinaMat, false);
            KitS2c.Luz(props, new Vector3(XPoniente + 0.9f, banqueta + 1.5f, -56.9f), LightType.Point, KitS2c.Fria, 3.2f, 5.5f, 0f);
            LetreroHorizontal(new Vector3(XPoniente, banqueta + 2.85f, -56.9f), Vector3.right, 1.8f, new Color(0.65f, 0.9f, 1f));
            KitS2c.Neon(o, props, new Vector3(XPoniente + 0.05f, banqueta + 3.3f, -55.6f), Vector3.right, Vector3.forward, KitS2c.Turquesa, true);
            Medidor(false, -55.6f, 0f);
        }

        private static void Andador(List<Vector3> puntasO, List<Vector3> puntasP)
        {
            // Las dos puntas del andador (z -59 a -64): poste con lámpara y un banco en cada lado, dejando libres los 5 m del paso.
            // La punta norte del lado oriente ya tiene el poste con transformador de la esquina K05 del sureste (z = -59.25).
            puntasP.Add(Poste(new Vector3(XPoniente + 1.8f, banqueta, -64.7f), Vector3.right, arroyo, false));
            Banco(new Vector3(XPoniente + 1.7f, banqueta, -57.9f), -90f);
            Banco(new Vector3(XOriente - 1.7f, banqueta, -66.0f), 90f);
        }

        private static void CuadraSur()
        {
            // ---- Poniente: Casa_19 (se queda), lote baldío con malla ciclónica y mural, casa de 1 piso en la esquina.
            LoteBaldio(-70.4f, -77.6f);
            Casita(false, 3, -77.6f, -84f, 1, 2);
            KitS2c.Casa(o, casas, new Vector3(XPoniente - 3.7f, banqueta, -84f), Vector3.back, 7.4f, 6f, Estilo(KitS2c.PlantaVivienda, 1, 2, null, false, true, null, false, true, false));
            Transform sur = casas.GetChild(casas.childCount - 1);
            sur.name = "Fachada_S_01_1p_c2";

            // ---- Oriente: Casa_13 (se queda), casa de 3 pisos, casita de 1 piso y tienda de esquina con toldo en z = -84.
            Transform alta = Casa(true, 3, -70.1f, -74.6f, 6f, Estilo(KitS2c.PlantaVivienda, 3, 0, "C20b_PisoDosVentanas_cy", true, true, null, true, true, false));
            Balcones(alta, true, -72.35f, 3.6f);
            Casa(true, 4, -74.6f, -78f, 6f, Estilo(KitS2c.PlantaVivienda, 1, 0, null, false, true, KitS2c.Magenta, false, false, false));
            Transform tienda = Casa(true, 5, -78f, -84f, 6f, Estilo(KitS2c.PlantaLocal, 2, 2, null, true, true, KitS2c.Turquesa, false, false, true));
            Cortina(tienda, 0.5f, new Color(1f, 0.7f, 0.35f));
            LetreroHorizontal(new Vector3(XOriente, banqueta + 3.15f, -81f), Vector3.left, 2.2f, new Color(1f, 0.8f, 0.45f));
            // La cara que da al sur de la tienda de esquina.
            KitS2c.Casa(o, casas, new Vector3(XOriente + 3f, banqueta, -84f), Vector3.back, 6f, 6f, Estilo(KitS2c.PlantaVivienda, 2, 2, null, false, true, null, false, false, true));
            casas.GetChild(casas.childCount - 1).name = "Fachada_S_02_2p_c2";
            Medidor(true, -76.8f, 0f);
        }

        private static void Casita(bool oriente, int n, float z0, float z1, int pisos, int color)
        {
            Casa(oriente, n, z0, z1, 7f, Estilo(KitS2c.PlantaVivienda, pisos, color, null, false, true, null, false, true, false));
        }

        // ================================================================ Remate del fondo

        private static void Remate(List<Vector3> puntasO)
        {
            // Banqueta de remate (la calle termina en la fachada) y tres fachadas centradas en x = -110.5, pegadas al muro oscuro de las Orillas.
            KitS2c.Losa(o, piso, "BanquetaRemate", new Vector3(XCalle, banqueta - 0.125f, ZRemate + 1.05f), Quaternion.identity,
                new Vector3(XOriente - XPoniente + 4f, ColocarS2cCalle.AltoBanqueta + 0.1f, 2.1f), KitS2c.MatBanqueta(), 2f, true);
            Transform remate = new GameObject("Remate").transform;
            remate.SetParent(o.Raiz, false);
            Transform izq = CasaRemate(1, XCalle - 7.5f, XCalle - 3f, 2f, Estilo(KitS2c.PlantaLocal, 2, 1, null, true, true, KitS2c.Magenta, false, true, false));
            Transform centro = CasaRemate(2, XCalle - 3f, XCalle + 3f, 2f, Estilo(KitS2c.PlantaVivienda, 3, 0, "C20b_PisoDosVentanas_cy", true, true, null, false, false, true));
            Transform der = CasaRemate(3, XCalle + 3f, XCalle + 7.5f, 2f, Estilo(KitS2c.PlantaLocal, 2, 3, null, true, true, KitS2c.Turquesa, false, true, false));
            foreach (Transform t in new[] { izq, centro, der })
            {
                t.SetParent(remate, true);
            }
            Cortina(izq, 0.35f, new Color(1f, 0.7f, 0.35f));
            // Neón grande de la cantina sobre la fachada del centro (NE02 grande, magenta) y otro turquesa de bandera.
            Vector3 c = new Vector3(XCalle, banqueta + 5.6f, ZRemate + 0.12f);
            GameObject neon = KitS2c.Pieza(remate, KitS2c.NeonPared, c - Vector3.up * 0.7f, Quaternion.LookRotation(Vector3.back), Vector3.one * 2.6f, false);
            neon.name = "NeonCantina";
            KitS2c.Luz(remate, c + Vector3.forward * 1.3f, LightType.Point, KitS2c.Magenta, 3.5f, 9f, 0f);
            KitS2c.Neon(o, remate, new Vector3(XCalle + 2.6f, banqueta + 3.2f, ZRemate), Vector3.forward, Vector3.right, KitS2c.Turquesa, true);
            Vector3 cartel = new Vector3(XCalle - 6f, banqueta + 3.1f, ZRemate);
            LetreroHorizontal(cartel, Vector3.forward, 2.2f, new Color(0.65f, 0.9f, 1f), remate);
            // Poste con luz frente al remate.
            puntasO.Add(Poste(new Vector3(XCalle + 4.6f, banqueta, ZRemate + 1.5f), Vector3.back * -1f, arroyo, true));
        }

        // ================================================================ Postes

        private static void Postes(List<Vector3> puntasO, List<Vector3> puntasP)
        {
            // Cada ~14 m alternando lados desde z = -14 (el del andador y el del remate ya están puestos).
            puntasO.Add(Poste(new Vector3(XOriente - 1.8f, banqueta, -14f), Vector3.left, arroyo, true));
            puntasO.Add(Poste(new Vector3(XOriente - 1.8f, banqueta, -34f), Vector3.left, arroyo, false));
            puntasO.Add(Poste(new Vector3(XOriente - 1.8f, banqueta, -72f), Vector3.left, arroyo, true));
            puntasP.Add(Poste(new Vector3(XPoniente + 1.8f, banqueta, -77f), Vector3.right, arroyo, false));
            puntasO.Sort((a, b) => b.z.CompareTo(a.z));
            puntasP.Sort((a, b) => b.z.CompareTo(a.z));
        }

        /// <summary>Poste del kit en la banqueta. Su collider (una caja de 3 x 1.9 m que cubre el brazo) se adelgaza al pie del poste: así la banqueta queda libre.</summary>
        private static Vector3 Poste(Vector3 pie, Vector3 haciaCalle, float pisoCalle, bool transformador)
        {
            Vector3 punta = KitS2c.Poste(o, postes, pie, haciaCalle, pisoCalle, transformador);
            Transform poste = postes.GetChild(postes.childCount - 1);
            AjustarLucesPoste(poste);
            foreach (BoxCollider bc in poste.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 c = bc.center;
                bc.center = new Vector3(0f, c.y, 0f);
                bc.size = new Vector3(AnchoColliderPoste, bc.size.y, AnchoColliderPoste);
            }
            return punta;
        }

        /// <summary>
        /// T-1112: la luz del poste del kit sale con alcance 20 m; los postes de la ciudad llevan los valores de ColocarS2oLuz (alcance corto, ver
        /// LuzK01S2oPruebas.Postes_ElNaranjaQuedaBajoCadaPoste). Los postes de esta calle usan los mismos.
        /// </summary>
        public static void AjustarLucesPoste(Transform poste)
        {
            foreach (Light l in poste.GetComponentsInChildren<Light>(true).Where(ColocarS2oLuz.EsPoste))
            {
                l.range = ColocarS2oLuz.RangoPoste;
                l.intensity = ColocarS2oLuz.IntensidadPoste;
                l.spotAngle = ColocarS2oLuz.AnguloPoste;
                l.innerSpotAngle = ColocarS2oLuz.AnguloPoste * 0.4f;
            }
        }

        /// <summary>Aplica <see cref="AjustarLucesPoste"/> a los postes ya colocados, sin rehacer la calle. Idempotente.</summary>
        [MenuItem("Mecha Cyberpunk/T-1112/Ajustar luces de los postes de la calle del Doc")]
        public static void AjustarLucesEnEscena()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Transform raiz = ColocarS2.Buscar(Raiz + "/Postes");
            if (raiz != null)
            {
                foreach (Transform poste in raiz.Cast<Transform>())
                {
                    AjustarLucesPoste(poste);
                }
                ColocarS2.Informe.AppendLine("Luces ajustadas en " + raiz.childCount + " postes");
            }
            ColocarS2.Guardar("t1112-luces");
        }

        private static void Cablear(List<Vector3> puntas, List<Vector3> mismoLado, List<Vector3> otroLado)
        {
            for (int i = 0; i + 1 < puntas.Count; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    KitS2c.Cable(o, puntas[i] + Vector3.down * (k * 0.22f), puntas[i + 1] + Vector3.down * (k * 0.22f), o.Rango(0.6f, 1f));
                }
            }
            foreach (Vector3 p in puntas)
            {
                foreach (Vector3 c in mismoLado.OrderBy(c => (c - p).sqrMagnitude).Take(3))
                {
                    KitS2c.Cable(o, p + Vector3.down * 0.3f, c, o.Rango(0.25f, 0.5f), 0.02f);
                }
                foreach (Vector3 c in otroLado.OrderBy(c => (c - p).sqrMagnitude).Take(2))
                {
                    KitS2c.Cable(o, p + Vector3.down * 0.1f, c, o.Rango(0.8f, 1.3f));
                }
            }
        }

        // ================================================================ Piezas sueltas

        private static Transform Hijo(string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(o.Raiz, false);
            return t;
        }

        private static GameObject Caja(Transform padre, string nombre, Vector3 centro, Vector3 tam, Material mat, bool collider, float metrosUv = 2f)
        {
            return KitS2c.Losa(o, padre, nombre, centro, Quaternion.identity, tam, mat, metrosUv, collider);
        }

        /// <summary>Silla de plástico mirando a <paramref name="giro"/> grados (0 = +Z); el pie está en <paramref name="pie"/>.</summary>
        private static void Silla(Vector3 pie, float giro)
        {
            Transform s = new GameObject("Silla").transform;
            s.SetParent(props, false);
            s.SetPositionAndRotation(pie, Quaternion.Euler(0f, giro, 0f));
            void Parte(string nombre, Vector3 local, Vector3 tam)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.name = nombre;
                g.transform.SetParent(s, false);
                g.transform.localPosition = local;
                g.transform.localScale = tam;
                g.GetComponent<MeshRenderer>().sharedMaterial = Plastico;
            }
            Parte("Asiento", new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.04f, 0.42f));
            Parte("Respaldo", new Vector3(0f, 0.72f, -0.2f), new Vector3(0.42f, 0.5f, 0.04f));
            foreach (float x in new[] { -0.17f, 0.17f })
            {
                foreach (float z in new[] { -0.17f, 0.17f })
                {
                    Parte("Pata", new Vector3(x, 0.22f, z), new Vector3(0.035f, 0.44f, 0.035f));
                }
            }
        }

        private static void Banco(Vector3 pie, float giro)
        {
            Transform b = new GameObject("Banco").transform;
            b.SetParent(props, false);
            b.SetPositionAndRotation(pie, Quaternion.Euler(0f, giro, 0f));
            void Parte(string nombre, Vector3 local, Vector3 tam)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.name = nombre;
                g.transform.SetParent(b, false);
                g.transform.localPosition = local;
                g.transform.localScale = tam;
                g.GetComponent<MeshRenderer>().sharedMaterial = nombre == "Pata" ? Metal : Madera;
            }
            Parte("Asiento", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.06f, 0.4f));
            Parte("Respaldo", new Vector3(0f, 0.8f, -0.17f), new Vector3(1.6f, 0.4f, 0.05f));
            foreach (float x in new[] { -0.7f, 0.7f })
            {
                Parte("Pata", new Vector3(x, 0.22f, 0f), new Vector3(0.07f, 0.44f, 0.36f));
            }
            BoxCollider bc = b.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.45f, -0.05f);
            bc.size = new Vector3(1.6f, 0.9f, 0.45f);
        }

        private static void Taburete(Vector3 pie)
        {
            Transform t = new GameObject("Taburete").transform;
            t.SetParent(props, false);
            t.position = pie;
            GameObject a = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(a.GetComponent<Collider>());
            a.name = "Asiento";
            a.transform.SetParent(t, false);
            a.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            a.transform.localScale = new Vector3(0.36f, 0.03f, 0.36f);
            a.GetComponent<MeshRenderer>().sharedMaterial = PlasticoRojo;
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.name = "Pata";
            p.transform.SetParent(t, false);
            p.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            p.transform.localScale = new Vector3(0.06f, 0.3f, 0.06f);
            p.GetComponent<MeshRenderer>().sharedMaterial = Metal;
        }

        /// <summary>Medidor de luz pegado a la pared en la banqueta (como los de K05).</summary>
        private static void Medidor(bool oriente, float z, float desfase)
        {
            Vector3 normal = oriente ? Vector3.left : Vector3.right;
            float x = oriente ? XOriente : XPoniente;
            KitS2c.Pieza(props, "C14_RegistroMedidores", new Vector3(x, banqueta + 1.2f, z), Quaternion.LookRotation(-normal), Vector3.one, false).name = "Medidores";
        }

        /// <summary>Letrero horizontal C15a pegado a la fachada, con su luz de color.</summary>
        private static void LetreroHorizontal(Vector3 p, Vector3 normal, float ancho, Color luz, Transform padre = null)
        {
            KitS2c.Medida m = KitS2c.Medir("C15a_LetreroHorizontal");
            float e = ancho / Mathf.Max(m.Caja.size.x, m.Caja.size.z);
            Quaternion giro = Quaternion.LookRotation(-normal);
            Transform dest = padre != null ? padre : props;
            GameObject g = KitS2c.Pieza(dest, "C15a_LetreroHorizontal", p + normal * 0.12f, giro, Vector3.one * e, false);
            g.name = "Letrero";
            KitS2c.Luz(dest, p + normal * 1.2f, LightType.Point, luz, 1.2f, 4.5f, 0f);
        }

        /// <summary>Sube la cortina del local (el garage PU02) la fracción <paramref name="fraccion"/> y deja un interior cálido debajo.</summary>
        private static void Cortina(Transform casa, float fraccion, Color luz)
        {
            Transform baja = casa.GetChild(0);
            Transform puerta = baja.Find("Puerta");
            KitS2c.Medida m = KitS2c.Medir(baja.name.Replace("(Clone)", "").Trim());
            if (puerta == null || !m.Vano.HasValue)
            {
                return;
            }
            Rect v = m.Vano.Value;
            Vector3 e = puerta.localScale;
            puerta.localScale = new Vector3(e.x, e.y * (1f - fraccion), e.z);
            puerta.localPosition += new Vector3(0f, v.height * fraccion, 0f);
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "InteriorCalido";
            q.transform.SetParent(baja, false);
            q.transform.localPosition = new Vector3(v.center.x, v.yMin + v.height * fraccion * 0.5f, m.Muro + 0.02f);
            q.transform.localRotation = Quaternion.identity;
            q.transform.localScale = new Vector3(v.width * 0.98f, v.height * fraccion, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = InteriorMat;
            q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Vector3 mundo = baja.TransformPoint(new Vector3(v.center.x, v.yMin + v.height * fraccion * 0.5f, m.Muro - 0.9f));
            KitS2c.Luz(casa, mundo, LightType.Point, luz, 2.2f, 5f, 0f);
        }

        private static void Neon(Transform casa, bool oriente, Color color)
        {
            // Neón chico del local, de bandera sobre la banqueta.
            float x = oriente ? XOriente : XPoniente;
            Vector3 normal = oriente ? Vector3.left : Vector3.right;
            KitS2c.Neon(o, props, new Vector3(x, banqueta + 3.5f, casa.position.z - 2.4f), normal, Vector3.Cross(Vector3.up, normal), color, true);
        }

        /// <summary>Balcones con barandal en los pisos altos de una casa (de plano, al frente de la fachada).</summary>
        private static void Balcones(Transform casa, bool oriente, float z, float ancho)
        {
            float x = oriente ? XOriente : XPoniente;
            float signo = oriente ? -1f : 1f;
            Transform b = new GameObject("Balcones").transform;
            b.SetParent(casa, false);
            for (int piso = 1; piso <= 2; piso++)
            {
                float y = banqueta + piso * 3.08f + 0.05f;
                if (y > banqueta + 9f)
                {
                    break;
                }
                Caja(b, "Losa", new Vector3(x + signo * 0.42f, y, z), new Vector3(0.84f, 0.08f, ancho * 0.5f), Metal, false);
                Caja(b, "Barandal", new Vector3(x + signo * 0.82f, y + 0.5f, z), new Vector3(0.05f, 0.9f, ancho * 0.5f), Metal, false);
                KitS2c.Pieza(b, "C20d_Macetas", new Vector3(x + signo * 0.5f, y + 0.04f, z + 0.5f), Quaternion.LookRotation(signo * Vector3.right * -1f), Vector3.one * 0.7f, false);
            }
        }

        /// <summary>Un segundo tinaco en la azotea de una vecindad (de las que llevan dos).</summary>
        private static void SegundoTinaco(Transform casa, bool oriente, float z)
        {
            float x = oriente ? XOriente : XPoniente;
            float signo = oriente ? -1f : 1f;
            float alto = casa.Find("Cuerpo").GetComponent<Renderer>().bounds.max.y;
            Vector3 p = new Vector3(x - signo * 4.2f, alto, z + 1.6f);
            KitS2c.Medida mt = KitS2c.Medir(KitS2c.BaseTinaco);
            KitS2c.Pieza(casa, KitS2c.BaseTinaco, p, Quaternion.identity, new Vector3(1.5f / mt.Caja.size.x, 0.7f / mt.Caja.size.y, 1f), false).name = "BaseTinaco_2";
            KitS2c.Pieza(casa, "C09_Tinaco", p + Vector3.up * 0.7f, Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.1f, false).name = "Tinaco_2";
        }

        /// <summary>Callejoncito entre casas: contenedor, bolsas de basura y un muro al fondo.</summary>
        private static void Callejoncito(float z0, float z1)
        {
            Transform c = new GameObject("Callejoncito").transform;
            c.SetParent(props, false);
            float zc = (z0 + z1) * 0.5f;
            float fondo = XOriente + 8f;
            float suelo = KitS2c.Suelo(new Vector3(XOriente + 1f, 0f, zc), 0.2f);
            Caja(c, "MuroFondo", new Vector3(fondo - 0.15f, banqueta + 2.4f, zc), new Vector3(0.3f, 4.8f, Mathf.Abs(z1 - z0)), KitS2c.MatCuerpo(0), true);
            KitS2c.Pieza(c, "L01_Contenedor", new Vector3(XOriente + 2.0f, suelo, zc), Quaternion.Euler(0f, 90f, 0f), Vector3.one, false).name = "Contenedor";
            KitS2c.Pieza(c, "BS01a_Basura", new Vector3(XOriente + 0.9f, suelo, zc + 0.5f), Quaternion.Euler(0f, 25f, 0f), Vector3.one, false);
            KitS2c.Pieza(c, "BS01c_Basura", new Vector3(XOriente + 0.7f, suelo, zc - 0.6f), Quaternion.Euler(0f, 140f, 0f), Vector3.one, false);
            KitS2c.Pieza(c, "C16_Tambo", new Vector3(XOriente + 0.6f, suelo, zc + 0.9f), Quaternion.identity, Vector3.one, false);
        }

        /// <summary>Taquería: parrilla con brasas (luz naranja chica), barra y tres bancos frente al local.</summary>
        private static void Taqueria(float z)
        {
            Transform t = new GameObject("Taqueria").transform;
            t.SetParent(props, false);
            Caja(t, "Barra", new Vector3(XOriente - 0.3f, banqueta + 0.48f, z + 1.6f), new Vector3(0.5f, 0.96f, 3.2f), Madera, true);
            Caja(t, "Parrilla", new Vector3(XOriente - 0.4f, banqueta + 0.4f, z - 1.4f), new Vector3(0.7f, 0.8f, 1.2f), Metal, true);
            GameObject brasa = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(brasa.GetComponent<Collider>());
            brasa.name = "Brasas";
            brasa.transform.SetParent(t, false);
            brasa.transform.position = new Vector3(XOriente - 0.4f, banqueta + 0.82f, z - 1.4f);
            brasa.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            brasa.transform.localScale = new Vector3(0.56f, 1.0f, 1f);
            brasa.GetComponent<MeshRenderer>().sharedMaterial = BrasaMat;
            KitS2c.Luz(t, new Vector3(XOriente - 0.5f, banqueta + 1.3f, z - 1.4f), LightType.Point, new Color(1f, 0.45f, 0.12f), 3.2f, 4.8f, 0f);
            for (int i = 0; i < 3; i++)
            {
                Taburete(new Vector3(XOriente - 1.0f, banqueta, z + 0.5f + i * 1.0f));
            }
        }

        /// <summary>Lote baldío del lado poniente: tierra, malla ciclónica en la línea de fachada, muro con mural al fondo y cosas tiradas.</summary>
        private static void LoteBaldio(float z0, float z1)
        {
            Transform l = new GameObject("LoteBaldio").transform;
            l.SetParent(o.Raiz, false);
            float zc = (z0 + z1) * 0.5f;
            float largo = Mathf.Abs(z1 - z0);
            float xMuro = XPoniente - 7.4f;
            float suelo = KitS2c.Suelo(new Vector3(XPoniente - 3f, 0f, zc), 0.2f);
            Caja(l, "Tierra", new Vector3(XPoniente - 3.7f, suelo + 0.02f, zc), new Vector3(7.4f, 0.04f, largo), Tierra, false);
            // Malla ciclónica con sus postes (alta de 2.3 m), con un portón de lámina a un lado.
            Material malla = MatMalla();
            Caja(l, "MallaCiclonica", new Vector3(XPoniente - 0.15f, banqueta + 1.15f, zc), new Vector3(0.03f, 2.3f, largo - 0.1f), malla, true, 0.5f);
            for (float z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1) + 0.01f; z += largo / 3f)
            {
                Caja(l, "PosteMalla", new Vector3(XPoniente - 0.15f, banqueta + 1.2f, z), new Vector3(0.1f, 2.4f, 0.1f), Metal, true);
            }
            Caja(l, "MallaLado_N", new Vector3(XPoniente - 3.7f, suelo + 1.15f, Mathf.Max(z0, z1) - 0.05f), new Vector3(7.4f, 2.3f, 0.03f), malla, false, 0.5f);
            // Muro del fondo con el mural.
            Caja(l, "MuroMural", new Vector3(xMuro - 0.15f, suelo + 1.75f, zc), new Vector3(0.3f, 3.5f, largo), KitS2c.MatCuerpo(1), true);
            GameObject m = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(m.GetComponent<Collider>());
            m.name = "Mural";
            m.transform.SetParent(l, false);
            m.transform.position = new Vector3(xMuro + 0.01f, suelo + 1.75f, zc);
            m.transform.rotation = Quaternion.LookRotation(Vector3.left);
            m.transform.localScale = new Vector3(largo - 0.3f, 3.2f, 1f);
            m.GetComponent<MeshRenderer>().sharedMaterial = MatMural();
            KitS2c.Luz(l, new Vector3(xMuro + 1.8f, suelo + 3.4f, zc), LightType.Point, new Color(1f, 0.85f, 0.7f), 2.4f, 6.5f, 0f);
            KitS2c.Luz(l, new Vector3(xMuro + 2.5f, suelo + 1.2f, zc + 1.5f), LightType.Point, KitS2c.Magenta, 0.8f, 5f, 0f);
            // Cosas tiradas.
            KitS2c.Pieza(l, "L04_Llantas", new Vector3(XPoniente - 3.2f, suelo, zc + 1.8f), Quaternion.Euler(0f, 40f, 0f), Vector3.one, false);
            KitS2c.Pieza(l, "C17_Huacales", new Vector3(XPoniente - 5.5f, suelo, zc - 2f), Quaternion.Euler(0f, 15f, 0f), Vector3.one, false);
            KitS2c.Pieza(l, "C16_Tambo", new Vector3(XPoniente - 5.9f, suelo, zc + 0.6f), Quaternion.identity, Vector3.one, false);
            KitS2c.Pieza(l, "BS01b_Basura", new Vector3(XPoniente - 2.2f, suelo, zc - 1.2f), Quaternion.Euler(0f, 100f, 0f), Vector3.one, false);
            KitS2c.Pieza(l, "BS01d_Basura", new Vector3(XPoniente - 4.4f, suelo, zc + 2.4f), Quaternion.Euler(0f, 200f, 0f), Vector3.one, false);
        }

        // ================================================================ Materiales dibujados en código

        private static Material MatMural()
        {
            return KitS2c.Mat("T1102_Mural", () =>
            {
                Texture2D t = GuardarTextura("T1102_Mural", DibujarMural(), TextureWrapMode.Clamp);
                Material m = KitS2c.Lit(Color.white, 0.12f, t);
                return m;
            });
        }

        private static Material MatMalla()
        {
            return KitS2c.Mat("T1102_Malla", () =>
            {
                Texture2D t = GuardarTextura("T1102_Malla", DibujarMalla(), TextureWrapMode.Repeat);
                Material m = KitS2c.Lit(new Color(0.55f, 0.58f, 0.6f), 0.4f, t);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
                m.SetOverrideTag("RenderType", "TransparentCutout");
                return m;
            });
        }

        private static Texture2D GuardarTextura(string nombre, Texture2D t, TextureWrapMode envolver)
        {
            Directory.CreateDirectory(CarpetaTexturas);
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
            ti.wrapMode = envolver;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>Mural de colores: cielo de atardecer, sol, montañas y una greca turquesa y magenta abajo.</summary>
        private static Texture2D DibujarMural()
        {
            const int w = 512, h = 192;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color arriba = new Color(0.16f, 0.12f, 0.42f), medio = new Color(0.95f, 0.32f, 0.5f), abajo = new Color(1f, 0.72f, 0.2f);
            Color turquesa = new Color(0.1f, 0.78f, 0.74f), magenta = new Color(0.85f, 0.12f, 0.6f), oscuro = new Color(0.08f, 0.07f, 0.16f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float v = y / (float)(h - 1);
                    Color c = v < 0.5f ? Color.Lerp(abajo, medio, v * 2f) : Color.Lerp(medio, arriba, (v - 0.5f) * 2f);
                    float dx = x - w * 0.62f, dy = (y - h * 0.46f) * 1.4f;
                    if (dx * dx + dy * dy < 40f * 40f)
                    {
                        c = new Color(1f, 0.94f, 0.55f);
                    }
                    float m1 = 38f + 24f * Mathf.Abs(Mathf.Sin(x * 0.021f)) + 16f * Mathf.Abs(Mathf.Sin(x * 0.047f + 1f));
                    if (y < m1)
                    {
                        c = ((x / 24) % 2 == 0) ? turquesa * 0.8f : oscuro;
                        if (y < 26) { c = ((x / 16 + y / 8) % 2 == 0) ? magenta : turquesa; }
                    }
                    float m2 = 58f + 18f * Mathf.Abs(Mathf.Sin(x * 0.013f + 2f)) + 10f * Mathf.Abs(Mathf.Sin(x * 0.06f));
                    if (y > m1 && y < m2 * 0.7f && x < w * 0.45f)
                    {
                        c = Color.Lerp(c, new Color(0.3f, 0.12f, 0.45f), 0.8f);
                    }
                    if (y > 150 && y < 164)
                    {
                        c = ((x / 12) % 2 == 0) ? turquesa : magenta;
                    }
                    t.SetPixel(x, y, new Color(c.r, c.g, c.b, 1f));
                }
            }
            t.Apply();
            return t;
        }

        /// <summary>Rombos de alambre (alfa) para la malla ciclónica.</summary>
        private static Texture2D DibujarMalla()
        {
            const int n = 32;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    bool linea = Mathf.Abs(((x + y) % 16) - 0f) < 1.5f || Mathf.Abs((((x - y) % 16 + 16) % 16) - 0f) < 1.5f;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, linea ? 1f : 0f));
                }
            }
            t.Apply();
            return t;
        }
    }
}
