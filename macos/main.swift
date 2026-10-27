import AppKit
import CoreServices

// MARK: - Config

let scale: CGFloat = 3
let statusBarOffset: CGFloat = 26 // altura aprox. de la barra de estado de Zed: el gato camina encima
let zedBundleIDs: Set<String> = ["dev.zed.Zed", "dev.zed.Zed-Preview"]
let paletteName = "naranja"
let eventName = Notification.Name("local.zedpet.event")

// MARK: - CLI: `zedpet <evento>` o `zedpet run <comando...>`

func send(_ event: String) {
    DistributedNotificationCenter.default().postNotificationName(eventName, object: event, userInfo: nil, deliverImmediately: true)
}

let cliArgs = Array(CommandLine.arguments.dropFirst())
if let first = cliArgs.first {
    guard first == "run" else { send(first); exit(0) }
    let command = Array(cliArgs.dropFirst().drop { $0 == "--" })
    guard !command.isEmpty else { fputs("uso: zedpet run <comando...>\n", stderr); exit(2) }
    let p = Process()
    p.executableURL = URL(fileURLWithPath: "/usr/bin/env")
    p.arguments = command
    send("trabajando")
    do { try p.run() } catch { send("error"); fputs("zedpet: \(error.localizedDescription)\n", stderr); exit(127) }
    p.waitUntilExit()
    send(p.terminationStatus == 0 ? "exito" : "error")
    exit(p.terminationStatus)
}

// MARK: - Sprites (compartidos con Windows en sprites/cat.json)

struct SpriteSheet: Decodable {
    struct Anim: Decodable { let frames: [String]; let ticksPerFrame: Int }
    let width: Int
    let height: Int
    let palettes: [String: [String: String]]
    let blink: [String: String]
    let animations: [String: Anim]
    let frames: [String: [String]]

    static func load() -> SpriteSheet {
        let exe = URL(fileURLWithPath: CommandLine.arguments[0]).resolvingSymlinksInPath()
        let candidates = [Bundle.main.url(forResource: "cat", withExtension: "json"),
                          exe.deletingLastPathComponent().appendingPathComponent("../Resources/cat.json")]
        for case let url? in candidates {
            if let data = try? Data(contentsOf: url), let sheet = try? JSONDecoder().decode(SpriteSheet.self, from: data) { return sheet }
        }
        fatalError("No encontré sprites/cat.json")
    }
}

extension NSColor {
    convenience init(hex: String) {
        let v = Int(hex.dropFirst(), radix: 16) ?? 0
        self.init(red: CGFloat(v >> 16 & 0xFF) / 255, green: CGFloat(v >> 8 & 0xFF) / 255, blue: CGFloat(v & 0xFF) / 255, alpha: 1)
    }
}

let sheet = SpriteSheet.load()
let palette = (sheet.palettes[paletteName] ?? [:]).reduce(into: [Character: NSColor]()) { $0[Character($1.key)] = NSColor(hex: $1.value) }
let blinkMap = sheet.blink.reduce(into: [Character: Character]()) { $0[Character($1.key)] = Character($1.value) }
let spriteW = CGFloat(sheet.width) * scale
let spriteH = CGFloat(sheet.height) * scale
let extraTop: CGFloat = 30 // espacio para burbujas

// MARK: - Vista

final class PetView: NSView {
    var frameName = "walk1"
    var facingRight = true
    var blinking = false
    var bubble: String?
    var bubbleColor = NSColor.systemPink
    var onClick: (() -> Void)?

    override var isFlipped: Bool { true }

    override func draw(_ dirtyRect: NSRect) {
        guard let rows = sheet.frames[frameName] else { return }
        for (y, row) in rows.enumerated() {
            for (x, raw) in row.enumerated() {
                let ch = blinking ? blinkMap[raw] ?? raw : raw
                guard let color = palette[ch] else { continue }
                color.setFill()
                let px = facingRight ? CGFloat(x) : CGFloat(row.count - 1 - x)
                NSRect(x: px * scale, y: extraTop + CGFloat(y) * scale, width: scale, height: scale).fill()
            }
        }
        if let bubble {
            let attrs: [NSAttributedString.Key: Any] = [.font: NSFont.boldSystemFont(ofSize: 14), .foregroundColor: bubbleColor]
            (bubble as NSString).draw(at: NSPoint(x: facingRight ? spriteW - 26 : 4, y: 4), withAttributes: attrs)
        }
    }

