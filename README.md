# 🐱 Mascotica

Un gatito pixel art que camina por el borde inferior de tu editor.

| Versión | Editor | Carpeta |
|---|---|---|
| Windows | MATLAB | [`windows/`](windows) |
| macOS | Zed | [`macos/`](macos) |

## Windows (MATLAB)

1. Ve a **[Releases](../../releases/latest)** y descarga `MatlabPet.exe`.
2. Haz doble clic. Si Windows muestra *"Windows protegió tu PC"*: **Más información → Ejecutar de todas formas** (sale porque el .exe no está firmado).
3. Abre MATLAB: el gato aparece caminando abajo de la ventana.

No necesita instalar nada (usa .NET Framework 4.8, que ya viene con Windows 10/11).

- **Clic al gato** → salta y te da un ♥
- **Icono del gato junto al reloj** (clic derecho): dormir/despertar, mostrar siempre, iniciar con Windows, salir.

## macOS (Zed)

```bash
./macos/build.sh && open macos/ZedPet.app
```

Menú 🐱 en la barra superior para opciones.
