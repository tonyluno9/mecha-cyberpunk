using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MechaCyberpunk.Herramientas.ModoVideo
{
    /// <summary>
    /// T-472: modo video. F10 oculta o muestra todo el HUD, F11 cámara libre lenta (WASD + ratón, Q/E, Shift rápido, rueda cambia la velocidad),
    /// F12 captura en alta resolución a Capturas/ (junto al .exe, o la raíz del repo en el editor). Se crea solo en ejecución y no existe en la
    /// demo final (símbolo DEMO_FINAL). Para no depender de otras áreas guarda y restaura el estado de los Canvas, las cámaras y las acciones de entrada.
    /// </summary>
    public class ModoVideo : MonoBehaviour
    {
        private const float SensibilidadRaton = 0.1f;

        public static ModoVideo Instancia { get; private set; }

        public bool HudOculto { get; private set; }
        public bool CamaraLibreActiva { get; private set; }
        public Camera CamaraLibre { get; private set; }
        public float Velocidad { get; private set; } = ReglasModoVideo.VelocidadInicial;
        public string UltimaCaptura { get; private set; }

        private readonly List<Canvas> canvasOcultos = new List<Canvas>();
        private readonly List<Camera> camarasApagadas = new List<Camera>();
        private readonly List<InputAction> accionesApagadas = new List<InputAction>();
        private CursorLockMode cursorAntes;
        private bool cursorVisibleAntes;
        private float yaw;
        private float pitch;
        private Canvas canvasAviso;
        private Text textoAviso;
        private float finAviso;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            Instancia = null;
        }

#if !DEMO_FINAL
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstalarAlCargar()
        {
            Asegurar();
        }
#endif

        /// <summary>Crea el modo video si todavía no existe (una vez por sesión).</summary>
        public static ModoVideo Asegurar()
        {
            if (Instancia != null)
            {
                return Instancia;
            }
            GameObject objeto = new GameObject("ModoVideo");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<ModoVideo>();
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                if (CamaraLibreActiva)
                {
                    SalirCamaraLibre();
                }
                Instancia = null;
            }
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.f10Key.wasPressedThisFrame)
                {
                    AlternarHud();
                }
                if (teclado.f11Key.wasPressedThisFrame)
                {
                    AlternarCamaraLibre();
                }
                if (teclado.f12Key.wasPressedThisFrame)
                {
                    Capturar();
                }
            }
            if (CamaraLibreActiva)
            {
                MoverCamaraLibre(teclado, Mouse.current);
            }
            if (canvasAviso != null && canvasAviso.enabled && Time.unscaledTime > finAviso)
            {
                canvasAviso.enabled = false;
            }
        }

        public void AlternarHud()
        {
            if (HudOculto)
            {
                foreach (Canvas canvas in canvasOcultos)
                {
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }
                canvasOcultos.Clear();
                HudOculto = false;
                return;
            }
            Canvas[] todos = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas canvas in todos)
            {
                if (canvas == canvasAviso || !canvas.isRootCanvas || !canvas.enabled || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }
                canvas.enabled = false;
                canvasOcultos.Add(canvas);
            }
            HudOculto = true;
        }

        public void AlternarCamaraLibre()
        {
            if (CamaraLibreActiva)
            {
                SalirCamaraLibre();
            }
            else
            {
                EntrarCamaraLibre();
            }
        }

        private void EntrarCamaraLibre()
        {
            Camera origen = Camera.main;
            camarasApagadas.Clear();
            foreach (Camera camara in Camera.allCameras)
            {
                if (camara.enabled && camara.targetTexture == null)
                {
                    if (origen == null)
                    {
                        origen = camara;
                    }
                    camarasApagadas.Add(camara);
                }
            }
            GameObject objeto = new GameObject("CamaraLibreModoVideo");
            CamaraLibre = objeto.AddComponent<Camera>();
            if (origen != null)
            {
                CamaraLibre.CopyFrom(origen);
                CamaraLibre.targetTexture = null;
                objeto.transform.SetPositionAndRotation(origen.transform.position, origen.transform.rotation);
            }
            UniversalAdditionalCameraData datos = objeto.GetComponent<UniversalAdditionalCameraData>();
            if (datos == null)
            {
                datos = objeto.AddComponent<UniversalAdditionalCameraData>();
            }
            datos.renderPostProcessing = true;
            CamaraLibre.enabled = true;
            foreach (Camera camara in camarasApagadas)
            {
                camara.enabled = false;
            }
            // Apaga la entrada del jugador: todas las acciones activas ahora (se vuelven a activar al salir).
            accionesApagadas.Clear();
            InputSystem.ListEnabledActions(accionesApagadas);
            foreach (InputAction accion in accionesApagadas)
            {
                accion.Disable();
            }
            cursorAntes = Cursor.lockState;
            cursorVisibleAntes = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Vector3 euler = objeto.transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            CamaraLibreActiva = true;
        }

        private void SalirCamaraLibre()
        {
            CamaraLibreActiva = false;
            if (CamaraLibre != null)
            {
                Destroy(CamaraLibre.gameObject);
                CamaraLibre = null;
            }
            foreach (Camera camara in camarasApagadas)
            {
                if (camara != null)
                {
                    camara.enabled = true;
                }
            }
            camarasApagadas.Clear();
            foreach (InputAction accion in accionesApagadas)
            {
                accion.Enable();
            }
            accionesApagadas.Clear();
            Cursor.lockState = cursorAntes;
            Cursor.visible = cursorVisibleAntes;
        }

        private void MoverCamaraLibre(Keyboard teclado, Mouse raton)
        {
            if (CamaraLibre == null)
            {
                return;
            }
            Transform t = CamaraLibre.transform;
            if (raton != null)
            {
                Velocidad = ReglasModoVideo.AjustarVelocidad(Velocidad, raton.scroll.ReadValue().y);
                Vector2 delta = raton.delta.ReadValue();
                yaw += delta.x * SensibilidadRaton;
                pitch = Mathf.Clamp(pitch - delta.y * SensibilidadRaton, -89f, 89f);
                t.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            if (teclado == null)
            {
                return;
            }
            Vector3 direccion = Vector3.zero;
            if (teclado.wKey.isPressed) direccion += t.forward;
            if (teclado.sKey.isPressed) direccion -= t.forward;
            if (teclado.dKey.isPressed) direccion += t.right;
            if (teclado.aKey.isPressed) direccion -= t.right;
            if (teclado.eKey.isPressed) direccion += Vector3.up;
            if (teclado.qKey.isPressed) direccion -= Vector3.up;
            float v = ReglasModoVideo.VelocidadEfectiva(Velocidad, teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed);
            t.position += direccion.normalized * (v * Time.unscaledDeltaTime);
        }

        public static string CarpetaCapturas()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Capturas");
        }

        public void Capturar()
        {
            string carpeta = CarpetaCapturas();
            Directory.CreateDirectory(carpeta);
            UltimaCaptura = Path.Combine(carpeta, ReglasModoVideo.NombreCaptura(System.DateTime.Now));
            ScreenCapture.CaptureScreenshot(UltimaCaptura, ReglasModoVideo.SuperTamano);
            StartCoroutine(MostrarAvisoTrasCaptura("CAPTURA GUARDADA: " + Path.GetFileName(UltimaCaptura)));
        }

        // El aviso sale dos cuadros después para que no quede dentro de la foto.
        private IEnumerator MostrarAvisoTrasCaptura(string mensaje)
        {
            yield return null;
            yield return null;
            if (canvasAviso == null)
            {
                CrearAviso();
            }
            textoAviso.text = mensaje;
            canvasAviso.enabled = true;
            finAviso = Time.unscaledTime + 2f;
        }

        private void CrearAviso()
        {
            GameObject objetoCanvas = new GameObject("AvisoModoVideo");
            objetoCanvas.transform.SetParent(transform, false);
            canvasAviso = objetoCanvas.AddComponent<Canvas>();
            canvasAviso.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasAviso.sortingOrder = 32000;
            GameObject objetoTexto = new GameObject("Texto");
            objetoTexto.transform.SetParent(objetoCanvas.transform, false);
            textoAviso = objetoTexto.AddComponent<Text>();
            textoAviso.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoAviso.fontSize = 22;
            textoAviso.alignment = TextAnchor.LowerCenter;
            textoAviso.color = new Color(0.4f, 1f, 0.9f);
            RectTransform rect = textoAviso.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(0f, 40f);
        }
    }
}