    override func mouseDown(with event: NSEvent) { onClick?() }
}

// MARK: - Detecta guardados mientras Zed está activo

final class SaveWatcher {
    let onSave: () -> Void
    var stream: FSEventStreamRef?
    static let ignored = ["/Library/", "/.", "node_modules", "/target/", "/build/", "DerivedData", "__pycache__", "/out/", "/obj/", "/bin/"]

    init(onSave: @escaping () -> Void) {
        self.onSave = onSave
        var ctx = FSEventStreamContext(version: 0, info: Unmanaged.passUnretained(self).toOpaque(), retain: nil, release: nil, copyDescription: nil)
        let callback: FSEventStreamCallback = { _, info, count, paths, flags, _ in
            let me = Unmanaged<SaveWatcher>.fromOpaque(info!).takeUnretainedValue()
            let list = Unmanaged<CFArray>.fromOpaque(paths).takeUnretainedValue() as! [String]
            let wanted = FSEventStreamEventFlags(kFSEventStreamEventFlagItemModified | kFSEventStreamEventFlagItemRenamed | kFSEventStreamEventFlagItemCreated)
            let isFile = FSEventStreamEventFlags(kFSEventStreamEventFlagItemIsFile)
            let hit = (0..<count).contains { i in
                flags[i] & wanted != 0 && flags[i] & isFile != 0 && !SaveWatcher.ignored.contains { list[i].contains($0) }
            }
            if hit { me.onSave() }
        }
        let flags = kFSEventStreamCreateFlagFileEvents | kFSEventStreamCreateFlagUseCFTypes | kFSEventStreamCreateFlagNoDefer
        stream = FSEventStreamCreate(nil, callback, &ctx, [NSHomeDirectory()] as CFArray,
                                     FSEventStreamEventId(kFSEventStreamEventIdSinceNow), 0.3, FSEventStreamCreateFlags(flags))
        if let stream {
            FSEventStreamSetDispatchQueue(stream, .main)
            FSEventStreamStart(stream)
        }
    }
}

// MARK: - Pet

enum Mood { case walking, idle, sleeping, working, oneShot, airborne }

final class Pet {
    let window: NSPanel
    let view = PetView()
    var watcher: SaveWatcher?
    var x: CGFloat = 200
    var jumpY: CGFloat = 0
    var vy: CGFloat = 0
    var vx: CGFloat = 0
    var speed: CGFloat = 1.6
    var mood = Mood.walking
    var moodTicks = 0
    var moodLength = 150
    var anim = "walk"
    var animStart = 0
    var next = Mood.walking
    var blinkTicks = 0
    var tick = 0
    var lastSave = Date.distantPast
    var onlyWithZed = true
    var forcedSleep = false

    init() {
        window = NSPanel(contentRect: NSRect(x: 0, y: 0, width: spriteW, height: spriteH + extraTop),
                         styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        window.isOpaque = false
        window.backgroundColor = .clear
        window.hasShadow = false
        window.level = .floating
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary]
        window.contentView = view
        view.onClick = { [weak self] in self?.handle("feliz") }
        DistributedNotificationCenter.default().addObserver(forName: eventName, object: nil, queue: .main) { [weak self] note in
            (note.object as? String).map { self?.handle($0) }
        }
        watcher = SaveWatcher { [weak self] in self?.saved() }
        Timer.scheduledTimer(withTimeInterval: 1 / 30, repeats: true) { [weak self] _ in self?.update() }
    }

    // MARK: Eventos

