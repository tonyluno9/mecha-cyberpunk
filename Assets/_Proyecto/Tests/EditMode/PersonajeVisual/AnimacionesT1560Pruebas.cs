using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1560 (decisión de Tony 21-nov, "en Unity se ven deformadas"). La primera parte es la tabla de decisiones (lógica pura, corre sin importar nada). La segunda comprueba, por muestreo
    /// representativo, lo que deja AnimacionesT1560.Aplicar: importación de los clips, estados nuevos, overrides y prefabs. Necesita haber corrido Aplicar.
    /// </summary>
    public class AnimacionesT1560Pruebas
    {
        // ---------- Tabla de decisiones (lógica pura) ----------

        [TestCase("PJ01_Muerte_Frente", 51)]
        [TestCase("PJ01_Dano_Levantarse_BocaArriba", 88)]
        [TestCase("NPC_Balazo_Frente", 25)]
        [TestCase("NPC_Balazo_Espalda", 25)]
        [TestCase("E15_Muerte", 71)]
        [TestCase("E16_Muerte", 71)]
        [TestCase("PJ01_Conductor_SerSacado", 74)]
        [TestCase("PJ01_Carro_SacarConductor", 98)]
        [TestCase("PJ01_Muerte_CaidaAltura", 46)]
        [TestCase("NPC_Platicar_A", 179)]
        [TestCase("NPC_Celular_Caminando", 1268)]
        public void Tabla_RangosDeLaBitacora(string clip, int ultimo)
        {
            DecisionClipT1560 d = TablaClipsT1560.Decidir(clip);
            Assert.IsNotNull(d, clip);
            Assert.IsTrue(d.Nuevo, clip);
            Assert.AreEqual(ultimo, d.Ultimo, clip);
        }

        [Test]
        public void Tabla_LaEscalaDePJ01_EsLaDelModelo_YNoElFactorDeBlender()
        {
            Assert.AreEqual(1.88f, TablaClipsT1560.EscalaPJ01, 1e-4f, "PJ01.fbx: globalScale y userData escala=1.88; 1.8367 es la escala de la armadura en Blender");
            var modelo = AssetImporter.GetAtPath(ImportadorClipsPJ01.RutaModeloOrigen) as ModelImporter;
            if (modelo != null)
            {
                Assert.AreEqual(TablaClipsT1560.EscalaPJ01, modelo.globalScale, 1e-4f);
            }
        }

        [TestCase("PJ01_Muerte_Frente", "PJ01")]
        [TestCase("NPC_Dano_Izquierda", "PJ01")]
        [TestCase("E15_Caida_Aire", "E15")]
        [TestCase("E16_Cojear", "E16")]
        [TestCase("E18_Caminar", "E18")]
        [TestCase("E20_Golpe_Brazo", "E20")]
        public void Tabla_DuenioPorPrefijo(string clip, string duenio)
        {
            Assert.AreEqual(duenio, TablaClipsT1560.DuenioDe(clip));
        }

        [TestCase("E15_Muerte", true)]
        [TestCase("E16_Caida_Aire", true)]
        [TestCase("E20_Golpe_Brazo", true)]
        [TestCase("E18_Caminar", true)]
        [TestCase("E15_Caminar", false)]
        [TestCase("E18_Caida_Aire", false)]
        [TestCase("NPC_Platicar_A", false)]
        [TestCase("PJ01_Muerte_Frente", false)]
        public void Tabla_RigPropio_SoloLosQueTraenElEsqueletoDelPersonaje(string clip, bool propio)
        {
            Assert.AreEqual(propio, TablaClipsT1560.Decidir(clip).RigPropio, clip);
        }

        [TestCase("NPC_Platicar_A", true)]
        [TestCase("NPC_Discutir_2", true)]
        [TestCase("PJ01_Herido_Idle", true)]
        [TestCase("PJ01_Sentado", true)]      // lo pide T-1561
        [TestCase("PJ01_Colgado_Idle", true)] // lo pide T-1561
        [TestCase("E16_Cojear", true)]
        [TestCase("PJ01_Muerte_Frente", false)]
        [TestCase("PJ01_Burla", false)]
        [TestCase("NPC_Senalar", false)]
        [TestCase("E16_Encenderse", false)]
        [TestCase("PJ01_Golpe_Protesis", false)]
        public void Tabla_SoloLosCiclosRepiten(string clip, bool bucle)
        {
            Assert.AreEqual(bucle, TablaClipsT1560.Decidir(clip).Bucle, clip);
        }

        [TestCase("PJ01_Golpe_Der", RaizClipT1560.Sitio)]
        [TestCase("NPC_Platicar_D", RaizClipT1560.Sitio)]
        [TestCase("PJ01_Muerte_Frente", RaizClipT1560.Avanza)]
        [TestCase("PJ01_Derribo_Explosion", RaizClipT1560.Avanza)]
        [TestCase("PJ01_Dano_Empujon", RaizClipT1560.Avanza)]
        [TestCase("NPC_SacadoDelCarro_Huir", RaizClipT1560.Avanza)]
        [TestCase("E16_Cojear", RaizClipT1560.Avanza)]
        [TestCase("PJ01_Escalera_SubirMixamo", RaizClipT1560.Sube)]
        [TestCase("PJ01_Colgado_Subir", RaizClipT1560.SubeYAvanza)]
        [TestCase("NPC_Caminar_Circulo", RaizClipT1560.Gira)]
        [TestCase("PJ01_Caminar", RaizClipT1560.SinCambio)]
        [TestCase("PJ01_Moto_Subir", RaizClipT1560.SinCambio)]
        public void Tabla_Raiz(string clip, RaizClipT1560 esperada)
        {
            Assert.AreEqual(esperada, TablaClipsT1560.Decidir(clip).Raiz, clip);
        }

        [Test]
        public void Tabla_LosClipsQueMuevenElCuerpoNoSeHornean_YLosDelSitioSi()
        {
            // Raíz que mueve la cápsula (RaizCaidaJugador): las muertes, levantarse, derribo, empujón y explosión cercana avanzan (XZ como root motion).
            foreach (string clip in new[] { "PJ01_Muerte_Explosion", "PJ01_Muerte_Atropello", "PJ01_Muerte_CaidaAltura", "PJ01_Dano_Levantarse_BocaArriba", "PJ01_Derribo_Explosion", "PJ01_Dano_ExplosionCercana" })
            {
                Assert.AreEqual(RaizClipT1560.Avanza, TablaClipsT1560.Decidir(clip).Raiz, clip);
            }
            // Todo clip nuevo que está en ClipsEnElSitio tiene su fila en ClipsNuevos y no está en las listas de movimiento.
            var nuevos = TablaClipsT1560.ClipsNuevos.Select(c => c.Nombre).ToArray();
            foreach (string clip in TablaClipsT1560.ClipsEnElSitio)
            {
                CollectionAssert.Contains(nuevos, clip);
                Assert.AreEqual(RaizClipT1560.Sitio, TablaClipsT1560.Decidir(clip).Raiz, clip);
            }
        }

        [Test]
        public void Tabla_SinClipsRepetidos_YTodosLosCiclosYRigPropioExisten()
        {
            var nuevos = TablaClipsT1560.ClipsNuevos.Select(c => c.Nombre).ToArray();
            Assert.AreEqual(nuevos.Length, nuevos.Distinct().Count());
            foreach (string clip in TablaClipsT1560.Ciclos)
            {
                CollectionAssert.Contains(nuevos, clip);
            }
            CollectionAssert.IsSubsetOf(TablaClipsT1560.ClipsConRigPropio.Where(c => nuevos.Contains(c)), nuevos);
        }

        [Test]
        public void Estados_IdsDeGestoYAccion_NoChocanConGestoVisual()
        {
            var ids = EstadosNpcT1560.Filas.Where(f => f.Tipo == TipoEstadoT1560.Gesto).Select(f => f.Id).ToArray();
            Assert.AreEqual(ids.Length, ids.Distinct().Count());
            foreach (int id in ids.Where(i => i != (int)GestoNpcT1560.BloquearTambaleo))
            {
                Assert.GreaterOrEqual(id, 40, "los gestos de T-1560 empiezan en 40 (T-1561 usa 15 a 20)");
            }
            Assert.AreEqual(19, (int)GestoNpcT1560.BloquearTambaleo, "lo definió T-1561: GestoVisual.BloquearTambaleo = 19");
            var acciones = EstadosNpcT1560.Filas.Where(f => f.Tipo == TipoEstadoT1560.Accion).Select(f => f.Id).ToArray();
            Assert.AreEqual(acciones.Length, acciones.Distinct().Count());
            Assert.IsFalse(acciones.Contains(0), "0 es ninguna acción");
            Assert.AreEqual(EstadosNpcT1560.Filas.Length, EstadosNpcT1560.Filas.Select(f => f.Estado).Distinct().Count());
        }

        [Test]
        public void Estados_NingunoSeRepiteConLosEstadosBase()
        {
            foreach (string estado in EstadosNpcT1560.Nombres)
            {
                CollectionAssert.DoesNotContain(ReglasAnimacion.EstadosBase, estado);
            }
        }

        [Test]
        public void TablaPorPapel_DecisionesDeTony21Nov()
        {
            // Jugador: golpes nuevos; los descartados no aparecen.
            Assert.AreEqual("PJ01_Golpe_Der", Fila("Jugador", "Punos_GolpeDer"));
            Assert.AreEqual("PJ01_Golpe_Izq", Fila("Jugador", "Punos_GolpeIzq"));
            Assert.AreEqual("PJ01_Golpe_Protesis", Fila("Jugador", "Punos_Fuerte"));
            Assert.AreEqual("PJ01_Dano_Frente", Fila("Jugador", "Pistola_Dano_Frente"));
            Assert.AreEqual("PJ01_Dano_Espalda", Fila("Jugador", "Pistola_Dano_Espalda"));
            foreach (string papel in ConstructorControladorBase.Papeles)
            {
                foreach (string descartado in new[] { "PJ01_Golpe1_Directo", "PJ01_Golpe2_Gancho", "PJ01_Punos_Fuerte" })
                {
                    CollectionAssert.DoesNotContain(ConstructorControladorBase.ClipsPorPapel[papel].Select(f => f.Papel).ToArray(), descartado, papel);
                }
                Assert.AreEqual("PJ01_Burla", Fila(papel, "Skull_Burla"), papel);
                Assert.AreEqual("PJ01_Presumir", Fila(papel, "Nova_Flexing"), papel);
            }
            // NPC: mismas muertes del jugador y daño lateral propio.
            Assert.AreEqual("PJ01_Muerte_Frente", Fila("NPC", "Muerte_Adelante"));
            Assert.AreEqual("PJ01_Muerte_Espaldas", Fila("NPC", "Muerte_Atras"));
            Assert.AreEqual("PJ01_Muerte_Desmayo", Fila("NPC", "Muerte_Desmayo"));
            Assert.AreEqual("NPC_Dano_Izquierda", Fila("NPC", "PJ01_Dano_Izquierda"));
            Assert.AreEqual("NPC_Dano_Derecha", Fila("NPC", "PJ01_Dano_Derecha"));
        }

        [Test]
        public void TablaExtra_Robots_CaidasHeridoYCojearPropios_YE20()
        {
            foreach (string robot in new[] { "E15", "E16" })
            {
                (string Base, string Papel)[] t = Extra(robot);
                Assert.AreEqual(robot + "_Caida_Aire", Valor(t, "Loco_Caida"), robot);
                Assert.AreEqual(robot + "_Caida_Aterrizaje", Valor(t, "Loco_SaltoAterrizaje"), robot);
                Assert.AreEqual(robot + "_Caida_Levantarse", Valor(t, "E18_Caida_Levantarse"), robot);
                Assert.AreEqual(robot + "_Herido_Idle", Valor(t, "PJ01_Herido_Idle"), robot);
                Assert.AreEqual(robot + "_Muerte", Valor(t, "Muerte_Adelante"), robot);
            }
            Assert.AreEqual("E16_Cojear", Valor(Extra("E16"), "E15_Cojear"));
            Assert.AreEqual("E15_Cojear", Valor(Extra("E15"), "E15_Cojear"));
            Assert.AreEqual("E18_Caminar", Valor(Extra("E18"), "Loco_Caminar"));
            Assert.AreEqual("E18_Correr", Valor(Extra("E18"), "Loco_Correr"));
            Assert.AreEqual("E18_Golpe_Der", Valor(Extra("E18"), "Punos_GolpeDer"));
            Assert.AreEqual("E20_Golpe_Brazo", Valor(Extra("E20"), "Punos_Fuerte"));
            // E16_Bloquear_Tambaleo y E16_Encenderse son solo del E16: ningún otro robot los trae.
            Assert.IsNull(Valor(Extra("E15"), "E16_Bloquear_Tambaleo"));
            Assert.IsNull(Valor(Extra("E18"), "E16_Encenderse"));
        }

        private static string Fila(string papel, string clave) => Valor(ConstructorControladorBase.ClipsPorPapel[papel], clave);

        private static string Valor((string Base, string Papel)[] tabla, string clave)
        {
            foreach ((string b, string p) in tabla)
            {
                if (b == clave) { return p; }
            }
            return null;
        }

        private static (string Base, string Papel)[] Extra(string nombre)
        {
            foreach ((string n, string padre, (string Base, string Papel)[] clips) in ClipsNpcNovaT1506.OverridesExtra)
            {
                if (n == nombre)
                {
                    return ClipsNpcNovaT1506.Combinar(ConstructorControladorBase.ClipsPorPapel[padre], clips);
                }
            }
            Assert.Fail("no hay override extra " + nombre);
            return null;
        }

        // ---------- Lo que deja Aplicar ----------

        private static ModelImporter Importador(string nombre)
        {
            var importador = AssetImporter.GetAtPath(TablaClipsT1560.RutaClip(nombre)) as ModelImporter;
            Assert.IsNotNull(importador, nombre + " no está en " + TablaClipsT1560.RutaClip(nombre));
            return importador;
        }

        [TestCase("PJ01_Muerte_Frente", 51)]
        [TestCase("PJ01_Dano_Levantarse_BocaArriba", 88)]
        [TestCase("NPC_Balazo_Frente", 25)]
        [TestCase("NPC_Balazo_Espalda", 25)]
        [TestCase("E15_Muerte", 71)]
        [TestCase("E16_Muerte", 71)]
        [TestCase("PJ01_Conductor_SerSacado", 74)]
        [TestCase("PJ01_Carro_SacarConductor", 98)]
        [TestCase("NPC_Platicar_B", 119)]
        [TestCase("E16_Encenderse", 72)]
        public void Importacion_Rango_PrimerAlUltimoCuadroDelFbx(string clip, int ultimo)
        {
            ModelImporterClipAnimation c = Importador(clip).clipAnimations[0];
            Assert.AreEqual(1f, c.firstFrame, 1e-3f, clip);
            Assert.AreEqual(ultimo, c.lastFrame, 1e-3f, clip);
            ModelImporterClipAnimation toma = Importador(clip).defaultClipAnimations[0];
            Assert.AreEqual(toma.lastFrame, c.lastFrame, 1e-3f, clip + ": el rango cubre toda la toma del FBX");
        }

        [TestCase("PJ01_Muerte_Frente")]
        [TestCase("NPC_Platicar_A")]
        [TestCase("PJ01_Caminar")]
        [TestCase("E15_Caida_Aire")]
        [TestCase("E18_Caminar")]
        public void Importacion_SinCompresion_YHumanoid(string clip)
        {
            ModelImporter imp = Importador(clip);
            Assert.AreEqual(ModelImporterAnimationCompression.Off, imp.animationCompression, clip);
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, clip);
            Assert.AreEqual(1, imp.clipAnimations.Length, clip);
            Assert.AreEqual(clip, imp.clipAnimations[0].name, clip);
        }

        [TestCase("PJ01_Golpe_Der", true, true, true)]
        [TestCase("NPC_Platicar_C", true, true, true)]
        [TestCase("PJ01_Muerte_Frente", true, true, false)]
        [TestCase("PJ01_Derribo_Explosion", true, true, false)]
        [TestCase("PJ01_Escalera_SubirMixamo", true, false, true)]
        [TestCase("PJ01_Colgado_Subir", true, false, false)]
        public void Importacion_Raiz(string clip, bool rotacion, bool altura, bool xz)
        {
            ModelImporterClipAnimation c = Importador(clip).clipAnimations[0];
            Assert.AreEqual(rotacion, c.lockRootRotation, clip + " rotación");
            Assert.AreEqual(altura, c.lockRootHeightY, clip + " altura");
            Assert.AreEqual(xz, c.lockRootPositionXZ, clip + " XZ");
        }

        [TestCase("NPC_Platicar_A", true)]
        [TestCase("PJ01_Herido_Idle", true)]
        [TestCase("PJ01_Sentado", true)]
        [TestCase("PJ01_Colgado_Idle", true)]
        [TestCase("PJ01_Burla", false)]
        [TestCase("PJ01_Muerte_Frente", false)]
        [TestCase("NPC_Senalar", false)]
        public void Importacion_Bucle(string clip, bool bucle)
        {
            Assert.AreEqual(bucle, Importador(clip).clipAnimations[0].loopTime, clip);
        }

        [TestCase("PJ01_Muerte_Frente")]
        [TestCase("NPC_Platicar_A")]
        [TestCase("PJ01_Burla")]
        public void Importacion_ClipsPJ01_UsanLaEscalaYLaPoseTDePJ01(string clip)
        {
            ModelImporter imp = Importador(clip);
            Assert.AreEqual(TablaClipsT1560.EscalaPJ01, imp.globalScale, 1e-4f, clip);
            Assert.IsTrue(AnimacionesT1560.DescripcionCoincideConPJ01(imp), clip + ": el avatar es el de PJ01 (pose T y mapeo)");
        }

        [TestCase("E15_Caida_Aire", "E15")]
        [TestCase("E15_Muerte", "E15")]
        [TestCase("E16_Caida_Aterrizaje", "E16")]
        [TestCase("E16_Herido_Idle", "E16")]
        [TestCase("E20_Golpe_Brazo", "E20")]
        public void Importacion_ClipsConRigPropio_UsanLaEscalaYElAvatarDeSuPersonaje(string clip, string duenio)
        {
            ModelImporter imp = Importador(clip);
            var modelo = (ModelImporter)AssetImporter.GetAtPath(TablaClipsT1560.RutaModeloDe(duenio));
            Assert.AreEqual(modelo.globalScale, imp.globalScale, 1e-4f, clip);
            Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, imp.avatarSetup, clip + ": avatar propio del clip (Copy From Other no saca el clip, T-1553)");
            Assert.GreaterOrEqual(imp.humanDescription.human.Length, 15, clip);
            Assert.AreEqual(ModelImporterMaterialImportMode.None, imp.materialImportMode, clip);
            ModelImporterClipAnimation c = imp.clipAnimations[0];
            Assert.IsFalse(c.keepOriginalOrientation, clip);
            Assert.AreEqual(0f, c.rotationOffset, 1e-4f, clip);
        }

        [Test]
        public void Importacion_AvatarDelE18_EsValido()
        {
            Assert.IsNotNull(ClipsRobotsNovaT1553.AvatarDelPersonaje("E18_Caminar"), "avatar humanoide válido del E18 (corre Aplicar)");
        }

        // ---------- Controlador y overrides ----------

        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        [Test]
        public void Controlador_TieneElParametroAccionNpc_YLosEstadosNuevos()
        {
            AnimatorController c = Base();
            Assert.IsTrue(c.parameters.Any(p => p.name == EstadosNpcT1560.AccionNpc && p.type == AnimatorControllerParameterType.Int));
            AnimatorStateMachine m = c.layers[0].stateMachine;
            foreach ((string estado, string clip, TipoEstadoT1560 tipo, int id, string disparador) in EstadosNpcT1560.Filas)
            {
                AnimatorState s = m.states.Select(h => h.state).FirstOrDefault(x => x.name == estado);
                Assert.IsNotNull(s, estado);
                Assert.IsNotNull(s.motion, estado + " sin clip");
                Assert.AreEqual(clip, s.motion.name, estado);
                Assert.GreaterOrEqual(s.transitions.Length, 1, estado + " sin salida");
                if (tipo == TipoEstadoT1560.Gesto)
                {
                    AnimatorStateTransition t = m.anyStateTransitions.Single(x => x.destinationState == s);
                    Assert.IsTrue(t.conditions.Any(k => k.parameter == ReglasAnimacion.Gesto), estado);
                    Assert.IsTrue(t.conditions.Any(k => k.parameter == ReglasAnimacion.GestoTipo && Mathf.Approximately(k.threshold, id)), estado + ": GestoTipo " + id);
                    Assert.AreEqual(ReglasAnimacion.EtiquetaGesto, s.tag, estado);
                }
                else
                {
                    bool entra = m.states.Any(h => h.state.transitions.Any(x => x.destinationState == s
                        && x.conditions.Any(k => k.parameter == EstadosNpcT1560.AccionNpc && k.mode == AnimatorConditionMode.Equals && Mathf.Approximately(k.threshold, id))));
                    Assert.IsTrue(entra, estado + ": entra con AccionNpc = " + id);
                    Assert.IsTrue(s.transitions.Any(x => x.conditions.Any(k => k.parameter == EstadosNpcT1560.AccionNpc && k.mode == AnimatorConditionMode.NotEqual)), estado + ": sale al cambiar AccionNpc");
                }
            }
        }

        [Test]
        public void Controlador_BloquearTambaleo_EntraConGestoTipo19()
        {
            AnimatorStateMachine m = Base().layers[0].stateMachine;
            AnimatorState s = m.states.Select(h => h.state).Single(x => x.name == "BloquearTambaleo");
            Assert.AreEqual("E16_Bloquear_Tambaleo", s.motion.name);
            AnimatorStateTransition t = m.anyStateTransitions.Single(x => x.destinationState == s);
            Assert.IsTrue(t.conditions.Any(k => k.parameter == ReglasAnimacion.GestoTipo && Mathf.Approximately(k.threshold, 19f)));
        }

        private static Dictionary<string, string> Valores(string ruta)
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            Assert.IsNotNull(o, "Falta " + ruta);
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            return pares.Where(p => p.Key != null).ToDictionary(p => p.Key.name, p => p.Value != null ? p.Value.name : "(vacío)");
        }

        [TestCase("Jugador", "Punos_GolpeDer", "PJ01_Golpe_Der")]
        [TestCase("Jugador", "Punos_GolpeIzq", "PJ01_Golpe_Izq")]
        [TestCase("Jugador", "Punos_Fuerte", "PJ01_Golpe_Protesis")]
        [TestCase("Jugador", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Jugador", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("Jugador", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Jugador", "Nova_Flexing", "PJ01_Presumir")]
        [TestCase("SKULLS", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Jefe", "Nova_Flexing", "PJ01_Presumir")]
        [TestCase("NPC", "Muerte_Adelante", "PJ01_Muerte_Frente")]
        [TestCase("NPC", "Muerte_Atras", "PJ01_Muerte_Espaldas")]
        [TestCase("NPC", "PJ01_Dano_Izquierda", "NPC_Dano_Izquierda")]
        [TestCase("NPC", "PJ01_Dano_Derecha", "NPC_Dano_Derecha")]
        [TestCase("Jugador", "PJ01_Dano_Izquierda", "PJ01_Dano_Izquierda")]
        public void Override_DePapel_TieneLoDeT1560(string papel, string clave, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ConstructorControladorBase.RutaOverride(papel))[clave]);
        }

        [TestCase("NPC_Mujer", "Muerte_Adelante", "PJ01_Muerte_Frente")]
        [TestCase("NPC_Mayor", "PJ01_Dano_Izquierda", "NPC_Dano_Izquierda")]
        [TestCase("NPC_Celular", "Skull_Burla", "PJ01_Burla")]
        [TestCase("E15", "Loco_Caida", "E15_Caida_Aire")]
        [TestCase("E15", "Loco_SaltoAterrizaje", "E15_Caida_Aterrizaje")]
        [TestCase("E15", "PJ01_Herido_Idle", "E15_Herido_Idle")]
        [TestCase("E15", "E15_Cojear", "E15_Cojear")]
        [TestCase("E16", "Loco_Caida", "E16_Caida_Aire")]
        [TestCase("E16", "Loco_SaltoAterrizaje", "E16_Caida_Aterrizaje")]
        [TestCase("E16", "PJ01_Herido_Idle", "E16_Herido_Idle")]
        [TestCase("E16", "E15_Cojear", "E16_Cojear")]
        [TestCase("E16", "Muerte_Adelante", "E16_Muerte")]
        [TestCase("E18", "Loco_Caminar", "E18_Caminar")]
        [TestCase("E18", "Punos_GolpeDer", "E18_Golpe_Der")]
        [TestCase("E20", "Punos_GolpeDer", "E20_Golpe_Brazo")]
        [TestCase("E20", "Skull_Burla", "PJ01_Burla")]
        public void OverrideExtra_TieneLoDeT1560(string nombre, string clave, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ClipsNpcNovaT1506.RutaOverrideExtra(nombre))[clave]);
        }

        [Test]
        public void Overrides_NingunaClaveQuedaSinClip()
        {
            foreach ((string ruta, (string Base, string Papel)[] tabla) in AnimacionesT1540.TablasDeOverrides())
            {
                string[] vacias = Valores(ruta).Where(kv => kv.Value == "(vacío)").Select(kv => kv.Key).ToArray();
                Assert.IsEmpty(vacias, ruta + ": claves sin clip: " + string.Join(", ", vacias));
            }
        }

        [Test]
        public void Prefabs_Visual_UsanElOverrideDeSuPersonaje()
        {
            foreach ((string visual, string nombreOverride) in AnimacionesT1560.OverridesEsperadosDeVisuales())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual.Substring("Visual_".Length)));
                if (prefab == null) { continue; }
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(a, visual);
                Assert.AreEqual("Override_" + nombreOverride, a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "(ninguno)", visual);
            }
        }
    }
}
