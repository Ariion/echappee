#!/usr/bin/env bash
# Copie le code C# pur (src/Echappee.Core) et les données (data/*.json) dans le projet Unity.
# À relancer après chaque modification du cœur ou de l'équilibrage. Les dossiers générés ne sont pas versionnés.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
core="$root/unity/Assets/Scripts/Core"
data="$root/unity/Assets/Resources/Data"
rm -rf "$core" "$data"
mkdir -p "$core" "$data"
( cd "$root/src/Echappee.Core" && find . -name '*.cs' -not -path './obj/*' -not -path './bin/*' -print0 | xargs -0 -I{} cp --parents {} "$core" )
cp "$root"/data/*.json "$data"/
cat > "$core/Echappee.Core.asmdef" <<'JSON'
{
  "name": "Echappee.Core",
  "rootNamespace": "Echappee",
  "references": ["Unity.Nuget.Newtonsoft-Json"],
  "autoReferenced": true,
  "noEngineReferences": true
}
JSON
echo "OK : $(find "$core" -name '*.cs' | wc -l) fichiers C# et $(ls "$data" | wc -l) fichiers de données copiés dans unity/."
