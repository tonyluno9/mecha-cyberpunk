using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-400: crea el muñeco humanoide low-poly de cubos (1.8 m) en Assets/Arte/Personajes/MunecoHumano/ con su AnimatorController
    /// (parámetros Velocidad, EnPiso, Apuntando, Arma, Golpe, Disparo, Recarga, KO), los clips por código y los prefabs
    /// ModeloHumano (pandilleros y aliados) y ModeloJugador (variante del jugador). El modelo final se cambia en esos prefabs
    /// sin tocar código: el código solo habla con el Animator por esos nombres.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorMunecoHumano.Crear
    /// </summary>
    public static class CreadorMunecoHumano
    {
        public const string Carpeta = "Assets/Arte/Personajes/MunecoHumano";
        public const string RutaModeloHumano = Carpeta + "/ModeloHumano.prefab";
        public const string RutaModeloJugador = Carpeta + "/ModeloJugador.prefab";
        private const string RutaControlador = Carpeta + "/MunecoHumano.controller";
        private const string RutaMascara = Carpeta + "/MascaraBrazos.mask";

        private const string RutaCadera = "Cadera";
        private const string RutaTorso = "Cadera/Torso";
        private const string BrazoSupDer = "Cadera/Torso/BrazoSupDer";
        private const string BrazoInfDer = "Cadera/Torso/BrazoSupDer/BrazoInfDer";
        private const string ManoDer = "Cadera/Torso/BrazoSupDer/BrazoInfDer/ManoDer";
        private const string BrazoSupIzq = "Cadera/Torso/BrazoSupIzq";
        private const string BrazoInfIzq = "Cadera/Torso/BrazoSupIzq/BrazoInfIzq";
        private const string PiernaSupDer = "Cadera/PiernaSupDer";
        private const string PiernaInfDer = "Cadera/PiernaSupDer/PiernaInfDer";
        private const string PiernaSupIzq = "Cadera/PiernaSupIzq";
        private const string PiernaInfIzq = "Cadera/PiernaSupIzq/PiernaInfIzq";

        private struct Curva
        {
            public string Ruta;
            public string Propiedad;
            public AnimationCurve Valores;
        }

        [MenuItem("Mecha Cyberpunk/Crear muñeco humano (T-400)")]
        public static void Crear()
        {
            Directory.CreateDirectory(Carpeta + "/Clips");
            Directory.CreateDirectory(Carpeta + "/Materiales");
            AssetDatabase.Refresh();

            Material piel = CrearMaterial(Carpeta + "/Materiales/Muneco_Piel.mat", new Color(0.85f, 0.65f, 0.5f));
            Material ropa = CrearMaterial(Carpeta + "/Materiales/Muneco_Ropa.mat", new Color(0.35f, 0.38f, 0.45f));
            Material ropaJugador = CrearMaterial(Carpeta + "/Materiales/Muneco_RopaJugador.mat", new Color(0.1f, 0.65f, 0.7f));

            AnimatorController controlador = CrearControlador();

            GameObject modelo = ArmarCuerpo("ModeloHumano", piel, ropa);
            modelo.AddComponent<Animator>().runtimeAnimatorController = controlador;
            GameObject prefabHumano = PrefabUtility.SaveAsPrefabAsset(modelo, RutaModeloHumano);
            Object.DestroyImmediate(modelo);

            // ModeloJugador es una variante de ModeloHumano con otra ropa.
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabHumano);
            instancia.name = "ModeloJugador";
            foreach (string ruta in new[] { RutaTorso, BrazoSupDer, BrazoSupIzq, PiernaSupDer, PiernaSupIzq })
            {
                instancia.transform.Find(ruta + "/Malla").GetComponent<MeshRenderer>().sharedMaterial = ropaJugador;
            }
            PrefabUtility.SaveAsPrefabAsset(instancia, RutaModeloJugador);
            Object.DestroyImmediate(instancia);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] Muñeco humano creado en " + Carpeta);
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        // ---- Cuerpo ----

        private static GameObject ArmarCuerpo(string nombre, Material piel, Material ropa)
        {
            GameObject raiz = new GameObject(nombre);
            Transform cadera = Pivote("Cadera", raiz.transform, new Vector3(0f, 0.95f, 0f));
            Transform torso = Pivote("Torso", cadera, Vector3.zero);
            Malla(torso, new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.22f), ropa);
            Transform cabeza = Pivote("Cabeza", torso, new Vector3(0f, 0.55f, 0f));
            Malla(cabeza, new Vector3(0f, 0.14f, 0f), new Vector3(0.26f, 0.26f, 0.26f), piel);

            foreach (int lado in new[] { -1, 1 })
            {
                string sufijo = lado > 0 ? "Der" : "Izq";
                Transform brazoSup = Pivote("BrazoSup" + sufijo, torso, new Vector3(0.28f * lado, 0.47f, 0f));
                Malla(brazoSup, new Vector3(0f, -0.15f, 0f), new Vector3(0.12f, 0.3f, 0.12f), ropa);
                Transform brazoInf = Pivote("BrazoInf" + sufijo, brazoSup, new Vector3(0f, -0.3f, 0f));
                Malla(brazoInf, new Vector3(0f, -0.15f, 0f), new Vector3(0.1f, 0.3f, 0.1f), piel);
                if (lado > 0)
                {
                    Pivote("ManoDer", brazoInf, new Vector3(0f, -0.3f, 0.02f));
                }

                Transform piernaSup = Pivote("PiernaSup" + sufijo, cadera, new Vector3(0.1f * lado, 0f, 0f));
                Malla(piernaSup, new Vector3(0f, -0.235f, 0f), new Vector3(0.16f, 0.47f, 0.16f), ropa);
                Transform piernaInf = Pivote("PiernaInf" + sufijo, piernaSup, new Vector3(0f, -0.47f, 0f));
                Malla(piernaInf, new Vector3(0f, -0.24f, 0f), new Vector3(0.14f, 0.48f, 0.14f), ropa);
            }
            return raiz;
        }

        private static Transform Pivote(string nombre, Transform padre, Vector3 posicion)
        {
            GameObject pivote = new GameObject(nombre);
            pivote.transform.SetParent(padre, false);
            pivote.transform.localPosition = posicion;
            return pivote.transform;
        }

        private static void Malla(Transform pivote, Vector3 centro, Vector3 tamano, Material material)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Malla";
            Object.DestroyImmediate(cubo.GetComponent<Collider>());
            cubo.transform.SetParent(pivote, false);
            cubo.transform.localPosition = centro;
            cubo.transform.localScale = tamano;
            cubo.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // ---- Animator ----

        private static AnimatorController CrearControlador()
        {
            AssetDatabase.DeleteAsset(RutaControlador);
            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            c.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            c.AddParameter("EnPiso", AnimatorControllerParameterType.Bool);
            c.AddParameter("Apuntando", AnimatorControllerParameterType.Bool);
            c.AddParameter("Arma", AnimatorControllerParameterType.Int);
            c.AddParameter("Golpe", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Disparo", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Recarga", AnimatorControllerParameterType.Trigger);
            c.AddParameter("KO", AnimatorControllerParameterType.Bool);
            // EnPiso arranca en true para que el muñeco no empiece cayendo.
            AnimatorControllerParameter[] ps = c.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name == "EnPiso")
                {
                    ps[i].defaultBool = true;
                }
            }
            c.parameters = ps;

            AnimationClip quieto = Clip("Quieto", 2f, true, new[]
            {
                Y(RutaCadera, V(0f, 0.95f), V(1f, 0.94f), V(2f, 0.95f)),
            });
            AnimationClip caminar = Clip("Caminar", 1f, true, Caminata(1f, 28f, 28f, 25f, 15f, 0f));
            AnimationClip correr = Clip("Correr", 0.6f, true, Caminata(0.6f, 55f, 80f, 55f, 70f, 10f));
            AnimationClip saltar = Clip("Saltar", 0.9f, false, new[]
            {
                X(PiernaSupDer, V(0f, 0f), V(0.3f, -45f), V(0.9f, -35f)),
                X(PiernaInfDer, V(0f, 0f), V(0.3f, 70f), V(0.9f, 40f)),
                X(PiernaSupIzq, V(0f, 0f), V(0.3f, 20f), V(0.9f, 25f)),
                X(PiernaInfIzq, V(0f, 0f), V(0.3f, 30f), V(0.9f, 20f)),
                X(BrazoSupDer, V(0f, 0f), V(0.3f, -140f), V(0.9f, -100f)),
                X(BrazoSupIzq, V(0f, 0f), V(0.3f, -140f), V(0.9f, -100f)),
            });
            AnimationClip ko = Clip("KO", 0.8f, false, new[]
            {
                X(RutaCadera, V(0f, 0f), V(0.5f, 70f), V(0.8f, 85f)),
                Y(RutaCadera, V(0f, 0.95f), V(0.5f, 0.35f), V(0.8f, 0.2f)),
                X(BrazoSupDer, V(0f, 0f), V(0.5f, -150f), V(0.8f, -160f)),
                X(BrazoSupIzq, V(0f, 0f), V(0.5f, -150f), V(0.8f, -160f)),
            });
            AnimationClip golpe = Clip("Golpe", 0.45f, false, new[]
            {
                X(BrazoSupDer, V(0f, -20f), V(0.15f, -100f), V(0.45f, -10f)),
                X(BrazoInfDer, V(0f, -100f), V(0.15f, 0f), V(0.45f, -60f)),
                X(BrazoSupIzq, V(0f, -40f), V(0.15f, -50f), V(0.45f, -30f)),
            });
            AnimationClip apuntar = Clip("Apuntar", 0.3f, true, new[]
            {
                X(BrazoSupDer, V(0f, -88f), V(0.3f, -88f)),
                X(BrazoInfDer, V(0f, -5f), V(0.3f, -5f)),
                X(BrazoSupIzq, V(0f, -70f), V(0.3f, -70f)),
                X(BrazoInfIzq, V(0f, -40f), V(0.3f, -40f)),
            });
            AnimationClip disparo = Clip("Disparo", 0.2f, false, new[]
            {
                X(BrazoSupDer, V(0f, -88f), V(0.05f, -105f), V(0.2f, -88f)),
                X(BrazoInfDer, V(0f, -5f), V(0.2f, -5f)),
                X(BrazoSupIzq, V(0f, -70f), V(0.05f, -75f), V(0.2f, -70f)),
            });
            AnimationClip recarga = Clip("Recarga", 1.2f, false, new[]
            {
                X(BrazoSupDer, V(0f, -60f), V(1.2f, -60f)),
                X(BrazoInfDer, V(0f, -80f), V(1.2f, -80f)),
                X(BrazoSupIzq, V(0f, -50f), V(1.2f, -50f)),
                X(BrazoInfIzq, V(0f, -90f), V(0.6f, -110f), V(1.2f, -90f)),
            });

            AnimatorStateMachine basico = c.layers[0].stateMachine;
            AnimatorState locomocion = c.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
            arbol.blendType = BlendTreeType.Simple1D;
            arbol.blendParameter = "Velocidad";
            arbol.useAutomaticThresholds = false;
            arbol.AddChild(quieto, 0f);
            arbol.AddChild(caminar, 4.5f);
            arbol.AddChild(correr, 7.5f);
            AnimatorState estadoSaltar = basico.AddState("Saltar");
            estadoSaltar.motion = saltar;
            AnimatorState estadoKO = basico.AddState("KO");
            estadoKO.motion = ko;
            basico.defaultState = locomocion;

            Transicion(locomocion, estadoSaltar, 0.1f, "EnPiso", false);
            Transicion(estadoSaltar, locomocion, 0.1f, "EnPiso", true);
            AnimatorStateTransition aKO = basico.AddAnyStateTransition(estadoKO);
            aKO.hasExitTime = false;
            aKO.duration = 0.1f;
            aKO.canTransitionToSelf = false;
            aKO.AddCondition(AnimatorConditionMode.If, 0f, "KO");
            Transicion(estadoKO, locomocion, 0.1f, "KO", false);

            // Capa de brazos: encima de la locomoción, solo mueve los brazos.
            AnimatorControllerLayer capa = new AnimatorControllerLayer
            {
                name = "Brazos",
                defaultWeight = 1f,
                avatarMask = CrearMascara(),
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine { name = "Brazos", hideFlags = HideFlags.HideInHierarchy },
            };
            AssetDatabase.AddObjectToAsset(capa.stateMachine, c);
            c.AddLayer(capa);
            AnimatorStateMachine brazos = capa.stateMachine;
            AnimatorState libre = brazos.AddState("Libre");
            libre.writeDefaultValues = false;
            AnimatorState sGolpe = EstadoBrazos(brazos, "Golpe", golpe);
            AnimatorState sApuntar = EstadoBrazos(brazos, "Apuntar", apuntar);
            AnimatorState sDisparo = EstadoBrazos(brazos, "Disparo", disparo);
            AnimatorState sRecarga = EstadoBrazos(brazos, "Recarga", recarga);
            brazos.defaultState = libre;

            Transicion(libre, sApuntar, 0.08f, "Apuntando", true);
            Transicion(sApuntar, libre, 0.1f, "Apuntando", false);
            ConfigurarDisparador(brazos.AddAnyStateTransition(sGolpe), "Golpe");
            ConfigurarDisparador(brazos.AddAnyStateTransition(sDisparo), "Disparo");
            ConfigurarDisparador(brazos.AddAnyStateTransition(sRecarga), "Recarga");
            // Al terminar vuelven a apuntar si se sigue apuntando, o quedan libres.
            foreach (AnimatorState estado in new[] { sGolpe, sDisparo, sRecarga })
            {
                AnimatorStateTransition aApuntar = estado.AddTransition(sApuntar);
                aApuntar.hasExitTime = true;
                aApuntar.exitTime = 1f;
                aApuntar.duration = 0.05f;
                aApuntar.AddCondition(AnimatorConditionMode.If, 0f, "Apuntando");
                AnimatorStateTransition aLibre = estado.AddTransition(libre);
                aLibre.hasExitTime = true;
                aLibre.exitTime = 1f;
                aLibre.duration = 0.05f;
                aLibre.AddCondition(AnimatorConditionMode.IfNot, 0f, "Apuntando");
            }
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        private static AnimatorState EstadoBrazos(AnimatorStateMachine maquina, string nombre, AnimationClip clip)
        {
            AnimatorState estado = maquina.AddState(nombre);
            estado.motion = clip;
            estado.writeDefaultValues = false;
            return estado;
        }

        private static void ConfigurarDisparador(AnimatorStateTransition transicion, string parametro)
        {
            transicion.hasExitTime = false;
            transicion.duration = 0.03f;
            transicion.canTransitionToSelf = true;
            transicion.AddCondition(AnimatorConditionMode.If, 0f, parametro);
        }

        private static void Transicion(AnimatorState desde, AnimatorState hacia, float duracion, string parametro, bool valor)
        {
            AnimatorStateTransition t = desde.AddTransition(hacia);
            t.hasExitTime = false;
            t.duration = duracion;
            t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro);
        }

        private static AvatarMask CrearMascara()
        {
            AssetDatabase.DeleteAsset(RutaMascara);
            string[] activos = { BrazoSupDer, BrazoSupIzq, BrazoInfDer, BrazoInfIzq };
            string[] todos =
            {
                RutaCadera, RutaTorso, RutaTorso + "/Cabeza", BrazoSupDer, BrazoInfDer, ManoDer,
                BrazoSupIzq, BrazoInfIzq, PiernaSupDer, PiernaInfDer, PiernaSupIzq, PiernaInfIzq,
            };
            AvatarMask mascara = new AvatarMask { transformCount = todos.Length };
            for (int i = 0; i < todos.Length; i++)
            {
                mascara.SetTransformPath(i, todos[i]);
                mascara.SetTransformActive(i, System.Array.IndexOf(activos, todos[i]) >= 0);
            }
            AssetDatabase.CreateAsset(mascara, RutaMascara);
            return mascara;
        }

        // ---- Clips ----

        private static Keyframe V(float tiempo, float valor) => new Keyframe(tiempo, valor);

        private static Curva X(string ruta, params Keyframe[] llaves) => Nueva(ruta, "localEulerAnglesRaw.x", llaves);

        private static Curva Y(string ruta, params Keyframe[] llaves) => Nueva(ruta, "m_LocalPosition.y", llaves);

        private static Curva Nueva(string ruta, string propiedad, Keyframe[] llaves)
        {
            return new Curva { Ruta = ruta, Propiedad = propiedad, Valores = new AnimationCurve(llaves) };
        }

        private static AnimationClip Clip(string nombre, float duracion, bool bucle, Curva[] curvas)
        {
            string ruta = Carpeta + "/Clips/" + nombre + ".anim";
            AssetDatabase.DeleteAsset(ruta);
            AnimationClip clip = new AnimationClip { name = nombre, frameRate = 30f };
            foreach (Curva curva in curvas)
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(curva.Ruta, typeof(Transform), curva.Propiedad), curva.Valores);
            }
            AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = bucle;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);
            AssetDatabase.CreateAsset(clip, ruta);
            return clip;
        }

        // Ciclo de caminata o carrera: piernas y brazos opuestos, rodillas y codos flexionados.
        private static Curva[] Caminata(float duracion, float piernas, float rodilla, float brazos, float codo, float inclinacion)
        {
            float medio = duracion * 0.5f;
            Keyframe[] Oscila(float amplitud) => new[] { V(0f, amplitud), V(medio, -amplitud), V(duracion, amplitud) };
            Keyframe[] Rodilla(bool primera) => new[]
            {
                V(0f, primera ? rodilla * 0.2f : rodilla), V(medio, primera ? rodilla : rodilla * 0.2f), V(duracion, primera ? rodilla * 0.2f : rodilla),
            };
            Keyframe[] Fija(float valor) => new[] { V(0f, valor), V(duracion, valor) };
            return new[]
            {
                X(PiernaSupDer, Oscila(piernas)),
                X(PiernaSupIzq, Oscila(-piernas)),
                X(PiernaInfDer, Rodilla(true)),
                X(PiernaInfIzq, Rodilla(false)),
                X(BrazoSupDer, Oscila(-brazos)),
                X(BrazoSupIzq, Oscila(brazos)),
                X(BrazoInfDer, Fija(-codo)),
                X(BrazoInfIzq, Fija(-codo)),
                X(RutaTorso, Fija(inclinacion)),
            };
        }
    }
}