    func handle(_ event: String) {
        switch event {
        case "feliz", "acariciar": forcedSleep = false; jump(vy: 7, anim: "happy", bubble: "♥", color: .systemPink)
        case "exito", "ok": forcedSleep = false; jump(vy: 8, anim: "happy", bubble: "★", color: .systemYellow)
        case "error", "fallo":
            forcedSleep = false
            jump(vy: 5, anim: "scared", bubble: "!", color: .systemRed, vx: view.facingRight ? -2 : 2, then: .idle)
        case "trabajando": forcedSleep = false; set(.working, length: 30 * 600)
        case "dormir": forcedSleep = true
        case "despertar": forcedSleep = false; play("stretch", ticks: 45, then: .walking)
        case "guardado": saved(force: true)
        default: break
        }
    }

    func saved(force: Bool = false) {
        guard force || zedIsActive, mood != .sleeping, mood != .airborne, Date().timeIntervalSince(lastSave) > 2 else { return }
        lastSave = Date()
        if force || Int.random(in: 0..<3) == 0 { jump(vy: 3.5, anim: "jump", bubble: "♪", color: .systemTeal) }
    }

    func jump(vy: CGFloat, anim: String, bubble: String, color: NSColor, vx: CGFloat = 0, then: Mood = .walking) {
        self.vy = vy
        self.vx = vx
        self.anim = anim
        next = then
        say(bubble, color, ticks: 40)
        set(.airborne, length: .max)
    }

    func play(_ anim: String, ticks: Int, then: Mood) {
        self.anim = anim
        next = then
        set(.oneShot, length: ticks)
    }

    var bubbleTicks = 0
    func say(_ text: String?, _ color: NSColor = .systemPink, ticks: Int = 40) {
        view.bubble = text
        view.bubbleColor = color
        bubbleTicks = ticks
    }

    func set(_ m: Mood, length: Int) {
        mood = m
        moodTicks = 0
        moodLength = length
        animStart = tick
        switch m {
        case .walking:
            let running = Int.random(in: 0..<5) == 0
            speed = running ? 3.5 : 1.6
            anim = running ? "run" : "walk"
            if Bool.random() { view.facingRight.toggle() }
        case .idle: anim = "idle"
        case .sleeping: anim = "sleep"
        case .working: anim = "sit"
        case .oneShot, .airborne: break
        }
    }

    // MARK: Ventana de Zed

    var zedIsActive: Bool { zedBundleIDs.contains(NSWorkspace.shared.frontmostApplication?.bundleIdentifier ?? "") }

