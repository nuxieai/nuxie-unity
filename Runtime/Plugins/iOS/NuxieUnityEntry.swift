import Foundation

private typealias Callback = @convention(c) (UnsafePointer<CChar>) -> Void
private let callbackLock = NSLock()
private var callback: Callback?
private var bridge: NuxieUnityBridge?

private func deliver(_ value: String) {
  callbackLock.lock()
  defer { callbackLock.unlock() }
  if let callback { value.withCString { callback($0) } }
}

@_cdecl("nuxie_unity_attach")
public func nuxieUnityAttach(_ pointer: UnsafeRawPointer) {
  callbackLock.lock(); callback = unsafeBitCast(pointer, to: Callback.self); callbackLock.unlock()
  DispatchQueue.main.async {
    bridge?.invalidate()
    let next = NuxieUnityBridge()
    next.emit = { deliver($0) }
    bridge = next
  }
}
@_cdecl("nuxie_unity_detach")
public func nuxieUnityDetach() {
  callbackLock.lock(); callback = nil; callbackLock.unlock()
  DispatchQueue.main.async { bridge?.invalidate(); bridge = nil }
}
@_cdecl("nuxie_unity_dispatch")
public func nuxieUnityDispatch(_ pointer: UnsafePointer<CChar>) {
  let json = String(cString: pointer)
  DispatchQueue.main.async {
    guard let data = json.data(using: .utf8),
      let request = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
      let id = request["requestId"] as? String,
      let method = request["method"] as? String,
      let arguments = request["arguments"] as? [String: Any] else { return }
    func reply(_ value: [String: Any]) {
      if let bytes = try? JSONSerialization.data(withJSONObject: value) { deliver(String(decoding: bytes, as: UTF8.self)) }
    }
    guard let bridge else { reply(["requestId": id, "error": ["code": "sessionExpired", "message": "Native bridge detached"]]); return }
    bridge.invoke(method, arguments: arguments, resolve: { reply(["requestId": id, "result": $0 ?? NSNull()]) },
      reject: { code, message, _ in reply(["requestId": id, "error": ["code": code, "message": message]]) })
  }
}
