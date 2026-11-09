using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 2 de docs/plan-look-ciudad.md §3: el kit de manzanas a partir de la calle de prueba de K01.
    /// - Seis frentes de cuadra (A–F) de 60 m como prefabs en Assets/Arte/Mundo/S2c/Kit/: cada uno es una receta de casas del kit C (qué planta
    ///   baja, qué colores, cuántos neones y focos, qué cosas en la banqueta) más su banqueta, postes con luz y halo, y cables entre ellos.
    ///   La ciudad usa la misma receta (<see cref="Receta"/>) para frentes de cualquier largo, así todo sale de piezas ya aprobadas.
    /// - La esquina de K05 (Esquina_K05.prefab): casa de esquina con dos frentes (farmacia con su letrero FARMACIA y cruz verde, tienda con
    ///   cortina), banqueta en L, semáforo, nombre de calle y cruce peatonal pintado.
    /// - La variante de Nova (Frente_Nova.prefab, K06): edificios altos de oficinas, banqueta ancha con bolardos y botes, cámaras, luz fría.
    /// El origen de cada prefab es la línea de fachada a nivel del arroyo; la calle queda hacia -Z y el frente corre hacia +X.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cKit.Colocar
    /// </summary>
    public static class ColocarS2cKit
    {
        public const string CarpetaKit = KitS2c.Carpeta + "/Kit";
        public const float LargoFrente = 60f;
        public const float Fondo = 8f;
        public const float Banqueta = 2f;
        public static readonly Vector3 Taller = new Vector3(3000f, 0f, 3000f);
        public static readonly string[] Frentes = { "A", "B", "C", "D", "E", "F" };

        /// <summary>La receta de un frente: el estilo de la casa i (con su azar) y lo que lleva la banqueta.</summary>
        public static KitS2c.Estilo Receta(string frente, KitS2c.Obra o, int i)
        {
            KitS2c.Estilo e = KitS2c.EstiloAlAzar(o);
            switch (frente)
            {
                case "A": // Vivienda: casi todo C02 con ventanas y macetas, colores suaves, pocos neones.
                    e.PlantaBaja = o.Chance(0.85f) ? KitS2c.PlantaVivienda : KitS2c.PlantaLocal;
                    e.Neon = o.Chance(0.15f) ? KitS2c.Turquesa : (Color?)null;
                    e.ColorPiso = i % 2 == 0 ? 0 : 2 + o.Azar.Next(2);
                    e.Foco = true;
                    break;
                case "B": // Locales: cortinas y toldos, neón cada 2 casas.
                    e.PlantaBaja = o.Chance(0.85f) ? KitS2c.PlantaLocal : KitS2c.PlantaVivienda;
                    e.Neon = i % 2 == 0 ? (o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa) : (Color?)null;
                    break;
                case "C": // Mixta, como la calle de K01.
                    e.PlantaBaja = i % 2 == 0 ? KitS2c.PlantaLocal : KitS2c.PlantaVivienda;
                    e.Neon = i % 3 == 1 ? KitS2c.Magenta : i % 3 == 2 ? KitS2c.Turquesa : (Color?)null;
                    break;
                case "D": // Talleres y bodegas: cortinas, casas más anchas, sin neón, antenas y aires.
                    e.PlantaBaja = KitS2c.PlantaLocal;
                    e.Neon = null;
                    e.Antena = o.Chance(0.4f);
                    e.AireAzotea = true;
                    e.ColorPiso = o.Chance(0.5f) ? 0 : 1;
                    break;
                case "E": // Neón: casi cada casa con su letrero, ventanas más prendidas.
                    e.Neon = o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa;
                    e.LucesVentanas = 0.65f;
                    break;
                case "F": // Popular con piso de C20 (macetas, dos ventanas) y tinaco siempre.
                    e.PisoAlto = o.Chance(0.5f) ? "C20b_PisoDosVentanas_cy" : KitS2c.PisoFachada;
                    e.Tinaco = true;
                    e.ColorPiso = o.Azar.Next(KitS2c.ColoresPiso.Length);
                    break;
            }
            return e;
        }

        /// <summary>Cosas de banqueta de cada frente (cada ~9 m, al azar con la receta).</summary>
        private static readonly Dictionary<string, string[]> Banquetas = new Dictionary<string, string[]>
        {
            { "A", new[] { "C20d_Macetas", "C11_Basurero", "C16_Tambo" } },
            { "B", new[] { "C17_Huacales", "CY06_Expendedora", "C11_Basurero" } },
            { "C", new[] { "C12_PuestoTacos", "C16_Tambo", "C17_Huacales" } },
            { "D", new[] { "L04_Llantas", "C16_Tambo", "L01_Contenedor" } },
            { "E", new[] { "CY06_Expendedora", "C13_PuestoCiber", "CY10_CargadorCalle" } },
            { "F", new[] { "C17_Huacales", "C16_Tambo", "C20d_Macetas" } },
        };

        [MenuItem("Mecha Cyberpunk/S2c/2 Kit de manzanas (frentes A-F, esquina K05, Nova K06)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaKit);
            AssetDatabase.Refresh();
            for (int k = 0; k < Frentes.Length; k++)
            {
                GuardarPrefab("Frente_" + Frentes[k], o => FrenteCuadra(o, o.Raiz, Vector3.zero, Vector3.right, LargoFrente, Frentes[k], 0f, true), 8800 + k);
            }
            GuardarPrefab("Esquina_K05", o => Esquina(o, o.Raiz, Vector3.zero, 0f, Quaternion.identity), 8900);
            GuardarPrefab("Frente_Nova", o => FrenteNova(o, o.Raiz, Vector3.zero, Vector3.right, LargoFrente, 0f), 8950);
            File.WriteAllText("Logs/s2c-kit.txt", ColocarS2.Informe.ToString());
            Debug.Log("[Arte] Kit de manzanas S2c:\n" + ColocarS2.Informe);
        }

        /// <summary>Batchmode: hace los prefabs del kit y sus fotos.</summary>
        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }

        private static void GuardarPrefab(string nombre, System.Action<KitS2c.Obra> armar, int semilla)
        {
            KitS2c.Obra o = KitS2c.Empezar("S2cKit_" + nombre, semilla);
            o.Raiz.position = Taller;
            armar(o);
            KitS2c.Terminar(o);
            o.Raiz.position = Vector3.zero;
            string ruta = CarpetaKit + "/" + nombre + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(o.Raiz.gameObject, ruta);
            Object.DestroyImmediate(o.Raiz.gameObject);
            ColocarS2.Informe.AppendLine(nombre + ": " + o.Casas + " casas, " + o.Neones + " neones, " + o.Postes + " postes, " + o.Cables + " cables → " + ruta);
        }

        /// <summary>
        /// Un frente de cuadra con su banqueta: de <paramref name="inicio"/> a lo largo de <paramref name="direccion"/>, la calle a la derecha
        /// de la marcha (normal = dirección girada -90°). <paramref name="piso"/> es la altura del arroyo. Devuelve los puntos altos de las casas
        /// y las puntas de los postes (para colgar cables que crucen la calle).
        /// </summary>
        public static (List<Vector3> altos, List<Vector3> puntas) FrenteCuadra(KitS2c.Obra o, Transform padre, Vector3 inicio, Vector3 direccion, float largo, string receta, float piso, bool conPostes)
        {
            direccion.y = 0f;
            direccion.Normalize();
            Vector3 normal = new Vector3(direccion.z, 0f, -direccion.x);
            Transform raiz = new GameObject("Frente_" + receta).transform;
            raiz.SetParent(padre, false);
            Vector3 a = o.Raiz.position + inicio;
            Vector3 b = a + direccion * largo;
            float banqueta = piso + ColocarS2cCalle.AltoBanqueta;
            Quaternion giro = Quaternion.LookRotation(direccion);
            KitS2c.Losa(o, raiz, "Banqueta", (a + b) * 0.5f + normal * (Banqueta * 0.5f) + Vector3.up * (banqueta - 0.125f), giro,
                new Vector3(Banqueta, ColocarS2cCalle.AltoBanqueta + 0.1f, largo), KitS2c.MatBanqueta(), 2f, true);
            List<Vector3> altos = KitS2c.Frente(o, raiz, a + Vector3.up * banqueta, b + Vector3.up * banqueta, normal, Fondo, i => Receta(receta, o, i));
            string[] cosas = Banquetas[receta];
            for (float x = o.Rango(3f, 6f); x < largo - 2f; x += o.Rango(7f, 11f))
            {
                string c = o.Uno(cosas);
                Vector3 p = a + direccion * x + normal * 0.7f + Vector3.up * banqueta;
                KitS2c.Pieza(raiz, c, p, Quaternion.LookRotation(normal) * Quaternion.Euler(0f, o.Rango(-8f, 8f), 0f), Vector3.one);
            }
            List<Vector3> puntas = new List<Vector3>();
            if (conPostes)
            {
                for (float x = 6f; x < largo - 3f; x += 24f)
                {
                    puntas.Add(KitS2c.Poste(o, raiz, a + direccion * x + normal * (Banqueta - 0.45f) + Vector3.up * banqueta, normal, piso, o.Chance(0.5f)));
                }
                for (int i = 0; i + 1 < puntas.Count; i++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        KitS2c.Cable(o, puntas[i] + Vector3.down * (k * 0.22f), puntas[i + 1] + Vector3.down * (k * 0.22f), o.Rango(0.55f, 0.95f));
                    }
                }
                foreach (Vector3 p in puntas)
                {
                    foreach (Vector3 c in altos.OrderBy(c => (c - p).sqrMagnitude).Take(3))
                    {
                        KitS2c.Cable(o, p + Vector3.down * 0.3f, c, o.Rango(0.25f, 0.5f), 0.02f);
                    }
                }
            }
            for (int i = 0; i < 2; i++)
            {
                KitS2c.Charco(o, raiz, a + direccion * o.Rango(4f, largo - 4f) + normal * o.Rango(0.6f, 1.4f) + Vector3.up * banqueta, o.Rango(0.8f, 1.3f), o.Rango(0.5f, 0.9f), o.Rango(0f, 180f));
            }
            return (altos, puntas);
        }

        /// <summary>
        /// La esquina de K05 en <paramref name="esquina"/> (punto donde se cruzan las dos líneas de fachada, a nivel del arroyo). Sin girar
        /// (<paramref name="giro"/> = identidad), una calle corre hacia +X con la banqueta hacia -Z y la otra hacia +Z con la banqueta hacia -X;
        /// la casa de esquina queda en el cuadrante +X +Z. Lleva farmacia y tienda, banqueta en L, semáforo, letrero de nombre de calle, poste con
        /// cables en diagonal y cruces peatonales pintados en los dos arroyos (<paramref name="ancho"/> es el ancho de cada arroyo).
        /// </summary>
        public static void Esquina(KitS2c.Obra o, Transform padre, Vector3 esquina, float piso, Quaternion giro, float ancho = 7.5f, bool farmacia = true, bool conBanqueta = true)
        {
            Transform raiz = new GameObject("Esquina_K05").transform;
            raiz.SetParent(padre, false);
            Vector3 c = o.Raiz.position + esquina;
            Vector3 P(float x, float y, float z) => c + giro * new Vector3(x, y, z);
            Vector3 D(Vector3 v) => giro * v;
            float banqueta = piso + ColocarS2cCalle.AltoBanqueta;
            float alto = ColocarS2cCalle.AltoBanqueta + 0.1f;
            if (conBanqueta)
            {
                KitS2c.Losa(o, raiz, "Banqueta_X", P(6f, banqueta - 0.125f, -Banqueta * 0.5f), giro, new Vector3(12f, alto, Banqueta), KitS2c.MatBanqueta(), 2f, true);
                KitS2c.Losa(o, raiz, "Banqueta_Z", P(-Banqueta * 0.5f, banqueta - 0.125f, 6f), giro, new Vector3(Banqueta, alto, 12f), KitS2c.MatBanqueta(), 2f, true);
                KitS2c.Losa(o, raiz, "Banqueta_Esquina", P(-Banqueta * 0.5f, banqueta - 0.125f, -Banqueta * 0.5f), giro, new Vector3(Banqueta, alto, Banqueta), KitS2c.MatBanqueta(), 2f, true);
            }
            Transform casas = new GameObject("Casas").transform;
            casas.SetParent(raiz, false);
            KitS2c.Estilo eFarmacia = new KitS2c.Estilo { PlantaBaja = KitS2c.PlantaVivienda, ColorPiso = 2, Neon = null, Foco = true, Tinaco = true };
            KitS2c.Estilo eTienda = new KitS2c.Estilo { PlantaBaja = KitS2c.PlantaLocal, ColorPiso = 1, Neon = null, Foco = true, Tinaco = false };
            KitS2c.Casa(o, casas, P(3f, banqueta, 0f), D(Vector3.back), 6f, 6f, farmacia ? eFarmacia : KitS2c.EstiloAlAzar(o));
            KitS2c.Casa(o, casas, P(0f, banqueta, 3f), D(Vector3.left), 6f, 6f, eTienda);
            if (farmacia)
            {
                LetreroFarmacia(o, raiz, P(3.6f, banqueta + 3.25f, -0.55f), D(Vector3.back));
            }
            Semaforo(o, raiz, P(-Banqueta + 0.35f, banqueta, -Banqueta + 0.35f), D(new Vector3(-1f, 0f, -1f)), giro);
            Vector3 punta = KitS2c.Poste(o, raiz, P(10.5f, banqueta, -Banqueta + 0.45f), D(Vector3.back), piso, true);
            KitS2c.Cable(o, punta, punta + D(new Vector3(-12f, -0.4f, 12f)), 1.1f);
            KitS2c.Cable(o, punta + Vector3.down * 0.2f, punta + D(new Vector3(-12.5f, -0.7f, 11f)), 1.2f);
            // Cruce peatonal de la calle Z, a la altura de la esquina (cada esquina pinta el de su calle Z: con las cuatro, cada brazo del
            // cruce lleva el suyo). Rayas de 0.5 m a lo largo de la calle, repartidas en todo el ancho del arroyo.
            int rayas = Mathf.FloorToInt((ancho - 0.6f) / 1.05f);
            for (int i = 0; i < rayas; i++)
            {
                float t = -Banqueta - 0.55f - i * 1.05f;
                KitS2c.Linea(raiz, P(t, piso, -1.6f), P(t, piso, 1.4f), 0.5f, piso);
            }
        }

        /// <summary>T-891: el letrero FA01 de K05 (FARMACIA a lo largo del muro y la cruz verde en la punta), con luz verde de noche.</summary>
        private static void LetreroFarmacia(KitS2c.Obra o, Transform padre, Vector3 p, Vector3 haciaCalle)
        {
            const string codigo = "FA01_Farmacia";
            const float escala = 0.7f;
            haciaCalle.y = 0f;
            haciaCalle.Normalize();
            GameObject raiz = new GameObject("LetreroFarmacia");
            raiz.transform.SetParent(padre, false);
            raiz.transform.position = p;
            KitS2c.Medida m = KitS2c.Medir(codigo);
            // El modelo trae las ménsulas hacia -X: su +X local va hacia la calle.
            Quaternion giro = Quaternion.LookRotation(Vector3.Cross(haciaCalle, Vector3.up));
            Vector3 muro = p - haciaCalle * 0.55f;
            Vector3 centro = muro + haciaCalle * (m.Caja.size.x * escala * 0.5f + 0.02f);
            KitS2c.Pieza(raiz.transform, codigo, centro - Vector3.up * (m.Caja.size.y * escala * 0.5f), giro, Vector3.one * escala, false).name = "Letrero";
            KitS2c.Luz(raiz.transform, centro + haciaCalle * 0.9f, LightType.Point, new Color(0.3f, 1f, 0.45f), 1.4f, 5f, 0f);
        }

        /// <summary>
        /// Semáforo: el ES01 (T-910) con la cara hacia el cruce y su ciclo verde, ámbar y rojo. Las esquinas de la calle cruzada (giro distinto en 90°)
        /// van a medio ciclo; las diagonales opuestas coinciden. Ver <see cref="ColocarT910.Semaforo"/>.
        /// </summary>
        private static void Semaforo(KitS2c.Obra o, Transform padre, Vector3 pie, Vector3 haciaCruce, Quaternion giroEsquina)
        {
            bool contrafase = Mathf.Abs(Mathf.RoundToInt(giroEsquina.eulerAngles.y / 90f)) % 2 != 0;
            ColocarT910.Semaforo(o, padre, pie, haciaCruce, contrafase);
        }

        /// <summary>Frente de Nova (K06): edificios altos con las torres del kit de exteriores, banqueta ancha con bolardos, cámaras y luz fría.</summary>
        public static void FrenteNova(KitS2c.Obra o, Transform padre, Vector3 inicio, Vector3 direccion, float largo, float piso)
        {
            Transform raiz = new GameObject("Frente_Nova").transform;
            raiz.SetParent(padre, false);
            direccion.Normalize();
            Vector3 normal = new Vector3(direccion.z, 0f, -direccion.x);
            Vector3 a = o.Raiz.position + inicio;
            float banqueta = piso + ColocarS2cCalle.AltoBanqueta;
            const float anchoBanqueta = 4f;
            KitS2c.Losa(o, raiz, "Banqueta", a + direccion * (largo * 0.5f) + normal * (anchoBanqueta * 0.5f) + Vector3.up * (banqueta - 0.125f), Quaternion.LookRotation(direccion),
                new Vector3(anchoBanqueta, ColocarS2cCalle.AltoBanqueta + 0.1f, largo), KitS2c.MatBanqueta(), 2f, true);
            float cubierto = NovaEdificios(o, raiz, a, direccion, normal, largo, banqueta);
            for (float x = 2f; x < largo - 1f; x += 3f)
            {
                KitS2c.Pieza(raiz, "BL01_Bolardo", a + direccion * x + normal * (anchoBanqueta - 0.35f) + Vector3.up * banqueta, Quaternion.LookRotation(direccion), new Vector3(EscalaBolardo, 1f, EscalaBolardo));
            }
            for (float x = 10f; x < largo - 3f; x += 20f)
            {
                KitS2c.Poste(o, raiz, a + direccion * x + normal * (anchoBanqueta - 0.8f) + Vector3.up * banqueta, normal, piso, false, KitS2c.Fria);
                if (x + 3.5f < cubierto)
                {
                    // T-888: solo donde hay torre detrás (con las torres juntas la hilera termina antes que la banqueta).
                    KitS2c.Pieza(raiz, "CY08_CamaraCorporativa", a + direccion * (x + 3f) + normal * 0.25f + Vector3.up * (banqueta + 3.6f), Quaternion.LookRotation(normal), Vector3.one, false);
                }
                if (x + 6f < largo - 1f)
                {
                    KitS2c.Pieza(raiz, "BT01_BoteBasura", a + direccion * (x + 6f) + normal * 0.8f + Vector3.up * banqueta, Quaternion.LookRotation(normal), Vector3.one);
                }
            }
        }

        public const float SeparacionTorres = 0.02f;
        private const float EscalaBolardo = 0.42f;
        private const string ModuloOficinas = "NV01_FachadaOficinas";
        private const string PlantaBajaNova = "NV02_PlantaBajaNova";
        private const float AnchoModulo = 6f;
        private const float FondoEdificioNova = 14f;
        private const int PisosConModulo = 5;
        private const int PisosMinimos = 7;
        private const int PisosMaximos = 12;

        /// <summary>
        /// T-891: edificios de oficinas de módulos NV01 sobre una planta baja con NV02 al centro. Arriba de <see cref="PisosConModulo"/> pisos sigue
        /// solo el cuerpo oscuro, que ya se pierde en la niebla. El cuerpo es el que choca. T-888: van juntos, sin rendijas.
        /// </summary>
        /// <returns>Hasta dónde llegan los edificios a lo largo del frente (m).</returns>
        private static float NovaEdificios(KitS2c.Obra o, Transform raiz, Vector3 a, Vector3 direccion, Vector3 normal, float largo, float banqueta)
        {
            KitS2c.Medida mo = KitS2c.Medir(ModuloOficinas);
            KitS2c.Medida mb = KitS2c.Medir(PlantaBajaNova);
            Quaternion giro = Quaternion.LookRotation(-normal);
            Material cuerpo = KitS2c.MatNovaCuerpo();
            float x = 0f;
            int n = 0;
            while (largo - x >= AnchoModulo)
            {
                int modulos = Mathf.Min(2 + o.Azar.Next(3), Mathf.FloorToInt((largo - x) / AnchoModulo));
                int pisos = o.Azar.Next(PisosMinimos, PisosMaximos + 1);
                float ancho = modulos * AnchoModulo;
                float alto = mb.Caja.size.y + (pisos - 1) * mo.Caja.size.y;
                Transform edificio = new GameObject("Edificio_" + n).transform;
                edificio.SetParent(raiz, false);
                for (int m = 0; m < modulos; m++)
                {
                    Vector3 pie = a + direccion * (x + (m + 0.5f) * AnchoModulo) + Vector3.up * banqueta;
                    bool entrada = m == modulos / 2;
                    KitS2c.Medida mPlanta = entrada ? mb : mo;
                    // Un NV01 de planta baja se estira a la altura del NV02 para que el piso de arriba empiece parejo.
                    KitS2c.Pieza(edificio, entrada ? PlantaBajaNova : ModuloOficinas, pie + normal * mPlanta.Muro, giro,
                        new Vector3(1f, mb.Caja.size.y / mPlanta.Caja.size.y, 1f), false);
                    for (int p = 1; p < Mathf.Min(pisos, PisosConModulo); p++)
                    {
                        KitS2c.Pieza(edificio, ModuloOficinas, pie + normal * mo.Muro + Vector3.up * (mb.Caja.size.y + (p - 1) * mo.Caja.size.y), giro, Vector3.one, false);
                    }
                }
                Vector3 centro = a + direccion * (x + ancho * 0.5f) - normal * (FondoEdificioNova * 0.5f + 0.02f) + Vector3.up * (banqueta + alto * 0.5f);
                KitS2c.Losa(o, edificio, "Cuerpo", centro, giro, new Vector3(ancho - SeparacionTorres, alto, FondoEdificioNova - 0.04f), cuerpo, 3f, true);
                x += ancho + SeparacionTorres;
                n++;
            }
            return x;
        }

        /// <summary>
        /// Muestrario para revisar el kit (no se guarda en la escena): en el taller, dos calles de 7.5 m que se cruzan con las 4 esquinas de K05,
        /// los frentes A–F en los brazos y una calle de Nova aparte. Fotos lado a lado con K05 y K06 en docs/capturas/s2c-kit-*.png.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/S2c/2b Fotos del kit de manzanas")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar("S2cKit_Muestrario", 8990);
            o.Raiz.position = Taller;
            const float medio = 5.75f;
            const float arroyo = 7.5f;
            const float largo = 40f;
            // Arroyos que se cruzan (el cruce en el origen del taller).
            KitS2c.Losa(o, o.Raiz, "ArroyoX", Taller + new Vector3(0f, -0.05f, 0f), Quaternion.identity, new Vector3(2f * largo, 0.1f, arroyo), KitS2c.MatAsfalto(), 8f, true);
            KitS2c.Losa(o, o.Raiz, "ArroyoZ", Taller + new Vector3(0f, -0.04f, 0f), Quaternion.identity, new Vector3(arroyo, 0.1f, 2f * largo), KitS2c.MatAsfalto(), 8f, true);
            for (int k = 0; k < 4; k++)
            {
                Quaternion g = Quaternion.Euler(0f, 90f * k, 0f);
                Esquina(o, o.Raiz, g * new Vector3(medio, 0f, medio), 0f, g, arroyo, k == 0);
                // Brazo: un frente de cada lado de la calle, de la esquina hacia afuera.
                float tramo = largo - 12f - medio;
                (List<Vector3> altos, List<Vector3> puntas) = FrenteCuadra(o, o.Raiz, g * new Vector3(medio + 12f, 0f, medio), g * Vector3.right, tramo, Frentes[k], 0f, true);
                (List<Vector3> altos2, List<Vector3> _) = FrenteCuadra(o, o.Raiz, g * new Vector3(largo, 0f, -medio), g * Vector3.left, tramo, Frentes[(k + 3) % Frentes.Length + 0], 0f, false);
                foreach (Vector3 p in puntas)
                {
                    foreach (Vector3 c in altos2.OrderBy(c => (c - p).sqrMagnitude).Take(2))
                    {
                        KitS2c.Cable(o, p + Vector3.down * 0.1f, c, o.Rango(0.8f, 1.3f));
                    }
                }
            }
            // Calle de Nova: 18 m de fachada a fachada (banquetas de 4 m y arroyo de 10 m).
            FrenteNova(o, o.Raiz, new Vector3(-30f, 0f, 120f), Vector3.right, LargoFrente, 0f);
            KitS2c.Losa(o, o.Raiz, "ArroyoNova", Taller + new Vector3(0f, -0.05f, 111f), Quaternion.identity, new Vector3(LargoFrente, 0.1f, 18f), KitS2c.MatAsfalto(), 8f, true);
            FrenteNova(o, o.Raiz, new Vector3(30f, 0f, 102f), Vector3.left, LargoFrente, 0f);
            KitS2c.Terminar(o);
            // K05: desde la banqueta de una esquina, mirando el cruce en diagonal hacia la siguiente cuadra.
            ColocarS2cLook.Foto(Taller + new Vector3(-1.8f, 2.7f, -15.5f), Taller + new Vector3(2.5f, 0.6f, 14f), "docs/capturas/s2c-kit-esquina-juego.png", 70f);
            ColocarS2.LadoALado("imageassets/referencias/look/K05_EsquinaBarrio.png", "docs/capturas/s2c-kit-esquina-juego.png", "docs/capturas/s2c-kit-esquina.png");
            ColocarS2cLook.Foto(Taller + new Vector3(-28f, 2.6f, 110.6f), Taller + new Vector3(30f, 4f, 111f), "docs/capturas/s2c-kit-nova-juego.png", 66f);
            ColocarS2.LadoALado("imageassets/referencias/look/K06_CalleNova.png", "docs/capturas/s2c-kit-nova-juego.png", "docs/capturas/s2c-kit-nova.png");
            ColocarS2cLook.Foto(Taller + new Vector3(-30f, 45f, -55f), Taller, "docs/capturas/s2c-kit-alto.png", 60f);
            for (int k = 0; k < 4; k++)
            {
                Quaternion g = Quaternion.Euler(0f, 90f * k, 0f);
                ColocarS2cLook.Foto(Taller + g * new Vector3(largo, 2.4f, -1.2f), Taller + g * new Vector3(medio, 2f, 1f), "docs/capturas/s2c-kit-frente-" + Frentes[k] + ".png", 62f);
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColocarS2.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
            AssetDatabase.DeleteAsset(KitS2c.CarpetaMallas + "/S2cKit_Muestrario.asset");
            Debug.Log("[Arte] Fotos del kit S2c listas");
        }
    }
}
