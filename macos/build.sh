#!/bin/zsh
# Compila ZedPet.app (app de barra de menú, sin icono en el Dock) e instala el comando `zedpet`
set -e
cd "$(dirname "$0")"
APP=ZedPet.app
rm -rf $APP
mkdir -p $APP/Contents/MacOS $APP/Contents/Resources
swiftc -O -swift-version 5 main.swift -o $APP/Contents/MacOS/ZedPet
cp ../sprites/cat.json $APP/Contents/Resources/
cat > $APP/Contents/Info.plist <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>ZedPet</string>
  <key>CFBundleIdentifier</key><string>local.zedpet</string>
  <key>CFBundleExecutable</key><string>ZedPet</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSUIElement</key><true/>
</dict></plist>
EOF
codesign --force -s - $APP >/dev/null 2>&1 || true

# En CI no hay ~/.local/bin en el PATH: solo compila
if [[ -z "$CI" ]]; then
  mkdir -p ~/.local/bin
  ln -sf "$(pwd)/$APP/Contents/MacOS/ZedPet" ~/.local/bin/zedpet
  echo "Comando instalado: zedpet  (~/.local/bin/zedpet)"
fi
echo "Listo: $(pwd)/$APP  →  open $(pwd)/$APP"
