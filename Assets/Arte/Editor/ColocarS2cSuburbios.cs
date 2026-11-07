using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 3 de docs/plan-look-ciudad.md (§4, M11): Suburbios con el kit de manzanas, empezando por la ruta de la demo.
    /// - La manzana de la casa (Manzana_4) queda en cuadras cortas: la calle de prueba de K01 (z = -43) y una calle nueva de norte a sur
    ///   (x = -110.5) que la cruza; en el cruce van las cuatro esquinas de K05 (una con la farmacia). Sin tráfico, como la calle de prueba.
    /// - Los edificios greybox que quedaban en esa manzana (35, 36, 38, 39, 41 y 44) se cambian por casas de 2 pisos del kit en todas sus
    ///   calles. Se quedan la casa, el I+D+I del Doc, el callejón de los SKULLS con su salida al poniente y los edificios 33 y 42 que son sus muros.
    /// - La calle de K01 sigue al oriente, cruzando la calle de la casa, por la fila de en medio de la Manzana_5 (edificios 46, 49 y 51) hasta la
    ///   calle de x = 47, con cruces peatonales pintados en las dos calles con tráfico. El taller El Canepa y el bar se quedan.
    /// Idempotente: rehace su raíz S2c_Suburbios. Antes rehace la calle de prueba (que deja el hueco del cruce).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cSuburbios.Colocar
    /// </summary>
    public static class ColocarS2cSuburbios
    {
        public const string Raiz = "S2c_Suburbios";
        public const int Semilla = 7220;
        public const float Fondo = 8f;
        public const float Lote = 6f;
        public const float ZAvenida = -10f;
        public const float ZCalleSur = -84f;
        public const float ZAndador = -64f;      // orilla sur del andador que sale del callejón al poniente (T-858)
        public const float ZAndadorNorte = -59f;
        public const float XNorteSurOriente = ColocarS2cCalle.XCruce + ColocarS2cCalle.MedioCruce;
        public const float XNorteSurPoniente = ColocarS2cCalle.XCruce - ColocarS2cCalle.MedioCruce;
        public const float XIdiPoniente = -104f;
        public const float XIdiOriente = -96f;
        public const float ZIdiSur = -16f;
        /// <summary>Tramo de la calle de K01 dentro de la Manzana_5 (de la banqueta de la calle de la casa a la de la calle de x = 47).</summary>
        public const float XOrienteDesde = -40f;
        public const float XOrienteHasta = 40f;
        public const float XFachadasOriente0 = -37f;
        public const float XFachadasOriente1 = 37f;

        public static readonly string[] Edificios =
        {
            "Barrio/Manzana_4/Edificios/Edificio_35", "Barrio/Manzana_4/Edificios/Edificio_36", "Barrio/Manzana_4/Edificios/Edificio_38",
            "Barrio/Manzana_4/Edificios/Edificio_39", "Barrio/Manzana_4/Edificios/Edificio_41", "Barrio/Manzana_4/Edificios/Edificio_44",
            "Barrio/Manzana_5/Edificios/Edificio_46", "Barrio/Manzana_5/Edificios/Edificio_49", "Barrio/Manzana_5/Edificios/Edificio_51",
        };

        // Tomas de revisión (desde, hacia).
        public static readonly (string nombre, Vector3 desde, Vector3 hacia)[] Tomas =
        {
            ("cruce-k05", new Vector3(-108.6f, 2.7f, -62f), new Vector3(-106f, 1.4f, -36f)),
            ("calle-ns", new Vector3(-110.5f, 2.6f, -14f), new Vector3(-110.5f, 1.4f, -60f)),
            ("avenida", new Vector3(-75f, 2.6f, -4f), new Vector3(-120f, 3f, -12f)),
            ("calle-casa", new Vector3(-47f, 2.6f, -88f), new Vector3(-47f, 2f, -20f)),
            ("oriente", new Vector3(-44f, 2.5f, -43.4f), new Vector3(30f, 1.4f, -43f)),
            ("alto", new Vector3(-60f, 60f, -110f), new Vector3(-75f, 0f, -45f)),
        };

        [MenuItem("Mecha Cyberpunk/S2c/3 Suburbios (cuadras cortas con el kit)")]
        public static void Colocar()
        {
            ColocarS2cCalle.Colocar();
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            List<Bounds> huellas = Edificios.Select(ColocarS2.Buscar).Where(t => t != null).Select(t => Huella(t.gameObject)).ToList();
            int n = ColocarS2cCalle.ApagarEdificios(Edificios);
            // El letrero I+D+I del Doc (T-540) es del local que se queda: si una pasada anterior lo apagó, se vuelve a prender.
            Transform idi = ColocarS2.Buscar("T542_Letreros/L13/Visual");
            if (idi != null)
            {
                idi.gameObject.SetActive(true);
            }
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            float arroyo = KitS2c.Suelo(new Vector3(-110.5f, 0f, -70f), 0.2f) + 0.02f;
            float banqueta = arroyo + ColocarS2cCalle.AltoBanqueta;
            const float perimetro = 0.2f;   // banquetas del barrio
            Transform piso = Hijo(o.Raiz, "Piso");
            Transform casas = Hijo(o.Raiz, "Casas");
            Transform postes = Hijo(o.Raiz, "Postes");

            // ---- Calle de norte a sur (x = -110.5): arroyo en dos tramos (el cruce es de la calle de K01), banquetas, líneas, postes.
            float xa0 = XNorteSurPoniente + ColocarS2cCalle.Banqueta, xa1 = XNorteSurOriente - ColocarS2cCalle.Banqueta;
            huellas.Add(Caja(xa0 - 2.5f, xa1 + 2.5f, -87f, -7f));
            foreach ((float z0, float z1) in new[] { (-87f, ColocarS2cCalle.ZCentro - 3.75f), (ColocarS2cCalle.ZCentro + 3.75f, -7f) })
            {
                KitS2c.Losa(o, piso, "ArroyoNS", new Vector3(ColocarS2cCalle.XCruce, arroyo - 0.05f, (z0 + z1) * 0.5f), Quaternion.identity, new Vector3(xa1 - xa0, 0.1f, z1 - z0), KitS2c.MatAsfalto(), 8f, true);
                for (float z = z0 + 1f; z < z1 - 2f; z += 4f)
                {
                    KitS2c.Linea(piso, new Vector3(ColocarS2cCalle.XCruce, arroyo, z), new Vector3(ColocarS2cCalle.XCruce, arroyo, z + 1.6f), 0.12f, arroyo);
                }
            }
            foreach ((float z0, float z1) in new[] { (ZCalleSur, ColocarS2cCalle.ZSur), (ColocarS2cCalle.ZNorte, ZAvenida) })
            {
                foreach ((float x0, float x1) in new[] { (XNorteSurPoniente, xa0), (xa1, XNorteSurOriente) })
                {
                    KitS2c.Losa(o, piso, "BanquetaNS", new Vector3((x0 + x1) * 0.5f, banqueta - 0.125f, (z0 + z1) * 0.5f), Quaternion.identity,
                        new Vector3(x1 - x0, ColocarS2cCalle.AltoBanqueta + 0.1f, z1 - z0), KitS2c.MatBanqueta(), 2f, true);
                }
            }
            List<Vector3> puntasOriente = new List<Vector3>(), puntasPoniente = new List<Vector3>();
            foreach (float z in new[] { -74f, -24f })
            {
                puntasOriente.Add(KitS2c.Poste(o, postes, new Vector3(xa1 + 0.45f, banqueta, z), Vector3.left, arroyo, true));
            }
            foreach (float z in new[] { -66f, -18f })
            {
                puntasPoniente.Add(KitS2c.Poste(o, postes, new Vector3(xa0 - 0.45f, banqueta, z), Vector3.right, arroyo, o.Chance(0.5f)));
            }

            // ---- Las cuatro esquinas de K05 en el cruce (la del noreste con la farmacia).
            (Vector3 esquina, float giro)[] esquinas =
            {
                (new Vector3(XNorteSurOriente, 0f, ColocarS2cCalle.ZNorte), 0f), (new Vector3(XNorteSurPoniente, 0f, ColocarS2cCalle.ZNorte), -90f),
                (new Vector3(XNorteSurPoniente, 0f, ColocarS2cCalle.ZSur), 180f), (new Vector3(XNorteSurOriente, 0f, ColocarS2cCalle.ZSur), 90f),
            };
            for (int k = 0; k < esquinas.Length; k++)
            {
                ColocarS2cKit.Esquina(o, casas, esquinas[k].esquina + Vector3.up * 0f, arroyo, Quaternion.Euler(0f, esquinas[k].giro, 0f), 7.5f, k == 0, false);
            }

            // ---- Casas de las cuadras cortas de la Manzana_4 (frentes con el kit; cada esquina con calle va con lote de dos frentes).
            List<Vector3> altosOriente = new List<Vector3>(), altosPoniente = new List<Vector3>();
            // Calle de norte a sur, lado oriente (mira al poniente) y lado poniente (mira al oriente).
            altosOriente.AddRange(Frente(o, casas, "C", XNorteSurOriente, ColocarS2cCalle.ZNorte + Lote, XNorteSurOriente, ZIdiSur, Vector3.left, Fondo, banqueta));
            altosOriente.AddRange(Frente(o, casas, "A", XNorteSurOriente, ColocarS2cCalle.ZSur - Lote, XNorteSurOriente, ZAndadorNorte, Vector3.left, 6f, banqueta));
            altosOriente.AddRange(Frente(o, casas, "F", XNorteSurOriente, ZAndador, XNorteSurOriente, ZCalleSur + Lote, Vector3.left, 6f, banqueta));
            altosPoniente.AddRange(Frente(o, casas, "B", XNorteSurPoniente, ColocarS2cCalle.ZNorte + Lote, XNorteSurPoniente, ZAvenida - Lote, Vector3.right, 7f, banqueta));
            altosPoniente.AddRange(Frente(o, casas, "A", XNorteSurPoniente, ColocarS2cCalle.ZSur - Lote, XNorteSurPoniente, ZAndadorNorte, Vector3.right, 6f, banqueta));
            altosPoniente.AddRange(Frente(o, casas, "E", XNorteSurPoniente, ZAndador, XNorteSurPoniente, ZCalleSur + Lote, Vector3.right, 7f, banqueta));
            // Avenida (z = -10, mira al norte): cuadra noroeste en dos lotes de esquina; la del I+D+I del Doc deja su frente.
            float medioNO = (ColocarS2cCalle.XPoniente + XNorteSurPoniente) * 0.5f;
            Esquina(o, casas, new Vector3(ColocarS2cCalle.XPoniente, perimetro, ZAvenida), Vector3.forward, Vector3.left, medioNO - ColocarS2cCalle.XPoniente, Lote);
            Esquina(o, casas, new Vector3(XNorteSurPoniente, perimetro, ZAvenida), Vector3.forward, Vector3.right, XNorteSurPoniente - medioNO, Lote);
            Frente(o, casas, "B", XIdiOriente, ZAvenida, ColocarS2cCalle.XOriente - Lote, ZAvenida, Vector3.forward, Fondo, perimetro);
            Esquina(o, casas, new Vector3(ColocarS2cCalle.XOriente, perimetro, ZAvenida), Vector3.forward, Vector3.right, Lote, Lote);
            // Calle de la casa (x = -57, mira al oriente), entre la esquina de la avenida y la casa de esquina de la calle de K01.
            Frente(o, casas, "C", ColocarS2cCalle.XOriente, ZAvenida - Lote, ColocarS2cCalle.XOriente, -32f, Vector3.right, Fondo, perimetro);
            // Calle del poniente (x = -131, mira al poniente).
            Frente(o, casas, "F", ColocarS2cCalle.XPoniente, ZAvenida - Lote, ColocarS2cCalle.XPoniente, ColocarS2cCalle.ZNorte, Vector3.left, 7f, perimetro);
            Frente(o, casas, "D", ColocarS2cCalle.XPoniente, ZAndador, ColocarS2cCalle.XPoniente, ZCalleSur + Lote, Vector3.left, 7f, perimetro);
            // Calle del sur (z = -84, mira al sur): esquinas con la calle del poniente y con la de norte a sur; hasta el edificio 42.
            float medioSO = (ColocarS2cCalle.XPoniente + XNorteSurPoniente) * 0.5f;
            Esquina(o, casas, new Vector3(ColocarS2cCalle.XPoniente, perimetro, ZCalleSur), Vector3.back, Vector3.left, medioSO - ColocarS2cCalle.XPoniente, Lote);
            Esquina(o, casas, new Vector3(XNorteSurPoniente, perimetro, ZCalleSur), Vector3.back, Vector3.right, XNorteSurPoniente - medioSO, Lote);
            Esquina(o, casas, new Vector3(XNorteSurOriente, perimetro, ZCalleSur), Vector3.back, Vector3.left, Lote, Lote);
            Frente(o, casas, "D", XNorteSurOriente + Lote, ZCalleSur, ColocarS2cCalle.XMedio, ZCalleSur, Vector3.back, ZAndador - ZCalleSur, perimetro);
            Cablear(o, puntasOriente, altosOriente, altosPoniente);
            Cablear(o, puntasPoniente, altosPoniente, altosOriente);

            // ---- La calle de K01 al oriente, por la Manzana_5.
            huellas.Add(Caja(XOrienteDesde, XOrienteHasta, ColocarS2cCalle.ZSur, ColocarS2cCalle.ZNorte));
            CalleOriente(o, arroyo, banqueta, perimetro);

            // ---- Cruces peatonales en las calles con tráfico (calle de la casa y calle de x = 47).
            foreach (float xc in new[] { -47f, 47f })
            {
                float pisoCalle = KitS2c.Suelo(new Vector3(xc, 0f, ColocarS2cCalle.ZCentro + 9f), 0f);
                for (float x = xc - 6.3f; x <= xc + 6.3f; x += 1.05f)
                {
                    KitS2c.Linea(piso, new Vector3(x, pisoCalle, ColocarS2cCalle.ZCentro - 1.5f), new Vector3(x, pisoCalle, ColocarS2cCalle.ZCentro + 1.5f), 0.5f, pisoCalle);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                KitS2c.Charco(o, piso, new Vector3(ColocarS2cCalle.XCruce + o.Rango(-2.6f, 2.6f), arroyo, o.Rango(-84f, -12f)), o.Rango(1.2f, 3.2f), o.Rango(0.8f, 1.8f), o.Rango(0f, 180f));
            }
            n += ColocarS2cCalle.ApagarEn(huellas.Select(h => { Bounds b = h; b.Expand(new Vector3(6f, 0f, 6f)); return b; }), false);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Suburbios: " + n + " objetos apagados, " + o.Casas + " casas, " + o.Neones + " neones, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos + " charcos");
            ColocarS2.Guardar("s2c-suburbios");
        }

        /// <summary>El tramo de la calle de K01 dentro de la Manzana_5, con sus casas, postes y cables.</summary>
        private static void CalleOriente(KitS2c.Obra o, float arroyo, float banqueta, float perimetro)
        {
            Transform raiz = Hijo(o.Raiz, "CalleOriente");
            float zs = ColocarS2cCalle.ZSur, zn = ColocarS2cCalle.ZNorte;
            float za0 = zs + ColocarS2cCalle.Banqueta, za1 = zn - ColocarS2cCalle.Banqueta;
            KitS2c.Losa(o, raiz, "Arroyo", new Vector3((XOrienteDesde + XOrienteHasta) * 0.5f, arroyo - 0.05f, ColocarS2cCalle.ZCentro), Quaternion.identity,
                new Vector3(XOrienteHasta - XOrienteDesde, 0.1f, za1 - za0), KitS2c.MatAsfalto(), 8f, true);
            foreach ((float z0, float z1) in new[] { (zs - 0.3f, za0), (za1, zn + 0.3f) })
            {
                KitS2c.Losa(o, raiz, "Banqueta", new Vector3(0f, banqueta - 0.125f, (z0 + z1) * 0.5f), Quaternion.identity,
                    new Vector3(XFachadasOriente1 - XFachadasOriente0, ColocarS2cCalle.AltoBanqueta + 0.1f, z1 - z0), KitS2c.MatBanqueta(), 2f, true);
            }
            for (float x = XOrienteDesde + 1f; x < XOrienteHasta - 2f; x += 4f)
            {
                KitS2c.Linea(raiz, new Vector3(x, arroyo, ColocarS2cCalle.ZCentro), new Vector3(x + 1.6f, arroyo, ColocarS2cCalle.ZCentro), 0.12f, arroyo);
            }
            // Al sur: el muro norte del taller El Canepa y de los edificios 48 y 50 está en z = -59.
            List<Vector3> sur = Frente(o, raiz, "C", XFachadasOriente0, zs, XFachadasOriente1, zs, Vector3.forward, zs - ZAndadorNorte, banqueta);
            // Al norte: el bar y los edificios 47 y 52 empiezan en z = -35.
            List<Vector3> norte = Frente(o, raiz, "B", XFachadasOriente0, zn, XFachadasOriente1, zn, Vector3.back, -35f - zn, banqueta);
            Frente(o, raiz, "A", XFachadasOriente0, ZAndadorNorte, XFachadasOriente0, zs, Vector3.left, 6f, perimetro);
            Frente(o, raiz, "E", XFachadasOriente1, zs, XFachadasOriente1, ZAndadorNorte, Vector3.right, 6f, perimetro);
            foreach (float x in new[] { XFachadasOriente0, XFachadasOriente1 })
            {
                KitS2c.Losa(o, raiz, "Muro", new Vector3(x - Mathf.Sign(x) * 1.2f, perimetro + 3.2f, (zn - 35f) * 0.5f), Quaternion.identity, new Vector3(2.4f, 6.4f, -35f - zn), KitS2c.MatCuerpo(0), 3f, true);
            }
            List<Vector3> pn = new List<Vector3>(), ps = new List<Vector3>();
            foreach (float x in new[] { -28f, 4f, 30f })
            {
                pn.Add(KitS2c.Poste(o, raiz, new Vector3(x, banqueta, za1 + 0.45f), Vector3.back, arroyo, true));
            }
            foreach (float x in new[] { -14f, 18f })
            {
                ps.Add(KitS2c.Poste(o, raiz, new Vector3(x, banqueta, za0 - 0.45f), Vector3.forward, arroyo, o.Chance(0.5f)));
            }
            Cablear(o, pn, norte, sur);
            Cablear(o, ps, sur, norte);
            for (int i = 0; i < 9; i++)
            {
                KitS2c.Charco(o, raiz, new Vector3(o.Rango(XOrienteDesde + 2f, XOrienteHasta - 2f), arroyo, ColocarS2cCalle.ZCentro + o.Rango(-3f, 3f)), o.Rango(1.2f, 3.2f), o.Rango(0.8f, 1.8f), o.Rango(0f, 180f));
            }
        }

        /// <summary>Frente del kit con la receta de un frente de cuadra (A–F) de (x0, z0) a (x1, z1), mirando a <paramref name="normal"/>.</summary>
        private static List<Vector3> Frente(KitS2c.Obra o, Transform padre, string receta, float x0, float z0, float x1, float z1, Vector3 normal, float fondo, float piso)
        {
            if (new Vector2(x1 - x0, z1 - z0).magnitude < 2f)
            {
                return new List<Vector3>();
            }
            return KitS2c.Frente(o, padre, new Vector3(x0, piso, z0), new Vector3(x1, piso, z1), normal, fondo, i => ColocarS2cKit.Receta(receta, o, i));
        }

        /// <summary>
        /// Lote de esquina con dos frentes (como la casa de esquina de K05): <paramref name="punto"/> es la esquina de las dos líneas de fachada,
        /// <paramref name="a"/> y <paramref name="b"/> las normales de las dos calles; mide <paramref name="anchoA"/> sobre la calle a y <paramref name="anchoB"/> sobre la b.
        /// </summary>
        private static void Esquina(KitS2c.Obra o, Transform padre, Vector3 punto, Vector3 a, Vector3 b, float anchoA, float anchoB)
        {
            // Sobre la calle a la casa corre en la dirección -b (hacia adentro de la cuadra desde la calle b) y al revés.
            Vector3 centroA = punto - b * (anchoA * 0.5f);
            Vector3 centroB = punto - a * (anchoB * 0.5f);
            KitS2c.Casa(o, padre, centroA, a, anchoA, anchoB, KitS2c.EstiloAlAzar(o));
            KitS2c.Casa(o, padre, centroB, b, anchoB, anchoA, KitS2c.EstiloAlAzar(o));
        }

        private static void Cablear(KitS2c.Obra o, List<Vector3> puntas, List<Vector3> mismoLado, List<Vector3> otroLado)
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
                foreach (Vector3 c in otroLado.OrderBy(c => (c - p).sqrMagnitude).Take(3))
                {
                    KitS2c.Cable(o, p + Vector3.down * 0.1f, c, o.Rango(0.8f, 1.3f));
                }
            }
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static Bounds Caja(float x0, float x1, float z0, float z1)
        {
            return new Bounds(new Vector3((x0 + x1) * 0.5f, 20f, (z0 + z1) * 0.5f), new Vector3(Mathf.Abs(x1 - x0), 40f, Mathf.Abs(z1 - z0)));
        }

        private static Bounds Huella(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.one);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            b.center = new Vector3(b.center.x, 20f, b.center.z);
            b.size = new Vector3(b.size.x, 40f, b.size.z);
            return b;
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/S2c/3b Fotos de Suburbios")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/s2c-suburbios-" + nombre + ".png", nombre == "alto" ? 60f : 66f);
            }
            ColocarS2.LadoALado("imageassets/referencias/look/K05_EsquinaBarrio.png", "docs/capturas/s2c-suburbios-cruce-k05.png", "docs/capturas/s2c-suburbios-k05.png");
            ColocarS2.LadoALado("imageassets/referencias/look/K01_Barrio_HUDnuevo.png", "docs/capturas/s2c-suburbios-oriente.png", "docs/capturas/s2c-suburbios-k01-oriente.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        /// <summary>Batchmode: coloca Suburbios y toma sus fotos.</summary>
        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }
    }
}
