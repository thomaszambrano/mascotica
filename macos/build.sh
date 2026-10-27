#!/bin/zsh
# Compila ZedPet.app (app de barra de menú, sin icono en el Dock)
set -e
cd "$(dirname "$0")"
APP=ZedPet.app
rm -rf $APP
mkdir -p $APP/Contents/MacOS
swiftc -O -swift-version 5 main.swift -o $APP/Contents/MacOS/ZedPet
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
echo "Listo: $(pwd)/$APP  →  open $APP"
