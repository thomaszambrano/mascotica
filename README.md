# 🐱 Mascotica

Mascotas pixel art que caminan por la parte de abajo de tu editor mientras trabajas: **MATLAB en Windows** y **Zed en macOS**.

| Mascota | Detalle |
|---|---|
| 🐱 Gatito | Rosado pastel en Windows, naranja en Mac |
| 🟢 Grogu | Orejotas; cierra los ojos al dormir |
| 🤖 R2-D2 | La cúpula gira al caminar; luz roja si se asusta |
| ⚫ Darth Vader | Ojos rojos cuando se enoja |
| 🕷️ Spider-Man | Levanta los brazos cuando está feliz |
| 🔴 Iron Man | Prende los propulsores al saltar |
| 🟩 Dr. Doom | Magia verde en las manos |

Todas parpadean, caminan, corren, se sientan, bostezan, se estiran y a veces se duermen (zZz). **Haz clic en la mascota** y salta con un ♥.

**Contenido:** [Windows (MATLAB)](#-windows-matlab) · [macOS (Zed)](#-macos-zed) · [Para desarrolladores](#-para-desarrolladores)

---

## 🪟 Windows (MATLAB)

**Requisitos:** Windows 10 u 11. No hay que instalar nada más: usa .NET Framework 4.8, que ya viene con Windows.

### Opción A — Que aparezca cada vez que abres MATLAB (recomendada)

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

Qué hace:

1. Descarga la mascota en `C:\Users\<tu usuario>\MatlabPet\MatlabPet.exe`.
2. Agrega una línea a tu `startup.m` (el archivo que MATLAB ejecuta solo cada vez que abre) para que aparezca siempre.
3. La abre ahora mismo.

Con esta opción la mascota **solo se ve cuando MATLAB es la ventana activa** y **se cierra sola cuando cierras MATLAB**.

### Opción B — Correrla por tu cuenta, sin MATLAB

Abre **PowerShell** (tecla Windows → escribe `PowerShell` → Enter) y pega:

```powershell
$dir = "$env:USERPROFILE\MatlabPet"
New-Item -ItemType Directory -Force $dir | Out-Null
Invoke-WebRequest https://github.com/thomaszambrano/mascotica/releases/latest/download/MatlabPet.exe -OutFile "$dir\MatlabPet.exe"
Start-Process "$dir\MatlabPet.exe"
```

O descarga `MatlabPet.exe` a mano desde **[Releases](https://github.com/thomaszambrano/mascotica/releases/latest)** y hazle doble clic.

> Si Windows dice *"Windows protegió tu PC"*: **Más información → Ejecutar de todas formas**. Sale porque el archivo no está firmado digitalmente.

⚠️ Por defecto la mascota se esconde si MATLAB no está activo. Para verla en el escritorio: clic derecho en su icono junto al reloj → desmarca **Solo cuando MATLAB está activo**. Ahí caminará por la parte de abajo de la pantalla.

### Comandos de Windows

| Qué | Comando (PowerShell o CMD) |
|---|---|
| Abrir la mascota | `"%USERPROFILE%\MatlabPet\MatlabPet.exe"` |
| Abrirla atada a MATLAB (se cierra al cerrar MATLAB) | `"%USERPROFILE%\MatlabPet\MatlabPet.exe" --con-matlab` |
| Cerrarla | `taskkill /im MatlabPet.exe /f` |
| Cambiar de mascota | `"%USERPROFILE%\MatlabPet\MatlabPet.exe" mascota grogu` (acepta `"Iron Man"`, `vader`, `doom`, `r2d2`, `spiderman`, `cat`…) |
| Abrir el mini menú para elegirla con el mouse | `… MatlabPet.exe mascota` (sin nombre). También se abre solo si el nombre no existe |
| Saltar con ♥ / celebrar ★ / asustarse ! | `… MatlabPet.exe feliz` · `… exito` · `… error` |
| Dormir / despertar | `… MatlabPet.exe dormir` · `… despertar` |
| Cerrarla con calma | `… MatlabPet.exe salir` |

> En PowerShell usa `$env:USERPROFILE` en vez de `%USERPROFILE%` y pon `& ` antes de la ruta entre comillas: `& "$env:USERPROFILE\MatlabPet\MatlabPet.exe" mascota grogu`
>
> Desde MATLAB: `system(['start "" "' fullfile(getenv('USERPROFILE'),'MatlabPet','MatlabPet.exe') '" mascota grogu']);`. Si la mascota no estaba abierta, el comando la abre con esa mascota.

### Elegir mascota y menú

**Clic derecho sobre la mascota** o sobre su icono junto al reloj de Windows (puede estar dentro de la flechita **^**) abre el menú. **Clic izquierdo** = acariciar. En el menú:

- **Mascota** → elige el personaje. Se guarda para la próxima vez.
- **Elegir mascota…** → abre una ventanita con todas las mascotas para elegirla con un clic. También se abre con **doble clic** en la mascota o en su icono.
- **Solo cuando MATLAB está activo**: desmárcalo para que se vea siempre.
- **Dormir / Despertar**
- **Acariciar**
- **Iniciar con Windows**: que arranque sola al prender el PC (no hace falta si usaste la Opción A).
- **Salir**

### Actualizar

Corre otra vez el bloque de la **Opción A** (o el de PowerShell de la Opción B). Cierra la mascota, descarga la última versión y la vuelve a abrir. No duplica nada en tu `startup.m`.

### Desinstalar

1. Clic derecho en el icono → **Salir**.
2. En MATLAB: `edit(fullfile(userpath, 'startup.m'))` y borra las líneas de `% Gatita`.
3. Si activaste **Iniciar con Windows**, desmárcalo antes de salir.
4. Borra la carpeta `C:\Users\<tu usuario>\MatlabPet`.

### Problemas

- **No aparece**: haz clic dentro de la ventana de MATLAB (solo se muestra cuando MATLAB está activo), o desmarca *Solo cuando MATLAB está activo*.
- **`websave` falla**: revisa tu conexión o descárgalo a mano desde [Releases](https://github.com/thomaszambrano/mascotica/releases/latest) y sigue la Opción B.
- **Darth Vader casi no se ve**: es negro sobre MATLAB oscuro 😅. Prueba otro personaje o el tema claro.
- **Sale dos veces o no se va al cerrar MATLAB**: clic derecho → **Salir**. Revisa que la línea `% Gatita` no esté repetida en tu `startup.m`.
- **Queda flotando o tapa la barra de abajo**: avísale a quien te la regaló, es un número que se ajusta en el código 😉

---

## 🍎 macOS (Zed)

**Requisitos:** macOS 13+ y las herramientas de línea de comandos de Xcode (`xcode-select --install`).

### Instalar y abrir

```bash
git clone https://github.com/thomaszambrano/mascotica.git
cd mascotica
./macos/build.sh                  # compila ZedPet.app e instala el comando `zedpet` en ~/.local/bin
open macos/ZedPet.app
```

Aparece un 🐱 en la barra de menú de arriba con: **Mascota**, **Solo cuando Zed está activo**, **Dormir / Despertar**, **Acariciar** y **Salir**.

Para que arranque sola: **Ajustes del Sistema → General → Ítems de inicio** → agrega `macos/ZedPet.app`.

### Comandos

| Comando | Qué hace |
|---|---|
| `zedpet mascota <nombre>` | Cambia de mascota. Acepta el nombre como sea: `ironman`, `"Iron Man"`, `doom`, `"Dr. Doom"`, `vader`, `grogu`, `r2d2`, `spiderman`, `cat` |
| `zedpet run <comando>` | Corre el comando; la mascota se sienta a esperar (···), salta con ★ si termina bien y se asusta (!) si falla. Devuelve el mismo código de salida |
| `zedpet feliz` | Salta con ♥ |
| `zedpet exito` / `zedpet error` | Celebra / se asusta |
| `zedpet trabajando` | Se sienta a esperar |
| `zedpet dormir` / `zedpet despertar` | Duerme / se despierta estirándose |

Ejemplos:

```bash
zedpet mascota Iron Man
zedpet run npm test
zedpet run python3 entrenar.py
```

Además, da un saltito con ♪ de vez en cuando al **guardar archivos** mientras Zed está activo.

### Tareas en Zed

```bash
mkdir -p ~/.config/zed && cp macos/zed-tasks.json ~/.config/zed/tasks.json
```

En Zed abre la paleta (`cmd-shift-p`) → **task: spawn** y elige:

- **🐱 Correr archivo actual** (Python, JS, TS, shell o Swift según la extensión)
- **🐱 npm test**
- **🐱 npm run build**

> Si ya tenías un `tasks.json`, copia las tareas dentro del tuyo en vez de reemplazarlo.

---

## 🛠 Para desarrolladores

```
sprites/   una mascota por archivo JSON, compartidas por Mac y Windows
macos/     Swift/AppKit: main.swift, build.sh, zed-tasks.json
windows/   C# WinForms (.NET Framework 4.8): Program.cs, MatlabPet.csproj
.github/   GitHub Actions: compila ambas versiones y publica el .exe
```

### Agregar una mascota

Crea `sprites/<id>.json` con `id`, `name`, `width`, `height`, `palettes`, `blink`, `animations` y `frames`. Cada frame es una lista de filas: una letra = un pixel del color de la paleta, `.` = transparente. Animaciones que se usan: `walk`, `run`, `idle`, `sit`, `sleep`, `yawn`, `stretch`, `jump`, `happy`, `scared`. Si falta alguna, se usa la más parecida.

Mac la carga al recompilar (`./macos/build.sh`). En Windows queda embebida dentro del `.exe`.

### Compilar Windows

En un PC con Windows y el [.NET SDK 8](https://dotnet.microsoft.com/download):

```powershell
dotnet build windows/MatlabPet.csproj -c Release -o out
.\out\MatlabPet.exe
```

### Publicar una versión

```bash
git tag v1.3 && git push origin v1.3
```

GitHub Actions compila el `.exe` y lo sube a **Releases**. El bloque de MATLAB siempre descarga la última.

---

<sub>Los personajes de Star Wars y Marvel son fan art pixelado sin fines comerciales; pertenecen a sus respectivos dueños (Lucasfilm / Marvel / Disney).</sub>
