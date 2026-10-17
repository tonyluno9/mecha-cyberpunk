#!/usr/bin/env bash
# Verificación automática: compilar, editmode, playmode, todo.
# Uso: bash herramientas/verificar.sh {compilar|editmode|playmode|playmode-por-area|todo|ensamblados}
cd "$(dirname "$0")/.." || exit 1
mkdir -p Logs

# Ensamblados de pruebas de un modo: los asmdef Tests/<Modo>/*/MechaCyberpunk.Tests.<Modo>.*.asmdef
# cuya carpeta tiene al menos un .cs (en ella o en subcarpetas). Un nombre por linea.
ensamblados_modo() {
  local modo="$1" asm dir
  for asm in Assets/_Proyecto/Tests/"$modo"/*/MechaCyberpunk.Tests."$modo".*.asmdef; do
    [ -f "$asm" ] || continue
    dir=$(dirname "$asm")
    if [ -n "$(find "$dir" -name '*.cs' -print -quit)" ]; then
      basename "$asm" .asmdef
    fi
  done
}

if [ "$1" = "ensamblados" ]; then
  echo "EditMode:"; ensamblados_modo EditMode | sed 's/^/  /'
  echo "PlayMode:"; ensamblados_modo PlayMode | sed 's/^/  /'
  exit 0
fi

unity_exe="${UNITY_EXE:-}"
if [ -z "$unity_exe" ] && [ -f .claude/maquina.local.md ]; then
  unity_exe=$(grep -m1 -E '^(- )?(unity_exe|Unity\.exe):' .claude/maquina.local.md | sed -E 's/^(- )?[^:]+:[[:space:]]*//' | tr -d '\r')
fi
if [ -z "$unity_exe" ] || [ ! -f "$unity_exe" ]; then
  echo "ERROR: no encuentro Unity.exe (UNITY_EXE o 'unity_exe:' en .claude/maquina.local.md): '$unity_exe'"
  exit 1
fi

abierto() {
  # Unity abierto: hay UnityLockfile y un Unity.exe de editor corriendo
  [ -f Temp/UnityLockfile ] || return 1
  # (el unity.exe del Hub/CLI no cuenta: solo el de ...\Editor\Unity.exe)
  powershell -NoProfile -Command 'if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited -and $_.Path -like "*\Editor\Unity.exe" }) { exit 0 } else { exit 1 }' >/dev/null 2>&1
}

if abierto; then
  echo "UNITY ABIERTO: hay un editor con este proyecto abierto (Temp/UnityLockfile). Ciérralo o usa herramientas/unity-vivo.sh detener."
  exit 3
fi

mostrar_errores() { grep -E 'error CS' "$1" | sort -u | head -n 8; }

compilar() {
  "$unity_exe" -batchmode -quit -projectPath . -logFile Logs/compilar.log
  local codigo=$?
  if [ $codigo -ne 0 ]; then
    echo "compilar FALLO (codigo $codigo)"; mostrar_errores Logs/compilar.log; return 1
  fi
  echo "compilar OK"
}

pruebas() {
  local modo="$1" minus; minus=$(echo "$modo" | tr 'A-Z' 'a-z')
  rm -f "Logs/resultados-$minus.xml"
  local lista
  lista=$(ensamblados_modo "$modo" | paste -sd ';' -)
  if [ -z "$lista" ]; then
    echo "$minus FALLO: no hay ensamblados de pruebas con codigo (Tests/$modo)"; return 1
  fi
  "$unity_exe" -batchmode -projectPath . -runTests -testPlatform "$modo" \
    -assemblyNames "$lista" -testResults "Logs/resultados-$minus.xml" -logFile "Logs/$minus.log"
  local codigo=$?
  local xml="Logs/resultados-$minus.xml"
  if [ ! -f "$xml" ]; then
    echo "$minus FALLO (codigo $codigo, sin resultados)"; mostrar_errores "Logs/$minus.log"; return 1
  fi
  local cabecera total pasaron falladas
  cabecera=$(grep -m1 '<test-run' "$xml")
  total=$(echo "$cabecera" | sed -n 's/.* total="\([0-9]*\)".*/\1/p')
  pasaron=$(echo "$cabecera" | sed -n 's/.* passed="\([0-9]*\)".*/\1/p')
  falladas=$(echo "$cabecera" | sed -n 's/.* failed="\([0-9]*\)".*/\1/p')
  if [ -z "$total" ] || [ "$total" = "0" ]; then
    echo "$minus FALLO: no corrio ninguna prueba (total=0)"; mostrar_errores "Logs/$minus.log"; return 1
  fi
  if [ "$codigo" -ne 0 ] || [ "$falladas" != "0" ]; then
    echo "$minus FALLO $pasaron/$total (fallaron $falladas)"
    grep -E '<test-case[^>]*result="Failed"' "$xml" | sed -n 's/.* fullname="\([^"]*\)".*/  fallo: \1/p' | head -n 8
    mostrar_errores "Logs/$minus.log"
    return 1
  fi
  echo "$minus $pasaron/$total"
}

# PlayMode un ensamblado por llamada de Unity, con limite de tiempo por area (LIMITE_AREA en segundos, 900 por defecto).
# AREAS="Economia Mecha" (opcional) limita las areas que corre. Si un area pasa el limite se cierra SOLO el Unity
# que lanzo este script (por su PID de Windows, con su arbol de procesos) y se sigue con la siguiente.
playmode_por_area() {
  local limite="${LIMITE_AREA:-900}" asm area xml log total_areas=0 pasaron_areas=0 gran_total=0 gran_pasaron=0
  local lista_asm filtro="${AREAS:-}"
  lista_asm=$(ensamblados_modo PlayMode)
  if [ -z "$lista_asm" ]; then
    echo "playmode-por-area FALLO: no hay ensamblados de pruebas con codigo (Tests/PlayMode)"; return 1
  fi
  for asm in $lista_asm; do
    area="${asm#MechaCyberpunk.Tests.PlayMode.}"
    if [ -n "$filtro" ] && ! echo " $filtro " | grep -q " $area "; then continue; fi
    total_areas=$((total_areas + 1))
    xml="Logs/resultados-playmode-$area.xml"; log="Logs/playmode-$area.log"
    rm -f "$xml"
    "$unity_exe" -batchmode -projectPath . -runTests -testPlatform PlayMode \
      -assemblyNames "$asm" -testResults "$xml" -logFile "$log" &
    local pid=$! winpid inicio=$SECONDS limite_alcanzado=0
    winpid=$(cat "/proc/$pid/winpid" 2>/dev/null || echo "$pid")
    while kill -0 "$pid" 2>/dev/null; do
      if [ $((SECONDS - inicio)) -ge "$limite" ]; then
        limite_alcanzado=1
        taskkill //PID "$winpid" //T //F >/dev/null 2>&1
        break
      fi
      sleep 2
    done
    wait "$pid" 2>/dev/null
    local codigo=$?
    if [ "$limite_alcanzado" = "1" ]; then
      rm -f Temp/UnityLockfile
      echo "$area LIMITE $limite s"
      continue
    fi
    if [ ! -f "$xml" ]; then
      echo "$area FALLO (codigo $codigo, sin resultados)"; mostrar_errores "$log"; continue
    fi
    local cabecera total pasaron falladas
    cabecera=$(grep -m1 '<test-run' "$xml")
    total=$(echo "$cabecera" | sed -n 's/.* total="\([0-9]*\)".*/\1/p')
    pasaron=$(echo "$cabecera" | sed -n 's/.* passed="\([0-9]*\)".*/\1/p')
    falladas=$(echo "$cabecera" | sed -n 's/.* failed="\([0-9]*\)".*/\1/p')
    if [ -z "$total" ] || [ "$total" = "0" ]; then
      echo "$area FALLO: no corrio ninguna prueba (total=0)"; mostrar_errores "$log"; continue
    fi
    gran_total=$((gran_total + total)); gran_pasaron=$((gran_pasaron + pasaron))
    if [ "$codigo" -ne 0 ] || [ "$falladas" != "0" ]; then
      echo "$area FALLO $pasaron/$total (fallaron $falladas)"
      grep -E '<test-case[^>]*result="Failed"' "$xml" | sed -n 's/.* fullname="\([^"]*\)".*/  fallo: \1/p' | head -n 8
      continue
    fi
    pasaron_areas=$((pasaron_areas + 1))
    echo "$area $pasaron/$total"
  done
  echo "playmode-por-area: $pasaron_areas/$total_areas areas pasaron, $gran_pasaron/$gran_total pruebas"
  [ "$total_areas" -gt 0 ] && [ "$pasaron_areas" -eq "$total_areas" ]
}

case "$1" in
  compilar) compilar ;;
  editmode) pruebas EditMode ;;
  playmode) pruebas PlayMode ;;
  playmode-por-area) playmode_por_area ;;
  todo) compilar && pruebas EditMode && pruebas PlayMode ;;
  *) echo "Uso: bash herramientas/verificar.sh {compilar|editmode|playmode|playmode-por-area|todo|ensamblados}"; exit 2 ;;
esac
