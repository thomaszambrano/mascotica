# 🐱 Mascotica

Una mascota pixel art que camina por la parte de abajo de MATLAB mientras trabajas.

**Mascotas disponibles:** 🐱 Gatita · 🟢 Grogu · 🤖 R2-D2 · ⚫ Darth Vader · 🕷️ Spider-Man · 🔴 Iron Man · 🟩 Dr. Doom

- **Elige la tuya** desde el menú (clic derecho en el icono junto al reloj → **Mascota**). Se acuerda de tu elección.
- **Haz clic en la mascota**: salta y te da un ♥
- Parpadea, se sienta, corre, bosteza, se estira y a veces se duerme (zZz)
- Solo aparece cuando MATLAB es la ventana activa; si cambias a otra app, se esconde
- Cuando cierras MATLAB, ella también se va

---

## Instalación (Windows + MATLAB) — 1 minuto

Abre MATLAB, **copia todo este bloque**, pégalo en la **Command Window** y presiona Enter:

```matlab
carpeta = fullfile(getenv('USERPROFILE'), 'MatlabPet');
if ~isfolder(carpeta), mkdir(carpeta); end
exe = fullfile(carpeta, 'MatlabPet.exe');
system('taskkill /im MatlabPet.exe /f >nul 2>&1');
websave(exe, 'https://github.com/thomaszambrano/mascotica/releases/latest/download/MatlabPet.exe');
if isempty(userpath), userpath('reset'); end
startup = fullfile(userpath, 'startup.m');
if ~isfile(startup) || ~contains(fileread(startup), 'MatlabPet')
    fid = fopen(startup, 'a');
    fprintf(fid, '\n%% Gatita\nsystem(''start "" "%s" --con-matlab'');\n', exe);
    fclose(fid);
end
system(['start "" "' exe '" --con-matlab']);
disp('🐱 Listo! Tu mascota aparecerá cada vez que abras MATLAB.')
```

Esto hace tres cosas:

1. Descarga la gatita en `C:\Users\<tu usuario>\MatlabPet\MatlabPet.exe`
2. Agrega una línea a tu `startup.m` (el archivo que MATLAB ejecuta solo cada vez que abre), para que ella aparezca siempre
3. La abre ahora mismo

**¿Hay versión nueva?** Corre el mismo bloque otra vez: cierra la mascota, descarga la última versión y la vuelve a abrir (no duplica nada en tu `startup.m`).

> No hay que instalar nada más: usa .NET Framework 4.8, que ya viene con Windows 10 y 11.

### Menú de la gatita

Al lado del reloj de Windows (puede estar dentro de la flechita **^**) hay un icono de la gatita. Con **clic derecho** puedes:

- **Mascota**: cambiar de personaje
- **Dormir / Despertar**
- **Acariciar**
- **Solo cuando MATLAB está activo**: si lo desmarcas, se queda visible siempre
- **Salir**

---

## Instalación manual (si prefieres no usar el bloque de arriba)

1. Descarga `MatlabPet.exe` desde **[Releases](https://github.com/thomaszambrano/mascotica/releases/latest)** y guárdalo en `C:\Users\<tu usuario>\MatlabPet\`.
   Si Windows dice *"Windows protegió tu PC"*: **Más información → Ejecutar de todas formas** (sale porque el archivo no está firmado).
2. En MATLAB escribe `edit(fullfile(userpath, 'startup.m'))`, pega esto al final y guarda:
   ```matlab
   % Gatita
   system('start "" "C:\Users\<tu usuario>\MatlabPet\MatlabPet.exe" --con-matlab');
   ```
3. Cierra y vuelve a abrir MATLAB.

## Desinstalar

1. Clic derecho en el icono de la gatita → **Salir**
2. En MATLAB: `edit(fullfile(userpath, 'startup.m'))` y borra las líneas de `% Gatita`
3. Borra la carpeta `C:\Users\<tu usuario>\MatlabPet`

## Problemas

- **No aparece**: haz clic dentro de la ventana de MATLAB (solo se muestra cuando MATLAB está activo).
- **Darth Vader se ve muy oscuro**: es negro sobre MATLAB oscuro 😅, prueba otro personaje o el tema claro.
- **Sale dos veces o no se va al cerrar MATLAB**: clic derecho en el icono → **Salir**. Si pasa seguido, revisa que la línea `% Gatita` no esté repetida en tu `startup.m`.
- **Queda flotando o tapa la barra de abajo**: avísale a quien te la regaló, es un número que se ajusta en el código 😉

---

<details>
<summary>Para desarrolladores: versión macOS (Zed) y cómo compilar</summary>

- `sprites/*.json`: una mascota por archivo (una letra = un pixel, `.` = transparente), compartidas por ambas versiones. Para agregar una nueva basta con crear otro JSON con las mismas animaciones (`walk`, `idle`, `sleep`, `jump`, `happy`, `scared`…); si falta alguna, se usa la más parecida.
- `windows/`: C# WinForms (.NET Framework 4.8). GitHub Actions compila el `.exe` y lo publica en Releases cuando se sube un tag `v*`.
- `macos/`: versión para el editor Zed en Swift/AppKit (gato naranja).

### macOS (Zed)

```bash
./macos/build.sh && open macos/ZedPet.app
cp macos/zed-tasks.json ~/.config/zed/tasks.json   # tareas 🐱 en Zed (task: spawn)
```

- Parpadea, mueve la cola, bosteza, se estira, respira cuando duerme.
- Da un saltito con ♪ cuando guardas archivos estando en Zed.
- Comando `zedpet` (se instala en `~/.local/bin`):
  - `zedpet run <comando>`: el gato se sienta a esperar mientras corre; ★ si termina bien, se asusta (!) si falla. Devuelve el mismo código de salida.
  - `zedpet feliz | exito | error | trabajando | guardado | dormir | despertar`
  - `zedpet mascota <cat | grogu | r2d2 | vader | spiderman | ironman | doom>` (o desde el menú 🐱 → Mascota)

</details>

---

<sub>Los personajes de Star Wars y Marvel son fan art pixelado sin fines comerciales; pertenecen a sus respectivos dueños (Lucasfilm / Marvel / Disney).</sub>
