# 🐱 Mascotica

Una mascota pixel art que camina por la parte de abajo de tu editor mientras trabajas.

- **Windows** → acompaña a **MATLAB**
- **Mac** → acompaña a **Zed**

Hay 7 para elegir: 🐱 Gatito · 🟢 Grogu · 🤖 R2-D2 · ⚫ Darth Vader · 🕷️ Spider-Man · 🔴 Iron Man · 🟩 Dr. Doom

Caminan, corren, bostezan, se duermen (zZz)… y si les haces clic, saltan con un ♥.

**Ir a:** [Windows](#-windows-matlab) · [Mac](#-mac-zed) · [Configurarla](#-configúrala-a-tu-gusto) · [Problemas](#-si-algo-no-funciona) · [Para programadores](#-para-programadores)

---

## 🪟 Windows (MATLAB)

### 1. Instalar (una sola vez)

1. Abre **MATLAB**.
2. Copia este bloque completo, pégalo abajo en la **Command Window** y presiona **Enter**:

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

3. ¡Listo! La mascota aparece abajo en MATLAB. Desde ahora sale **sola cada vez que abras MATLAB** y se va cuando lo cierras.

> 💡 **Para actualizarla** a la última versión, vuelve a pegar el mismo bloque. No duplica nada.

### 2. Usarla con el mouse

| Haz esto | Y pasa esto |
|---|---|
| **Clic** en la mascota | Salta con un ♥ |
| **Doble clic** en la mascota | Se abre una ventanita para **elegir otra mascota** |
| **Clic derecho** en la mascota | Se abre el menú con todas las opciones |

También tiene un icono al lado del reloj de Windows (a veces escondido en la flechita **^**) con el mismo menú.

**Opciones del menú:**

- **Mascota** / **Elegir mascota…** → cambia de personaje (se recuerda para la próxima vez)
- **Solo cuando MATLAB está activo** → quítale el ✓ si quieres verla también en el escritorio
- **Dormir / Despertar** · **Acariciar**
- **Iniciar con Windows** → que se abra sola al prender el PC
- **Salir**

### 3. Usarla con comandos (opcional)

Todo lo anterior también se puede hacer escribiendo. Abre **PowerShell** (tecla Windows → escribe `PowerShell` → Enter), pega esta línea primero:

```powershell
$mascota = "$env:USERPROFILE\MatlabPet\MatlabPet.exe"
```

y después usa los comandos que quieras:

| Escribe | Qué hace |
|---|---|
| `& $mascota mascota grogu` | Cambia a Grogu |
| `& $mascota mascota` | Abre la ventanita para elegir con el mouse |
| `& $mascota feliz` | Salta con ♥ |
| `& $mascota exito` | Celebra con ★ |
| `& $mascota error` | Se asusta con ! |
| `& $mascota dormir` / `& $mascota despertar` | Se duerme / se despierta |
| `& $mascota salir` | La cierra |

**Nombres que entiende:** `cat`, `grogu`, `r2d2`, `vader`, `spiderman`, `ironman`, `doom`. Da igual si usas mayúsculas o espacios (`"Iron Man"` también sirve). Si escribes un nombre que no conoce, abre la ventanita para que la elijas tú.

<details>
<summary>Lo mismo pero desde MATLAB</summary>

```matlab
mascota = fullfile(getenv('USERPROFILE'), 'MatlabPet', 'MatlabPet.exe');
system(['start "" "' mascota '" mascota grogu']);
```

Cambia `mascota grogu` por cualquier comando de la tabla (`feliz`, `dormir`…).
</details>

### Desinstalar

1. Clic derecho en la mascota → **Salir** (si activaste *Iniciar con Windows*, quítale el ✓ antes).
2. En MATLAB escribe `edit(fullfile(userpath, 'startup.m'))` y borra las líneas que dicen `% Gatita`.
3. Borra la carpeta `C:\Users\<tu usuario>\MatlabPet`.

<details>
<summary>¿Sin MATLAB? Instalarla solo con PowerShell</summary>

```powershell
$dir = "$env:USERPROFILE\MatlabPet"
New-Item -ItemType Directory -Force $dir | Out-Null
Invoke-WebRequest https://github.com/thomaszambrano/mascotica/releases/latest/download/MatlabPet.exe -OutFile "$dir\MatlabPet.exe"
Start-Process "$dir\MatlabPet.exe"
```

O descarga `MatlabPet.exe` desde **[Releases](https://github.com/thomaszambrano/mascotica/releases/latest)** y hazle doble clic. Sin MATLAB abierto se esconde: clic derecho en su icono junto al reloj → quita el ✓ de **Solo cuando MATLAB está activo**.
</details>

---

## 🍎 Mac (Zed)

### 1. Instalar (una sola vez)

Necesitas las herramientas de Xcode. Si no las tienes, abre **Terminal** y escribe `xcode-select --install`.

Luego, en la Terminal:

```bash
git clone https://github.com/thomaszambrano/mascotica.git
cd mascotica
./macos/build.sh
open macos/ZedPet.app
```

La mascota aparece abajo en Zed, y un 🐱 arriba en la barra de menú.

> 💡 **Para que se abra sola** al prender el Mac: **Ajustes del Sistema → General → Ítems de inicio** → agrega `macos/ZedPet.app`. Si no, ábrela con `open macos/ZedPet.app` cada vez.

### 2. Usarla con el mouse

- **Clic** en la mascota → salta con ♥
- **Clic en el 🐱 de la barra de arriba** → menú: **Mascota**, **Solo cuando Zed está activo**, **Dormir / Despertar**, **Acariciar**, **Salir**

### 3. Usarla con comandos (opcional)

En la Terminal:

| Escribe | Qué hace |
|---|---|
| `zedpet mascota grogu` | Cambia a Grogu (sirven los mismos nombres que en Windows) |
| `zedpet feliz` | Salta con ♥ |
| `zedpet exito` / `zedpet error` | Celebra ★ / se asusta ! |
| `zedpet dormir` / `zedpet despertar` | Se duerme / se despierta |
| `zedpet run npm test` | Corre tu comando: la mascota espera sentada, celebra si sale bien y se asusta si falla |

Además, da un saltito ♪ de vez en cuando cuando **guardas un archivo** en Zed.

<details>
<summary>Extra: tareas dentro de Zed</summary>

```bash
mkdir -p ~/.config/zed && cp macos/zed-tasks.json ~/.config/zed/tasks.json
```

En Zed: `cmd-shift-p` → **task: spawn** → elige **🐱 Correr archivo actual**, **🐱 npm test** o **🐱 npm run build**. La mascota reacciona al resultado.

> Si ya tenías un `tasks.json`, copia las tareas dentro del tuyo en vez de reemplazarlo.
</details>

---

## 🎨 Configúrala a tu gusto

Todo se guarda solo: la próxima vez que la abras sigue como la dejaste.

| Quiero… | Windows | Mac |
|---|---|---|
| **Otra mascota** | Doble clic en la mascota y elige, o `& $mascota mascota vader` | 🐱 de arriba → **Mascota**, o `zedpet mascota vader` |
| **Verla siempre**, no solo con el editor | Clic derecho → quita el ✓ de **Solo cuando MATLAB está activo** | 🐱 de arriba → quita el ✓ de **Solo cuando Zed está activo** |
| **Que se abra sola** al prender el computador | Clic derecho → **Iniciar con Windows** (con la instalación de MATLAB ya sale sola al abrir MATLAB) | **Ajustes del Sistema → General → Ítems de inicio** → agrega `ZedPet.app` |
| **Que no moleste** un rato | Clic derecho → **Dormir** (o `& $mascota dormir`) | 🐱 de arriba → **Dormir** (o `zedpet dormir`) |
| **Cerrarla** | Clic derecho → **Salir** | 🐱 de arriba → **Salir** |

---

## 🆘 Si algo no funciona

| Problema | Solución |
|---|---|
| **No la veo** | Haz clic dentro de la ventana de MATLAB / Zed: solo se muestra cuando el editor está activo. O quita el ✓ de **Solo cuando … está activo** en el menú |
| **(Mac) No la veo ni en la barra de arriba** | No está abierta: `open macos/ZedPet.app` |
| **(Windows) "Windows protegió tu PC"** | **Más información → Ejecutar de todas formas**. Sale porque el archivo no está firmado |
| **(Windows) `websave` falla** | Revisa tu internet, o instálala con PowerShell (ver *¿Sin MATLAB?* arriba) |
| **No me deja cambiar de mascota** | Doble clic en la mascota, o el comando `mascota` sin nombre: abre la ventanita para elegir |
| **Darth Vader casi no se ve** | Es negro sobre fondo oscuro 😅. Prueba otra mascota o el tema claro |
| **Sale dos veces** | Clic derecho → **Salir**, y revisa que `% Gatita` no esté repetido en tu `startup.m` |

---

## 🛠 Para programadores

```
sprites/   una mascota por archivo JSON (las usan Mac y Windows)
macos/     app de Mac en Swift: main.swift, build.sh, zed-tasks.json
windows/   app de Windows en C# (.NET Framework 4.8): Program.cs, MatlabPet.csproj
.github/   GitHub Actions: compila las dos y publica el .exe
```

**Agregar una mascota:** crea `sprites/<id>.json` con `id`, `name`, `width`, `height`, `palettes`, `blink`, `animations` y `frames`. Cada frame es una lista de filas: cada letra es un pixel del color de la paleta y `.` es transparente. Animaciones: `walk`, `run`, `idle`, `sit`, `sleep`, `yawn`, `stretch`, `jump`, `happy`, `scared` (si falta alguna, usa la más parecida). En Mac recompila con `./macos/build.sh`; en Windows va dentro del `.exe`.

**Compilar Windows** (en un PC con el [.NET SDK 8](https://dotnet.microsoft.com/download)):

```powershell
dotnet build windows/MatlabPet.csproj -c Release -o out
.\out\MatlabPet.exe
```

**Publicar una versión nueva:** `git tag v1.5 && git push origin v1.5`. GitHub Actions compila el `.exe` y lo sube a **Releases**; la instalación siempre baja la última.

---

<sub>Los personajes de Star Wars y Marvel son fan art pixelado sin fines comerciales; pertenecen a sus respectivos dueños (Lucasfilm / Marvel / Disney).</sub>
