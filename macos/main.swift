import AppKit

// MARK: - Config

let scale: CGFloat = 3
let statusBarOffset: CGFloat = 26 // altura aprox. de la barra de estado de Zed: el gato camina encima
let zedBundleIDs: Set<String> = ["dev.zed.Zed", "dev.zed.Zed-Preview"]

let palette: [Character: NSColor] = [
    "k": NSColor(red: 0.16, green: 0.12, blue: 0.10, alpha: 1),
    "o": NSColor(red: 0.95, green: 0.60, blue: 0.25, alpha: 1),
    "w": NSColor(red: 1.00, green: 0.96, blue: 0.90, alpha: 1),
    "p": NSColor(red: 0.98, green: 0.55, blue: 0.65, alpha: 1),
    "e": NSColor(red: 0.10, green: 0.45, blue: 0.30, alpha: 1),
]

// Gato mirando a la derecha, 20x12
let body = [
    "..............k...k.",
    ".............kok.kok",
    ".k...........koooook",
    ".k...........kooeoek",
    "..k..........kooowpk",
    "..k..kkkkkkkkkoooook",
    "...kkooooooooooooook",
    "....koooooooooooook.",
    "....kowwwwwwwwwwok..",
    "....kkkkkkkkkkkkk...",
]
let legsA = ["....kk...kk..kk..kk.", "....kk...kk..kk..kk."]
let legsB = [".....kk.kk....kk.kk.", "......kkk......kkk.."]
let tucked = ["....................", "...................."]

let frames: [String: [String]] = [
    "walk1": body + legsA,
    "walk2": body + legsB,
    "sit": body + legsA,
    "sleep": tucked + body.map { $0.replacingOccurrences(of: "e", with: "k") },
]

let spriteW = CGFloat(body[0].count) * scale
let spriteH = CGFloat(frames["walk1"]!.count) * scale
let extraTop: CGFloat = 30 // espacio para "zZz" y corazones

// MARK: - Vista

final class PetView: NSView {
    var frameName = "walk1"
    var facingRight = true
    var bubble: String?
    var onClick: (() -> Void)?

    override var isFlipped: Bool { true }

    override func draw(_ dirtyRect: NSRect) {
        guard let rows = frames[frameName] else { return }
        for (y, row) in rows.enumerated() {
            for (x, ch) in row.enumerated() {
                guard let color = palette[ch] else { continue }
                color.setFill()
                let px = facingRight ? CGFloat(x) : CGFloat(row.count - 1 - x)
                NSRect(x: px * scale, y: extraTop + CGFloat(y) * scale, width: scale, height: scale).fill()
            }
        }
        if let bubble {
            let attrs: [NSAttributedString.Key: Any] = [.font: NSFont.boldSystemFont(ofSize: 14), .foregroundColor: NSColor.systemPink]
            (bubble as NSString).draw(at: NSPoint(x: facingRight ? spriteW - 24 : 4, y: 4), withAttributes: attrs)
        }
    }

    override func mouseDown(with event: NSEvent) { onClick?() }
}

// MARK: - Pet

enum Mood { case walking, sitting, sleeping, jumping }

final class Pet {
    let window: NSPanel
    let view = PetView()
    var x: CGFloat = 200
    var jumpY: CGFloat = 0
    var vy: CGFloat = 0
    var speed: CGFloat = 1.6
    var mood = Mood.walking
    var moodTicks = 0
    var tick = 0
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
        view.onClick = { [weak self] in self?.pet() }
        Timer.scheduledTimer(withTimeInterval: 1 / 30, repeats: true) { [weak self] _ in self?.update() }
    }

    func pet() {
        forcedSleep = false
        mood = .jumping
        vy = 7
        view.bubble = "♥"
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.2) { [weak self] in
            if self?.view.bubble == "♥" { self?.view.bubble = nil }
        }
    }

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

    func update() {
        tick += 1
        let zedActive = zedBundleIDs.contains(NSWorkspace.shared.frontmostApplication?.bundleIdentifier ?? "")
        let area: NSRect
        if onlyWithZed {
            guard zedActive, let f = zedFrame() else { window.orderOut(nil); return }
            area = NSRect(x: f.minX, y: f.minY + statusBarOffset, width: f.width, height: f.height)
        } else {
            area = zedFrame().map { NSRect(x: $0.minX, y: $0.minY + statusBarOffset, width: $0.width, height: $0.height) }
                ?? NSScreen.main?.visibleFrame ?? .zero
        }

        think(area)
        x = min(max(x, area.minX), area.maxX - spriteW)
        window.setFrameOrigin(NSPoint(x: x, y: area.minY + jumpY))
        if !window.isVisible { window.orderFrontRegardless() }
        view.needsDisplay = true
    }

    func think(_ area: NSRect) {
        moodTicks += 1
        if forcedSleep && mood != .jumping { mood = .sleeping }

        switch mood {
        case .walking:
            x += view.facingRight ? speed : -speed
            if x <= area.minX || x >= area.maxX - spriteW { view.facingRight.toggle() }
            view.frameName = (tick / 6) % 2 == 0 ? "walk1" : "walk2"
            if moodTicks > 30 * Int.random(in: 4...10) { change(Int.random(in: 0...3) == 0 ? .sleeping : .sitting) }
        case .sitting:
            view.frameName = "sit"
            if moodTicks > 30 * 3 { change(.walking) }
        case .sleeping:
            view.frameName = "sleep"
            view.bubble = ["z", "zZ", "zZz"][(tick / 20) % 3]
            if !forcedSleep && moodTicks > 30 * 12 { view.bubble = nil; change(.walking) }
        case .jumping:
            jumpY += vy
            vy -= 0.6
            if jumpY <= 0 { jumpY = 0; change(.walking) }
        }
    }

    func change(_ m: Mood) {
        mood = m
        moodTicks = 0
        if m == .walking { speed = Int.random(in: 0...4) == 0 ? 3.5 : 1.6; if Bool.random() { view.facingRight.toggle() } }
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
    @objc func toggleSleep() {
        pet.forcedSleep.toggle()
        if !pet.forcedSleep { pet.view.bubble = nil; pet.change(.walking) }
    }
    @objc func petIt() { pet.pet() }
    @objc func quit() { NSApp.terminate(nil) }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.accessory) // sin icono en el Dock
app.run()
