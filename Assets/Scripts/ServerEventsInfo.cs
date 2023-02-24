using System.Collections.Generic;
using System;
using Fusion.Sockets;
using System.Linq;
using UnityEngine;

namespace Fusion.Collywobbles.Futsal
{

  public class ServerEventsInfo : SimulationBehaviour, INetworkRunnerCallbacks {

    private const int TIMEOUT = 5;
    private float TIME_COUNTER = TIMEOUT;

    private void Update() {

      TIME_COUNTER -= Time.deltaTime;

      if (TIME_COUNTER < 0) {
        TIME_COUNTER = TIMEOUT;

        if (Runner != null && Runner.IsServer) {
          Log.Debug($"Total Players: {Runner.ActivePlayers.Count()}");
        }
      }
    }

    private void OnDestroy() {
      Log.Debug($"{nameof(OnDestroy)}: {gameObject.name}");
    }

    public void OnConnectedToServer(NetworkRunner runner) {
      Log.Debug($"{nameof(OnConnectedToServer)}: {nameof(runner.CurrentConnectionType)}: {runner.CurrentConnectionType}, {nameof(runner.LocalPlayer)}: {runner.LocalPlayer}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
      Log.Debug($"{nameof(OnConnectFailed)}: {nameof(remoteAddress)}: {remoteAddress}, {nameof(reason)}: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
      Log.Debug($"{nameof(OnConnectRequest)}: {nameof(request.RemoteAddress)}: {request.RemoteAddress}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner) {
      Log.Debug($"{nameof(OnDisconnectedFromServer)}: {nameof(runner.LocalPlayer)}: {runner.LocalPlayer}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
      Log.Debug($"{nameof(OnPlayerJoined)}: {nameof(player)}: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
      Log.Debug($"{nameof(OnPlayerLeft)}: {nameof(player)}: {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
      Log.Debug($"{nameof(OnShutdown)}: {nameof(shutdownReason)}: {shutdownReason}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {
      Log.Debug($"{nameof(OnSessionListUpdated)}: {nameof(sessionList)}: {sessionList.Count}");
    }

    public void OnSceneLoadDone(NetworkRunner runner) {
      Log.Debug($"{nameof(OnSceneLoadDone)}: {nameof(runner.CurrentScene)}: {runner.CurrentScene}");
    }

    public void OnSceneLoadStart(NetworkRunner runner) {
      Log.Debug($"{nameof(OnSceneLoadStart)}: {nameof(runner.CurrentScene)}: {runner.CurrentScene}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {
      Log.Debug($"{nameof(OnCustomAuthenticationResponse)}");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) {
      Log.Debug($"{nameof(OnReliableDataReceived)}: {nameof(PlayerRef)}:{player}, {nameof(data)}:{data.Count}");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {
      Log.Debug($"{nameof(OnHostMigration)}: {nameof(HostMigrationToken)}: {hostMigrationToken}");
    }
  }
}