    /// Frame de la ventana de Zed en coordenadas de Cocoa (origen abajo-izquierda).
    func zedFrame() -> NSRect? {
        guard let pid = NSWorkspace.shared.runningApplications.first(where: { zedBundleIDs.contains($0.bundleIdentifier ?? "") })?.processIdentifier,
              let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]],
              let info = list.first(where: { ($0[kCGWindowOwnerPID as String] as? pid_t) == pid && ($0[kCGWindowLayer as String] as? Int) == 0 }),
              let dict = info[kCGWindowBounds as String] as CFTypeRef?,
              let b = CGRect(dictionaryRepresentation: dict as! CFDictionary),
              b.width > 200
        else { return nil }
        let mainH = NSScreen.screens.first?.frame.height ?? 0
        return NSRect(x: b.minX, y: mainH - b.maxY, width: b.width, height: b.height)
    }

    // MARK: Loop

    func update() {
        tick += 1
        let zed = zedFrame()
        // Mientras trabaja un comando de `zedpet run` se queda visible aunque estés en otra app
        if onlyWithZed && (zed == nil || !(zedIsActive || mood == .working)) {
            window.orderOut(nil)
            return
        }
        let area = zed.map { NSRect(x: $0.minX, y: $0.minY + statusBarOffset, width: $0.width, height: $0.height) }
            ?? NSScreen.main?.visibleFrame ?? .zero

        think(area)
        x = min(max(x, area.minX), area.maxX - spriteW)
        window.setFrameOrigin(NSPoint(x: x, y: area.minY + jumpY))
        if !window.isVisible { window.orderFrontRegardless() }
        render()
    }

    func render() {
        if let a = sheet.animations[anim] {
            view.frameName = a.frames[((tick - animStart) / max(a.ticksPerFrame, 1)) % a.frames.count]
        }
        if blinkTicks > 0 { blinkTicks -= 1 } else if mood != .sleeping, Int.random(in: 0..<120) == 0 { blinkTicks = 5 }
        view.blinking = blinkTicks > 0
        if bubbleTicks > 0 { bubbleTicks -= 1; if bubbleTicks == 0 { view.bubble = nil } }
        view.needsDisplay = true
    }

    func think(_ area: NSRect) {
        moodTicks += 1
        if forcedSleep, mood != .sleeping, mood != .airborne { set(.sleeping, length: .max) }

        switch mood {
        case .walking:
            x += view.facingRight ? speed : -speed
            if x <= area.minX || x >= area.maxX - spriteW { view.facingRight.toggle() }
            if moodTicks > moodLength {
                switch Int.random(in: 0..<20) {
                case 0..<10: set(.idle, length: 30 * Int.random(in: 3...6))
                case 10..<13: set(.sleeping, length: 30 * Int.random(in: 10...20))
                case 13..<16: play("yawn", ticks: 40, then: .idle)
                case 16..<18: play("stretch", ticks: 45, then: .walking)
                default: set(.walking, length: 30 * Int.random(in: 4...10))
                }
            }
        case .idle:
            if moodTicks > moodLength { Int.random(in: 0..<4) == 0 ? play("yawn", ticks: 40, then: .walking) : set(.walking, length: 30 * Int.random(in: 4...10)) }
        case .sleeping:
            say(["z", "zZ", "zZz"][(tick / 20) % 3], .secondaryLabelColor, ticks: 2)
            if !forcedSleep && moodTicks > moodLength { play("stretch", ticks: 45, then: .walking) }
        case .working:
            say(["·", "··", "···"][(tick / 12) % 3], .secondaryLabelColor, ticks: 2)
            if moodTicks > moodLength { set(.idle, length: 90) }
        case .oneShot:
            if moodTicks > moodLength { set(next, length: next == .idle ? 90 : 30 * Int.random(in: 4...10)) }
        case .airborne:
            jumpY += vy
            x += vx
            vy -= 0.6
            if jumpY <= 0 {
                jumpY = 0
                vx = 0
                next == .idle ? play("sit", ticks: 60, then: .walking) : set(next, length: 30 * Int.random(in: 4...10))
            }
        }
    }
}

// MARK: - App

final class AppDelegate: NSObject, NSApplicationDelegate {
    var pet: Pet!
    var statusItem: NSStatusItem!

    func applicationDidFinishLaunching(_ notification: Notification) {
        pet = Pet()
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.title = "🐱"
        let menu = NSMenu()
        menu.addItem(item("Solo cuando Zed está activo", #selector(toggleZed), state: true))
        menu.addItem(item("Dormir / Despertar", #selector(toggleSleep)))
        menu.addItem(item("Acariciar", #selector(petIt)))
        menu.addItem(.separator())
        menu.addItem(item("Salir", #selector(quit), key: "q"))
        statusItem.menu = menu
    }

    func item(_ title: String, _ action: Selector, key: String = "", state: Bool = false) -> NSMenuItem {
        let i = NSMenuItem(title: title, action: action, keyEquivalent: key)
        i.target = self
        i.state = state ? .on : .off
        return i
    }

    @objc func toggleZed(_ sender: NSMenuItem) {
        pet.onlyWithZed.toggle()
        sender.state = pet.onlyWithZed ? .on : .off
    }
    @objc func toggleSleep() { pet.handle(pet.forcedSleep ? "despertar" : "dormir") }
    @objc func petIt() { pet.handle("feliz") }
    @objc func quit() { NSApp.terminate(nil) }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.accessory) // sin icono en el Dock
app.run()
