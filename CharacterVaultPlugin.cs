using System.Collections;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Landoria.CharacterVault
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class CharacterVaultPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "Landoria.CharacterVault";
        private const string PluginName = "Landoria.CharacterVault";
        private const string PluginVersion = "1.0.29";
        internal static ManualLogSource Log { get; private set; }
        internal static GracefulShutdownCoordinator Coordinator { get; private set; }
        internal static VoluntaryDisconnectCoordinator DisconnectCoordinator { get; private set; }
        internal static ServerDisconnectSaveCoordinator ServerDisconnects { get; private set; }
        internal static CharacterSaveStatusDisplay SaveStatus { get; private set; }
        internal static CharacterVaultPlugin Instance { get; private set; }
        internal static CharacterVaultSettings Settings { get; private set; }
        internal static ProfileTransferService Transfers { get; private set; }
        internal static bool PlayFabVerboseLogging { get; private set; }


        private Harmony _harmony;

        private void RegisterPatches0()
        {
            _harmony.CreateClassProcessor(typeof(CharacterVaultManualSavePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultSaveStatusPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PendingExitRequestPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultConnectionPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultHelloPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultAdmissionBarrierPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPermittedListReasonPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPermittedListMessagePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultDisconnectPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultClientNetworkDestroyPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPublishPlayFabFailurePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultKickBarrierPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultProfileSavedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultVoluntaryLogoutPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultContinueLogoutDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultGameDestroyDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultLogoutButtonDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultMenuQuitPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultStartingItemsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultApplyProfilePatch)).Patch();
        }

        private void RegisterPatches1()
        {
            _harmony.CreateClassProcessor(typeof(CharacterVaultWorldSavePatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabClientSocketCreatedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabServerSocketCreatedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabClientConnectVerboseLoggingPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabSessionFoundPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabSessionNotFoundPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabNetworkJoinedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabTransportConnectedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabSocketDisposedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabJoinLobbyFailedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabGetLobbyFailedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabPeerHandshakeCompletedPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabServerListOriginPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(PlayFabJoinCodeOriginPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabSendDataDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabEndpointResolutionDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabRawReceiveDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabDecodedReceiveDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabQueueEnqueueDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabQueueDropDiagnosticsPatch)).Patch();
        }

        private void RegisterPatches2()
        {
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabQueueResetDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabProcessAckDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultZNetStopAllDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultPlayFabSocketDisposeDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultLeaveLobbyDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultLeaveNetworkDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultScheduleResetPartyDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultResetPartyTimeoutDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultCancelResetPartyDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultResetPartyDiagnosticsPatch)).Patch();
            _harmony.CreateClassProcessor(typeof(CharacterVaultResetPartyTaskDiagnosticsPatch)).Patch();
        }

        private static void EnsureConnectionFailurePatch()
        {
            // Install the menu integration once, including alongside older mods.
            const string key = "Landoria.SharedLib.ConnectionFailureMenuPatch.v1";
            lock (System.AppDomain.CurrentDomain)
            {
                if (System.AppDomain.CurrentDomain.GetData(key) != null) return;
                new Harmony("Landoria.ConnectionFailureMessages")
                    .CreateClassProcessor(typeof(ConnectionFailureMenuPatch)).Patch();
                System.AppDomain.CurrentDomain.SetData(key, true);
            }
        }

        private void Awake()
        {
            Instance = this;
            PlayFabVerboseLogging = Config.Bind(
                "Diagnostics",
                "PlayFabVerboseLogging",
                false,
                "Enables verbose PlayFab Party logging for local diagnostics.").Value;
            Log = Logger;
            Logger.LogInfo($"AssemblyVersion: {GetType().Assembly.GetName().Version}.");
            _harmony = new Harmony(PluginGuid);
            EnsureConnectionFailurePatch();
            RegisterPatches0();
            RegisterPatches1();
            RegisterPatches2();
            EnsureConnectionFailurePatch();
            Settings = new CharacterVaultSettings();
            Transfers = new ProfileTransferService(SynchronizationContext.Current);
            Coordinator = new GracefulShutdownCoordinator(SynchronizationContext.Current);
            DisconnectCoordinator = new VoluntaryDisconnectCoordinator();
            ServerDisconnects = new ServerDisconnectSaveCoordinator();
            SaveStatus = new CharacterSaveStatusDisplay();
            PlayFabVerboseDiagnostics.Enable();
            CharacterVaultLobbyLeftDiagnostics.Register();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        internal void Run(IEnumerator routine)
        {
            StartCoroutine(routine);
        }

        internal void QuitNextFrame()
        {
            StartCoroutine(QuitAfterCurrentFrame());
        }

        private void Update()
        {
            CharacterVaultRejection.Tick();
            Transfers.MonitorFinalSaves();
        }

        private static IEnumerator QuitAfterCurrentFrame()
        {
            yield return null;
            Application.Quit();
        }

        private void OnDestroy()
        {
            CharacterVaultLobbyLeftDiagnostics.Unregister();
            DisconnectCoordinator?.Dispose();
            ServerDisconnects?.Dispose();
            Coordinator?.Dispose();
            Transfers?.Dispose();
            SaveStatus?.Dispose();
            CharacterVaultRejection.Clear();
            DisconnectCoordinator = null;
            ServerDisconnects = null;
            Coordinator = null;
            Transfers = null;
            SaveStatus = null;
            Settings = null;
            PlayFabVerboseLogging = false;
            Instance = null;
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            _harmony?.UnpatchSelf();
            _harmony = null;
            Log = null;
        }
    }
}
