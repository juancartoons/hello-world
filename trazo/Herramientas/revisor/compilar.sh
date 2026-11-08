#!/bin/bash
# REVISOR DE TIPOS de los scripts de TrazoVR, sin Unity (para las sesiones en la nube).
# Usa las referencias de Unity 2021.3 (NuGet "UnityEngine.Modules") + simulacros de TextMeshPro y del Meta XR SDK.
# Uso: bash trazo/Herramientas/revisor/compilar.sh   → limpio = solo las dos líneas "== -define:...".
# (No ve las APIs nuevas de Unity 6 que no existen en 2021.3; esas se cambian por simulacros abajo.)
AQUI=$(cd "$(dirname "$0")" && pwd)
S=$AQUI/../../Assets/Scripts
W=${TMPDIR:-/tmp}/revisor-trazo; mkdir -p $W
which mcs >/dev/null || apt-get install -y mono-mcs >/dev/null 2>&1
if [ ! -f $W/um/lib/net45/UnityEngine.CoreModule.dll ]; then
  curl -sS -o $W/um.nupkg "https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg"
  python3 -c "import zipfile,sys;zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" $W/um.nupkg $W/um
fi
T=$W/src; rm -rf $T $W/stubs; mkdir -p $T $W/stubs
for f in $AQUI/stubs/*.cs.txt; do cp "$f" $W/stubs/$(basename "$f" .txt); done
for f in $S/*.cs; do sed -e 's/new RenderPipeline\.StandardRequest/new RPStub.RenderPipeline.StandardRequest/; s/RenderPipeline\.SupportsRenderRequest/RPStub.RenderPipeline.SupportsRenderRequest/; s/RenderPipeline\.SubmitRenderRequest/RPStub.RenderPipeline.SubmitRenderRequest/' "$f" > $T/$(basename "$f"); done
REFS=$(ls $W/um/lib/net45/*.dll | sed 's/^/-r:/' | tr '\n' ' ')
for DEF in "UNITY_EDITOR" "UNITY_ANDROID"; do
  echo "== -define:$DEF"
  mcs -target:library -define:$DEF -nowarn:0414,0649,0169,0618,0612,0108,0114 -out:$W/t.dll $REFS -r:System.Core -r:System.Xml $W/stubs/*.cs $T/*.cs 2>&1 | grep -E "error|warning CS0(16[2-9]|219|1717)" | head -40
done
