using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;

namespace HarmonyModBuggos {
    public class ApClient : MonoBehaviour {
        public ApClient(IntPtr ptr) : base(ptr) { } 

        public static ApClient instance { get; set; }

        public ApSession integration;

        public static void Create() {
            if (instance == null) {
                ClassInjector.RegisterTypeInIl2Cpp<ApClient>();
                GameObject ApClientObject = new GameObject("apclient");
                ApClientObject.hideFlags = HideFlags.HideAndDontSave;
                instance = ApClientObject.AddComponent<ApClient>();
                instance.integration = new ApSession();
                UnityEngine.Object.DontDestroyOnLoad(ApClientObject);
            }
        }

        public void Start() {
            InternalLogger.LogInfo("Ap Instance Started.");
        }

        public void Update() {
            integration.Update();
            ModTick.Update(integration);
            // ItemDispatcher.Update(integration); put into MODTICK
            
        }

        public void OnDestroy() {
            if (integration == null)
            {
                return;
            }
            ApStore.Flush(integration);
            integration.TryDisconnect();
            
        }

        public void OnApplicationQuit()
        {
            if (integration == null)
            {
                return;
            }
            ApStore.Flush(integration);
        }

        public string Connect() {
            return integration.TryConnect();
        }

        public void Disconnect() {
            integration.TryDisconnect();
        }

        public bool IsConnected() {
            return integration != null ? integration.connected : false;
        }
    }
}