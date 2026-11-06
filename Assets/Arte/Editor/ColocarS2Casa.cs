using System.Linq;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 zona 4 (docs/playtest-05nov.md): la casa del jugador. Fachada rehecha con el kit modular (planta baja C02/C03, pisos C20/C21, franja C23,
    /// cornisa C22, pilares C04, pretil C05, tinaco, antena, aires y cables) sobre los cubos viejos (que se quedan solo como colisión); el tablero de
    /// encargos deja de ser un cubo blanco; el cuarto se acomoda según docs/referencias-interiores/habitacion.png (ropero a la derecha, foco pelón con
    /// cables, mochila a su tamaño, más pósters, caja eléctrica y una puerta de verdad) y las puertas de cuarto usan su fundido corto.
    /// Idempotente (rehace S2_Casa y vuelve a acomodar el cuarto). Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Casa.Colocar
    /// </summary>
    public static class ColocarS2Casa
    {
        public const string NombreRaiz = "S2_Casa";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const string L = "Assets/Arte/Callejon/Prefabs/";
        private const string I = "Assets/Arte/Interiores/Prefabs/";
        private static readonly Color Calido = new Color(1f, 0.72f, 0.42f);

        // Frente de la casa: plano x = -57 mirando a +X; de z -83.7 a -70.3; azotea a 7.2 m; puerta entre z -78.5 y -75.5 bajo el dintel (3.4 m).
        private const float FrenteX = -56.95f;
        private const float Z0 = -83.7f, Z1 = -70.3f, PuertaZ0 = -78.5f, PuertaZ1 = -75.5f, Suelo = 0.22f, PlantaBaja = 3.4f, Azotea = 7.2f;

        [MenuItem("Mecha Cyberpunk/S2/4 Casa (fachada, cuarto, puerta y fundido)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Fachada(raiz);
            Tablero(raiz);
            Cuarto();
            Fundidos();
            ColocarT550.AislarDelSol();
            ColocarS2.Guardar("s2-casa");
        }

        // ---------------------------------------------------------------- Fachada

        private static void Fachada(Transform raiz)
        {
            Transform casa = ColocarS2.Buscar("Barrio/Manzana_4/Lugar_Casa");
            foreach (string n in new[] { "FachadaIzquierda", "FachadaDerecha", "Dintel" })
            {
                Renderer rend = casa.Find(n)?.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.enabled = false; // el cubo se queda como colisión; lo que se ve es el kit
                }
            }
            foreach (string n in new[] { "H01b_ParedVentana_Visual_T250", "H01c_ParedLisa_Visual_T250" })
            {
                Transform t = casa.Find(n);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }
            Transform f = Grupo(raiz, "Fachada");
            Quaternion haciaLaCalle = Quaternion.Euler(0f, 270f, 0f); // el kit mira a -Z: girado 270° mira a +X
            // Planta baja: vivienda a la izquierda de la puerta y local con cortina a la derecha.
            Tramo(f, C + "C02_FachadaVivienda_cy", PuertaZ1, Z1, Suelo, PlantaBaja, haciaLaCalle);
            Tramo(f, C + "C03_FachadaPisoAlto_cy", Z0, PuertaZ0, Suelo, PlantaBaja, haciaLaCalle);
            // Franja entre planta baja y pisos, pisos con ventanas (alternando) y cornisa.
            Tramo(f, C + "C23_Franja", Z0, Z1, PlantaBaja - 0.1f, PlantaBaja + 0.45f, haciaLaCalle, 0.12f);
            string[] pisos = { "C20b_PisoDosVentanas_cy", "C20a_PisoVentana_cy", "C21_PisoLiso_cy", "C20b_PisoDosVentanas_cy" };
            float ancho = (Z1 - Z0) / pisos.Length;
            for (int i = 0; i < pisos.Length; i++)
            {
                Tramo(f, C + pisos[i], Z0 + ancho * i, Z0 + ancho * (i + 1), PlantaBaja + 0.45f, Azotea - 0.5f, haciaLaCalle);
            }
            Tramo(f, C + "C22_Cornisa_cy", Z0, Z1, Azotea - 0.55f, Azotea + 0.1f, haciaLaCalle, 0.1f);
            // Lo de la calle: pilares de esquina (ya estaban), lámpara de jaula sobre la puerta, medidores, cables y aires.
            Pieza(f, C + "N01", new Vector3(FrenteX + 0.25f, 0f, -77f), 270f, PlantaBaja - 0.2f, 0.9f);
            Pieza(f, C + "C14_RegistroMedidores", new Vector3(FrenteX + 0.25f, 0f, -79.4f), 270f, 1.0f, 0.8f);
            Pieza(f, C + "CY03_MazoCables", new Vector3(FrenteX + 0.2f, 0f, -74.6f), 270f, 0.8f, 1f);
            Pieza(f, C + "C10_AireAcondicionado", new Vector3(FrenteX + 0.35f, 0f, -72.4f), 270f, 4.4f, 1f);
            Pieza(f, C + "C10_AireAcondicionado", new Vector3(FrenteX + 0.35f, 0f, -81.6f), 270f, 5.0f, 1f);
            // Azotea: tinaco, antena (los pretiles C05 de T-250 ya están en la orilla).
            Pieza(f, C + "C09_Tinaco", new Vector3(-61.5f, 0f, -73.2f), 0f, Azotea, 1f);
            Pieza(f, C + "C09_Tinaco", new Vector3(-61.5f, 0f, -75.0f), 0f, Azotea, 1f);
            Pieza(f, C + "CY04_AntenaAzotea", new Vector3(-60.5f, 0f, -82.3f), 30f, Azotea, 1f);
            Transform luces = Grupo(f, "Luces");
            Light l = new GameObject("Luz_Puerta").AddComponent<Light>();
            l.transform.SetParent(luces, false);
            l.transform.position = new Vector3(FrenteX + 0.8f, PlantaBaja - 0.5f, -77f);
            l.type = LightType.Point;
            l.color = Calido;
            l.intensity = 3f;
            l.range = 5f;
            ColocarS2.Informe.AppendLine("Fachada de la casa: " + f.childCount + " piezas del kit");
        }

        /// <summary>Estira una pieza del kit para cubrir el tramo [za, zb] x [ya, yb] del frente, pegada al plano de la fachada.</summary>
        private static GameObject Tramo(Transform padre, string ruta, float za, float zb, float ya, float yb, Quaternion giro, float fondo = 0.35f)
        {
            GameObject g = Instanciar(padre, ruta);
            if (g == null)
            {
                return null;
            }
            g.transform.rotation = giro;
            Bounds b = ArteT250.Limites(g);
            Vector3 s = g.transform.localScale;
            // Con el giro, el ancho del modelo queda en Z del mundo y su fondo en X.
            g.transform.localScale = new Vector3(s.x * (zb - za) / Mathf.Max(0.01f, b.size.z), s.y * (yb - ya) / Mathf.Max(0.01f, b.size.y), s.z * fondo / Mathf.Max(0.01f, b.size.x));
            b = ArteT250.Limites(g);
            g.transform.position += new Vector3(FrenteX - b.min.x, ya - b.min.y, (za + zb) * 0.5f - b.center.z);
            Estatico(g);
            return g;
        }

        private static GameObject Pieza(Transform padre, string ruta, Vector3 centro, float yaw, float y, float escala)
        {
            GameObject g = ColocarS2Interiores.Modelo(padre, ruta, centro, yaw, y, escala, sinCollider: true);
            if (g != null)
            {
                Estatico(g);
            }
            return g;
        }

        // ---------------------------------------------------------------- Tablero de encargos

        private static void Tablero(Transform raiz)
        {
            Transform t = ColocarS2.Buscar("GTA_T426/TableroEncargos");
            if (t == null)
            {
                return;
            }
            Renderer rend = t.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = false; // el cubo blanco: su collider y el TableroEncargos se quedan
            }
            GameObject kiosco = ColocarS2Interiores.Modelo(Grupo(raiz, "Tablero"), C + "CY10_CargadorCalle", new Vector3(t.position.x, 0f, t.position.z), t.eulerAngles.y + 180f, Suelo, 1f, sinCollider: true);
            if (kiosco != null)
            {
                kiosco.name = "Tablero_CY10";
            }
            ColocarS2.Informe.AppendLine("Tablero de encargos: kiosco CY10 en lugar del cubo blanco");
        }

        // ---------------------------------------------------------------- Cuarto (habitación)

        private static void Cuarto()
        {
            Transform r = ColocarS2.Buscar(ColocarT550.NombreRaiz + "/Cuarto_Habitacion");
            if (r == null)
            {
                return;
            }
            foreach (string viejo in new[] { "S2_Cuarto", "FocoColgante" })
            {
                Transform t = r.Find(viejo);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            Transform s2 = Grupo(r, "S2_Cuarto");
            Vector3 P(float x, float y, float z) => r.TransformPoint(new Vector3(x, y, z));
            float piso = r.position.y;

            // Ropero a la derecha (como en la referencia), contra la pared derecha entre la puerta y el escritorio.
            Transform ropero = r.Find("I01");
            if (ropero != null)
            {
                ropero.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up); // el frente (-Z del modelo) hacia -X: hacia dentro
                Bounds b = ArteT250.Limites(ropero.gameObject);
                ropero.position += new Vector3(P(1.5f, 0f, 0f).x - b.max.x - 0.02f, piso - b.min.y, P(0f, 0f, -0.35f).z - b.center.z);
            }
            // Con el ropero a la derecha, el jugador aparece frente a la puerta, del lado izquierdo, mirando hacia dentro.
            Transform aparicion = r.Find("Aparicion");
            if (aparicion != null)
            {
                aparicion.localPosition = new Vector3(0.3f, 0.05f, -1.15f);
                aparicion.localRotation = Quaternion.identity;
            }
            // Mochila: a su tamaño (unos 50 cm de alto), parada junto al escritorio.
            Transform mochila = r.Find("I05");
            if (mochila != null)
            {
                mochila.localScale = Vector3.one;
                // El modelo viene acostado (su alto queda en Z): se para.
                mochila.rotation = Quaternion.Euler(0f, 200f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                Bounds b = ArteT250.Limites(mochila.gameObject);
                float factor = 0.5f / Mathf.Max(b.size.y, 0.01f);
                mochila.localScale = Vector3.one * factor;
                b = ArteT250.Limites(mochila.gameObject);
                Vector3 c = P(0.5f, 0f, 0.3f);
                mochila.position += new Vector3(c.x - b.center.x, piso - b.min.y, c.z - b.center.z);
            }
            // Ventana más grande y centrada en la pared del fondo, con su barra de cortina.
            Transform ventana = r.Find("I04");
            if (ventana != null)
            {
                Bounds b0 = ArteT250.Limites(ventana.gameObject);
                ventana.localScale *= 1.35f / Mathf.Max(b0.size.x / 1.43f, 0.01f) * 1f;
                Bounds b = ArteT250.Limites(ventana.gameObject);
                Vector3 c = P(0.15f, 1.65f, 2f);
                ventana.position += new Vector3(c.x - b.center.x, c.y - b.center.y, c.z - b.max.z + 0.02f);
            }
            Barra(s2, P(-0.85f, 2.42f, 1.92f), P(1.15f, 2.42f, 1.92f));

            // Foco pelón colgando de su cable, con cables que cruzan el techo hacia las esquinas.
            Vector3 techo = P(0.05f, 2.7f, 0.25f);
            Vector3 foco = techo + Vector3.down * 0.55f;
            Cable(s2, techo, foco, 0f, 0.008f);
            Cable(s2, techo, P(-1.48f, 2.62f, -1.2f), 0.18f, 0.012f);
            Cable(s2, techo, P(1.48f, 2.6f, 1.6f), 0.15f, 0.012f);
            Cable(s2, techo, P(-1.4f, 2.66f, 1.95f), 0.12f, 0.012f);
            GameObject bulbo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(bulbo.GetComponent<Collider>());
            bulbo.name = "Foco_Pelon";
            bulbo.transform.SetParent(s2, false);
            bulbo.transform.position = foco + Vector3.down * 0.05f;
            bulbo.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);
            Renderer rb = bulbo.GetComponent<Renderer>();
            rb.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Interiores/Materiales/T546_Foco.mat");
            rb.shadowCastingMode = ShadowCastingMode.Off;
            Light luzFoco = r.Find("Luces/FocoColgante_Luz")?.GetComponent<Light>();
            if (luzFoco != null)
            {
                luzFoco.transform.position = foco + Vector3.down * 0.15f;
                luzFoco.intensity = 1.6f;
                luzFoco.range = 6.5f;
            }

            // Más pósters en la pared de la cama y uno grande junto a la ventana; caja eléctrica sobre el escritorio.
            Poster(r, s2, "Poster_2", new Vector3(-1.48f, 1.55f, -0.15f), Vector3.right, new Vector2(0.5f, 0.7f), 0f);
            Poster(r, s2, "Poster_3", new Vector3(-1.48f, 1.95f, 0.35f), Vector3.right, new Vector2(0.42f, 0.6f), 4f);
            Poster(r, s2, "Poster_1", new Vector3(-1.48f, 1.25f, 0.5f), Vector3.right, new Vector2(0.36f, 0.5f), -5f);
            Poster(r, s2, "Poster_2", new Vector3(-0.95f, 1.6f, 1.99f), Vector3.back, new Vector2(0.7f, 0.95f), 0f);
            ColocarS2Interiores.Modelo(s2, C + "C14_RegistroMedidores", P(1.42f, 0f, 1.55f), 270f, piso + 1.55f, 0.45f, sinCollider: true);
            ColocarS2Interiores.Modelo(s2, C + "N06", P(-0.1f, 0f, -0.6f), 30f, piso, 0.35f, sinCollider: true);   // basura y latas en el piso

            // Puerta del cuarto: una de verdad (L02) en lugar de la hoja de cubos.
            Transform vieja = r.Find("PuertaInterior");
            if (vieja != null)
            {
                foreach (Renderer rend in vieja.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = false;
                }
                GameObject puerta = ColocarS2Interiores.Modelo(s2, L + "L02_PuertaTrasera", vieja.position + vieja.forward * 0.05f, 0f, piso, 1f, sinCollider: true);
                if (puerta != null)
                {
                    puerta.name = "Puerta_L02";
                    Bounds b = ArteT250.Limites(puerta);
                    Vector3 e = puerta.transform.localScale;
                    puerta.transform.localScale = new Vector3(e.x * 1.05f / Mathf.Max(b.size.x, 0.01f), e.y * 2.15f / Mathf.Max(b.size.y, 0.01f), e.z * 0.12f / Mathf.Max(b.size.z, 0.01f));
                    b = ArteT250.Limites(puerta);
                    puerta.transform.position += new Vector3(vieja.position.x - b.center.x, piso - b.min.y, P(0f, 0f, -2f).z + 0.03f - b.min.z);
                }
            }
            ColocarS2.Informe.AppendLine("Cuarto: ropero a la derecha, mochila " + (mochila != null ? ArteT250.Limites(mochila.gameObject).size.ToString("F2") : "-") + ", foco pelón con cables, puerta L02");
        }

        private static void Poster(Transform r, Transform padre, string textura, Vector3 posLocal, Vector3 frente, Vector2 tam, float giro)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Poster_S2";
            q.transform.SetParent(padre, false);
            q.transform.SetPositionAndRotation(r.TransformPoint(posLocal), Quaternion.LookRotation(-frente, Vector3.up) * Quaternion.Euler(0f, 0f, giro));
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            Renderer rend = q.GetComponent<Renderer>();
            rend.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Interiores/Materiales/T546_Poster_T546_" + textura + ".mat");
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Cable(Transform padre, Vector3 a, Vector3 b, float comba, float grosor)
        {
            GameObject go = new GameObject("Cable");
            go.transform.SetParent(padre, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            const int N = 12;
            lr.positionCount = N + 1;
            for (int i = 0; i <= N; i++)
            {
                float t = (float)i / N;
                lr.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * comba);
            }
            lr.widthMultiplier = grosor;
            lr.sharedMaterial = ColocarT550.Mat("S2_Cable", new Color(0.04f, 0.04f, 0.045f));
            lr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Barra(Transform padre, Vector3 a, Vector3 b)
        {
            GameObject cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(cil.GetComponent<Collider>());
            cil.name = "Barra_Cortina";
            cil.transform.SetParent(padre, false);
            cil.transform.position = (a + b) * 0.5f;
            cil.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            cil.transform.localScale = new Vector3(0.025f, Vector3.Distance(a, b) * 0.5f, 0.025f);
            cil.GetComponent<Renderer>().sharedMaterial = ColocarT550.Mat("T550_Tuberia", new Color(0.12f, 0.12f, 0.13f));
        }

        // ---------------------------------------------------------------- Fundidos

        private static void Fundidos()
        {
            foreach (PuertaCuarto p in Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject so = new SerializedObject(p);
                so.FindProperty("segundosEnNegro").floatValue = 0.08f;
                so.FindProperty("segundosAlNegro").floatValue = 0.2f;
                so.FindProperty("segundosDesdeNegro").floatValue = 0.3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------------------------------------------------------- Ayudas

        private static GameObject Instanciar(Transform padre, string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta.EndsWith(".prefab") ? ruta : ruta + ".prefab");
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine("  FALTA " + ruta);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = prefab.name;
            foreach (Collider col in g.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }
            return g;
        }

        private static void Estatico(GameObject g)
        {
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }
    }
}
