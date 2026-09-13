using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using Nuxie.Unity.Editor;
#endif
namespace Nuxie.Unity.Samples
{
    public sealed class SdkLab : MonoBehaviour
    {
        private INuxieClient sdk;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private readonly List<string> journal = new List<string>();
        private string iosKey = "", androidKey = "", customer = "unity-lab-player", feature = "energy", entityA = "character-a", entityB = "character-b", trigger = "sdk_lab_requested", operation = "", locale = "";
        private bool busy, simulated, dark;
        private Vector2 scroll;
        private float fieldWidth;
        private GUIStyle card, title, label, field, button;
        private Texture2D surface, canvas, control, accent;
        private readonly HashSet<string> presentations = new HashSet<string>();
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool ownsPause;
#if UNITY_EDITOR
        private NuxieEditorSimulator simulator;
#endif
        private bool runLocalChecks;
        [Serializable] private sealed class LocalConfiguration
        {
            public string iosKey, androidKey, customer, feature, entityA, entityB, trigger;
            public bool runApiChecks;
        }
        private void Awake()
        {
            sdk = Nuxie.Client; operation = NewOperation();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var asset = Resources.Load<TextAsset>("NuxieLab.local");
            if (asset != null)
            {
                var config = JsonUtility.FromJson<LocalConfiguration>(asset.text);
                iosKey = config.iosKey ?? iosKey; androidKey = config.androidKey ?? androidKey;
                customer = config.customer ?? customer; feature = config.feature ?? feature;
                entityA = config.entityA ?? entityA; entityB = config.entityB ?? entityB;
                trigger = config.trigger ?? trigger; runLocalChecks = config.runApiChecks;
            }
#endif
        }
        private void Start()
        {
#if !UNITY_EDITOR
            if (runLocalChecks && sdk.Status.State == NuxieStatusKind.Unconfigured) Run(async () =>
            {
                await sdk.ConfigureAsync(new NuxieOptions { IosApiKey = iosKey, AndroidApiKey = androidKey, Environment = NuxieEnvironment.Development, LogLevel = NuxieLogLevel.Debug });
                await LabChecks.RunAsync(sdk,customer,feature,entityA,entityB,operation,Record);
            });
#endif
        }
        private void OnEnable() { if (sdk != null) Subscribe(); }
        private void OnDisable() { Unsubscribe(); RestoreGame(); }
        private void OnDestroy() { foreach (var texture in new[] { surface,canvas,control,accent }) if (texture != null) Destroy(texture); }
        private string NewOperation() => "unity-" + Guid.NewGuid().ToString("N");
        private void Unsubscribe() { foreach (var d in subscriptions) d.Dispose(); subscriptions.Clear(); }
        private void Subscribe()
        {
            Unsubscribe(); subscriptions.Add(sdk.SubscribeStatus(s => Record("Status: " + s.State)));
            subscriptions.Add(sdk.SubscribeErrors(e => Record(e.Code + ": " + e.Message)));
            subscriptions.Add(sdk.SubscribeAppAction(a => Record("App Action: " + a.Name)));
            subscriptions.Add(sdk.SubscribeActivity(a => {
                Record(a.Name);
                HandleScreenActivity(a.Name,a.Properties);
            }));
        }
        private void HandleScreenActivity(string name,IReadOnlyDictionary<string,object> properties)
        {
            if (properties == null || !properties.TryGetValue("screen_id",out var screen)) return;
            var journey = properties.TryGetValue("journey_id",out var run) ? run.ToString() : "";
            var id = journey + ":" + screen;
            if (name == "screen_shown" && presentations.Add(id) && !ownsPause)
            {
                ownsPause = true; previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause;
                Time.timeScale = 0; AudioListener.pause = true; Record("Gameplay paused");
            }
            if (name == "screen_dismissed")
            {
                presentations.Remove(id);
                if (properties.TryGetValue("revealing_screen_id",out var revealed)) presentations.Add(journey + ":" + revealed);
                if (presentations.Count == 0) RestoreGame();
            }
        }
        private void RestoreGame() { if (ownsPause) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; ownsPause = false; Record("Gameplay restored: timeScale=" + Time.timeScale + ", audioPaused=" + AudioListener.pause); } presentations.Clear(); }
        private void Record(string text) { journal.Insert(0,text); if (journal.Count > 80) journal.RemoveAt(journal.Count - 1); Debug.Log("[Nuxie Lab] " + text); }
        private async void Run(Func<Task> work)
        {
            if (busy) return; busy = true;
            try { await work(); } catch (Exception e) { Record("FAILED: " + e.Message); } finally { busy = false; }
        }
        private void Styles()
        {
            if (card != null) return;
            surface = Round(dark ? new Color(.13f,.13f,.16f) : Color.white,20);
            canvas = Round(dark ? new Color(.06f,.06f,.08f) : new Color(.95f,.95f,.97f),0);
            control = Round(dark ? new Color(.22f,.22f,.25f) : new Color(.91f,.91f,.94f),16);
            accent = Round(new Color(.48f,.36f,.88f),24);
            var text = dark ? Color.white : new Color(.12f,.12f,.16f);
            card = new GUIStyle(GUI.skin.box) { border = new RectOffset(20,20,20,20), padding = new RectOffset(22,22,18,18), margin = new RectOffset(0,0,8,8), normal = { background = surface } };
            title = new GUIStyle(GUI.skin.label) { fontSize = 30,fontStyle = FontStyle.Bold,normal = { textColor = text } };
            label = new GUIStyle(GUI.skin.label) { fontSize = 18,wordWrap = true,normal = { textColor = text } };
            field = new GUIStyle(GUI.skin.textField) { fontSize = 18,border = new RectOffset(16,16,16,16),padding = new RectOffset(14,14,10,10),normal = { background = control,textColor = text },focused = { background = control,textColor = text } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18,border = new RectOffset(16,16,16,16),padding = new RectOffset(16,16,12,12),normal = { background = control,textColor = text },hover = { background = control,textColor = text },active = { background = accent,textColor = Color.white } };
        }
        private static Texture2D Round(Color color,int radius)
        {
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp }; var pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float dx = Mathf.Max(radius - x,x - (63 - radius)); float dy = Mathf.Max(radius - y,y - (63 - radius)); pixels[y*64+x] = dx > 0 && dy > 0 && dx*dx+dy*dy > radius*radius ? Color.clear : color; }
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }
        private void Input(string name,ref string value) { GUILayout.Label(name,label); value = name.Contains("key") ? GUILayout.PasswordField(value,'*',field,GUILayout.Width(fieldWidth),GUILayout.Height(44)) : GUILayout.TextField(value,field,GUILayout.Width(fieldWidth)); }
        private void Action(string name,Func<Task> work) { if (GUILayout.Button(name,button)) Run(work); }
        private void OnGUI()
        {
            Styles(); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),canvas);
            float scale = Mathf.Max(1,Screen.width/620f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
            var safe = Screen.safeArea; var areaWidth = safe.width/scale-40;
            fieldWidth = areaWidth-66;
            GUILayout.BeginArea(new Rect(safe.x/scale+20,(Screen.height-safe.yMax)/scale+16,areaWidth,safe.height/scale-32));
            scroll = GUILayout.BeginScrollView(scroll,false,true,GUIStyle.none,GUI.skin.verticalScrollbar);
            GUILayout.BeginVertical(GUILayout.Width(areaWidth-22));
            GUILayout.Label("Nuxie / Unity",title); GUILayout.Label(simulated ? "EDITOR SIMULATION · No backend or store" : "SDK Lab · Native mobile integration",label);
            GUILayout.Label("Connect. Observe. Play.",label); GUI.enabled = !busy;
            if (GUILayout.Button(dark ? "Light appearance" : "Dark appearance",button)) { dark = !dark; card = null; foreach (var t in new[] {surface,canvas,control,accent}) Destroy(t); }
            GUILayout.BeginVertical(card); GUILayout.Label("01  Connect",title);
            Input("iOS public key",ref iosKey); Input("Android public key",ref androidKey);
#if UNITY_EDITOR
            if (GUILayout.Button("Use explicit Editor simulator",button)) { Unsubscribe(); simulator?.Dispose(); simulator = new NuxieEditorSimulator(); sdk = simulator.Client; simulated = true; iosKey = "simulated"; androidKey = "simulated"; Subscribe(); }
#endif
            Action("Connect",() => sdk.ConfigureAsync(new NuxieOptions { IosApiKey = iosKey,AndroidApiKey = androidKey,Environment = NuxieEnvironment.Development,LogLevel = NuxieLogLevel.Debug }));
            GUILayout.Label(sdk.Status.State + " · " + sdk.Status.NativeVersion,label); Action("Disconnect",() => sdk.ShutdownAsync()); GUILayout.EndVertical();
            GUILayout.BeginVertical(card); GUILayout.Label("02  Player",title); Input("Customer ID",ref customer); Action("Identify",() => sdk.IdentifyAsync(customer)); Action("Read identity",async () => { var i = await sdk.GetIdentityAsync(); Record(i.CustomerId + " / anonymous " + i.AnonymousId); }); Action("Reset",() => sdk.ResetAsync()); Input("Locale (empty = device)",ref locale); Action("Set locale",() => sdk.SetLocaleAsync(string.IsNullOrWhiteSpace(locale) ? null : locale)); GUILayout.EndVertical();
            GUILayout.BeginVertical(card); GUILayout.Label("03  Feature access",title); Input("Feature ID",ref feature); Input("Entity A",ref entityA); Input("Entity B",ref entityB); GUILayout.Label("Snapshot: " + sdk.Features.State + " / revision " + sdk.Features.Revision,label);
            foreach (var item in sdk.Features.All) GUILayout.Label(item.Key + ": " + item.Value.Allowed + " · " + item.Value.Balance,label);
            Action("Query entity A",async () => { var a = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityA,Policy = FeatureQueryPolicy.Remote }); Record("Access: " + a.Allowed + " / " + a.Balance); });
            Input("Operation ID — reuse for retries",ref operation); if (GUILayout.Button("New operation ID",button)) operation = NewOperation();
            Action("Consume / retry one unit",async () => { var r = await sdk.ConsumeFeatureAsync(feature,new FeatureCommand { OperationId = operation,EntityId = entityA }); Record("Accepted " + r.Accepted + " · balance " + r.Balance + " · replay " + r.IdempotentReplay); });
            Action("Run API checks (spends one unit)",() => LabChecks.RunAsync(sdk,customer,feature,entityA,entityB,operation,Record)); GUILayout.EndVertical();
            GUILayout.BeginVertical(card); GUILayout.Label("04  Experiences",title); Input("Trigger event",ref trigger); Action("Trigger",() => sdk.TriggerAsync(trigger)); Action("Dismiss",async () => { await sdk.DismissAsync(); RestoreGame(); }); Action("Restore purchases",async () => { var r = await sdk.RestorePurchasesAsync(); Record("Restore: " + r.Outcome); });
#if UNITY_EDITOR
            if (simulated && GUILayout.Button("Simulate App Action",button)) simulator.EmitAppAction("sdk_lab_continue");
#endif
            var nextScene = SceneManager.GetActiveScene().name == "SdkLab" ? "Gameplay" : "SdkLab";
            if (Application.CanStreamedLevelBeLoaded(nextScene) && GUILayout.Button("Switch scene",button)) SceneManager.LoadScene(nextScene);
            GUILayout.EndVertical(); GUI.enabled = true; GUILayout.BeginVertical(card); GUILayout.Label("Activity",title); foreach (var entry in journal) GUILayout.Label(entry,label); GUILayout.EndVertical(); GUILayout.EndVertical(); GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
