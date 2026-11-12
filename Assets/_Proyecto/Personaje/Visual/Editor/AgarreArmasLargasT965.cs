using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-965 (A6): herramientas del agarre de las armas largas en R_Hand de PJ01. <see cref="Describir"/> imprime el socket de la mano y cada arma colgada (con su Modelo);
    /// <see cref="Aplicar"/> pone en JugadorAPie el agarre de <see cref="AgarresArmas"/> (lo mismo que AplicarAgarresArmas).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AgarreArmasLargasT965.Describir
    /// </summary>
    public static class AgarreArmasLargasT965
    {
        public static void Describir()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(AplicarAgarresArmas.RutaJugador);
            try
            {
                var sb = new StringBuilder("[Personaje] T-965 armas en la mano:\n");
                foreach (ApuntadoIK ik in contenido.GetComponentsInChildren<ApuntadoIK>(true))
                {
                    Transform socket = ik.SocketArma;
                    if (socket == null) { continue; }
                    sb.AppendLine("socket " + Ruta(socket) + " local " + socket.localPosition.ToString("F4") + " " + socket.localEulerAngles.ToString("F1") + " escala " + socket.lossyScale.ToString("F3"));
                    foreach (Transform arma in socket)
                    {
                        sb.AppendLine("  " + arma.name + " local " + arma.localPosition.ToString("F4") + " " + arma.localEulerAngles.ToString("F1") + " escala " + arma.localScale.ToString("F3"));
                        foreach (Transform hijo in arma)
                        {
                            sb.AppendLine("    " + hijo.name + " local " + hijo.localPosition.ToString("F4") + " " + hijo.localEulerAngles.ToString("F1") + " escala " + hijo.localScale.ToString("F3"));
                            foreach (Transform nieto in hijo)
                            {
                                sb.AppendLine("      " + nieto.name + " local " + nieto.localPosition.ToString("F4") + " " + nieto.localEulerAngles.ToString("F1") + " escala " + nieto.localScale.ToString("F3"));
                            }
                        }
                    }
                }
                Debug.Log(sb.ToString());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>
        /// Pone el agarre nuevo a las tres armas largas en una copia del jugador, muestrea los clips de arma larga de Blender y mide: la mano izquierda en el espacio del arma
        /// (para AgarreIzq) y cuánto se separa del eje del cañón; y guarda capturas (docs/capturas/pj01-rifle-mano-*.png).
        /// </summary>
        public static void Medir()
        {
            CapturaPersonajes.EscenaDePrueba();
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AplicarAgarresArmas.RutaJugador));
            Animator a = jugador.GetComponentInChildren<Animator>();
            a.enabled = false;
            Transform socket = jugador.GetComponentInChildren<ApuntadoIK>(true).SocketArma;
            var sb = new StringBuilder("[Personaje] T-965 medida del agarre:").AppendLine();
            foreach (string clipNombre in new[] { "PJ01_Rifle_Apuntar", "PJ01_Rifle_Idle", "PJ01_Rifle_ApuntarCaminar" })
            {
                AnimationClip clip = ConstructorControladorBase.Clip(clipNombre);
                clip.SampleAnimation(a.gameObject, clip.length * 0.3f);
                foreach (string nombre in new[] { "Fusil", "Escopeta", "Sniper" })
                {
                    Transform arma = socket.Find(nombre);
                    arma.localPosition = AgarresArmas.PosicionArmaLargaPJ01;
                    arma.localRotation = Quaternion.Euler(AgarresArmas.GiroArmaLargaPJ01);
                    Transform izq = a.GetBoneTransform(HumanBodyBones.LeftHand);
                    Vector3 local = arma.InverseTransformPoint(izq.position);
                    Vector3 giro = (Quaternion.Inverse(arma.rotation) * izq.rotation).eulerAngles;
                    float fueraDelEje = new Vector2(local.x, local.y).magnitude;
                    sb.AppendLine(clipNombre + " " + nombre + ": mano izquierda en el arma " + local.ToString("F3") + " giro " + giro.ToString("F1") + ", a " + fueraDelEje.ToString("F3")
                        + " m del eje; cañón " + (arma.rotation * Vector3.forward).ToString("F2") + ", arriba " + (arma.rotation * Vector3.up).ToString("F2"));
                }
            }
            Debug.Log(sb.ToString());
            foreach (Transform arma in socket)
            {
                arma.gameObject.SetActive(arma.name == "Fusil");
            }
            AnimationClip apuntar = ConstructorControladorBase.Clip("PJ01_Rifle_Apuntar");
            apuntar.SampleAnimation(a.gameObject, apuntar.length * 0.3f);
            Vector3 pecho = a.GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 frente = Vector3.ProjectOnPlane(a.GetBoneTransform(HumanBodyBones.Hips).forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, frente);
            Camera c = CapturaPersonajes.ColocarCamara(pecho - Vector3.up * 0.3f, frente - derecha * 0.8f, 1.6f, 0.3f, 40f);
            CapturaPersonajes.Guardar(c, "docs/capturas/pj01-rifle-mano-frente.png");
            c = CapturaPersonajes.ColocarCamara(pecho - Vector3.up * 0.3f, -derecha, 1.6f, 0.3f, 40f);
            CapturaPersonajes.Guardar(c, "docs/capturas/pj01-rifle-mano-lado.png");
        }

        /// <summary>
        /// Compara conversiones de los valores de Blender contra lo que pide el clip de Blender (PJ01_Rifle_Apuntar): el cañón de la mano derecha hacia la izquierda y la
        /// mira hacia arriba. Imprime, para cada una, los grados de error del cañón y de lo de arriba, y dónde queda la mano izquierda en el arma.
        /// </summary>
        public static void Candidatos()
        {
            CapturaPersonajes.EscenaDePrueba();
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AplicarAgarresArmas.RutaJugador));
            Animator a = jugador.GetComponentInChildren<Animator>();
            a.enabled = false;
            Transform socket = jugador.GetComponentInChildren<ApuntadoIK>(true).SocketArma;
            Transform arma = socket.Find("Fusil");
            AnimationClip clip = ConstructorControladorBase.Clip("PJ01_Rifle_Apuntar");
            clip.SampleAnimation(a.gameObject, clip.length * 0.3f);
            Vector3 derecha = a.GetBoneTransform(HumanBodyBones.RightHand).position;
            Vector3 izquierda = a.GetBoneTransform(HumanBodyBones.LeftHand).position;
            Vector3 canonIdeal = (izquierda - derecha).normalized;
            Vector3 arribaIdeal = Vector3.ProjectOnPlane(Vector3.up, canonIdeal).normalized;
            Quaternion eb = AgarresArmas.EulerBlender(AgarresArmas.GiroArmaLargaBlender);
            Quaternion conj = new Quaternion(eb.x, -eb.y, -eb.z, eb.w);
            Quaternion conjX = new Quaternion(-eb.x, eb.y, eb.z, eb.w);
            Quaternion inv = Quaternion.Inverse(Quaternion.Euler(90f, 0f, 180f));
            Quaternion y180 = Quaternion.Euler(0f, 180f, 0f);
            Quaternion unityE = Quaternion.Euler(AgarresArmas.GiroArmaLargaBlender);
            var candidatos = new (string, Quaternion)[]
            {
                ("Y180*Eb*invM", y180 * eb * inv), ("Eb*invM", eb * inv), ("Y180*EbConjYZ*invM", y180 * conj * inv), ("EbConjYZ*invM", conj * inv),
                ("Y180*EbConjX*invM", y180 * conjX * inv), ("EbConjX*invM", conjX * inv), ("Y180*EulerUnity*invM", y180 * unityE * inv),
                ("Y180*Eb", y180 * eb), ("Eb", eb), ("Y180*Eb*X-90", y180 * eb * Quaternion.Euler(-90f, 0f, 0f)), ("Y180*Eb*X90", y180 * eb * Quaternion.Euler(90f, 0f, 0f)),
                ("Y180*Eb*X90Z180", y180 * eb * Quaternion.Euler(90f, 0f, 180f)), ("Y180*EbConjYZ*X-90", y180 * conj * Quaternion.Euler(-90f, 0f, 0f)),
                ("Y180*EbConjYZ*X90", y180 * conj * Quaternion.Euler(90f, 0f, 0f)), ("EbConjYZ*X-90", conj * Quaternion.Euler(-90f, 0f, 0f)), ("EbConjYZ*X90", conj * Quaternion.Euler(90f, 0f, 0f)),
            };
            var sb = new StringBuilder("[Personaje] T-965 candidatos:").AppendLine();
            foreach ((string nombre, Quaternion q) in candidatos)
            {
                arma.localPosition = AgarresArmas.PosicionArmaLargaPJ01;
                arma.localRotation = q;
                float errCanon = Vector3.Angle(arma.forward, canonIdeal);
                float errArriba = Vector3.Angle(Vector3.ProjectOnPlane(arma.up, canonIdeal), arribaIdeal);
                Vector3 local = arma.InverseTransformPoint(izquierda);
                sb.AppendLine(nombre + ": cañón " + errCanon.ToString("F1") + "°, arriba " + errArriba.ToString("F1") + "°, mano izquierda en el arma " + local.ToString("F3") + ", euler " + q.eulerAngles.ToString("F1"));
            }
            Quaternion ideal = Quaternion.Inverse(socket.rotation) * Quaternion.LookRotation(canonIdeal, arribaIdeal);
            sb.AppendLine("ideal por las manos (local en la mano): " + ideal.eulerAngles.ToString("F1") + "; distancia entre manos " + Vector3.Distance(derecha, izquierda).ToString("F3"));
            Debug.Log(sb.ToString());
        }

        private static string Ruta(Transform t) => t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
    }
}
